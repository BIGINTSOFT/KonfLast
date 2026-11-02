using System.Linq.Expressions;
using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Core.Application.Features.MasterData;
using ERP.Core.Domain.Common;
using ERP.Dokuma.Domain.Entities;
using ERP.Dokuma.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using static ERP.Core.Application.Features.MasterData.MasterDataRules;

namespace ERP.Dokuma.Application.Features.Floor;

public sealed record DokumaOperatorDto(
    int Id, string Code, string FirstName, string LastName, string Name, DokumaOperatorRole Role, string? CardUid, bool CanPause, bool CanReportBreakdown, bool IsActive, int? BreakGroupId, string? BreakGroupName);

public sealed record DokumaOperatorRequest(
    string Code, string FirstName, string LastName, DokumaOperatorRole Role = DokumaOperatorRole.Weaver, string? CardUid = null, bool CanPause = false, bool CanReportBreakdown = false, bool IsActive = true, int? BreakGroupId = null);

public sealed class DokumaOperatorRequestValidator : AbstractValidator<DokumaOperatorRequest>
{
    public DokumaOperatorRequestValidator()
    {
        RuleFor(x => x.Code).ValidCode();
        RuleFor(x => x.FirstName).ValidName(100);
        RuleFor(x => x.LastName).ValidName(100);
        RuleFor(x => x.Role).IsInEnum().WithMessage("Geçerli bir görev seçin.");
        RuleFor(x => x.CardUid)
            .Must(uid => RfidUid.Normalize(uid) is not null).WithMessage("Kart numarası 1-10 rakam olmalıdır.")
            .When(x => !string.IsNullOrWhiteSpace(x.CardUid));
    }
}

public sealed class DokumaOperatorService(IAppDbContext db) : MasterDataService<DokumaOperator, DokumaOperatorDto, DokumaOperatorRequest>(db)
{
    protected override DbSet<DokumaOperator> Set => Db.Set<DokumaOperator>();
    protected override string Label => "Operatör";
    protected override string CodeField => "code";

    protected override string CodeOf(DokumaOperatorRequest request) => request.Code;
    protected override Expression<Func<DokumaOperator, bool>> WithCode(string code) => e => e.Code == code;
    protected override Expression<Func<DokumaOperator, bool>> Matches(string term) => e => e.Code.Contains(term) || e.Name.Contains(term) || e.CardUid!.Contains(term);
    protected override IQueryable<DokumaOperator> Order(IQueryable<DokumaOperator> query) => query.OrderBy(e => e.Code);
    protected override Expression<Func<DokumaOperator, DokumaOperatorDto>> ToDto => e => new DokumaOperatorDto(e.Id, e.Code, e.FirstName, e.LastName, e.Name, e.Role, e.CardUid, e.CanPause, e.CanReportBreakdown, e.IsActive, e.BreakGroupId, e.BreakGroup != null ? e.BreakGroup.Name : null);
    protected override Expression<Func<DokumaOperator, LookupDto>> ToLookup => e => new LookupDto(e.Id, e.Code, e.Name);

    protected override void Apply(DokumaOperatorRequest request, DokumaOperator entity, string code)
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

    protected override async Task<Error?> ValidateAsync(DokumaOperatorRequest request, DokumaOperator? existing, CancellationToken cancellationToken)
    {
        if (await ReferenceValidation.RequireSelectableAsync(Db.Set<DokumaBreakGroup>(), request.BreakGroupId, existing?.BreakGroupId, "breakGroupId", "Mola grubu", cancellationToken) is { } groupError)
        {
            return groupError;
        }

        if (RfidUid.Normalize(request.CardUid) is not { } uid)
        {
            return null;
        }

        var existingId = existing?.Id ?? 0;
        var owner = await Db.Set<DokumaOperator>().Where(o => o.CardUid == uid && o.Id != existingId).Select(o => o.Code).FirstOrDefaultAsync(cancellationToken);
        var cardError = await DokumaCards.CheckAsync(Db, uid, cancellationToken);
        if (owner is null && cardError is null)
        {
            return null;
        }

        var message = owner is not null ? $"Bu kart {owner} operatörüne ait." : cardError!.Message;
        return new Error(ErrorCodes.ValidationError, message, ErrorType.Validation, new Dictionary<string, string[]> { ["cardUid"] = [message] });
    }
}

public static class DokumaCards
{
    /// <summary>Kart tanımlı ve aktif mi? Sorun varsa okutana gösterilecek hatayı döner.</summary>
    public static async Task<Error?> CheckAsync(IAppDbContext db, string uid, CancellationToken cancellationToken)
    {
        var card = await db.Set<DokumaRfidCard>().AsNoTracking().Where(c => c.Uid == uid).Select(c => new { c.IsActive }).SingleOrDefaultAsync(cancellationToken);
        if (card is null)
        {
            return new Error("CARD_NOT_REGISTERED", "Kart sistemde tanımlı değil.", ErrorType.Conflict);
        }

        return card.IsActive ? null : new Error("CARD_INACTIVE", "Bu kart pasif.", ErrorType.Conflict);
    }
}

public sealed record DokumaRfidCardDto(int Id, string Uid, string? Label, string? AssignedTo, bool IsActive);

public sealed record DokumaRfidCardRequest(string Uid, string? Label = null, bool IsActive = true);

public sealed class DokumaRfidCardRequestValidator : AbstractValidator<DokumaRfidCardRequest>
{
    public DokumaRfidCardRequestValidator()
    {
        RuleFor(x => x.Uid).Must(uid => RfidUid.Normalize(uid) is not null).WithMessage("Kart numarası 1-10 rakam olmalıdır.");
        RuleFor(x => x.Label).OptionalText(50);
    }
}

public sealed class DokumaRfidCardService(IAppDbContext db) : MasterDataService<DokumaRfidCard, DokumaRfidCardDto, DokumaRfidCardRequest>(db)
{
    protected override DbSet<DokumaRfidCard> Set => Db.Set<DokumaRfidCard>();
    protected override string Label => "RFID kartı";
    protected override string CodeField => "uid";

    protected override string CodeOf(DokumaRfidCardRequest request) => RfidUid.Normalize(request.Uid) ?? request.Uid;
    protected override Expression<Func<DokumaRfidCard, bool>> WithCode(string code) => e => e.Uid == code;
    protected override Expression<Func<DokumaRfidCard, bool>> Matches(string term) => e => e.Uid.Contains(term) || e.Label!.Contains(term);
    protected override IQueryable<DokumaRfidCard> Order(IQueryable<DokumaRfidCard> query) => query.OrderBy(e => e.Label).ThenBy(e => e.Uid);

    protected override Expression<Func<DokumaRfidCard, DokumaRfidCardDto>> ToDto => e => new DokumaRfidCardDto(
        e.Id,
        e.Uid,
        e.Label,
        Db.Set<DokumaOperator>().Where(o => o.CardUid == e.Uid).Select(o => o.Name + " (" + o.Code + ")").FirstOrDefault(),
        e.IsActive);

    protected override Expression<Func<DokumaRfidCard, LookupDto>> ToLookup => e => new LookupDto(e.Id, e.Uid, e.Label ?? e.Uid);

    protected override void Apply(DokumaRfidCardRequest request, DokumaRfidCard entity, string code)
    {
        entity.Uid = code;
        entity.Label = NullIfBlank(request.Label);
        entity.IsActive = request.IsActive;
    }

    protected override async Task<Error?> ValidateAsync(DokumaRfidCardRequest request, DokumaRfidCard? existing, CancellationToken cancellationToken)
    {
        if (existing is null || existing.Uid == RfidUid.Normalize(request.Uid) || !await IsAssignedAsync(existing.Uid, cancellationToken))
        {
            return null;
        }

        const string message = "Atanmış kartın numarası değiştirilemez; önce kartı personelden alın.";
        return new Error(ErrorCodes.ValidationError, message, ErrorType.Validation, new Dictionary<string, string[]> { ["uid"] = [message] });
    }

    protected override async Task<string?> FindUsageAsync(int id, CancellationToken cancellationToken)
    {
        var uid = await Db.Set<DokumaRfidCard>().Where(c => c.Id == id).Select(c => c.Uid).SingleAsync(cancellationToken);
        return await IsAssignedAsync(uid, cancellationToken) ? "kart şu an bir operatöre atanmış" : null;
    }

    private Task<bool> IsAssignedAsync(string uid, CancellationToken cancellationToken) =>
        Db.Set<DokumaOperator>().AnyAsync(o => o.CardUid == uid, cancellationToken);
}

public sealed record DokumaStopReasonDto(int Id, string Code, string Name, DokumaStopCategory Category, int SortOrder, bool IsActive);

public sealed record DokumaStopReasonRequest(string Code, string Name, DokumaStopCategory Category, int SortOrder = 0, bool IsActive = true);

public sealed class DokumaStopReasonRequestValidator : AbstractValidator<DokumaStopReasonRequest>
{
    public DokumaStopReasonRequestValidator()
    {
        RuleFor(x => x.Code).ValidCode();
        RuleFor(x => x.Name).ValidName(100);
        RuleFor(x => x.Category).IsInEnum().WithMessage("Geçerli bir tür seçin.");
        RuleFor(x => x.SortOrder).InclusiveBetween(0, 9999).WithMessage("Sıra 0 ile 9999 arasında olmalıdır.");
    }
}

public sealed class DokumaStopReasonService(IAppDbContext db) : MasterDataService<DokumaStopReason, DokumaStopReasonDto, DokumaStopReasonRequest>(db)
{
    protected override DbSet<DokumaStopReason> Set => Db.Set<DokumaStopReason>();
    protected override string Label => "Duruş nedeni";
    protected override string CodeField => "code";

    protected override string CodeOf(DokumaStopReasonRequest request) => request.Code;
    protected override Expression<Func<DokumaStopReason, bool>> WithCode(string code) => e => e.Code == code;
    protected override Expression<Func<DokumaStopReason, bool>> Matches(string term) => e => e.Code.Contains(term) || e.Name.Contains(term);
    protected override IQueryable<DokumaStopReason> Order(IQueryable<DokumaStopReason> query) => query.OrderBy(e => e.Category).ThenBy(e => e.SortOrder).ThenBy(e => e.Name);
    protected override Expression<Func<DokumaStopReason, DokumaStopReasonDto>> ToDto => e => new DokumaStopReasonDto(e.Id, e.Code, e.Name, e.Category, e.SortOrder, e.IsActive);
    protected override Expression<Func<DokumaStopReason, LookupDto>> ToLookup => e => new LookupDto(e.Id, e.Code, e.Name);

    protected override void Apply(DokumaStopReasonRequest request, DokumaStopReason entity, string code)
    {
        entity.Code = code;
        entity.Name = request.Name.Trim();
        entity.Category = request.Category;
        entity.SortOrder = request.SortOrder;
        entity.IsActive = request.IsActive;
    }

    protected override async Task<string?> FindUsageAsync(int id, CancellationToken cancellationToken) =>
        await Db.Set<DokumaLoomStop>().AnyAsync(s => s.ReasonId == id, cancellationToken) || await Db.Set<DokumaSessionPause>().AnyAsync(p => p.ReasonId == id, cancellationToken)
            ? "tezgâh duruşları ve oturum araları"
            : null;
}

/// <param name="IsOnline">Tabletten son iki dakikada istek geldi mi.</param>
public sealed record DokumaTabletDto(int Id, string TabletCode, string? Name, int? LoomId, string? LoomCode, bool IsEnrolled, bool IsOnline, DateTime? LastSeenAt, bool IsActive);

public sealed record DokumaTabletRequest(string TabletCode, string? Name = null, int? LoomId = null, bool IsActive = true);

public sealed class DokumaTabletRequestValidator : AbstractValidator<DokumaTabletRequest>
{
    public DokumaTabletRequestValidator()
    {
        RuleFor(x => x.TabletCode).ValidCode();
        RuleFor(x => x.Name).OptionalText(150);
    }
}

public sealed class DokumaTabletService(IAppDbContext db, TimeProvider timeProvider) : MasterDataService<DokumaTablet, DokumaTabletDto, DokumaTabletRequest>(db)
{
    private const int OnlineSeconds = 120;

    protected override DbSet<DokumaTablet> Set => Db.Set<DokumaTablet>();
    protected override string Label => "Tablet";
    protected override string CodeField => "tabletCode";

    protected override string CodeOf(DokumaTabletRequest request) => request.TabletCode;
    protected override Expression<Func<DokumaTablet, bool>> WithCode(string code) => e => e.TabletCode == code;
    protected override Expression<Func<DokumaTablet, bool>> Matches(string term) => e => e.TabletCode.Contains(term) || e.Name!.Contains(term) || e.Loom!.Code.Contains(term);
    protected override IQueryable<DokumaTablet> Order(IQueryable<DokumaTablet> query) => query.OrderBy(e => e.TabletCode);

    protected override Expression<Func<DokumaTablet, DokumaTabletDto>> ToDto
    {
        get
        {
            var onlineSince = timeProvider.GetUtcNow().UtcDateTime.AddSeconds(-OnlineSeconds);
            return e => new DokumaTabletDto(
                e.Id, e.TabletCode, e.Name, e.LoomId, e.Loom!.Code, e.DeviceKeyHash != null,
                e.IsActive && e.LastSeenAt != null && e.LastSeenAt >= onlineSince, e.LastSeenAt, e.IsActive);
        }
    }

    protected override Expression<Func<DokumaTablet, LookupDto>> ToLookup => e => new LookupDto(e.Id, e.TabletCode, e.Name ?? e.TabletCode);

    protected override void Apply(DokumaTabletRequest request, DokumaTablet entity, string code)
    {
        entity.TabletCode = code;
        entity.Name = NullIfBlank(request.Name);
        entity.LoomId = request.LoomId;
        entity.IsActive = request.IsActive;
    }

    protected override async Task<Error?> ValidateAsync(DokumaTabletRequest request, DokumaTablet? existing, CancellationToken cancellationToken)
    {
        if (await ReferenceValidation.RequireSelectableAsync(Db.Set<DokumaLoom>(), request.LoomId, existing?.LoomId, "loomId", "Tezgâh", cancellationToken) is { } error)
        {
            return error;
        }

        if (request.LoomId is null)
        {
            return null;
        }

        var existingId = existing?.Id ?? 0;
        var other = await Db.Set<DokumaTablet>().Where(t => t.LoomId == request.LoomId && t.Id != existingId).Select(t => t.TabletCode).FirstOrDefaultAsync(cancellationToken);
        if (other is null)
        {
            return null;
        }

        var message = $"Bu tezgâha zaten bir tablet bağlı ({other}).";
        return new Error(ErrorCodes.MachineAlreadyHasTablet, message, ErrorType.Conflict, new Dictionary<string, string[]> { ["loomId"] = [message] });
    }
}

public static class DokumaSessions
{
    /// <summary>Oturumu ve açık duraklamasını kapatır.</summary>
    public static void Close(DokumaTabletSession session, DokumaSessionEndReason reason, DateTime now)
    {
        foreach (var pause in session.Pauses.Where(p => p.EndedAt == null))
        {
            pause.EndedAt = now > pause.StartedAt ? now : pause.StartedAt;
        }

        session.EndedAt = now;
        session.EndReason = reason;
    }
}

public sealed class DokumaWorkSessionQuery : PageRequest
{
    /// <summary>Personel, tablet ya da tezgâhta aranır.</summary>
    public string? Search { get; set; }
}

public sealed record DokumaWorkSessionDto(
    long Id,
    string TabletCode,
    string? LoomCode,
    string OperatorCode,
    string OperatorName,
    DateTime StartedAt,
    DateTime? EndedAt,
    DokumaSessionEndReason? EndReason,
    DokumaPauseKind? PauseKind);

public interface IDokumaWorkSessionService : IFloorSessionExpiry
{
    Task<PagedResult<DokumaWorkSessionDto>> GetListAsync(DokumaWorkSessionQuery query, CancellationToken cancellationToken);
    Task<Result<DokumaWorkSessionDto>> CloseAsync(long id, CancellationToken cancellationToken);
}

public sealed class DokumaWorkSessionService(IAppDbContext db, TimeProvider timeProvider, IOptions<FloorSessionOptions> floorOptions) : IDokumaWorkSessionService
{
    private const int AutoCloseBatch = 200;

    /// <summary>Konfeksiyon kuralı: vardiyası biten açık oturum kapanır; dokumada elde süren iş olmadığı için beklenecek bir şey yoktur.</summary>
    public async Task<int> CloseExpiredAsync(CancellationToken cancellationToken)
    {
        var options = floorOptions.Value;
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var shifts = await db.Set<DokumaShift>().AsNoTracking().Where(s => s.IsActive).OrderBy(s => s.StartTime).Select(s => new ShiftWindow(s.StartTime, s.EndTime)).ToListAsync(cancellationToken);
        var candidates = await db.Set<DokumaTabletSession>().AsNoTracking()
            .Where(s => s.EndedAt == null)
            .OrderBy(s => s.Id)
            .Take(AutoCloseBatch)
            .Select(s => new { s.Id, s.StartedAt })
            .ToListAsync(cancellationToken);

        var closed = 0;
        foreach (var candidate in candidates.Where(c => FloorSessionTimes.ExpiresAt(c.StartedAt, shifts, options) <= now))
        {
            var session = await db.Set<DokumaTabletSession>().Include(s => s.Pauses).SingleAsync(s => s.Id == candidate.Id, cancellationToken);
            if (session.EndedAt is not null)
            {
                continue;
            }

            // Unutulan oturum, kapatıldığı ana değil vardiya sonuna (sonrasında top kesildiyse son kesime) damgalanır.
            // Molada ya da arada kalmış oturumda süren duraklama şu an biter; o oturum şu ana damgalanır.
            var stamp = now;
            var interrupted = session.Pauses.Any(p => p.EndedAt == null && p.Kind != DokumaPauseKind.WaitingForBeam);
            if (!interrupted && FloorSessionTimes.ShiftEndOf(session.StartedAt, shifts, options) is { } shiftEnd)
            {
                var lastWork = await db.Set<DokumaFabricRoll>()
                    .Where(r => r.CutByOperatorId == session.OperatorId && r.CreatedAt >= session.StartedAt)
                    .MaxAsync(r => (DateTime?)r.CreatedAt, cancellationToken);
                var earliest = lastWork is { } worked && worked > shiftEnd ? worked : shiftEnd;
                stamp = earliest < now ? earliest : now;
            }

            DokumaSessions.Close(session, DokumaSessionEndReason.ShiftEnded, stamp);
            await db.SaveChangesAsync(cancellationToken);
            closed++;
        }

        return closed;
    }

    private static IQueryable<DokumaWorkSessionDto> Project(IQueryable<DokumaTabletSession> sessions) => sessions.Select(s => new DokumaWorkSessionDto(
        s.Id,
        s.Tablet.TabletCode,
        s.Tablet.Loom != null ? s.Tablet.Loom.Code : null,
        s.Operator.Code,
        s.Operator.Name,
        s.StartedAt,
        s.EndedAt,
        s.EndReason,
        s.Pauses.Where(p => p.EndedAt == null).Select(p => (DokumaPauseKind?)p.Kind).FirstOrDefault()));

    public Task<PagedResult<DokumaWorkSessionDto>> GetListAsync(DokumaWorkSessionQuery query, CancellationToken cancellationToken)
    {
        var sessions = db.Set<DokumaTabletSession>().AsNoTracking();
        if (NullIfBlank(query.Search) is { } term)
        {
            sessions = sessions.Where(s => s.Operator.Code.Contains(term) || s.Operator.Name.Contains(term) || s.Tablet.TabletCode.Contains(term)
                || (s.Tablet.Loom != null && s.Tablet.Loom.Code.Contains(term)));
        }

        return Project(sessions.OrderByDescending(s => s.Id)).ToPagedResultAsync(query, cancellationToken);
    }

    public async Task<Result<DokumaWorkSessionDto>> CloseAsync(long id, CancellationToken cancellationToken)
    {
        var session = await db.Set<DokumaTabletSession>().Include(s => s.Pauses).SingleOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (session is null)
        {
            return new Error(ErrorCodes.NotFound, "Oturum bulunamadı.", ErrorType.NotFound);
        }

        if (session.EndedAt is not null)
        {
            return new Error("SESSION_NOT_ACTIVE", "Oturum zaten kapatılmış.", ErrorType.Conflict);
        }

        DokumaSessions.Close(session, DokumaSessionEndReason.ClosedByOffice, timeProvider.GetUtcNow().UtcDateTime);
        await db.SaveChangesAsync(cancellationToken);
        return await Project(db.Set<DokumaTabletSession>().AsNoTracking().Where(s => s.Id == id)).SingleAsync(cancellationToken);
    }
}

public sealed class DokumaLoomStopQuery : PageRequest
{
    /// <summary>Tezgâh, levent, neden ya da personelde aranır.</summary>
    public string? Search { get; set; }
}

public sealed record DokumaLoomStopDto(
    long Id,
    int LoomId,
    string LoomCode,
    string? BeamNo,
    DokumaStopCategory Category,
    string ReasonName,
    string? Note,
    DateTime StartedAt,
    DateTime? EndedAt,
    string StartedBy,
    string? EndedBy);

public sealed record StartDokumaLoomStopRequest(int ReasonId, string? Note = null);

public sealed class StartDokumaLoomStopRequestValidator : AbstractValidator<StartDokumaLoomStopRequest>
{
    public StartDokumaLoomStopRequestValidator()
    {
        RuleFor(x => x.ReasonId).GreaterThan(0).WithMessage("Duruş nedeni seçilmelidir.");
        RuleFor(x => x.Note).OptionalText(500);
    }
}

public interface IDokumaLoomStopService
{
    Task<PagedResult<DokumaLoomStopDto>> GetListAsync(DokumaLoomStopQuery query, CancellationToken cancellationToken);

    /// <param name="startedBy">Tabletten bildiren personel; boşsa oturumdaki kullanıcı.</param>
    Task<Result> StartAsync(int loomId, StartDokumaLoomStopRequest request, string? startedBy, CancellationToken cancellationToken);

    /// <param name="endedBy">Tabletten kapatan personel; boşsa oturumdaki kullanıcı.</param>
    Task<Result> EndAsync(int loomId, string? endedBy, CancellationToken cancellationToken);
}

/// <summary>Tezgâh duruşları: tabletten ya da ofisten açılır, kapanınca süresi verim raporuna yazılır.</summary>
public sealed class DokumaLoomStopService(IAppDbContext db, IAppLock appLock, ICurrentUser currentUser, TimeProvider timeProvider) : IDokumaLoomStopService
{
    private const string InvalidState = "DOKUMA_LOOM_STOP_INVALID";
    private static readonly TimeSpan LockTimeout = TimeSpan.FromSeconds(10);

    public Task<PagedResult<DokumaLoomStopDto>> GetListAsync(DokumaLoomStopQuery query, CancellationToken cancellationToken)
    {
        var stops = db.Set<DokumaLoomStop>().AsNoTracking();
        if (NullIfBlank(query.Search) is { } term)
        {
            stops = stops.Where(s => s.Loom.Code.Contains(term) || s.Reason.Name.Contains(term) || s.StartedBy.Contains(term) || (s.Beam != null && s.Beam.BeamNo.Contains(term)));
        }

        return stops.OrderByDescending(s => s.Id)
            .Select(s => new DokumaLoomStopDto(
                s.Id, s.LoomId, s.Loom.Code, s.Beam != null ? s.Beam.BeamNo : null, s.Category, s.Reason.Name, s.Note, s.StartedAt, s.EndedAt, s.StartedBy, s.EndedBy))
            .ToPagedResultAsync(query, cancellationToken);
    }

    public async Task<Result> StartAsync(int loomId, StartDokumaLoomStopRequest request, string? startedBy, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (!await appLock.TryAcquireAsync($"DokumaLoom:{loomId}", LockTimeout, cancellationToken))
        {
            return new Error("DOKUMA_BUSY", "Aynı anda başka bir işlem yapılıyor; tekrar deneyin.", ErrorType.Conflict);
        }

        if (!await db.Set<DokumaLoom>().AnyAsync(l => l.Id == loomId && l.IsActive, cancellationToken))
        {
            return new Error(ErrorCodes.NotFound, "Tezgâh bulunamadı ya da pasif.", ErrorType.NotFound);
        }

        var reason = await db.Set<DokumaStopReason>().AsNoTracking()
            .Where(r => r.Id == request.ReasonId && r.IsActive && r.Category != DokumaStopCategory.Pause)
            .Select(r => new { r.Category })
            .SingleOrDefaultAsync(cancellationToken);
        if (reason is null)
        {
            return new Error("REASON_NOT_FOUND", "Seçilen duruş nedeni bulunamadı.", ErrorType.Conflict);
        }

        if (await db.Set<DokumaLoomStop>().AnyAsync(s => s.LoomId == loomId && s.EndedAt == null, cancellationToken))
        {
            return new Error(InvalidState, "Bu tezgâhta açık bir duruş var; önce onu kapatın.", ErrorType.Conflict);
        }

        var beamId = await db.Set<DokumaBeam>().Where(b => b.LoomId == loomId && b.Status == BeamStatus.OnLoom).Select(b => (int?)b.Id).FirstOrDefaultAsync(cancellationToken);
        db.Set<DokumaLoomStop>().Add(new DokumaLoomStop
        {
            LoomId = loomId,
            BeamId = beamId,
            ReasonId = request.ReasonId,
            Category = reason.Category,
            Note = NullIfBlank(request.Note),
            StartedAt = timeProvider.GetUtcNow().UtcDateTime,
            StartedBy = startedBy ?? currentUser.UserName ?? "Sistem",
            TabletId = currentUser.TabletId,
        });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> EndAsync(int loomId, string? endedBy, CancellationToken cancellationToken)
    {
        var stop = await db.Set<DokumaLoomStop>().SingleOrDefaultAsync(s => s.LoomId == loomId && s.EndedAt == null, cancellationToken);
        if (stop is null)
        {
            return new Error(InvalidState, "Bu tezgâhta açık duruş yok.", ErrorType.Conflict);
        }

        stop.EndedAt = timeProvider.GetUtcNow().UtcDateTime;
        stop.EndedBy = endedBy ?? currentUser.UserName ?? "Sistem";
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
