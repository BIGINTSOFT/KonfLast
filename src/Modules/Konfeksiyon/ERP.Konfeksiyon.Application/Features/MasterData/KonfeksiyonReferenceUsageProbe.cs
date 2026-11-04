using ERP.Konfeksiyon.Domain.Entities;
using ERP.Core.Application.Abstractions;
using ERP.Core.Domain.Entities;
using ERP.Konfeksiyon.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace ERP.Konfeksiyon.Application.Features.MasterData;

/// <summary>Tanım kayıtlarının Konfeksiyon verisinde (sipariş, üretim, sevkiyat, rota) kullanılıp kullanılmadığı.</summary>
public sealed class KonfeksiyonReferenceUsageProbe(IKonfeksiyonDbContext db) : IReferenceUsageProbe
{
    public async Task<string?> FindUsageAsync(Type entityType, object key, CancellationToken cancellationToken)
    {
        if (entityType == typeof(Currency))
        {
            var code = (string)key;
            if (await db.SalesOrders.IgnoreQueryFilters().AnyAsync(o => o.Currency == code, cancellationToken))
            {
                return "müşteri siparişleri";
            }

            if (await db.Customers.AnyAsync(c => c.Currency == code, cancellationToken))
            {
                return "müşteriler";
            }

            return await db.Products.AnyAsync(p => p.ListPrice != null && p.Currency == code, cancellationToken) ? "ürünler" : null;
        }

        var id = (int)key;

        if (entityType == typeof(Customer))
        {
            if (await db.SalesOrders.AnyAsync(s => s.CustomerId == id, cancellationToken))
            {
                return "müşteri siparişleri";
            }

            if (await db.Shipments.AnyAsync(s => s.CustomerId == id, cancellationToken))
            {
                return "sevkiyatlar";
            }

            if (await db.AssortmentTemplates.AnyAsync(t => t.CustomerId == id, cancellationToken))
            {
                return "asorti şablonları";
            }

            return await db.ProductionOrders.AnyAsync(o => o.CustomerId == id, cancellationToken)
                ? "iş emirleri"
                : null;
        }

        if (entityType == typeof(CustomerAddress))
        {
            return await db.SalesOrders.IgnoreQueryFilters().AnyAsync(o => o.DeliveryAddressId == id, cancellationToken)
                ? "müşteri siparişlerinde teslim adresi olarak"
                : null;
        }

        if (entityType == typeof(CustomerBuyer))
        {
            return await db.SalesOrders.IgnoreQueryFilters().AnyAsync(o => o.CustomerBuyerId == id, cancellationToken)
                ? "müşteri siparişleri"
                : null;
        }

        if (entityType == typeof(Product))
        {
            if (await db.ProductionOrders.AnyAsync(o => o.ProductId == id, cancellationToken))
            {
                return "iş emirleri";
            }

            return await db.SalesOrderLines.AnyAsync(l => l.ProductId == id, cancellationToken)
                ? "müşteri siparişleri"
                : null;
        }

        if (entityType == typeof(Model))
        {
            return await db.ProductionRoutes.AnyAsync(r => r.ModelId == id, cancellationToken) ? "rotalar" : null;
        }

        if (entityType == typeof(Color))
        {
            return await db.ProductionOrderItems.AnyAsync(i => i.ColorId == id, cancellationToken)
                ? "iş emirleri"
                : null;
        }

        if (entityType == typeof(Size))
        {
            if (await db.ProductionOrderItems.AnyAsync(i => i.SizeId == id, cancellationToken))
            {
                return "iş emirleri";
            }

            return await db.AssortmentTemplateLines.AnyAsync(l => l.SizeId == id, cancellationToken)
                ? "asorti şablonları"
                : null;
        }

        return null;
    }
}
