using ERP.Core.Application.Features.Integrations;
using ERP.Konfeksiyon.Domain.Enums;

namespace ERP.Konfeksiyon.Application.Features.Integrations;

/// <summary>
/// Dış sistemlere duyurulan olay türleri. Bir türün verisi (aşağıdaki kayıtlar) dış sistemlerle sözleşmedir:
/// alan eklenebilir, var olan alanın adı ya da anlamı değiştirilmez.
/// </summary>
public static class IntegrationEventTypes
{
    /// <summary>Sevkiyat sevk edildi, stok düştü (e-İrsaliye / e-Fatura, kargo, muhasebe için).</summary>
    public const string ShipmentShipped = "shipment.shipped";

    /// <summary>Satın alma siparişine mal kabul yapıldı, stok arttı (muhasebe, SAP için).</summary>
    public const string GoodsReceiptPosted = "goods_receipt.posted";

    /// <summary>Üretim siparişi tamamlandı (SAP / muhasebe üretim teyidi için).</summary>
    public const string ProductionOrderCompleted = "production_order.completed";

    public static readonly IReadOnlyList<IntegrationEventTypeDto> All =
    [
        new(ShipmentShipped, "Sevkiyat sevk edildi"),
        new(GoodsReceiptPosted, "Mal kabul yapıldı"),
        new(ProductionOrderCompleted, "İş emri tamamlandı"),
    ];
}

public sealed class KonfeksiyonIntegrationEventCatalog : IIntegrationEventCatalog
{
    public IReadOnlyList<IntegrationEventTypeDto> EventTypes => IntegrationEventTypes.All;
}

public sealed record ShipmentShippedPayload(
    string ShipmentNo,
    DateOnly ShipDate,
    DateTime ShippedAt,
    string ShippedBy,
    string? DocumentNo,
    string? Carrier,
    string? Notes,
    PartyPayload Customer,
    string WarehouseCode,
    string? ProductionOrderNo,
    IReadOnlyList<StockLinePayload> Items);

public sealed record GoodsReceiptPostedPayload(
    string PurchaseOrderNo,
    PurchaseOrderStatus PurchaseOrderStatus,
    DateTime ReceivedAt,
    string ReceivedBy,
    string? DocumentNo,
    string? Note,
    PartyPayload Supplier,
    string WarehouseCode,
    string Currency,
    IReadOnlyList<StockLinePayload> Lines);

public sealed record ProductionOrderLinePayload(string ColorCode, string SizeCode, int Quantity);

public sealed record ProductionOrderCompletedPayload(
    string OrderNo,
    string? CustomerOrderNo,
    PartyPayload Customer,
    string ProductCode,
    string ProductName,
    DateOnly OrderDate,
    DateOnly DueDate,
    int OrderedQuantity,
    decimal ReceivedIntoStock,
    IReadOnlyList<ProductionOrderLinePayload> Items);
