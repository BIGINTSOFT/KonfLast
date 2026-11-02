using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Core.Domain.Common;
using ERP.Orme.Application.Features.Knitting;
using ERP.Orme.Domain.Entities;
using ERP.Orme.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using static ERP.Core.Application.Features.MasterData.MasterDataRules;

namespace ERP.Orme.Application.Features.Floor;

public sealed record OrmeTabletTokenRequest(string TabletCode, string DeviceKey, string DeviceIdentifier);

public sealed class OrmeTabletTokenRequestValidator : AbstractValidator<OrmeTabletTokenRequest>
{
    public OrmeTabletTokenRequestValidator()
    {
        RuleFor(x => x.TabletCode).NotEmpty().WithMessage("Tablet kodu zorunludur.").MaximumLength(30);
        RuleFor(x => x.DeviceKey).NotEmpty().WithMessage("Cihaz anahtarı zorunludur.");
        RuleFor(x => x.DeviceIdentifier).NotEmpty().WithMessage("Cihaz kimliği zorunludur.");
    }
}

public sealed record OrmeTabletTokenResponse(string AccessToken, DateTime AccessTokenExpiresAt);

public sealed record OrmeTabletOperatorDto(int Id, string Code, string Name, OrmeOperatorRole Role, DateTime LoginAt);

public sealed record OrmeTabletPauseDto(OrmePauseKind Kind, string? ReasonName, string? BreakName, DateTime StartedAt);

public sealed record OrmeTabletReasonDto(int Id, string Name, OrmeStopCategory Category);

/// <param name="MachineId">Tabletin bağlı olduğu makine; boşsa salon tabletidir.</param>
/// <param name="Pause">Oturumdaki açık mola ya da ara.</param>
/// <param name="Machines">Tabletin gösterdiği makineler ve üzerlerindeki emir.</param>
/// <param name="PauseReasons">Personelin "Ara ver" nedenleri.</param>
/// <param name="StopReasons">Makine duruş ve arıza nedenleri.</param>
public sealed record OrmeTabletContextDto(
    int TabletId,
    string TabletCode,
    DateTime ServerTime,
    int? MachineId,
    OrmeTabletOperatorDto? Operator,
    OrmeTabletPauseDto? Pause,
    IReadOnlyList<OrmeMachineBoardItemDto> Machines,
    IReadOnlyList<OrmeTabletReasonDto> PauseReasons,
    IReadOnlyList<OrmeTabletReasonDto> StopReasons,
    OrmeDueBreakDto? DueBreak,
    int IdlePromptMinutes);

/// <param name="Kind">Mola, ara ya da emir bekleme.</param>
/// <param name="ReasonId">Arada zorunludur.</param>
/// <param name="AuthorizerUid">Ara için okutulan kart: kişiye özel yetkili oturum sahibi ya da vardiya amiri.</param>
public sealed record OrmeTabletPauseRequest(OrmePauseKind Kind, int? ReasonId = null, string? Note = null, string? AuthorizerUid = null);

public sealed class OrmeTabletPauseRequestValidator : AbstractValidator<OrmeTabletPauseRequest>
{
    public OrmeTabletPauseRequestValidator()
    {
        RuleFor(x => x.Kind).IsInEnum().WithMessage("Mola, ara ya da emir bekleme seçilmelidir.");
        RuleFor(x => x.ReasonId).NotNull().WithMessage("Ara nedeni seçilmelidir.").When(x => x.Kind == OrmePauseKind.Pause);
        RuleFor(x => x.Note).OptionalText(500);
    }
}

/// <param name="AuthorizerUid">Aradan dönüşte okutulan kart; moladan dönüşte gerekmez.</param>
public sealed record OrmeTabletResumeRequest(string? AuthorizerUid = null);

/// <param name="AuthorizerUid">Arıza için okutulan kart: kişiye özel yetkili oturum sahibi, vardiya amiri ya da bakım. Diğer duruşlarda boş.</param>
public sealed record OrmeTabletStopRequest(int ReasonId, string? Note = null, string? AuthorizerUid = null);

public sealed class OrmeTabletStopRequestValidator : AbstractValidator<OrmeTabletStopRequest>
{
    public OrmeTabletStopRequestValidator()
    {
        RuleFor(x => x.ReasonId).GreaterThan(0).WithMessage("Neden seçilmelidir.");
        RuleFor(x => x.Note).OptionalText(500);
    }
}

public sealed record OrmeTabletLoginRequest(string Uid);

/// <param name="RequestId">Bağlantı kesilince yeniden gönderilen kayıt ikinci top açmasın diye.</param>
/// <param name="MachineId">Topun çıktığı makine.</param>
/// <param name="CutOn">Topun çıktığı gün; kayıt sonradan gönderilebilir.</param>
/// <param name="Kilograms">Makine başında tartılan kilo; zorunludur.</param>
/// <param name="Meters">Ölçüldüyse metre.</param>
public sealed record OrmeTabletRollRequest(Guid RequestId, int MachineId, DateOnly CutOn, decimal Kilograms, decimal? Meters = null, string? Notes = null);

public sealed class OrmeTabletRollRequestValidator : AbstractValidator<OrmeTabletRollRequest>
{
    public OrmeTabletRollRequestValidator()
    {
        RuleFor(x => x.RequestId).NotEmpty().WithMessage("İşlem kimliği zorunludur.");
        RuleFor(x => x.MachineId).GreaterThan(0).WithMessage("Makine seçilmelidir.");
        RuleFor(x => x.CutOn).NotEmpty().WithMessage("Tarih zorunludur.");
        RuleFor(x => x.Kilograms)
            .GreaterThan(0).WithMessage("Topun kilosu tartılıp girilmelidir.")
            .LessThanOrEqualTo(99_999).WithMessage("Kilo çok büyük.")
            .PrecisionScale(12, 2, true).WithMessage("Kilo en fazla iki ondalık olabilir.");
        RuleFor(x => x.Meters)
            .GreaterThan(0).WithMessage("Metre sıfırdan büyük olmalıdır.")
            .PrecisionScale(12, 2, true).WithMessage("Metre en fazla iki ondalık olabilir.")
            .When(x => x.Meters is not null);
        RuleFor(x => x.Notes).OptionalText(500);
    }
}

public sealed record OrmeTabletRollDto(string RollNo, decimal Kilograms);

/// <param name="AuthorizerUid">Arızayı kapatan vardiya amirinin ya da bakımcının okutulan kartı; diğer duruşlarda boş.</param>
public sealed record OrmeTabletEndStopRequest(string? AuthorizerUid = null);

public interface IOrmeTabletDeviceService
{
    Task<Result> ResetDeviceAsync(int tabletId, CancellationToken cancellationToken);
    Task<Result<IReadOnlyList<TabletPairingStationDto>>> GetPairingStationsAsync(TabletPairingListRequest request, CancellationToken cancellationToken);
    Task<Result<PairTabletResponse>> PairAsync(PairTabletRequest request, CancellationToken cancellationToken);
    Task<Result<OrmeTabletTokenResponse>> IssueTokenAsync(OrmeTabletTokenRequest request, CancellationToken cancellationToken);
    Task<Result<OrmeTabletContextDto>> GetContextAsync(CancellationToken cancellationToken);
    Task<Result> HeartbeatAsync(CancellationToken cancellationToken);
    Task<Result<OrmeTabletContextDto>> LoginAsync(OrmeTabletLoginRequest request, CancellationToken cancellationToken);
    Task<Result<OrmeTabletContextDto>> LogoutAsync(CancellationToken cancellationToken);
    Task<Result<OrmeTabletContextDto>> PauseAsync(OrmeTabletPauseRequest request, CancellationToken cancellationToken);
    Task<Result<OrmeTabletContextDto>> ResumeAsync(OrmeTabletResumeRequest request, CancellationToken cancellationToken);
    Task<Result<OrmeTabletRollDto>> CutRollAsync(int knitOrderId, OrmeTabletRollRequest request, CancellationToken cancellationToken);
    Task<Result<OrmeTabletContextDto>> StartStopAsync(int machineId, OrmeTabletStopRequest request, CancellationToken cancellationToken);
    Task<Result<OrmeTabletContextDto>> EndStopAsync(int machineId, OrmeTabletEndStopRequest request, CancellationToken cancellationToken);
}

public sealed class OrmeTabletDeviceService(
    IAppDbContext db,
    ITokenService tokenService,
    IOrmeKnitOrderService knitting,
    IOrmeMachineStopService stops,
    IOptions<TabletPairingOptions> pairingOptions,
    IOptions<FloorSessionOptions> floorOptions,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : IOrmeTabletDeviceService
{
    private const string ModuleKey = "Orme";

    private static readonly Error NotFound = new(ErrorCodes.NotFound, "Tablet bulunamadı.", ErrorType.NotFound);
    private static readonly Error InvalidCredentials = new(
        ErrorCodes.InvalidDeviceCredentials, "Cihaz kimlik bilgileri geçersiz. Tabletin yeniden eşleştirilmesi gerekebilir.", ErrorType.Unauthorized);
    private static readonly Error NoSession = new("SESSION_REQUIRED", "Önce personel kartınızı okutun.", ErrorType.Conflict);
    private static readonly Error OtherMachine = new("MACHINE_NOT_ON_TABLET", "Bu tablet o makineye bağlı değil.", ErrorType.Forbidden);
    private static readonly Error Paused = new("SESSION_PAUSED", "Oturum duraklatılmış; önce işe dönün.", ErrorType.Conflict);

    private DateTime UtcNow => timeProvider.GetUtcNow().UtcDateTime;

    public async Task<Result> ResetDeviceAsync(int tabletId, CancellationToken cancellationToken)
    {
        var tablet = await db.Set<OrmeTablet>().SingleOrDefaultAsync(t => t.Id == tabletId, cancellationToken);
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
        var tablets = await db.Set<OrmeTablet>().AsNoTracking()
            .Select(t => new { t.Id, t.TabletCode, t.Name, t.MachineId, t.IsActive, t.DeviceKeyHash, t.DeviceIdentifier })
            .ToListAsync(cancellationToken);
        var machines = await db.Set<OrmeMachine>().AsNoTracking()
            .Where(l => l.IsActive)
            .OrderBy(l => l.Code)
            .Select(l => new { l.Id, l.Code, l.Name })
            .ToListAsync(cancellationToken);

        var stations = machines.Select(l =>
        {
            var tablet = tablets.SingleOrDefault(t => t.MachineId == l.Id);
            return new TabletPairingStationDto(
                l.Id, null, l.Code, l.Name, null, tablet?.TabletCode,
                tablet?.DeviceKeyHash is not null,
                tablet?.DeviceKeyHash is not null && tablet.DeviceIdentifier == deviceIdentifier);
        });

        // Makinesız tablet bütün makineleri gösterir.
        var halls = tablets
            .Where(t => t.MachineId is null && t.IsActive)
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

        OrmeMachine? machine = null;
        OrmeTablet? tablet;
        if (request.MachineId is { } machineId)
        {
            machine = await db.Set<OrmeMachine>().AsNoTracking().SingleOrDefaultAsync(l => l.Id == machineId && l.IsActive, cancellationToken);
            if (machine is null)
            {
                return TabletPairing.StationNotFound;
            }

            tablet = await db.Set<OrmeTablet>().SingleOrDefaultAsync(t => t.MachineId == machineId, cancellationToken);
        }
        else
        {
            tablet = await db.Set<OrmeTablet>().SingleOrDefaultAsync(t => t.Id == request.TabletId && t.MachineId == null, cancellationToken);
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
        var previous = await db.Set<OrmeTablet>().Where(t => t.DeviceIdentifier == deviceIdentifier).ToListAsync(cancellationToken);
        previous.ForEach(t => t.ResetDevice());
        tablet?.ResetDevice();
        await db.SaveChangesAsync(cancellationToken);

        if (tablet is null)
        {
            tablet = new OrmeTablet { TabletCode = TabletPairing.NewTabletCode("ORM", machine!.Code, machine.Id), MachineId = machine.Id };
            db.Set<OrmeTablet>().Add(tablet);
        }

        var deviceKey = Secrets.NewDeviceKey();
        tablet.Enroll(deviceIdentifier, Secrets.Hash(deviceKey));
        tablet.LastSeenAt = UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return new PairTabletResponse(tablet.Id, tablet.TabletCode, deviceKey, stationCode, stationName);
    }

    public async Task<Result<OrmeTabletTokenResponse>> IssueTokenAsync(OrmeTabletTokenRequest request, CancellationToken cancellationToken)
    {
        var code = NormalizeCode(request.TabletCode);
        var tablet = await db.Set<OrmeTablet>().AsNoTracking().SingleOrDefaultAsync(t => t.TabletCode == code, cancellationToken);

        // Anahtar her durumda karşılaştırılır: tabletin var olup olmadığı yanıt süresinden anlaşılmasın.
        var keyMatches = Secrets.Matches(tablet?.DeviceKeyHash ?? Secrets.Hash(string.Empty), request.DeviceKey);
        if (tablet is null || !tablet.IsActive || !keyMatches || tablet.DeviceIdentifier != request.DeviceIdentifier.Trim())
        {
            return InvalidCredentials;
        }

        await TouchAsync(tablet.Id, cancellationToken);
        var token = tokenService.CreateTabletAccessToken(tablet.Id, tablet.TabletCode, ModuleKey);
        return new OrmeTabletTokenResponse(token.Token, token.ExpiresAt);
    }

    public async Task<Result<OrmeTabletContextDto>> GetContextAsync(CancellationToken cancellationToken)
    {
        var tablet = await db.Set<OrmeTablet>().AsNoTracking().SingleOrDefaultAsync(t => t.Id == currentUser.TabletId && t.IsActive, cancellationToken);
        if (tablet is null)
        {
            return InvalidCredentials;
        }

        var person = await db.Set<OrmeTabletSession>().AsNoTracking()
            .Where(s => s.TabletId == tablet.Id && s.EndedAt == null)
            .Select(s => new OrmeTabletOperatorDto(s.Operator.Id, s.Operator.Code, s.Operator.Name, s.Operator.Role, s.StartedAt))
            .FirstOrDefaultAsync(cancellationToken);
        var pause = await db.Set<OrmeSessionPause>().AsNoTracking()
            .Where(p => p.EndedAt == null && p.Session.TabletId == tablet.Id && p.Session.EndedAt == null)
            .Select(p => new OrmeTabletPauseDto(p.Kind, p.Reason != null ? p.Reason.Name : p.Note, p.BreakSchedule != null ? p.BreakSchedule.Name : null, p.StartedAt))
            .FirstOrDefaultAsync(cancellationToken);
        var board = await knitting.GetBoardAsync(cancellationToken);
        var reasons = await db.Set<OrmeStopReason>().AsNoTracking()
            .Where(r => r.IsActive)
            .OrderBy(r => r.SortOrder).ThenBy(r => r.Name)
            .Select(r => new OrmeTabletReasonDto(r.Id, r.Name, r.Category))
            .ToListAsync(cancellationToken);

        // Moladaki operatöre yeni mola hatırlatılmaz.
        var openSession = await db.Set<OrmeTabletSession>().AsNoTracking().FirstOrDefaultAsync(s => s.TabletId == tablet.Id && s.EndedAt == null, cancellationToken);
        var dueBreak = openSession is null || pause?.Kind == OrmePauseKind.Break
            ? null
            : await OrmeBreaks.FindDueAsync(db, openSession, UtcNow, floorOptions.Value, cancellationToken);

        return new OrmeTabletContextDto(
            tablet.Id,
            tablet.TabletCode,
            UtcNow,
            tablet.MachineId,
            person,
            pause,
            tablet.MachineId is { } machineId ? board.Where(l => l.MachineId == machineId).ToList() : board,
            reasons.Where(r => r.Category == OrmeStopCategory.Pause).ToList(),
            reasons.Where(r => r.Category != OrmeStopCategory.Pause).ToList(),
            dueBreak,
            Math.Max(floorOptions.Value.IdlePromptMinutes, 0));
    }

    public async Task<Result> HeartbeatAsync(CancellationToken cancellationToken) =>
        currentUser.TabletId is { } tabletId && await TouchAsync(tabletId, cancellationToken) > 0 ? Result.Success() : InvalidCredentials;

    /// <summary>Makine personele bağlı değildir: yeni kart tabletteki oturumu devralır, personelin başka tabletteki oturumu kapanır.</summary>
    public async Task<Result<OrmeTabletContextDto>> LoginAsync(OrmeTabletLoginRequest request, CancellationToken cancellationToken)
    {
        if (RfidUid.Normalize(request.Uid) is not { } uid)
        {
            return new Error("CARD_NOT_RECOGNIZED", "Personel kartı okunamadı.", ErrorType.Conflict);
        }

        if (await OrmeCards.CheckAsync(db, uid, cancellationToken) is { } cardError)
        {
            return cardError;
        }

        var person = await db.Set<OrmeOperator>().AsNoTracking().SingleOrDefaultAsync(o => o.CardUid == uid, cancellationToken);
        if (person is null || !person.IsActive)
        {
            return new Error("CARD_NOT_RECOGNIZED", "Bu kart aktif bir personele atanmamış.", ErrorType.Conflict);
        }

        var tabletId = currentUser.TabletId!.Value;
        var open = await db.Set<OrmeTabletSession>()
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
            OrmeSessions.Close(session, session.TabletId == tabletId ? OrmeSessionEndReason.TakenOver : OrmeSessionEndReason.MovedAway, now);
        }

        await db.SaveChangesAsync(cancellationToken);
        db.Set<OrmeTabletSession>().Add(new OrmeTabletSession { TabletId = tabletId, OperatorId = person.Id, StartedAt = now });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetContextAsync(cancellationToken);
    }

    public async Task<Result<OrmeTabletContextDto>> LogoutAsync(CancellationToken cancellationToken)
    {
        var session = await OpenSessionAsync(cancellationToken);
        if (session is not null)
        {
            OrmeSessions.Close(session, OrmeSessionEndReason.Logout, UtcNow);
            await db.SaveChangesAsync(cancellationToken);
        }

        return await GetContextAsync(cancellationToken);
    }

    public async Task<Result<OrmeTabletContextDto>> PauseAsync(OrmeTabletPauseRequest request, CancellationToken cancellationToken)
    {
        var session = await OpenSessionAsync(cancellationToken);
        if (session is null)
        {
            return NoSession;
        }

        if (request.Kind == OrmePauseKind.Pause)
        {
            if (!await db.Set<OrmeStopReason>().AnyAsync(r => r.Id == request.ReasonId && r.IsActive && r.Category == OrmeStopCategory.Pause, cancellationToken))
            {
                return new Error("REASON_NOT_FOUND", "Seçilen ara nedeni bulunamadı.", ErrorType.Conflict);
            }

            // Ara kartla verilir (mola değil): kişiye özel yetkili oturum sahibi ya da görevle yetkili kişi.
            var actor = await FloorActorAsync(OrmeFloorAction.Pause, session.OperatorId, request.AuthorizerUid, cancellationToken);
            if (!actor.IsSuccess)
            {
                return actor.Error!;
            }
        }

        int? breakScheduleId = null;
        if (request.Kind == OrmePauseKind.Break)
        {
            var due = await OrmeBreaks.FindDueAsync(db, session, UtcNow, floorOptions.Value, cancellationToken);
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

        session.Pauses.Add(new OrmeSessionPause
        {
            Kind = request.Kind,
            ReasonId = request.Kind == OrmePauseKind.Pause ? request.ReasonId : null,
            BreakScheduleId = breakScheduleId,
            Note = NullIfBlank(request.Note),
            StartedAt = now,
        });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetContextAsync(cancellationToken);
    }

    public async Task<Result<OrmeTabletContextDto>> ResumeAsync(OrmeTabletResumeRequest request, CancellationToken cancellationToken)
    {
        var session = await OpenSessionAsync(cancellationToken);
        if (session is null)
        {
            return NoSession;
        }

        var current = session.Pauses.SingleOrDefault(p => p.EndedAt == null);

        // Aradan dönüş de ara gibi kart ister; moladan dönüş serbesttir.
        if (current?.Kind == OrmePauseKind.Pause)
        {
            var actor = await FloorActorAsync(OrmeFloorAction.Pause, session.OperatorId, request.AuthorizerUid, cancellationToken);
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

    public async Task<Result<OrmeTabletRollDto>> CutRollAsync(int knitOrderId, OrmeTabletRollRequest request, CancellationToken cancellationToken)
    {
        // Yeniden gönderilen kayıt zaten alınmışsa sonucu döner; oturum sonradan kapanmış olabilir.
        var duplicate = await db.Set<OrmeFabricRoll>().AsNoTracking()
            .Where(r => r.ClientRequestId == request.RequestId)
            .Select(r => new OrmeTabletRollDto(r.RollNo, r.Kilograms))
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

        if (!await ServesMachineAsync(request.MachineId, cancellationToken))
        {
            return OtherMachine;
        }

        if (await OrmeOperatorCardService.CheckMachineAsync(db, person.Id, request.MachineId, cancellationToken) is { } notQualified)
        {
            return notQualified;
        }

        await EndWaitingAsync(cancellationToken);
        return await knitting.AddRollFromTabletAsync(knitOrderId, request, person.Id, cancellationToken);
    }

    /// <summary>Üretim ve planlı duruşu oturumdaki personel bildirir; arıza kartla bildirilir.</summary>
    public async Task<Result<OrmeTabletContextDto>> StartStopAsync(int machineId, OrmeTabletStopRequest request, CancellationToken cancellationToken)
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

        if (!await ServesMachineAsync(machineId, cancellationToken))
        {
            return OtherMachine;
        }

        if (await OrmeOperatorCardService.CheckMachineAsync(db, person.Id, machineId, cancellationToken) is { } notQualified)
        {
            return notQualified;
        }

        var category = await db.Set<OrmeStopReason>().AsNoTracking()
            .Where(r => r.Id == request.ReasonId && r.IsActive)
            .Select(r => (OrmeStopCategory?)r.Category)
            .FirstOrDefaultAsync(cancellationToken);
        var startedBy = person.Name;
        if (category == OrmeStopCategory.Breakdown)
        {
            var actor = await FloorActorAsync(OrmeFloorAction.ReportBreakdown, person.Id, request.AuthorizerUid, cancellationToken);
            if (!actor.IsSuccess)
            {
                return actor.Error!;
            }

            startedBy = actor.Value;
        }

        await EndWaitingAsync(cancellationToken);
        var started = await stops.StartAsync(machineId, new StartOrmeMachineStopRequest(request.ReasonId, request.Note), startedBy, cancellationToken);
        return started.IsSuccess ? await GetContextAsync(cancellationToken) : started.Error!;
    }

    /// <summary>Üretim ve planlı duruşu oturumdaki personel, arızayı yalnızca vardiya amirinin ya da bakımcının kartı kapatır.</summary>
    public async Task<Result<OrmeTabletContextDto>> EndStopAsync(int machineId, OrmeTabletEndStopRequest request, CancellationToken cancellationToken)
    {
        var person = await CurrentOperatorAsync(cancellationToken);
        if (person is null)
        {
            return NoSession;
        }

        if (!await ServesMachineAsync(machineId, cancellationToken))
        {
            return OtherMachine;
        }

        var category = await db.Set<OrmeMachineStop>().AsNoTracking()
            .Where(s => s.MachineId == machineId && s.EndedAt == null)
            .Select(s => (OrmeStopCategory?)s.Category)
            .FirstOrDefaultAsync(cancellationToken);
        var endedBy = person.Name;
        if (category == OrmeStopCategory.Breakdown)
        {
            var authorizer = await AuthorizerAsync(OrmeFloorAction.ResolveBreakdown, null, request.AuthorizerUid, cancellationToken);
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

        var ended = await stops.EndAsync(machineId, endedBy, cancellationToken);
        return ended.IsSuccess ? await GetContextAsync(cancellationToken) : ended.Error!;
    }

    private Task<OrmeTabletOperatorDto?> CurrentOperatorAsync(CancellationToken cancellationToken) =>
        db.Set<OrmeTabletSession>().AsNoTracking()
            .Where(s => s.TabletId == currentUser.TabletId && s.EndedAt == null)
            .Select(s => new OrmeTabletOperatorDto(s.Operator.Id, s.Operator.Code, s.Operator.Name, s.Operator.Role, s.StartedAt))
            .FirstOrDefaultAsync(cancellationToken);

    private Task<OrmeTabletSession?> OpenSessionAsync(CancellationToken cancellationToken) =>
        db.Set<OrmeTabletSession>().Include(s => s.Pauses).SingleOrDefaultAsync(s => s.TabletId == currentUser.TabletId && s.EndedAt == null, cancellationToken);

    private Task<bool> IsPausedAsync(CancellationToken cancellationToken) =>
        db.Set<OrmeSessionPause>().AnyAsync(p => p.EndedAt == null && p.Kind != OrmePauseKind.WaitingForOrder && p.Session.TabletId == currentUser.TabletId && p.Session.EndedAt == null, cancellationToken);

    /// <summary>Üretim hareketi operatörün işe döndüğünü gösterir: emir bekleme biter.</summary>
    private Task EndWaitingAsync(CancellationToken cancellationToken)
    {
        var now = UtcNow;
        return db.Set<OrmeSessionPause>()
            .Where(p => p.EndedAt == null && p.Kind == OrmePauseKind.WaitingForOrder && p.Session.TabletId == currentUser.TabletId && p.Session.EndedAt == null)
            .ExecuteUpdateAsync(update => update.SetProperty(p => p.EndedAt, now), cancellationToken);
    }

    /// <summary>İşlemi onaylayan: kart her zaman okutulur; okutulmadıysa kart istenir.</summary>
    private async Task<Result<string>> FloorActorAsync(OrmeFloorAction action, int sessionOperatorId, string? authorizerUid, CancellationToken cancellationToken)
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
    private async Task<Result<OrmeOperator>> AuthorizerAsync(OrmeFloorAction action, int? sessionOperatorId, string? authorizerUid, CancellationToken cancellationToken)
    {
        var uid = RfidUid.Normalize(authorizerUid);
        if (uid is null || await OrmeCards.CheckAsync(db, uid, cancellationToken) is not null)
        {
            return new Error(ErrorCodes.NotAuthorizedForAction, "Bu kart yetkili personel kartı değil.", ErrorType.Forbidden);
        }

        var person = await db.Set<OrmeOperator>().AsNoTracking().SingleOrDefaultAsync(o => o.CardUid == uid && o.IsActive, cancellationToken);
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

    private static string WhoCan(OrmeFloorAction action) =>
        action switch
        {
            OrmeFloorAction.Pause => "yetkili operatör ya da vardiya amiri",
            OrmeFloorAction.ReportBreakdown => "yetkili operatör, vardiya amiri ya da bakım personeli",
            _ => "vardiya amiri ya da bakım personeli",
        };

    /// <summary>Salon tableti her makineye, bağlı tablet yalnızca kendi makinenina işlem yapar.</summary>
    private Task<bool> ServesMachineAsync(int machineId, CancellationToken cancellationToken) =>
        db.Set<OrmeTablet>().AnyAsync(t => t.Id == currentUser.TabletId && t.IsActive && (t.MachineId == null || t.MachineId == machineId), cancellationToken);

    private Task<int> TouchAsync(int tabletId, CancellationToken cancellationToken)
    {
        var now = UtcNow;
        return db.Set<OrmeTablet>()
            .Where(t => t.Id == tabletId && t.IsActive)
            .ExecuteUpdateAsync(update => update.SetProperty(t => t.LastSeenAt, now), cancellationToken);
    }
}
