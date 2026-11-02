using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Core.Domain.Common;
using ERP.Dokuma.Application.Features.Weaving;
using ERP.Dokuma.Domain.Entities;
using ERP.Dokuma.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using static ERP.Core.Application.Features.MasterData.MasterDataRules;

namespace ERP.Dokuma.Application.Features.Floor;

public sealed record DokumaTabletTokenRequest(string TabletCode, string DeviceKey, string DeviceIdentifier);

public sealed class DokumaTabletTokenRequestValidator : AbstractValidator<DokumaTabletTokenRequest>
{
    public DokumaTabletTokenRequestValidator()
    {
        RuleFor(x => x.TabletCode).NotEmpty().WithMessage("Tablet kodu zorunludur.").MaximumLength(30);
        RuleFor(x => x.DeviceKey).NotEmpty().WithMessage("Cihaz anahtarı zorunludur.");
        RuleFor(x => x.DeviceIdentifier).NotEmpty().WithMessage("Cihaz kimliği zorunludur.");
    }
}

public sealed record DokumaTabletTokenResponse(string AccessToken, DateTime AccessTokenExpiresAt);

public sealed record DokumaTabletOperatorDto(int Id, string Code, string Name, DokumaOperatorRole Role, DateTime LoginAt);

public sealed record DokumaTabletPauseDto(DokumaPauseKind Kind, string? ReasonName, string? BreakName, DateTime StartedAt);

public sealed record DokumaTabletReasonDto(int Id, string Name, DokumaStopCategory Category);

/// <param name="LoomId">Tabletin bağlı olduğu tezgâh; boşsa salon tabletidir.</param>
/// <param name="Pause">Oturumdaki açık mola ya da ara.</param>
/// <param name="Looms">Tabletin gösterdiği tezgâhlar ve üzerlerindeki levent.</param>
/// <param name="PauseReasons">Personelin "Ara ver" nedenleri.</param>
/// <param name="StopReasons">Tezgâh duruş ve arıza nedenleri.</param>
public sealed record DokumaTabletContextDto(
    int TabletId,
    string TabletCode,
    DateTime ServerTime,
    int? LoomId,
    DokumaTabletOperatorDto? Operator,
    DokumaTabletPauseDto? Pause,
    IReadOnlyList<DokumaLoomBoardItemDto> Looms,
    IReadOnlyList<DokumaTabletReasonDto> PauseReasons,
    IReadOnlyList<DokumaTabletReasonDto> StopReasons,
    DokumaDueBreakDto? DueBreak,
    int IdlePromptMinutes);

/// <param name="Kind">Mola, ara ya da levent bekleme.</param>
/// <param name="ReasonId">Arada zorunludur.</param>
/// <param name="AuthorizerUid">Ara için okutulan kart: kişiye özel yetkili oturum sahibi ya da vardiya amiri.</param>
public sealed record DokumaTabletPauseRequest(DokumaPauseKind Kind, int? ReasonId = null, string? Note = null, string? AuthorizerUid = null);

public sealed class DokumaTabletPauseRequestValidator : AbstractValidator<DokumaTabletPauseRequest>
{
    public DokumaTabletPauseRequestValidator()
    {
        RuleFor(x => x.Kind).IsInEnum().WithMessage("Mola, ara ya da levent bekleme seçilmelidir.");
        RuleFor(x => x.ReasonId).NotNull().WithMessage("Ara nedeni seçilmelidir.").When(x => x.Kind == DokumaPauseKind.Pause);
        RuleFor(x => x.Note).OptionalText(500);
    }
}

/// <param name="AuthorizerUid">Aradan dönüşte okutulan kart; moladan dönüşte gerekmez.</param>
public sealed record DokumaTabletResumeRequest(string? AuthorizerUid = null);

/// <param name="AuthorizerUid">Arıza için okutulan kart: kişiye özel yetkili oturum sahibi, vardiya amiri ya da bakım. Diğer duruşlarda boş.</param>
public sealed record DokumaTabletStopRequest(int ReasonId, string? Note = null, string? AuthorizerUid = null);

public sealed class DokumaTabletStopRequestValidator : AbstractValidator<DokumaTabletStopRequest>
{
    public DokumaTabletStopRequestValidator()
    {
        RuleFor(x => x.ReasonId).GreaterThan(0).WithMessage("Neden seçilmelidir.");
        RuleFor(x => x.Note).OptionalText(500);
    }
}

public sealed record DokumaTabletLoginRequest(string Uid);

/// <param name="RequestId">Bağlantı kesilince yeniden gönderilen kesim ikinci top açmasın diye.</param>
/// <param name="CutOn">Kesimin yapıldığı gün; kayıt sonradan gönderilebilir.</param>
public sealed record DokumaTabletRollRequest(Guid RequestId, DateOnly CutOn, decimal Meters, decimal? Kilograms = null, string? Notes = null);

public sealed class DokumaTabletRollRequestValidator : AbstractValidator<DokumaTabletRollRequest>
{
    public DokumaTabletRollRequestValidator()
    {
        RuleFor(x => x.RequestId).NotEmpty().WithMessage("İşlem kimliği zorunludur.");
        RuleFor(x => x.CutOn).NotEmpty().WithMessage("Kesim tarihi zorunludur.");
        RuleFor(x => x.Meters)
            .GreaterThan(0).WithMessage("Metre sıfırdan büyük olmalıdır.")
            .PrecisionScale(12, 2, true).WithMessage("Metre en fazla iki ondalık olabilir.");
        RuleFor(x => x.Kilograms)
            .GreaterThan(0).WithMessage("Kilo sıfırdan büyük olmalıdır.")
            .PrecisionScale(12, 2, true).WithMessage("Kilo en fazla iki ondalık olabilir.")
            .When(x => x.Kilograms is not null);
        RuleFor(x => x.Notes).OptionalText(500);
    }
}

public sealed record DokumaTabletRollDto(string RollNo, decimal Meters);

/// <param name="AuthorizerUid">Arızayı kapatan vardiya amirinin ya da bakımcının okutulan kartı; diğer duruşlarda boş.</param>
public sealed record DokumaTabletEndStopRequest(string? AuthorizerUid = null);

public interface IDokumaTabletDeviceService
{
    Task<Result> ResetDeviceAsync(int tabletId, CancellationToken cancellationToken);
    Task<Result<IReadOnlyList<TabletPairingStationDto>>> GetPairingStationsAsync(TabletPairingListRequest request, CancellationToken cancellationToken);
    Task<Result<PairTabletResponse>> PairAsync(PairTabletRequest request, CancellationToken cancellationToken);
    Task<Result<DokumaTabletTokenResponse>> IssueTokenAsync(DokumaTabletTokenRequest request, CancellationToken cancellationToken);
    Task<Result<DokumaTabletContextDto>> GetContextAsync(CancellationToken cancellationToken);
    Task<Result> HeartbeatAsync(CancellationToken cancellationToken);
    Task<Result<DokumaTabletContextDto>> LoginAsync(DokumaTabletLoginRequest request, CancellationToken cancellationToken);
    Task<Result<DokumaTabletContextDto>> LogoutAsync(CancellationToken cancellationToken);
    Task<Result<DokumaTabletContextDto>> PauseAsync(DokumaTabletPauseRequest request, CancellationToken cancellationToken);
    Task<Result<DokumaTabletContextDto>> ResumeAsync(DokumaTabletResumeRequest request, CancellationToken cancellationToken);
    Task<Result<DokumaTabletRollDto>> CutRollAsync(int beamId, DokumaTabletRollRequest request, CancellationToken cancellationToken);
    Task<Result<DokumaTabletContextDto>> StartStopAsync(int loomId, DokumaTabletStopRequest request, CancellationToken cancellationToken);
    Task<Result<DokumaTabletContextDto>> EndStopAsync(int loomId, DokumaTabletEndStopRequest request, CancellationToken cancellationToken);
}

public sealed class DokumaTabletDeviceService(
    IAppDbContext db,
    ITokenService tokenService,
    IDokumaWeavingService weaving,
    IDokumaLoomStopService stops,
    IOptions<TabletPairingOptions> pairingOptions,
    IOptions<FloorSessionOptions> floorOptions,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : IDokumaTabletDeviceService
{
    private const string ModuleKey = "Dokuma";

    private static readonly Error NotFound = new(ErrorCodes.NotFound, "Tablet bulunamadı.", ErrorType.NotFound);
    private static readonly Error InvalidCredentials = new(
        ErrorCodes.InvalidDeviceCredentials, "Cihaz kimlik bilgileri geçersiz. Tabletin yeniden eşleştirilmesi gerekebilir.", ErrorType.Unauthorized);
    private static readonly Error NoSession = new("SESSION_REQUIRED", "Önce personel kartınızı okutun.", ErrorType.Conflict);
    private static readonly Error OtherLoom = new("LOOM_NOT_ON_TABLET", "Bu tablet o tezgâha bağlı değil.", ErrorType.Forbidden);
    private static readonly Error Paused = new("SESSION_PAUSED", "Oturum duraklatılmış; önce işe dönün.", ErrorType.Conflict);

    private DateTime UtcNow => timeProvider.GetUtcNow().UtcDateTime;

    public async Task<Result> ResetDeviceAsync(int tabletId, CancellationToken cancellationToken)
    {
        var tablet = await db.Set<DokumaTablet>().SingleOrDefaultAsync(t => t.Id == tabletId, cancellationToken);
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
        var tablets = await db.Set<DokumaTablet>().AsNoTracking()
            .Select(t => new { t.Id, t.TabletCode, t.Name, t.LoomId, t.IsActive, t.DeviceKeyHash, t.DeviceIdentifier })
            .ToListAsync(cancellationToken);
        var looms = await db.Set<DokumaLoom>().AsNoTracking()
            .Where(l => l.IsActive)
            .OrderBy(l => l.Code)
            .Select(l => new { l.Id, l.Code, l.Name })
            .ToListAsync(cancellationToken);

        var stations = looms.Select(l =>
        {
            var tablet = tablets.SingleOrDefault(t => t.LoomId == l.Id);
            return new TabletPairingStationDto(
                l.Id, null, l.Code, l.Name, null, tablet?.TabletCode,
                tablet?.DeviceKeyHash is not null,
                tablet?.DeviceKeyHash is not null && tablet.DeviceIdentifier == deviceIdentifier);
        });

        // Tezgâhsız tablet bütün tezgâhları gösterir.
        var halls = tablets
            .Where(t => t.LoomId is null && t.IsActive)
            .OrderBy(t => t.TabletCode)
            .Select(t => new TabletPairingStationDto(
                null, t.Id, t.TabletCode, t.Name ?? t.TabletCode, "Salon tableti", t.TabletCode,
                t.DeviceKeyHash is not null,
                t.DeviceKeyHash is not null && t.DeviceIdentifier == deviceIdentifier));

        return stations.Concat(halls).ToList();
    }

    public async Task<Result<PairTabletResponse>> PairAsync(PairTabletRequest request, CancellationToken cancellationToken)
    {
        if (!TabletPairing.PasswordMatches(pairingOptions.Value, request.Password))
        {
            return TabletPairing.InvalidPassword;
        }

        DokumaLoom? loom = null;
        DokumaTablet? tablet;
        if (request.MachineId is { } loomId)
        {
            loom = await db.Set<DokumaLoom>().AsNoTracking().SingleOrDefaultAsync(l => l.Id == loomId && l.IsActive, cancellationToken);
            if (loom is null)
            {
                return TabletPairing.StationNotFound;
            }

            tablet = await db.Set<DokumaTablet>().SingleOrDefaultAsync(t => t.LoomId == loomId, cancellationToken);
        }
        else
        {
            tablet = await db.Set<DokumaTablet>().SingleOrDefaultAsync(t => t.Id == request.TabletId && t.LoomId == null, cancellationToken);
            if (tablet is null)
            {
                return TabletPairing.StationNotFound;
            }
        }

        var stationCode = loom?.Code ?? tablet!.TabletCode;
        var stationName = loom?.Name ?? tablet!.Name ?? tablet!.TabletCode;
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
        var previous = await db.Set<DokumaTablet>().Where(t => t.DeviceIdentifier == deviceIdentifier).ToListAsync(cancellationToken);
        previous.ForEach(t => t.ResetDevice());
        tablet?.ResetDevice();
        await db.SaveChangesAsync(cancellationToken);

        if (tablet is null)
        {
            tablet = new DokumaTablet { TabletCode = TabletPairing.NewTabletCode("DKM", loom!.Code, loom.Id), LoomId = loom.Id };
            db.Set<DokumaTablet>().Add(tablet);
        }

        var deviceKey = Secrets.NewDeviceKey();
        tablet.Enroll(deviceIdentifier, Secrets.Hash(deviceKey));
        tablet.LastSeenAt = UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return new PairTabletResponse(tablet.Id, tablet.TabletCode, deviceKey, stationCode, stationName);
    }

    public async Task<Result<DokumaTabletTokenResponse>> IssueTokenAsync(DokumaTabletTokenRequest request, CancellationToken cancellationToken)
    {
        var code = NormalizeCode(request.TabletCode);
        var tablet = await db.Set<DokumaTablet>().AsNoTracking().SingleOrDefaultAsync(t => t.TabletCode == code, cancellationToken);

        // Anahtar her durumda karşılaştırılır: tabletin var olup olmadığı yanıt süresinden anlaşılmasın.
        var keyMatches = Secrets.Matches(tablet?.DeviceKeyHash ?? Secrets.Hash(string.Empty), request.DeviceKey);
        if (tablet is null || !tablet.IsActive || !keyMatches || tablet.DeviceIdentifier != request.DeviceIdentifier.Trim())
        {
            return InvalidCredentials;
        }

        await TouchAsync(tablet.Id, cancellationToken);
        var token = tokenService.CreateTabletAccessToken(tablet.Id, tablet.TabletCode, ModuleKey);
        return new DokumaTabletTokenResponse(token.Token, token.ExpiresAt);
    }

    public async Task<Result<DokumaTabletContextDto>> GetContextAsync(CancellationToken cancellationToken)
    {
        var tablet = await db.Set<DokumaTablet>().AsNoTracking().SingleOrDefaultAsync(t => t.Id == currentUser.TabletId && t.IsActive, cancellationToken);
        if (tablet is null)
        {
            return InvalidCredentials;
        }

        var person = await db.Set<DokumaTabletSession>().AsNoTracking()
            .Where(s => s.TabletId == tablet.Id && s.EndedAt == null)
            .Select(s => new DokumaTabletOperatorDto(s.Operator.Id, s.Operator.Code, s.Operator.Name, s.Operator.Role, s.StartedAt))
            .FirstOrDefaultAsync(cancellationToken);
        var pause = await db.Set<DokumaSessionPause>().AsNoTracking()
            .Where(p => p.EndedAt == null && p.Session.TabletId == tablet.Id && p.Session.EndedAt == null)
            .Select(p => new DokumaTabletPauseDto(p.Kind, p.Reason != null ? p.Reason.Name : p.Note, p.BreakSchedule != null ? p.BreakSchedule.Name : null, p.StartedAt))
            .FirstOrDefaultAsync(cancellationToken);
        var board = await weaving.GetBoardAsync(cancellationToken);
        var reasons = await db.Set<DokumaStopReason>().AsNoTracking()
            .Where(r => r.IsActive)
            .OrderBy(r => r.SortOrder).ThenBy(r => r.Name)
            .Select(r => new DokumaTabletReasonDto(r.Id, r.Name, r.Category))
            .ToListAsync(cancellationToken);

        // Moladaki operatöre yeni mola hatırlatılmaz.
        var openSession = await db.Set<DokumaTabletSession>().AsNoTracking().FirstOrDefaultAsync(s => s.TabletId == tablet.Id && s.EndedAt == null, cancellationToken);
        var dueBreak = openSession is null || pause?.Kind == DokumaPauseKind.Break
            ? null
            : await DokumaBreaks.FindDueAsync(db, openSession, UtcNow, floorOptions.Value, cancellationToken);

        return new DokumaTabletContextDto(
            tablet.Id,
            tablet.TabletCode,
            UtcNow,
            tablet.LoomId,
            person,
            pause,
            tablet.LoomId is { } loomId ? board.Where(l => l.LoomId == loomId).ToList() : board,
            reasons.Where(r => r.Category == DokumaStopCategory.Pause).ToList(),
            reasons.Where(r => r.Category != DokumaStopCategory.Pause).ToList(),
            dueBreak,
            Math.Max(floorOptions.Value.IdlePromptMinutes, 0));
    }

    public async Task<Result> HeartbeatAsync(CancellationToken cancellationToken) =>
        currentUser.TabletId is { } tabletId && await TouchAsync(tabletId, cancellationToken) > 0 ? Result.Success() : InvalidCredentials;

    /// <summary>Tezgâh personele bağlı değildir: yeni kart tabletteki oturumu devralır, personelin başka tabletteki oturumu kapanır.</summary>
    public async Task<Result<DokumaTabletContextDto>> LoginAsync(DokumaTabletLoginRequest request, CancellationToken cancellationToken)
    {
        if (RfidUid.Normalize(request.Uid) is not { } uid)
        {
            return new Error("CARD_NOT_RECOGNIZED", "Personel kartı okunamadı.", ErrorType.Conflict);
        }

        if (await DokumaCards.CheckAsync(db, uid, cancellationToken) is { } cardError)
        {
            return cardError;
        }

        var person = await db.Set<DokumaOperator>().AsNoTracking().SingleOrDefaultAsync(o => o.CardUid == uid, cancellationToken);
        if (person is null || !person.IsActive)
        {
            return new Error("CARD_NOT_RECOGNIZED", "Bu kart aktif bir personele atanmamış.", ErrorType.Conflict);
        }

        var tabletId = currentUser.TabletId!.Value;
        var open = await db.Set<DokumaTabletSession>()
            .Include(s => s.Pauses)
            .Where(s => s.EndedAt == null && (s.TabletId == tabletId || s.OperatorId == person.Id))
            .ToListAsync(cancellationToken);
        if (open.Any(s => s.TabletId == tabletId && s.OperatorId == person.Id))
        {
            return await GetContextAsync(cancellationToken);
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var now = UtcNow;
        foreach (var session in open)
        {
            DokumaSessions.Close(session, session.TabletId == tabletId ? DokumaSessionEndReason.TakenOver : DokumaSessionEndReason.MovedAway, now);
        }

        await db.SaveChangesAsync(cancellationToken);
        db.Set<DokumaTabletSession>().Add(new DokumaTabletSession { TabletId = tabletId, OperatorId = person.Id, StartedAt = now });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetContextAsync(cancellationToken);
    }

    public async Task<Result<DokumaTabletContextDto>> LogoutAsync(CancellationToken cancellationToken)
    {
        var session = await OpenSessionAsync(cancellationToken);
        if (session is not null)
        {
            DokumaSessions.Close(session, DokumaSessionEndReason.Logout, UtcNow);
            await db.SaveChangesAsync(cancellationToken);
        }

        return await GetContextAsync(cancellationToken);
    }

    public async Task<Result<DokumaTabletContextDto>> PauseAsync(DokumaTabletPauseRequest request, CancellationToken cancellationToken)
    {
        var session = await OpenSessionAsync(cancellationToken);
        if (session is null)
        {
            return NoSession;
        }

        if (request.Kind == DokumaPauseKind.Pause)
        {
            if (!await db.Set<DokumaStopReason>().AnyAsync(r => r.Id == request.ReasonId && r.IsActive && r.Category == DokumaStopCategory.Pause, cancellationToken))
            {
                return new Error("REASON_NOT_FOUND", "Seçilen ara nedeni bulunamadı.", ErrorType.Conflict);
            }

            // Ara kartla verilir (mola değil): kişiye özel yetkili oturum sahibi ya da görevle yetkili kişi.
            var actor = await FloorActorAsync(DokumaFloorAction.Pause, session.OperatorId, request.AuthorizerUid, cancellationToken);
            if (!actor.IsSuccess)
            {
                return actor.Error!;
            }
        }

        int? breakScheduleId = null;
        if (request.Kind == DokumaPauseKind.Break)
        {
            var due = await DokumaBreaks.FindDueAsync(db, session, UtcNow, floorOptions.Value, cancellationToken);
            if (due is null)
            {
                return new Error(ErrorCodes.NoBreakDue, "Şu an planlı molanız yok. Kısa bir ara için 'Ara ver' seçeneğini kullanın.");
            }

            breakScheduleId = due.BreakScheduleId;
        }

        var now = UtcNow;
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var current = session.Pauses.SingleOrDefault(p => p.EndedAt == null);
        if (current is not null)
        {
            current.EndedAt = now;
            await db.SaveChangesAsync(cancellationToken);
        }

        session.Pauses.Add(new DokumaSessionPause
        {
            Kind = request.Kind,
            ReasonId = request.Kind == DokumaPauseKind.Pause ? request.ReasonId : null,
            BreakScheduleId = breakScheduleId,
            Note = NullIfBlank(request.Note),
            StartedAt = now,
        });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetContextAsync(cancellationToken);
    }

    public async Task<Result<DokumaTabletContextDto>> ResumeAsync(DokumaTabletResumeRequest request, CancellationToken cancellationToken)
    {
        var session = await OpenSessionAsync(cancellationToken);
        if (session is null)
        {
            return NoSession;
        }

        var current = session.Pauses.SingleOrDefault(p => p.EndedAt == null);

        // Aradan dönüş de ara gibi kart ister; moladan dönüş serbesttir.
        if (current?.Kind == DokumaPauseKind.Pause)
        {
            var actor = await FloorActorAsync(DokumaFloorAction.Pause, session.OperatorId, request.AuthorizerUid, cancellationToken);
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

    public async Task<Result<DokumaTabletRollDto>> CutRollAsync(int beamId, DokumaTabletRollRequest request, CancellationToken cancellationToken)
    {
        // Yeniden gönderilen kesim zaten kayıtlıysa sonucu döner; oturum sonradan kapanmış olabilir.
        var duplicate = await db.Set<DokumaFabricRoll>().AsNoTracking()
            .Where(r => r.ClientRequestId == request.RequestId)
            .Select(r => new DokumaTabletRollDto(r.RollNo, r.Meters))
            .FirstOrDefaultAsync(cancellationToken);
        if (duplicate is not null)
        {
            return duplicate;
        }

        var person = await CurrentOperatorAsync(cancellationToken);
        if (person is null)
        {
            return NoSession;
        }

        if (await IsPausedAsync(cancellationToken))
        {
            return Paused;
        }

        var loomId = await db.Set<DokumaBeam>().AsNoTracking().Where(b => b.Id == beamId).Select(b => b.LoomId).FirstOrDefaultAsync(cancellationToken);
        if (loomId is { } mounted)
        {
            if (!await ServesLoomAsync(mounted, cancellationToken))
            {
                return OtherLoom;
            }

            if (await DokumaOperatorCardService.CheckLoomAsync(db, person.Id, mounted, cancellationToken) is { } notQualified)
            {
                return notQualified;
            }
        }

        await EndWaitingAsync(cancellationToken);
        return await weaving.CutRollFromTabletAsync(beamId, request, person.Id, cancellationToken);
    }

    /// <summary>Üretim ve planlı duruşu oturumdaki personel bildirir; arıza kartla bildirilir.</summary>
    public async Task<Result<DokumaTabletContextDto>> StartStopAsync(int loomId, DokumaTabletStopRequest request, CancellationToken cancellationToken)
    {
        var person = await CurrentOperatorAsync(cancellationToken);
        if (person is null)
        {
            return NoSession;
        }

        if (await IsPausedAsync(cancellationToken))
        {
            return Paused;
        }

        if (!await ServesLoomAsync(loomId, cancellationToken))
        {
            return OtherLoom;
        }

        if (await DokumaOperatorCardService.CheckLoomAsync(db, person.Id, loomId, cancellationToken) is { } notQualified)
        {
            return notQualified;
        }

        var category = await db.Set<DokumaStopReason>().AsNoTracking()
            .Where(r => r.Id == request.ReasonId && r.IsActive)
            .Select(r => (DokumaStopCategory?)r.Category)
            .FirstOrDefaultAsync(cancellationToken);
        var startedBy = person.Name;
        if (category == DokumaStopCategory.Breakdown)
        {
            var actor = await FloorActorAsync(DokumaFloorAction.ReportBreakdown, person.Id, request.AuthorizerUid, cancellationToken);
            if (!actor.IsSuccess)
            {
                return actor.Error!;
            }

            startedBy = actor.Value;
        }

        await EndWaitingAsync(cancellationToken);
        var started = await stops.StartAsync(loomId, new StartDokumaLoomStopRequest(request.ReasonId, request.Note), startedBy, cancellationToken);
        return started.IsSuccess ? await GetContextAsync(cancellationToken) : started.Error!;
    }

    /// <summary>Üretim ve planlı duruşu oturumdaki personel, arızayı yalnızca vardiya amirinin ya da bakımcının kartı kapatır.</summary>
    public async Task<Result<DokumaTabletContextDto>> EndStopAsync(int loomId, DokumaTabletEndStopRequest request, CancellationToken cancellationToken)
    {
        var person = await CurrentOperatorAsync(cancellationToken);
        if (person is null)
        {
            return NoSession;
        }

        if (!await ServesLoomAsync(loomId, cancellationToken))
        {
            return OtherLoom;
        }

        var category = await db.Set<DokumaLoomStop>().AsNoTracking()
            .Where(s => s.LoomId == loomId && s.EndedAt == null)
            .Select(s => (DokumaStopCategory?)s.Category)
            .FirstOrDefaultAsync(cancellationToken);
        var endedBy = person.Name;
        if (category == DokumaStopCategory.Breakdown)
        {
            var authorizer = await AuthorizerAsync(DokumaFloorAction.ResolveBreakdown, null, request.AuthorizerUid, cancellationToken);
            if (!authorizer.IsSuccess)
            {
                return authorizer.Error!;
            }

            endedBy = authorizer.Value.Name;
        }
        else if (await IsPausedAsync(cancellationToken))
        {
            return Paused;
        }

        var ended = await stops.EndAsync(loomId, endedBy, cancellationToken);
        return ended.IsSuccess ? await GetContextAsync(cancellationToken) : ended.Error!;
    }

    private Task<DokumaTabletOperatorDto?> CurrentOperatorAsync(CancellationToken cancellationToken) =>
        db.Set<DokumaTabletSession>().AsNoTracking()
            .Where(s => s.TabletId == currentUser.TabletId && s.EndedAt == null)
            .Select(s => new DokumaTabletOperatorDto(s.Operator.Id, s.Operator.Code, s.Operator.Name, s.Operator.Role, s.StartedAt))
            .FirstOrDefaultAsync(cancellationToken);

    private Task<DokumaTabletSession?> OpenSessionAsync(CancellationToken cancellationToken) =>
        db.Set<DokumaTabletSession>().Include(s => s.Pauses).SingleOrDefaultAsync(s => s.TabletId == currentUser.TabletId && s.EndedAt == null, cancellationToken);

    private Task<bool> IsPausedAsync(CancellationToken cancellationToken) =>
        db.Set<DokumaSessionPause>().AnyAsync(p => p.EndedAt == null && p.Kind != DokumaPauseKind.WaitingForBeam && p.Session.TabletId == currentUser.TabletId && p.Session.EndedAt == null, cancellationToken);

    /// <summary>Üretim hareketi operatörün işe döndüğünü gösterir: levent bekleme biter.</summary>
    private Task EndWaitingAsync(CancellationToken cancellationToken)
    {
        var now = UtcNow;
        return db.Set<DokumaSessionPause>()
            .Where(p => p.EndedAt == null && p.Kind == DokumaPauseKind.WaitingForBeam && p.Session.TabletId == currentUser.TabletId && p.Session.EndedAt == null)
            .ExecuteUpdateAsync(update => update.SetProperty(p => p.EndedAt, now), cancellationToken);
    }

    /// <summary>İşlemi onaylayan: kart her zaman okutulur; okutulmadıysa kart istenir.</summary>
    private async Task<Result<string>> FloorActorAsync(DokumaFloorAction action, int sessionOperatorId, string? authorizerUid, CancellationToken cancellationToken)
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
    private async Task<Result<DokumaOperator>> AuthorizerAsync(DokumaFloorAction action, int? sessionOperatorId, string? authorizerUid, CancellationToken cancellationToken)
    {
        var uid = RfidUid.Normalize(authorizerUid);
        if (uid is null || await DokumaCards.CheckAsync(db, uid, cancellationToken) is not null)
        {
            return new Error(ErrorCodes.NotAuthorizedForAction, "Bu kart yetkili personel kartı değil.", ErrorType.Forbidden);
        }

        var person = await db.Set<DokumaOperator>().AsNoTracking().SingleOrDefaultAsync(o => o.CardUid == uid && o.IsActive, cancellationToken);
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

    private static string WhoCan(DokumaFloorAction action) =>
        action switch
        {
            DokumaFloorAction.Pause => "yetkili operatör ya da vardiya amiri",
            DokumaFloorAction.ReportBreakdown => "yetkili operatör, vardiya amiri ya da bakım personeli",
            _ => "vardiya amiri ya da bakım personeli",
        };

    /// <summary>Salon tableti her tezgâha, bağlı tablet yalnızca kendi tezgâhına işlem yapar.</summary>
    private Task<bool> ServesLoomAsync(int loomId, CancellationToken cancellationToken) =>
        db.Set<DokumaTablet>().AnyAsync(t => t.Id == currentUser.TabletId && t.IsActive && (t.LoomId == null || t.LoomId == loomId), cancellationToken);

    private Task<int> TouchAsync(int tabletId, CancellationToken cancellationToken)
    {
        var now = UtcNow;
        return db.Set<DokumaTablet>()
            .Where(t => t.Id == tabletId && t.IsActive)
            .ExecuteUpdateAsync(update => update.SetProperty(t => t.LastSeenAt, now), cancellationToken);
    }
}
