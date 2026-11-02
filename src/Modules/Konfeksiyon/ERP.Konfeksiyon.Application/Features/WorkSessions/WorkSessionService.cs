using ERP.Konfeksiyon.Application.Abstractions;
using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Konfeksiyon.Application.Features.Production;
using ERP.Konfeksiyon.Application.Features.Rfid;
using ERP.Konfeksiyon.Application.Features.Shifts;
using ERP.Konfeksiyon.Domain.Entities;
using ERP.Konfeksiyon.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ERP.Konfeksiyon.Application.Features.WorkSessions;

public interface IWorkSessionService
{
    /// <summary>
    /// Tablette okutulan personel kartıyla, tabletin makinesinde çalışma oturumu açar. Makinede ya da
    /// operatörde süren iş yoksa önceki açık oturumlar kendiliğinden kapatılır (devralma / makine değişimi).
    /// </summary>
    Task<Result<TabletSessionDto>> LoginAsync(RfidReadRequest request, CancellationToken cancellationToken);

    /// <summary>Tabletin makinesindeki açık oturumu kapatır; açık oturum yoksa bir şey yapmaz.</summary>
    Task<Result> LogoutAsync(CancellationToken cancellationToken);

    /// <summary>Tabletin makinesindeki açık oturum (yoksa boş).</summary>
    Task<Result<TabletSessionDto?>> GetCurrentAsync(CancellationToken cancellationToken);

    /// <summary>Operatörün planlı molaya çıkması, ara vermesi ya da demet beklediğini bildirmesi.</summary>
    Task<Result<TabletSessionDto>> PauseAsync(PauseSessionRequest request, CancellationToken cancellationToken);

    /// <summary>Operatörün duraklamadan çalışmaya dönmesi.</summary>
    Task<Result<TabletSessionDto>> ResumeAsync(ResumeSessionRequest request, CancellationToken cancellationToken);

    Task<PagedResult<WorkSessionDto>> GetListAsync(WorkSessionListQuery query, CancellationToken cancellationToken);

    /// <summary>Oturumun duraklamaları (mola, ara, arıza, demet bekleme), eskiden yeniye.</summary>
    Task<Result<IReadOnlyList<WorkSessionPauseDto>>> GetPausesAsync(long id, CancellationToken cancellationToken);

    /// <summary>
    /// Yetkili kullanıcının açık kalmış bir oturumu kapatması. Operatörün süren işi varsa
    /// <paramref name="releaseWork"/> ile iş adet bildirilmeden bırakılır (kalan adet sonra sürdürülür); aksi halde
    /// WORK_IN_PROGRESS döner.
    /// </summary>
    Task<Result<WorkSessionDto>> CloseAsync(long id, bool releaseWork, CancellationToken cancellationToken);

    /// <summary>
    /// Vardiyası biten (ya da hiçbir vardiyaya denk gelmeyip çok uzun açık kalan) ve süren işi olmayan
    /// oturumları kapatır. Kapatılan oturum sayısını döner.
    /// </summary>
    Task<int> CloseExpiredAsync(CancellationToken cancellationToken);
}

public sealed class WorkSessionService(
    IKonfeksiyonDbContext db,
    IAppLock appLock,
    ICurrentUser currentUser,
    IStationResolver stations,
    IRfidReadService rfidReads,
    IOperatorQualificationService qualifications,
    ISessionInterruptions interruptions,
    IStopReasonLookup stopReasons,
    IFloorAuthorization floorAuthorization,
    IStaffRoleSettingService roleSettings,
    IFactoryClock clock,
    Production.IProductionService production,
    IOptions<ProductionOptions> options) : IWorkSessionService
{
    private const string SystemActor = "system";
    private const int AutoCloseBatch = 500;
    private static readonly TimeSpan LockTimeout = TimeSpan.FromSeconds(10);

    private static readonly Error Busy = new(
        ErrorCodes.ResourceBusy, "Oturum şu an başka bir işlemle değiştiriliyor. Lütfen tekrar deneyin.",
        ErrorType.Conflict);

    private static readonly Error NoSession = new(
        ErrorCodes.NoActiveSession, "Bu makinede açık oturum yok. Önce personel kartınızı okutun.");

    private DateTime UtcNow => clock.UtcNow;

    /// <summary>Makinenin oturumlarını değiştiren her işlem bu kilidi alır (operasyon başlatma dahil).</summary>
    public static string MachineLock(int machineId) => $"work-session:machine:{machineId}";

    private static string OperatorLock(int operatorId) => $"work-session:operator:{operatorId}";

    public async Task<Result<TabletSessionDto>> LoginAsync(RfidReadRequest request, CancellationToken cancellationToken)
    {
        var resolved = await stations.ResolveAsync(cancellationToken);
        if (!resolved.IsSuccess)
        {
            return resolved.Error!;
        }

        var station = resolved.Value;
        if (!station.MachineActive)
        {
            return new Error(ErrorCodes.MachineInactive, $"{station.MachineCode} makinesi pasif durumda.");
        }

        // Kart çözümü, çift okuma kontrolü ve okuma kaydı RFID katmanında yapılır.
        var read = await rfidReads.ReadOperatorAsync(request, cancellationToken);
        if (!read.IsSuccess)
        {
            return read.Error!;
        }

        var operatorId = read.Value.OperatorId;

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (!await appLock.TryAcquireAsync(MachineLock(station.MachineId), LockTimeout, cancellationToken)
            || !await appLock.TryAcquireAsync(OperatorLock(operatorId), LockTimeout, cancellationToken))
        {
            return Busy;
        }

        var open = await db.WorkSessions
            .Where(s => s.LogoutAt == null && (s.MachineId == station.MachineId || s.OperatorId == operatorId))
            .ToListAsync(cancellationToken);

        var existing = open.SingleOrDefault(s => s.MachineId == station.MachineId && s.OperatorId == operatorId);
        if (existing is not null)
        {
            return await ToTabletDtoAsync(existing, duplicate: true, cancellationToken);
        }

        if (await qualifications.CheckAsync(operatorId, station.MachineId, station.OperationId, cancellationToken)
            is { } notQualified)
        {
            return notQualified;
        }

        var now = UtcNow;
        var actor = currentUser.UserName ?? station.TabletCode;

        // Makinede başka operatörün oturumu: işi sürmüyorsa devralınır (çıkış yapmayı unutmuş olabilir).
        var onMachine = open.SingleOrDefault(s => s.MachineId == station.MachineId);
        if (onMachine is not null)
        {
            if (await HasWorkInProgressAsync(onMachine, cancellationToken))
            {
                return new Error(
                    ErrorCodes.WorkInProgress,
                    "Bu makinede başka bir operatörün süren işi var; önce o işin adedi bildirilmeli.",
                    ErrorType.Conflict);
            }

            await CloseAsync(onMachine, WorkSessionEndReason.TakenOver, now, actor, cancellationToken);
        }

        // Operatörün başka makinedeki oturumu: işi sürmüyorsa kapatılır (makine değişimi).
        var elsewhere = open.SingleOrDefault(s => s.OperatorId == operatorId);
        if (elsewhere is not null)
        {
            if (await HasWorkInProgressAsync(elsewhere, cancellationToken))
            {
                var machineCode = await db.Machines
                    .IgnoreQueryFilters()
                    .Where(m => m.Id == elsewhere.MachineId)
                    .Select(m => m.MachineCode)
                    .SingleAsync(cancellationToken);
                return new Error(
                    ErrorCodes.OperatorBusy,
                    $"Operatörün {machineCode} makinesinde süren bir işi var; önce onun adedi bildirilmeli.",
                    ErrorType.Conflict);
            }

            await CloseAsync(elsewhere, WorkSessionEndReason.MovedToAnotherMachine, now, actor, cancellationToken);
        }

        if (onMachine is not null || elsewhere is not null)
        {
            // Tekil index'ler (operatör / makine başına tek açık oturum) için önce eskiler kapatılır.
            await db.SaveChangesAsync(cancellationToken);
        }

        var session = new WorkSession
        {
            OperatorId = operatorId, MachineId = station.MachineId, TabletId = station.TabletId, LoginAt = now,
        };
        db.WorkSessions.Add(session);
        await db.SaveChangesAsync(cancellationToken);

        // Arızalı makinede oturum açan operatör çalışamaz: oturum arıza durumunda başlar.
        if (station.MachineStatus == MachineStatus.Breakdown)
        {
            await interruptions.BeginAsync(
                session, WorkSessionPauseType.MachineBreakdown, null, null, null, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);

        return await ToTabletDtoAsync(session, duplicate: false, cancellationToken);
    }

    public async Task<Result> LogoutAsync(CancellationToken cancellationToken)
    {
        var resolved = await stations.ResolveAsync(cancellationToken);
        if (!resolved.IsSuccess)
        {
            return resolved.Error!;
        }

        var station = resolved.Value;

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (!await appLock.TryAcquireAsync(MachineLock(station.MachineId), LockTimeout, cancellationToken))
        {
            return Busy;
        }

        var session = await OpenSessionOnAsync(station.MachineId, cancellationToken);
        if (session is null)
        {
            return Result.Success();
        }

        if (await HasWorkInProgressAsync(session, cancellationToken))
        {
            return WorkInProgress;
        }

        await CloseAsync(
            session, WorkSessionEndReason.Logout, UtcNow, currentUser.UserName ?? station.TabletCode, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<TabletSessionDto?>> GetCurrentAsync(CancellationToken cancellationToken)
    {
        var resolved = await stations.ResolveAsync(cancellationToken);
        if (!resolved.IsSuccess)
        {
            return resolved.Error!;
        }

        var session = await db.WorkSessions
            .AsNoTracking()
            .SingleOrDefaultAsync(s => s.MachineId == resolved.Value.MachineId && s.LogoutAt == null, cancellationToken);

        return Result<TabletSessionDto?>.Success(
            session is null ? null : await ToTabletDtoAsync(session, duplicate: false, cancellationToken));
    }

    public async Task<Result<TabletSessionDto>> PauseAsync(
        PauseSessionRequest request, CancellationToken cancellationToken)
    {
        var resolved = await stations.ResolveAsync(cancellationToken);
        if (!resolved.IsSuccess)
        {
            return resolved.Error!;
        }

        var station = resolved.Value;

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (!await appLock.TryAcquireAsync(MachineLock(station.MachineId), LockTimeout, cancellationToken))
        {
            return Busy;
        }

        var session = await OpenSessionOnAsync(station.MachineId, cancellationToken);
        if (session is null)
        {
            return NoSession;
        }

        // Aynı türde duraklama zaten sürüyorsa istek tekrardır.
        if (session.Status == WorkSession.StatusFor(request.Type))
        {
            return await ToTabletDtoAsync(session, duplicate: true, cancellationToken);
        }

        if (session.Status == WorkSessionStatus.MachineBreakdown)
        {
            return new Error(
                ErrorCodes.SessionInterrupted, "Makine arızalı. Arıza giderilince 'Arıza giderildi' ile bildirin.");
        }

        int? breakScheduleId = null;
        string? approvedBy = null;
        switch (request.Type)
        {
            case WorkSessionPauseType.Pause:
            {
                // Neden zorunluluğu doğrulamada denetlenir; burada ana veride geçerli olduğuna bakılır.
                var reason = await stopReasons.ResolveAsync(
                    request.ReasonId!.Value, StopReasonCategory.Pause, cancellationToken);
                if (!reason.IsSuccess)
                {
                    return reason.Error!;
                }

                // Ara yetkiyle verilir: operatörün kendi yetkisi ya da okutulan bant şefi kartı.
                var actor = await floorAuthorization.AuthorizeAsync(
                    FloorAction.Pause, session.OperatorId, request.AuthorizerUid, cancellationToken);
                if (!actor.IsSuccess)
                {
                    return actor.Error!;
                }

                approvedBy = actor.Value.ByCard ? actor.Value.DisplayName : null;
                break;
            }

            case WorkSessionPauseType.Break:
            {
                var due = await interruptions.FindDueBreakAsync(session, cancellationToken);
                if (due is null)
                {
                    return new Error(
                        ErrorCodes.NoBreakDue,
                        "Şu an planlı molanız yok. Kısa bir ara için 'Ara ver' seçeneğini kullanın.");
                }

                if (!due.CanStartNow)
                {
                    return new Error(ErrorCodes.BreakAfterWork, due.Hint!);
                }

                breakScheduleId = due.BreakScheduleId;
                break;
            }

            case WorkSessionPauseType.WaitingForBundle when await HasWorkInProgressAsync(session, cancellationToken):
                return new Error(
                    ErrorCodes.WorkInProgress, "Elinizde süren bir iş var; demet bekleme bildirilemez.", ErrorType.Conflict);
        }

        await interruptions.BeginAsync(
            session, request.Type, request.ReasonId, PauseNote(request.Note, approvedBy), breakScheduleId,
            cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return await ToTabletDtoAsync(session, duplicate: false, cancellationToken);
    }

    public async Task<Result<TabletSessionDto>> ResumeAsync(ResumeSessionRequest request, CancellationToken cancellationToken)
    {
        var resolved = await stations.ResolveAsync(cancellationToken);
        if (!resolved.IsSuccess)
        {
            return resolved.Error!;
        }

        var station = resolved.Value;

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (!await appLock.TryAcquireAsync(MachineLock(station.MachineId), LockTimeout, cancellationToken))
        {
            return Busy;
        }

        var session = await OpenSessionOnAsync(station.MachineId, cancellationToken);
        if (session is null)
        {
            return NoSession;
        }

        if (session.Status == WorkSessionStatus.MachineBreakdown)
        {
            return new Error(
                ErrorCodes.SessionInterrupted, "Makine arızalı. Arıza giderilince 'Arıza giderildi' ile bildirin.");
        }

        if (!session.IsInterrupted)
        {
            return await ToTabletDtoAsync(session, duplicate: true, cancellationToken);
        }

        // Aradan dönüş de ara gibi kart ister; mola ve demet beklemeden dönüş serbesttir.
        if (session.Status == WorkSessionStatus.Paused)
        {
            var actor = await floorAuthorization.AuthorizeAsync(
                FloorAction.Pause, session.OperatorId, request.AuthorizerUid, cancellationToken);
            if (!actor.IsSuccess)
            {
                return actor.Error!;
            }
        }

        await interruptions.EndAsync(session, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return await ToTabletDtoAsync(session, duplicate: false, cancellationToken);
    }

    public async Task<PagedResult<WorkSessionDto>> GetListAsync(
        WorkSessionListQuery query, CancellationToken cancellationToken)
    {
        // Geçmiş oturumlar, sonradan silinen operatör / makine / tablet için de eksiksiz listelenir.
        var sessions = db.WorkSessions.AsNoTracking().IgnoreQueryFilters();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            sessions = sessions.Where(s =>
                s.Operator.OperatorCode.Contains(search)
                || (s.Operator.FirstName + " " + s.Operator.LastName).Contains(search)
                || s.Machine.MachineCode.Contains(search));
        }

        if (query.IsOpen is { } isOpen)
        {
            sessions = sessions.Where(s => (s.LogoutAt == null) == isOpen);
        }

        if (query.OperatorId is { } operatorId)
        {
            sessions = sessions.Where(s => s.OperatorId == operatorId);
        }

        if (query.MachineId is { } machineId)
        {
            sessions = sessions.Where(s => s.MachineId == machineId);
        }

        if (query.From is { } from)
        {
            sessions = sessions.Where(s => s.LoginAt >= from);
        }

        if (query.To is { } to)
        {
            sessions = sessions.Where(s => s.LoginAt < to);
        }

        var page = await ProjectRows(sessions.OrderByDescending(s => s.Id)).ToPagedResultAsync(query, cancellationToken);

        var now = UtcNow;
        return new PagedResult<WorkSessionDto>(
            page.Items.Select(r => ToDto(r, now)).ToList(), page.Page, page.PageSize, page.TotalCount);
    }

    public async Task<Result<IReadOnlyList<WorkSessionPauseDto>>> GetPausesAsync(
        long id, CancellationToken cancellationToken)
    {
        if (!await db.WorkSessions.AnyAsync(s => s.Id == id, cancellationToken))
        {
            return SessionNotFound;
        }

        var pauses = await db.WorkSessionPauses
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(p => p.WorkSessionId == id)
            .OrderBy(p => p.Id)
            .Select(p => new
            {
                p.Id, p.Type, ReasonName = p.StopReason!.Name, BreakName = p.BreakSchedule!.Name, p.Note, p.StartedAt,
                p.EndedAt,
            })
            .ToListAsync(cancellationToken);

        var now = UtcNow;
        return Result<IReadOnlyList<WorkSessionPauseDto>>.Success(pauses
            .Select(p => new WorkSessionPauseDto(
                p.Id, p.Type, p.ReasonName, p.BreakName, p.Note, p.StartedAt, p.EndedAt,
                Math.Round(((p.EndedAt ?? now) - p.StartedAt).TotalMinutes, 1)))
            .ToList());
    }

    public async Task<Result<WorkSessionDto>> CloseAsync(long id, bool releaseWork, CancellationToken cancellationToken)
    {
        if (releaseWork)
        {
            // Süren iş kendi işleminde bırakılır (demet ve makine kilitleri orada alınır); sonra oturum kapatılır.
            var running = await db.WorkSessions
                .Where(s => s.Id == id && s.LogoutAt == null)
                .SelectMany(s => db.BundleOperations.Where(o =>
                    o.MachineId == s.MachineId && o.OperatorId == s.OperatorId && o.Status == BundleOperationStatus.InProgress))
                .Select(o => (long?)o.Id)
                .FirstOrDefaultAsync(cancellationToken);
            if (running is { } operationId && await production.ReleaseAsync(operationId, cancellationToken) is { IsSuccess: false } released)
            {
                return released.Error!;
            }
        }

        var machineId = await db.WorkSessions
            .Where(s => s.Id == id)
            .Select(s => (int?)s.MachineId)
            .SingleOrDefaultAsync(cancellationToken);
        if (machineId is null)
        {
            return SessionNotFound;
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (!await appLock.TryAcquireAsync(MachineLock(machineId.Value), LockTimeout, cancellationToken))
        {
            return Busy;
        }

        var session = await db.WorkSessions.SingleAsync(s => s.Id == id, cancellationToken);
        if (!session.IsOpen)
        {
            return new Error(ErrorCodes.SessionNotActive, "Oturum zaten kapatılmış.");
        }

        if (await HasWorkInProgressAsync(session, cancellationToken))
        {
            return WorkInProgress;
        }

        await CloseAsync(session, WorkSessionEndReason.ClosedBySupervisor, UtcNow, currentUser.UserName, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var row = await ProjectRows(db.WorkSessions.AsNoTracking().IgnoreQueryFilters().Where(s => s.Id == id))
            .SingleAsync(cancellationToken);
        return ToDto(row, UtcNow);
    }

    public async Task<int> CloseExpiredAsync(CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var now = UtcNow;

        var shifts = await db.Shifts.AsNoTracking().Where(s => s.IsActive).OrderBy(s => s.StartTime).ToListAsync(cancellationToken);
        var candidates = await db.WorkSessions
            .AsNoTracking()
            .Where(s => s.LogoutAt == null)
            .OrderBy(s => s.Id)
            .Take(AutoCloseBatch)
            .Select(s => new { s.Id, s.MachineId, s.LoginAt })
            .ToListAsync(cancellationToken);

        var closed = 0;
        foreach (var candidate in candidates.Where(c => ExpiresAt(c.LoginAt, shifts, settings) <= now))
        {
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            if (!await appLock.TryAcquireAsync(MachineLock(candidate.MachineId), LockTimeout, cancellationToken))
            {
                continue;
            }

            var session = await db.WorkSessions.SingleAsync(s => s.Id == candidate.Id, cancellationToken);

            // Süren işi olan oturum kapatılmaz: adet bildirilince bir sonraki turda kapanır.
            if (!session.IsOpen || await HasWorkInProgressAsync(session, cancellationToken))
            {
                continue;
            }

            // Unutulan oturum, kapatıldığı ana değil vardiya sonuna (sonrasında çalışıldıysa son üretim hareketine)
            // damgalanır: bekleme süresi çalışılmış sayılıp günlük verimi ve primi düşürmez. Molada / arada / arızada
            // kalmış oturumda süren duraklama şu an biter; o oturum şu ana damgalanır.
            var stamp = now;
            if (session.Status is WorkSessionStatus.Active or WorkSessionStatus.WaitingForBundle
                && ShiftEndOf(session.LoginAt, shifts) is { } shiftEnd)
            {
                var lastWork = await db.BundleOperationEvents
                    .Where(e => e.WorkSessionId == session.Id)
                    .MaxAsync(e => (DateTime?)e.OccurredAt, cancellationToken);
                var earliest = lastWork is { } worked && worked > shiftEnd ? worked : shiftEnd;
                stamp = earliest < now ? earliest : now;
            }

            await CloseAsync(session, WorkSessionEndReason.ShiftEnded, stamp, SystemActor, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            closed++;
        }

        return closed;
    }

    /// <summary>
    /// Oturumun kendiliğinden kapanacağı an: giriş saatinin denk geldiği vardiyanın bitişi + bekleme süresi;
    /// hiçbir vardiyaya denk gelmiyorsa girişten itibaren en uzun oturum süresi.
    /// </summary>
    private DateTime ExpiresAt(DateTime loginAt, IReadOnlyList<Shift> shifts, ProductionOptions settings)
    {
        var loginTime = TimeOnly.FromDateTime(clock.ToLocal(loginAt));
        var shift = shifts.FirstOrDefault(s => s.Contains(loginTime));

        return shift is null
            ? loginAt.AddHours(settings.MaxSessionHours)
            : loginAt.AddMinutes(shift.LengthMinutes - shift.OffsetOf(loginTime) + settings.AutoCloseAfterShiftMinutes);
    }

    /// <summary>Giriş saatinin denk geldiği vardiyanın bitiş anı; hiçbir vardiyaya denk gelmiyorsa boş.</summary>
    private DateTime? ShiftEndOf(DateTime loginAt, IReadOnlyList<Shift> shifts)
    {
        var loginTime = TimeOnly.FromDateTime(clock.ToLocal(loginAt));
        var shift = shifts.FirstOrDefault(s => s.Contains(loginTime));
        return shift is null ? null : loginAt.AddMinutes(shift.LengthMinutes - shift.OffsetOf(loginTime));
    }

    private static Error WorkInProgress => new(
        ErrorCodes.WorkInProgress,
        "Operatörün süren bir işi var; önce o işin adedi bildirilmeli ya da iş bırakılarak oturum kapatılmalı.",
        ErrorType.Conflict);

    private static Error SessionNotFound => new(ErrorCodes.NotFound, "Çalışma oturumu bulunamadı.", ErrorType.NotFound);

    private Task<WorkSession?> OpenSessionOnAsync(int machineId, CancellationToken cancellationToken) =>
        db.WorkSessions.SingleOrDefaultAsync(s => s.MachineId == machineId && s.LogoutAt == null, cancellationToken);

    /// <summary>Oturumu kapatır; süren duraklaması varsa o da sonlanır.</summary>
    private async Task CloseAsync(
        WorkSession session, WorkSessionEndReason reason, DateTime now, string? actor, CancellationToken cancellationToken)
    {
        await interruptions.EndAsync(session, cancellationToken);
        session.Close(reason, now, actor);
    }

    /// <summary>Oturumun operatörü, oturumun makinesinde bir operasyonu sürdürüyor mu?</summary>
    private Task<bool> HasWorkInProgressAsync(WorkSession session, CancellationToken cancellationToken) =>
        db.BundleOperations.AnyAsync(
            o => o.MachineId == session.MachineId
                && o.OperatorId == session.OperatorId
                && o.Status == BundleOperationStatus.InProgress,
            cancellationToken);

    /// <summary>Arayı başkası onayladıysa duraklama notuna kimin onayladığı eklenir.</summary>
    private static string? PauseNote(string? note, string? approvedBy)
    {
        if (approvedBy is null)
        {
            return note;
        }

        var approval = $"Onaylayan: {approvedBy}";
        var combined = string.IsNullOrWhiteSpace(note) ? approval : $"{note.Trim()} · {approval}";
        return combined.Length <= 200 ? combined : combined[..200];
    }

    private async Task<TabletSessionDto> ToTabletDtoAsync(
        WorkSession session, bool duplicate, CancellationToken cancellationToken)
    {
        var machineOperator = await db.Operators
            .AsNoTracking()
            .IgnoreQueryFilters()
            .SingleAsync(o => o.Id == session.OperatorId, cancellationToken);

        var pause = session.IsInterrupted
            ? await db.WorkSessionPauses
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Where(p => p.WorkSessionId == session.Id && p.EndedAt == null)
                .Select(p => new TabletPauseDto(p.Type, p.StopReason!.Name, p.BreakSchedule!.Name, p.StartedAt))
                .SingleOrDefaultAsync(cancellationToken)
            : null;

        // Moladaki operatöre yeni mola hatırlatılmaz.
        var dueBreak = session.Status == WorkSessionStatus.Break
            ? null
            : await interruptions.FindDueBreakAsync(session, cancellationToken);

        var roleSetting = await roleSettings.GetAsync(machineOperator.Role, cancellationToken);
        return new TabletSessionDto(
            duplicate, session.Id, session.OperatorId, machineOperator.OperatorCode,
            machineOperator.FirstName + " " + machineOperator.LastName, session.LoginAt, session.Status, pause, dueBreak,
            machineOperator.Role, machineOperator.Allows(FloorAction.Pause, roleSetting),
            machineOperator.Allows(FloorAction.ReportBreakdown, roleSetting));
    }

    // Giriş, çıkış ve durum ayrıca taşınır: liste ekranı bu alanlarla süzer ve sıralar (GridQuery).
    private sealed record Row(
        WorkSession Session, string OperatorCode, string OperatorName, string MachineCode, string TabletCode,
        DateTime LoginAt, DateTime? LogoutAt, WorkSessionStatus Status);

    private static IQueryable<Row> ProjectRows(IQueryable<WorkSession> sessions) =>
        sessions.Select(s => new Row(
            s, s.Operator.OperatorCode, s.Operator.FirstName + " " + s.Operator.LastName, s.Machine.MachineCode,
            s.Tablet.TabletCode, s.LoginAt, s.LogoutAt, s.Status));

    private static WorkSessionDto ToDto(Row row, DateTime now)
    {
        var s = row.Session;
        return new(
            s.Id,
            s.OperatorId,
            row.OperatorCode,
            row.OperatorName,
            s.MachineId,
            row.MachineCode,
            row.TabletCode,
            s.LoginAt,
            s.LogoutAt,
            Math.Round(((s.LogoutAt ?? now) - s.LoginAt).TotalMinutes, 1),
            s.Status,
            s.EndReason,
            s.ClosedBy,
            s.IsOpen);
    }
}
