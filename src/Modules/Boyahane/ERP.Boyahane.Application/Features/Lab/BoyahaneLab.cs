using System.Linq.Expressions;
using ERP.Boyahane.Domain.Entities;
using ERP.Boyahane.Domain.Enums;
using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Core.Application.Features.MasterData;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using static ERP.Core.Application.Features.MasterData.MasterDataRules;

namespace ERP.Boyahane.Application.Features.Lab;

public sealed record BoyahaneDyeChemicalDto(int Id, string Code, string Name, DyeChemicalKind Kind, decimal? MinStockKilograms, bool IsActive);

public sealed record BoyahaneDyeChemicalRequest(string Code, string Name, DyeChemicalKind Kind = DyeChemicalKind.Dye, decimal? MinStockKilograms = null, bool IsActive = true);

public sealed class BoyahaneDyeChemicalRequestValidator : AbstractValidator<BoyahaneDyeChemicalRequest>
{
    public BoyahaneDyeChemicalRequestValidator()
    {
        RuleFor(x => x.Code).ValidCode();
        RuleFor(x => x.Name).ValidName(200);
        RuleFor(x => x.Kind).IsInEnum().WithMessage("Geçerli bir tür seçin.");
        RuleFor(x => x.MinStockKilograms).InclusiveBetween(0, 9_999_999).WithMessage("Kritik stok geçersiz.").When(x => x.MinStockKilograms is not null);
    }
}

public sealed class BoyahaneDyeChemicalService(IAppDbContext db)
    : MasterDataService<BoyahaneDyeChemical, BoyahaneDyeChemicalDto, BoyahaneDyeChemicalRequest>(db)
{
    protected override DbSet<BoyahaneDyeChemical> Set => Db.Set<BoyahaneDyeChemical>();
    protected override string Label => "Boya / kimyasal";
    protected override string CodeField => "code";

    protected override string CodeOf(BoyahaneDyeChemicalRequest request) => request.Code;
    protected override Expression<Func<BoyahaneDyeChemical, bool>> WithCode(string code) => e => e.Code == code;
    protected override Expression<Func<BoyahaneDyeChemical, bool>> Matches(string term) => e => e.Code.Contains(term) || e.Name.Contains(term);
    protected override IQueryable<BoyahaneDyeChemical> Order(IQueryable<BoyahaneDyeChemical> query) => query.OrderBy(e => e.Code);

    protected override Expression<Func<BoyahaneDyeChemical, BoyahaneDyeChemicalDto>> ToDto =>
        e => new BoyahaneDyeChemicalDto(e.Id, e.Code, e.Name, e.Kind, e.MinStockKilograms, e.IsActive);

    protected override Expression<Func<BoyahaneDyeChemical, LookupDto>> ToLookup => e => new LookupDto(e.Id, e.Code, e.Name);

    protected override void Apply(BoyahaneDyeChemicalRequest request, BoyahaneDyeChemical entity, string code)
    {
        entity.Code = code;
        entity.Name = request.Name.Trim();
        entity.Kind = request.Kind;
        entity.MinStockKilograms = request.MinStockKilograms;
        entity.IsActive = request.IsActive;
    }

    protected override async Task<string?> FindUsageAsync(int id, CancellationToken cancellationToken) =>
        await Db.Set<BoyahaneLabTrialItem>().AnyAsync(i => i.DyeChemicalId == id, cancellationToken) ? "laboratuvar denemeleri"
        : await Db.Set<BoyahanePrintRecipeLine>().AnyAsync(l => l.DyeChemicalId == id, cancellationToken) ? "baskı reçeteleri"
        : await Db.Set<BoyahaneStrippingRecipeLine>().AnyAsync(l => l.DyeChemicalId == id, cancellationToken) ? "söküm reçeteleri"
        : null;
}

public sealed class BoyahaneLabRequestQuery : PageRequest
{
    /// <summary>Talep no, sipariş no, renk ya da müşteri adında aranır.</summary>
    public string? Search { get; set; }
}

public sealed record BoyahaneLabRequestListItemDto(
    int Id,
    string RequestNo,
    string OrderNo,
    int OrderId,
    string? CustomerName,
    string ItemCode,
    string? ColorName,
    string? ColorCode,
    int TrialCount,
    string? ApprovedTrialCode,
    LabRequestStatus Status,
    DateTime CreatedAt);

public sealed record BoyahaneLabTrialItemDto(int Id, int DyeChemicalId, string Code, string Name, DyeChemicalKind Kind, decimal Amount);

/// <param name="ReusedFromRequestNo">Laboratuvar atlanıp önceki reçete kullanıldıysa kaynak talep.</param>
public sealed record BoyahaneLabTrialDto(
    int Id,
    string TrialCode,
    string? Notes,
    LabTrialDecision Decision,
    string? DecisionBy,
    CustomerDecisionChannel? DecisionChannel,
    DateOnly? DecisionOn,
    string? DecisionNote,
    string? DecisionRecordedBy,
    string? ReusedFromRequestNo,
    IReadOnlyList<BoyahaneLabTrialItemDto> Items);

public sealed record BoyahaneLabRequestDto(
    int Id,
    string RequestNo,
    int OrderId,
    string OrderNo,
    int OrderLineId,
    string? CustomerName,
    string ItemCode,
    string ItemName,
    string? ColorName,
    string? ColorCode,
    string? TargetNote,
    LabRequestStatus Status,
    IReadOnlyList<BoyahaneLabTrialDto> Trials,
    int? ColorCardId = null,
    string? ColorCardCode = null);

public sealed record CreateBoyahaneLabRequest(int OrderLineId, string? TargetNote = null);

public sealed class CreateBoyahaneLabRequestValidator : AbstractValidator<CreateBoyahaneLabRequest>
{
    public CreateBoyahaneLabRequestValidator()
    {
        RuleFor(x => x.OrderLineId).GreaterThan(0).WithMessage("Sipariş satırı seçilmelidir.");
        RuleFor(x => x.TargetNote).OptionalText(500);
    }
}

public sealed record BoyahaneLabTrialItemRequest(int DyeChemicalId, decimal Amount);

public sealed record BoyahaneLabTrialRequest(IReadOnlyList<BoyahaneLabTrialItemRequest> Items, string? Notes = null);

public sealed class BoyahaneLabTrialRequestValidator : AbstractValidator<BoyahaneLabTrialRequest>
{
    public BoyahaneLabTrialRequestValidator()
    {
        RuleFor(x => x.Notes).OptionalText(500);
        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("En az bir boya ya da kimyasal girilmelidir.")
            .Must(items => items.Count <= 50).WithMessage("En fazla 50 kalem girilebilir.")
            .Must(items => items.All(i => i.DyeChemicalId > 0)).WithMessage("Boya / kimyasal seçilmelidir.")
            .Must(items => items.All(i => i.Amount is > 0 and <= 100_000)).WithMessage("Miktar sıfırdan büyük olmalıdır.")
            .Must(items => items.Select(i => i.DyeChemicalId).Distinct().Count() == items.Count).WithMessage("Aynı boya / kimyasal iki kez girilemez.");
    }
}

/// <param name="Accepted">Müşteri (stoğa üretimde yetkili) denemeyi kabul etti mi.</param>
public sealed record BoyahaneLabDecisionRequest(
    bool Accepted,
    string DecidedBy,
    CustomerDecisionChannel Channel,
    DateOnly DecidedOn,
    string? Note = null);

public sealed class BoyahaneLabDecisionRequestValidator : AbstractValidator<BoyahaneLabDecisionRequest>
{
    public BoyahaneLabDecisionRequestValidator()
    {
        RuleFor(x => x.DecidedBy).NotEmpty().WithMessage("Kararı veren kişi zorunludur.").MaximumLength(100).WithMessage("En fazla 100 karakter olabilir.");
        RuleFor(x => x.Channel).IsInEnum().WithMessage("Geçerli bir iletişim yolu seçin.");
        RuleFor(x => x.DecidedOn).NotEmpty().WithMessage("Karar tarihi zorunludur.");
        RuleFor(x => x.Note).OptionalText(500);
    }
}

/// <summary>Aynı kart ve renk (aynı renk kartı ya da aynı müşterinin rengi) için daha önce onaylanmış reçete.</summary>
public sealed record BoyahanePreviousRecipeDto(
    int TrialId,
    string RequestNo,
    string TrialCode,
    string OrderNo,
    DateOnly? DecisionOn,
    IReadOnlyList<BoyahaneLabTrialItemDto> Items);

public sealed record ReuseBoyahaneRecipeRequest(int OrderLineId, int TrialId);

public interface IBoyahaneLabService
{
    Task<PagedResult<BoyahaneLabRequestListItemDto>> GetListAsync(BoyahaneLabRequestQuery query, CancellationToken cancellationToken);
    Task<Result<BoyahaneLabRequestDto>> GetAsync(int id, CancellationToken cancellationToken);
    Task<Result<BoyahaneLabRequestDto>> CreateAsync(CreateBoyahaneLabRequest request, CancellationToken cancellationToken);
    Task<Result<BoyahaneLabRequestDto>> CancelAsync(int id, CancellationToken cancellationToken);
    Task<Result<BoyahaneLabRequestDto>> ReopenAsync(int id, CancellationToken cancellationToken);
    Task<Result<BoyahaneLabRequestDto>> AddTrialAsync(int id, BoyahaneLabTrialRequest request, CancellationToken cancellationToken);
    Task<Result<BoyahaneLabRequestDto>> UpdateTrialAsync(int id, int trialId, BoyahaneLabTrialRequest request, CancellationToken cancellationToken);
    Task<Result<BoyahaneLabRequestDto>> DeleteTrialAsync(int id, int trialId, CancellationToken cancellationToken);
    Task<Result<BoyahaneLabRequestDto>> DecideAsync(int id, int trialId, BoyahaneLabDecisionRequest request, CancellationToken cancellationToken);
    Task<Result<IReadOnlyList<BoyahanePreviousRecipeDto>>> GetPreviousRecipesAsync(int orderLineId, CancellationToken cancellationToken);
    Task<Result<BoyahaneLabRequestDto>> ReuseRecipeAsync(ReuseBoyahaneRecipeRequest request, CancellationToken cancellationToken);
}

public sealed class BoyahaneLabService(IAppDbContext db, IDocumentNumberGenerator numbers, ICurrentUser currentUser) : IBoyahaneLabService
{
    private const string Series = "BoyahaneLabRequest";
    private const string InvalidState = "BOYAHANE_LAB_INVALID_STATE";
    private const int MaxPreviousRecipes = 5;
    private const int MaxTrials = 26;

    private static readonly Error NotFound = new(ErrorCodes.NotFound, "Laboratuvar talebi bulunamadı.", ErrorType.NotFound);
    private static readonly Error TrialNotFound = new(ErrorCodes.NotFound, "Deneme bulunamadı.", ErrorType.NotFound);
    private static readonly Error LineNotFound = new(ErrorCodes.NotFound, "Sipariş satırı bulunamadı.", ErrorType.NotFound);

    public Task<PagedResult<BoyahaneLabRequestListItemDto>> GetListAsync(BoyahaneLabRequestQuery query, CancellationToken cancellationToken)
    {
        var requests = db.Set<BoyahaneLabRequest>().AsNoTracking();
        if (NullIfBlank(query.Search) is { } term)
        {
            requests = requests.Where(r => r.RequestNo.Contains(term)
                || r.OrderLine.Order.OrderNo.Contains(term)
                || (r.OrderLine.ColorName != null && r.OrderLine.ColorName.Contains(term))
                || (r.OrderLine.Order.Customer != null && r.OrderLine.Order.Customer.Name.Contains(term)));
        }

        return requests
            .OrderByDescending(r => r.Id)
            .Select(r => new BoyahaneLabRequestListItemDto(
                r.Id,
                r.RequestNo,
                r.OrderLine.Order.OrderNo,
                r.OrderLine.OrderId,
                r.OrderLine.Order.Customer != null ? r.OrderLine.Order.Customer.Name : null,
                r.OrderLine.Fabric != null ? r.OrderLine.Fabric.FabricCode : r.OrderLine.Yarn != null ? r.OrderLine.Yarn.YarnCode : "",
                r.OrderLine.ColorName,
                r.OrderLine.ColorCode,
                r.Trials.Count,
                r.Trials.Where(t => t.Decision == LabTrialDecision.Accepted).Select(t => t.TrialCode).FirstOrDefault(),
                r.Status,
                r.CreatedAt))
            .ToPagedResultAsync(query, cancellationToken);
    }

    public async Task<Result<BoyahaneLabRequestDto>> GetAsync(int id, CancellationToken cancellationToken)
    {
        var request = await db.Set<BoyahaneLabRequest>().AsNoTracking()
            .Include(r => r.OrderLine).ThenInclude(l => l.Order).ThenInclude(o => o.Customer)
            .Include(r => r.OrderLine).ThenInclude(l => l.Fabric)
            .Include(r => r.OrderLine).ThenInclude(l => l.Yarn)
            .Include(r => r.OrderLine).ThenInclude(l => l.ColorCard)
            .Include(r => r.Trials).ThenInclude(t => t.Items).ThenInclude(i => i.DyeChemical)
            .Include(r => r.Trials).ThenInclude(t => t.ReusedFromTrial!).ThenInclude(t => t.Request)
            .SingleOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (request is null)
        {
            return NotFound;
        }

        var line = request.OrderLine;
        return new BoyahaneLabRequestDto(
            request.Id,
            request.RequestNo,
            line.OrderId,
            line.Order.OrderNo,
            line.Id,
            line.Order.Customer?.Name,
            line.Fabric?.FabricCode ?? line.Yarn?.YarnCode ?? "",
            line.Fabric?.Name ?? line.Yarn?.Name ?? "",
            line.ColorName,
            line.ColorCode,
            request.TargetNote,
            request.Status,
            request.Trials.OrderBy(t => t.Id).Select(t => new BoyahaneLabTrialDto(
                t.Id,
                t.TrialCode,
                t.Notes,
                t.Decision,
                t.DecisionBy,
                t.DecisionChannel,
                t.DecisionOn,
                t.DecisionNote,
                t.DecisionRecordedBy,
                t.ReusedFromTrial?.Request.RequestNo,
                ItemsOf(t))).ToList(),
            line.ColorCardId,
            line.ColorCard?.Code);
    }

    public async Task<Result<BoyahaneLabRequestDto>> CreateAsync(CreateBoyahaneLabRequest request, CancellationToken cancellationToken)
    {
        if (await RequireOpenableLineAsync(request.OrderLineId, cancellationToken) is { } error)
        {
            return error;
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var entity = await NewRequestAsync(request.OrderLineId, request.TargetNote, LabRequestStatus.Open, cancellationToken);
        db.Set<BoyahaneLabRequest>().Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetAsync(entity.Id, cancellationToken);
    }

    public async Task<Result<BoyahaneLabRequestDto>> CancelAsync(int id, CancellationToken cancellationToken)
    {
        var request = await db.Set<BoyahaneLabRequest>().SingleOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (request is null)
        {
            return NotFound;
        }

        if (request.Status != LabRequestStatus.Open)
        {
            return new Error(InvalidState, "Yalnızca açık talep iptal edilebilir.", ErrorType.Conflict);
        }

        request.Status = LabRequestStatus.Cancelled;
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    /// <summary>Yanlış onayı geri alır: kabul edilen deneme karar bekler duruma döner, talep yeniden açılır.</summary>
    public async Task<Result<BoyahaneLabRequestDto>> ReopenAsync(int id, CancellationToken cancellationToken)
    {
        var request = await db.Set<BoyahaneLabRequest>().Include(r => r.Trials).SingleOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (request is null)
        {
            return NotFound;
        }

        if (request.Status != LabRequestStatus.Approved)
        {
            return new Error(InvalidState, "Yalnızca onaylanmış talep yeniden açılabilir.", ErrorType.Conflict);
        }

        var trialIds = request.Trials.Select(t => t.Id).ToList();
        if (await db.Set<BoyahaneBatch>().AnyAsync(b => b.Status != BatchStatus.Cancelled && b.RecipeTrialId != null && trialIds.Contains(b.RecipeTrialId.Value), cancellationToken))
        {
            return new Error(InvalidState, "Bu reçeteyle açılmış parti var; önce partiyi iptal edin.", ErrorType.Conflict);
        }

        foreach (var trial in request.Trials.Where(t => t.Decision == LabTrialDecision.Accepted))
        {
            trial.Decision = LabTrialDecision.Pending;
            trial.DecisionBy = null;
            trial.DecisionChannel = null;
            trial.DecisionOn = null;
            trial.DecisionNote = null;
            trial.DecisionRecordedBy = null;
        }

        request.Status = LabRequestStatus.Open;
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    public async Task<Result<BoyahaneLabRequestDto>> AddTrialAsync(int id, BoyahaneLabTrialRequest request, CancellationToken cancellationToken)
    {
        var entity = await db.Set<BoyahaneLabRequest>().Include(r => r.Trials).SingleOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (entity is null)
        {
            return NotFound;
        }

        if (entity.Status != LabRequestStatus.Open)
        {
            return new Error(InvalidState, "Yalnızca açık talebe deneme eklenebilir.", ErrorType.Conflict);
        }

        if (entity.Trials.Count >= MaxTrials)
        {
            return new Error(InvalidState, $"Bir talepte en fazla {MaxTrials} deneme olabilir.", ErrorType.Conflict);
        }

        if (await ValidateItemsAsync(request, cancellationToken) is { } error)
        {
            return error;
        }

        // Mevcut en büyük harfin bir sonrası verilir.
        var next = entity.Trials.Count == 0 ? 'A' : (char)(entity.Trials.Max(t => t.TrialCode[0]) + 1);
        if (next > 'Z')
        {
            return new Error(InvalidState, $"Bir talepte en fazla {MaxTrials} deneme olabilir.", ErrorType.Conflict);
        }

        var trial = new BoyahaneLabTrial { RequestId = id, TrialCode = next.ToString(), Notes = NullIfBlank(request.Notes) };
        foreach (var item in request.Items)
        {
            trial.Items.Add(new BoyahaneLabTrialItem { DyeChemicalId = item.DyeChemicalId, Amount = item.Amount });
        }

        db.Set<BoyahaneLabTrial>().Add(trial);
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    public async Task<Result<BoyahaneLabRequestDto>> UpdateTrialAsync(int id, int trialId, BoyahaneLabTrialRequest request, CancellationToken cancellationToken)
    {
        var trial = await db.Set<BoyahaneLabTrial>().Include(t => t.Items).SingleOrDefaultAsync(t => t.Id == trialId && t.RequestId == id, cancellationToken);
        if (trial is null)
        {
            return TrialNotFound;
        }

        if (trial.Decision != LabTrialDecision.Pending)
        {
            return new Error(InvalidState, "Karar verilmiş deneme değiştirilemez.", ErrorType.Conflict);
        }

        if (await ValidateItemsAsync(request, cancellationToken) is { } error)
        {
            return error;
        }

        trial.Notes = NullIfBlank(request.Notes);
        db.Set<BoyahaneLabTrialItem>().RemoveRange(trial.Items);
        foreach (var item in request.Items)
        {
            db.Set<BoyahaneLabTrialItem>().Add(new BoyahaneLabTrialItem { TrialId = trialId, DyeChemicalId = item.DyeChemicalId, Amount = item.Amount });
        }

        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    public async Task<Result<BoyahaneLabRequestDto>> DeleteTrialAsync(int id, int trialId, CancellationToken cancellationToken)
    {
        var trial = await db.Set<BoyahaneLabTrial>().Include(t => t.Items).SingleOrDefaultAsync(t => t.Id == trialId && t.RequestId == id, cancellationToken);
        if (trial is null)
        {
            return TrialNotFound;
        }

        if (trial.Decision != LabTrialDecision.Pending)
        {
            return new Error(InvalidState, "Karar verilmiş deneme silinemez.", ErrorType.Conflict);
        }

        db.Set<BoyahaneLabTrialItem>().RemoveRange(trial.Items);
        db.Set<BoyahaneLabTrial>().Remove(trial);
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    public async Task<Result<BoyahaneLabRequestDto>> DecideAsync(int id, int trialId, BoyahaneLabDecisionRequest request, CancellationToken cancellationToken)
    {
        var entity = await db.Set<BoyahaneLabRequest>().Include(r => r.Trials).SingleOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (entity is null)
        {
            return NotFound;
        }

        var trial = entity.Trials.SingleOrDefault(t => t.Id == trialId);
        if (trial is null)
        {
            return TrialNotFound;
        }

        if (entity.Status != LabRequestStatus.Open || trial.Decision != LabTrialDecision.Pending)
        {
            return new Error(InvalidState, "Karar yalnızca açık talepte, karar bekleyen deneme için girilir.", ErrorType.Conflict);
        }

        trial.Decision = request.Accepted ? LabTrialDecision.Accepted : LabTrialDecision.Rejected;
        trial.DecisionBy = request.DecidedBy.Trim();
        trial.DecisionChannel = request.Channel;
        trial.DecisionOn = request.DecidedOn;
        trial.DecisionNote = NullIfBlank(request.Note);
        trial.DecisionRecordedBy = currentUser.UserName;
        if (request.Accepted)
        {
            entity.Status = LabRequestStatus.Approved;
        }

        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    public async Task<Result<IReadOnlyList<BoyahanePreviousRecipeDto>>> GetPreviousRecipesAsync(int orderLineId, CancellationToken cancellationToken)
    {
        var line = await db.Set<BoyahaneOrderLine>().AsNoTracking().Include(l => l.Order).SingleOrDefaultAsync(l => l.Id == orderLineId, cancellationToken);
        if (line is null)
        {
            return LineNotFound;
        }

        var trials = await SameRecipeTrials(line)
            .Include(t => t.Request).ThenInclude(r => r.OrderLine).ThenInclude(l => l.Order)
            .Include(t => t.Items).ThenInclude(i => i.DyeChemical)
            .OrderByDescending(t => t.DecisionOn).ThenByDescending(t => t.Id)
            .Take(MaxPreviousRecipes)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return trials.Select(t => new BoyahanePreviousRecipeDto(
            t.Id, t.Request.RequestNo, t.TrialCode, t.Request.OrderLine.Order.OrderNo, t.DecisionOn, ItemsOf(t))).ToList();
    }

    /// <summary>Laboratuvarı atlar: önceki onaylı reçete kopyalanır, talep onaylı açılır. Karar kaynağı kayıtta görünür.</summary>
    public async Task<Result<BoyahaneLabRequestDto>> ReuseRecipeAsync(ReuseBoyahaneRecipeRequest request, CancellationToken cancellationToken)
    {
        if (await RequireOpenableLineAsync(request.OrderLineId, cancellationToken) is { } error)
        {
            return error;
        }

        var line = await db.Set<BoyahaneOrderLine>().AsNoTracking().Include(l => l.Order).SingleAsync(l => l.Id == request.OrderLineId, cancellationToken);
        var source = await SameRecipeTrials(line)
            .Include(t => t.Items)
            .Include(t => t.Request)
            .AsNoTracking()
            .SingleOrDefaultAsync(t => t.Id == request.TrialId, cancellationToken);
        if (source is null)
        {
            return new Error(InvalidState, "Bu reçete aynı kart ve renge (aynı renk kartı ya da aynı müşterinin rengi) ait değil.", ErrorType.Conflict);
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var entity = await NewRequestAsync(request.OrderLineId, null, LabRequestStatus.Approved, cancellationToken);
        var trial = new BoyahaneLabTrial
        {
            TrialCode = "A",
            ReusedFromTrialId = source.Id,
            Decision = LabTrialDecision.Accepted,
            DecisionBy = $"Önceki reçete: {source.Request.RequestNo}/{source.TrialCode}",
            DecisionRecordedBy = currentUser.UserName,
        };
        foreach (var item in source.Items)
        {
            trial.Items.Add(new BoyahaneLabTrialItem { DyeChemicalId = item.DyeChemicalId, Amount = item.Amount });
        }

        entity.Trials.Add(trial);
        db.Set<BoyahaneLabRequest>().Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetAsync(entity.Id, cancellationToken);
    }

    private async Task<BoyahaneLabRequest> NewRequestAsync(int orderLineId, string? targetNote, LabRequestStatus status, CancellationToken cancellationToken)
    {
        var year = DateTime.UtcNow.Year;
        var sequence = await numbers.NextAsync(Series, year, cancellationToken);
        return new BoyahaneLabRequest
        {
            RequestNo = $"LB-{year}-{sequence:D5}",
            OrderLineId = orderLineId,
            TargetNote = NullIfBlank(targetNote),
            Status = status,
        };
    }

    /// <summary>Talep yalnızca renk boyama satırına, iptal edilmemiş ve kapatılmamış siparişte ve satırda etkin talep yokken açılır.</summary>
    private async Task<Error?> RequireOpenableLineAsync(int orderLineId, CancellationToken cancellationToken)
    {
        var line = await db.Set<BoyahaneOrderLine>().AsNoTracking().Include(l => l.Order).SingleOrDefaultAsync(l => l.Id == orderLineId, cancellationToken);
        if (line is null)
        {
            return LineNotFound;
        }

        if (line.ProcessType is not (OrderProcessType.FabricDyeing or OrderProcessType.YarnDyeing))
        {
            return new Error(InvalidState, "Laboratuvar talebi yalnızca kumaş ve iplik boyama satırına açılır.", ErrorType.Conflict);
        }

        if (line.Order.Status is OrderStatus.Cancelled or OrderStatus.Closed)
        {
            return new Error(InvalidState, "İptal edilmiş ya da kapatılmış siparişe talep açılamaz.", ErrorType.Conflict);
        }

        var active = await db.Set<BoyahaneLabRequest>().AnyAsync(r => r.OrderLineId == orderLineId && r.Status != LabRequestStatus.Cancelled, cancellationToken);
        return active ? new Error(InvalidState, "Bu satırda zaten etkin bir laboratuvar talebi var.", ErrorType.Conflict) : null;
    }

    /// <summary>
    /// Aynı kart (kumaş / iplik) için başka satırlarda onaylanmış denemeler: aynı renk kartını taşıyanlar (müşteri fark etmez)
    /// ya da aynı müşteri ve aynı renk (renk kodu varsa kod, yoksa ad).
    /// </summary>
    private IQueryable<BoyahaneLabTrial> SameRecipeTrials(BoyahaneOrderLine line)
    {
        var trials = db.Set<BoyahaneLabTrial>().Where(t =>
            t.Decision == LabTrialDecision.Accepted
            && t.Request.Status == LabRequestStatus.Approved
            && t.Request.OrderLineId != line.Id
            && t.Request.OrderLine.FabricId == line.FabricId
            && t.Request.OrderLine.YarnId == line.YarnId);

        var customerId = line.Order.CustomerId;
        var cardId = line.ColorCardId;
        return line.ColorCode is { } code
            ? trials.Where(t => (cardId != null && t.Request.OrderLine.ColorCardId == cardId)
                || (t.Request.OrderLine.Order.CustomerId == customerId && t.Request.OrderLine.ColorCode == code))
            : trials.Where(t => (cardId != null && t.Request.OrderLine.ColorCardId == cardId)
                || (t.Request.OrderLine.Order.CustomerId == customerId && t.Request.OrderLine.ColorName == line.ColorName));
    }

    private async Task<Error?> ValidateItemsAsync(BoyahaneLabTrialRequest request, CancellationToken cancellationToken)
    {
        foreach (var id in request.Items.Select(i => i.DyeChemicalId))
        {
            if (await ReferenceValidation.RequireSelectableAsync(
                    db.Set<BoyahaneDyeChemical>(), id, null, "items", "Boya / kimyasal", cancellationToken) is { } error)
            {
                return error;
            }
        }

        return null;
    }

    private static List<BoyahaneLabTrialItemDto> ItemsOf(BoyahaneLabTrial trial) =>
        trial.Items.OrderBy(i => i.DyeChemical.Kind).ThenBy(i => i.Id).Select(i => new BoyahaneLabTrialItemDto(
            i.Id, i.DyeChemicalId, i.DyeChemical.Code, i.DyeChemical.Name, i.DyeChemical.Kind, i.Amount)).ToList();
}
