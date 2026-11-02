using ERP.Konfeksiyon.Application.Abstractions;
using ERP.Konfeksiyon.Application.Features.Shifts;
using ERP.Konfeksiyon.Domain.Entities;
using ERP.Konfeksiyon.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ERP.Konfeksiyon.Application.Features.WorkSessions;

/// <summary>Operatör için şu an zamanı gelmiş planlı mola.</summary>
/// <param name="CanStartNow">Mola şimdi başlatılabilir mi (politika gereği önce eldeki iş bildirilmeli olabilir).</param>
/// <param name="Hint">Başlatılamıyorsa operatöre gösterilecek açıklama.</param>
public sealed record DueBreakDto(
    int BreakScheduleId, string Name, TimeOnly StartTime, TimeOnly EndTime, int PlannedMinutes, BreakPolicy Policy,
    bool CanStartNow, string? Hint);

/// <summary>
/// Oturum duraklamalarının (mola, ara, arıza, demet bekleme) ortak mantığı. Oturum, üretim ve makine
/// durumu servisleri aynı kuralları buradan kullanır. Çağıran, makinenin oturum kilidini almış olmalı ve
/// değişiklikleri kendisi kaydeder.
/// </summary>
public interface ISessionInterruptions
{
    /// <summary>Operatörün grubuna ve şu anki saate göre zamanı gelmiş, henüz çıkılmamış planlı mola.</summary>
    Task<DueBreakDto?> FindDueBreakAsync(WorkSession session, CancellationToken cancellationToken);

    /// <summary>Oturumu duraklatır; süren başka bir duraklama varsa önce onu sonlandırır.</summary>
    Task BeginAsync(
        WorkSession session, WorkSessionPauseType type, int? stopReasonId, string? note, int? breakScheduleId,
        CancellationToken cancellationToken);

    /// <summary>Süren duraklamayı sonlandırır ve oturumu çalışır duruma döndürür.</summary>
    Task EndAsync(WorkSession session, CancellationToken cancellationToken);

    /// <summary>
    /// Üretim hareketi (operasyon başlatma / adet bildirme) operatörün işe döndüğünü gösterir: makinedeki
    /// oturumun süren duraklaması sonlanır. Makine arızası bunun dışındadır; onu arıza giderme bildirimi kapatır.
    /// </summary>
    Task ResumeForProductionAsync(int machineId, CancellationToken cancellationToken);

    /// <summary>Makine arızalandı: çalışmakta olan oturum arıza durumuna geçer.</summary>
    Task OnMachineBreakdownAsync(int machineId, CancellationToken cancellationToken);

    /// <summary>Makine arızadan çıktı: arıza nedeniyle duran oturum çalışmaya döner.</summary>
    Task OnMachineRecoveredAsync(int machineId, CancellationToken cancellationToken);
}

public sealed class SessionInterruptions(IKonfeksiyonDbContext db, IFactoryClock clock, IOptions<ProductionOptions> options)
    : ISessionInterruptions
{
    public async Task<DueBreakDto?> FindDueBreakAsync(WorkSession session, CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var localTime = TimeOnly.FromDateTime(clock.ToLocal(now));
        var lateStart = options.Value.BreakLateStartMinutes;

        var breakGroupId = await db.Operators
            .IgnoreQueryFilters()
            .Where(o => o.Id == session.OperatorId)
            .Select(o => o.BreakGroupId)
            .SingleOrDefaultAsync(cancellationToken);

        // Mola planı sayısı azdır; zaman karşılaştırması (gece vardiyası dahil) bellekte yapılır.
        var schedules = await db.BreakSchedules
            .AsNoTracking()
            .Include(b => b.Shift)
            .Where(b => b.IsActive && b.Shift.IsActive && (b.BreakGroupId == null || b.BreakGroupId == breakGroupId))
            .ToListAsync(cancellationToken);

        var due = schedules
            .Select(schedule =>
            {
                var sinceShiftStart = schedule.Shift.OffsetOf(localTime);
                var from = schedule.Shift.OffsetOf(schedule.StartTime);
                var planned = ShiftTimes.MinutesBetween(schedule.StartTime, schedule.EndTime);
                return new { schedule, sinceShiftStart, from, planned };
            })
            .Where(x => x.sinceShiftStart >= x.from && x.sinceShiftStart <= x.from + x.planned + lateStart)
            // Gruba özel mola, herkese açık moladan önce gelir.
            .OrderByDescending(x => x.schedule.BreakGroupId.HasValue)
            .ThenBy(x => x.from)
            .ToList();

        foreach (var candidate in due)
        {
            // Bugünkü bu mola başlangıcından beri operatör bu molaya çıktı mı?
            var occurrenceStart = now.AddMinutes(-(candidate.sinceShiftStart - candidate.from) - 1);
            var taken = await db.WorkSessionPauses.AnyAsync(
                p => p.BreakScheduleId == candidate.schedule.Id
                    && p.WorkSession.OperatorId == session.OperatorId
                    && p.StartedAt >= occurrenceStart,
                cancellationToken);
            if (taken)
            {
                continue;
            }

            var working = candidate.schedule.Policy != BreakPolicy.Immediate
                && await db.BundleOperations.AnyAsync(
                    o => o.MachineId == session.MachineId
                        && o.OperatorId == session.OperatorId
                        && o.Status == BundleOperationStatus.InProgress,
                    cancellationToken);

            return new DueBreakDto(
                candidate.schedule.Id,
                candidate.schedule.Name,
                candidate.schedule.StartTime,
                candidate.schedule.EndTime,
                candidate.planned,
                candidate.schedule.Policy,
                !working,
                working ? HintFor(candidate.schedule.Policy) : null);
        }

        return null;
    }

    public async Task BeginAsync(
        WorkSession session, WorkSessionPauseType type, int? stopReasonId, string? note, int? breakScheduleId,
        CancellationToken cancellationToken)
    {
        var open = await OpenPauseAsync(session.Id, cancellationToken);
        if (open is not null)
        {
            open.End(clock.UtcNow);
            // Tekil index (oturum başına tek açık duraklama) için önce eskisi kapatılır.
            await db.SaveChangesAsync(cancellationToken);
        }

        session.Interrupt(type);
        db.WorkSessionPauses.Add(new WorkSessionPause
        {
            WorkSessionId = session.Id,
            Type = type,
            StopReasonId = type == WorkSessionPauseType.Pause ? stopReasonId : null,
            BreakScheduleId = type == WorkSessionPauseType.Break ? breakScheduleId : null,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
            StartedAt = clock.UtcNow,
        });
    }

    public async Task EndAsync(WorkSession session, CancellationToken cancellationToken)
    {
        var open = await OpenPauseAsync(session.Id, cancellationToken);
        open?.End(clock.UtcNow);

        if (session.IsOpen)
        {
            session.Resume();
        }
    }

    public async Task ResumeForProductionAsync(int machineId, CancellationToken cancellationToken)
    {
        var session = await OpenSessionAsync(machineId, cancellationToken);
        if (session is { IsInterrupted: true } && session.Status != WorkSessionStatus.MachineBreakdown)
        {
            await EndAsync(session, cancellationToken);
        }
    }

    public async Task OnMachineBreakdownAsync(int machineId, CancellationToken cancellationToken)
    {
        var session = await OpenSessionAsync(machineId, cancellationToken);

        // Moladaki / ara vermiş operatörün durumu ezilmez: arıza onun çalışmasını kesmedi.
        if (session is { Status: WorkSessionStatus.Active or WorkSessionStatus.WaitingForBundle })
        {
            await BeginAsync(session, WorkSessionPauseType.MachineBreakdown, null, null, null, cancellationToken);
        }
    }

    public async Task OnMachineRecoveredAsync(int machineId, CancellationToken cancellationToken)
    {
        var session = await OpenSessionAsync(machineId, cancellationToken);
        if (session is { Status: WorkSessionStatus.MachineBreakdown })
        {
            await EndAsync(session, cancellationToken);
        }
    }

    private Task<WorkSession?> OpenSessionAsync(int machineId, CancellationToken cancellationToken) =>
        db.WorkSessions.SingleOrDefaultAsync(s => s.MachineId == machineId && s.LogoutAt == null, cancellationToken);

    private Task<WorkSessionPause?> OpenPauseAsync(long sessionId, CancellationToken cancellationToken) =>
        db.WorkSessionPauses.SingleOrDefaultAsync(p => p.WorkSessionId == sessionId && p.EndedAt == null, cancellationToken);

    private static string HintFor(BreakPolicy policy) => policy switch
    {
        BreakPolicy.AfterQuantity => "Molaya çıkmadan önce yaptığınız adedi bildirin.",
        BreakPolicy.AfterOperation => "Molaya çıkmadan önce eldeki operasyonu tamamlayıp adedini bildirin.",
        _ => "Molaya çıkmadan önce eldeki demeti tamamlayıp adedini bildirin.",
    };
}
