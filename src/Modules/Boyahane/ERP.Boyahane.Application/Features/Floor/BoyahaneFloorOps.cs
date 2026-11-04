using System.Linq.Expressions;
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

public sealed record BoyahaneRfidCardDto(int Id, string Uid, BoyahaneCardType CardType, string? Label, string? AssignedTo, bool IsActive);

public sealed record BoyahaneRfidCardRequest(string Uid, BoyahaneCardType CardType, string? Label = null, bool IsActive = true);

public sealed class BoyahaneRfidCardRequestValidator : AbstractValidator<BoyahaneRfidCardRequest>
{
    public BoyahaneRfidCardRequestValidator()
    {
        RuleFor(x => x.Uid).Must(uid => RfidUid.Normalize(uid) is not null).WithMessage("Kart numarası en fazla 10 harf ya da rakam olmalıdır.");
        RuleFor(x => x.CardType).IsInEnum().WithMessage("Kart türü geçersiz.");
        RuleFor(x => x.Label).OptionalText(50);
    }
}

public static class BoyahaneCards
{
    /// <summary>Kart tanımlı, aktif ve istenen türde mi? Sorun varsa okutana gösterilecek hatayı döner.</summary>
    public static async Task<Error?> CheckAsync(IAppDbContext db, string uid, BoyahaneCardType expected, CancellationToken cancellationToken)
    {
        var card = await db.Set<BoyahaneRfidCard>().AsNoTracking()
            .Where(c => c.Uid == uid)
            .Select(c => new { c.CardType, c.IsActive })
            .SingleOrDefaultAsync(cancellationToken);
        if (card is null)
        {
            return new Error("CARD_NOT_REGISTERED", "Kart sistemde tanımlı değil.", ErrorType.Conflict);
        }

        if (!card.IsActive)
        {
            return new Error("CARD_INACTIVE", "Bu kart pasif.", ErrorType.Conflict);
        }

        if (card.CardType != expected)
        {
            return new Error(
                "CARD_TYPE_MISMATCH",
                expected == BoyahaneCardType.Batch ? "Bu bir personel kartı. Parti kartını okutun." : "Bu bir parti kartı. Personel kartınızı okutun.",
                ErrorType.Conflict);
        }

        return null;
    }
}

public sealed class BoyahaneRfidCardService(IAppDbContext db) : MasterDataService<BoyahaneRfidCard, BoyahaneRfidCardDto, BoyahaneRfidCardRequest>(db)
{
    protected override DbSet<BoyahaneRfidCard> Set => Db.Set<BoyahaneRfidCard>();
    protected override string Label => "RFID kartı";
    protected override string CodeField => "uid";

    protected override string CodeOf(BoyahaneRfidCardRequest request) => RfidUid.Normalize(request.Uid) ?? request.Uid;
    protected override Expression<Func<BoyahaneRfidCard, bool>> WithCode(string code) => e => e.Uid == code;
    protected override Expression<Func<BoyahaneRfidCard, bool>> Matches(string term) => e => e.Uid.Contains(term) || e.Label!.Contains(term);
    protected override IQueryable<BoyahaneRfidCard> Order(IQueryable<BoyahaneRfidCard> query) => query.OrderBy(e => e.CardType).ThenBy(e => e.Label).ThenBy(e => e.Uid);

    protected override Expression<Func<BoyahaneRfidCard, BoyahaneRfidCardDto>> ToDto => e => new BoyahaneRfidCardDto(
        e.Id,
        e.Uid,
        e.CardType,
        e.Label,
        e.CardType == BoyahaneCardType.Batch
            ? Db.Set<BoyahaneBatch>().Where(b => b.CardUid == e.Uid).Select(b => "Parti " + b.BatchNo).FirstOrDefault()
            : Db.Set<BoyahaneOperator>().Where(o => o.CardUid == e.Uid).Select(o => o.Name + " (" + o.Code + ")").FirstOrDefault(),
        e.IsActive);

    protected override Expression<Func<BoyahaneRfidCard, LookupDto>> ToLookup => e => new LookupDto(e.Id, e.Uid, e.Label ?? e.Uid);

    protected override void Apply(BoyahaneRfidCardRequest request, BoyahaneRfidCard entity, string code)
    {
        entity.Uid = code;
        entity.CardType = request.CardType;
        entity.Label = NullIfBlank(request.Label);
        entity.IsActive = request.IsActive;
    }

    protected override async Task<Error?> ValidateAsync(BoyahaneRfidCardRequest request, BoyahaneRfidCard? existing, CancellationToken cancellationToken)
    {
        if (existing is null || !await IsAssignedAsync(existing.Uid, cancellationToken))
        {
            return null;
        }

        if (existing.CardType == request.CardType && existing.Uid == RfidUid.Normalize(request.Uid))
        {
            return null;
        }

        const string message = "Atanmış kartın numarası ve türü değiştirilemez; önce kartı bırakın.";
        return new Error(ErrorCodes.ValidationError, message, ErrorType.Validation, new Dictionary<string, string[]> { ["cardType"] = [message] });
    }

    protected override async Task<string?> FindUsageAsync(int id, CancellationToken cancellationToken)
    {
        var uid = await Db.Set<BoyahaneRfidCard>().Where(c => c.Id == id).Select(c => c.Uid).SingleAsync(cancellationToken);
        return await IsAssignedAsync(uid, cancellationToken) ? "kart şu an bir partiye ya da personele atanmış" : null;
    }

    private async Task<bool> IsAssignedAsync(string uid, CancellationToken cancellationToken) =>
        await Db.Set<BoyahaneBatch>().AnyAsync(b => b.CardUid == uid, cancellationToken)
        || await Db.Set<BoyahaneOperator>().AnyAsync(o => o.CardUid == uid, cancellationToken);
}

public sealed record BoyahaneStopReasonDto(int Id, string Code, string Name, BoyahaneStopReasonCategory Category, int SortOrder, bool IsActive);

public sealed record BoyahaneStopReasonRequest(string Code, string Name, BoyahaneStopReasonCategory Category, int SortOrder = 0, bool IsActive = true);

public sealed class BoyahaneStopReasonRequestValidator : AbstractValidator<BoyahaneStopReasonRequest>
{
    public BoyahaneStopReasonRequestValidator()
    {
        RuleFor(x => x.Code).ValidCode();
        RuleFor(x => x.Name).ValidName(100);
        RuleFor(x => x.Category).IsInEnum().WithMessage("Geçerli bir tür seçin.");
        RuleFor(x => x.SortOrder).InclusiveBetween(0, 9999).WithMessage("Sıra 0 ile 9999 arasında olmalıdır.");
    }
}

public sealed class BoyahaneStopReasonService(IAppDbContext db) : MasterDataService<BoyahaneStopReason, BoyahaneStopReasonDto, BoyahaneStopReasonRequest>(db)
{
    protected override DbSet<BoyahaneStopReason> Set => Db.Set<BoyahaneStopReason>();
    protected override string Label => "Duruş nedeni";
    protected override string CodeField => "code";

    protected override string CodeOf(BoyahaneStopReasonRequest request) => request.Code;
    protected override Expression<Func<BoyahaneStopReason, bool>> WithCode(string code) => e => e.Code == code;
    protected override Expression<Func<BoyahaneStopReason, bool>> Matches(string term) => e => e.Code.Contains(term) || e.Name.Contains(term);
    protected override IQueryable<BoyahaneStopReason> Order(IQueryable<BoyahaneStopReason> query) => query.OrderBy(e => e.Category).ThenBy(e => e.SortOrder).ThenBy(e => e.Name);
    protected override Expression<Func<BoyahaneStopReason, BoyahaneStopReasonDto>> ToDto => e => new BoyahaneStopReasonDto(e.Id, e.Code, e.Name, e.Category, e.SortOrder, e.IsActive);
    protected override Expression<Func<BoyahaneStopReason, LookupDto>> ToLookup => e => new LookupDto(e.Id, e.Code, e.Name);

    protected override void Apply(BoyahaneStopReasonRequest request, BoyahaneStopReason entity, string code)
    {
        entity.Code = code;
        entity.Name = request.Name.Trim();
        entity.Category = request.Category;
        entity.SortOrder = request.SortOrder;
        entity.IsActive = request.IsActive;
    }

    protected override async Task<string?> FindUsageAsync(int id, CancellationToken cancellationToken) =>
        await Db.Set<BoyahaneSessionPause>().AnyAsync(p => p.ReasonId == id, cancellationToken) ? "oturum duraklamaları" : null;
}

public static class BoyahaneSessions
{
    /// <summary>Oturumu ve açık duraklamasını kapatır.</summary>
    public static void Close(BoyahaneTabletSession session, BoyahaneSessionEndReason reason, DateTime now)
    {
        foreach (var pause in session.Pauses.Where(p => p.EndedAt == null))
        {
            pause.EndedAt = now > pause.StartedAt ? now : pause.StartedAt;
        }

        session.EndedAt = now;
        session.EndReason = reason;
    }
}

public sealed record BoyahaneMachineStatusLogDto(DyeMachineStatus Status, DateTime StartedAt, DateTime? EndedAt, string? Reason, string ChangedBy);

public sealed record BoyahaneMachineStatusDto(int MachineId, string MachineCode, DyeMachineStatus Status, IReadOnlyList<BoyahaneMachineStatusLogDto> History);

public sealed record ChangeBoyahaneMachineStatusRequest(DyeMachineStatus Status, string? Reason = null);

public sealed class ChangeBoyahaneMachineStatusRequestValidator : AbstractValidator<ChangeBoyahaneMachineStatusRequest>
{
    public ChangeBoyahaneMachineStatusRequestValidator()
    {
        RuleFor(x => x.Status).IsInEnum().WithMessage("Geçerli bir durum seçin.");
        RuleFor(x => x.Reason).OptionalText(500);
    }
}

public interface IBoyahaneMachineStatusService
{
    Task<Result<BoyahaneMachineStatusDto>> GetAsync(int machineId, CancellationToken cancellationToken);

    /// <param name="changedBy">Tabletten bildiren personel; boşsa oturumdaki kullanıcı.</param>
    Task<Result<BoyahaneMachineStatusDto>> ChangeAsync(int machineId, DyeMachineStatus status, string? reason, string? changedBy, CancellationToken cancellationToken);

    /// <summary>Ofisten değişiklik: arızalı makineyi arızadan çıkarmak ayrı yetki ister.</summary>
    Task<Result<BoyahaneMachineStatusDto>> ChangeFromOfficeAsync(int machineId, ChangeBoyahaneMachineStatusRequest request, bool canResolveBreakdown, CancellationToken cancellationToken);
}

public sealed class BoyahaneMachineStatusService(IAppDbContext db, ICurrentUser currentUser, TimeProvider timeProvider) : IBoyahaneMachineStatusService
{
    private const int HistorySize = 20;

    private static readonly Error NotFound = new(ErrorCodes.NotFound, "Makine bulunamadı.", ErrorType.NotFound);

    public async Task<Result<BoyahaneMachineStatusDto>> GetAsync(int machineId, CancellationToken cancellationToken)
    {
        var machine = await db.Set<BoyahaneMachine>().AsNoTracking().SingleOrDefaultAsync(m => m.Id == machineId, cancellationToken);
        if (machine is null)
        {
            return NotFound;
        }

        var history = await db.Set<BoyahaneMachineStatusLog>().AsNoTracking()
            .Where(l => l.MachineId == machineId)
            .OrderByDescending(l => l.StartedAt).ThenByDescending(l => l.Id)
            .Take(HistorySize)
            .Select(l => new BoyahaneMachineStatusLogDto(l.Status, l.StartedAt, l.EndedAt, l.Reason, l.ChangedBy))
            .ToListAsync(cancellationToken);
        return new BoyahaneMachineStatusDto(machine.Id, machine.Code, machine.Status, history);
    }

    public async Task<Result<BoyahaneMachineStatusDto>> ChangeFromOfficeAsync(int machineId, ChangeBoyahaneMachineStatusRequest request, bool canResolveBreakdown, CancellationToken cancellationToken)
    {
        var current = await db.Set<BoyahaneMachine>().AsNoTracking().Where(m => m.Id == machineId).Select(m => (DyeMachineStatus?)m.Status).SingleOrDefaultAsync(cancellationToken);
        if (current == DyeMachineStatus.Breakdown && request.Status != DyeMachineStatus.Breakdown && !canResolveBreakdown)
        {
            return new Error("NOT_AUTHORIZED_FOR_ACTION", "Arızayı kapatma yetkiniz yok.", ErrorType.Forbidden);
        }

        return await ChangeAsync(machineId, request.Status, request.Reason, null, cancellationToken);
    }

    public async Task<Result<BoyahaneMachineStatusDto>> ChangeAsync(int machineId, DyeMachineStatus status, string? reason, string? changedBy, CancellationToken cancellationToken)
    {
        var machine = await db.Set<BoyahaneMachine>().SingleOrDefaultAsync(m => m.Id == machineId, cancellationToken);
        if (machine is null)
        {
            return NotFound;
        }

        if (machine.Status == status)
        {
            return await GetAsync(machineId, cancellationToken);
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var open = await db.Set<BoyahaneMachineStatusLog>().Where(l => l.MachineId == machineId && l.EndedAt == null).ToListAsync(cancellationToken);
        foreach (var log in open)
        {
            log.EndedAt = now;
        }

        var previous = machine.Status;
        machine.Status = status;
        if (status != DyeMachineStatus.Available)
        {
            db.Set<BoyahaneMachineStatusLog>().Add(new BoyahaneMachineStatusLog
            {
                MachineId = machineId,
                Status = status,
                StartedAt = now,
                Reason = NullIfBlank(reason),
                ChangedBy = changedBy ?? currentUser.UserName ?? "Sistem",
                TabletId = currentUser.TabletId,
            });
        }

        // Arıza makinedeki oturumu duraklatır; arıza kapanınca oturum kaldığı yerden sürer.
        if (status == DyeMachineStatus.Breakdown || previous == DyeMachineStatus.Breakdown)
        {
            var sessions = await db.Set<BoyahaneTabletSession>()
                .Include(s => s.Pauses)
                .Where(s => s.EndedAt == null && s.Tablet.MachineId == machineId)
                .ToListAsync(cancellationToken);
            foreach (var session in sessions)
            {
                foreach (var pause in session.Pauses.Where(p => p.EndedAt == null && (status == DyeMachineStatus.Breakdown || p.Kind == BoyahanePauseKind.MachineBreakdown)))
                {
                    pause.EndedAt = now;
                }

                if (status == DyeMachineStatus.Breakdown)
                {
                    await db.SaveChangesAsync(cancellationToken);
                    session.Pauses.Add(new BoyahaneSessionPause { Kind = BoyahanePauseKind.MachineBreakdown, StartedAt = now });
                }
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetAsync(machineId, cancellationToken);
    }
}

public sealed class BoyahaneWorkSessionQuery : PageRequest
{
    /// <summary>Personel, tablet ya da makinede aranır.</summary>
    public string? Search { get; set; }
}

public sealed record BoyahaneWorkSessionDto(
    long Id,
    string TabletCode,
    string? MachineCode,
    string OperatorCode,
    string OperatorName,
    DateTime StartedAt,
    DateTime? EndedAt,
    BoyahaneSessionEndReason? EndReason,
    BoyahanePauseKind? PauseKind,
    bool HasWorkInProgress);

public interface IBoyahaneWorkSessionService : IFloorSessionExpiry
{
    Task<PagedResult<BoyahaneWorkSessionDto>> GetListAsync(BoyahaneWorkSessionQuery query, CancellationToken cancellationToken);
    Task<Result<BoyahaneWorkSessionDto>> CloseAsync(long id, CancellationToken cancellationToken);
}

public sealed class BoyahaneWorkSessionService(IAppDbContext db, TimeProvider timeProvider, IOptions<FloorSessionOptions> floorOptions) : IBoyahaneWorkSessionService
{
    private const int AutoCloseBatch = 200;

    /// <summary>Konfeksiyon kuralı: süren adımı olan oturum kapatılmaz, adım bitince bir sonraki turda kapanır.</summary>
    public async Task<int> CloseExpiredAsync(CancellationToken cancellationToken)
    {
        var options = floorOptions.Value;
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var shifts = await db.Set<BoyahaneShift>().AsNoTracking().Where(s => s.IsActive).OrderBy(s => s.StartTime).Select(s => new ShiftWindow(s.StartTime, s.EndTime)).ToListAsync(cancellationToken);
        var candidates = await db.Set<BoyahaneTabletSession>().AsNoTracking()
            .Where(s => s.EndedAt == null)
            .OrderBy(s => s.Id)
            .Take(AutoCloseBatch)
            .Select(s => new { s.Id, s.StartedAt })
            .ToListAsync(cancellationToken);

        var closed = 0;
        foreach (var candidate in candidates.Where(c => FloorSessionTimes.ExpiresAt(c.StartedAt, shifts, options) <= now))
        {
            var session = await db.Set<BoyahaneTabletSession>().Include(s => s.Pauses).SingleAsync(s => s.Id == candidate.Id, cancellationToken);
            var hasWork = await db.Set<BoyahaneBatchStep>().AnyAsync(
                s => s.Status == BatchStepStatus.InProgress && s.TabletId == session.TabletId && s.StartedByOperatorId == session.OperatorId,
                cancellationToken);
            if (session.EndedAt is not null || hasWork)
            {
                continue;
            }

            // Unutulan oturum, kapatıldığı ana değil vardiya sonuna (sonrasında çalışıldıysa son hareketine) damgalanır.
            // Molada, arada ya da arızada kalmış oturumda süren duraklama şu an biter; o oturum şu ana damgalanır.
            var stamp = now;
            var interrupted = session.Pauses.Any(p => p.EndedAt == null && p.Kind != BoyahanePauseKind.WaitingForBatch);
            if (!interrupted && FloorSessionTimes.ShiftEndOf(session.StartedAt, shifts, options) is { } shiftEnd)
            {
                var lastWork = await db.Set<BoyahaneBatchStep>()
                    .Where(s => s.TabletId == session.TabletId && s.CompletedByOperatorId == session.OperatorId && s.CompletedAt >= session.StartedAt)
                    .MaxAsync(s => s.CompletedAt, cancellationToken);
                var earliest = lastWork is { } worked && worked > shiftEnd ? worked : shiftEnd;
                stamp = earliest < now ? earliest : now;
            }

            BoyahaneSessions.Close(session, BoyahaneSessionEndReason.ShiftEnded, stamp);
            await db.SaveChangesAsync(cancellationToken);
            closed++;
        }

        return closed;
    }

    private IQueryable<BoyahaneWorkSessionDto> Project(IQueryable<BoyahaneTabletSession> sessions) => sessions.Select(s => new BoyahaneWorkSessionDto(
        s.Id,
        s.Tablet.TabletCode,
        s.Tablet.Machine != null ? s.Tablet.Machine.Code : null,
        s.Operator.Code,
        s.Operator.Name,
        s.StartedAt,
        s.EndedAt,
        s.EndReason,
        s.Pauses.Where(p => p.EndedAt == null).Select(p => (BoyahanePauseKind?)p.Kind).FirstOrDefault(),
        s.EndedAt == null && db.Set<BoyahaneBatchStep>().Any(x => x.Status == BatchStepStatus.InProgress && x.TabletId == s.TabletId && x.StartedByOperatorId == s.OperatorId)));

    public Task<PagedResult<BoyahaneWorkSessionDto>> GetListAsync(BoyahaneWorkSessionQuery query, CancellationToken cancellationToken)
    {
        var sessions = db.Set<BoyahaneTabletSession>().AsNoTracking();
        if (NullIfBlank(query.Search) is { } term)
        {
            sessions = sessions.Where(s => s.Operator.Code.Contains(term) || s.Operator.Name.Contains(term) || s.Tablet.TabletCode.Contains(term)
                || (s.Tablet.Machine != null && s.Tablet.Machine.Code.Contains(term)));
        }

        return Project(sessions.OrderByDescending(s => s.Id)).ToPagedResultAsync(query, cancellationToken);
    }

    /// <summary>Ofisten kapatma: süren adım işlemde kalır, başka personel oturum açıp bitirebilir.</summary>
    public async Task<Result<BoyahaneWorkSessionDto>> CloseAsync(long id, CancellationToken cancellationToken)
    {
        var session = await db.Set<BoyahaneTabletSession>().Include(s => s.Pauses).SingleOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (session is null)
        {
            return new Error(ErrorCodes.NotFound, "Oturum bulunamadı.", ErrorType.NotFound);
        }

        if (session.EndedAt is not null)
        {
            return new Error("SESSION_NOT_ACTIVE", "Oturum zaten kapatılmış.", ErrorType.Conflict);
        }

        BoyahaneSessions.Close(session, BoyahaneSessionEndReason.ClosedByOffice, timeProvider.GetUtcNow().UtcDateTime);
        await db.SaveChangesAsync(cancellationToken);
        return await Project(db.Set<BoyahaneTabletSession>().AsNoTracking().Where(s => s.Id == id)).SingleAsync(cancellationToken);
    }
}
