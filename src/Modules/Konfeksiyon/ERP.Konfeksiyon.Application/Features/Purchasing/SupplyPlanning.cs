using ERP.Konfeksiyon.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Konfeksiyon.Application.Features.Warehousing;
using ERP.Konfeksiyon.Domain.Entities;
using ERP.Core.Domain.Enums;
using ERP.Konfeksiyon.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ERP.Konfeksiyon.Application.Features.Purchasing;

/// <summary>Bir kartın ihtiyacını doğuran açık üretim siparişi.</summary>
public sealed record SupplyDemandDto(int ProductionOrderId, string OrderNo, DateOnly DueDate, decimal Quantity);

/// <param name="OnHand">Tüm depolardaki toplam stok.</param>
/// <param name="Demand">Onaylı ve üretimdeki siparişlerin henüz malzemesi çıkılmamış adetleri için reçete ihtiyacı.</param>
/// <param name="OnOrder">Onaylı satın alma siparişlerinde henüz teslim alınmamış miktar.</param>
/// <param name="InDraft">Taslak satın alma siparişlerindeki miktar.</param>
/// <param name="Shortage">İhtiyaç + asgari stok − stok − yoldaki − taslaktaki; eksi olamaz.</param>
/// <param name="SuggestedQuantity">Eksik için önerilen sipariş miktarı (sipariş katından az olmaz).</param>
public sealed record SupplySuggestionDto(
    int StockItemId,
    string ItemCode,
    string ItemName,
    StockItemType ItemType,
    StockUnit Unit,
    decimal OnHand,
    decimal Demand,
    decimal MinimumStock,
    decimal OnOrder,
    decimal InDraft,
    decimal Shortage,
    decimal SuggestedQuantity,
    int? SupplierId,
    string? SupplierName,
    IReadOnlyList<SupplyDemandDto> Demands);

public sealed class SupplySuggestionQuery
{
    /// <summary>Kart kodu ya da adı içinde aranır.</summary>
    public string? Search { get; set; }
    public StockItemType? ItemType { get; set; }
    public int? StockItemId { get; set; }

    /// <summary>true: eksiği olmayan ama ihtiyacı ya da asgari stoğu tanımlı kartlar da listelenir.</summary>
    public bool IncludeCovered { get; set; }
}

/// <param name="SupplierId">Boşsa kartın varsayılan tedarikçisi kullanılır.</param>
public sealed record SupplyOrderLineRequest(int StockItemId, decimal Quantity, int? SupplierId = null);

/// <param name="ExpectedDate">Açılan siparişlere yazılacak beklenen teslim tarihi (isteğe bağlı).</param>
public sealed record CreateSupplyOrdersRequest(IReadOnlyList<SupplyOrderLineRequest> Lines, DateOnly? ExpectedDate = null);

public sealed class CreateSupplyOrdersRequestValidator : AbstractValidator<CreateSupplyOrdersRequest>
{
    public CreateSupplyOrdersRequestValidator()
    {
        RuleFor(x => x.Lines).Must(lines => lines is { Count: > 0 }).WithMessage("En az bir satır seçilmelidir.");
        When(x => x.Lines is { Count: > 0 }, () =>
        {
            RuleFor(x => x.Lines)
                .Must(lines => lines.Count <= PurchaseOrderRequestValidator.MaxItems)
                .WithMessage($"Tek seferde en fazla {PurchaseOrderRequestValidator.MaxItems} satır siparişe dönüştürülebilir.")
                .Must(lines => lines.All(l => l.StockItemId > 0)).WithMessage("Her satırda stok kartı belirtilmelidir.")
                .Must(lines => lines.All(l =>
                    l.Quantity > 0 && l.Quantity <= StockRules.MaxQuantity && decimal.Round(l.Quantity, 3) == l.Quantity))
                .WithMessage("Sipariş miktarı sıfırdan büyük olmalı ve en fazla 3 ondalık basamak içermelidir.")
                .Must(lines => lines.Select(l => l.StockItemId).Distinct().Count() == lines.Count)
                .WithMessage("Aynı stok kartı birden fazla satırda yer alamaz.");
        });
    }
}

public interface ISupplyPlanningService
{
    /// <summary>Tedarik önerisi: açık siparişlerin ihtiyacı ve asgari stok için eksik kalan malzemeler.</summary>
    Task<IReadOnlyList<SupplySuggestionDto>> GetSuggestionsAsync(SupplySuggestionQuery query, CancellationToken cancellationToken);

    /// <summary>Seçilen satırları tedarikçiye göre gruplayıp her tedarikçi için bir taslak satın alma siparişi açar.</summary>
    Task<Result<IReadOnlyList<PurchaseOrderDto>>> CreateOrdersAsync(
        CreateSupplyOrdersRequest request, CancellationToken cancellationToken);
}

/// <summary>
/// Tedarik planlama. Öneri kayıt tutmaz, her çağrıda güncel durumdan hesaplanır: taslak siparişe dönüşen eksik,
/// taslaktaki miktar olarak sayıldığı için ikinci kez önerilmez.
/// </summary>
public sealed class SupplyPlanningService(
    IKonfeksiyonDbContext db, IPurchaseOrderService purchaseOrders, TimeProvider timeProvider) : ISupplyPlanningService
{
    private const string OrderNote = "Tedarik önerisinden oluşturuldu.";

    public async Task<IReadOnlyList<SupplySuggestionDto>> GetSuggestionsAsync(
        SupplySuggestionQuery query, CancellationToken cancellationToken)
    {
        // Pasif kart da ihtiyacı varsa görünür; silinmiş kart önerilmez.
        var items = db.StockItems.AsNoTracking().Where(i => i.ItemType != StockItemType.FinishedGood);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            items = items.Where(i => i.Code.Contains(search) || i.Name.Contains(search));
        }

        if (query.ItemType is { } itemType)
        {
            items = items.Where(i => i.ItemType == itemType);
        }

        if (query.StockItemId is { } stockItemId)
        {
            items = items.Where(i => i.Id == stockItemId);
        }

        var cards = await items
            .Select(i => new { Item = i, SupplierName = i.Supplier == null ? null : i.Supplier.Name })
            .ToListAsync(cancellationToken);
        var units = cards.ToDictionary(c => c.Item.Id, c => c.Item.Unit);

        var demands = await LoadDemandsAsync(units, cancellationToken);

        var onHand = await db.Stocks
            .AsNoTracking()
            .Where(s => s.StockItem.ItemType != StockItemType.FinishedGood)
            .GroupBy(s => s.StockItemId)
            .Select(g => new { StockItemId = g.Key, Quantity = g.Sum(s => s.Quantity) })
            .ToDictionaryAsync(g => g.StockItemId, g => g.Quantity, cancellationToken);

        var incoming = await db.PurchaseOrderItems
            .AsNoTracking()
            .Where(i => !i.PurchaseOrder.IsDeleted
                && (i.PurchaseOrder.Status == PurchaseOrderStatus.Draft
                    || i.PurchaseOrder.Status == PurchaseOrderStatus.Approved
                    || i.PurchaseOrder.Status == PurchaseOrderStatus.PartiallyReceived))
            .GroupBy(i => new { i.StockItemId, i.PurchaseOrder.Status })
            .Select(g => new { g.Key.StockItemId, g.Key.Status, Quantity = g.Sum(i => i.Quantity - i.ReceivedQuantity) })
            .ToListAsync(cancellationToken);
        var inDraft = incoming
            .Where(i => i.Status == PurchaseOrderStatus.Draft)
            .ToDictionary(i => i.StockItemId, i => i.Quantity);
        var onOrder = incoming
            .Where(i => i.Status != PurchaseOrderStatus.Draft)
            .GroupBy(i => i.StockItemId)
            .ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity));

        return cards
            .Select(card =>
            {
                var item = card.Item;
                var itemDemands = demands.GetValueOrDefault(item.Id) ?? [];
                var demand = itemDemands.Sum(d => d.Quantity);
                var stock = onHand.GetValueOrDefault(item.Id);
                var ordered = onOrder.GetValueOrDefault(item.Id);
                var draft = inDraft.GetValueOrDefault(item.Id);
                var shortage = item.ShortageFor(stock, ordered + draft, demand);
                return new SupplySuggestionDto(
                    item.Id, item.Code, item.Name, item.ItemType, item.Unit, stock, demand, item.MinimumStock ?? 0, ordered,
                    draft, shortage, item.SuggestedOrderQuantity(shortage), item.SupplierId, card.SupplierName,
                    itemDemands.OrderBy(d => d.DueDate).ThenBy(d => d.OrderNo, StringComparer.Ordinal).ToList());
            })
            .Where(s => s.Shortage > 0 || (query.IncludeCovered && (s.Demand > 0 || s.MinimumStock > 0)))
            .OrderBy(s => s.ItemCode, StringComparer.Ordinal)
            .ToList();
    }

    public async Task<Result<IReadOnlyList<PurchaseOrderDto>>> CreateOrdersAsync(
        CreateSupplyOrdersRequest request, CancellationToken cancellationToken)
    {
        var itemIds = request.Lines.Select(l => l.StockItemId).ToList();
        var items = await db.StockItems
            .AsNoTracking()
            .Where(i => itemIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, cancellationToken);

        // Siparişler ayrı ayrı açıldığı için bütün satırlar önce denetlenir: biri hatalıysa hiçbiri açılmaz.
        var missing = request.Lines.FirstOrDefault(l => !items.TryGetValue(l.StockItemId, out var item) || !item.IsActive);
        if (missing is not null)
        {
            return ReferenceValidation.InvalidReference("lines", "Stok kartı bulunamadı ya da pasif.");
        }

        var finished = request.Lines.Select(l => items[l.StockItemId]).FirstOrDefault(i => i.IsFinishedGood);
        if (finished is not null)
        {
            return LineError(ErrorCodes.MaterialNotAllowed, $"{finished.Code}: mamul kartı satın alma siparişine eklenemez.");
        }

        var fractional = request.Lines.FirstOrDefault(l => !items[l.StockItemId].Accepts(l.Quantity));
        if (fractional is not null)
        {
            return LineError(
                ErrorCodes.StockQuantityInvalid,
                $"{items[fractional.StockItemId].Code}: adetle izlenen stok kartında miktar tam sayı olmalıdır.",
                ErrorType.Validation);
        }

        var lines = request.Lines
            .Select(l => new { Line = l, SupplierId = l.SupplierId ?? items[l.StockItemId].SupplierId })
            .ToList();
        var unassigned = lines.FirstOrDefault(l => l.SupplierId is null);
        if (unassigned is not null)
        {
            return LineError(
                ErrorCodes.SupplySupplierRequired,
                $"{items[unassigned.Line.StockItemId].Code}: tedarikçi seçilmelidir (kartta varsayılan tedarikçi tanımlı değil).");
        }

        var supplierIds = lines.Select(l => l.SupplierId!.Value).Distinct().ToList();
        if (await ReferenceValidation.RequireSelectableAsync(
                db.Suppliers, supplierIds, new HashSet<int>(), "lines", "Tedarikçi", cancellationToken) is { } supplierError)
        {
            return supplierError;
        }

        var today = DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);
        if (request.ExpectedDate is { } expected && expected < today)
        {
            const string message = "Beklenen teslim tarihi bugünden önce olamaz.";
            return new Error(
                ErrorCodes.ValidationError, message, ErrorType.Validation,
                new Dictionary<string, string[]> { ["expectedDate"] = [message] });
        }

        var created = new List<PurchaseOrderDto>();
        foreach (var group in lines.GroupBy(l => l.SupplierId!.Value).OrderBy(g => g.Key))
        {
            var result = await purchaseOrders.CreateAsync(
                new PurchaseOrderRequest(
                    group.Key, today, request.ExpectedDate, OrderNote,
                    group.Select(l => new PurchaseOrderItemRequest(l.Line.StockItemId, l.Line.Quantity)).ToList()),
                cancellationToken);
            if (!result.IsSuccess)
            {
                return result.Error!;
            }

            created.Add(result.Value);
        }

        return created;
    }

    /// <summary>
    /// Kart başına açık ihtiyaç: onaylı ve üretimdeki siparişlerin (ve onaylı müşteri siparişinden açılmış taslakların)
    /// henüz demetlenmemiş adetleri için reçete miktarı (demet açılırken malzemesi stoktan çıkılır, o yüzden
    /// demetlenen adet ihtiyaçtan düşer).
    /// </summary>
    private async Task<Dictionary<int, List<SupplyDemandDto>>> LoadDemandsAsync(
        IReadOnlyDictionary<int, StockUnit> units, CancellationToken cancellationToken)
    {
        var orders = await db.ProductionOrders
            .AsNoTracking()
            // Onaylı müşteri siparişinden açılmış taslak üretim siparişi de ihtiyaç sayılır: tedarik, üretim onayını beklemez.
            .Where(o => o.Status == ProductionOrderStatus.Released || o.Status == ProductionOrderStatus.InProduction
                || (o.Status == ProductionOrderStatus.Draft && o.SalesOrder != null && !o.SalesOrder.IsDeleted
                    && o.SalesOrder.Status == SalesOrderStatus.Approved))
            .Select(o => new
            {
                o.Id,
                o.OrderNo,
                o.DueDate,
                o.ProductId,
                Items = o.Items.Select(i => new { i.ColorId, i.SizeId, i.Quantity }).ToList(),
            })
            .ToListAsync(cancellationToken);
        if (orders.Count == 0)
        {
            return [];
        }

        var bundled = (await db.Bundles
                .AsNoTracking()
                .Where(b => b.Status != BundleStatus.Cancelled
                    && (b.ProductionOrder.Status == ProductionOrderStatus.Released
                        || b.ProductionOrder.Status == ProductionOrderStatus.InProduction))
                .GroupBy(b => new { b.ProductionOrderId, b.ColorId, b.SizeId })
                .Select(g => new { g.Key.ProductionOrderId, g.Key.ColorId, g.Key.SizeId, Quantity = g.Sum(b => b.Quantity) })
                .ToListAsync(cancellationToken))
            .ToDictionary(b => (b.ProductionOrderId, b.ColorId, b.SizeId), b => b.Quantity);

        var productIds = orders.Select(o => o.ProductId).Distinct().ToList();
        var recipes = (await db.ProductMaterials
                .AsNoTracking()
                .Where(m => productIds.Contains(m.ProductId))
                .ToListAsync(cancellationToken))
            .ToLookup(m => m.ProductId);

        var demands = new Dictionary<int, List<SupplyDemandDto>>();
        foreach (var order in orders)
        {
            var needs = new Dictionary<int, decimal>();
            foreach (var item in order.Items)
            {
                var unbundled = Math.Max(item.Quantity - bundled.GetValueOrDefault((order.Id, item.ColorId, item.SizeId)), 0);
                if (unbundled == 0)
                {
                    continue;
                }

                foreach (var line in MaterialRequirement.LinesFor(recipes[order.ProductId], item.ColorId, item.SizeId))
                {
                    // Süzülmüş ya da silinmiş kartın ihtiyacı listede yer almaz.
                    if (units.TryGetValue(line.StockItemId, out var unit))
                    {
                        needs[line.StockItemId] = needs.GetValueOrDefault(line.StockItemId)
                            + MaterialRequirement.QuantityForNext(line, unit, item.Quantity - unbundled, unbundled);
                    }
                }
            }

            foreach (var (stockItemId, quantity) in needs)
            {
                if (!demands.TryGetValue(stockItemId, out var list))
                {
                    demands[stockItemId] = list = [];
                }

                list.Add(new SupplyDemandDto(order.Id, order.OrderNo, order.DueDate, quantity));
            }
        }

        return demands;
    }

    private static Error LineError(string code, string message, ErrorType type = ErrorType.BusinessRule) =>
        new(code, message, type, new Dictionary<string, string[]> { ["lines"] = [message] });
}
