using System.Globalization;
using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Core.Domain.Enums;
using ERP.Konfeksiyon.Application.Abstractions;
using ERP.Konfeksiyon.Application.Features.Bundles;
using ERP.Konfeksiyon.Application.Features.Warehousing;
using ERP.Konfeksiyon.Domain.Entities;
using ERP.Konfeksiyon.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ERP.Konfeksiyon.Application.Features.Cutting;

public interface ICuttingPlanService
{
    Task<PagedResult<CuttingPlanListItemDto>> GetListAsync(CuttingPlanListQuery query, CancellationToken cancellationToken);
    Task<Result<CuttingPlanDto>> GetAsync(int id, CancellationToken cancellationToken);

    /// <summary>Siparişin renk-beden bazında kesilecek kalan adetleri ve kesim planları.</summary>
    Task<Result<OrderCuttingSummaryDto>> GetOrderSummaryAsync(int orderId, CancellationToken cancellationToken);

    /// <summary>Plana atanabilecek, stoğu olan kumaş topları.</summary>
    Task<IReadOnlyList<CuttingLotOptionDto>> GetLotOptionsAsync(CuttingLotQuery query, CancellationToken cancellationToken);

    Task<Result<CuttingPlanDto>> CreateAsync(CuttingPlanRequest request, CancellationToken cancellationToken);
    Task<Result<CuttingPlanDto>> UpdateAsync(int id, CuttingPlanRequest request, CancellationToken cancellationToken);
    Task<Result<CuttingPlanDto>> CancelAsync(int id, string rowVersion, CancellationToken cancellationToken);

    /// <summary>
    /// Kesimi kaydeder: gerçek kat ve beden adetleri yazılır, kesilen adetlerden demetler açılır, kullanılan kumaş
    /// plana atanmış toplardan düşülür. Hepsi tek işlemdir; biri olmazsa hiçbiri olmaz.
    /// </summary>
    Task<Result<CuttingPlanDto>> CutAsync(int id, CuttingPlanCutRequest request, CancellationToken cancellationToken);
}

/// <summary>
/// Kesim planları (pastal). Kesilecek kalan, renk-beden başına sipariş adedi − kesilmiş planların adedi − plan dışında
/// açılmış demetlerin adedidir; fazla kesim engellenmez, uyarılır.
///
/// Kumaş düşümü: demet açılışı reçete malzemesini kendiliğinden düşer. Plana top atanmışsa o toplardaki kumaş kartı
/// reçete düşümünden çıkarılır ve kesimde gerçekten kullanılan miktar atanan toplardan düşülür (aynı kumaş iki kez
/// düşülmez). Bu hareketler demete değil siparişe ve plan numarasına bağlıdır: kesilmiş kumaş depoya dönmeyeceği için
/// demet iptalinde iade edilmez. Top atanmamış planda kumaş, öteki demetlerde olduğu gibi reçeteden düşülür.
/// </summary>
public sealed class CuttingPlanService(
    IKonfeksiyonDbContext db,
    IAppLock appLock,
    IDocumentNumberGenerator numbers,
    IBundleService bundleService,
    IStockLedger stockLedger,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : ICuttingPlanService
{
    private const string Series = "CuttingPlan";
    private const string NumberPrefix = "KP";
    private const string InvalidState = "CUTTING_PLAN_INVALID_STATE";
    private const string MixedShade = "CUTTING_PLAN_MIXED_SHADE";
    private const string InvalidLot = "CUTTING_PLAN_INVALID_LOT";
    private static readonly TimeSpan LockTimeout = TimeSpan.FromSeconds(15);
    private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");

    private static readonly Error NotFound = new(ErrorCodes.NotFound, "Kesim planı bulunamadı.", ErrorType.NotFound);
    private static readonly Error OrderNotFound = new(ErrorCodes.NotFound, "Üretim siparişi bulunamadı.", ErrorType.NotFound);

    private static readonly Error ConcurrencyConflict = new(
        ErrorCodes.ConcurrencyConflict,
        "Kesim planı siz açtıktan sonra değişti. Sayfayı yenileyip tekrar deneyin.",
        ErrorType.Conflict);

    private static readonly Error Busy = new(
        ErrorCodes.ResourceBusy, "Bu kesim planı şu an başka bir işlemle kaydediliyor. Lütfen tekrar deneyin.", ErrorType.Conflict);

    /// <summary>Siparişin bir renk-bedeninin kesim durumu.</summary>
    private sealed record ItemState(
        int ColorId, string ColorName, int SizeId, string SizeName, int Ordered, int Cut, int BundledOutside, int PlannedInDrafts)
    {
        public int Remaining => Ordered - Cut - BundledOutside;
    }

    private sealed record LotInfo(
        int Id, string LotNo, int StockItemId, string ItemCode, StockItemType ItemType, string? BatchNo, string? Shade);

    private sealed record LotBalance(decimal Quantity, string Locations);

    public Task<PagedResult<CuttingPlanListItemDto>> GetListAsync(CuttingPlanListQuery query, CancellationToken cancellationToken)
    {
        var plans = db.CuttingPlans.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            plans = plans.Where(p =>
                p.PlanNo.Contains(search)
                || p.ProductionOrder.OrderNo.Contains(search)
                || p.ProductionOrder.Product.ProductCode.Contains(search));
        }

        if (query.Status is { } status)
        {
            plans = plans.Where(p => p.Status == status);
        }

        if (query.ProductionOrderId is { } orderId)
        {
            plans = plans.Where(p => p.ProductionOrderId == orderId);
        }

        return Project(plans.OrderByDescending(p => p.Id)).ToPagedResultAsync(query, cancellationToken);
    }

    public async Task<Result<CuttingPlanDto>> GetAsync(int id, CancellationToken cancellationToken)
    {
        var plan = await db.CuttingPlans
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Include(p => p.ProductionOrder).ThenInclude(o => o.Customer)
            .Include(p => p.ProductionOrder).ThenInclude(o => o.Product).ThenInclude(p => p.Model)
            .Include(p => p.Color)
            .Include(p => p.Sizes).ThenInclude(s => s.Size)
            .Include(p => p.Lots).ThenInclude(l => l.StockLot).ThenInclude(l => l.StockItem)
            .SingleOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (plan is null)
        {
            return NotFound;
        }

        var states = (await LoadItemStatesAsync(plan.ProductionOrderId, plan.Id, cancellationToken))
            .Where(s => s.ColorId == plan.ColorId)
            .ToDictionary(s => s.SizeId);
        var balances = await LoadLotBalancesAsync(plan.Lots.Select(l => l.StockLotId).ToList(), cancellationToken);
        var bundles = await db.Bundles
            .AsNoTracking()
            .Where(b => b.CuttingPlanId == id)
            .OrderBy(b => b.Id)
            .Select(b => new CuttingPlanBundleDto(b.Id, b.BundleNo, b.Size.Name, b.Quantity, b.Status))
            .ToListAsync(cancellationToken);
        var cutBlocked = await CutBlockedReasonAsync(plan.ProductionOrder, cancellationToken);
        var fabricItemIds = plan.Lots.Select(l => l.StockLot.StockItemId).Distinct().ToList();
        var hasRecipe = await db.ProductMaterials
            .AnyAsync(m => m.ProductId == plan.ProductionOrder.ProductId && !fabricItemIds.Contains(m.StockItemId), cancellationToken);

        return ToDto(plan, states, balances, bundles, cutBlocked, hasRecipe);
    }

    public async Task<Result<OrderCuttingSummaryDto>> GetOrderSummaryAsync(int orderId, CancellationToken cancellationToken)
    {
        var order = await db.ProductionOrders
            .AsNoTracking()
            .Include(o => o.Product)
            .SingleOrDefaultAsync(o => o.Id == orderId, cancellationToken);
        if (order is null)
        {
            return OrderNotFound;
        }

        var states = await LoadItemStatesAsync(orderId, null, cancellationToken);
        var plans = await Project(db.CuttingPlans.AsNoTracking().Where(p => p.ProductionOrderId == orderId).OrderBy(p => p.Id))
            .ToListAsync(cancellationToken);
        var planBlocked = PlanBlockedReason(order.Status);
        var cutBlocked = await CutBlockedReasonAsync(order, cancellationToken);

        return new OrderCuttingSummaryDto(
            order.Id,
            order.OrderNo,
            order.Status,
            planBlocked is null,
            planBlocked,
            cutBlocked is null,
            cutBlocked,
            states
                .Select(s => new CuttingSummaryItemDto(
                    s.ColorId, s.ColorName, s.SizeId, s.SizeName, s.Ordered, s.Cut, s.BundledOutside, s.Remaining, s.PlannedInDrafts))
                .ToList(),
            plans);
    }

    public async Task<IReadOnlyList<CuttingLotOptionDto>> GetLotOptionsAsync(CuttingLotQuery query, CancellationToken cancellationToken)
    {
        var take = Math.Clamp(query.Take, 1, 200);

        List<int> recipeItemIds = [];
        if (query.ProductionOrderId is { } orderId)
        {
            var productId = await db.ProductionOrders
                .AsNoTracking()
                .Where(o => o.Id == orderId)
                .Select(o => (int?)o.ProductId)
                .SingleOrDefaultAsync(cancellationToken);
            if (productId is not null)
            {
                recipeItemIds = await db.ProductMaterials
                    .AsNoTracking()
                    .Where(m => m.ProductId == productId)
                    .Select(m => m.StockItemId)
                    .Distinct()
                    .ToListAsync(cancellationToken);
            }
        }

        var stocks = db.Stocks
            .AsNoTracking()
            .Where(s => s.LotId != null && s.Quantity > 0 && s.StockItem.ItemType == StockItemType.Fabric);

        if (query.StockItemId is { } stockItemId)
        {
            stocks = stocks.Where(s => s.StockItemId == stockItemId);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            stocks = stocks.Where(s =>
                s.Lot!.LotNo.Contains(term)
                || s.StockItem.Code.Contains(term)
                || s.StockItem.Name.Contains(term)
                || s.Lot.BatchNo!.Contains(term)
                || s.Lot.Shade!.Contains(term)
                || s.Lot.SupplierRollNo!.Contains(term));
        }

        // Bir top birden fazla adreste durabilir: satırlar topa göre birleştirilir.
        var rows = await stocks
            .OrderBy(s => recipeItemIds.Contains(s.StockItemId) ? 0 : 1)
            .ThenBy(s => s.StockItem.Code)
            .ThenBy(s => s.LotId)
            .Take(take * 4)
            .Select(s => new
            {
                LotId = s.LotId!.Value,
                s.Lot!.LotNo,
                s.StockItemId,
                s.StockItem.Code,
                s.StockItem.Name,
                s.StockItem.Unit,
                s.Lot.BatchNo,
                s.Lot.Shade,
                s.Lot.Width,
                s.Lot.SupplierRollNo,
                s.Quantity,
                WarehouseCode = s.Warehouse.Code,
                LocationCode = s.Location.Code,
            })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(r => r.LotId)
            .Take(take)
            .Select(g =>
            {
                var first = g.First();
                return new CuttingLotOptionDto(
                    first.LotId,
                    first.LotNo,
                    first.StockItemId,
                    first.Code,
                    first.Name,
                    first.Unit,
                    first.BatchNo,
                    first.Shade,
                    first.Width,
                    first.SupplierRollNo,
                    g.Sum(r => r.Quantity),
                    string.Join(", ", g.Select(r => $"{r.WarehouseCode} / {r.LocationCode}").Distinct()),
                    recipeItemIds.Contains(first.StockItemId));
            })
            .ToList();
    }

    public Task<Result<CuttingPlanDto>> CreateAsync(CuttingPlanRequest request, CancellationToken cancellationToken) =>
        SaveAsync(null, request, cancellationToken);

    public Task<Result<CuttingPlanDto>> UpdateAsync(int id, CuttingPlanRequest request, CancellationToken cancellationToken) =>
        SaveAsync(id, request, cancellationToken);

    private async Task<Result<CuttingPlanDto>> SaveAsync(int? id, CuttingPlanRequest request, CancellationToken cancellationToken)
    {
        CuttingPlan? plan = null;
        if (id is not null)
        {
            plan = await db.CuttingPlans
                .Include(p => p.Sizes)
                .Include(p => p.Lots)
                .SingleOrDefaultAsync(p => p.Id == id, cancellationToken);
            if (plan is null)
            {
                return NotFound;
            }

            if (RowVersionToken.Check(plan.RowVersion, request.RowVersion, ConcurrencyConflict) is { } versionError)
            {
                return versionError;
            }

            if (!plan.IsEditable)
            {
                return new Error(InvalidState, "Yalnızca taslak kesim planı değiştirilebilir.", ErrorType.Conflict);
            }

            if (plan.ProductionOrderId != request.ProductionOrderId)
            {
                return ReferenceValidation.InvalidReference("productionOrderId", "Kesim planının siparişi değiştirilemez.");
            }
        }

        var order = await db.ProductionOrders
            .AsNoTracking()
            .Include(o => o.Items)
            .SingleOrDefaultAsync(o => o.Id == request.ProductionOrderId, cancellationToken);
        if (order is null)
        {
            return ReferenceValidation.InvalidReference("productionOrderId", "Üretim siparişi bulunamadı.");
        }

        if (PlanBlockedReason(order.Status) is { } blocked)
        {
            return new Error(InvalidState, blocked, ErrorType.Conflict);
        }

        var colorSizes = order.Items.Where(i => i.ColorId == request.ColorId).Select(i => i.SizeId).ToHashSet();
        if (colorSizes.Count == 0)
        {
            return ReferenceValidation.InvalidReference("colorId", "Siparişte bu renk yok.");
        }

        if (request.Sizes.Any(s => !colorSizes.Contains(s.SizeId)))
        {
            return ReferenceValidation.InvalidReference("sizes", "Siparişte bu rengin olmayan bir bedeni girildi.");
        }

        var lotIds = request.LotIds ?? [];
        if (await ValidateLotsAsync(lotIds, cancellationToken) is { } lotError)
        {
            return lotError;
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (plan is null)
        {
            var year = timeProvider.GetUtcNow().Year;
            var sequence = await numbers.NextAsync(Series, year, cancellationToken);
            plan = new CuttingPlan
            {
                PlanNo = $"{NumberPrefix}-{year}-{sequence.ToString(CultureInfo.InvariantCulture).PadLeft(5, '0')}",
                ProductionOrderId = order.Id,
            };
            db.CuttingPlans.Add(plan);
        }

        plan.ColorId = request.ColorId;
        plan.MarkerLength = request.MarkerLength;
        plan.EndAllowance = request.EndAllowance ?? CuttingPlan.DefaultEndAllowance;
        plan.PlannedPlies = request.PlannedPlies;
        plan.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();

        // Satırlar yerinde güncellenir: (plan, beden) ve (plan, top) benzersizdir.
        foreach (var removed in plan.Sizes.Where(s => request.Sizes.All(r => r.SizeId != s.SizeId)).ToList())
        {
            plan.Sizes.Remove(removed);
            db.CuttingPlanSizes.Remove(removed);
        }

        foreach (var size in request.Sizes)
        {
            var line = plan.Sizes.FirstOrDefault(s => s.SizeId == size.SizeId);
            if (line is null)
            {
                line = new CuttingPlanSize { SizeId = size.SizeId };
                plan.Sizes.Add(line);
            }

            line.Ratio = size.Ratio;
        }

        foreach (var removed in plan.Lots.Where(l => !lotIds.Contains(l.StockLotId)).ToList())
        {
            plan.Lots.Remove(removed);
            db.CuttingPlanLots.Remove(removed);
        }

        foreach (var (lotId, index) in lotIds.Select((lotId, index) => (lotId, index)))
        {
            var line = plan.Lots.FirstOrDefault(l => l.StockLotId == lotId);
            if (line is null)
            {
                line = new CuttingPlanLot { StockLotId = lotId };
                plan.Lots.Add(line);
            }

            line.SortOrder = index + 1;
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
        return await GetAsync(plan.Id, cancellationToken);
    }

    public async Task<Result<CuttingPlanDto>> CancelAsync(int id, string rowVersion, CancellationToken cancellationToken)
    {
        var plan = await db.CuttingPlans.SingleOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (plan is null)
        {
            return NotFound;
        }

        if (RowVersionToken.Check(plan.RowVersion, rowVersion, ConcurrencyConflict) is { } versionError)
        {
            return versionError;
        }

        if (!plan.IsEditable)
        {
            return new Error(
                InvalidState,
                "Yalnızca taslak kesim planı iptal edilebilir. Kesilmiş planın demetleri gerekirse demet ekranından iptal edilir.",
                ErrorType.Conflict);
        }

        plan.Cancel();
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ConcurrencyConflict;
        }

        return await GetAsync(id, cancellationToken);
    }

    public async Task<Result<CuttingPlanDto>> CutAsync(int id, CuttingPlanCutRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (!await appLock.TryAcquireAsync($"cutting-plan:{id}", LockTimeout, cancellationToken))
        {
            return Busy;
        }

        var plan = await db.CuttingPlans
            .IgnoreQueryFilters()
            .Include(p => p.Sizes)
            .Include(p => p.Lots).ThenInclude(l => l.StockLot).ThenInclude(l => l.StockItem)
            .SingleOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (plan is null)
        {
            return NotFound;
        }

        if (RowVersionToken.Check(plan.RowVersion, request.RowVersion, ConcurrencyConflict) is { } versionError)
        {
            return versionError;
        }

        if (!plan.IsEditable)
        {
            return new Error(InvalidState, "Yalnızca taslak kesim planının kesimi kaydedilebilir.", ErrorType.Conflict);
        }

        // Beden adetleri: girilmeyen beden oran × gerçek kat sayılır.
        var requested = (request.Sizes ?? []).ToDictionary(s => s.SizeId, s => s.Quantity);
        if (requested.Keys.Any(sizeId => plan.Sizes.All(s => s.SizeId != sizeId)))
        {
            return ReferenceValidation.InvalidReference("sizes", "Planda olmayan bir beden girildi.");
        }

        foreach (var size in plan.Sizes)
        {
            size.CutQuantity = requested.TryGetValue(size.SizeId, out var quantity) ? quantity : size.Ratio * request.ActualPlies;
        }

        if (plan.Sizes.Sum(s => s.CutQuantity ?? 0) == 0)
        {
            return ReferenceValidation.InvalidReference("sizes", "Kesilen adet girilmemiş.");
        }

        var lotLines = new List<(int LotId, decimal Quantity)>();
        if (plan.Lots.Count > 0)
        {
            if (await AssignFabricUsageAsync(plan, request, cancellationToken) is { } fabricError)
            {
                return fabricError;
            }

            lotLines = plan.Lots
                .OrderBy(l => l.SortOrder).ThenBy(l => l.Id)
                .Where(l => l.UsedQuantity > 0)
                .Select(l => (LotId: l.StockLotId, Quantity: l.UsedQuantity!.Value))
                .ToList();
        }

        plan.MarkCut(request.ActualPlies, timeProvider.GetUtcNow().UtcDateTime, currentUser.UserName);

        // Demetler, öteki demetlerle aynı kurallarla açılır; atanan topların kumaşı reçete düşümünden çıkarılır.
        var fabricItemIds = plan.Lots.Select(l => l.StockLot.StockItemId).Distinct().ToList();
        var items = plan.Sizes
            .Where(s => s.CutQuantity > 0)
            .Select(s => new CuttingBundleItem(plan.ColorId, s.SizeId, s.CutQuantity!.Value))
            .ToList();

        Result<IReadOnlyList<int>> created;
        try
        {
            created = await bundleService.CreateForCuttingAsync(
                new CuttingBundleRequest(plan.ProductionOrderId, plan.Id, request.BundleSize, items, fabricItemIds, request.WarehouseId),
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ConcurrencyConflict;
        }

        if (!created.IsSuccess)
        {
            return created.Error!;
        }

        if (lotLines.Count > 0)
        {
            var orderNo = await db.ProductionOrders
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Where(o => o.Id == plan.ProductionOrderId)
                .Select(o => o.OrderNo)
                .SingleAsync(cancellationToken);
            if (await stockLedger.PostLotIssueAsync(
                    plan.ProductionOrderId, plan.PlanNo, $"Kesim ({orderNo})", lotLines, cancellationToken) is { } stockError)
            {
                return stockError;
            }

            await db.SaveChangesAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    /// <summary>
    /// Toplardan kullanılan miktarı plana yazar: girildiyse girilen; girilmediyse (pastal boyu + pay) × gerçek kat,
    /// toplara serim sırasıyla, her topun bakiyesi kadar dağıtılır (yalnızca metreyle izlenen kumaşta).
    /// </summary>
    private async Task<Error?> AssignFabricUsageAsync(CuttingPlan plan, CuttingPlanCutRequest request, CancellationToken cancellationToken)
    {
        var lots = plan.Lots.OrderBy(l => l.SortOrder).ThenBy(l => l.Id).ToList();

        if (request.LotUsages is { } usages && usages.Any(u => u.Quantity > 0))
        {
            if (usages.Any(u => lots.All(l => l.StockLotId != u.LotId)))
            {
                return ReferenceValidation.InvalidReference("lotUsages", "Plana atanmamış bir top girildi.");
            }

            foreach (var lot in lots)
            {
                lot.UsedQuantity = usages.FirstOrDefault(u => u.LotId == lot.StockLotId)?.Quantity ?? 0;
            }

            return null;
        }

        var item = lots[0].StockLot.StockItem;
        if (item.Unit != StockUnit.Meter)
        {
            var message = $"{item.Code} kumaşı metreyle izlenmiyor; her toptan kullanılan miktarı girin.";
            return new Error(
                ErrorCodes.ValidationError, message, ErrorType.Validation,
                new Dictionary<string, string[]> { ["lotUsages"] = [message] });
        }

        var need = CuttingPlan.FabricFor(plan.MarkerLength, plan.EndAllowance, request.ActualPlies);
        var balances = await LoadLotBalancesAsync(lots.Select(l => l.StockLotId).ToList(), cancellationToken);
        var left = need;
        foreach (var lot in lots)
        {
            var taken = Math.Min(balances.GetValueOrDefault(lot.StockLotId)?.Quantity ?? 0, left);
            lot.UsedQuantity = taken;
            left -= taken;
        }

        if (left > 0)
        {
            return new Error(
                ErrorCodes.InsufficientStock,
                $"Atanan toplarda yeterli kumaş yok: gereken {Format(need)} m, toplarda {Format(need - left)} m. " +
                "Plana top ekleyin ya da her toptan kullanılan miktarı girin.");
        }

        return null;
    }

    /// <summary>
    /// Toplar: hepsi var, kumaş, aynı kart, stoğu olan ve tek tonlu olmalıdır. Ton bilgisi olan toplarda ton, hiçbirinde
    /// ton yoksa tedarikçi partisi (boya partisi) karşılaştırılır.
    /// </summary>
    private async Task<Error?> ValidateLotsAsync(IReadOnlyList<int> lotIds, CancellationToken cancellationToken)
    {
        if (lotIds.Count == 0)
        {
            return null;
        }

        var lots = await db.StockLots
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(l => lotIds.Contains(l.Id))
            .Select(l => new LotInfo(l.Id, l.LotNo, l.StockItemId, l.StockItem.Code, l.StockItem.ItemType, l.BatchNo, l.Shade))
            .ToListAsync(cancellationToken);
        if (lots.Count != lotIds.Distinct().Count())
        {
            return ReferenceValidation.InvalidReference("lotIds", "Seçilen toplardan biri bulunamadı.");
        }

        if (lots.FirstOrDefault(l => l.ItemType != StockItemType.Fabric) is { } notFabric)
        {
            return LotError($"{notFabric.LotNo} bir kumaş topu değil.");
        }

        if (lots.Select(l => l.StockItemId).Distinct().Count() > 1)
        {
            var items = lots.GroupBy(l => l.ItemCode).Select(g => $"{g.Key}: {string.Join(", ", g.Select(l => l.LotNo))}");
            return LotError($"Bir pastaldaki toplar aynı kumaştan olmalıdır ({string.Join("; ", items)}).");
        }

        var balances = await LoadLotBalancesAsync(lotIds, cancellationToken);
        var empty = lots.Where(l => !balances.ContainsKey(l.Id)).Select(l => l.LotNo).ToList();
        if (empty.Count > 0)
        {
            return LotError($"Stoğu kalmamış top seçilemez: {string.Join(", ", empty)}.");
        }

        return CheckShade(lots);
    }

    private static Error LotError(string message) =>
        new(InvalidLot, message, ErrorType.Validation, new Dictionary<string, string[]> { ["lotIds"] = [message] });

    private static Error? CheckShade(IReadOnlyList<LotInfo> lots)
    {
        if (lots.Count < 2)
        {
            return null;
        }

        var byShade = lots.Any(l => !string.IsNullOrWhiteSpace(l.Shade));
        var groups = lots.GroupBy(l => Normalize(byShade ? l.Shade : l.BatchNo)).ToList();
        if (groups.Count == 1)
        {
            return null;
        }

        var label = byShade ? "Ton" : "Parti";
        var detail = string.Join(
            "; ",
            groups.Select(g => $"{(g.Key.Length == 0 ? $"{label} belirtilmemiş" : $"{label} {g.Key}")}: {string.Join(", ", g.Select(l => l.LotNo))}"));
        var message =
            $"Bir pastalda tek ton kumaş kullanılır; seçilen toplar farklı {(byShade ? "tonlarda" : "partilerde")} ({detail}). " +
            "Farklı tondaki topları ayrı pastala alın.";
        return new Error(MixedShade, message, ErrorType.Validation, new Dictionary<string, string[]> { ["lotIds"] = [message] });
    }

    private static string Normalize(string? value) => value?.Trim().ToUpperInvariant() ?? "";

    /// <summary>Plan açılabilir mi: yalnızca onaylı ya da üretimdeki siparişte.</summary>
    private static string? PlanBlockedReason(ProductionOrderStatus status) =>
        status is ProductionOrderStatus.Released or ProductionOrderStatus.InProduction
            ? null
            : "Kesim planı yalnızca onaylı veya üretimdeki siparişler için açılabilir ve kesilebilir.";

    /// <summary>Kesim kaydedilebilir mi: kesim demet açtığı için modelin etkin rotası da gerekir.</summary>
    private async Task<string?> CutBlockedReasonAsync(ProductionOrder order, CancellationToken cancellationToken)
    {
        if (PlanBlockedReason(order.Status) is { } blocked)
        {
            return blocked;
        }

        var modelId = await db.Products
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(p => p.Id == order.ProductId)
            .Select(p => p.ModelId)
            .SingleAsync(cancellationToken);
        var hasRoute = await db.ProductionRoutes
            .AnyAsync(r => r.ModelId == modelId && r.Status == RouteStatus.Active, cancellationToken);
        return hasRoute
            ? null
            : "Siparişin modeli için etkin bir operasyon rotası yok. Kesimde demet açıldığı için önce rotayı etkinleştirin.";
    }

    /// <summary>
    /// Siparişin renk-beden kalemleri ve kesim durumları. <paramref name="excludePlanId"/> verilirse o plan kesilen ve
    /// taslak adetlere katılmaz (planın kendi karşılaştırması için).
    /// </summary>
    private async Task<IReadOnlyList<ItemState>> LoadItemStatesAsync(int orderId, int? excludePlanId, CancellationToken cancellationToken)
    {
        var items = await db.ProductionOrderItems
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(i => i.ProductionOrderId == orderId)
            .OrderBy(i => i.Color.ColorCode).ThenBy(i => i.Size.SortOrder).ThenBy(i => i.Size.SizeCode)
            .Select(i => new { i.ColorId, ColorName = i.Color.Name, i.SizeId, SizeName = i.Size.Name, i.Quantity })
            .ToListAsync(cancellationToken);

        // Kesilmiş planlarda kesilen, taslak planlarda planlanan (oran × kat) adet; iptal edilen planlar sayılmaz.
        var planned = await db.CuttingPlanSizes
            .AsNoTracking()
            .Where(s => s.CuttingPlan.ProductionOrderId == orderId
                && s.CuttingPlanId != excludePlanId
                && s.CuttingPlan.Status != CuttingPlanStatus.Cancelled)
            .Select(s => new
            {
                s.CuttingPlan.ColorId,
                s.SizeId,
                IsCut = s.CuttingPlan.Status == CuttingPlanStatus.Cut,
                Quantity = s.CuttingPlan.Status == CuttingPlanStatus.Cut ? s.CutQuantity ?? 0 : s.Ratio * s.CuttingPlan.PlannedPlies,
            })
            .ToListAsync(cancellationToken);

        var cut = planned
            .Where(p => p.IsCut)
            .GroupBy(p => (p.ColorId, p.SizeId))
            .ToDictionary(g => g.Key, g => g.Sum(p => p.Quantity));
        var drafts = planned
            .Where(p => !p.IsCut)
            .GroupBy(p => (p.ColorId, p.SizeId))
            .ToDictionary(g => g.Key, g => g.Sum(p => p.Quantity));

        var bundled = await db.Bundles
            .AsNoTracking()
            .Where(b => b.ProductionOrderId == orderId && b.Status != BundleStatus.Cancelled && b.CuttingPlanId == null)
            .GroupBy(b => new { b.ColorId, b.SizeId })
            .Select(g => new { g.Key.ColorId, g.Key.SizeId, Quantity = g.Sum(b => b.Quantity) })
            .ToDictionaryAsync(g => (g.ColorId, g.SizeId), g => g.Quantity, cancellationToken);

        return items
            .Select(i => new ItemState(
                i.ColorId,
                i.ColorName,
                i.SizeId,
                i.SizeName,
                i.Quantity,
                cut.GetValueOrDefault((i.ColorId, i.SizeId)),
                bundled.GetValueOrDefault((i.ColorId, i.SizeId)),
                drafts.GetValueOrDefault((i.ColorId, i.SizeId))))
            .ToList();
    }

    /// <summary>Topların bütün depo ve adreslerdeki toplam bakiyesi; stoğu kalmamış top sözlükte yoktur.</summary>
    private async Task<Dictionary<int, LotBalance>> LoadLotBalancesAsync(
        IReadOnlyCollection<int> lotIds, CancellationToken cancellationToken)
    {
        if (lotIds.Count == 0)
        {
            return [];
        }

        var rows = await db.Stocks
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(s => s.LotId != null && lotIds.Contains(s.LotId.Value) && s.Quantity > 0)
            .Select(s => new { LotId = s.LotId!.Value, s.Quantity, WarehouseCode = s.Warehouse.Code, LocationCode = s.Location.Code })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(r => r.LotId)
            .ToDictionary(
                g => g.Key,
                g => new LotBalance(
                    g.Sum(r => r.Quantity),
                    string.Join(", ", g.Select(r => $"{r.WarehouseCode} / {r.LocationCode}").Distinct())));
    }

    private static IQueryable<CuttingPlanListItemDto> Project(IQueryable<CuttingPlan> plans) =>
        plans.Select(p => new CuttingPlanListItemDto(
            p.Id,
            p.PlanNo,
            p.ProductionOrderId,
            p.ProductionOrder.OrderNo,
            p.ProductionOrder.Product.ProductCode,
            p.ProductionOrder.Product.Model.ModelCode,
            p.Color.Name,
            p.MarkerLength,
            p.PlannedPlies,
            p.ActualPlies,
            p.Sizes.Sum(s => s.Ratio),
            p.Sizes.Sum(s => s.Ratio) * p.PlannedPlies,
            p.Status == CuttingPlanStatus.Cut ? (int?)p.Sizes.Sum(s => s.CutQuantity ?? 0) : null,
            (p.MarkerLength + p.EndAllowance) * p.PlannedPlies,
            p.Status,
            p.CreatedAt,
            p.CutAt));

    private static CuttingPlanDto ToDto(
        CuttingPlan plan,
        IReadOnlyDictionary<int, ItemState> states,
        IReadOnlyDictionary<int, LotBalance> balances,
        IReadOnlyList<CuttingPlanBundleDto> bundles,
        string? cutBlockedReason,
        bool hasRecipe)
    {
        var isCut = plan.Status == CuttingPlanStatus.Cut;
        var sizes = plan.Sizes
            .OrderBy(s => s.Size.SortOrder).ThenBy(s => s.Size.SizeCode)
            .Select(s =>
            {
                var state = states.GetValueOrDefault(s.SizeId);
                var remaining = state?.Remaining ?? 0;
                var planned = s.Ratio * plan.PlannedPlies;
                var compared = isCut ? s.CutQuantity ?? 0 : planned;
                return new CuttingPlanSizeDto(
                    s.SizeId, s.Size.Name, s.Ratio, planned, state?.Ordered ?? 0, remaining, compared - remaining, s.CutQuantity);
            })
            .ToList();

        var lots = plan.Lots
            .OrderBy(l => l.SortOrder).ThenBy(l => l.Id)
            .Select(l =>
            {
                var balance = balances.GetValueOrDefault(l.StockLotId);
                return new CuttingPlanLotDto(
                    l.Id,
                    l.StockLotId,
                    l.StockLot.LotNo,
                    l.StockLot.StockItemId,
                    l.StockLot.StockItem.Code,
                    l.StockLot.StockItem.Name,
                    l.StockLot.StockItem.Unit,
                    l.StockLot.BatchNo,
                    l.StockLot.Shade,
                    l.StockLot.Width,
                    l.StockLot.SupplierRollNo,
                    balance?.Quantity ?? 0,
                    balance?.Locations ?? "",
                    l.SortOrder,
                    l.UsedQuantity);
            })
            .ToList();

        var warnings = new List<string>();
        foreach (var size in sizes.Where(s => s.Difference > 0))
        {
            warnings.Add(isCut
                ? $"{size.SizeName}: kesilen {size.CutQuantity}, kesilecek kalan {Math.Max(size.Remaining, 0)}; {size.Difference} adet fazla kesildi."
                : $"{size.SizeName}: planlanan {size.PlannedQuantity}, kesilecek kalan {Math.Max(size.Remaining, 0)}; {size.Difference} adet fazla kesim.");
        }

        if (plan.Status == CuttingPlanStatus.Draft)
        {
            if (lots.Count == 0)
            {
                warnings.Add("Kumaş topu atanmamış: kesimde kumaş reçeteye göre, depodaki en eski toplardan düşülür.");
            }
            else if (lots.All(l => l.Unit == StockUnit.Meter) && lots.Sum(l => l.Balance) < plan.PlannedFabric)
            {
                warnings.Add(
                    $"Atanan toplarda {Format(lots.Sum(l => l.Balance))} m kumaş var; pastal ihtiyacı {Format(plan.PlannedFabric)} m.");
            }
        }

        var shadeLot = lots.FirstOrDefault(l => !string.IsNullOrWhiteSpace(l.Shade));
        var shadeKey = shadeLot is not null
            ? $"Ton {shadeLot.Shade}"
            : lots.FirstOrDefault(l => !string.IsNullOrWhiteSpace(l.BatchNo)) is { } batchLot ? $"Parti {batchLot.BatchNo}" : null;

        return new CuttingPlanDto(
            plan.Id,
            plan.PlanNo,
            plan.ProductionOrderId,
            plan.ProductionOrder.OrderNo,
            plan.ProductionOrder.Status,
            plan.ProductionOrder.Customer.Name,
            plan.ProductionOrder.Product.ProductCode,
            plan.ProductionOrder.Product.Name,
            plan.ProductionOrder.Product.Model.ModelCode,
            plan.ColorId,
            plan.Color.Name,
            plan.MarkerLength,
            plan.EndAllowance,
            plan.PlannedPlies,
            plan.ActualPlies,
            plan.PiecesPerPly,
            plan.PiecesPerPly * plan.PlannedPlies,
            isCut ? (int?)plan.Sizes.Sum(s => s.CutQuantity ?? 0) : null,
            plan.PlannedFabric,
            isCut && lots.Count > 0 ? (decimal?)lots.Sum(l => l.UsedQuantity ?? 0) : null,
            CuttingPlan.SuggestPlies(sizes.Select(s => (s.Ratio, s.Remaining))),
            shadeKey,
            plan.Status,
            plan.Notes,
            plan.IsEditable,
            plan.IsEditable && cutBlockedReason is null,
            plan.IsEditable ? cutBlockedReason : null,
            warnings,
            plan.CreatedAt,
            plan.CreatedBy,
            plan.CutAt,
            plan.CutBy,
            RowVersionToken.From(plan.RowVersion),
            sizes,
            lots,
            bundles,
            hasRecipe);
    }

    private static string Format(decimal quantity) => quantity.ToString("#,0.###", Turkish);
}
