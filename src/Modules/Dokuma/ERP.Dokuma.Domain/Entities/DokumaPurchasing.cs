using ERP.Core.Domain.Common;
using ERP.Dokuma.Domain.Enums;

namespace ERP.Dokuma.Domain.Entities;

/// <summary>
/// İplik ve malzeme satın alma siparişi. Teslim alınan miktar iplikte iplik lotu, malzemede malzeme lotu olarak girer;
/// lot sipariş satırına bağlanır.
/// </summary>
public class DokumaPurchaseOrder : AuditableEntity
{
    public string OrderNo { get; set; } = null!;

    public int SupplierId { get; set; }
    public DokumaSupplier Supplier { get; set; } = null!;

    public DateOnly OrderDate { get; set; }

    /// <summary>Genel termin; satırda ayrı termin girilebilir.</summary>
    public DateOnly? DueDate { get; set; }

    public string Currency { get; set; } = "TRY";
    public string? Notes { get; set; }
    public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Draft;

    public DateTime? ApprovedAt { get; set; }
    public string? ApprovedBy { get; set; }

    public ICollection<DokumaPurchaseOrderLine> Lines { get; } = [];

    /// <summary>Teslim durumunu satırlardan yeniden hesaplar; taslak, kapatılan ya da iptal edilen sipariş değişmez.</summary>
    public void RefreshReceiptStatus()
    {
        if (Status is PurchaseOrderStatus.Draft or PurchaseOrderStatus.Closed or PurchaseOrderStatus.Cancelled)
        {
            return;
        }

        Status = Lines.All(l => l.ReceivedQuantity >= l.Quantity) ? PurchaseOrderStatus.Received
            : Lines.Any(l => l.ReceivedQuantity > 0) ? PurchaseOrderStatus.PartiallyReceived
            : PurchaseOrderStatus.Approved;
    }
}

/// <summary>Sipariş satırı: ya iplik (kilogram) ya da malzeme (malzemenin biriminde).</summary>
public class DokumaPurchaseOrderLine : AuditableEntity
{
    public int OrderId { get; set; }
    public DokumaPurchaseOrder Order { get; set; } = null!;

    /// <summary>Satırın sırası (1'den başlar).</summary>
    public int Sequence { get; set; }

    public int? YarnId { get; set; }
    public DokumaYarn? Yarn { get; set; }

    public int? MaterialId { get; set; }
    public DokumaMaterial? Material { get; set; }

    public decimal Quantity { get; set; }

    /// <summary>İplikte her zaman kilogram; malzemede malzeme kartının birimi.</summary>
    public MaterialUnit Unit { get; set; } = MaterialUnit.Kilogram;

    public decimal? UnitPrice { get; set; }

    /// <summary>Satırın termini; boşsa siparişin termini.</summary>
    public DateOnly? DueDate { get; set; }

    /// <summary>Teslim alınan toplam; satıra bağlı lotlardan hesaplanır.</summary>
    public decimal ReceivedQuantity { get; set; }

    public string? Notes { get; set; }
}
