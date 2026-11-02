using ERP.Core.Domain.Common;
using ERP.Konfeksiyon.Domain.Enums;

namespace ERP.Konfeksiyon.Domain.Entities;

/// <summary>
/// Sevkiyat: bir depodan bir müşteriye gönderilen mamuller. Taslakken düzenlenir; sevk edildiğinde stok
/// düşer ve kayıt kesinleşir (değiştirilemez, geri alınamaz).
/// </summary>
public class Shipment : SoftDeletableEntity
{
    /// <summary>Sistem tarafından üretilir, örn. SV-2026-00031; değiştirilemez ve yeniden kullanılmaz.</summary>
    public string ShipmentNo { get; set; } = null!;

    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    /// <summary>Mamullerin çıkacağı depo.</summary>
    public int WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;

    /// <summary>Sevkiyat bir üretim siparişine aitse o sipariş; sevk edilen adet siparişe işlenir.</summary>
    public int? ProductionOrderId { get; set; }
    public ProductionOrder? ProductionOrder { get; set; }

    /// <summary>
    /// Sevkiyatın karşıladığı müşteri siparişi. Bir sevkiyat siparişin birden fazla ürününü taşıyabilir; üretim
    /// siparişine bağlı sevkiyatta o siparişin müşteri siparişidir. Sevk edilen adet müşteri siparişine işlenir.
    /// </summary>
    public int? SalesOrderId { get; set; }
    public SalesOrder? SalesOrder { get; set; }

    /// <summary>Planlanan / gerçekleşen sevk günü.</summary>
    public DateOnly ShipDate { get; set; }

    /// <summary>İrsaliye numarası.</summary>
    public string? DocumentNo { get; set; }

    /// <summary>Taşıyıcı firma, araç plakası vb.</summary>
    public string? Carrier { get; set; }
    public string? Notes { get; set; }

    /// <summary>Faturalandıysa fatura; bir sevkiyat tek faturaya girer.</summary>
    public int? InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }

    public ShipmentStatus Status { get; private set; } = ShipmentStatus.Draft;

    /// <summary>Stoğun düştüğü an (sunucu saati).</summary>
    public DateTime? ShippedAt { get; private set; }
    public string? ShippedBy { get; private set; }

    /// <summary>Aynı sevkiyatın iki kullanıcı tarafından eşzamanlı değiştirilmesini engeller.</summary>
    public byte[] RowVersion { get; set; } = null!;

    public ICollection<ShipmentItem> Items { get; } = new List<ShipmentItem>();

    /// <summary>
    /// Koliler (çeki listesi). Boş olabilir; koli varsa sevk anında kolilerdeki adetler kalemlerle birebir tutmalıdır.
    /// </summary>
    public ICollection<ShipmentCarton> Cartons { get; } = new List<ShipmentCarton>();

    /// <summary>Başlık ve kalemler yalnızca taslakken değiştirilebilir.</summary>
    public bool IsEditable => Status == ShipmentStatus.Draft;

    /// <summary>Kalemi olan taslak sevk edilebilir.</summary>
    public bool CanShip => Status == ShipmentStatus.Draft && Items.Count > 0;

    public bool CanCancel => Status == ShipmentStatus.Draft;

    public void Ship(DateTime utcNow, string shippedBy)
    {
        if (!CanShip)
        {
            throw new InvalidOperationException(
                Status == ShipmentStatus.Draft ? "Kalemi olmayan sevkiyat sevk edilemez." : $"{Status} durumundaki sevkiyat sevk edilemez.");
        }

        Status = ShipmentStatus.Shipped;
        ShippedAt = utcNow;
        ShippedBy = shippedBy;
    }

    public void Cancel()
    {
        if (!CanCancel)
        {
            throw new InvalidOperationException($"{Status} durumundaki sevkiyat iptal edilemez.");
        }

        Status = ShipmentStatus.Cancelled;
    }
}

public class ShipmentItem : BaseEntity
{
    public int ShipmentId { get; set; }
    public Shipment Shipment { get; set; } = null!;

    /// <summary>Sevk edilen mamul kartı.</summary>
    public int StockItemId { get; set; }
    public StockItem StockItem { get; set; } = null!;

    public decimal Quantity { get; set; }
}
