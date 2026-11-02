using System.Linq.Expressions;
using ERP.Boyahane.Application.Features.Stock;
using ERP.Boyahane.Domain.Authorization;
using ERP.Boyahane.Domain.Entities;
using ERP.Boyahane.Domain.Enums;
using ERP.Boyahane.Domain.Rules;
using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Core.Application.Features.MasterData;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using static ERP.Core.Application.Features.MasterData.MasterDataRules;

namespace ERP.Boyahane.Application.Features.Batches;

public sealed record BoyahaneMachineDto(
    int Id,
    string Code,
    string Name,
    DyeMachineType MachineType,
    decimal CapacityKg,
    decimal? DefaultLiquorRatio,
    DyeMachineStatus Status,
    int OperationCount,
    bool IsActive);

public sealed record BoyahaneMachineRequest(
    string Code,
    string Name,
    decimal CapacityKg,
    DyeMachineType MachineType = DyeMachineType.Jet,
    decimal? DefaultLiquorRatio = null,
    bool IsActive = true);

public sealed class BoyahaneMachineRequestValidator : AbstractValidator<BoyahaneMachineRequest>
{
    public BoyahaneMachineRequestValidator()
    {
        RuleFor(x => x.Code).ValidCode();
        RuleFor(x => x.Name).ValidName(200);
        RuleFor(x => x.MachineType).IsInEnum().WithMessage("Geçerli bir makine türü seçin.");
        RuleFor(x => x.CapacityKg).InclusiveBetween(0.01m, 1_000_000).WithMessage("Kapasite sıfırdan büyük olmalıdır.");
        RuleFor(x => x.DefaultLiquorRatio).InclusiveBetween(0.1m, 100).WithMessage("Flotte oranı 0,1 ile 100 arasında olmalıdır.").When(x => x.DefaultLiquorRatio is not null);
    }
}

public sealed class BoyahaneMachineService(IAppDbContext db)
    : MasterDataService<BoyahaneMachine, BoyahaneMachineDto, BoyahaneMachineRequest>(db)
{
    protected override DbSet<BoyahaneMachine> Set => Db.Set<BoyahaneMachine>();
    protected override string Label => "Makine";
    protected override string CodeField => "code";

    protected override string CodeOf(BoyahaneMachineRequest request) => request.Code;
    protected override Expression<Func<BoyahaneMachine, bool>> WithCode(string code) => e => e.Code == code;
    protected override Expression<Func<BoyahaneMachine, bool>> Matches(string term) => e => e.Code.Contains(term) || e.Name.Contains(term);
    protected override IQueryable<BoyahaneMachine> Order(IQueryable<BoyahaneMachine> query) => query.OrderBy(e => e.Code);

    protected override Expression<Func<BoyahaneMachine, BoyahaneMachineDto>> ToDto =>
        e => new BoyahaneMachineDto(e.Id, e.Code, e.Name, e.MachineType, e.CapacityKg, e.DefaultLiquorRatio, e.Status, Db.Set<BoyahaneMachineOperation>().Count(o => o.MachineId == e.Id), e.IsActive);

    protected override Expression<Func<BoyahaneMachine, LookupDto>> ToLookup => e => new LookupDto(e.Id, e.Code, e.Name);

    protected override void Apply(BoyahaneMachineRequest request, BoyahaneMachine entity, string code)
    {
        entity.Code = code;
        entity.Name = request.Name.Trim();
        entity.MachineType = request.MachineType;
        entity.CapacityKg = request.CapacityKg;
        entity.DefaultLiquorRatio = request.DefaultLiquorRatio;
        entity.IsActive = request.IsActive;
    }

    protected override async Task<string?> FindUsageAsync(int id, CancellationToken cancellationToken) =>
        await Db.Set<BoyahaneBatch>().AnyAsync(b => b.MachineId == id, cancellationToken) ? "partiler" : null;
}

public sealed class BoyahaneBatchQuery : PageRequest
{
    /// <summary>Parti no ya da makinede aranır.</summary>
    public string? Search { get; set; }
}

public sealed record BoyahaneBatchListItemDto(
    int Id,
    string BatchNo,
    string MachineCode,
    DateOnly? PlannedDate,
    int ItemCount,
    decimal TotalKilograms,
    string ItemCode,
    string? ColorName,
    OrderProcessType ProcessType,
    BatchStatus Status,
    string? CurrentStep,
    DateTime CreatedAt);

public sealed record BoyahaneBatchItemDto(
    int Id,
    int OrderId,
    string OrderNo,
    string? CustomerName,
    string ItemNo,
    string? ColorName,
    decimal Kilograms,
    decimal? Meters,
    FinalQualityResult? FinalResult,
    string? FinalNote,
    decimal? FinalKilograms,
    decimal? FinalMeters,
    decimal? FireKilogramPercent,
    decimal? FireMeterPercent,
    decimal? FireTolerancePercent,
    bool OverTolerance);

public sealed record BoyahaneBatchConsumptionLotDto(string LotNo, string? SupplierLotNo, decimal Kilograms);

/// <param name="WithoutLotKilograms">Stok yetmediği için lottan düşülemeyen miktar.</param>
public sealed record BoyahaneBatchConsumptionDto(string Code, string Name, decimal PlannedKilograms, decimal ActualKilograms, decimal WithoutLotKilograms, IReadOnlyList<BoyahaneBatchConsumptionLotDto> Lots);

public sealed record BoyahaneBatchCostDto(string Currency, decimal Amount);

public sealed record BoyahaneBatchStepDto(
    int Id,
    int Sequence,
    string OperationCode,
    string OperationName,
    OperationBehavior Behavior,
    ColorCheckResult? ReworkKind,
    BatchStepStatus Status,
    bool NeedsMachine,
    string? MachineCode,
    string? StartedBy,
    string? CompletedBy,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    ColorCheckResult? ColorResult,
    string? Note,
    IReadOnlyList<BoyahaneBatchConsumptionDto> Consumptions,
    string? StrippingRecipeCode = null,
    string? StrippingRecipeName = null,
    IReadOnlyList<BoyahaneBatchStrippingLineDto>? StrippingLines = null);

/// <summary>Söküm adımında uygulanacak reçete satırı, parti ölçeğinde.</summary>
/// <param name="RequiredKilograms">Partinin kilosu ve flottesine göre gereken miktar.</param>
/// <param name="UsableKilograms">Depoda kullanılabilir stok.</param>
public sealed record BoyahaneBatchStrippingLineDto(
    int DyeChemicalId,
    string Code,
    string Name,
    decimal Amount,
    RecipeAmountUnit Unit,
    int? TemperatureCelsius,
    int? DurationMinutes,
    decimal RequiredKilograms,
    decimal UsableKilograms);

/// <summary>Baskı reçetesi satırı, parti ölçeğinde.</summary>
/// <param name="RequiredKilograms">Rengin pasta kilosuna göre gereken miktar.</param>
public sealed record BoyahaneBatchPrintLineDto(int DyeChemicalId, string Code, string Name, decimal GramsPerKilogram, decimal RequiredKilograms);

/// <summary>Baskı partisinde bir renk (şablon / silindir): metre başına pasta, gereken pasta ve pasta reçetesi.</summary>
/// <param name="PasteKilograms">Parti metresi × metre başına pasta; tahmin girilmemişse boş.</param>
public sealed record BoyahaneBatchPrintColorDto(
    int Position,
    string Name,
    string? ColorCardCode,
    decimal? PasteKilogramsPerMeter,
    decimal? PasteKilograms,
    IReadOnlyList<BoyahaneBatchPrintLineDto> Lines);

/// <summary>Baskı partisinin boya / kimyasal toplamı (bütün renkler); baskı adımında tüketim buna göre hesaplanır.</summary>
public sealed record BoyahaneBatchPrintChemicalDto(int DyeChemicalId, string Code, string Name, decimal RequiredKilograms, decimal UsableKilograms);

/// <param name="RequiredKilograms">Partinin kilosu ve flottesine göre gereken miktar.</param>
/// <param name="UsableKilograms">Depoda kullanılabilir stok.</param>
public sealed record BoyahaneBatchRecipeLineDto(int DyeChemicalId, string Code, string Name, DyeChemicalKind Kind, decimal Amount, decimal RequiredKilograms, decimal UsableKilograms);

public sealed record BoyahaneBatchDto(
    int Id,
    string BatchNo,
    int MachineId,
    string MachineCode,
    string MachineName,
    decimal MachineCapacityKg,
    OrderProcessType ProcessType,
    decimal? LiquorRatio,
    decimal? LiquorLiters,
    DateOnly? PlannedDate,
    string? Notes,
    BatchStatus Status,
    string? CapacityOverrideBy,
    string? CapacityOverrideNote,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    int? RouteId,
    string? RouteName,
    string? RecipeNo,
    decimal TotalKilograms,
    decimal? TotalMeters,
    string? CardUid,
    int? ReworkOfBatchId,
    string? ReworkOfBatchNo,
    int? CurrentStepId,
    IReadOnlyList<BoyahaneBatchItemDto> Items,
    IReadOnlyList<BoyahaneBatchRecipeLineDto> Recipe,
    IReadOnlyList<BoyahaneBatchStepDto> Steps,
    IReadOnlyList<BoyahaneBatchCostDto> ChemicalCosts,
    bool HasUncostedConsumption,
    string? PatternCode = null,
    string? PatternName = null,
    string? ColorwayCode = null,
    string? ColorwayName = null,
    IReadOnlyList<BoyahaneBatchPrintColorDto>? PrintColors = null,
    IReadOnlyList<BoyahaneBatchPrintChemicalDto>? PrintRecipe = null);

/// <summary>Partiye alınabilecek mal. Aynı <paramref name="GroupKey"/> taşıyanlar birlikte boyanabilir.</summary>
/// <param name="GroupKey">Kart ve reçete içeriğinden üretilir.</param>
public sealed record BoyahaneBatchCandidateDto(
    int? RollId,
    int? YarnLotId,
    string ItemNo,
    string GroupKey,
    string GroupTitle,
    int OrderId,
    string OrderNo,
    string? CustomerName,
    string? ColorName,
    decimal Kilograms,
    decimal? Meters,
    bool UsesRecipe,
    OrderProcessType ProcessType,
    bool Selected);

/// <param name="LiquorRatio">Boşsa makinenin varsayılan flotte oranı kullanılır.</param>
/// <param name="CapacityOverrideNote">Kapasite aşılıyorsa gerekçe; ayrı yetki ister.</param>
public sealed record BoyahaneBatchRequest(
    int MachineId,
    IReadOnlyList<int>? RollIds = null,
    IReadOnlyList<int>? YarnLotIds = null,
    decimal? LiquorRatio = null,
    int? RouteId = null,
    DateOnly? PlannedDate = null,
    string? Notes = null,
    string? CapacityOverrideNote = null);

public sealed class BoyahaneBatchRequestValidator : AbstractValidator<BoyahaneBatchRequest>
{
    public BoyahaneBatchRequestValidator()
    {
        RuleFor(x => x.MachineId).GreaterThan(0).WithMessage("Makine seçilmelidir.");
        RuleFor(x => x.LiquorRatio).InclusiveBetween(0.1m, 100).WithMessage("Flotte oranı 0,1 ile 100 arasında olmalıdır.").When(x => x.LiquorRatio is not null);
        RuleFor(x => x.Notes).OptionalText(1000);
        RuleFor(x => x.CapacityOverrideNote).OptionalText(500);
        RuleFor(x => x)
            .Must(x => (x.RollIds?.Count ?? 0) + (x.YarnLotIds?.Count ?? 0) > 0)
            .WithName("items").WithMessage("Partiye en az bir top ya da lot seçilmelidir.");
    }
}

public interface IBoyahaneBatchService
{
    Task<PagedResult<BoyahaneBatchListItemDto>> GetListAsync(BoyahaneBatchQuery query, CancellationToken cancellationToken);
    Task<IReadOnlyList<BoyahaneBatchListItemDto>> GetPlanAsync(CancellationToken cancellationToken);
    Task<Result<BoyahaneBatchDto>> GetAsync(int id, CancellationToken cancellationToken);
    Task<IReadOnlyList<BoyahaneBatchCandidateDto>> GetCandidatesAsync(int? batchId, CancellationToken cancellationToken);
    Task<Result<BoyahaneBatchDto>> CreateAsync(BoyahaneBatchRequest request, CancellationToken cancellationToken);
    Task<Result<BoyahaneBatchDto>> UpdateAsync(int id, BoyahaneBatchRequest request, CancellationToken cancellationToken);
    Task<Result<BoyahaneBatchDto>> SetCardAsync(int id, SetBatchCardRequest request, CancellationToken cancellationToken);
    Task<Result<BoyahaneBatchDto>> FindByCardAsync(string code, CancellationToken cancellationToken);
    Task<Result<BoyahaneBatchDto>> StartStepAsync(int batchId, int stepId, BatchStepActor actor, Guid? requestId, CancellationToken cancellationToken);
    Task<Result<BoyahaneBatchDto>> CompleteStepAsync(int batchId, int stepId, CompleteBatchStepRequest request, BatchStepActor actor, CancellationToken cancellationToken);
    Task<Result<BoyahaneBatchDto>> SkipStepAsync(int batchId, int stepId, CancellationToken cancellationToken);
    Task<Result<BoyahaneBatchDto>> CancelAsync(int id, CancellationToken cancellationToken);
}

public sealed partial class BoyahaneBatchService(
    IAppDbContext db,
    IDocumentNumberGenerator numbers,
    ICurrentUser currentUser,
    ICurrentPermissions permissions,
    IBoyahaneChemicalStockService chemicalStock,
    TimeProvider timeProvider) : IBoyahaneBatchService
{
    public const string OverCapacity = "BATCH_OVER_CAPACITY";
    private const string Series = "BoyahaneBatch";
    private const string InvalidState = "BATCH_INVALID_STATE";
    private const string InvalidItem = "BATCH_INVALID_ITEM";
    private const int MaxCandidates = 1000;

    private static readonly Error NotFound = new(ErrorCodes.NotFound, "Parti bulunamadı.", ErrorType.NotFound);

    public Task<PagedResult<BoyahaneBatchListItemDto>> GetListAsync(BoyahaneBatchQuery query, CancellationToken cancellationToken)
    {
        var batches = db.Set<BoyahaneBatch>().AsNoTracking();
        if (NullIfBlank(query.Search) is { } term)
        {
            batches = batches.Where(b => b.BatchNo.Contains(term) || b.Machine.Code.Contains(term) || b.Machine.Name.Contains(term));
        }

        return batches
            .OrderByDescending(b => b.Id)
            .Select(ListItem)
            .ToPagedResultAsync(query, cancellationToken);
    }

    /// <summary>Makine planı: planlanan bütün partiler, sayfasız. Planlanan parti sayısı durumla sınırlıdır.</summary>
    public async Task<IReadOnlyList<BoyahaneBatchListItemDto>> GetPlanAsync(CancellationToken cancellationToken) =>
        await db.Set<BoyahaneBatch>().AsNoTracking()
            .Where(b => b.Status == BatchStatus.Planned)
            .OrderBy(b => b.PlannedDate == null).ThenBy(b => b.PlannedDate).ThenBy(b => b.Id)
            .Select(ListItem)
            .ToListAsync(cancellationToken);

    private static readonly Expression<Func<BoyahaneBatch, BoyahaneBatchListItemDto>> ListItem = b => new BoyahaneBatchListItemDto(
        b.Id,
        b.BatchNo,
        b.Machine.Code,
        b.PlannedDate,
        b.Items.Count,
        b.Items.Sum(i => i.Kilograms),
        b.Items.OrderBy(i => i.Id)
            .Select(i => i.OrderLine.Fabric != null ? i.OrderLine.Fabric.FabricCode : i.OrderLine.Yarn != null ? i.OrderLine.Yarn.YarnCode : "")
            .FirstOrDefault() ?? "",
        b.Items.OrderBy(i => i.Id).Select(i => i.OrderLine.ColorName).FirstOrDefault(),
        b.Items.OrderBy(i => i.Id).Select(i => i.OrderLine.ProcessType).FirstOrDefault(),
        b.Status,
        b.Steps.Where(s => s.Status == BatchStepStatus.InProgress || s.Status == BatchStepStatus.Pending)
            .OrderBy(s => s.Sequence)
            .Select(s => s.Operation.Name)
            .FirstOrDefault(),
        b.CreatedAt);

    public async Task<Result<BoyahaneBatchDto>> GetAsync(int id, CancellationToken cancellationToken)
    {
        var batch = await db.Set<BoyahaneBatch>().AsNoTracking()
            .Include(b => b.Machine)
            .Include(b => b.RecipeTrial).ThenInclude(t => t!.Request)
            .Include(b => b.RecipeTrial).ThenInclude(t => t!.Items).ThenInclude(i => i.DyeChemical)
            .Include(b => b.Items).ThenInclude(i => i.Roll)
            .Include(b => b.Items).ThenInclude(i => i.YarnLot)
            .Include(b => b.Items).ThenInclude(i => i.OrderLine).ThenInclude(l => l.Order).ThenInclude(o => o.Customer)
            .Include(b => b.Items).ThenInclude(i => i.OrderLine).ThenInclude(l => l.Pattern)
            .Include(b => b.Items).ThenInclude(i => i.OrderLine).ThenInclude(l => l.Colorway)
            .Include(b => b.ReworkOfBatch)
            .Include(b => b.Route)
            .Include(b => b.Steps).ThenInclude(s => s.Operation)
            .Include(b => b.Steps).ThenInclude(s => s.Machine)
            .Include(b => b.Steps).ThenInclude(s => s.StartedByOperator)
            .Include(b => b.Steps).ThenInclude(s => s.CompletedByOperator)
            .Include(b => b.Steps).ThenInclude(s => s.StrippingRecipe!).ThenInclude(r => r.Lines).ThenInclude(l => l.DyeChemical)
            .Include(b => b.Steps).ThenInclude(s => s.Consumptions).ThenInclude(c => c.DyeChemical)
            .Include(b => b.Steps).ThenInclude(s => s.Consumptions).ThenInclude(c => c.Lots).ThenInclude(l => l.Lot)
            .SingleOrDefaultAsync(b => b.Id == id, cancellationToken);
        if (batch is null)
        {
            return NotFound;
        }

        var totalKilograms = batch.Items.Sum(i => i.Kilograms);
        var hasMeters = batch.Items.All(i => i.Meters is not null);
        var trial = batch.RecipeTrial;
        var firstLine = batch.Items.OrderBy(i => i.Id).First().OrderLine;
        var printColors = await PrintColorsAsync(firstLine.ColorwayId, cancellationToken);
        var totalMeters = batch.Items.Sum(i => i.Meters ?? 0);
        var printPlan = PrintPlan(printColors, totalMeters);
        var chemicalIds = (trial?.Items.Select(i => i.DyeChemicalId) ?? [])
            .Concat(batch.Steps.Where(s => s.StrippingRecipe is not null).SelectMany(s => s.StrippingRecipe!.Lines.Select(l => l.DyeChemicalId)))
            .Concat(printPlan.Keys)
            .Distinct()
            .ToList();
        var usable = chemicalIds.Count == 0
            ? new Dictionary<int, decimal>()
            : await chemicalStock.GetUsableAsync(chemicalIds, cancellationToken);
        var consumptions = batch.Steps.SelectMany(s => s.Consumptions).ToList();
        var costedLots = consumptions.SelectMany(c => c.Lots).ToList();
        var ratio = batch.LiquorRatio ?? 0;
        var printChemicals = printColors.SelectMany(c => c.Lines).Select(l => l.DyeChemical).DistinctBy(c => c.Id).ToDictionary(c => c.Id);
        return new BoyahaneBatchDto(
            batch.Id,
            batch.BatchNo,
            batch.MachineId,
            batch.Machine.Code,
            batch.Machine.Name,
            batch.Machine.CapacityKg,
            batch.Items.OrderBy(i => i.Id).First().OrderLine.ProcessType,
            batch.LiquorRatio,
            batch.LiquorRatio is null ? null : RecipeScaling.LiquorLiters(totalKilograms, ratio),
            batch.PlannedDate,
            batch.Notes,
            batch.Status,
            batch.CapacityOverrideBy,
            batch.CapacityOverrideNote,
            batch.StartedAt,
            batch.CompletedAt,
            batch.RouteId,
            batch.Route?.Name,
            trial is null ? null : $"{trial.Request.RequestNo}/{trial.TrialCode}",
            totalKilograms,
            hasMeters ? batch.Items.Sum(i => i.Meters) : null,
            batch.CardUid,
            batch.ReworkOfBatchId,
            batch.ReworkOfBatch?.BatchNo,
            batch.Steps.Where(s => s.Status is BatchStepStatus.InProgress or BatchStepStatus.Pending).OrderBy(s => s.Sequence).Select(s => (int?)s.Id).FirstOrDefault(),
            batch.Items.OrderBy(i => i.Id).Select(i => new BoyahaneBatchItemDto(
                i.Id,
                i.OrderLine.OrderId,
                i.OrderLine.Order.OrderNo,
                i.OrderLine.Order.Customer?.Name,
                i.Roll?.RollNo ?? i.YarnLot?.LotNo ?? "",
                i.OrderLine.ColorName,
                i.Kilograms,
                i.Meters,
                i.FinalResult,
                i.FinalNote,
                i.FinalKilograms,
                i.FinalMeters,
                Billing.LossPercent(i.Kilograms, i.FinalKilograms),
                Billing.LossPercent(i.Meters, i.FinalMeters),
                i.OrderLine.Order.Customer?.FireTolerancePercent,
                Billing.LossPercent(i.Kilograms, i.FinalKilograms) > i.OrderLine.Order.Customer?.FireTolerancePercent)).ToList(),
            trial is null ? [] : trial.Items.OrderBy(i => i.DyeChemical.Kind).ThenBy(i => i.Id).Select(i => new BoyahaneBatchRecipeLineDto(
                i.DyeChemicalId,
                i.DyeChemical.Code,
                i.DyeChemical.Name,
                i.DyeChemical.Kind,
                i.Amount,
                RecipeScaling.RequiredKilograms(i.DyeChemical.Kind, i.Amount, totalKilograms, ratio),
                usable.GetValueOrDefault(i.DyeChemicalId))).ToList(),
            batch.Steps.OrderBy(s => s.Sequence).Select(s => new BoyahaneBatchStepDto(
                s.Id,
                s.Sequence,
                s.Operation.Code,
                s.Operation.Name,
                s.Operation.Behavior,
                s.ReworkKind,
                s.Status,
                s.Operation.NeedsMachine,
                s.Machine?.Code,
                s.StartedByOperator?.Name,
                s.CompletedByOperator?.Name,
                s.StartedAt,
                s.CompletedAt,
                s.ColorResult,
                s.Note,
                s.Consumptions.OrderBy(c => c.Id).Select(c => new BoyahaneBatchConsumptionDto(
                    c.DyeChemical.Code,
                    c.DyeChemical.Name,
                    c.PlannedKilograms,
                    c.ActualKilograms,
                    c.ActualKilograms - c.Lots.Sum(l => l.Kilograms),
                    c.Lots.OrderBy(l => l.Id).Select(l => new BoyahaneBatchConsumptionLotDto(l.Lot.LotNo, l.Lot.SupplierLotNo, l.Kilograms)).ToList())).ToList(),
                s.StrippingRecipe?.Code,
                s.StrippingRecipe?.Name,
                s.StrippingRecipe is null ? null : s.StrippingRecipe.Lines.OrderBy(l => l.Sequence).Select(l => new BoyahaneBatchStrippingLineDto(
                    l.DyeChemicalId,
                    l.DyeChemical.Code,
                    l.DyeChemical.Name,
                    l.Amount,
                    l.Unit,
                    l.TemperatureCelsius,
                    l.DurationMinutes,
                    RecipeScaling.RequiredKilograms(l.Unit, l.Amount, totalKilograms, ratio),
                    usable.GetValueOrDefault(l.DyeChemicalId))).ToList())).ToList(),
            costedLots.Where(l => l.Lot.UnitCost is not null)
                .GroupBy(l => l.Lot.Currency)
                .OrderBy(g => g.Key)
                .Select(g => new BoyahaneBatchCostDto(g.Key, Math.Round(g.Sum(l => l.Kilograms * l.Lot.UnitCost!.Value), 2)))
                .ToList(),
            costedLots.Any(l => l.Lot.UnitCost is null) || consumptions.Any(c => c.ActualKilograms > c.Lots.Sum(l => l.Kilograms)),
            firstLine.Pattern?.Code,
            firstLine.Pattern?.Name,
            firstLine.Colorway?.Code,
            firstLine.Colorway?.Name,
            printColors.Select(c =>
            {
                decimal? paste = c.PasteKilogramsPerMeter is { } perMeter ? RecipeScaling.PasteKilograms(totalMeters, perMeter) : null;
                return new BoyahaneBatchPrintColorDto(
                    c.Position,
                    c.Name,
                    c.ColorCard?.Code,
                    c.PasteKilogramsPerMeter,
                    paste,
                    c.Lines.OrderBy(l => l.DyeChemical.Kind).ThenBy(l => l.Id).Select(l => new BoyahaneBatchPrintLineDto(
                        l.DyeChemicalId,
                        l.DyeChemical.Code,
                        l.DyeChemical.Name,
                        l.GramsPerKilogram,
                        paste is { } kilograms ? RecipeScaling.PasteChemicalKilograms(kilograms, l.GramsPerKilogram) : 0)).ToList());
            }).ToList(),
            printPlan.OrderBy(p => printChemicals[p.Key].Code).Select(p => new BoyahaneBatchPrintChemicalDto(
                p.Key,
                printChemicals[p.Key].Code,
                printChemicals[p.Key].Name,
                p.Value,
                usable.GetValueOrDefault(p.Key))).ToList());
    }

    /// <summary>Baskı satırının renk yolundaki renkler ve pasta reçeteleri; renk yolu seçilmemişse boş.</summary>
    private async Task<List<BoyahaneColorwayColor>> PrintColorsAsync(int? colorwayId, CancellationToken cancellationToken) =>
        colorwayId is null
            ? new List<BoyahaneColorwayColor>()
            : await db.Set<BoyahaneColorwayColor>().AsNoTracking()
                .Include(c => c.ColorCard)
                .Include(c => c.Lines).ThenInclude(l => l.DyeChemical)
                .Where(c => c.ColorwayId == colorwayId)
                .OrderBy(c => c.Position)
                .ToListAsync(cancellationToken);

    /// <summary>Baskı tüketim planı: her rengin pastası (metre × kg/m) ve pasta reçetesi (g/kg), boya / kimyasal bazında toplanır.</summary>
    private static Dictionary<int, decimal> PrintPlan(IReadOnlyList<BoyahaneColorwayColor> colors, decimal meters) =>
        colors.Where(c => c.PasteKilogramsPerMeter is not null)
            .SelectMany(c => c.Lines.Select(l => (l.DyeChemicalId, Kilograms: RecipeScaling.PasteChemicalKilograms(RecipeScaling.PasteKilograms(meters, c.PasteKilogramsPerMeter!.Value), l.GramsPerKilogram))))
            .GroupBy(x => x.DyeChemicalId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Kilograms));

    /// <summary>Söküm tüketim planı: reçete satırları parti kilosu ve flottesine göre; aynı kimyasal birden çok aşamadaysa toplanır.</summary>
    private static Dictionary<int, decimal> StrippingPlan(BoyahaneStrippingRecipe? recipe, decimal kilograms, decimal liquorRatio) =>
        recipe is null
            ? new Dictionary<int, decimal>()
            : recipe.Lines
                .GroupBy(l => l.DyeChemicalId)
                .ToDictionary(g => g.Key, g => g.Sum(l => RecipeScaling.RequiredKilograms(l.Unit, l.Amount, kilograms, liquorRatio)));

    public async Task<IReadOnlyList<BoyahaneBatchCandidateDto>> GetCandidatesAsync(int? batchId, CancellationToken cancellationToken)
    {
        // Boyama satırının onaylı reçetesi: talep onaylı ve kabul edilmiş deneme. Baskı ve terbiye reçete istemez.
        var recipes = await ApprovedRecipesAsync(null, cancellationToken);
        var lineIds = recipes.Keys.ToList();

        var busyRollIds = db.Set<BoyahaneBatchItem>().Where(i => i.IsActive && i.BatchId != batchId && i.RollId != null).Select(i => i.RollId);
        var rolls = await db.Set<BoyahaneFabricRoll>().AsNoTracking()
            .Where(t => t.OrderLineId != null
                && (lineIds.Contains(t.OrderLineId!.Value) || t.OrderLine!.ProcessType == OrderProcessType.Printing || t.OrderLine!.ProcessType == OrderProcessType.Finishing)
                && t.Status == FabricRollStatus.Approved
                && t.OrderLine!.Order.Status == OrderStatus.Confirmed
                && !busyRollIds.Contains(t.Id))
            .OrderBy(t => t.RollNo)
            .Take(MaxCandidates)
            .Select(t => new
            {
                t.Id,
                t.RollNo,
                LineId = t.OrderLineId!.Value,
                t.OrderLine!.OrderId,
                t.OrderLine.Order.OrderNo,
                CustomerName = t.OrderLine.Order.Customer != null ? t.OrderLine.Order.Customer.Name : null,
                t.OrderLine.ColorName,
                t.OrderLine.ColorCode,
                t.OrderLine.ProcessType,
                t.OrderLine.FabricId,
                FabricCode = t.OrderLine.Fabric != null ? t.OrderLine.Fabric.FabricCode : "",
                Kilograms = t.InspectedKilograms ?? t.Kilograms,
                Meters = t.InspectedMeters ?? t.Meters,
            })
            .ToListAsync(cancellationToken);

        var busyLotIds = db.Set<BoyahaneBatchItem>().Where(i => i.IsActive && i.BatchId != batchId && i.YarnLotId != null).Select(i => i.YarnLotId);
        var lots = await db.Set<BoyahaneYarnLot>().AsNoTracking()
            .Where(l => l.OrderLineId != null && lineIds.Contains(l.OrderLineId!.Value)
                && l.Status == YarnLotStatus.Approved
                && l.OrderLine!.Order.Status == OrderStatus.Confirmed
                && !busyLotIds.Contains(l.Id))
            .OrderBy(l => l.LotNo)
            .Take(MaxCandidates)
            .Select(l => new
            {
                l.Id,
                l.LotNo,
                LineId = l.OrderLineId!.Value,
                l.OrderLine!.OrderId,
                l.OrderLine.Order.OrderNo,
                CustomerName = l.OrderLine.Order.Customer != null ? l.OrderLine.Order.Customer.Name : null,
                l.OrderLine.ColorName,
                l.OrderLine.ColorCode,
                l.OrderLine.ProcessType,
                l.YarnId,
                YarnCode = l.Yarn.YarnCode,
                l.Kilograms,
            })
            .ToListAsync(cancellationToken);

        var selectedRolls = new HashSet<int>();
        var selectedLots = new HashSet<int>();
        if (batchId is { } id)
        {
            var items = await db.Set<BoyahaneBatchItem>().Where(i => i.BatchId == id && i.IsActive).Select(i => new { i.RollId, i.YarnLotId }).ToListAsync(cancellationToken);
            selectedRolls = items.Where(i => i.RollId is not null).Select(i => i.RollId!.Value).ToHashSet();
            selectedLots = items.Where(i => i.YarnLotId is not null).Select(i => i.YarnLotId!.Value).ToHashSet();
        }

        var result = rolls.Select(t =>
        {
            var group = GroupOf(t.ProcessType, $"F{t.FabricId}", t.FabricCode, t.ColorName, t.ColorCode, recipes.GetValueOrDefault(t.LineId));
            return new BoyahaneBatchCandidateDto(
                t.Id, null, t.RollNo, group.Key, group.Title,
                t.OrderId, t.OrderNo, t.CustomerName, t.ColorName, t.Kilograms, t.Meters, UsesRecipe(t.ProcessType), t.ProcessType, selectedRolls.Contains(t.Id));
        }).ToList();
        result.AddRange(lots.Select(l =>
        {
            var group = GroupOf(l.ProcessType, $"Y{l.YarnId}", l.YarnCode, l.ColorName, l.ColorCode, recipes.GetValueOrDefault(l.LineId));
            return new BoyahaneBatchCandidateDto(
                null, l.Id, l.LotNo, group.Key, group.Title,
                l.OrderId, l.OrderNo, l.CustomerName, l.ColorName, l.Kilograms, null, true, l.ProcessType, selectedLots.Contains(l.Id));
        }));
        return result;
    }

    public async Task<Result<BoyahaneBatchDto>> CreateAsync(BoyahaneBatchRequest request, CancellationToken cancellationToken)
    {
        var plan = await BuildPlanAsync(request, null, cancellationToken);
        if (plan.Error is { } error)
        {
            return error;
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var year = timeProvider.GetUtcNow().Year;
        var sequence = await numbers.NextAsync(Series, year, cancellationToken);
        var batch = new BoyahaneBatch { BatchNo = $"BP-{year}-{sequence:D5}" };
        ApplyPlan(batch, request, plan);
        db.Set<BoyahaneBatch>().Add(batch);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetAsync(batch.Id, cancellationToken);
    }

    public async Task<Result<BoyahaneBatchDto>> UpdateAsync(int id, BoyahaneBatchRequest request, CancellationToken cancellationToken)
    {
        var batch = await db.Set<BoyahaneBatch>().Include(b => b.Items).Include(b => b.Steps).SingleOrDefaultAsync(b => b.Id == id, cancellationToken);
        if (batch is null)
        {
            return NotFound;
        }

        if (batch.Status != BatchStatus.Planned)
        {
            return new Error(InvalidState, "Yalnızca planlanan parti düzenlenebilir.", ErrorType.Conflict);
        }

        var plan = await BuildPlanAsync(request, batch, cancellationToken);
        if (plan.Error is { } error)
        {
            return error;
        }

        // Benzersiz indeks yüzünden eski kalemler önce silinir; iki adım tek işlemdir.
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        db.Set<BoyahaneBatchItem>().RemoveRange(batch.Items);
        await db.SaveChangesAsync(cancellationToken);
        batch.Items.Clear();
        ApplyPlan(batch, request, plan);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    public async Task<Result<BoyahaneBatchDto>> CancelAsync(int id, CancellationToken cancellationToken)
    {
        var batch = await db.Set<BoyahaneBatch>().Include(b => b.Items).SingleOrDefaultAsync(b => b.Id == id, cancellationToken);
        if (batch is null)
        {
            return NotFound;
        }

        if (batch.Status != BatchStatus.Planned)
        {
            return new Error(InvalidState, "Yalnızca planlanan parti iptal edilebilir.", ErrorType.Conflict);
        }

        batch.Status = BatchStatus.Cancelled;
        batch.CardUid = null;
        foreach (var item in batch.Items)
        {
            item.IsActive = false;
        }

        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    private sealed record RecipeInfo(int TrialId, string Key, string Summary);

    private sealed record PlannedItem(int OrderLineId, int? RollId, int? YarnLotId, decimal Kilograms, decimal? Meters);

    private sealed record BatchPlan(
        Error? Error,
        int? RecipeTrialId = null,
        decimal? LiquorRatio = null,
        string? OverrideBy = null,
        string? OverrideNote = null,
        IReadOnlyList<PlannedItem>? Items = null,
        BoyahaneRoute? Route = null,
        int? ReworkOfBatchId = null)
    {
        public static implicit operator BatchPlan(Error error) => new(error);
    }

    /// <summary>
    /// Partinin kuralları: uygun top ya da uygun lot, onaylı siparişin satırına bağlı, boyama satırında onaylı reçete var, bütün
    /// mal aynı kart ve aynı işlemde, kapasite aşılıyorsa yetkili gerekçesi. Flotte yalnızca reçeteli partide istenir.
    /// </summary>
    private async Task<BatchPlan> BuildPlanAsync(BoyahaneBatchRequest request, BoyahaneBatch? existing, CancellationToken cancellationToken)
    {
        if (await ReferenceValidation.RequireSelectableAsync(
                db.Set<BoyahaneMachine>(), request.MachineId, existing?.MachineId, "machineId", "Makine", cancellationToken) is { } machineError)
        {
            return machineError;
        }

        var machine = await db.Set<BoyahaneMachine>().AsNoTracking().SingleAsync(m => m.Id == request.MachineId, cancellationToken);
        var rollIds = (request.RollIds ?? []).Distinct().ToList();
        var lotIds = (request.YarnLotIds ?? []).Distinct().ToList();
        var rolls = await db.Set<BoyahaneFabricRoll>().AsNoTracking()
            .Include(t => t.OrderLine).ThenInclude(l => l!.Order)
            .Where(t => rollIds.Contains(t.Id))
            .ToListAsync(cancellationToken);
        var lots = await db.Set<BoyahaneYarnLot>().AsNoTracking()
            .Include(l => l.OrderLine).ThenInclude(o => o!.Order)
            .Where(l => lotIds.Contains(l.Id))
            .ToListAsync(cancellationToken);
        if (rolls.Count != rollIds.Count || lots.Count != lotIds.Count)
        {
            return new Error(InvalidItem, "Seçilen mallardan biri bulunamadı.", ErrorType.Conflict);
        }

        var lineIds = rolls.Select(t => t.OrderLineId).Concat(lots.Select(l => l.OrderLineId)).Where(i => i is not null).Select(i => i!.Value).Distinct().ToList();
        var recipes = await ApprovedRecipesAsync(lineIds, cancellationToken);

        var batchId = existing?.Id;
        var busyRolls = await db.Set<BoyahaneBatchItem>()
            .Where(i => i.IsActive && i.BatchId != batchId && i.RollId != null && rollIds.Contains(i.RollId!.Value))
            .Select(i => i.RollId)
            .ToListAsync(cancellationToken);
        var busyLots = await db.Set<BoyahaneBatchItem>()
            .Where(i => i.IsActive && i.BatchId != batchId && i.YarnLotId != null && lotIds.Contains(i.YarnLotId!.Value))
            .Select(i => i.YarnLotId)
            .ToListAsync(cancellationToken);

        var items = new List<PlannedItem>();
        var groups = new HashSet<string>();
        int? recipeTrialId = null;
        var process = OrderProcessType.FabricDyeing;

        foreach (var roll in rolls)
        {
            var problem = roll.Status != FabricRollStatus.Approved ? "ham kontrolde uygun değil"
                : roll.OrderLineId is null ? "siparişe bağlı değil"
                : roll.OrderLine!.Order.Status != OrderStatus.Confirmed ? "siparişi onaylı değil"
                : UsesRecipe(roll.OrderLine.ProcessType) && !recipes.ContainsKey(roll.OrderLineId.Value) ? "sipariş satırının onaylı reçetesi yok"
                : busyRolls.Contains(roll.Id) ? "başka bir partide"
                : null;
            if (problem is not null)
            {
                return new Error(InvalidItem, $"{roll.RollNo} partiye alınamaz: {problem}.", ErrorType.Conflict);
            }

            var line = roll.OrderLine!;
            var recipe = recipes.GetValueOrDefault(line.Id);
            groups.Add(GroupOf(line.ProcessType, $"F{line.FabricId}", "", line.ColorName, line.ColorCode, recipe).Key);
            process = line.ProcessType;
            recipeTrialId ??= recipe?.TrialId;
            items.Add(new PlannedItem(line.Id, roll.Id, null, roll.InspectedKilograms ?? roll.Kilograms, roll.InspectedMeters ?? roll.Meters));
        }

        foreach (var lot in lots)
        {
            var problem = lot.Status != YarnLotStatus.Approved ? "lot reddedilmiş"
                : lot.OrderLineId is null ? "siparişe bağlı değil"
                : lot.OrderLine!.Order.Status != OrderStatus.Confirmed ? "siparişi onaylı değil"
                : !recipes.ContainsKey(lot.OrderLineId.Value) ? "sipariş satırının onaylı reçetesi yok"
                : busyLots.Contains(lot.Id) ? "başka bir partide"
                : null;
            if (problem is not null)
            {
                return new Error(InvalidItem, $"{lot.LotNo} partiye alınamaz: {problem}.", ErrorType.Conflict);
            }

            var line = lot.OrderLine!;
            var recipe = recipes.GetValueOrDefault(line.Id);
            groups.Add(GroupOf(line.ProcessType, $"Y{lot.YarnId}", "", line.ColorName, line.ColorCode, recipe).Key);
            process = line.ProcessType;
            recipeTrialId ??= recipe?.TrialId;
            items.Add(new PlannedItem(line.Id, null, lot.Id, lot.Kilograms, null));
        }

        if (groups.Count > 1)
        {
            return new Error(InvalidItem, "Partideki mal aynı kart ve aynı işlemde (reçete ya da desen) olmalıdır.", ErrorType.Conflict);
        }

        // Rota: seçilen ya da işlem türünün varsayılanı.
        var route = await db.Set<BoyahaneRoute>().AsNoTracking()
            .Include(r => r.Steps).ThenInclude(x => x.Operation)
            .Where(r => r.IsActive && r.ProcessType == process && (request.RouteId != null ? r.Id == request.RouteId : r.IsDefault))
            .FirstOrDefaultAsync(cancellationToken);
        if (route is null)
        {
            return new Error(InvalidItem, "Bu işlem türü için geçerli rota yok; Operasyon Rotaları ekranından tanımlayın.", ErrorType.Conflict);
        }

        if (route.Steps.All(x => x.Operation.Behavior != OperationBehavior.FinalQuality))
        {
            return new Error(InvalidItem, $"{route.Name} rotasında final kalite adımı yok; rotayı düzenleyin.", ErrorType.Conflict);
        }

        // Partinin makinesi, rotanın asıl operasyonunu yapabilmelidir: boyamada boyama, diğerlerinde ilk makine adımı.
        var mainOperation = BoyahaneMachineFit.MainOperation(route.Steps.Select(x => (x.Sequence, x.Operation)), recipeTrialId is not null);
        if (mainOperation is not null
            && !await db.Set<BoyahaneMachineOperation>().AnyAsync(o => o.MachineId == machine.Id && o.OperationId == mainOperation.Id, cancellationToken))
        {
            return new Error(InvalidItem, $"{machine.Code} makinesinde {mainOperation.Name} yapılmaz; makine kartını kontrol edin.", ErrorType.Conflict);
        }

        decimal? liquorRatio = null;
        if (recipeTrialId is not null)
        {
            liquorRatio = request.LiquorRatio ?? machine.DefaultLiquorRatio;
            if (liquorRatio is null)
            {
                return new Error(ErrorCodes.ValidationError, "Bu makinede varsayılan flotte oranı yok; parti için flotte oranı girin.", ErrorType.Validation,
                    new Dictionary<string, string[]> { ["liquorRatio"] = ["Flotte oranı zorunludur."] });
            }
        }

        // Kapasite aşımı: uyarı ve yetkili onayı. Gerekçe ve yapan kayda geçer.
        var total = items.Sum(i => i.Kilograms);
        var note = NullIfBlank(request.CapacityOverrideNote);
        string? overrideBy = null;
        if (total > machine.CapacityKg)
        {
            if (!await permissions.HasAsync(BoyahanePermissions.Batch.CapacityOverride, cancellationToken))
            {
                return new Error(OverCapacity, $"Parti {total:0.##} kg, makine kapasitesi {machine.CapacityKg:0.##} kg. Aşım için yetkili onayı gerekir.", ErrorType.Conflict);
            }

            if (note is null)
            {
                return new Error(OverCapacity, $"Parti {total:0.##} kg, makine kapasitesi {machine.CapacityKg:0.##} kg. Devam etmek için gerekçe yazın.", ErrorType.Conflict);
            }

            overrideBy = currentUser.UserName;
        }
        else
        {
            note = null;
        }

        // Yeniden işleme ayrılmış malla açılan parti, önceki partiye bağlanır.
        var reworkOf = await db.Set<BoyahaneBatchItem>().AsNoTracking()
            .Where(i => i.FinalResult == FinalQualityResult.Rework
                && ((i.RollId != null && rollIds.Contains(i.RollId.Value)) || (i.YarnLotId != null && lotIds.Contains(i.YarnLotId.Value))))
            .OrderByDescending(i => i.Id)
            .Select(i => (int?)i.BatchId)
            .FirstOrDefaultAsync(cancellationToken);

        return new BatchPlan(null, recipeTrialId, liquorRatio, overrideBy, note, items, route, reworkOf);
    }

    private void ApplyPlan(BoyahaneBatch batch, BoyahaneBatchRequest request, BatchPlan plan)
    {
        // Makine değişirse eski kuyruktaki sıra geçersizdir; parti yeni makinenin kuyruğunda plan tarihine göre dizilir.
        if (batch.MachineId != request.MachineId)
        {
            batch.PlanSequence = null;
        }

        batch.MachineId = request.MachineId;
        batch.RecipeTrialId = plan.RecipeTrialId;
        batch.LiquorRatio = plan.LiquorRatio;
        batch.PlannedDate = request.PlannedDate;
        batch.Notes = NullIfBlank(request.Notes);
        batch.CapacityOverrideBy = plan.OverrideBy;
        batch.CapacityOverrideNote = plan.OverrideNote;
        batch.ReworkOfBatchId = plan.ReworkOfBatchId;

        // Adımlar rotadan kopyalanır; düzenlemede rota değişmediyse kaldırılan adımlar korunur.
        batch.RouteId = plan.Route!.Id;
        var operations = plan.Route.Steps.OrderBy(x => x.Sequence).Select(x => x.OperationId).ToList();
        if (!batch.Steps.OrderBy(x => x.Sequence).Select(x => x.OperationId).SequenceEqual(operations))
        {
            db.Set<BoyahaneBatchStep>().RemoveRange(batch.Steps);
            var sequence = 0;
            foreach (var operationId in operations)
            {
                batch.Steps.Add(new BoyahaneBatchStep { Sequence = ++sequence, OperationId = operationId });
            }
        }

        foreach (var item in plan.Items!)
        {
            batch.Items.Add(new BoyahaneBatchItem
            {
                OrderLineId = item.OrderLineId,
                RollId = item.RollId,
                YarnLotId = item.YarnLotId,
                Kilograms = item.Kilograms,
                Meters = item.Meters,
            });
        }
    }

    private static bool UsesRecipe(OrderProcessType process) =>
        process is OrderProcessType.FabricDyeing or OrderProcessType.YarnDyeing;

    /// <summary>Birlikte işlenebilecek malın grubu: boyamada kart + reçete, baskıda kart + desen, terbiyede kart.</summary>
    private static (string Key, string Title) GroupOf(OrderProcessType process, string card, string cardCode, string? color, string? colorCode, RecipeInfo? recipe) => process switch
    {
        OrderProcessType.Finishing => ($"{card}|FINISHING", $"{cardCode} · Terbiye"),
        OrderProcessType.Printing => ($"{card}|PRINTING|{colorCode ?? color}", $"{cardCode} · Baskı · {color}"),
        _ => ($"{card}|{recipe!.Key}", $"{cardCode} · {color} · {recipe.Summary}"),
    };

    /// <summary>Satırların onaylı reçeteleri. Anahtar reçete içeriğidir: kopyalanmış reçete aslıyla aynı anahtarı taşır.</summary>
    private async Task<Dictionary<int, RecipeInfo>> ApprovedRecipesAsync(IReadOnlyList<int>? lineIds, CancellationToken cancellationToken)
    {
        var trials = db.Set<BoyahaneLabTrial>().AsNoTracking()
            .Where(t => t.Decision == LabTrialDecision.Accepted && t.Request.Status == LabRequestStatus.Approved);
        if (lineIds is not null)
        {
            trials = trials.Where(t => lineIds.Contains(t.Request.OrderLineId));
        }

        var rows = await trials
            .Select(t => new
            {
                t.Id,
                t.Request.OrderLineId,
                t.Request.RequestNo,
                t.TrialCode,
                Items = t.Items.Select(i => new { i.DyeChemicalId, i.Amount }).ToList(),
            })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(
            r => r.OrderLineId,
            r => new RecipeInfo(
                r.Id,
                string.Join(",", r.Items.OrderBy(i => i.DyeChemicalId).Select(i => $"{i.DyeChemicalId}:{i.Amount:0.####}")),
                $"{r.RequestNo}/{r.TrialCode}"));
    }
}
