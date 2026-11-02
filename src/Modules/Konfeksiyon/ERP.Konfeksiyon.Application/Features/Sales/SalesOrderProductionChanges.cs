using ERP.Konfeksiyon.Application.Abstractions;
using System.Text.Json;
using ERP.Core.Application.Common;
using ERP.Konfeksiyon.Domain.Entities;
using ERP.Core.Domain.Enums;
using ERP.Konfeksiyon.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ERP.Konfeksiyon.Application.Features.Sales;

/// <summary>Bir renk / bedenin müşteri siparişindeki ve üretim siparişindeki adedi.</summary>
public sealed record ProductionDifferenceItemDto(
    string ColorName, string SizeName, int SizeSortOrder, int Ordered, int Produced, int Difference);

/// <summary>Üretim siparişi kalemindeki bir değişiklik: kim, ne zaman, hangi renk / bedende, adet neden neye.</summary>
/// <param name="Action">Eklendi, değiştirildi ya da silindi.</param>
public sealed record ProductionItemChangeDto(
    DateTime At, string UserName, AuditAction Action, string ColorName, string SizeName, int? OldQuantity, int? NewQuantity);

public sealed record ProductionOrderDifferenceDto(
    int ProductionOrderId,
    string OrderNo,
    ProductionOrderStatus Status,
    string ProductCode,
    IReadOnlyList<ProductionDifferenceItemDto> Items,
    IReadOnlyList<ProductionItemChangeDto> Changes);

public interface ISalesOrderProductionChangesService
{
    /// <summary>
    /// Siparişten açılmış üretim siparişlerinin renk / beden bazında sipariş adedinden farkı ve üretim kalemlerinde
    /// kimin ne değiştirdiği (işlem kayıtlarından).
    /// </summary>
    Task<Result<IReadOnlyList<ProductionOrderDifferenceDto>>> GetAsync(int salesOrderId, CancellationToken cancellationToken);
}

public sealed class SalesOrderProductionChangesService(IKonfeksiyonDbContext db) : ISalesOrderProductionChangesService
{
    private const string ItemEntity = nameof(ProductionOrderItem);

    public async Task<Result<IReadOnlyList<ProductionOrderDifferenceDto>>> GetAsync(
        int salesOrderId, CancellationToken cancellationToken)
    {
        if (!await db.SalesOrders.AnyAsync(o => o.Id == salesOrderId, cancellationToken))
        {
            return new Error(ErrorCodes.NotFound, "Müşteri siparişi bulunamadı.", ErrorType.NotFound);
        }

        var orders = await db.ProductionOrders.AsNoTracking()
            .Where(p => p.SalesOrderId == salesOrderId)
            .OrderBy(p => p.Id)
            .Select(p => new
            {
                p.Id, p.OrderNo, p.Status, p.ProductId, p.Product.ProductCode,
                Items = p.Items.Select(i => new { i.Id, i.ColorId, i.SizeId, i.Quantity }).ToList(),
            })
            .ToListAsync(cancellationToken);
        if (orders.Count == 0)
        {
            return Array.Empty<ProductionOrderDifferenceDto>();
        }

        var lines = await db.SalesOrderLines.AsNoTracking()
            .Where(l => l.SalesOrderId == salesOrderId)
            .Select(l => new { l.ProductId, l.ColorId, l.SizeId, l.Quantity })
            .ToListAsync(cancellationToken);
        var colors = await db.Colors.AsNoTracking().IgnoreQueryFilters().ToDictionaryAsync(c => c.Id, c => c.Name, cancellationToken);
        var sizes = await db.Sizes.AsNoTracking().IgnoreQueryFilters()
            .ToDictionaryAsync(s => s.Id, s => (s.Name, s.SortOrder), cancellationToken);

        // Üretim kalemlerinin kayıtları: var olan kalemler kimliğiyle, silinmiş / eklenmiş kalemler siparişin kimliğiyle bulunur.
        var itemIds = orders.SelectMany(o => o.Items.Select(i => i.Id.ToString())).ToList();
        var tokens = orders.SelectMany(o => new[] { $"\"ProductionOrderId\":{o.Id},", $"\"ProductionOrderId\":{o.Id}}}" }).ToList();
        var logs = await db.AuditLogs.AsNoTracking()
            .Where(l => l.EntityName == ItemEntity)
            .Where(l => itemIds.Contains(l.EntityId)
                || tokens.Any(t => (l.NewValues != null && l.NewValues.Contains(t)) || (l.OldValues != null && l.OldValues.Contains(t))))
            .OrderBy(l => l.CreatedAt).ThenBy(l => l.Id)
            .ToListAsync(cancellationToken);

        // Kalem kimliği → (üretim siparişi, renk, beden); güncelleme kayıtları yalnızca değişen alanı taşır.
        var itemKeys = orders.SelectMany(o => o.Items.Select(i => (i.Id, (o.Id, i.ColorId, i.SizeId)))).ToDictionary(x => x.Id, x => x.Item2);
        var changes = new List<(int OrderId, ProductionItemChangeDto Change)>();
        foreach (var log in logs)
        {
            var values = Read(log.NewValues) ?? Read(log.OldValues) ?? [];
            var old = Read(log.OldValues);
            var current = Read(log.NewValues);
            int? itemId = int.TryParse(log.EntityId, out var parsed) ? parsed : null;
            (int OrderId, int ColorId, int SizeId)? key = itemId is { } id && itemKeys.TryGetValue(id, out var known)
                ? known
                : Int(values, "ProductionOrderId") is { } orderId && Int(values, "ColorId") is { } colorId && Int(values, "SizeId") is { } sizeId
                    ? (orderId, colorId, sizeId)
                    : null;
            if (key is not { } k)
            {
                continue;
            }

            changes.Add((k.OrderId, new ProductionItemChangeDto(
                log.CreatedAt, log.UserName, log.Action,
                colors.GetValueOrDefault(k.ColorId, "?"), sizes.TryGetValue(k.SizeId, out var size) ? size.Name : "?",
                Int(old, "Quantity"), Int(current, "Quantity"))));
        }

        return orders.Select(o =>
        {
            var keys = lines.Where(l => l.ProductId == o.ProductId).Select(l => (l.ColorId, l.SizeId))
                .Union(o.Items.Select(i => (i.ColorId, i.SizeId)));
            var items = keys
                .Select(k =>
                {
                    var ordered = lines.Where(l => l.ProductId == o.ProductId && l.ColorId == k.ColorId && l.SizeId == k.SizeId).Sum(l => l.Quantity);
                    var produced = o.Items.Where(i => i.ColorId == k.ColorId && i.SizeId == k.SizeId).Sum(i => i.Quantity);
                    var size = sizes.TryGetValue(k.SizeId, out var found) ? found : (Name: "?", SortOrder: 0);
                    return new ProductionDifferenceItemDto(
                        colors.GetValueOrDefault(k.ColorId, "?"), size.Name, size.SortOrder, ordered, produced, produced - ordered);
                })
                .OrderBy(i => i.ColorName).ThenBy(i => i.SizeSortOrder)
                .ToList();
            return new ProductionOrderDifferenceDto(
                o.Id, o.OrderNo, o.Status, o.ProductCode, items,
                changes.Where(c => c.OrderId == o.Id).Select(c => c.Change).ToList());
        }).ToList();
    }

    private static Dictionary<string, JsonElement>? Read(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static int? Int(Dictionary<string, JsonElement>? values, string name) =>
        values is not null && values.TryGetValue(name, out var value) && value.ValueKind == JsonValueKind.Number ? value.GetInt32() : null;
}
