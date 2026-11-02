using ERP.Konfeksiyon.Application.Abstractions;
using System.Linq.Expressions;
using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Core.Application.Features.MasterData;
using ERP.Konfeksiyon.Application.Features.MasterData;
using ERP.Konfeksiyon.Application.Features.ProductionLines;
using ERP.Konfeksiyon.Application.Features.Shifts;
using ERP.Konfeksiyon.Application.Features.WorkSessions;
using ERP.Konfeksiyon.Domain.Entities;
using ERP.Konfeksiyon.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ERP.Konfeksiyon.Application.Features.Tablets;

/// <summary>Tablet tanımları (ana veri akışı): kod, bağlı makine, aktiflik.</summary>
public sealed class TabletService(IKonfeksiyonDbContext db, TimeProvider timeProvider, IOptions<TabletOptions> options)
    : KonfeksiyonMasterDataService<Tablet, TabletDto, TabletRequest>(db)
{
    protected override DbSet<Tablet> Set => Db.Tablets;
    protected override string Label => "Tablet";
    protected override string CodeField => "tabletCode";

    protected override string CodeOf(TabletRequest request) => request.TabletCode;
    protected override Expression<Func<Tablet, bool>> WithCode(string code) => e => e.TabletCode == code;

    protected override Expression<Func<Tablet, bool>> Matches(string term) =>
        e => e.TabletCode.Contains(term) || e.Name!.Contains(term) || e.Machine!.MachineCode.Contains(term);

    protected override IQueryable<Tablet> Order(IQueryable<Tablet> query) => query.OrderBy(e => e.TabletCode);

    protected override Expression<Func<Tablet, TabletDto>> ToDto
    {
        get
        {
            var onlineSince = timeProvider.GetUtcNow().UtcDateTime.AddSeconds(-options.Value.OnlineThresholdSeconds);

            return e => new TabletDto(
                e.Id,
                e.TabletCode,
                e.Name,
                e.MachineId,
                e.Machine!.MachineCode,
                e.Machine!.MachineName,
                Db.LineMachineAssignments
                    .Where(a => a.MachineId == e.MachineId && a.UnassignedAt == null)
                    .Select(a => a.ProductionLine.LineCode)
                    .FirstOrDefault(),
                e.DeviceIdentifier,
                e.DeviceKeyHash != null,
                e.IsActive && e.LastSeenAt != null && e.LastSeenAt >= onlineSince,
                e.LastSeenAt,
                e.IsActive);
        }
    }

    protected override Expression<Func<Tablet, LookupDto>> ToLookup =>
        e => new LookupDto(e.Id, e.TabletCode, e.Name ?? e.TabletCode);

    protected override void Apply(TabletRequest request, Tablet entity, string code)
    {
        entity.TabletCode = code;
        entity.Name = MasterDataRules.NullIfBlank(request.Name);
        entity.MachineId = request.MachineId;
        entity.IsActive = request.IsActive;
    }

    protected override async Task<Error?> ValidateAsync(
        TabletRequest request, Tablet? existing, CancellationToken cancellationToken)
    {
        var referenceError = await RequireSelectableAsync(
            Db.Machines, request.MachineId, existing?.MachineId, "machineId", "Makine", cancellationToken);
        if (referenceError is not null || request.MachineId is null)
        {
            return referenceError;
        }

        var existingId = existing?.Id ?? 0;
        var otherTablet = await Db.Tablets
            .Where(t => t.MachineId == request.MachineId && t.Id != existingId)
            .Select(t => t.TabletCode)
            .FirstOrDefaultAsync(cancellationToken);
        if (otherTablet is null)
        {
            return null;
        }

        var message = $"Bu makineye zaten bir tablet bağlı ({otherTablet}).";
        return new Error(
            ErrorCodes.MachineAlreadyHasTablet,
            message,
            ErrorType.Conflict,
            new Dictionary<string, string[]> { ["machineId"] = [message] });
    }
}

public interface ITabletDeviceService
{
    /// <summary>Cihaz bağını koparır; tabletin yeniden eşleştirilmesi gerekir.</summary>
    Task<Result> ResetDeviceAsync(int tabletId, CancellationToken cancellationToken);

    /// <summary>Tabletin ayarlar ekranında seçilebilecek makineler; parola gerekir.</summary>
    Task<Result<IReadOnlyList<TabletPairingStationDto>>> GetPairingStationsAsync(TabletPairingListRequest request, CancellationToken cancellationToken);

    /// <summary>Cihazı seçilen makineye bağlar; makinenin tablet kaydı yoksa oluşturur. Cihazın önceki bağı kopar.</summary>
    Task<Result<PairTabletResponse>> PairAsync(PairTabletRequest request, CancellationToken cancellationToken);
    Task<Result<TabletTokenResponse>> IssueTokenAsync(TabletTokenRequest request, CancellationToken cancellationToken);

    /// <summary>İsteği yapan tabletin makine / operasyon / bant / plan bilgisi.</summary>
    Task<Result<TabletContextDto>> GetContextAsync(CancellationToken cancellationToken);

    /// <summary>Tabletin çevrimiçi olduğunu kaydeder.</summary>
    Task<Result> HeartbeatAsync(CancellationToken cancellationToken);
}

public sealed class TabletDeviceService(
    IKonfeksiyonDbContext db,
    ITokenService tokenService,
    ILinePlanService planService,
    IStopReasonLookup stopReasons,
    IStaffRoleSettingService roleSettings,
    IOptions<ProductionOptions> productionOptions,
    IOptions<TabletPairingOptions> pairingOptions,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : ITabletDeviceService
{
    private static readonly Error NotFound = new(ErrorCodes.NotFound, "Tablet bulunamadı.", ErrorType.NotFound);

    private static readonly Error InvalidDeviceCredentials = new(
        ErrorCodes.InvalidDeviceCredentials, "Cihaz kimlik bilgileri geçersiz. Tabletin yeniden eşleştirilmesi gerekebilir.",
        ErrorType.Unauthorized);

    private DateTime UtcNow => timeProvider.GetUtcNow().UtcDateTime;

    public async Task<Result> ResetDeviceAsync(int tabletId, CancellationToken cancellationToken)
    {
        var tablet = await db.Tablets.SingleOrDefaultAsync(t => t.Id == tabletId, cancellationToken);
        if (tablet is null)
        {
            return NotFound;
        }

        tablet.ResetDevice();
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<TabletPairingStationDto>>> GetPairingStationsAsync(
        TabletPairingListRequest request, CancellationToken cancellationToken)
    {
        if (!TabletPairing.PasswordMatches(pairingOptions.Value, request.Password))
        {
            return TabletPairing.InvalidPassword;
        }

        var deviceIdentifier = request.DeviceIdentifier.Trim();
        var stations = await db.Machines
            .AsNoTracking()
            .Where(m => m.IsActive)
            .OrderBy(m => m.MachineCode)
            .Select(m => new
            {
                m.Id,
                m.MachineCode,
                m.MachineName,
                OperationName = m.Operation == null ? null : m.Operation.Name,
                Tablet = db.Tablets
                    .Where(t => t.MachineId == m.Id)
                    .Select(t => new { t.TabletCode, t.DeviceKeyHash, t.DeviceIdentifier })
                    .FirstOrDefault(),
            })
            .ToListAsync(cancellationToken);

        return stations
            .Select(s => new TabletPairingStationDto(
                s.Id,
                null,
                s.MachineCode,
                s.MachineName,
                s.OperationName,
                s.Tablet?.TabletCode,
                s.Tablet?.DeviceKeyHash is not null,
                s.Tablet?.DeviceKeyHash is not null && s.Tablet.DeviceIdentifier == deviceIdentifier))
            .ToList();
    }

    public async Task<Result<PairTabletResponse>> PairAsync(PairTabletRequest request, CancellationToken cancellationToken)
    {
        if (!TabletPairing.PasswordMatches(pairingOptions.Value, request.Password))
        {
            return TabletPairing.InvalidPassword;
        }

        var machine = await db.Machines
            .AsNoTracking()
            .SingleOrDefaultAsync(m => m.Id == request.MachineId && m.IsActive, cancellationToken);
        if (machine is null)
        {
            return TabletPairing.StationNotFound;
        }

        var deviceIdentifier = request.DeviceIdentifier.Trim();
        var tablet = await db.Tablets.SingleOrDefaultAsync(t => t.MachineId == machine.Id, cancellationToken);
        if (tablet is { IsActive: false })
        {
            return TabletPairing.TabletInactive;
        }

        if (tablet is { IsEnrolled: true } && tablet.DeviceIdentifier != deviceIdentifier && !request.TakeOver)
        {
            return TabletPairing.AlreadyPaired(machine.MachineCode);
        }

        // Cihaz başka bir makineden gelebilir. Bağlar önce kopar: cihaz kimliği iki kayıtta aynı anda duramaz (benzersiz index).
        var previous = await db.Tablets.Where(t => t.DeviceIdentifier == deviceIdentifier).ToListAsync(cancellationToken);
        previous.ForEach(t => t.ResetDevice());
        tablet?.ResetDevice();
        await db.SaveChangesAsync(cancellationToken);

        if (tablet is null)
        {
            var code = TabletPairing.NewTabletCode("TBL", machine.MachineCode, machine.Id);
            tablet = new Tablet { TabletCode = code, MachineId = machine.Id };
            db.Tablets.Add(tablet);
        }

        var deviceKey = Secrets.NewDeviceKey();
        tablet.Enroll(deviceIdentifier, Secrets.Hash(deviceKey));
        tablet.LastSeenAt = UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return new PairTabletResponse(tablet.Id, tablet.TabletCode, deviceKey, machine.MachineCode, machine.MachineName);
    }

    public async Task<Result<TabletTokenResponse>> IssueTokenAsync(
        TabletTokenRequest request, CancellationToken cancellationToken)
    {
        var tabletCode = MasterDataRules.NormalizeCode(request.TabletCode);
        var tablet = await db.Tablets
            .AsNoTracking()
            .SingleOrDefaultAsync(t => t.TabletCode == tabletCode, cancellationToken);

        // Anahtar her durumda karşılaştırılır: tabletin var olup olmadığı yanıt süresinden anlaşılmasın.
        var keyMatches = Secrets.Matches(tablet?.DeviceKeyHash ?? Secrets.Hash(string.Empty), request.DeviceKey);

        if (tablet is null
            || !tablet.IsActive
            || !keyMatches
            || tablet.DeviceIdentifier != request.DeviceIdentifier.Trim())
        {
            return InvalidDeviceCredentials;
        }

        await TouchAsync(tablet.Id, cancellationToken);
        var token = tokenService.CreateTabletAccessToken(tablet.Id, tablet.TabletCode, "Konfeksiyon");
        return new TabletTokenResponse(token.Token, token.ExpiresAt);
    }

    public async Task<Result<TabletContextDto>> GetContextAsync(CancellationToken cancellationToken)
    {
        var row = await db.Tablets
            .AsNoTracking()
            .Where(t => t.Id == currentUser.TabletId && t.IsActive)
            .Select(t => new
            {
                t.Id,
                t.TabletCode,
                Machine = t.Machine == null
                    ? null
                    : new TabletMachineDto(
                        t.Machine.Id, t.Machine.MachineCode, t.Machine.MachineName, t.Machine.MachineType.Name,
                        t.Machine.Status,
                        db.MachineStatusLogs
                            .Where(l => l.MachineId == t.MachineId && l.EndedAt == null)
                            .Select(l => l.Reason)
                            .FirstOrDefault()),
                Operation = t.Machine!.Operation == null
                    ? null
                    : new TabletOperationDto(
                        t.Machine.Operation.Id, t.Machine.Operation.OperationCode, t.Machine.Operation.Name,
                        t.Machine.Operation.Sam),
                Line = db.LineMachineAssignments
                    .Where(a => a.MachineId == t.MachineId && a.UnassignedAt == null)
                    .Select(a => new TabletLineDto(a.ProductionLine.Id, a.ProductionLine.LineCode, a.ProductionLine.Name))
                    .FirstOrDefault(),
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            // Tablet silinmiş ya da pasif yapılmış: token süresi dolmasa da erişim hemen kesilir.
            return InvalidDeviceCredentials;
        }

        TabletPlanDto? currentPlan = null;
        if (row.Line is not null)
        {
            var (current, _) = await planService.GetCurrentAndUpcomingAsync(row.Line.Id, 0, cancellationToken);
            currentPlan = current is null
                ? null
                : new TabletPlanDto(
                    current.ProductionOrderId, current.OrderNo, current.ModelCode, current.ProductName,
                    current.CustomerName, current.PlannedQuantity);
        }

        return new TabletContextDto(
            row.Id, row.TabletCode, UtcNow, row.Machine, row.Operation, row.Line, currentPlan,
            await stopReasons.GetOptionsAsync(StopReasonCategory.Pause, cancellationToken),
            await stopReasons.GetOptionsAsync(StopReasonCategory.Breakdown, cancellationToken),
            (await roleSettings.GetAllAsync(cancellationToken)).Select(StaffRoleSettingService.ToDto).ToList(),
            Math.Max(productionOptions.Value.IdlePromptMinutes, 0));
    }

    public async Task<Result> HeartbeatAsync(CancellationToken cancellationToken)
    {
        if (currentUser.TabletId is not { } tabletId || await TouchAsync(tabletId, cancellationToken) == 0)
        {
            return InvalidDeviceCredentials;
        }

        return Result.Success();
    }

    /// <summary>Son görülme zamanını tek UPDATE ile yazar (sık çağrılır; audit ve sürüm kontrolü gerekmez).</summary>
    private Task<int> TouchAsync(int tabletId, CancellationToken cancellationToken)
    {
        var now = UtcNow;
        return db.Tablets
            .Where(t => t.Id == tabletId && t.IsActive)
            .ExecuteUpdateAsync(update => update.SetProperty(t => t.LastSeenAt, now), cancellationToken);
    }
}
