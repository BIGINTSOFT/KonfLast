using ERP.Konfeksiyon.Application.Abstractions;
using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Core.Application.Features.MasterData;
using ERP.Konfeksiyon.Application.Features.Shifts;
using ERP.Konfeksiyon.Application.Features.WorkSessions;
using ERP.Konfeksiyon.Domain.Entities;
using ERP.Konfeksiyon.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ERP.Konfeksiyon.Application.Features.Tablets;

/// <param name="DurationMinutes">Durumun sürdüğü süre; durum hâlâ sürüyorsa şu ana kadar geçen süre.</param>
public sealed record MachineStatusLogDto(
    long Id, MachineStatus Status, DateTime StartedAt, DateTime? EndedAt, double DurationMinutes, string? Reason,
    string ChangedBy, bool ReportedFromTablet);

/// <param name="StatusReason">Şu anki durum için kaydedilmiş neden (ör. arıza nedeni); yoksa boş.</param>
/// <param name="AllowedTargets">Kullanıcının makineyi şu an geçirebileceği durumlar.</param>
public sealed record MachineStatusDto(
    int MachineId,
    string MachineCode,
    string MachineName,
    MachineStatus Status,
    string? StatusReason,
    IReadOnlyList<MachineStatus> AllowedTargets,
    IReadOnlyList<MachineStatusLogDto> History);

public sealed record ChangeMachineStatusRequest(MachineStatus Status, string? Reason);

public sealed class ChangeMachineStatusRequestValidator : AbstractValidator<ChangeMachineStatusRequest>
{
    public ChangeMachineStatusRequestValidator()
    {
        RuleFor(x => x.Status).IsInEnum().WithMessage("Geçersiz durum.");
        RuleFor(x => x.Reason).MaximumLength(300).WithMessage("Açıklama en fazla 300 karakter olabilir.");
    }
}

public interface IMachineStatusService
{
    Task<Result<MachineStatusDto>> GetAsync(int machineId, CancellationToken cancellationToken);

    /// <summary>Yetkili kullanıcının makine durumunu değiştirmesi (arıza, bakım, müsait, çevrimdışı).</summary>
    Task<Result<MachineStatusDto>> ChangeAsync(
        int machineId, ChangeMachineStatusRequest request, CancellationToken cancellationToken);

    /// <summary>İsteği yapan tabletin bağlı olduğu makine için arıza bildirimi.</summary>
    Task<Result<MachineStatusDto>> ReportBreakdownFromTabletAsync(
        ReportBreakdownRequest request, CancellationToken cancellationToken);

    /// <summary>İsteği yapan tabletin bağlı olduğu makinenin arızadan çıktığının bildirimi.</summary>
    Task<Result<MachineStatusDto>> ReportRecoveryFromTabletAsync(
        ReportRecoveryRequest request, CancellationToken cancellationToken);
}

public sealed class MachineStatusService(
    IKonfeksiyonDbContext db,
    IAppLock appLock,
    ICurrentUser currentUser,
    ISessionInterruptions interruptions,
    IStopReasonLookup stopReasons,
    IFloorAuthorization floorAuthorization,
    ICurrentPermissions permissions,
    TimeProvider timeProvider) : IMachineStatusService
{
    private const int HistoryCount = 20;
    private const string SystemUser = "system";
    private static readonly TimeSpan LockTimeout = TimeSpan.FromSeconds(10);

    private static readonly Error NotFound = new(ErrorCodes.NotFound, "Makine bulunamadı.", ErrorType.NotFound);

    private static readonly IReadOnlyDictionary<MachineStatus, string> Labels = new Dictionary<MachineStatus, string>
    {
        [MachineStatus.Available] = "Müsait",
        [MachineStatus.Running] = "Çalışıyor",
        [MachineStatus.Idle] = "Boşta",
        [MachineStatus.Breakdown] = "Arıza",
        [MachineStatus.Maintenance] = "Bakımda",
        [MachineStatus.Offline] = "Çevrimdışı",
    };

    private DateTime UtcNow => timeProvider.GetUtcNow().UtcDateTime;

    public async Task<Result<MachineStatusDto>> GetAsync(int machineId, CancellationToken cancellationToken)
    {
        var machine = await db.Machines.AsNoTracking().SingleOrDefaultAsync(m => m.Id == machineId, cancellationToken);
        if (machine is null)
        {
            return NotFound;
        }

        var now = UtcNow;
        var logs = await db.MachineStatusLogs
            .AsNoTracking()
            .Where(l => l.MachineId == machineId)
            .OrderByDescending(l => l.Id)
            .Take(HistoryCount)
            .ToListAsync(cancellationToken);

        return new MachineStatusDto(
            machine.Id,
            machine.MachineCode,
            machine.MachineName,
            machine.Status,
            logs.FirstOrDefault(l => l.EndedAt is null)?.Reason,
            machine.ManualStatusTargets,
            logs.Select(l => new MachineStatusLogDto(
                    l.Id, l.Status, l.StartedAt, l.EndedAt,
                    Math.Round(((l.EndedAt ?? now) - l.StartedAt).TotalMinutes, 1),
                    l.Reason, l.ChangedBy, l.TabletId is not null))
                .ToList());
    }

    public async Task<Result<MachineStatusDto>> ChangeAsync(
        int machineId, ChangeMachineStatusRequest request, CancellationToken cancellationToken)
    {
        // Arızalı makineyi kullanıma açmak tabletteki kartla kapatmanın ofis karşılığıdır: ayrı yetki ister.
        var current = await db.Machines.Where(m => m.Id == machineId).Select(m => (MachineStatus?)m.Status)
            .SingleOrDefaultAsync(cancellationToken);
        if (current == MachineStatus.Breakdown && request.Status != MachineStatus.Breakdown
            && !await permissions.HasAsync(ERP.Konfeksiyon.Domain.Authorization.KonfeksiyonPermissions.Machine.ResolveBreakdown, cancellationToken))
        {
            return new Error(
                ErrorCodes.Forbidden, "Arızalı makineyi kullanıma açma yetkiniz yok (Arızalı makineyi ofisten kullanıma açma).",
                ErrorType.Forbidden);
        }

        return await ChangeCoreAsync(machineId, request.Status, request.Reason, requiredCurrent: null, cancellationToken);
    }

    public async Task<Result<MachineStatusDto>> ReportBreakdownFromTabletAsync(
        ReportBreakdownRequest request, CancellationToken cancellationToken)
    {
        var machineId = await TabletMachineIdAsync(cancellationToken);
        if (!machineId.IsSuccess)
        {
            return machineId.Error!;
        }

        // Arıza yetkiyle bildirilir: oturumu açık operatörün kendi yetkisi ya da okutulan yetkili kartı.
        var actor = await floorAuthorization.AuthorizeAsync(
            FloorAction.ReportBreakdown, await SessionOperatorIdAsync(machineId.Value, cancellationToken),
            request.AuthorizerUid, cancellationToken);
        if (!actor.IsSuccess)
        {
            return actor.Error!;
        }

        // Durum kaydında nedenin adı saklanır: neden sonradan değişse ya da silinse de geçmiş okunur kalır.
        var reason = MasterDataRules.NullIfBlank(request.Note);
        if (request.ReasonId is { } reasonId)
        {
            var selected = await stopReasons.ResolveAsync(reasonId, StopReasonCategory.Breakdown, cancellationToken);
            if (!selected.IsSuccess)
            {
                return selected.Error!;
            }

            reason = reason is null ? selected.Value.Name : $"{selected.Value.Name} — {reason}";
        }

        return await ChangeCoreAsync(
            machineId.Value, MachineStatus.Breakdown, reason, null, cancellationToken, actor.Value.DisplayName);
    }

    public async Task<Result<MachineStatusDto>> ReportRecoveryFromTabletAsync(
        ReportRecoveryRequest request, CancellationToken cancellationToken)
    {
        var machineId = await TabletMachineIdAsync(cancellationToken);
        if (!machineId.IsSuccess)
        {
            return machineId.Error!;
        }

        // Arızayı yalnızca bakım operatörü ya da bant şefi, kartını okutarak kapatır.
        var actor = await floorAuthorization.AuthorizeAsync(
            FloorAction.ResolveBreakdown, sessionOperatorId: null, request.AuthorizerUid, cancellationToken);
        if (!actor.IsSuccess)
        {
            return actor.Error!;
        }

        // Tablet yalnızca arızadan çıkışı bildirebilir; bakımdaki makineyi bakım ekibi açar.
        return await ChangeCoreAsync(
            machineId.Value, MachineStatus.Available, null, MachineStatus.Breakdown, cancellationToken,
            actor.Value.DisplayName);
    }

    /// <summary>Makinede oturumu açık operatör (yoksa boş).</summary>
    private Task<int?> SessionOperatorIdAsync(int machineId, CancellationToken cancellationToken) =>
        db.WorkSessions
            .Where(s => s.MachineId == machineId && s.LogoutAt == null)
            .Select(s => (int?)s.OperatorId)
            .SingleOrDefaultAsync(cancellationToken);

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];

    private async Task<Result<int>> TabletMachineIdAsync(CancellationToken cancellationToken)
    {
        var tablet = await db.Tablets
            .AsNoTracking()
            .Where(t => t.Id == currentUser.TabletId && t.IsActive)
            .Select(t => new { t.MachineId })
            .SingleOrDefaultAsync(cancellationToken);

        if (tablet is null)
        {
            return new Error(
                ErrorCodes.InvalidDeviceCredentials, "Cihaz kimlik bilgileri geçersiz.", ErrorType.Unauthorized);
        }

        if (tablet.MachineId is not { } machineId)
        {
            return new Error(ErrorCodes.TabletNotAssigned, "Tablet henüz bir makineye bağlanmamış.");
        }

        return machineId;
    }

    private async Task<Result<MachineStatusDto>> ChangeCoreAsync(
        int machineId,
        MachineStatus target,
        string? reason,
        MachineStatus? requiredCurrent,
        CancellationToken cancellationToken,
        string? changedBy = null)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        // Aynı makine için eşzamanlı durum değişiklikleri sıraya girer; açık durum kaydı tek kalır.
        // Arıza, makinedeki oturumu da etkiler: kilit sırası üretim akışıyla aynıdır (önce oturum, sonra durum).
        if (!await appLock.TryAcquireAsync(WorkSessionService.MachineLock(machineId), LockTimeout, cancellationToken)
            || !await appLock.TryAcquireAsync($"machine-status:{machineId}", LockTimeout, cancellationToken))
        {
            return new Error(
                ErrorCodes.ResourceBusy, "Makine durumu şu an başka bir işlemle değiştiriliyor. Lütfen tekrar deneyin.",
                ErrorType.Conflict);
        }

        var machine = await db.Machines.SingleOrDefaultAsync(m => m.Id == machineId, cancellationToken);
        if (machine is null)
        {
            return NotFound;
        }

        if ((requiredCurrent is not null && machine.Status != requiredCurrent) || !machine.CanSetStatusManually(target))
        {
            var message = machine.Status == target
                ? $"Makine zaten '{Labels[target]}' durumunda."
                : $"Makine '{Labels[machine.Status]}' durumundan '{Labels[target]}' durumuna geçirilemez.";
            return new Error(ErrorCodes.InvalidStatusTransition, message);
        }

        var now = UtcNow;
        var openLog = await db.MachineStatusLogs
            .SingleOrDefaultAsync(l => l.MachineId == machineId && l.EndedAt == null, cancellationToken);
        if (openLog is not null)
        {
            openLog.EndedAt = now;
            // Unique index (makine başına tek açık kayıt) için önce eski kayıt kapatılır.
            await db.SaveChangesAsync(cancellationToken);
        }

        var previous = machine.Status;
        machine.SetStatusManually(target);

        // Arıza operatörün çalışmasını keser; mola ya da kişisel ara sayılmaz, ayrı kaydedilir.
        if (target == MachineStatus.Breakdown)
        {
            await interruptions.OnMachineBreakdownAsync(machineId, cancellationToken);
        }
        else if (previous == MachineStatus.Breakdown)
        {
            await interruptions.OnMachineRecoveredAsync(machineId, cancellationToken);
        }

        db.MachineStatusLogs.Add(new MachineStatusLog
        {
            MachineId = machineId,
            Status = target,
            StartedAt = now,
            Reason = MasterDataRules.NullIfBlank(reason),
            // Tabletten bildirimde işlemi yapan / onaylayan personel; web'de oturumdaki kullanıcı.
            ChangedBy = Truncate(changedBy ?? currentUser.UserName ?? SystemUser, 100),
            TabletId = currentUser.TabletId,
        });

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return await GetAsync(machineId, cancellationToken);
    }
}
