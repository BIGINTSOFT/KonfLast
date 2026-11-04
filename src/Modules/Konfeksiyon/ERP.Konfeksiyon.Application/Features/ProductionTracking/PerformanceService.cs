using ERP.Konfeksiyon.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Konfeksiyon.Application.Features.Shifts;
using ERP.Konfeksiyon.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ERP.Konfeksiyon.Application.Features.ProductionTracking;

public enum PerformanceGroupBy
{
    Operator,
    Machine,
    Line,

    /// <summary>Operasyonlar arası karşılaştırma (darboğaz analizi).</summary>
    Operation,
}

public sealed class PerformanceQuery
{
    /// <summary>Aralığın başı (UTC, dahil).</summary>
    public DateTime From { get; set; }

    /// <summary>Aralığın sonu (UTC, hariç).</summary>
    public DateTime To { get; set; }

    public PerformanceGroupBy GroupBy { get; set; } = PerformanceGroupBy.Operator;
}

/// <summary>Süreler dakikadır. Tanımlar için bkz. <see cref="PerformanceCalculator"/>.</summary>
/// <param name="NetWorkingMinutes">Mevcudiyet − planlı mola − ara − makine arızası.</param>
/// <param name="ActiveMinutes">Bir operasyonun fiilen işlemde olduğu süre.</param>
/// <param name="EarnedMinutes">Üretilen adet × SAM.</param>
/// <param name="TargetQuantity">%100 verimde aynı sürede üretilmesi gereken adet.</param>
/// <param name="EfficiencyPercent">Kazanılan dakika ÷ (net çalışma − demet bekleme).</param>
/// <param name="UtilizationPercent">Aktif üretim ÷ net çalışma.</param>
public sealed record PerformanceRowDto(
    int Id,
    string Code,
    string Name,
    double PresenceMinutes,
    double BreakMinutes,
    double PauseMinutes,
    double BreakdownMinutes,
    double WaitingMinutes,
    double NetWorkingMinutes,
    double ActiveMinutes,
    int Produced,
    int Rejected,
    double EarnedMinutes,
    int? TargetQuantity,
    double? EfficiencyPercent,
    double? UtilizationPercent,
    double? RejectRatePercent,
    double? ProducedPerHour,
    double? TargetPerHour);

/// <param name="Total">Tüm satırların toplamı üzerinden aynı hesap (operasyon boyutunda süreler aktif üretimdir).</param>
public sealed record PerformanceReportDto(
    DateTime From, DateTime To, PerformanceGroupBy GroupBy, PerformanceRowDto Total, IReadOnlyList<PerformanceRowDto> Rows);

public interface IPerformanceService
{
    /// <summary>Tarih aralığında operatör / makine / bant / operasyon bazında süre, üretim ve verim.</summary>
    Task<Result<PerformanceReportDto>> GetAsync(PerformanceQuery query, CancellationToken cancellationToken);
}

/// <summary>
/// Performans verisini oturum, duraklama ve üretim olayı kayıtlarından toplar; hesabı
/// <see cref="PerformanceCalculator"/> yapar. Kayıtlar değiştirilmediği için sonuç her zaman yeniden üretilebilir.
/// </summary>
public sealed class PerformanceService(IKonfeksiyonDbContext db, IFactoryClock clock) : IPerformanceService
{
    public const int MaxRangeDays = 31;

    /// <summary>"Banta atanmamış makineler" satırının kimliği.</summary>
    private const int NoLine = 0;

    /// <summary>Aralıktan önce başlamış operasyonların başlangıcını bulmak için geriye bakılan süre.</summary>
    private static readonly TimeSpan WorkLookBack = TimeSpan.FromDays(1);

    public async Task<Result<PerformanceReportDto>> GetAsync(PerformanceQuery query, CancellationToken cancellationToken)
    {
        if (query.From == default || query.To == default || query.To <= query.From)
        {
            return RangeError("Geçerli bir tarih aralığı verin: bitiş, başlangıçtan sonra olmalıdır.");
        }

        if ((query.To - query.From).TotalDays > MaxRangeDays)
        {
            return RangeError($"Performans için tarih aralığı en fazla {MaxRangeDays} gün olabilir.");
        }

        var from = DateTime.SpecifyKind(query.From.ToUniversalTime(), DateTimeKind.Utc);
        var to = DateTime.SpecifyKind(query.To.ToUniversalTime(), DateTimeKind.Utc);
        // Süren oturum / duraklama / operasyon "şu an"a kadar sayılır; gelecek sayılmaz.
        var now = clock.UtcNow;
        var until = to < now ? to : now;

        var sessions = (await db.WorkSessions
                .AsNoTracking()
                .Where(s => s.LoginAt < to && (s.LogoutAt == null || s.LogoutAt > from))
                .Select(s => new { s.Id, s.OperatorId, s.MachineId, s.LoginAt, s.LogoutAt })
                .ToListAsync(cancellationToken))
            .Select(s => new SessionFact(s.Id, s.OperatorId, s.MachineId, s.LoginAt, s.LogoutAt ?? until))
            .ToList();

        var pauses = (await db.WorkSessionPauses
                .AsNoTracking()
                .Where(p => p.StartedAt < to && (p.EndedAt == null || p.EndedAt > from))
                .Select(p => new { p.WorkSessionId, p.Type, p.StartedAt, p.EndedAt })
                .ToListAsync(cancellationToken))
            .Select(p => new PauseFact(p.WorkSessionId, p.Type, p.StartedAt, p.EndedAt ?? until))
            .ToList();

        var lookBack = from - WorkLookBack;
        var events = await db.BundleOperationEvents
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(e => e.OccurredAt >= lookBack && e.OccurredAt < to)
            .OrderBy(e => e.Id)
            .Select(e => new
            {
                e.BundleOperationId,
                e.EventType,
                e.WorkSessionId,
                e.OperatorId,
                e.MachineId,
                e.BundleOperation.OperationId,
                e.BundleOperation.Sam,
                e.ProducedQuantity,
                e.RejectedQuantity,
                e.OccurredAt,
                e.CountsForPerformance,
            })
            .ToListAsync(cancellationToken);

        // Performansa sayılmayan elle bildirimler (operatörsüz ya da "sayılmasın") verim hesabına girmez.
        var outputs = events
            .Where(e => e.CountsForPerformance && e.OperatorId != null
                && e.EventType is BundleOperationEventType.QuantityReported or BundleOperationEventType.RejectionReported)
            .Select(e => new OutputFact(
                e.WorkSessionId, e.OperatorId!.Value, e.MachineId, e.OperationId, e.OccurredAt, e.ProducedQuantity,
                e.RejectedQuantity, e.Sam))
            .ToList();

        // Çalışma aralıkları: başlat / devam → bırak / tamamla. Kapanmamış aralık şu ana kadar sürer.
        var work = new List<WorkFact>();
        foreach (var operation in events.GroupBy(e => e.BundleOperationId))
        {
            var open = operation.First();
            var isOpen = false;
            foreach (var e in operation)
            {
                if (e.EventType is BundleOperationEventType.OperationStarted or BundleOperationEventType.OperationResumed)
                {
                    open = e;
                    isOpen = true;
                }
                else if (isOpen && e.EventType is BundleOperationEventType.OperationPaused or BundleOperationEventType.OperationCompleted)
                {
                    // Performansa sayılmayan elle kapatmada süre bilinmez: aralık kimseye çalışma süresi olarak yazılmaz.
                    isOpen = false;
                    if (!e.CountsForPerformance || open.OperatorId is not { } worker)
                    {
                        continue;
                    }

                    work.Add(new WorkFact(
                        open.WorkSessionId, worker, open.MachineId, open.OperationId, open.OccurredAt, e.OccurredAt));
                }
            }

            if (isOpen && open.OperatorId is { } openWorker)
            {
                work.Add(new WorkFact(
                    open.WorkSessionId, openWorker, open.MachineId, open.OperationId, open.OccurredAt, until));
            }
        }

        Func<int, DateTime, int?> lineOf = query.GroupBy == PerformanceGroupBy.Line
            ? await LoadLineResolverAsync(
                sessions.Select(s => s.MachineId).Concat(work.Select(w => w.MachineId)).Concat(outputs.Select(o => o.MachineId)),
                cancellationToken)
            : (_, _) => NoLine;

        IReadOnlyDictionary<int, PerformanceFigures> Compute(
            Func<SessionFact, int?> sessionKey, Func<WorkFact, int?> workKey, Func<OutputFact, int?> outputKey,
            bool activeBase = false) =>
            PerformanceCalculator.Calculate(from, to, sessions, pauses, work, outputs, sessionKey, workKey, outputKey, activeBase);

        var operationBase = query.GroupBy == PerformanceGroupBy.Operation;
        var figures = query.GroupBy switch
        {
            PerformanceGroupBy.Machine => Compute(s => s.MachineId, w => w.MachineId, o => o.MachineId),
            PerformanceGroupBy.Line => Compute(
                s => lineOf(s.MachineId, s.Start), w => lineOf(w.MachineId, w.Start), o => lineOf(o.MachineId, o.At)),
            PerformanceGroupBy.Operation => Compute(_ => null, w => w.OperationId, o => o.OperationId, activeBase: true),
            _ => Compute(s => s.OperatorId, w => w.OperatorId, o => o.OperatorId),
        };

        // Toplam: tek anahtar altında aynı hesap (yüzdeler satırların ortalaması değil, toplamın oranıdır).
        var total = PerformanceCalculator
            .Calculate(
                from, to, sessions, pauses, work, outputs, s => operationBase ? null : 0, _ => 0, _ => 0, operationBase)
            .GetValueOrDefault(0)
            ?? new PerformanceFigures(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, null, null, null, null, null, null);

        var names = await LoadNamesAsync(query.GroupBy, figures.Keys.ToList(), cancellationToken);
        var rows = figures
            .Select(f =>
            {
                var (code, name) = names.GetValueOrDefault(f.Key, ("?", "?"));
                return ToRow(f.Key, code, name, f.Value);
            })
            // Darboğaz en üstte: önce verimi en düşük olan.
            .OrderBy(r => r.EfficiencyPercent ?? double.MaxValue)
            .ThenBy(r => r.Code, StringComparer.Ordinal)
            .ToList();

        return new PerformanceReportDto(from, to, query.GroupBy, ToRow(0, "", "Toplam", total), rows);
    }

    /// <summary>Makinenin verilen anda hangi bantta olduğunu söyleyen fonksiyon (atama geçmişinden).</summary>
    private async Task<Func<int, DateTime, int?>> LoadLineResolverAsync(
        IEnumerable<int> machineIds, CancellationToken cancellationToken)
    {
        var ids = machineIds.Distinct().ToList();
        var assignments = (await db.LineMachineAssignments
                .AsNoTracking()
                .Where(a => ids.Contains(a.MachineId))
                .Select(a => new { a.MachineId, a.ProductionLineId, a.AssignedAt, a.UnassignedAt })
                .ToListAsync(cancellationToken))
            .ToLookup(a => a.MachineId);

        return (machineId, at) => assignments[machineId]
            .Where(a => a.AssignedAt <= at && (a.UnassignedAt == null || a.UnassignedAt > at))
            .Select(a => (int?)a.ProductionLineId)
            .FirstOrDefault() ?? NoLine;
    }

    private async Task<Dictionary<int, (string Code, string Name)>> LoadNamesAsync(
        PerformanceGroupBy groupBy, List<int> ids, CancellationToken cancellationToken)
    {
        // Sonradan silinen kayıtlar da adlarıyla görünür.
        var names = groupBy switch
        {
            PerformanceGroupBy.Machine => await db.Machines.IgnoreQueryFilters().Where(m => ids.Contains(m.Id))
                .Select(m => new { m.Id, Code = m.MachineCode, Name = m.MachineName }).ToListAsync(cancellationToken),
            PerformanceGroupBy.Line => await db.ProductionLines.IgnoreQueryFilters().Where(l => ids.Contains(l.Id))
                .Select(l => new { l.Id, Code = l.LineCode, l.Name }).ToListAsync(cancellationToken),
            PerformanceGroupBy.Operation => await db.Operations.IgnoreQueryFilters().Where(o => ids.Contains(o.Id))
                .Select(o => new { o.Id, Code = o.OperationCode, o.Name }).ToListAsync(cancellationToken),
            _ => await db.Operators.IgnoreQueryFilters().Where(o => ids.Contains(o.Id))
                .Select(o => new { o.Id, Code = o.OperatorCode, Name = o.FirstName + " " + o.LastName })
                .ToListAsync(cancellationToken),
        };

        var result = names.ToDictionary(n => n.Id, n => (n.Code, n.Name));
        if (groupBy == PerformanceGroupBy.Line)
        {
            result[NoLine] = ("—", "Banta atanmamış makineler");
        }

        return result;
    }

    private static PerformanceRowDto ToRow(int id, string code, string name, PerformanceFigures f) => new(
        id, code, name, f.PresenceMinutes, f.BreakMinutes, f.PauseMinutes, f.BreakdownMinutes, f.WaitingMinutes,
        f.NetWorkingMinutes, f.ActiveMinutes, f.Produced, f.Rejected, f.EarnedMinutes, f.TargetQuantity,
        f.EfficiencyPercent, f.UtilizationPercent, f.RejectRatePercent, f.ProducedPerHour, f.TargetPerHour);

    private static Error RangeError(string message) => new(
        ErrorCodes.ValidationError, message, ErrorType.Validation,
        new Dictionary<string, string[]> { ["from"] = [message] });
}
