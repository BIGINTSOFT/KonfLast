using ERP.Konfeksiyon.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Konfeksiyon.Application.Features.Warehousing;
using ERP.Konfeksiyon.Domain.Entities;
using ERP.Konfeksiyon.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ERP.Konfeksiyon.Application.Features.Sales;

/// <summary>Siparişin bir kaleminin karşılanma durumu: stoktan ayrılan, üretimde, planlanacak.</summary>
/// <param name="Shipped">Bu kaleme sevk edilen toplam adet.</param>
/// <param name="Reserved">Depoda bu kaleme ayrılmış duran adet.</param>
/// <param name="ShippedFromReservation">Ayırmadan sevk edilmiş adet.</param>
/// <param name="InProduction">Siparişten açılmış (iptal edilmemiş) üretim siparişlerindeki adet.</param>
/// <param name="ToPlan">Henüz ne stoktan ne üretimden karşılanan adet (planlamaya düşer).</param>
/// <param name="Available">Kalemin hazır mamul kartında başka siparişlere ayrılmamış stok (bu kalemin ayırması dahil).</param>
/// <param name="HasStockItem">Kalem için hazır mamul stok kartı var (yoksa ayırma yapılamaz).</param>
public sealed record SalesOrderReservationLineDto(
    int LineId,
    int ProductId,
    string ProductCode,
    string ProductName,
    int ColorId,
    string ColorName,
    int SizeId,
    string SizeName,
    int SizeSortOrder,
    int Ordered,
    int Shipped,
    decimal Reserved,
    decimal ShippedFromReservation,
    int InProduction,
    int ToPlan,
    decimal Available,
    bool HasStockItem);

/// <param name="CanEdit">Ayırma yalnızca onaylı siparişte değiştirilebilir.</param>
public sealed record SalesOrderReservationsDto(int SalesOrderId, string OrderNo, bool CanEdit, IReadOnlyList<SalesOrderReservationLineDto> Lines);

public sealed record SalesOrderReservationLineRequest(int LineId, decimal Quantity);

/// <summary>Kalemlerin istenen ayırma adedi; listede olmayan kalemlerin ayırması değişmez.</summary>
public sealed record SetSalesOrderReservationsRequest(IReadOnlyList<SalesOrderReservationLineRequest> Lines);

public sealed class SetSalesOrderReservationsRequestValidator : AbstractValidator<SetSalesOrderReservationsRequest>
{
    public SetSalesOrderReservationsRequestValidator()
    {
        RuleFor(x => x.Lines).NotNull().WithMessage("Kalem listesi zorunludur.");
        RuleForEach(x => x.Lines)
            .Must(l => l.Quantity >= 0 && decimal.Round(l.Quantity) == l.Quantity)
            .WithMessage("Ayırma adedi sıfır ya da pozitif tam sayı olmalıdır.");
    }
}

public interface ISalesOrderReservationService
{
    Task<Result<SalesOrderReservationsDto>> GetAsync(int salesOrderId, CancellationToken cancellationToken);

    /// <summary>Kalemlerin ayırmasını elle değiştirir; stok yetmezse kullanılabilir kadar ayrılır.</summary>
    Task<Result<SalesOrderReservationsDto>> SetAsync(
        int salesOrderId, SetSalesOrderReservationsRequest request, CancellationToken cancellationToken);

    /// <summary>Onaylanan siparişe, sevk edilmemiş adet kadar hazır mamul ayırır (stok yettiğince).</summary>
    Task ReserveRemainingAsync(int salesOrderId, CancellationToken cancellationToken);

    /// <summary>Siparişin bütün ayırmalarını bırakır (revizyon, iptal, kapanış).</summary>
    Task ReleaseAsync(int salesOrderId, CancellationToken cancellationToken);
}

public sealed class SalesOrderReservationService(IKonfeksiyonDbContext db, IStockLedger stockLedger) : ISalesOrderReservationService
{
    private static readonly Error NotFound = new(ErrorCodes.NotFound, "Müşteri siparişi bulunamadı.", ErrorType.NotFound);

    public async Task<Result<SalesOrderReservationsDto>> GetAsync(int salesOrderId, CancellationToken cancellationToken)
    {
        var order = await db.SalesOrders.AsNoTracking()
            .Where(o => o.Id == salesOrderId)
            .Select(o => new { o.Id, o.OrderNo, o.Status })
            .SingleOrDefaultAsync(cancellationToken);
        if (order is null)
        {
            return NotFound;
        }

        var lines = await db.SalesOrderLines.AsNoTracking().IgnoreQueryFilters()
            .Where(l => l.SalesOrderId == salesOrderId)
            .Select(l => new
            {
                l.Id, l.ProductId, l.Product.ProductCode, ProductName = l.Product.Name, l.ColorId, ColorName = l.Color.Name,
                l.SizeId, SizeName = l.Size.Name, l.Size.SortOrder, l.Quantity, l.ShippedFromReservation,
            })
            .ToListAsync(cancellationToken);
        var coverage = await SalesOrderCoverage.LoadAsync(db, [salesOrderId], cancellationToken);
        var stockItems = await FinishedItemsAsync(lines.Select(l => (l.ProductId, l.ColorId, l.SizeId)), cancellationToken);
        var itemIds = stockItems.Values.ToList();

        var onHand = await db.Stocks.AsNoTracking()
            .Where(s => itemIds.Contains(s.StockItemId))
            .GroupBy(s => s.StockItemId)
            .Select(g => new { g.Key, Quantity = g.Sum(s => s.Quantity) })
            .ToDictionaryAsync(g => g.Key, g => g.Quantity, cancellationToken);
        var reservedByOthers = (await db.StockReservations.AsNoTracking()
                .Where(r => itemIds.Contains(r.StockItemId))
                .Select(r => new { r.StockItemId, r.Quantity })
                .Concat(db.SalesOrderReservations.AsNoTracking()
                    .Where(r => itemIds.Contains(r.StockItemId) && r.SalesOrderId != salesOrderId)
                    .Select(r => new { r.StockItemId, r.Quantity }))
                .ToListAsync(cancellationToken))
            .GroupBy(r => r.StockItemId)
            .ToDictionary(g => g.Key, g => g.Sum(r => r.Quantity));

        var result = lines
            .OrderBy(l => l.ProductCode, StringComparer.Ordinal).ThenBy(l => l.ColorName).ThenBy(l => l.SortOrder)
            .Select(l =>
            {
                var key = (l.ProductId, l.ColorId, l.SizeId);
                var line = coverage.GetValueOrDefault((salesOrderId, l.ProductId, l.ColorId, l.SizeId));
                var hasItem = stockItems.TryGetValue(key, out var itemId);
                return new SalesOrderReservationLineDto(
                    l.Id, l.ProductId, l.ProductCode, l.ProductName, l.ColorId, l.ColorName, l.SizeId, l.SizeName, l.SortOrder,
                    l.Quantity, line.Shipped, line.Reserved, l.ShippedFromReservation, line.InProduction,
                    SalesOrderCoverage.ToPlan(l.Quantity, line.Reserved, l.ShippedFromReservation, line.InProduction),
                    hasItem ? Math.Max(onHand.GetValueOrDefault(itemId) - reservedByOthers.GetValueOrDefault(itemId), 0) : 0,
                    hasItem);
            })
            .ToList();

        return new SalesOrderReservationsDto(order.Id, order.OrderNo, order.Status == SalesOrderStatus.Approved, result);
    }

    public async Task<Result<SalesOrderReservationsDto>> SetAsync(
        int salesOrderId, SetSalesOrderReservationsRequest request, CancellationToken cancellationToken)
    {
        var order = await db.SalesOrders.AsNoTracking().Include(o => o.Lines)
            .SingleOrDefaultAsync(o => o.Id == salesOrderId, cancellationToken);
        if (order is null)
        {
            return NotFound;
        }

        if (order.Status != SalesOrderStatus.Approved)
        {
            return new Error(ErrorCodes.InvalidStatusTransition, "Stok ayırması yalnızca onaylı siparişte değiştirilebilir.");
        }

        var unknown = request.Lines.FirstOrDefault(r => order.Lines.All(l => l.Id != r.LineId));
        if (unknown is not null)
        {
            return ReferenceValidation.InvalidReference("lines", "Kalem bu siparişe ait değil.");
        }

        var shipped = await SalesOrderCoverage.LoadAsync(db, [salesOrderId], cancellationToken);
        var targets = request.Lines.Select(r =>
        {
            var line = order.Lines.Single(l => l.Id == r.LineId);
            // Ayrılabilecek en fazla adet: henüz sevk edilmemiş kısım.
            var open = Math.Max(line.Quantity - shipped.GetValueOrDefault((salesOrderId, line.ProductId, line.ColorId, line.SizeId)).Shipped, 0);
            return (Line: line, Target: Math.Min(r.Quantity, open));
        }).ToList();

        var error = await ApplyAsync(salesOrderId, targets, cancellationToken);
        return error ?? await GetAsync(salesOrderId, cancellationToken);
    }

    public async Task ReserveRemainingAsync(int salesOrderId, CancellationToken cancellationToken)
    {
        var lines = await db.SalesOrderLines.AsNoTracking().Where(l => l.SalesOrderId == salesOrderId).ToListAsync(cancellationToken);
        var coverage = await SalesOrderCoverage.LoadAsync(db, [salesOrderId], cancellationToken);
        var targets = lines
            .Select(l => (Line: l, Target: (decimal)Math.Max(
                l.Quantity - coverage.GetValueOrDefault((salesOrderId, l.ProductId, l.ColorId, l.SizeId)).Shipped, 0)))
            .ToList();

        // Stok yoksa ya da kilit alınamazsa sipariş ayırmasız kalır; ayırma sipariş ekranından sonra yapılabilir.
        await ApplyAsync(salesOrderId, targets, cancellationToken);
    }

    public async Task ReleaseAsync(int salesOrderId, CancellationToken cancellationToken)
    {
        await stockLedger.ReleaseSalesOrderReservationsAsync(salesOrderId, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<Error?> ApplyAsync(
        int salesOrderId, IReadOnlyList<(SalesOrderLine Line, decimal Target)> targets, CancellationToken cancellationToken)
    {
        var stockItems = await FinishedItemsAsync(targets.Select(t => (t.Line.ProductId, t.Line.ColorId, t.Line.SizeId)), cancellationToken);
        var resolved = targets
            .Where(t => stockItems.ContainsKey((t.Line.ProductId, t.Line.ColorId, t.Line.SizeId)))
            .Select(t => (t.Line.Id, stockItems[(t.Line.ProductId, t.Line.ColorId, t.Line.SizeId)], t.Target))
            .ToList();
        if (resolved.Count == 0)
        {
            return null;
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (await stockLedger.ReserveForSalesOrderAsync(salesOrderId, resolved, cancellationToken) is { } error)
        {
            return error;
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return null;
    }

    /// <summary>Kalemlerin hazır mamul stok kartları (ürün + renk + beden); kartı olmayan kalem listede yer almaz.</summary>
    private async Task<Dictionary<(int ProductId, int ColorId, int SizeId), int>> FinishedItemsAsync(
        IEnumerable<(int ProductId, int ColorId, int SizeId)> keys, CancellationToken cancellationToken)
    {
        var productIds = keys.Select(k => k.ProductId).Distinct().ToList();
        return (await db.StockItems.AsNoTracking()
                .Where(i => i.ItemType == StockItemType.FinishedGood && i.ProductId != null && productIds.Contains(i.ProductId.Value))
                .Select(i => new { i.Id, i.ProductId, i.ColorId, i.SizeId })
                .ToListAsync(cancellationToken))
            .Where(i => i.ColorId is not null && i.SizeId is not null)
            .GroupBy(i => (i.ProductId!.Value, i.ColorId!.Value, i.SizeId!.Value))
            .ToDictionary(g => g.Key, g => g.Min(i => i.Id));
    }
}

/// <summary>Sipariş kalemlerinin sevk, ayırma ve üretim karşılığı (sipariş, ürün, renk, beden başına).</summary>
public static class SalesOrderCoverage
{
    public readonly record struct LineCoverage(int Shipped, decimal Reserved, int InProduction);

    /// <summary>Henüz karşılanmamış (planlamaya düşen) adet: sipariş − stoktan karşılanan − üretimdeki.</summary>
    public static int ToPlan(int ordered, decimal reserved, decimal shippedFromReservation, int inProduction) =>
        (int)Math.Max(ordered - reserved - shippedFromReservation - inProduction, 0);

    public static async Task<Dictionary<(int SalesOrderId, int ProductId, int ColorId, int SizeId), LineCoverage>> LoadAsync(
        IKonfeksiyonDbContext db, IReadOnlyCollection<int> salesOrderIds, CancellationToken cancellationToken)
    {
        var shipped = await db.ShipmentItems.AsNoTracking()
            .Where(i => i.Shipment.SalesOrderId != null && salesOrderIds.Contains(i.Shipment.SalesOrderId.Value)
                && i.Shipment.Status == ShipmentStatus.Shipped
                && i.StockItem.ProductId != null && i.StockItem.ColorId != null && i.StockItem.SizeId != null)
            .GroupBy(i => new { SalesOrderId = i.Shipment.SalesOrderId!.Value, ProductId = i.StockItem.ProductId!.Value, ColorId = i.StockItem.ColorId!.Value, SizeId = i.StockItem.SizeId!.Value })
            .Select(g => new { g.Key.SalesOrderId, g.Key.ProductId, g.Key.ColorId, g.Key.SizeId, Quantity = g.Sum(i => i.Quantity) })
            .ToListAsync(cancellationToken);
        var reserved = await db.SalesOrderReservations.AsNoTracking()
            .Where(r => salesOrderIds.Contains(r.SalesOrderId))
            .GroupBy(r => new { r.SalesOrderId, r.SalesOrderLine.ProductId, r.SalesOrderLine.ColorId, r.SalesOrderLine.SizeId })
            .Select(g => new { g.Key.SalesOrderId, g.Key.ProductId, g.Key.ColorId, g.Key.SizeId, Quantity = g.Sum(r => r.Quantity) })
            .ToListAsync(cancellationToken);
        var production = await db.ProductionOrders.AsNoTracking()
            .Where(p => p.SalesOrderId != null && salesOrderIds.Contains(p.SalesOrderId.Value) && p.Status != ProductionOrderStatus.Cancelled)
            .SelectMany(p => p.Items.Select(i => new
            {
                OrderId = p.Id,
                SalesOrderId = p.SalesOrderId!.Value,
                p.ProductId,
                i.ColorId,
                i.SizeId,
                i.Quantity,
                IsCompleted = p.Status == ProductionOrderStatus.Completed,
                Bundled = db.Bundles.Where(b => b.ProductionOrderId == p.Id && b.Status != BundleStatus.Cancelled && b.ColorId == i.ColorId && b.SizeId == i.SizeId).Sum(b => (int?)b.Quantity) ?? 0,
                Lost = db.Bundles.Where(b => b.ProductionOrderId == p.Id && b.Status != BundleStatus.Cancelled && b.ColorId == i.ColorId && b.SizeId == i.SizeId)
                    .SelectMany(b => b.Operations).Sum(o => (int?)o.RejectedQuantity) ?? 0,
            }))
            .ToListAsync(cancellationToken);

        var result = new Dictionary<(int, int, int, int), LineCoverage>();
        foreach (var s in shipped)
        {
            var key = (s.SalesOrderId, s.ProductId, s.ColorId, s.SizeId);
            result[key] = result.GetValueOrDefault(key) with { Shipped = (int)s.Quantity };
        }

        foreach (var r in reserved)
        {
            var key = (r.SalesOrderId, r.ProductId, r.ColorId, r.SizeId);
            result[key] = result.GetValueOrDefault(key) with { Reserved = r.Quantity };
        }

        // Üretimin karşılığı: fireye ayrılan düşülür, tamamlanan siparişte demetlenmemiş adet de karşılık sayılmaz.
        // Açık kalan adet planlamaya yeniden düşer.
        foreach (var p in production)
        {
            var key = (p.SalesOrderId, p.ProductId, p.ColorId, p.SizeId);
            var covered = Math.Max((p.IsCompleted ? Math.Min(p.Quantity, p.Bundled) : p.Quantity) - p.Lost, 0);
            var current = result.GetValueOrDefault(key);
            result[key] = current with { InProduction = current.InProduction + covered };
        }

        return result;
    }
}
