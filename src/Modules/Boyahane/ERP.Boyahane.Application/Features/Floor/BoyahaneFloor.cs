using System.Linq.Expressions;
using ERP.Boyahane.Application.Features.Batches;
using ERP.Boyahane.Domain.Entities;
using ERP.Boyahane.Domain.Enums;
using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Core.Application.Features.MasterData;
using ERP.Core.Domain.Common;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using static ERP.Core.Application.Features.MasterData.MasterDataRules;

namespace ERP.Boyahane.Application.Features.Floor;

public sealed record BoyahaneOperatorDto(
    int Id, string Code, string FirstName, string LastName, string Name, BoyahaneOperatorRole Role, string? CardUid, bool CanPause, bool CanReportBreakdown, bool IsActive, int? BreakGroupId, string? BreakGroupName);

public sealed record BoyahaneOperatorRequest(
    string Code, string FirstName, string LastName, BoyahaneOperatorRole Role = BoyahaneOperatorRole.Operator, string? CardUid = null, bool CanPause = false, bool CanReportBreakdown = false, bool IsActive = true, int? BreakGroupId = null);

public sealed class BoyahaneOperatorRequestValidator : AbstractValidator<BoyahaneOperatorRequest>
{
    public BoyahaneOperatorRequestValidator()
    {
        RuleFor(x => x.Code).ValidCode();
        RuleFor(x => x.FirstName).ValidName(100);
        RuleFor(x => x.LastName).ValidName(100);
        RuleFor(x => x.Role).IsInEnum().WithMessage("Geçerli bir görev seçin.");
        RuleFor(x => x.CardUid)
            .Must(uid => RfidUid.Normalize(uid) is not null).WithMessage("Kart numarası en fazla 10 harf ya da rakam olmalıdır.")
            .When(x => !string.IsNullOrWhiteSpace(x.CardUid));
    }
}

public sealed class BoyahaneOperatorService(IAppDbContext db)
    : MasterDataService<BoyahaneOperator, BoyahaneOperatorDto, BoyahaneOperatorRequest>(db)
{
    protected override DbSet<BoyahaneOperator> Set => Db.Set<BoyahaneOperator>();
    protected override string Label => "Operatör";
    protected override string CodeField => "code";

    protected override string CodeOf(BoyahaneOperatorRequest request) => request.Code;
    protected override Expression<Func<BoyahaneOperator, bool>> WithCode(string code) => e => e.Code == code;
    protected override Expression<Func<BoyahaneOperator, bool>> Matches(string term) => e => e.Code.Contains(term) || e.Name.Contains(term) || e.CardUid!.Contains(term);
    protected override IQueryable<BoyahaneOperator> Order(IQueryable<BoyahaneOperator> query) => query.OrderBy(e => e.Code);

    protected override Expression<Func<BoyahaneOperator, BoyahaneOperatorDto>> ToDto =>
        e => new BoyahaneOperatorDto(e.Id, e.Code, e.FirstName, e.LastName, e.Name, e.Role, e.CardUid, e.CanPause, e.CanReportBreakdown, e.IsActive, e.BreakGroupId, e.BreakGroup != null ? e.BreakGroup.Name : null);

    protected override Expression<Func<BoyahaneOperator, LookupDto>> ToLookup => e => new LookupDto(e.Id, e.Code, e.Name);

    protected override void Apply(BoyahaneOperatorRequest request, BoyahaneOperator entity, string code)
    {
        entity.Code = code;
        entity.FirstName = request.FirstName.Trim();
        entity.LastName = request.LastName.Trim();
        entity.Name = $"{entity.FirstName} {entity.LastName}";
        entity.Role = request.Role;
        entity.CardUid = RfidUid.Normalize(request.CardUid);
        entity.BreakGroupId = request.BreakGroupId;
        entity.CanPause = request.CanPause;
        entity.CanReportBreakdown = request.CanReportBreakdown;
        entity.IsActive = request.IsActive;
    }

    protected override async Task<Error?> ValidateAsync(BoyahaneOperatorRequest request, BoyahaneOperator? existing, CancellationToken cancellationToken)
    {
        if (await ReferenceValidation.RequireSelectableAsync(Db.Set<BoyahaneBreakGroup>(), request.BreakGroupId, existing?.BreakGroupId, "breakGroupId", "Mola grubu", cancellationToken) is { } groupError)
        {
            return groupError;
        }

        if (RfidUid.Normalize(request.CardUid) is not { } uid)
        {
            return null;
        }

        var existingId = existing?.Id ?? 0;
        var owner = await Db.Set<BoyahaneOperator>()
            .Where(o => o.CardUid == uid && o.Id != existingId)
            .Select(o => o.Code)
            .FirstOrDefaultAsync(cancellationToken);
        var cardError = await BoyahaneCards.CheckAsync(Db, uid, BoyahaneCardType.Operator, cancellationToken);
        if (owner is null && cardError is null)
        {
            return null;
        }

        var message = owner is not null ? $"Bu kart {owner} operatörüne ait." : cardError!.Message;
        return new Error(ErrorCodes.ValidationError, message, ErrorType.Validation, new Dictionary<string, string[]> { ["cardUid"] = [message] });
    }
}

/// <param name="IsOnline">Tabletten son iki dakikada istek geldi mi.</param>
public sealed record BoyahaneTabletDto(
    int Id,
    string TabletCode,
    string? Name,
    int? MachineId,
    string? MachineCode,
    bool IsEnrolled,
    bool IsOnline,
    DateTime? LastSeenAt,
    bool IsActive);

public sealed record BoyahaneTabletRequest(string TabletCode, string? Name = null, int? MachineId = null, bool IsActive = true);

public sealed class BoyahaneTabletRequestValidator : AbstractValidator<BoyahaneTabletRequest>
{
    public BoyahaneTabletRequestValidator()
    {
        RuleFor(x => x.TabletCode).ValidCode();
        RuleFor(x => x.Name).OptionalText(150);
    }
}

public sealed class BoyahaneTabletService(IAppDbContext db, TimeProvider timeProvider)
    : MasterDataService<BoyahaneTablet, BoyahaneTabletDto, BoyahaneTabletRequest>(db)
{
    private const int OnlineSeconds = 120;

    protected override DbSet<BoyahaneTablet> Set => Db.Set<BoyahaneTablet>();
    protected override string Label => "Tablet";
    protected override string CodeField => "tabletCode";

    protected override string CodeOf(BoyahaneTabletRequest request) => request.TabletCode;
    protected override Expression<Func<BoyahaneTablet, bool>> WithCode(string code) => e => e.TabletCode == code;
    protected override Expression<Func<BoyahaneTablet, bool>> Matches(string term) => e => e.TabletCode.Contains(term) || e.Name!.Contains(term) || e.Machine!.Code.Contains(term);
    protected override IQueryable<BoyahaneTablet> Order(IQueryable<BoyahaneTablet> query) => query.OrderBy(e => e.TabletCode);

    protected override Expression<Func<BoyahaneTablet, BoyahaneTabletDto>> ToDto
    {
        get
        {
            var onlineSince = timeProvider.GetUtcNow().UtcDateTime.AddSeconds(-OnlineSeconds);
            return e => new BoyahaneTabletDto(
                e.Id, e.TabletCode, e.Name, e.MachineId, e.Machine!.Code, e.DeviceKeyHash != null,
                e.IsActive && e.LastSeenAt != null && e.LastSeenAt >= onlineSince, e.LastSeenAt, e.IsActive);
        }
    }

    protected override Expression<Func<BoyahaneTablet, LookupDto>> ToLookup => e => new LookupDto(e.Id, e.TabletCode, e.Name ?? e.TabletCode);

    protected override void Apply(BoyahaneTabletRequest request, BoyahaneTablet entity, string code)
    {
        entity.TabletCode = code;
        entity.Name = NullIfBlank(request.Name);
        entity.MachineId = request.MachineId;
        entity.IsActive = request.IsActive;
    }

    protected override async Task<Error?> ValidateAsync(BoyahaneTabletRequest request, BoyahaneTablet? existing, CancellationToken cancellationToken)
    {
        if (await ReferenceValidation.RequireSelectableAsync(Db.Set<BoyahaneMachine>(), request.MachineId, existing?.MachineId, "machineId", "Makine", cancellationToken) is { } error)
        {
            return error;
        }

        if (request.MachineId is null)
        {
            return null;
        }

        var existingId = existing?.Id ?? 0;
        var other = await Db.Set<BoyahaneTablet>()
            .Where(t => t.MachineId == request.MachineId && t.Id != existingId)
            .Select(t => t.TabletCode)
            .FirstOrDefaultAsync(cancellationToken);
        if (other is null)
        {
            return null;
        }

        var message = $"Bu makineye zaten bir tablet bağlı ({other}).";
        return new Error(ErrorCodes.MachineAlreadyHasTablet, message, ErrorType.Conflict, new Dictionary<string, string[]> { ["machineId"] = [message] });
    }
}

public sealed record BoyahaneTabletTokenRequest(string TabletCode, string DeviceKey, string DeviceIdentifier);

public sealed class BoyahaneTabletTokenRequestValidator : AbstractValidator<BoyahaneTabletTokenRequest>
{
    public BoyahaneTabletTokenRequestValidator()
    {
        RuleFor(x => x.TabletCode).NotEmpty().WithMessage("Tablet kodu zorunludur.").MaximumLength(30);
        RuleFor(x => x.DeviceKey).NotEmpty().WithMessage("Cihaz anahtarı zorunludur.");
        RuleFor(x => x.DeviceIdentifier).NotEmpty().WithMessage("Cihaz kimliği zorunludur.");
    }
}

public sealed record BoyahaneTabletTokenResponse(string AccessToken, DateTime AccessTokenExpiresAt);

public sealed record BoyahaneTabletMachineDto(int Id, string Code, string Name);

public sealed record BoyahaneTabletOperatorDto(int Id, string Code, string Name, BoyahaneOperatorRole Role, DateTime LoginAt);

public sealed record BoyahaneTabletChemicalDto(int Id, string Code, string Name, DyeChemicalKind Kind);

public sealed record BoyahaneTabletPauseDto(BoyahanePauseKind Kind, string? ReasonName, string? BreakName, DateTime StartedAt);

public sealed record BoyahaneTabletReasonDto(int Id, string Name);

/// <param name="Machine">Boşsa tablet kontrol noktasıdır: yalnızca makinesiz adımları yürütür.</param>
/// <param name="CurrentBatchId">Bu tablette başlatılmış ve bitmemiş adımın partisi.</param>
/// <param name="Pause">Oturumdaki açık mola, ara ya da arıza.</param>
public sealed record BoyahaneTabletContextDto(
    int TabletId,
    string TabletCode,
    DateTime ServerTime,
    BoyahaneTabletMachineDto? Machine,
    DyeMachineStatus? MachineStatus,
    string? MachineStatusReason,
    BoyahaneTabletOperatorDto? Operator,
    BoyahaneTabletPauseDto? Pause,
    int? CurrentBatchId,
    IReadOnlyList<BoyahaneTabletChemicalDto> Chemicals,
    IReadOnlyList<BoyahaneTabletReasonDto> PauseReasons,
    IReadOnlyList<BoyahaneTabletReasonDto> BreakdownReasons,
    BoyahaneDueBreakDto? DueBreak,
    int IdlePromptMinutes);

/// <param name="Kind">Mola, ara ya da parti bekleme; arıza ayrı bildirilir.</param>
/// <param name="ReasonId">Arada zorunludur.</param>
/// <param name="AuthorizerUid">Ara için okutulan kart: kişiye özel yetkili oturum sahibi ya da vardiya amiri.</param>
public sealed record BoyahaneTabletPauseRequest(BoyahanePauseKind Kind, int? ReasonId = null, string? Note = null, string? AuthorizerUid = null);

public sealed class BoyahaneTabletPauseRequestValidator : AbstractValidator<BoyahaneTabletPauseRequest>
{
    public BoyahaneTabletPauseRequestValidator()
    {
        RuleFor(x => x.Kind).Must(k => k is BoyahanePauseKind.Break or BoyahanePauseKind.Pause or BoyahanePauseKind.WaitingForBatch).WithMessage("Mola, ara ya da parti bekleme seçilmelidir.");
        RuleFor(x => x.ReasonId).NotNull().WithMessage("Ara nedeni seçilmelidir.").When(x => x.Kind == BoyahanePauseKind.Pause);
        RuleFor(x => x.Note).OptionalText(500);
    }
}

/// <param name="ReasonId">Arıza nedeni (Duruş Nedenleri ana verisinden).</param>
/// <param name="AuthorizerUid">Arıza için okutulan kart: kişiye özel yetkili oturum sahibi, vardiya amiri ya da bakım.</param>
/// <param name="AuthorizerUid">Aradan dönüşte okutulan kart; moladan dönüşte gerekmez.</param>
public sealed record BoyahaneTabletResumeRequest(string? AuthorizerUid = null);

public sealed record BoyahaneTabletBreakdownRequest(int? ReasonId = null, string? Note = null, string? AuthorizerUid = null);

public sealed class BoyahaneTabletBreakdownRequestValidator : AbstractValidator<BoyahaneTabletBreakdownRequest>
{
    public BoyahaneTabletBreakdownRequestValidator()
    {
        RuleFor(x => x.ReasonId).NotNull().WithMessage("Arıza nedeni seçilmelidir.");
        RuleFor(x => x.Note).OptionalText(400);
    }
}

/// <param name="AuthorizerUid">Vardiya amirinin ya da bakımcının okutulan kartı.</param>
public sealed record BoyahaneTabletRecoveryRequest(string AuthorizerUid);

public sealed record BoyahaneTabletLoginRequest(string Uid);

public sealed record BoyahaneTabletReadRequest(string Code);

public sealed record BoyahaneTabletStartRequest(Guid RequestId);

public sealed class BoyahaneTabletStartRequestValidator : AbstractValidator<BoyahaneTabletStartRequest>
{
    public BoyahaneTabletStartRequestValidator() => RuleFor(x => x.RequestId).NotEmpty().WithMessage("İşlem kimliği zorunludur.");
}

public interface IBoyahaneTabletDeviceService
{
    Task<Result> ResetDeviceAsync(int tabletId, CancellationToken cancellationToken);
    Task<Result<IReadOnlyList<TabletPairingStationDto>>> GetPairingStationsAsync(TabletPairingListRequest request, CancellationToken cancellationToken);
    Task<Result<PairTabletResponse>> PairAsync(PairTabletRequest request, CancellationToken cancellationToken);
    Task<Result<BoyahaneTabletTokenResponse>> IssueTokenAsync(BoyahaneTabletTokenRequest request, CancellationToken cancellationToken);
    Task<Result<BoyahaneTabletContextDto>> GetContextAsync(CancellationToken cancellationToken);
    Task<Result> HeartbeatAsync(CancellationToken cancellationToken);
    Task<Result<BoyahaneTabletContextDto>> LoginAsync(BoyahaneTabletLoginRequest request, CancellationToken cancellationToken);
    Task<Result<BoyahaneTabletContextDto>> LogoutAsync(CancellationToken cancellationToken);
    Task<Result<BoyahaneTabletContextDto>> PauseAsync(BoyahaneTabletPauseRequest request, CancellationToken cancellationToken);
    Task<Result<BoyahaneTabletContextDto>> ResumeAsync(BoyahaneTabletResumeRequest request, CancellationToken cancellationToken);
    Task<Result<BoyahaneTabletContextDto>> ReportBreakdownAsync(BoyahaneTabletBreakdownRequest request, CancellationToken cancellationToken);
    Task<Result<BoyahaneTabletContextDto>> ReportRecoveryAsync(BoyahaneTabletRecoveryRequest request, CancellationToken cancellationToken);
    Task<Result<BoyahaneBatchDto>> ReadBatchAsync(BoyahaneTabletReadRequest request, CancellationToken cancellationToken);
    Task<Result<BoyahaneBatchDto>> GetBatchAsync(int batchId, CancellationToken cancellationToken);
    Task<Result<BoyahaneBatchDto>> StartStepAsync(int batchId, int stepId, BoyahaneTabletStartRequest request, CancellationToken cancellationToken);
    Task<Result<BoyahaneBatchDto>> CompleteStepAsync(int batchId, int stepId, CompleteBatchStepRequest request, CancellationToken cancellationToken);
}

public sealed class BoyahaneTabletDeviceService(
    IAppDbContext db,
    ITokenService tokenService,
    IBoyahaneBatchService batches,
    IBoyahaneMachineStatusService machineStatus,
    IOptions<TabletPairingOptions> pairingOptions,
    IOptions<FloorSessionOptions> floorOptions,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : IBoyahaneTabletDeviceService
{
    private const string ModuleKey = "Boyahane";

    private static readonly Error NotFound = new(ErrorCodes.NotFound, "Tablet bulunamadı.", ErrorType.NotFound);
    private static readonly Error InvalidCredentials = new(
        ErrorCodes.InvalidDeviceCredentials, "Cihaz kimlik bilgileri geçersiz. Tabletin yeniden eşleştirilmesi gerekebilir.", ErrorType.Unauthorized);
    private static readonly Error NoSession = new("SESSION_REQUIRED", "Önce personel kartınızı okutun.", ErrorType.Conflict);
    private static readonly Error Paused = new("SESSION_PAUSED", "Oturum duraklatılmış; önce işe dönün.", ErrorType.Conflict);
    private static readonly Error Broken = new("SESSION_INTERRUPTED", "Makine arızalı. Arıza giderilince yetkili kartıyla kapatın.", ErrorType.Conflict);

    private DateTime UtcNow => timeProvider.GetUtcNow().UtcDateTime;

    public async Task<Result> ResetDeviceAsync(int tabletId, CancellationToken cancellationToken)
    {
        var tablet = await db.Set<BoyahaneTablet>().SingleOrDefaultAsync(t => t.Id == tabletId, cancellationToken);
        if (tablet is null)
        {
            return NotFound;
        }

        tablet.ResetDevice();
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<TabletPairingStationDto>>> GetPairingStationsAsync(TabletPairingListRequest request, CancellationToken cancellationToken)
    {
        if (!TabletPairing.PasswordMatches(pairingOptions.Value, request.Password))
        {
            return TabletPairing.InvalidPassword;
        }

        var deviceIdentifier = request.DeviceIdentifier.Trim();
        var tablets = await db.Set<BoyahaneTablet>().AsNoTracking()
            .Select(t => new { t.Id, t.TabletCode, t.Name, t.MachineId, t.IsActive, t.DeviceKeyHash, t.DeviceIdentifier })
            .ToListAsync(cancellationToken);
        var machines = await db.Set<BoyahaneMachine>().AsNoTracking()
            .Where(m => m.IsActive)
            .OrderBy(m => m.Code)
            .Select(m => new { m.Id, m.Code, m.Name })
            .ToListAsync(cancellationToken);

        var stations = machines.Select(m =>
        {
            var tablet = tablets.SingleOrDefault(t => t.MachineId == m.Id);
            return new TabletPairingStationDto(
                m.Id, null, m.Code, m.Name, null, tablet?.TabletCode,
                tablet?.DeviceKeyHash is not null,
                tablet?.DeviceKeyHash is not null && tablet.DeviceIdentifier == deviceIdentifier);
        });

        // Makinesiz tablet kontrol noktasıdır: renk kontrolü, final kalite, paketleme.
        var controlPoints = tablets
            .Where(t => t.MachineId is null && t.IsActive)
            .OrderBy(t => t.TabletCode)
            .Select(t => new TabletPairingStationDto(
                null, t.Id, t.TabletCode, t.Name ?? t.TabletCode, "Kontrol noktası", t.TabletCode,
                t.DeviceKeyHash is not null,
                t.DeviceKeyHash is not null && t.DeviceIdentifier == deviceIdentifier));

        return stations.Concat(controlPoints).ToList();
    }

    public async Task<Result<PairTabletResponse>> PairAsync(PairTabletRequest request, CancellationToken cancellationToken)
    {
        if (!TabletPairing.PasswordMatches(pairingOptions.Value, request.Password))
        {
            return TabletPairing.InvalidPassword;
        }

        BoyahaneMachine? machine = null;
        BoyahaneTablet? tablet;
        if (request.MachineId is { } machineId)
        {
            machine = await db.Set<BoyahaneMachine>().AsNoTracking().SingleOrDefaultAsync(m => m.Id == machineId && m.IsActive, cancellationToken);
            if (machine is null)
            {
                return TabletPairing.StationNotFound;
            }

            tablet = await db.Set<BoyahaneTablet>().SingleOrDefaultAsync(t => t.MachineId == machineId, cancellationToken);
        }
        else
        {
            tablet = await db.Set<BoyahaneTablet>().SingleOrDefaultAsync(t => t.Id == request.TabletId && t.MachineId == null, cancellationToken);
            if (tablet is null)
            {
                return TabletPairing.StationNotFound;
            }
        }

        var stationCode = machine?.Code ?? tablet!.TabletCode;
        var stationName = machine?.Name ?? tablet!.Name ?? tablet!.TabletCode;
        var deviceIdentifier = request.DeviceIdentifier.Trim();
        if (tablet is { IsActive: false })
        {
            return TabletPairing.TabletInactive;
        }

        if (tablet is { IsEnrolled: true } && tablet.DeviceIdentifier != deviceIdentifier && !request.TakeOver)
        {
            return TabletPairing.AlreadyPaired(stationCode);
        }

        // Cihaz başka bir istasyondan gelebilir. Bağlar önce kopar: cihaz kimliği iki kayıtta aynı anda duramaz (benzersiz index).
        var previous = await db.Set<BoyahaneTablet>().Where(t => t.DeviceIdentifier == deviceIdentifier).ToListAsync(cancellationToken);
        previous.ForEach(t => t.ResetDevice());
        tablet?.ResetDevice();
        await db.SaveChangesAsync(cancellationToken);

        if (tablet is null)
        {
            tablet = new BoyahaneTablet { TabletCode = TabletPairing.NewTabletCode("BYH", machine!.Code, machine.Id), MachineId = machine.Id };
            db.Set<BoyahaneTablet>().Add(tablet);
        }

        var deviceKey = Secrets.NewDeviceKey();
        tablet.Enroll(deviceIdentifier, Secrets.Hash(deviceKey));
        tablet.LastSeenAt = UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return new PairTabletResponse(tablet.Id, tablet.TabletCode, deviceKey, stationCode, stationName);
    }

    public async Task<Result<BoyahaneTabletTokenResponse>> IssueTokenAsync(BoyahaneTabletTokenRequest request, CancellationToken cancellationToken)
    {
        var code = NormalizeCode(request.TabletCode);
        var tablet = await db.Set<BoyahaneTablet>().AsNoTracking().SingleOrDefaultAsync(t => t.TabletCode == code, cancellationToken);

        // Anahtar her durumda karşılaştırılır: tabletin var olup olmadığı yanıt süresinden anlaşılmasın.
        var keyMatches = Secrets.Matches(tablet?.DeviceKeyHash ?? Secrets.Hash(string.Empty), request.DeviceKey);
        if (tablet is null || !tablet.IsActive || !keyMatches || tablet.DeviceIdentifier != request.DeviceIdentifier.Trim())
        {
            return InvalidCredentials;
        }

        await TouchAsync(tablet.Id, cancellationToken);
        var token = tokenService.CreateTabletAccessToken(tablet.Id, tablet.TabletCode, ModuleKey);
        return new BoyahaneTabletTokenResponse(token.Token, token.ExpiresAt);
    }

    public async Task<Result<BoyahaneTabletContextDto>> GetContextAsync(CancellationToken cancellationToken)
    {
        var tablet = await db.Set<BoyahaneTablet>().AsNoTracking()
            .Include(t => t.Machine)
            .SingleOrDefaultAsync(t => t.Id == currentUser.TabletId && t.IsActive, cancellationToken);
        if (tablet is null)
        {
            return InvalidCredentials;
        }

        var session = await db.Set<BoyahaneTabletSession>().AsNoTracking()
            .Where(s => s.TabletId == tablet.Id && s.EndedAt == null)
            .Select(s => new BoyahaneTabletOperatorDto(s.Operator.Id, s.Operator.Code, s.Operator.Name, s.Operator.Role, s.StartedAt))
            .FirstOrDefaultAsync(cancellationToken);
        var pause = await db.Set<BoyahaneSessionPause>().AsNoTracking()
            .Where(p => p.EndedAt == null && p.Session.TabletId == tablet.Id && p.Session.EndedAt == null)
            .Select(p => new BoyahaneTabletPauseDto(p.Kind, p.Reason != null ? p.Reason.Name : p.Note, p.BreakSchedule != null ? p.BreakSchedule.Name : null, p.StartedAt))
            .FirstOrDefaultAsync(cancellationToken);
        var statusReason = tablet.Machine is null || tablet.Machine.Status == DyeMachineStatus.Available
            ? null
            : await db.Set<BoyahaneMachineStatusLog>().AsNoTracking()
                .Where(l => l.MachineId == tablet.MachineId && l.EndedAt == null)
                .Select(l => l.Reason)
                .FirstOrDefaultAsync(cancellationToken);
        var reasons = await db.Set<BoyahaneStopReason>().AsNoTracking()
            .Where(r => r.IsActive)
            .OrderBy(r => r.SortOrder).ThenBy(r => r.Name)
            .Select(r => new { r.Id, r.Name, r.Category })
            .ToListAsync(cancellationToken);
        var currentBatchId = await db.Set<BoyahaneBatchStep>().AsNoTracking()
            .Where(s => s.Status == BatchStepStatus.InProgress
                && (s.TabletId == tablet.Id || (tablet.MachineId != null && s.MachineId == tablet.MachineId)))
            .OrderByDescending(s => s.StartedAt)
            .Select(s => (int?)s.BatchId)
            .FirstOrDefaultAsync(cancellationToken);
        var chemicals = await db.Set<BoyahaneDyeChemical>().AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.Code)
            .Select(c => new BoyahaneTabletChemicalDto(c.Id, c.Code, c.Name, c.Kind))
            .ToListAsync(cancellationToken);

        // Moladaki operatöre yeni mola hatırlatılmaz.
        var openSession = await db.Set<BoyahaneTabletSession>().AsNoTracking().FirstOrDefaultAsync(s => s.TabletId == tablet.Id && s.EndedAt == null, cancellationToken);
        var dueBreak = openSession is null || pause?.Kind == BoyahanePauseKind.Break
            ? null
            : await BoyahaneBreaks.FindDueAsync(db, openSession, UtcNow, floorOptions.Value, cancellationToken);

        return new BoyahaneTabletContextDto(
            tablet.Id,
            tablet.TabletCode,
            UtcNow,
            tablet.Machine is null ? null : new BoyahaneTabletMachineDto(tablet.Machine.Id, tablet.Machine.Code, tablet.Machine.Name),
            tablet.Machine?.Status,
            statusReason,
            session,
            pause,
            currentBatchId,
            chemicals,
            reasons.Where(r => r.Category == BoyahaneStopReasonCategory.Pause).Select(r => new BoyahaneTabletReasonDto(r.Id, r.Name)).ToList(),
            reasons.Where(r => r.Category == BoyahaneStopReasonCategory.Breakdown).Select(r => new BoyahaneTabletReasonDto(r.Id, r.Name)).ToList(),
            dueBreak,
            Math.Max(floorOptions.Value.IdlePromptMinutes, 0));
    }

    public async Task<Result> HeartbeatAsync(CancellationToken cancellationToken) =>
        currentUser.TabletId is { } tabletId && await TouchAsync(tabletId, cancellationToken) > 0 ? Result.Success() : InvalidCredentials;

    /// <summary>
    /// Konfeksiyon oturum kuralı: başka operatörün oturumu süren işi yoksa devralınır; operatörün başka tabletteki oturumu
    /// süren işi yoksa kapatılır. Süren adım varsa önce bitirilmeli ya da oturum ofisten kapatılmalıdır.
    /// </summary>
    public async Task<Result<BoyahaneTabletContextDto>> LoginAsync(BoyahaneTabletLoginRequest request, CancellationToken cancellationToken)
    {
        if (RfidUid.Normalize(request.Uid) is not { } uid)
        {
            return new Error("CARD_NOT_RECOGNIZED", "Personel kartı okunamadı.", ErrorType.Conflict);
        }

        if (await BoyahaneCards.CheckAsync(db, uid, BoyahaneCardType.Operator, cancellationToken) is { } cardError)
        {
            return cardError;
        }

        var person = await db.Set<BoyahaneOperator>().AsNoTracking().SingleOrDefaultAsync(o => o.CardUid == uid, cancellationToken);
        if (person is null || !person.IsActive)
        {
            return new Error("CARD_NOT_RECOGNIZED", "Bu kart aktif bir operatöre atanmamış.", ErrorType.Conflict);
        }

        var tabletId = currentUser.TabletId!.Value;
        var tabletMachineId = await db.Set<BoyahaneTablet>().Where(t => t.Id == tabletId).Select(t => t.MachineId).SingleOrDefaultAsync(cancellationToken);
        if (await BoyahaneOperatorCardService.CheckQualificationAsync(db, person.Id, tabletMachineId, null, cancellationToken) is { } qualificationError)
        {
            return qualificationError;
        }

        var open = await db.Set<BoyahaneTabletSession>()
            .Include(s => s.Pauses)
            .Where(s => s.EndedAt == null && (s.TabletId == tabletId || s.OperatorId == person.Id))
            .ToListAsync(cancellationToken);
        if (open.Any(s => s.TabletId == tabletId && s.OperatorId == person.Id))
        {
            return await GetContextAsync(cancellationToken);
        }

        var onTablet = open.SingleOrDefault(s => s.TabletId == tabletId);
        if (onTablet is not null && await HasWorkAsync(onTablet, cancellationToken))
        {
            return new Error("WORK_IN_PROGRESS", "Bu tablette başka operatörün süren adımı var; önce o adım bitirilmeli ya da oturum ofisten kapatılmalı.", ErrorType.Conflict);
        }

        var elsewhere = open.SingleOrDefault(s => s.OperatorId == person.Id);
        if (elsewhere is not null && await HasWorkAsync(elsewhere, cancellationToken))
        {
            var code = await db.Set<BoyahaneTablet>().Where(t => t.Id == elsewhere.TabletId).Select(t => t.TabletCode).SingleAsync(cancellationToken);
            return new Error("OPERATOR_BUSY", $"Operatörün {code} tabletinde süren adımı var; önce o adım bitirilmeli.", ErrorType.Conflict);
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var now = UtcNow;
        if (onTablet is not null)
        {
            BoyahaneSessions.Close(onTablet, BoyahaneSessionEndReason.TakenOver, now);
        }

        if (elsewhere is not null)
        {
            BoyahaneSessions.Close(elsewhere, BoyahaneSessionEndReason.MovedAway, now);
        }

        await db.SaveChangesAsync(cancellationToken);

        var session = new BoyahaneTabletSession { TabletId = tabletId, OperatorId = person.Id, StartedAt = now };
        var machineBroken = await db.Set<BoyahaneTablet>()
            .AnyAsync(t => t.Id == tabletId && t.Machine != null && t.Machine.Status == DyeMachineStatus.Breakdown, cancellationToken);
        if (machineBroken)
        {
            // Arızalı makinede açılan oturum arıza durumunda başlar.
            session.Pauses.Add(new BoyahaneSessionPause { Kind = BoyahanePauseKind.MachineBreakdown, StartedAt = now });
        }

        db.Set<BoyahaneTabletSession>().Add(session);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetContextAsync(cancellationToken);
    }

    public async Task<Result<BoyahaneTabletContextDto>> LogoutAsync(CancellationToken cancellationToken)
    {
        var open = await OpenSessionAsync(cancellationToken);
        if (open is not null)
        {
            if (await HasWorkAsync(open, cancellationToken))
            {
                return new Error("WORK_IN_PROGRESS", "Süren adımınız var; önce adımı bitirin ya da oturumu ofisten kapattırın.", ErrorType.Conflict);
            }

            BoyahaneSessions.Close(open, BoyahaneSessionEndReason.Logout, UtcNow);
            await db.SaveChangesAsync(cancellationToken);
        }

        return await GetContextAsync(cancellationToken);
    }

    public async Task<Result<BoyahaneTabletContextDto>> PauseAsync(BoyahaneTabletPauseRequest request, CancellationToken cancellationToken)
    {
        var session = await OpenSessionAsync(cancellationToken);
        if (session is null)
        {
            return NoSession;
        }

        var current = session.Pauses.SingleOrDefault(p => p.EndedAt == null);
        if (current?.Kind == BoyahanePauseKind.MachineBreakdown)
        {
            return Broken;
        }

        if (request.Kind == BoyahanePauseKind.Pause
            && !await db.Set<BoyahaneStopReason>().AnyAsync(r => r.Id == request.ReasonId && r.IsActive && r.Category == BoyahaneStopReasonCategory.Pause, cancellationToken))
        {
            return new Error("REASON_NOT_FOUND", "Seçilen ara nedeni bulunamadı.", ErrorType.Conflict);
        }

        // Ara kartla verilir (mola değil): kişiye özel yetkili oturum sahibi ya da görevle yetkili kişi.
        if (request.Kind == BoyahanePauseKind.Pause)
        {
            var actor = await FloorActorAsync(BoyahaneFloorAction.Pause, session.OperatorId, request.AuthorizerUid, cancellationToken);
            if (!actor.IsSuccess)
            {
                return actor.Error!;
            }
        }

        int? breakScheduleId = null;
        if (request.Kind == BoyahanePauseKind.Break)
        {
            var due = await BoyahaneBreaks.FindDueAsync(db, session, UtcNow, floorOptions.Value, cancellationToken);
            if (due is null)
            {
                return new Error(ErrorCodes.NoBreakDue, "Şu an planlı molanız yok. Kısa bir ara için 'Ara ver' seçeneğini kullanın.");
            }

            if (!due.CanStartNow)
            {
                return new Error(ErrorCodes.BreakAfterWork, due.Hint!);
            }

            breakScheduleId = due.BreakScheduleId;
        }

        if (request.Kind == BoyahanePauseKind.WaitingForBatch && await HasWorkAsync(session, cancellationToken))
        {
            return new Error(ErrorCodes.WorkInProgress, "Süren adımınız var; parti bekleme bildirilemez.", ErrorType.Conflict);
        }

        var now = UtcNow;
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (current is not null)
        {
            current.EndedAt = now;
            await db.SaveChangesAsync(cancellationToken);
        }

        session.Pauses.Add(new BoyahaneSessionPause
        {
            Kind = request.Kind,
            ReasonId = request.Kind == BoyahanePauseKind.Pause ? request.ReasonId : null,
            BreakScheduleId = breakScheduleId,
            Note = NullIfBlank(request.Note),
            StartedAt = now,
        });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetContextAsync(cancellationToken);
    }

    public async Task<Result<BoyahaneTabletContextDto>> ResumeAsync(BoyahaneTabletResumeRequest request, CancellationToken cancellationToken)
    {
        var session = await OpenSessionAsync(cancellationToken);
        if (session is null)
        {
            return NoSession;
        }

        var current = session.Pauses.SingleOrDefault(p => p.EndedAt == null);
        if (current?.Kind == BoyahanePauseKind.MachineBreakdown)
        {
            return Broken;
        }

        // Aradan dönüş de ara gibi kart ister; moladan dönüş serbesttir.
        if (current?.Kind == BoyahanePauseKind.Pause)
        {
            var actor = await FloorActorAsync(BoyahaneFloorAction.Pause, session.OperatorId, request.AuthorizerUid, cancellationToken);
            if (!actor.IsSuccess)
            {
                return actor.Error!;
            }
        }

        if (current is not null)
        {
            current.EndedAt = UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }

        return await GetContextAsync(cancellationToken);
    }

    /// <summary>Arıza kartla bildirilir: kişiye özel yetkili oturum sahibi ya da görevle yetkili kişi.</summary>
    public async Task<Result<BoyahaneTabletContextDto>> ReportBreakdownAsync(BoyahaneTabletBreakdownRequest request, CancellationToken cancellationToken)
    {
        var tablet = await db.Set<BoyahaneTablet>().AsNoTracking().SingleOrDefaultAsync(t => t.Id == currentUser.TabletId && t.IsActive, cancellationToken);
        if (tablet is null)
        {
            return InvalidCredentials;
        }

        if (tablet.MachineId is not { } machineId)
        {
            return new Error("STEP_NEEDS_MACHINE", "Bu tablet bir makineye bağlı değil; arıza bildirilemez.", ErrorType.Conflict);
        }

        var session = await OpenSessionAsync(cancellationToken);
        if (session is null)
        {
            return NoSession;
        }

        var reason = NullIfBlank(request.Note);
        if (request.ReasonId is { } reasonId)
        {
            var name = await db.Set<BoyahaneStopReason>()
                .Where(r => r.Id == reasonId && r.IsActive && r.Category == BoyahaneStopReasonCategory.Breakdown)
                .Select(r => r.Name)
                .FirstOrDefaultAsync(cancellationToken);
            if (name is null)
            {
                return new Error("REASON_NOT_FOUND", "Seçilen arıza nedeni bulunamadı.", ErrorType.Conflict);
            }

            reason = reason is null ? name : $"{name} — {reason}";
        }

        var actor = await FloorActorAsync(BoyahaneFloorAction.ReportBreakdown, session.OperatorId, request.AuthorizerUid, cancellationToken);
        if (!actor.IsSuccess)
        {
            return actor.Error!;
        }

        var changed = await machineStatus.ChangeAsync(machineId, DyeMachineStatus.Breakdown, reason, actor.Value, cancellationToken);
        return changed.IsSuccess ? await GetContextAsync(cancellationToken) : changed.Error!;
    }

    /// <summary>Arızayı yalnızca yetkili görevdeki kişi, kartını okutarak kapatır.</summary>
    public async Task<Result<BoyahaneTabletContextDto>> ReportRecoveryAsync(BoyahaneTabletRecoveryRequest request, CancellationToken cancellationToken)
    {
        var machineId = await db.Set<BoyahaneTablet>().AsNoTracking().Where(t => t.Id == currentUser.TabletId && t.IsActive).Select(t => t.MachineId).FirstOrDefaultAsync(cancellationToken);
        if (machineId is null)
        {
            return new Error("STEP_NEEDS_MACHINE", "Bu tablet bir makineye bağlı değil.", ErrorType.Conflict);
        }

        var person = await AuthorizerAsync(BoyahaneFloorAction.ResolveBreakdown, null, request.AuthorizerUid, cancellationToken);
        if (!person.IsSuccess)
        {
            return person.Error!;
        }

        var changed = await machineStatus.ChangeAsync(machineId.Value, DyeMachineStatus.Available, null, person.Value.Name, cancellationToken);
        return changed.IsSuccess ? await GetContextAsync(cancellationToken) : changed.Error!;
    }

    /// <summary>İşlemi onaylayan: kart her zaman okutulur; okutulmadıysa kart istenir.</summary>
    private async Task<Result<string>> FloorActorAsync(BoyahaneFloorAction action, int sessionOperatorId, string? authorizerUid, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(authorizerUid))
        {
            return new Error(ErrorCodes.AuthorizationCardRequired, $"Bu işlem için {WhoCan(action)} kartı okutulmalıdır.", ErrorType.Forbidden);
        }

        var authorizer = await AuthorizerAsync(action, sessionOperatorId, authorizerUid, cancellationToken);
        return authorizer.IsSuccess ? authorizer.Value.Name : authorizer.Error!;
    }

    /// <summary>
    /// Okutulan kartın sahibi. Görevle yetkili kişi her tablette geçer; kişiye özel yetki yalnızca kendi oturumunun
    /// açık olduğu tablette geçer: başka tabletin işini operatör başlatıp bitiremez.
    /// </summary>
    private async Task<Result<BoyahaneOperator>> AuthorizerAsync(BoyahaneFloorAction action, int? sessionOperatorId, string? authorizerUid, CancellationToken cancellationToken)
    {
        var uid = RfidUid.Normalize(authorizerUid);
        if (uid is null || await BoyahaneCards.CheckAsync(db, uid, BoyahaneCardType.Operator, cancellationToken) is not null)
        {
            return new Error(ErrorCodes.NotAuthorizedForAction, "Bu kart yetkili personel kartı değil.", ErrorType.Forbidden);
        }

        var person = await db.Set<BoyahaneOperator>().AsNoTracking().SingleOrDefaultAsync(o => o.CardUid == uid && o.IsActive, cancellationToken);
        if (person is not null && person.RoleAllows(action))
        {
            return person;
        }

        if (person is not null && person.PersonallyAllows(action))
        {
            return person.Id == sessionOperatorId
                ? person
                : new Error(ErrorCodes.NotAuthorizedForAction, $"{person.Name} bu işlemi yalnızca kendi oturumunun açık olduğu tablette yapabilir.", ErrorType.Forbidden);
        }

        return new Error(ErrorCodes.NotAuthorizedForAction, $"Bu işlem için {WhoCan(action)} kartı gerekir.", ErrorType.Forbidden);
    }

    private static string WhoCan(BoyahaneFloorAction action) =>
        action switch
        {
            BoyahaneFloorAction.Pause => "yetkili operatör ya da vardiya amiri",
            BoyahaneFloorAction.ReportBreakdown => "yetkili operatör, vardiya amiri ya da bakım personeli",
            _ => "vardiya amiri ya da bakım personeli",
        };

    private Task<BoyahaneTabletSession?> OpenSessionAsync(CancellationToken cancellationToken) =>
        db.Set<BoyahaneTabletSession>().Include(s => s.Pauses).SingleOrDefaultAsync(s => s.TabletId == currentUser.TabletId && s.EndedAt == null, cancellationToken);

    /// <summary>Oturumun personelinin bu tablette başlatıp bitirmediği adım.</summary>
    private Task<bool> HasWorkAsync(BoyahaneTabletSession session, CancellationToken cancellationToken) =>
        db.Set<BoyahaneBatchStep>().AnyAsync(
            s => s.Status == BatchStepStatus.InProgress && s.TabletId == session.TabletId && s.StartedByOperatorId == session.OperatorId,
            cancellationToken);

    public async Task<Result<BoyahaneBatchDto>> ReadBatchAsync(BoyahaneTabletReadRequest request, CancellationToken cancellationToken)
    {
        return await batches.FindByCardAsync(request.Code, cancellationToken);
    }

    public Task<Result<BoyahaneBatchDto>> GetBatchAsync(int batchId, CancellationToken cancellationToken) => batches.GetAsync(batchId, cancellationToken);

    public async Task<Result<BoyahaneBatchDto>> StartStepAsync(int batchId, int stepId, BoyahaneTabletStartRequest request, CancellationToken cancellationToken)
    {
        if (await ActorAsync(cancellationToken) is not { } actor)
        {
            return InvalidCredentials;
        }

        if (actor.OperatorId is null)
        {
            return NoSession;
        }

        if (await db.Set<BoyahaneSessionPause>().AnyAsync(p => p.EndedAt == null && p.Kind != BoyahanePauseKind.WaitingForBatch && p.Session.TabletId == actor.TabletId && p.Session.EndedAt == null, cancellationToken))
        {
            return Paused;
        }

        // Üretim hareketi operatörün işe döndüğünü gösterir: parti bekleme biter.
        var started = await batches.StartStepAsync(batchId, stepId, actor, request.RequestId, cancellationToken);
        if (started.IsSuccess)
        {
            var now = UtcNow;
            await db.Set<BoyahaneSessionPause>()
                .Where(p => p.EndedAt == null && p.Kind == BoyahanePauseKind.WaitingForBatch && p.Session.TabletId == actor.TabletId && p.Session.EndedAt == null)
                .ExecuteUpdateAsync(update => update.SetProperty(p => p.EndedAt, now), cancellationToken);
        }

        return started;
    }

    public async Task<Result<BoyahaneBatchDto>> CompleteStepAsync(int batchId, int stepId, CompleteBatchStepRequest request, CancellationToken cancellationToken)
    {
        if (await ActorAsync(cancellationToken) is not { } actor)
        {
            return InvalidCredentials;
        }

        // Yeniden gönderilen işlem zaten kayıtlıysa sonucu döner; oturum sonradan kapanmış olabilir.
        if (request.RequestId is { } duplicate && await db.Set<BoyahaneStepRequest>().AnyAsync(r => r.RequestId == duplicate, cancellationToken))
        {
            return await batches.GetAsync(batchId, cancellationToken);
        }

        if (actor.OperatorId is null)
        {
            return NoSession;
        }

        if (await db.Set<BoyahaneSessionPause>().AnyAsync(p => p.EndedAt == null && p.Kind != BoyahanePauseKind.WaitingForBatch && p.Session.TabletId == actor.TabletId && p.Session.EndedAt == null, cancellationToken))
        {
            return Paused;
        }

        return await batches.CompleteStepAsync(batchId, stepId, request, actor, cancellationToken);
    }

    private async Task<BatchStepActor?> ActorAsync(CancellationToken cancellationToken)
    {
        var tablet = await db.Set<BoyahaneTablet>().AsNoTracking()
            .Where(t => t.Id == currentUser.TabletId && t.IsActive)
            .Select(t => new { t.Id, t.MachineId })
            .SingleOrDefaultAsync(cancellationToken);
        if (tablet is null)
        {
            return null;
        }

        var operatorId = await db.Set<BoyahaneTabletSession>().AsNoTracking()
            .Where(s => s.TabletId == tablet.Id && s.EndedAt == null)
            .Select(s => (int?)s.OperatorId)
            .FirstOrDefaultAsync(cancellationToken);
        return new BatchStepActor(tablet.Id, operatorId, tablet.MachineId);
    }

    private Task<int> TouchAsync(int tabletId, CancellationToken cancellationToken)
    {
        var now = UtcNow;
        return db.Set<BoyahaneTablet>()
            .Where(t => t.Id == tabletId && t.IsActive)
            .ExecuteUpdateAsync(update => update.SetProperty(t => t.LastSeenAt, now), cancellationToken);
    }
}
