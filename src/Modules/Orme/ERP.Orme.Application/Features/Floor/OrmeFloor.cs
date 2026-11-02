using System.Linq.Expressions;
using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Core.Application.Features.MasterData;
using ERP.Core.Domain.Common;
using ERP.Orme.Domain.Entities;
using ERP.Orme.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using static ERP.Core.Application.Features.MasterData.MasterDataRules;

namespace ERP.Orme.Application.Features.Floor;

public sealed record OrmeOperatorDto(
    int Id, string Code, string FirstName, string LastName, string Name, OrmeOperatorRole Role, string? CardUid, bool CanPause, bool CanReportBreakdown, bool IsActive, int? BreakGroupId, string? BreakGroupName);

public sealed record OrmeOperatorRequest(
    string Code, string FirstName, string LastName, OrmeOperatorRole Role = OrmeOperatorRole.Knitter, string? CardUid = null, bool CanPause = false, bool CanReportBreakdown = false, bool IsActive = true, int? BreakGroupId = null);

public sealed class OrmeOperatorRequestValidator : AbstractValidator<OrmeOperatorRequest>
{
    public OrmeOperatorRequestValidator()
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

public sealed class OrmeOperatorService(IAppDbContext db) : MasterDataService<OrmeOperator, OrmeOperatorDto, OrmeOperatorRequest>(db)
{
    protected override DbSet<OrmeOperator> Set => Db.Set<OrmeOperator>();
    protected override string Label => "Operatör";
    protected override string CodeField => "code";

    protected override string CodeOf(OrmeOperatorRequest request) => request.Code;
    protected override Expression<Func<OrmeOperator, bool>> WithCode(string code) => e => e.Code == code;
    protected override Expression<Func<OrmeOperator, bool>> Matches(string term) => e => e.Code.Contains(term) || e.Name.Contains(term) || e.CardUid!.Contains(term);
    protected override IQueryable<OrmeOperator> Order(IQueryable<OrmeOperator> query) => query.OrderBy(e => e.Code);
    protected override Expression<Func<OrmeOperator, OrmeOperatorDto>> ToDto => e => new OrmeOperatorDto(e.Id, e.Code, e.FirstName, e.LastName, e.Name, e.Role, e.CardUid, e.CanPause, e.CanReportBreakdown, e.IsActive, e.BreakGroupId, e.BreakGroup != null ? e.BreakGroup.Name : null);
    protected override Expression<Func<OrmeOperator, LookupDto>> ToLookup => e => new LookupDto(e.Id, e.Code, e.Name);

    protected override void Apply(OrmeOperatorRequest request, OrmeOperator entity, string code)
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

    protected override async Task<Error?> ValidateAsync(OrmeOperatorRequest request, OrmeOperator? existing, CancellationToken cancellationToken)
    {
        if (await ReferenceValidation.RequireSelectableAsync(Db.Set<OrmeBreakGroup>(), request.BreakGroupId, existing?.BreakGroupId, "breakGroupId", "Mola grubu", cancellationToken) is { } groupError)
        {
            return groupError;
        }

        if (RfidUid.Normalize(request.CardUid) is not { } uid)
        {
            return null;
        }

        var existingId = existing?.Id ?? 0;
        var owner = await Db.Set<OrmeOperator>().Where(o => o.CardUid == uid && o.Id != existingId).Select(o => o.Code).FirstOrDefaultAsync(cancellationToken);
        var cardError = await OrmeCards.CheckAsync(Db, uid, cancellationToken);
        if (owner is null && cardError is null)
        {
            return null;
        }

        var message = owner is not null ? $"Bu kart {owner} operatörüne ait." : cardError!.Message;
        return new Error(ErrorCodes.ValidationError, message, ErrorType.Validation, new Dictionary<string, string[]> { ["cardUid"] = [message] });
    }
}

public static class OrmeCards
{
    /// <summary>Kart tanımlı ve aktif mi? Sorun varsa okutana gösterilecek hatayı döner.</summary>
    public static async Task<Error?> CheckAsync(IAppDbContext db, string uid, CancellationToken cancellationToken)
    {
        var card = await db.Set<OrmeRfidCard>().AsNoTracking().Where(c => c.Uid == uid).Select(c => new { c.IsActive }).SingleOrDefaultAsync(cancellationToken);
        if (card is null)
        {
            return new Error("CARD_NOT_REGISTERED", "Kart sistemde tanımlı değil.", ErrorType.Conflict);
        }

        return card.IsActive ? null : new Error("CARD_INACTIVE", "Bu kart pasif.", ErrorType.Conflict);
    }
}

public sealed record OrmeRfidCardDto(int Id, string Uid, string? Label, string? AssignedTo, bool IsActive);

public sealed record OrmeRfidCardRequest(string Uid, string? Label = null, bool IsActive = true);

public sealed class OrmeRfidCardRequestValidator : AbstractValidator<OrmeRfidCardRequest>
{
    public OrmeRfidCardRequestValidator()
    {
        RuleFor(x => x.Uid).Must(uid => RfidUid.Normalize(uid) is not null).WithMessage("Kart numarası 1-10 rakam olmalıdır.");
        RuleFor(x => x.Label).OptionalText(50);
    }
}

public sealed class OrmeRfidCardService(IAppDbContext db) : MasterDataService<OrmeRfidCard, OrmeRfidCardDto, OrmeRfidCardRequest>(db)
{
    protected override DbSet<OrmeRfidCard> Set => Db.Set<OrmeRfidCard>();
    protected override string Label => "RFID kartı";
    protected override string CodeField => "uid";

    protected override string CodeOf(OrmeRfidCardRequest request) => RfidUid.Normalize(request.Uid) ?? request.Uid;
    protected override Expression<Func<OrmeRfidCard, bool>> WithCode(string code) => e => e.Uid == code;
    protected override Expression<Func<OrmeRfidCard, bool>> Matches(string term) => e => e.Uid.Contains(term) || e.Label!.Contains(term);
    protected override IQueryable<OrmeRfidCard> Order(IQueryable<OrmeRfidCard> query) => query.OrderBy(e => e.Label).ThenBy(e => e.Uid);

    protected override Expression<Func<OrmeRfidCard, OrmeRfidCardDto>> ToDto => e => new OrmeRfidCardDto(
        e.Id,
        e.Uid,
        e.Label,
        Db.Set<OrmeOperator>().Where(o => o.CardUid == e.Uid).Select(o => o.Name + " (" + o.Code + ")").FirstOrDefault(),
        e.IsActive);

    protected override Expression<Func<OrmeRfidCard, LookupDto>> ToLookup => e => new LookupDto(e.Id, e.Uid, e.Label ?? e.Uid);

    protected override void Apply(OrmeRfidCardRequest request, OrmeRfidCard entity, string code)
    {
        entity.Uid = code;
        entity.Label = NullIfBlank(request.Label);
        entity.IsActive = request.IsActive;
    }

    protected override async Task<Error?> ValidateAsync(OrmeRfidCardRequest request, OrmeRfidCard? existing, CancellationToken cancellationToken)
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
        var uid = await Db.Set<OrmeRfidCard>().Where(c => c.Id == id).Select(c => c.Uid).SingleAsync(cancellationToken);
        return await IsAssignedAsync(uid, cancellationToken) ? "kart şu an bir operatöre atanmış" : null;
    }

    private Task<bool> IsAssignedAsync(string uid, CancellationToken cancellationToken) =>
        Db.Set<OrmeOperator>().AnyAsync(o => o.CardUid == uid, cancellationToken);
}

public sealed record OrmeStopReasonDto(int Id, string Code, string Name, OrmeStopCategory Category, int SortOrder, bool IsActive);

public sealed record OrmeStopReasonRequest(string Code, string Name, OrmeStopCategory Category, int SortOrder = 0, bool IsActive = true);

public sealed class OrmeStopReasonRequestValidator : AbstractValidator<OrmeStopReasonRequest>
{
    public OrmeStopReasonRequestValidator()
    {
        RuleFor(x => x.Code).ValidCode();
        RuleFor(x => x.Name).ValidName(100);
        RuleFor(x => x.Category).IsInEnum().WithMessage("Geçerli bir tür seçin.");
        RuleFor(x => x.SortOrder).InclusiveBetween(0, 9999).WithMessage("Sıra 0 ile 9999 arasında olmalıdır.");
    }
}

public sealed class OrmeStopReasonService(IAppDbContext db) : MasterDataService<OrmeStopReason, OrmeStopReasonDto, OrmeStopReasonRequest>(db)
{
    protected override DbSet<OrmeStopReason> Set => Db.Set<OrmeStopReason>();
    protected override string Label => "Duruş nedeni";
    protected override string CodeField => "code";

    protected override string CodeOf(OrmeStopReasonRequest request) => request.Code;
    protected override Expression<Func<OrmeStopReason, bool>> WithCode(string code) => e => e.Code == code;
    protected override Expression<Func<OrmeStopReason, bool>> Matches(string term) => e => e.Code.Contains(term) || e.Name.Contains(term);
    protected override IQueryable<OrmeStopReason> Order(IQueryable<OrmeStopReason> query) => query.OrderBy(e => e.Category).ThenBy(e => e.SortOrder).ThenBy(e => e.Name);
    protected override Expression<Func<OrmeStopReason, OrmeStopReasonDto>> ToDto => e => new OrmeStopReasonDto(e.Id, e.Code, e.Name, e.Category, e.SortOrder, e.IsActive);
    protected override Expression<Func<OrmeStopReason, LookupDto>> ToLookup => e => new LookupDto(e.Id, e.Code, e.Name);

    protected override void Apply(OrmeStopReasonRequest request, OrmeStopReason entity, string code)
    {
        entity.Code = code;
        entity.Name = request.Name.Trim();
        entity.Category = request.Category;
        entity.SortOrder = request.SortOrder;
        entity.IsActive = request.IsActive;
    }

    protected override async Task<string?> FindUsageAsync(int id, CancellationToken cancellationToken) =>
        await Db.Set<OrmeMachineStop>().AnyAsync(s => s.ReasonId == id, cancellationToken) || await Db.Set<OrmeSessionPause>().AnyAsync(p => p.ReasonId == id, cancellationToken)
            ? "makine duruşları ve oturum araları"
            : null;
}

/// <param name="IsOnline">Tabletten son iki dakikada istek geldi mi.</param>
public sealed record OrmeTabletDto(int Id, string TabletCode, string? Name, int? MachineId, string? MachineCode, bool IsEnrolled, bool IsOnline, DateTime? LastSeenAt, bool IsActive);

public sealed record OrmeTabletRequest(string TabletCode, string? Name = null, int? MachineId = null, bool IsActive = true);

public sealed class OrmeTabletRequestValidator : AbstractValidator<OrmeTabletRequest>
{
    public OrmeTabletRequestValidator()
    {
        RuleFor(x => x.TabletCode).ValidCode();
        RuleFor(x => x.Name).OptionalText(150);
    }
}

public sealed class OrmeTabletService(IAppDbContext db, TimeProvider timeProvider) : MasterDataService<OrmeTablet, OrmeTabletDto, OrmeTabletRequest>(db)
{
    private const int OnlineSeconds = 120;

    protected override DbSet<OrmeTablet> Set => Db.Set<OrmeTablet>();
    protected override string Label => "Tablet";
    protected override string CodeField => "tabletCode";

    protected override string CodeOf(OrmeTabletRequest request) => request.TabletCode;
    protected override Expression<Func<OrmeTablet, bool>> WithCode(string code) => e => e.TabletCode == code;
    protected override Expression<Func<OrmeTablet, bool>> Matches(string term) => e => e.TabletCode.Contains(term) || e.Name!.Contains(term) || e.Machine!.Code.Contains(term);
    protected override IQueryable<OrmeTablet> Order(IQueryable<OrmeTablet> query) => query.OrderBy(e => e.TabletCode);

    protected override Expression<Func<OrmeTablet, OrmeTabletDto>> ToDto
    {
        get
        {
            var onlineSince = timeProvider.GetUtcNow().UtcDateTime.AddSeconds(-OnlineSeconds);
            return e => new OrmeTabletDto(
                e.Id, e.TabletCode, e.Name, e.MachineId, e.Machine!.Code, e.DeviceKeyHash != null,
                e.IsActive && e.LastSeenAt != null && e.LastSeenAt >= onlineSince, e.LastSeenAt, e.IsActive);
        }
    }

    protected override Expression<Func<OrmeTablet, LookupDto>> ToLookup => e => new LookupDto(e.Id, e.TabletCode, e.Name ?? e.TabletCode);

    protected override void Apply(OrmeTabletRequest request, OrmeTablet entity, string code)
    {
        entity.TabletCode = code;
        entity.Name = NullIfBlank(request.Name);
        entity.MachineId = request.MachineId;
        entity.IsActive = request.IsActive;
    }

    protected override async Task<Error?> ValidateAsync(OrmeTabletRequest request, OrmeTablet? existing, CancellationToken cancellationToken)
    {
        if (await ReferenceValidation.RequireSelectableAsync(Db.Set<OrmeMachine>(), request.MachineId, existing?.MachineId, "machineId", "Makine", cancellationToken) is { } error)
        {
            return error;
        }

        if (request.MachineId is null)
        {
            return null;
        }

        var existingId = existing?.Id ?? 0;
        var other = await Db.Set<OrmeTablet>().Where(t => t.MachineId == request.MachineId && t.Id != existingId).Select(t => t.TabletCode).FirstOrDefaultAsync(cancellationToken);
        if (other is null)
        {
            return null;
        }

        var message = $"Bu makineye zaten bir tablet bağlı ({other}).";
        return new Error(ErrorCodes.MachineAlreadyHasTablet, message, ErrorType.Conflict, new Dictionary<string, string[]> { ["machineId"] = [message] });
    }
}

public static class OrmeSessions
{
    /// <summary>Oturumu ve açık duraklamasını kapatır.</summary>
    public static void Close(OrmeTabletSession session, OrmeSessionEndReason reason, DateTime now)
    {
        foreach (var pause in session.Pauses.Where(p => p.EndedAt == null))
        {
            pause.EndedAt = now > pause.StartedAt ? now : pause.StartedAt;
        }

        session.EndedAt = now;
        session.EndReason = reason;
    }
}

public sealed class OrmeWorkSessionQuery : PageRequest
{
    /// <summary>Personel, tablet ya da makinede aranır.</summary>
    public string? Search { get; set; }
}

public sealed record OrmeWorkSessionDto(
    long Id,
    string TabletCode,
    string? MachineCode,
    string OperatorCode,
    string OperatorName,
    DateTime StartedAt,
    DateTime? EndedAt,
    OrmeSessionEndReason? EndReason,
    OrmePauseKind? PauseKind);

public interface IOrmeWorkSessionService : IFloorSessionExpiry
{
    Task<PagedResult<OrmeWorkSessionDto>> GetListAsync(OrmeWorkSessionQuery query, CancellationToken cancellationToken);
    Task<Result<OrmeWorkSessionDto>> CloseAsync(long id, CancellationToken cancellationToken);
}

public sealed class OrmeWorkSessionService(IAppDbContext db, TimeProvider timeProvider, IOptions<FloorSessionOptions> floorOptions) : IOrmeWorkSessionService
{
    private const int AutoCloseBatch = 200;

    /// <summary>Konfeksiyon kuralı: vardiyası biten açık oturum kapanır; örmede elde süren iş olmadığı için beklenecek bir şey yoktur.</summary>
    public async Task<int> CloseExpiredAsync(CancellationToken cancellationToken)
    {
        var options = floorOptions.Value;
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var shifts = await db.Set<OrmeShift>().AsNoTracking().Where(s => s.IsActive).OrderBy(s => s.StartTime).Select(s => new ShiftWindow(s.StartTime, s.EndTime)).ToListAsync(cancellationToken);
        var candidates = await db.Set<OrmeTabletSession>().AsNoTracking()
            .Where(s => s.EndedAt == null)
            .OrderBy(s => s.Id)
            .Take(AutoCloseBatch)
            .Select(s => new { s.Id, s.StartedAt })
            .ToListAsync(cancellationToken);

        var closed = 0;
        foreach (var candidate in candidates.Where(c => FloorSessionTimes.ExpiresAt(c.StartedAt, shifts, options) <= now))
        {
            var session = await db.Set<OrmeTabletSession>().Include(s => s.Pauses).SingleAsync(s => s.Id == candidate.Id, cancellationToken);
            if (session.EndedAt is not null)
            {
                continue;
            }

            // Unutulan oturum, kapatıldığı ana değil vardiya sonuna (sonrasında top kaydedildiyse son topa) damgalanır.
            // Molada ya da arada kalmış oturumda süren duraklama şu an biter; o oturum şu ana damgalanır.
            var stamp = now;
            var interrupted = session.Pauses.Any(p => p.EndedAt == null && p.Kind != OrmePauseKind.WaitingForOrder);
            if (!interrupted && FloorSessionTimes.ShiftEndOf(session.StartedAt, shifts, options) is { } shiftEnd)
            {
                var lastWork = await db.Set<OrmeFabricRoll>()
                    .Where(r => r.CutByOperatorId == session.OperatorId && r.CreatedAt >= session.StartedAt)
                    .MaxAsync(r => (DateTime?)r.CreatedAt, cancellationToken);
                var earliest = lastWork is { } worked && worked > shiftEnd ? worked : shiftEnd;
                stamp = earliest < now ? earliest : now;
            }

            OrmeSessions.Close(session, OrmeSessionEndReason.ShiftEnded, stamp);
            await db.SaveChangesAsync(cancellationToken);
            closed++;
        }

        return closed;
    }

    private static IQueryable<OrmeWorkSessionDto> Project(IQueryable<OrmeTabletSession> sessions) => sessions.Select(s => new OrmeWorkSessionDto(
        s.Id,
        s.Tablet.TabletCode,
        s.Tablet.Machine != null ? s.Tablet.Machine.Code : null,
        s.Operator.Code,
        s.Operator.Name,
        s.StartedAt,
        s.EndedAt,
        s.EndReason,
        s.Pauses.Where(p => p.EndedAt == null).Select(p => (OrmePauseKind?)p.Kind).FirstOrDefault()));

    public Task<PagedResult<OrmeWorkSessionDto>> GetListAsync(OrmeWorkSessionQuery query, CancellationToken cancellationToken)
    {
        var sessions = db.Set<OrmeTabletSession>().AsNoTracking();
        if (NullIfBlank(query.Search) is { } term)
        {
            sessions = sessions.Where(s => s.Operator.Code.Contains(term) || s.Operator.Name.Contains(term) || s.Tablet.TabletCode.Contains(term)
                || (s.Tablet.Machine != null && s.Tablet.Machine.Code.Contains(term)));
        }

        return Project(sessions.OrderByDescending(s => s.Id)).ToPagedResultAsync(query, cancellationToken);
    }

    public async Task<Result<OrmeWorkSessionDto>> CloseAsync(long id, CancellationToken cancellationToken)
    {
        var session = await db.Set<OrmeTabletSession>().Include(s => s.Pauses).SingleOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (session is null)
        {
            return new Error(ErrorCodes.NotFound, "Oturum bulunamadı.", ErrorType.NotFound);
        }

        if (session.EndedAt is not null)
        {
            return new Error("SESSION_NOT_ACTIVE", "Oturum zaten kapatılmış.", ErrorType.Conflict);
        }

        OrmeSessions.Close(session, OrmeSessionEndReason.ClosedByOffice, timeProvider.GetUtcNow().UtcDateTime);
        await db.SaveChangesAsync(cancellationToken);
        return await Project(db.Set<OrmeTabletSession>().AsNoTracking().Where(s => s.Id == id)).SingleAsync(cancellationToken);
    }
}

public sealed class OrmeMachineStopQuery : PageRequest
{
    /// <summary>Makine, emir, neden ya da personelde aranır.</summary>
    public string? Search { get; set; }
}

public sealed record OrmeMachineStopDto(
    long Id,
    int MachineId,
    string MachineCode,
    string? KnitOrderNo,
    OrmeStopCategory Category,
    string ReasonName,
    string? Note,
    DateTime StartedAt,
    DateTime? EndedAt,
    string StartedBy,
    string? EndedBy);

public sealed record StartOrmeMachineStopRequest(int ReasonId, string? Note = null);

public sealed class StartOrmeMachineStopRequestValidator : AbstractValidator<StartOrmeMachineStopRequest>
{
    public StartOrmeMachineStopRequestValidator()
    {
        RuleFor(x => x.ReasonId).GreaterThan(0).WithMessage("Duruş nedeni seçilmelidir.");
        RuleFor(x => x.Note).OptionalText(500);
    }
}

public interface IOrmeMachineStopService
{
    Task<PagedResult<OrmeMachineStopDto>> GetListAsync(OrmeMachineStopQuery query, CancellationToken cancellationToken);

    /// <param name="startedBy">Tabletten bildiren personel; boşsa oturumdaki kullanıcı.</param>
    Task<Result> StartAsync(int machineId, StartOrmeMachineStopRequest request, string? startedBy, CancellationToken cancellationToken);

    /// <param name="endedBy">Tabletten kapatan personel; boşsa oturumdaki kullanıcı.</param>
    Task<Result> EndAsync(int machineId, string? endedBy, CancellationToken cancellationToken);
}

/// <summary>Makine duruşları: tabletten ya da ofisten açılır, kapanınca süresi verim raporuna yazılır.</summary>
public sealed class OrmeMachineStopService(IAppDbContext db, IAppLock appLock, ICurrentUser currentUser, TimeProvider timeProvider) : IOrmeMachineStopService
{
    private const string InvalidState = "ORME_MACHINE_STOP_INVALID";
    private static readonly TimeSpan LockTimeout = TimeSpan.FromSeconds(10);

    public Task<PagedResult<OrmeMachineStopDto>> GetListAsync(OrmeMachineStopQuery query, CancellationToken cancellationToken)
    {
        var stops = db.Set<OrmeMachineStop>().AsNoTracking();
        if (NullIfBlank(query.Search) is { } term)
        {
            stops = stops.Where(s => s.Machine.Code.Contains(term) || s.Reason.Name.Contains(term) || s.StartedBy.Contains(term) || (s.KnitOrder != null && s.KnitOrder.KnitOrderNo.Contains(term)));
        }

        return stops.OrderByDescending(s => s.Id)
            .Select(s => new OrmeMachineStopDto(
                s.Id, s.MachineId, s.Machine.Code, s.KnitOrder != null ? s.KnitOrder.KnitOrderNo : null, s.Category, s.Reason.Name, s.Note, s.StartedAt, s.EndedAt, s.StartedBy, s.EndedBy))
            .ToPagedResultAsync(query, cancellationToken);
    }

    public async Task<Result> StartAsync(int machineId, StartOrmeMachineStopRequest request, string? startedBy, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (!await appLock.TryAcquireAsync($"OrmeMachine:{machineId}", LockTimeout, cancellationToken))
        {
            return new Error("ORME_BUSY", "Aynı anda başka bir işlem yapılıyor; tekrar deneyin.", ErrorType.Conflict);
        }

        if (!await db.Set<OrmeMachine>().AnyAsync(l => l.Id == machineId && l.IsActive, cancellationToken))
        {
            return new Error(ErrorCodes.NotFound, "Makine bulunamadı ya da pasif.", ErrorType.NotFound);
        }

        var reason = await db.Set<OrmeStopReason>().AsNoTracking()
            .Where(r => r.Id == request.ReasonId && r.IsActive && r.Category != OrmeStopCategory.Pause)
            .Select(r => new { r.Category })
            .SingleOrDefaultAsync(cancellationToken);
        if (reason is null)
        {
            return new Error("REASON_NOT_FOUND", "Seçilen duruş nedeni bulunamadı.", ErrorType.Conflict);
        }

        if (await db.Set<OrmeMachineStop>().AnyAsync(s => s.MachineId == machineId && s.EndedAt == null, cancellationToken))
        {
            return new Error(InvalidState, "Bu makinede açık bir duruş var; önce onu kapatın.", ErrorType.Conflict);
        }

        var knitOrderId = await db.Set<OrmeKnitOrderMachine>().Where(m => m.MachineId == machineId && m.ReleasedOn == null).Select(m => (int?)m.KnitOrderId).FirstOrDefaultAsync(cancellationToken);
        db.Set<OrmeMachineStop>().Add(new OrmeMachineStop
        {
            MachineId = machineId,
            KnitOrderId = knitOrderId,
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

    public async Task<Result> EndAsync(int machineId, string? endedBy, CancellationToken cancellationToken)
    {
        var stop = await db.Set<OrmeMachineStop>().SingleOrDefaultAsync(s => s.MachineId == machineId && s.EndedAt == null, cancellationToken);
        if (stop is null)
        {
            return new Error(InvalidState, "Bu makinede açık duruş yok.", ErrorType.Conflict);
        }

        stop.EndedAt = timeProvider.GetUtcNow().UtcDateTime;
        stop.EndedBy = endedBy ?? currentUser.UserName ?? "Sistem";
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
