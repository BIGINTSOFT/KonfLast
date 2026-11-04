using ERP.Konfeksiyon.Application.Abstractions;
using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Konfeksiyon.Application.Features.Costing;
using ERP.Konfeksiyon.Application.Features.Payroll;
using ERP.Konfeksiyon.Application.Features.Warehousing;
using ERP.Core.Domain.Entities;
using ERP.Konfeksiyon.Domain.Entities;
using ERP.Konfeksiyon.Domain.Enums;
using ERP.Konfeksiyon.Domain.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ERP.Konfeksiyon.Application.Features.Bundles;

public interface IBundleService
{
    Task<PagedResult<BundleListItemDto>> GetListAsync(BundleListQuery query, CancellationToken cancellationToken);
    Task<Result<BundleDto>> GetAsync(int id, CancellationToken cancellationToken);

    /// <summary>Siparişin kalem bazında sipariş / demetlenen / kalan adetleri.</summary>
    Task<Result<OrderBundleSummaryDto>> GetOrderSummaryAsync(int orderId, CancellationToken cancellationToken);

    Task<Result<BundleDto>> CreateAsync(CreateBundleRequest request, CancellationToken cancellationToken);

    /// <summary>Siparişin kalan adetlerini verilen demet boyuna bölerek toplu demet oluşturur.</summary>
    Task<Result<GenerateBundlesResultDto>> GenerateAsync(
        int orderId, GenerateBundlesRequest request, CancellationToken cancellationToken);

    Task<Result<BundleDto>> CancelAsync(int id, string rowVersion, CancellationToken cancellationToken);

    /// <summary>
    /// Kesim planının kesilen adetlerinden demet açar (demet boyu kuralları aynıdır; sipariş adedini aşan kesim
    /// reddedilmez, uyarıyı plan verir). Demetler plana bağlanır. Reçete malzemesi düşülür; ancak
    /// <see cref="CuttingBundleRequest.SkipMaterialItemIds"/> kartları (plana atanmış toplarla düşülecek kumaş) atlanır.
    /// Çağıranın açık transaction'ı içinde çalışır: kaydeder ama commit etmez.
    /// </summary>
    Task<Result<IReadOnlyList<int>>> CreateForCuttingAsync(CuttingBundleRequest request, CancellationToken cancellationToken);
}

public sealed class BundleService(
    IKonfeksiyonDbContext db,
    IAppLock appLock,
    IDocumentNumberGenerator numberGenerator,
    IPayrollService payroll,
    IStockLedger stockLedger,
    TimeProvider timeProvider,
    IOptions<BundleOptions> options) : IBundleService
{
    private static readonly TimeSpan LockTimeout = TimeSpan.FromSeconds(15);

    private static readonly Error NotFound = new(ErrorCodes.NotFound, "Demet bulunamadı.", ErrorType.NotFound);
    private static readonly Error OrderNotFound = new(ErrorCodes.NotFound, "İş emri bulunamadı.", ErrorType.NotFound);

    private static readonly Error ConcurrencyConflict = new(
        ErrorCodes.ConcurrencyConflict,
        "Demet siz açtıktan sonra değişti. Sayfayı yenileyip tekrar deneyin.",
        ErrorType.Conflict);

    private readonly BundleOptions _options = options.Value;

    /// <summary>Bir renk-beden için demetlenecek adetler (her eleman bir demet).</summary>
    private sealed record Batch(int ColorId, int SizeId, IReadOnlyList<int> Quantities);

    private sealed record OrderState(
        ProductionOrder Order, ProductionRoute? Route, string? BlockedReason, IReadOnlyList<BundleSummaryItemDto> Items);

    public Task<PagedResult<BundleListItemDto>> GetListAsync(BundleListQuery query, CancellationToken cancellationToken)
    {
        var bundles = db.Bundles.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            bundles = bundles.Where(b => b.BundleNo.Contains(search) || b.ProductionOrder.OrderNo.Contains(search));
        }

        if (query.ProductionOrderId is { } orderId)
        {
            bundles = bundles.Where(b => b.ProductionOrderId == orderId);
        }

        if (query.Status is { } status)
        {
            bundles = bundles.Where(b => b.Status == status);
        }

        if (query.ColorId is { } colorId)
        {
            bundles = bundles.Where(b => b.ColorId == colorId);
        }

        if (query.SizeId is { } sizeId)
        {
            bundles = bundles.Where(b => b.SizeId == sizeId);
        }

        return ProjectToListItem(bundles.OrderByDescending(b => b.Id)).ToPagedResultAsync(query, cancellationToken);
    }

    public async Task<Result<BundleDto>> GetAsync(int id, CancellationToken cancellationToken)
    {
        var bundle = await db.Bundles
            .AsNoTracking()
            .Include(b => b.ProductionOrder).ThenInclude(o => o.Customer)
            .Include(b => b.ProductionOrder).ThenInclude(o => o.Product).ThenInclude(p => p.Model)
            .Include(b => b.Color)
            .Include(b => b.Size)
            .Include(b => b.ProductionRoute)
            .Include(b => b.Operations).ThenInclude(o => o.Operation)
            .Include(b => b.Operations).ThenInclude(o => o.Machine)
            .Include(b => b.Operations).ThenInclude(o => o.Operator)
            .SingleOrDefaultAsync(b => b.Id == id, cancellationToken);
        if (bundle is null)
        {
            return NotFound;
        }

        // Öncüller demetin sıra moduna göredir (sıralı: sıra no, sırasız: yok, eski demetler: rota tanımı).
        var flow = await db.LoadFlowAsync(bundle, cancellationToken);
        var sequenceOf = bundle.Operations.ToDictionary(o => o.RouteOperationId, o => o.SequenceNo);
        var predecessors = bundle.Operations.ToDictionary(
            o => o.RouteOperationId,
            o => (IReadOnlyList<int>)flow.PredecessorsOf(o.RouteOperationId).Select(id => sequenceOf[id]).Order().ToList());

        var rfidUid = await db.BundleCardAssignments
            .Where(a => a.BundleId == id && a.ReleasedAt == null)
            .Select(a => a.RfidCard.Uid)
            .FirstOrDefaultAsync(cancellationToken);

        return ToDto(bundle, predecessors, rfidUid);
    }

    public async Task<Result<OrderBundleSummaryDto>> GetOrderSummaryAsync(
        int orderId, CancellationToken cancellationToken)
    {
        var state = await LoadOrderStateAsync(orderId, cancellationToken);
        if (state is null)
        {
            return OrderNotFound;
        }

        return new OrderBundleSummaryDto(
            state.Order.Id,
            state.Order.OrderNo,
            state.BlockedReason is null,
            state.BlockedReason,
            state.Items.Sum(i => i.Ordered),
            state.Items.Sum(i => i.Bundled),
            state.Items.Sum(i => i.Remaining),
            state.Items,
            await LoadProgressAsync(orderId, cancellationToken),
            await db.ProductMaterials.AnyAsync(m => m.ProductId == state.Order.ProductId, cancellationToken));
    }

    private async Task<OrderBundleProgressDto> LoadProgressAsync(int orderId, CancellationToken cancellationToken)
    {
        var counts = await db.Bundles
            .Where(b => b.ProductionOrderId == orderId && b.Status != BundleStatus.Cancelled)
            .GroupBy(b => b.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Status, g => g.Count, cancellationToken);

        var total = counts.Values.Sum();
        var withoutCard = counts.GetValueOrDefault(BundleStatus.Created);
        var completed = counts.GetValueOrDefault(BundleStatus.Completed);
        var closed = counts.GetValueOrDefault(BundleStatus.Closed);
        return new OrderBundleProgressDto(total, withoutCard, total - withoutCard - completed - closed, completed, closed);
    }

    public async Task<Result<BundleDto>> CreateAsync(CreateBundleRequest request, CancellationToken cancellationToken)
    {
        var created = await CreateBundlesAsync(
            request.ProductionOrderId,
            request.WarehouseId,
            items =>
            {
                var item = items.SingleOrDefault(i => i.ColorId == request.ColorId && i.SizeId == request.SizeId);
                if (item is null)
                {
                    return ReferenceValidation.InvalidReference("colorId", "Siparişte bu renk-beden kalemi yok.");
                }

                if (request.Quantity > _options.MaxBundleSize)
                {
                    return TooLarge("quantity");
                }

                if (request.Quantity > item.Remaining)
                {
                    return ExceedsOrder(item, "quantity");
                }

                return Result<IReadOnlyList<Batch>>.Success([new Batch(request.ColorId, request.SizeId, [request.Quantity])]);
            },
            cancellationToken);

        return created.IsSuccess ? await GetAsync(created.Value[0].Id, cancellationToken) : created.Error!;
    }

    public async Task<Result<GenerateBundlesResultDto>> GenerateAsync(
        int orderId, GenerateBundlesRequest request, CancellationToken cancellationToken)
    {
        var created = await CreateBundlesAsync(
            orderId,
            request.WarehouseId,
            items =>
            {
                if (request.BundleSize > _options.MaxBundleSize)
                {
                    return TooLarge("bundleSize");
                }

                var selected = items;
                if (request.Items is { Count: > 0 } requested)
                {
                    var wanted = requested.Select(i => (i.ColorId, i.SizeId)).ToHashSet();
                    selected = items.Where(i => wanted.Remove((i.ColorId, i.SizeId))).ToList();
                    if (wanted.Count > 0)
                    {
                        return ReferenceValidation.InvalidReference("items", "Siparişte olmayan bir renk-beden istendi.");
                    }
                }

                var batches = selected
                    .Where(i => i.Remaining > 0)
                    .Select(i => new Batch(i.ColorId, i.SizeId, Split(i.Remaining, request.BundleSize)))
                    .ToList();

                if (batches.Count == 0)
                {
                    return new Error(ErrorCodes.NothingToBundle, "Demetlenecek adet kalmamış.");
                }

                return Result<IReadOnlyList<Batch>>.Success(batches);
            },
            cancellationToken);
        if (!created.IsSuccess)
        {
            return created.Error!;
        }

        var ids = created.Value.Select(b => b.Id).ToList();
        var bundles = await ProjectToListItem(db.Bundles.AsNoTracking().Where(b => ids.Contains(b.Id)).OrderBy(b => b.Id))
            .ToListAsync(cancellationToken);

        return new GenerateBundlesResultDto(bundles.Count, bundles.Sum(b => b.Quantity), bundles);
    }

    public async Task<Result<IReadOnlyList<int>>> CreateForCuttingAsync(
        CuttingBundleRequest request, CancellationToken cancellationToken)
    {
        if (db.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException("Kesimden demet açma, kesim kaydıyla aynı transaction içinde yapılmalıdır.");
        }

        var created = await CreateBundlesAsync(
            request.ProductionOrderId,
            request.WarehouseId,
            items =>
            {
                if (request.BundleSize > _options.MaxBundleSize)
                {
                    return TooLarge("bundleSize");
                }

                var batches = new List<Batch>();
                foreach (var item in request.Items.Where(i => i.Quantity > 0))
                {
                    if (!items.Any(i => i.ColorId == item.ColorId && i.SizeId == item.SizeId))
                    {
                        return ReferenceValidation.InvalidReference("sizes", "Siparişte bu renk-beden kalemi yok.");
                    }

                    // Kesilen parça kesilmiştir: sipariş adedini aşsa da demetlenir (fazla kesim planda uyarılır).
                    batches.Add(new Batch(item.ColorId, item.SizeId, Split(item.Quantity, request.BundleSize)));
                }

                if (batches.Count == 0)
                {
                    return new Error(ErrorCodes.NothingToBundle, "Kesilen adet girilmemiş.");
                }

                return Result<IReadOnlyList<Batch>>.Success(batches);
            },
            cancellationToken,
            request.CuttingPlanId,
            request.SkipMaterialItemIds);

        return created.IsSuccess
            ? Result<IReadOnlyList<int>>.Success(created.Value.Select(b => b.Id).ToList())
            : Result<IReadOnlyList<int>>.Failure(created.Error!);
    }

    public async Task<Result<BundleDto>> CancelAsync(int id, string rowVersion, CancellationToken cancellationToken)
    {
        // Demetin iptali ile malzemesinin depoya dönüşü tek işlemdir.
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var bundle = await db.Bundles.Include(b => b.Operations).SingleOrDefaultAsync(b => b.Id == id, cancellationToken);
        if (bundle is null)
        {
            return NotFound;
        }

        if (RowVersionToken.Check(bundle.RowVersion, rowVersion, ConcurrencyConflict) is { } versionError)
        {
            return versionError;
        }

        if (!bundle.CanCancel)
        {
            return new Error(ErrorCodes.BundleNotCancellable, "Üretime girmiş demet iptal edilemez.");
        }

        bundle.Cancel();

        // İptal edilen demetin kartı serbest kalır; başka demete atanabilir.
        var assignment = await db.BundleCardAssignments
            .SingleOrDefaultAsync(a => a.BundleId == id && a.ReleasedAt == null, cancellationToken);
        assignment?.Release(timeProvider.GetUtcNow().UtcDateTime);

        if (await stockLedger.PostProductionReturnAsync([id], cancellationToken) is { } stockError)
        {
            return stockError;
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ConcurrencyConflict;
        }

        await transaction.CommitAsync(cancellationToken);

        return await GetAsync(id, cancellationToken);
    }

    /// <summary>
    /// Ortak oluşturma akışı. Sipariş için kilit alınır; kalan adetler kilit altında hesaplanır,
    /// <paramref name="plan"/> bunlardan oluşturulacak demetleri belirler, numaralar tek seferde ayrılır
    /// ve hepsi tek transaction'da kaydedilir (eşzamanlı isteklerde sipariş adedi aşılamaz). Ürünün reçetesi
    /// varsa malzemesi aynı işlemde depodan düşülür; stok yetmezse demet açılmaz.
    /// Çağıranın açık bir transaction'ı varsa (kesim kaydı) onun içinde çalışır; commit'i çağıran yapar.
    /// </summary>
    /// <param name="cuttingPlanId">Demetler bir kesim planından açılıyorsa o plan.</param>
    /// <param name="skipMaterialItemIds">Reçeteden düşülmeyecek kartlar (kesim planının toplarıyla düşülen kumaş).</param>
    private async Task<Result<List<Bundle>>> CreateBundlesAsync(
        int orderId,
        int? warehouseId,
        Func<IReadOnlyList<BundleSummaryItemDto>, Result<IReadOnlyList<Batch>>> plan,
        CancellationToken cancellationToken,
        int? cuttingPlanId = null,
        IReadOnlyCollection<int>? skipMaterialItemIds = null)
    {
        await using var transaction = db.Database.CurrentTransaction is null
            ? await db.Database.BeginTransactionAsync(cancellationToken)
            : null;

        if (!await appLock.TryAcquireAsync($"bundles:order:{orderId}", LockTimeout, cancellationToken))
        {
            return new Error(
                ErrorCodes.ResourceBusy, "Bu sipariş için şu an başka bir demetleme işlemi sürüyor. Lütfen tekrar deneyin.",
                ErrorType.Conflict);
        }

        var state = await LoadOrderStateAsync(orderId, cancellationToken);
        if (state is null)
        {
            return OrderNotFound;
        }

        if (state.BlockedReason is not null)
        {
            return new Error(
                state.Route is null && state.Order.Status is ProductionOrderStatus.Released or ProductionOrderStatus.InProduction
                    ? ErrorCodes.RouteRequired
                    : ErrorCodes.OrderNotBundleable,
                state.BlockedReason);
        }

        var planned = plan(state.Items);
        if (!planned.IsSuccess)
        {
            return planned.Error!;
        }

        var quantities = planned.Value.SelectMany(b => b.Quantities.Select(q => (b.ColorId, b.SizeId, Quantity: q))).ToList();
        if (quantities.Count > _options.MaxBundlesPerRequest)
        {
            return new Error(
                ErrorCodes.ValidationError,
                $"Tek seferde en fazla {_options.MaxBundlesPerRequest} demet oluşturulabilir ({quantities.Count} demet istendi). " +
                "Demet adedini büyütün ya da kalemleri ayrı ayrı demetleyin.",
                ErrorType.Validation);
        }

        var firstNumber = await numberGenerator.NextRangeAsync(
            DocumentSeries.Bundle, DocumentSeries.NoYear, quantities.Count, cancellationToken);

        // Parça başı birim ücret demet açılırken sabitlenir: sonradan değişen ücret geçmiş kazancı etkilemez.
        var payrollSetting = await payroll.LoadSettingAsync(cancellationToken);
        var routeOperationIds = state.Route!.Operations.Select(o => o.OperationId).Distinct().ToList();
        var pieceRates = await db.Operations
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(o => routeOperationIds.Contains(o.Id))
            .ToDictionaryAsync(o => o.Id, o => o.PieceRate, cancellationToken);

        var bundles = new List<Bundle>(quantities.Count);
        foreach (var (index, item) in quantities.Index())
        {
            var bundle = new Bundle
            {
                BundleNo = (firstNumber + index).ToString().PadLeft(_options.NumberDigits, '0'),
                ProductionOrderId = orderId,
                ColorId = item.ColorId,
                SizeId = item.SizeId,
                Quantity = item.Quantity,
                ProductionRouteId = state.Route.Id,
                SequenceMode = state.Order.OperationSequence,
                CuttingPlanId = cuttingPlanId,
            };
            bundle.AddOperationsFrom(
                state.Route, step => payrollSetting.UnitRateFor(pieceRates.GetValueOrDefault(step.OperationId), step.Sam));
            bundles.Add(bundle);
        }

        db.Bundles.AddRange(bundles);
        if (await IssueMaterialsAsync(state.Order, bundles, warehouseId, skipMaterialItemIds ?? [], cancellationToken) is { } materialError)
        {
            return materialError;
        }

        await db.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        return bundles;
    }

    /// <summary>
    /// Yeni demetlerin reçete malzemesini depodan düşer. Reçetesi olmayan üründe hiçbir şey yapmaz;
    /// <paramref name="skipItemIds"/> kartları atlanır (başka yoldan düşülür).
    /// </summary>
    private async Task<Error?> IssueMaterialsAsync(
        ProductionOrder order, IReadOnlyList<Bundle> bundles, int? warehouseId, IReadOnlyCollection<int> skipItemIds,
        CancellationToken cancellationToken)
    {
        var recipe = (await db.ProductMaterials
                .AsNoTracking()
                .Where(m => m.ProductId == order.ProductId)
                .ToListAsync(cancellationToken))
            .Where(m => !skipItemIds.Contains(m.StockItemId))
            .ToList();
        if (recipe.Count == 0)
        {
            return null;
        }

        var stockItemIds = recipe.Select(m => m.StockItemId).Distinct().ToList();
        var units = await db.StockItems
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(i => stockItemIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, i => i.Unit, cancellationToken);

        // Kalemin daha önce demetlenen adedi: miktar, kalemin toplamı üzerinden yuvarlanarak düşülür.
        var covered = (await db.Bundles
                .Where(b => b.ProductionOrderId == order.Id && b.Status != BundleStatus.Cancelled)
                .GroupBy(b => new { b.ColorId, b.SizeId })
                .Select(g => new { g.Key.ColorId, g.Key.SizeId, Quantity = g.Sum(b => b.Quantity) })
                .ToListAsync(cancellationToken))
            .ToDictionary(g => (g.ColorId, g.SizeId), g => g.Quantity);

        var lines = new List<(Bundle Bundle, int StockItemId, decimal Quantity)>();
        foreach (var bundle in bundles)
        {
            var key = (bundle.ColorId, bundle.SizeId);
            var before = covered.GetValueOrDefault(key);
            foreach (var line in MaterialRequirement.LinesFor(recipe, bundle.ColorId, bundle.SizeId))
            {
                var quantity = MaterialRequirement.QuantityForNext(line, units[line.StockItemId], before, bundle.Quantity);
                if (quantity > 0)
                {
                    lines.Add((bundle, line.StockItemId, quantity));
                }
            }

            covered[key] = before + bundle.Quantity;
        }

        if (lines.Count == 0)
        {
            return null;
        }

        var warehouse = await MaterialWarehouse.ResolveAsync(db, warehouseId, order.MaterialWarehouseId, cancellationToken);
        if (!warehouse.IsSuccess)
        {
            return warehouse.Error;
        }

        return await stockLedger.PostProductionIssueAsync(warehouse.Value, order.Id, order.OrderNo, lines, cancellationToken);
    }

    /// <summary>Siparişi, modelinin etkin rotasını ve kalem bazında demetlenen / kalan adetleri yükler.</summary>
    private async Task<OrderState?> LoadOrderStateAsync(int orderId, CancellationToken cancellationToken)
    {
        var order = await db.ProductionOrders
            .AsNoTracking()
            .Include(o => o.Items).ThenInclude(i => i.Color)
            .Include(o => o.Items).ThenInclude(i => i.Size)
            .Include(o => o.Product)
            .SingleOrDefaultAsync(o => o.Id == orderId, cancellationToken);
        if (order is null)
        {
            return null;
        }

        var route = await db.ProductionRoutes
            .AsNoTracking()
            .Include(r => r.Operations).ThenInclude(o => o.Predecessors)
            .SingleOrDefaultAsync(
                r => r.ModelId == order.Product.ModelId && r.Status == RouteStatus.Active, cancellationToken);

        var bundled = await db.Bundles
            .Where(b => b.ProductionOrderId == orderId && b.Status != BundleStatus.Cancelled)
            .GroupBy(b => new { b.ColorId, b.SizeId })
            .Select(g => new { g.Key.ColorId, g.Key.SizeId, Quantity = g.Sum(b => b.Quantity), Count = g.Count() })
            .ToDictionaryAsync(g => (g.ColorId, g.SizeId), cancellationToken);

        var items = order.Items
            .OrderBy(i => i.Color.ColorCode).ThenBy(i => i.Size.SortOrder).ThenBy(i => i.Size.SizeCode)
            .Select(i =>
            {
                var done = bundled.GetValueOrDefault((i.ColorId, i.SizeId));
                var allowed = (int)Math.Floor(i.Quantity * (1 + _options.OverageTolerancePercent / 100m));
                var bundledQuantity = done?.Quantity ?? 0;
                return new BundleSummaryItemDto(
                    i.ColorId, i.Color.Name, i.SizeId, i.Size.Name, i.Quantity, allowed, bundledQuantity,
                    Math.Max(allowed - bundledQuantity, 0), done?.Count ?? 0);
            })
            .ToList();

        string? blockedReason = null;
        if (order.Status is not (ProductionOrderStatus.Released or ProductionOrderStatus.InProduction))
        {
            blockedReason = "Yalnızca onaylı veya üretimdeki siparişler için demet oluşturulabilir.";
        }
        else if (route is null)
        {
            blockedReason = "Siparişin modeli için etkin bir operasyon rotası yok. Önce rotayı etkinleştirin.";
        }

        return new OrderState(order, route, blockedReason, items);
    }

    /// <summary>Toplam adedi demet boyuna böler; artan adet son demete kalır (45 / 20 → 20, 20, 5).</summary>
    private static List<int> Split(int total, int bundleSize)
    {
        var quantities = Enumerable.Repeat(bundleSize, total / bundleSize).ToList();
        if (total % bundleSize > 0)
        {
            quantities.Add(total % bundleSize);
        }

        return quantities;
    }

    private Error TooLarge(string field)
    {
        var message = $"Bir demete en fazla {_options.MaxBundleSize} parça konabilir.";
        return new Error(
            ErrorCodes.ValidationError, message, ErrorType.Validation,
            new Dictionary<string, string[]> { [field] = [message] });
    }

    private static Error ExceedsOrder(BundleSummaryItemDto item, string field)
    {
        var message =
            $"{item.ColorName} / {item.SizeName} için siparişte {item.Ordered} adet var; demetlenebilecek kalan {item.Remaining} adet.";
        return new Error(
            ErrorCodes.BundleExceedsOrderQuantity, message, ErrorType.BusinessRule,
            new Dictionary<string, string[]> { [field] = [message] });
    }

    private IQueryable<BundleListItemDto> ProjectToListItem(IQueryable<Bundle> bundles) =>
        bundles.Select(b => new BundleListItemDto(
            b.Id,
            b.BundleNo,
            b.ProductionOrderId,
            b.ProductionOrder.OrderNo,
            b.ProductionOrder.Product.ProductCode,
            b.ProductionOrder.Product.Model.ModelCode,
            b.Color.Name,
            b.Size.Name,
            b.Quantity,
            b.Status,
            db.BundleCardAssignments
                .Where(a => a.BundleId == b.Id && a.ReleasedAt == null)
                .Select(a => a.RfidCard.Uid)
                .FirstOrDefault(),
            b.Operations.Count(o => o.Status == BundleOperationStatus.Completed),
            b.Operations.Count,
            b.CreatedAt));

    private static BundleDto ToDto(
        Bundle bundle, Dictionary<int, IReadOnlyList<int>> predecessors, string? rfidUid)
    {
        IReadOnlyList<int> PredecessorsOf(BundleOperation operation) =>
            predecessors.GetValueOrDefault(operation.RouteOperationId) ?? [];

        var stages = RouteGraph
            .Analyze(bundle.Operations.Select(o => new RouteNode(o.SequenceNo, PredecessorsOf(o))).ToList())
            .Stages;

        var ordered = bundle.Operations.OrderBy(o => o.SequenceNo).ToList();
        var inProgress = ordered.Where(o => o.Status is BundleOperationStatus.InProgress or BundleOperationStatus.Paused).ToList();
        var current = inProgress.Count > 0 ? inProgress : ordered.Where(o => o.Status == BundleOperationStatus.Ready).ToList();

        return new BundleDto(
            bundle.Id,
            bundle.BundleNo,
            bundle.ProductionOrderId,
            bundle.ProductionOrder.OrderNo,
            bundle.ProductionOrder.Customer.Name,
            bundle.ProductionOrder.Product.ProductCode,
            bundle.ProductionOrder.Product.Name,
            bundle.ProductionOrder.Product.Model.ModelCode,
            bundle.ProductionRoute.Version,
            bundle.ColorId,
            bundle.Color.Name,
            bundle.SizeId,
            bundle.Size.Name,
            bundle.Quantity,
            bundle.Status,
            rfidUid,
            bundle.IsOpen && rfidUid is null,
            bundle.CanCancel,
            bundle.Status == BundleStatus.Cancelled ? [] : current.Select(o => o.Operation.Name).ToList(),
            RowVersionToken.From(bundle.RowVersion),
            bundle.CreatedAt,
            bundle.CreatedBy,
            ordered
                .Select(o => new BundleOperationDto(
                    o.Id,
                    o.SequenceNo,
                    o.OperationId,
                    o.Operation.OperationCode,
                    o.Operation.Name,
                    o.Sam,
                    o.Status,
                    o.PlannedQuantity,
                    o.ProducedQuantity,
                    o.RejectedQuantity,
                    o.RemainingQuantity,
                    o.Machine?.MachineCode,
                    o.Operator is null ? null : $"{o.Operator.FirstName} {o.Operator.LastName}",
                    o.StartedAt,
                    o.CompletedAt,
                    PredecessorsOf(o),
                    stages.GetValueOrDefault(o.SequenceNo),
                    o.ReworkQuantity))
                .ToList(),
            bundle.SequenceMode,
            bundle.AtSubcontractor);
    }
}
