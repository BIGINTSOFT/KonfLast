using ERP.Konfeksiyon.Application.Abstractions;
using ERP.Core.Application.Abstractions;
using ERP.Konfeksiyon.Application.Features.Warehousing;
using ERP.Konfeksiyon.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ERP.Konfeksiyon.Application.Features.Sales;

public interface ISalesOrderFulfillment
{
    /// <summary>
    /// Siparişin bütün kalemleri (ürün / renk / beden) sipariş adedince sevk edildiyse siparişi kendiliğinden kapatır.
    /// Sevkiyat kaydedildikten sonra, aynı işlem içinde çağrılır.
    /// </summary>
    Task<bool> CloseIfFullyShippedAsync(int salesOrderId, string shipmentNo, CancellationToken cancellationToken);
}

public sealed class SalesOrderFulfillment(IKonfeksiyonDbContext db, IStockLedger stockLedger, ICurrentUser currentUser, TimeProvider timeProvider)
    : ISalesOrderFulfillment
{
    public async Task<bool> CloseIfFullyShippedAsync(int salesOrderId, string shipmentNo, CancellationToken cancellationToken)
    {
        var order = await db.SalesOrders.Include(o => o.Lines).SingleOrDefaultAsync(o => o.Id == salesOrderId, cancellationToken);
        if (order is null || order.Status != SalesOrderStatus.Approved || order.Lines.Count == 0)
        {
            return false;
        }

        var shipped = (await db.ShipmentItems.AsNoTracking()
                .Where(i => i.Shipment.SalesOrderId == salesOrderId && i.Shipment.Status == ShipmentStatus.Shipped)
                .GroupBy(i => new { i.StockItem.ProductId, i.StockItem.ColorId, i.StockItem.SizeId })
                .Select(g => new { g.Key.ProductId, g.Key.ColorId, g.Key.SizeId, Quantity = g.Sum(i => i.Quantity) })
                .ToListAsync(cancellationToken))
            .ToDictionary(g => (g.ProductId, g.ColorId, g.SizeId), g => g.Quantity);

        var complete = order.Lines.All(line =>
            shipped.GetValueOrDefault(((int?)line.ProductId, (int?)line.ColorId, (int?)line.SizeId)) >= line.Quantity);
        if (!complete)
        {
            return false;
        }

        order.Close(
            timeProvider.GetUtcNow().UtcDateTime, currentUser.UserName ?? "system",
            $"Tamamı sevk edildi ({shipmentNo}); sipariş kendiliğinden kapandı.", automatic: true);
        // Sevkiyatta tüketilmeyen ayırma kalmışsa (ör. başka depodan sevk) bırakılır.
        await stockLedger.ReleaseSalesOrderReservationsAsync(salesOrderId, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
