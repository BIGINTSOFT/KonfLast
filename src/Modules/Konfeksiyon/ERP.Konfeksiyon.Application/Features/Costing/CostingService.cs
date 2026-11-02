using ERP.Konfeksiyon.Application.Abstractions;
using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Konfeksiyon.Application.Features.Payroll;
using ERP.Konfeksiyon.Application.Features.Warehousing;
using ERP.Konfeksiyon.Domain.Entities;
using ERP.Core.Domain.Enums;
using ERP.Konfeksiyon.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ERP.Konfeksiyon.Application.Features.Costing;

public interface IProductMaterialService
{
    Task<Result<ProductMaterialsDto>> GetAsync(int productId, CancellationToken cancellationToken);

    /// <summary>Ürünün reçetesini verilen satırlarla değiştirir. Açılmış demetlerin düşülmüş malzemesi etkilenmez.</summary>
    Task<Result<ProductMaterialsDto>> SaveAsync(
        int productId, SaveProductMaterialsRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Reçeteye eklenebilecek malzemeler (mamul kartları hariç), koda göre. Reçeteyi düzenleyen kullanıcının stok
    /// ekranlarını görme yetkisi olmayabilir; reçete formu malzeme listesini buradan alır.
    /// </summary>
    Task<IReadOnlyList<MaterialOptionDto>> GetOptionsAsync(CancellationToken cancellationToken);
}

public interface ICostingService
{
    /// <summary>Siparişin reçeteye göre malzeme ihtiyacı, düşülen miktar ve depodaki stok.</summary>
    Task<Result<OrderMaterialsDto>> GetOrderMaterialsAsync(int orderId, int? warehouseId, CancellationToken cancellationToken);

    /// <summary>Siparişin gerçekleşen malzeme ve işçilik maliyeti ile standart birim maliyeti.</summary>
    Task<Result<OrderCostDto>> GetOrderCostAsync(int orderId, CancellationToken cancellationToken);

    /// <summary>
    /// Siparişin kalan malzeme ihtiyacını depoda ayırır (stok yetmeyen kalemde yettiği kadar). Yalnızca onaylı ya
    /// da üretimdeki sipariş için; yeniden çağrılırsa rezervasyon güncel ihtiyaca göre yenilenir.
    /// </summary>
    Task<Result<OrderMaterialsDto>> ReserveMaterialsAsync(int orderId, int? warehouseId, CancellationToken cancellationToken);

    /// <summary>Siparişin tüm rezervasyonlarını bırakır.</summary>
    Task<Result<OrderMaterialsDto>> ReleaseReservationsAsync(int orderId, int? warehouseId, CancellationToken cancellationToken);

    /// <summary>
    /// Toplama listesi: siparişin kalan ihtiyacı için hangi adresten, hangi toptan ne kadar alınacağı. Sıra,
    /// demet açılırken yapılan otomatik çıkışla aynıdır (önce seçilen depo, sonra öteki hammadde depoları; her depoda önce
    /// genel adres, her adreste en eski top).
    /// </summary>
    Task<Result<PickListDto>> GetPickListAsync(int orderId, int? warehouseId, CancellationToken cancellationToken);
}

/// <summary>Ürün reçetesi (malzeme listesi).</summary>
public sealed class ProductMaterialService(IKonfeksiyonDbContext db, IPayrollService payroll, ICurrentPermissions permissions)
    : IProductMaterialService
{
    private static readonly Error ProductNotFound = new(ErrorCodes.NotFound, "Ürün bulunamadı.", ErrorType.NotFound);

    public async Task<IReadOnlyList<MaterialOptionDto>> GetOptionsAsync(CancellationToken cancellationToken)
    {
        var options = await db.StockItems
            .AsNoTracking()
            .Where(i => i.ItemType != StockItemType.FinishedGood)
            .OrderBy(i => i.Code)
            .Select(i => new MaterialOptionDto(i.Id, i.Code, i.Name, i.Unit, i.UnitCost, i.IsActive))
            .ToListAsync(cancellationToken);

        return await CostRules.CanSeeCostAsync(permissions, cancellationToken)
            ? options
            : options.Select(o => o with { UnitCost = null }).ToList();
    }

    public async Task<Result<ProductMaterialsDto>> GetAsync(int productId, CancellationToken cancellationToken)
    {
        var product = await db.Products
            .AsNoTracking()
            .Where(p => p.Id == productId)
            .Select(p => new { p.ProductCode, p.Name })
            .SingleOrDefaultAsync(cancellationToken);
        if (product is null)
        {
            return ProductNotFound;
        }

        // Sonradan pasif yapılan / silinen malzeme, renk ve beden de reçetede adıyla görünür.
        var lines = await db.ProductMaterials
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(m => m.ProductId == productId)
            .OrderBy(m => m.StockItem.Code).ThenBy(m => m.Color!.ColorCode).ThenBy(m => m.Size!.SortOrder)
            .Select(m => new ProductMaterialDto(
                m.Id,
                m.StockItemId,
                m.StockItem.Code,
                m.StockItem.Name,
                m.StockItem.ItemType,
                m.StockItem.Unit,
                m.ColorId,
                m.Color == null ? null : m.Color.Name,
                m.SizeId,
                m.Size == null ? null : m.Size.Name,
                m.QuantityPerPiece,
                m.WastePercent,
                m.StockItem.UnitCost,
                m.StockItem.UnitCost == null
                    ? null
                    : m.QuantityPerPiece * (1 + m.WastePercent / 100m) * m.StockItem.UnitCost))
            .ToListAsync(cancellationToken);

        var currency = (await payroll.LoadSettingAsync(cancellationToken)).Currency;
        // Maliyet görme yetkisi yoksa reçete miktarlarıyla döner, maliyet alanları boştur.
        var showCost = await CostRules.CanSeeCostAsync(permissions, cancellationToken);
        return new ProductMaterialsDto(
            productId, product.ProductCode, product.Name, currency,
            lines.Select(l => showCost
                    ? l with { CostPerPiece = l.CostPerPiece is { } cost ? decimal.Round(cost, 4) : null }
                    : l with { UnitCost = null, CostPerPiece = null })
                .ToList());
    }

    public async Task<Result<ProductMaterialsDto>> SaveAsync(
        int productId, SaveProductMaterialsRequest request, CancellationToken cancellationToken)
    {
        if (!await db.Products.AnyAsync(p => p.Id == productId, cancellationToken))
        {
            return ProductNotFound;
        }

        var existing = await db.ProductMaterials.Where(m => m.ProductId == productId).ToListAsync(cancellationToken);

        var stockItemIds = request.Lines.Select(l => l.StockItemId).Distinct().ToList();
        var colorIds = request.Lines.Where(l => l.ColorId is not null).Select(l => l.ColorId!.Value).Distinct().ToList();
        var sizeIds = request.Lines.Where(l => l.SizeId is not null).Select(l => l.SizeId!.Value).Distinct().ToList();

        // Reçetede zaten olan pasif kayıtlar kalabilir; yeni seçilenler aktif olmalıdır.
        var referenceError =
            await ReferenceValidation.RequireSelectableAsync(
                db.StockItems, stockItemIds, existing.Select(m => m.StockItemId).ToHashSet(), "lines", "Stok kartı",
                cancellationToken)
            ?? await ReferenceValidation.RequireSelectableAsync(
                db.Colors, colorIds, existing.Where(m => m.ColorId is not null).Select(m => m.ColorId!.Value).ToHashSet(),
                "lines", "Renk", cancellationToken)
            ?? await ReferenceValidation.RequireSelectableAsync(
                db.Sizes, sizeIds, existing.Where(m => m.SizeId is not null).Select(m => m.SizeId!.Value).ToHashSet(),
                "lines", "Beden", cancellationToken);
        if (referenceError is not null)
        {
            return referenceError;
        }

        var finishedGood = await db.StockItems
            .AsNoTracking()
            .Where(i => stockItemIds.Contains(i.Id) && i.ItemType == StockItemType.FinishedGood)
            .Select(i => i.Code)
            .FirstOrDefaultAsync(cancellationToken);
        if (finishedGood is not null)
        {
            var message = $"{finishedGood}: mamul kartı reçeteye malzeme olarak eklenemez.";
            return new Error(
                ErrorCodes.MaterialNotAllowed, message, ErrorType.BusinessRule,
                new Dictionary<string, string[]> { ["lines"] = [message] });
        }

        // Satırlar (malzeme, renk, beden) anahtarıyla eşitlenir: kalan güncellenir, eksilen silinir, yeni eklenir.
        var wanted = request.Lines.ToDictionary(l => (l.StockItemId, l.ColorId, l.SizeId));
        foreach (var line in existing)
        {
            if (wanted.Remove((line.StockItemId, line.ColorId, line.SizeId), out var update))
            {
                line.QuantityPerPiece = update.QuantityPerPiece;
                line.WastePercent = update.WastePercent;
            }
            else
            {
                db.ProductMaterials.Remove(line);
            }
        }

        foreach (var line in wanted.Values)
        {
            db.ProductMaterials.Add(new ProductMaterial
            {
                ProductId = productId,
                StockItemId = line.StockItemId,
                ColorId = line.ColorId,
                SizeId = line.SizeId,
                QuantityPerPiece = line.QuantityPerPiece,
                WastePercent = line.WastePercent,
            });
        }

        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(productId, cancellationToken);
    }
}

/// <summary>
/// Sipariş bazında malzeme ihtiyacı ve maliyet. Gerçekleşen maliyet değiştirilemeyen stok hareketlerinden
/// (çıkış anındaki birim maliyetle) ve demette sabitlenen birim ücretlerden okunur.
/// </summary>
public sealed class CostingService(IKonfeksiyonDbContext db, IPayrollService payroll, IStockLedger stockLedger) : ICostingService
{
    private static readonly Error OrderNotFound = new(ErrorCodes.NotFound, "Üretim siparişi bulunamadı.", ErrorType.NotFound);

    private sealed record ItemInfo(string Code, string Name, StockUnit Unit, decimal? UnitCost);

    public async Task<Result<OrderMaterialsDto>> GetOrderMaterialsAsync(
        int orderId, int? warehouseId, CancellationToken cancellationToken)
    {
        var order = await LoadOrderAsync(orderId, cancellationToken);
        if (order is null)
        {
            return OrderNotFound;
        }

        var needs = await LoadNeedsAsync(orderId, order, cancellationToken);
        var resolved = await MaterialWarehouse.ResolveAsync(db, warehouseId, order.MaterialWarehouseId, cancellationToken);
        int? stockWarehouseId = resolved.IsSuccess ? resolved.Value : null;
        var warehouseName = stockWarehouseId is null
            ? null
            : await db.Warehouses
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Where(w => w.Id == stockWarehouseId)
                .Select(w => w.Name)
                .SingleOrDefaultAsync(cancellationToken);

        var itemIds = needs.Items.Keys.ToList();
        var stocks = db.Stocks.AsNoTracking().Where(s => itemIds.Contains(s.StockItemId));
        var reservations = db.StockReservations.AsNoTracking().Where(r => itemIds.Contains(r.StockItemId));
        if (stockWarehouseId is { } selectedWarehouse)
        {
            stocks = stocks.Where(s => s.WarehouseId == selectedWarehouse);
            reservations = reservations.Where(r => r.WarehouseId == selectedWarehouse);
        }

        var available = await stocks
            .GroupBy(s => s.StockItemId)
            .Select(g => new { StockItemId = g.Key, Quantity = g.Sum(s => s.Quantity) })
            .ToDictionaryAsync(g => g.StockItemId, g => g.Quantity, cancellationToken);
        var reserved = await reservations
            .GroupBy(r => new { r.StockItemId, Own = r.ProductionOrderId == orderId })
            .Select(g => new { g.Key.StockItemId, g.Key.Own, Quantity = g.Sum(r => r.Quantity) })
            .ToListAsync(cancellationToken);

        // Bu sipariş için açılmış, henüz tamamı teslim alınmamış satın alma kalemleri ("siparişte").
        var purchases = (await db.PurchaseOrderItems.AsNoTracking()
                .Where(i => i.ProductionOrderId == orderId && itemIds.Contains(i.StockItemId)
                    && (i.PurchaseOrder.Status == PurchaseOrderStatus.Draft || i.PurchaseOrder.Status == PurchaseOrderStatus.Approved
                        || i.PurchaseOrder.Status == PurchaseOrderStatus.PartiallyReceived))
                .Select(i => new
                {
                    i.StockItemId, i.PurchaseOrderId, i.PurchaseOrder.OrderNo, i.PurchaseOrder.Status, i.Quantity, i.ReceivedQuantity,
                    i.PurchaseOrder.ExpectedDate,
                })
                .ToListAsync(cancellationToken))
            .Where(p => p.Quantity > p.ReceivedQuantity)
            .ToList();

        // Malzemenin bütün depolardaki stoğu: seçili depoda yetmiyorsa başka depoda olup olmadığı görülür.
        var byWarehouse = await db.Stocks.AsNoTracking()
            .Where(s => itemIds.Contains(s.StockItemId))
            .GroupBy(s => new { s.StockItemId, s.WarehouseId, s.Warehouse.Code, s.Warehouse.Name })
            .Select(g => new { g.Key.StockItemId, g.Key.WarehouseId, g.Key.Code, g.Key.Name, Quantity = g.Sum(s => s.Quantity) })
            .Where(g => g.Quantity > 0)
            .ToListAsync(cancellationToken);
        var warehouseReservations = await db.StockReservations.AsNoTracking()
            .Where(r => itemIds.Contains(r.StockItemId))
            .Select(r => new { r.StockItemId, r.WarehouseId, r.ProductionOrderId, r.Quantity })
            .ToListAsync(cancellationToken);
        var reservedByWarehouse = warehouseReservations
            .GroupBy(r => (r.StockItemId, r.WarehouseId))
            .ToDictionary(g => g.Key, g => g.Sum(r => r.Quantity));
        var reservedForOthersByWarehouse = warehouseReservations
            .Where(r => r.ProductionOrderId != orderId)
            .GroupBy(r => (r.StockItemId, r.WarehouseId))
            .ToDictionary(g => g.Key, g => g.Sum(r => r.Quantity));
        // Demet açılırken seçili depoda yetmeyen malzeme öteki aktif hammadde depolarından düşülür.
        var otherRawWarehouseIds = stockWarehouseId is null
            ? []
            : await db.Warehouses.AsNoTracking()
                .Where(w => w.Id != stockWarehouseId && w.IsActive && w.Type == WarehouseType.RawMaterial)
                .Select(w => w.Id)
                .ToListAsync(cancellationToken);

        var lines = itemIds
            .Select(id =>
            {
                var item = needs.Items[id];
                var stocksOfItem = byWarehouse.Where(w => w.StockItemId == id)
                    .OrderBy(w => w.Code, StringComparer.Ordinal)
                    .Select(w => new OrderMaterialStockDto(
                        w.WarehouseId, w.Code, w.Name, w.Quantity, reservedByWarehouse.GetValueOrDefault((id, w.WarehouseId))))
                    .ToList();
                var need = needs.Remaining.GetValueOrDefault(id);
                var onHand = available.GetValueOrDefault(id);
                var own = reserved.Where(r => r.StockItemId == id && r.Own).Sum(r => r.Quantity);
                var others = reserved.Where(r => r.StockItemId == id && !r.Own).Sum(r => r.Quantity);
                var ordered = purchases.Where(p => p.StockItemId == id)
                    .OrderBy(p => p.ExpectedDate ?? DateOnly.MaxValue)
                    .Select(p => new OrderMaterialPurchaseDto(
                        p.PurchaseOrderId, p.OrderNo, p.Status, p.Quantity, p.Quantity - p.ReceivedQuantity, p.ExpectedDate))
                    .ToList();
                var freeElsewhere = stocksOfItem
                    .Where(w => otherRawWarehouseIds.Contains(w.WarehouseId))
                    .Sum(w => Math.Max(w.OnHand - reservedForOthersByWarehouse.GetValueOrDefault((id, w.WarehouseId)), 0));
                // Başka siparişlere ayrılmış stok bu siparişin ihtiyacını karşılamaz.
                return new OrderMaterialLineDto(
                    id, item.Code, item.Name, item.Unit, needs.Required.GetValueOrDefault(id), needs.Issued.GetValueOrDefault(id),
                    need, onHand, Math.Max(need - Math.Max(onHand - others, 0) - freeElsewhere, 0), own, others,
                    ordered.Sum(p => p.Remaining), ordered, stocksOfItem);
            })
            .OrderBy(l => l.ItemCode, StringComparer.Ordinal)
            .ToList();

        return new OrderMaterialsDto(orderId, needs.Recipe.Count > 0, stockWarehouseId, warehouseName, lines);
    }

    public async Task<Result<OrderMaterialsDto>> ReserveMaterialsAsync(
        int orderId, int? warehouseId, CancellationToken cancellationToken)
    {
        var order = await LoadOrderAsync(orderId, cancellationToken);
        if (order is null)
        {
            return OrderNotFound;
        }

        var entity = await db.ProductionOrders.SingleAsync(o => o.Id == orderId, cancellationToken);
        var status = entity.Status;
        if (status is not (ProductionOrderStatus.Released or ProductionOrderStatus.InProduction))
        {
            return new Error(
                ErrorCodes.InvalidStatusTransition, "Yalnızca onaylı veya üretimdeki sipariş için malzeme ayrılabilir.");
        }

        var warehouse = await MaterialWarehouse.ResolveAsync(db, warehouseId, order.MaterialWarehouseId, cancellationToken);
        if (!warehouse.IsSuccess)
        {
            return warehouse.Error!;
        }

        // Kullanıcının ilk kez seçtiği depo siparişin malzeme deposu olur: sonraki işlemlerde yeniden sorulmaz.
        // Kendiliğinden bulunan depo yazılmaz (onaydaki otomatik ayırma siparişin sürümünü değiştirmesin).
        if (warehouseId is not null)
        {
            entity.MaterialWarehouseId ??= warehouse.Value;
        }

        var needs = await LoadNeedsAsync(orderId, order, cancellationToken);
        if (needs.Recipe.Count == 0)
        {
            return new Error(ErrorCodes.OrderHasNoRecipe, "Ürünün reçetesi yok; ayrılacak malzeme bulunamadı.");
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var targets = needs.Remaining.Select(n => (n.Key, n.Value)).ToList();
        if (await stockLedger.ReserveAsync(warehouse.Value, orderId, targets, cancellationToken) is { } error)
        {
            return error;
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetOrderMaterialsAsync(orderId, warehouse.Value, cancellationToken);
    }

    public async Task<Result<OrderMaterialsDto>> ReleaseReservationsAsync(
        int orderId, int? warehouseId, CancellationToken cancellationToken)
    {
        if (!await db.ProductionOrders.AnyAsync(o => o.Id == orderId, cancellationToken))
        {
            return OrderNotFound;
        }

        await stockLedger.ReleaseReservationsAsync(orderId, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return await GetOrderMaterialsAsync(orderId, warehouseId, cancellationToken);
    }

    public async Task<Result<PickListDto>> GetPickListAsync(int orderId, int? warehouseId, CancellationToken cancellationToken)
    {
        var order = await LoadOrderAsync(orderId, cancellationToken);
        if (order is null)
        {
            return OrderNotFound;
        }

        var warehouse = await MaterialWarehouse.ResolveAsync(db, warehouseId, order.MaterialWarehouseId, cancellationToken);
        if (!warehouse.IsSuccess)
        {
            return warehouse.Error!;
        }

        var header = await db.ProductionOrders
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(o => o.Id == orderId)
            .Select(o => new { o.OrderNo, o.Product.ProductCode, ProductName = o.Product.Name })
            .SingleAsync(cancellationToken);
        var warehouseInfo = await db.Warehouses
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(w => w.Id == warehouse.Value)
            .Select(w => new { w.Code, w.Name })
            .SingleOrDefaultAsync(cancellationToken);
        if (warehouseInfo is null)
        {
            return ReferenceValidation.InvalidReference("warehouseId", "Depo bulunamadı.");
        }

        var needs = await LoadNeedsAsync(orderId, order, cancellationToken);
        var itemIds = needs.Remaining.Where(n => n.Value > 0).Select(n => n.Key).ToList();

        // Demet açılırken olduğu gibi: önce seçilen depo, yetmeyen kısım öteki aktif hammadde depolarından.
        var warehouses = await db.Warehouses
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(w => w.Id == warehouse.Value || (w.IsActive && w.Type == WarehouseType.RawMaterial))
            .Select(w => new { w.Id, w.Code, w.Name })
            .ToListAsync(cancellationToken);
        var warehouseOrder = warehouses.OrderBy(w => w.Id == warehouse.Value ? 0 : 1).ThenBy(w => w.Id).ToList();
        var warehouseIds = warehouseOrder.Select(w => w.Id).ToList();

        // Çıkış sırası: her depoda önce genel adres, sonra adres kodu; her adreste en eski top.
        var rows = await db.Stocks
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(s => warehouseIds.Contains(s.WarehouseId) && itemIds.Contains(s.StockItemId) && s.Quantity > 0)
            .OrderByDescending(s => s.Location.IsDefault).ThenBy(s => s.Location.Code).ThenBy(s => s.LotId)
            .Select(s => new
            {
                s.StockItemId,
                s.WarehouseId,
                LocationCode = s.Location.Code,
                LotNo = s.Lot == null ? null : s.Lot.LotNo,
                BatchNo = s.Lot == null ? null : s.Lot.BatchNo,
                Shade = s.Lot == null ? null : s.Lot.Shade,
                s.Quantity,
            })
            .ToListAsync(cancellationToken);
        var reservedByOthers = await db.StockReservations
            .AsNoTracking()
            .Where(r => warehouseIds.Contains(r.WarehouseId) && itemIds.Contains(r.StockItemId) && r.ProductionOrderId != orderId)
            .GroupBy(r => new { r.StockItemId, r.WarehouseId })
            .Select(g => new { g.Key.StockItemId, g.Key.WarehouseId, Quantity = g.Sum(r => r.Quantity) })
            .ToDictionaryAsync(g => (g.StockItemId, g.WarehouseId), g => g.Quantity, cancellationToken);

        var items = itemIds
            .Select(id =>
            {
                var item = needs.Items[id];
                var need = needs.Remaining[id];
                var lines = new List<PickListLineDto>();
                var left = need;
                foreach (var source in warehouseOrder)
                {
                    var itemRows = rows.Where(r => r.StockItemId == id && r.WarehouseId == source.Id).ToList();
                    // Başka siparişlere ayrılmış stok toplanamaz.
                    var fromWarehouse = Math.Min(
                        left, Math.Max(itemRows.Sum(r => r.Quantity) - reservedByOthers.GetValueOrDefault((id, source.Id)), 0));
                    left -= fromWarehouse;
                    foreach (var row in itemRows)
                    {
                        var taken = Math.Min(row.Quantity, fromWarehouse);
                        if (taken <= 0)
                        {
                            break;
                        }

                        lines.Add(new PickListLineDto(
                            row.LocationCode, row.LotNo, row.BatchNo, row.Shade, taken, row.Quantity, source.Id, source.Code, source.Name));
                        fromWarehouse -= taken;
                    }
                }

                return new PickListItemDto(id, item.Code, item.Name, item.Unit, need, left, lines);
            })
            .OrderBy(i => i.ItemCode, StringComparer.Ordinal)
            .ToList();

        return new PickListDto(
            orderId, header.OrderNo, header.ProductCode, header.ProductName, warehouse.Value, warehouseInfo.Code,
            warehouseInfo.Name, items);
    }

    /// <summary>Siparişin reçeteye göre malzeme ihtiyacı: toplam, kalan (demetlenmemiş adet için) ve düşülen.</summary>
    private sealed record MaterialNeeds(
        IReadOnlyList<ProductMaterial> Recipe,
        IReadOnlyDictionary<int, ItemInfo> Items,
        IReadOnlyDictionary<int, decimal> Required,
        IReadOnlyDictionary<int, decimal> Remaining,
        IReadOnlyDictionary<int, decimal> Issued);

    private async Task<MaterialNeeds> LoadNeedsAsync(int orderId, OrderInfo order, CancellationToken cancellationToken)
    {
        var recipe = await LoadRecipeAsync(order.ProductId, cancellationToken);

        var issued = await db.StockTransactions
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(t => t.ProductionOrderId == orderId
                && (t.Type == StockTransactionType.ProductionIssue || t.Type == StockTransactionType.ProductionReturn))
            .GroupBy(t => t.StockItemId)
            .Select(g => new { StockItemId = g.Key, Quantity = -g.Sum(t => t.Quantity) })
            .ToDictionaryAsync(g => g.StockItemId, g => g.Quantity, cancellationToken);

        var itemIds = recipe.Select(m => m.StockItemId).Concat(issued.Keys).Distinct().ToList();
        var items = await LoadItemsAsync(itemIds, cancellationToken);

        var bundled = await db.Bundles
            .Where(b => b.ProductionOrderId == orderId && b.Status != BundleStatus.Cancelled)
            .GroupBy(b => new { b.ColorId, b.SizeId })
            .Select(g => new { g.Key.ColorId, g.Key.SizeId, Quantity = g.Sum(b => b.Quantity) })
            .ToDictionaryAsync(g => (g.ColorId, g.SizeId), g => g.Quantity, cancellationToken);

        var required = new Dictionary<int, decimal>();
        var remaining = new Dictionary<int, decimal>();
        foreach (var item in order.Items)
        {
            var covered = Math.Min(bundled.GetValueOrDefault((item.ColorId, item.SizeId)), item.Quantity);
            foreach (var line in MaterialRequirement.LinesFor(recipe, item.ColorId, item.SizeId))
            {
                var unit = items[line.StockItemId].Unit;
                required[line.StockItemId] = required.GetValueOrDefault(line.StockItemId)
                    + MaterialRequirement.QuantityFor(line, unit, item.Quantity);
                // Kalan, demetlenince gerçekten düşülecek miktardır (demet çıkışıyla aynı yuvarlama).
                remaining[line.StockItemId] = remaining.GetValueOrDefault(line.StockItemId)
                    + MaterialRequirement.QuantityForNext(line, unit, covered, item.Quantity - covered);
            }
        }

        return new MaterialNeeds(recipe, items, required, remaining, issued);
    }

    public async Task<Result<OrderCostDto>> GetOrderCostAsync(int orderId, CancellationToken cancellationToken)
    {
        var order = await LoadOrderAsync(orderId, cancellationToken);
        if (order is null)
        {
            return OrderNotFound;
        }

        var setting = await payroll.LoadSettingAsync(cancellationToken);

        // Malzeme: çıkışlar eksi, iadeler artı miktarlıdır; net tüketim ve tutar ikisinin toplamından çıkar.
        var consumption = await db.StockTransactions
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(t => t.ProductionOrderId == orderId
                && (t.Type == StockTransactionType.ProductionIssue || t.Type == StockTransactionType.ProductionReturn))
            .GroupBy(t => t.StockItemId)
            .Select(g => new
            {
                StockItemId = g.Key,
                Quantity = -g.Sum(t => t.Quantity),
                Amount = -g.Sum(t => t.Quantity * (t.UnitCost ?? 0)),
                Uncosted = g.Any(t => t.UnitCost == null),
            })
            .ToListAsync(cancellationToken);

        var recipe = await LoadRecipeAsync(order.ProductId, cancellationToken);
        var items = await LoadItemsAsync(
            consumption.Select(c => c.StockItemId).Concat(recipe.Select(m => m.StockItemId)).Distinct().ToList(), cancellationToken);

        var materials = consumption
            .Where(c => c.Quantity != 0)
            .Select(c => new OrderCostMaterialDto(
                c.StockItemId, items[c.StockItemId].Code, items[c.StockItemId].Name, items[c.StockItemId].Unit, c.Quantity,
                c.Uncosted ? null : decimal.Round(c.Amount, 2)))
            .OrderBy(m => m.ItemCode, StringComparer.Ordinal)
            .ToList();
        var materialCost = decimal.Round(consumption.Sum(c => c.Amount), 2);

        // İşçilik: üretilen sağlam adet × demet açılırken sabitlenen birim ücret.
        var labor = await db.BundleOperations
            .AsNoTracking()
            .Where(o => o.Bundle.ProductionOrderId == orderId && o.ProducedQuantity > 0)
            .GroupBy(o => o.UnitRate)
            .Select(g => new { UnitRate = g.Key, Quantity = g.Sum(o => o.ProducedQuantity) })
            .ToListAsync(cancellationToken);
        var laborCost = decimal.Round(labor.Sum(l => l.Quantity * (l.UnitRate ?? 0)), 2);

        var bundledQuantity = await db.Bundles
            .Where(b => b.ProductionOrderId == orderId && b.Status != BundleStatus.Cancelled)
            .SumAsync(b => (int?)b.Quantity, cancellationToken) ?? 0;
        var finishedQuantity = (int)(await db.StockTransactions
            .Where(t => t.ProductionOrderId == orderId && t.Type == StockTransactionType.ProductionReceipt)
            .SumAsync(t => (decimal?)t.Quantity, cancellationToken) ?? 0);

        var orderedQuantity = order.Items.Sum(i => i.Quantity);
        decimal? standardMaterial = null;
        var recipeUncosted = false;
        if (recipe.Count > 0 && orderedQuantity > 0)
        {
            // Renk-bedene özel satırlar olabildiği için parça başı maliyet sipariş adetleriyle ağırlıklandırılır.
            var total = 0m;
            foreach (var item in order.Items)
            {
                foreach (var line in MaterialRequirement.LinesFor(recipe, item.ColorId, item.SizeId))
                {
                    var unitCost = items[line.StockItemId].UnitCost;
                    recipeUncosted |= unitCost is null;
                    total += item.Quantity * line.GrossQuantityPerPiece * (unitCost ?? 0);
                }
            }

            standardMaterial = decimal.Round(total / orderedQuantity, 4);
        }

        var routeSteps = await db.ProductionRouteOperations
            .AsNoTracking()
            .Where(o => o.ProductionRoute.ModelId == order.ModelId && o.ProductionRoute.Status == RouteStatus.Active)
            .Select(o => new { o.Sam, o.OperationId })
            .ToListAsync(cancellationToken);
        decimal? standardLabor = null;
        var routeUnpriced = false;
        if (routeSteps.Count > 0)
        {
            var operationIds = routeSteps.Select(s => s.OperationId).Distinct().ToList();
            var pieceRates = await db.Operations
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Where(o => operationIds.Contains(o.Id))
                .ToDictionaryAsync(o => o.Id, o => o.PieceRate, cancellationToken);
            var rates = routeSteps.Select(s => setting.UnitRateFor(pieceRates.GetValueOrDefault(s.OperationId), s.Sam)).ToList();
            routeUnpriced = rates.Any(r => r is null);
            standardLabor = rates.Sum(r => r ?? 0);
        }

        var totalCost = materialCost + laborCost;
        return new OrderCostDto(
            orderId,
            setting.Currency,
            orderedQuantity,
            bundledQuantity,
            finishedQuantity,
            materialCost,
            laborCost,
            totalCost,
            finishedQuantity > 0 ? decimal.Round(totalCost / finishedQuantity, 4) : null,
            standardMaterial,
            standardLabor,
            standardMaterial is null && standardLabor is null ? null : (standardMaterial ?? 0) + (standardLabor ?? 0),
            consumption.Any(c => c.Uncosted && c.Quantity != 0) || recipeUncosted,
            labor.Any(l => l.UnitRate is null) || routeUnpriced,
            materials);
    }

    private sealed record OrderInfo(
        int ProductId, int ModelId, IReadOnlyList<ProductionOrderItem> Items, int? MaterialWarehouseId = null);

    private async Task<OrderInfo?> LoadOrderAsync(int orderId, CancellationToken cancellationToken)
    {
        var order = await db.ProductionOrders
            .AsNoTracking()
            .Where(o => o.Id == orderId)
            .Select(o => new { o.ProductId, ModelId = o.Product.ModelId, o.MaterialWarehouseId })
            .SingleOrDefaultAsync(cancellationToken);
        if (order is null)
        {
            return null;
        }

        var items = await db.ProductionOrderItems
            .AsNoTracking()
            .Where(i => i.ProductionOrderId == orderId)
            .ToListAsync(cancellationToken);
        return new OrderInfo(order.ProductId, order.ModelId, items, order.MaterialWarehouseId);
    }

    private Task<List<ProductMaterial>> LoadRecipeAsync(int productId, CancellationToken cancellationToken) =>
        db.ProductMaterials.AsNoTracking().Where(m => m.ProductId == productId).ToListAsync(cancellationToken);

    private async Task<Dictionary<int, ItemInfo>> LoadItemsAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken) =>
        await db.StockItems
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(i => ids.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, i => new ItemInfo(i.Code, i.Name, i.Unit, i.UnitCost), cancellationToken);
}
