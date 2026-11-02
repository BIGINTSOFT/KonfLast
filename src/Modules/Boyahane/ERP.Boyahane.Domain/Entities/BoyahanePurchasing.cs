using ERP.Boyahane.Domain.Enums;
using ERP.Core.Domain.Common;

namespace ERP.Boyahane.Domain.Entities;

/// <summary>Boyahanenin tedarikçisi; kayıtlar Konfeksiyon'dan ayrıdır.</summary>
public class BoyahaneSupplier : SoftDeletableEntity, IActivatable
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? TaxNumber { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string Currency { get; set; } = "TRY";

    /// <summary>Ortalama teslim süresi (gün); tahmini tarih için.</summary>
    public int? LeadTimeDays { get; set; }

    public bool IsActive { get; set; } = true;
}

/// <summary>Boya ve kimyasal satın alma siparişi. Teslim alınan miktar lot olarak depoya girer.</summary>
public class BoyahanePurchaseOrder : AuditableEntity
{
    public string OrderNo { get; set; } = null!;

    public int SupplierId { get; set; }
    public BoyahaneSupplier Supplier { get; set; } = null!;

    public DateOnly OrderDate { get; set; }
    public DateOnly? ExpectedDate { get; set; }
    public string Currency { get; set; } = "TRY";
    public string? Notes { get; set; }
    public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Draft;

    public ICollection<BoyahanePurchaseOrderLine> Lines { get; } = [];

    /// <summary>Teslim durumunu satırlardan yeniden hesaplar; kapatılan ya da iptal edilen sipariş değişmez.</summary>
    public void RefreshReceiptStatus()
    {
        if (Status is PurchaseOrderStatus.Draft or PurchaseOrderStatus.Closed or PurchaseOrderStatus.Cancelled)
        {
            return;
        }

        Status = Lines.All(l => l.ReceivedKilograms >= l.Kilograms) ? PurchaseOrderStatus.Received
            : Lines.Any(l => l.ReceivedKilograms > 0) ? PurchaseOrderStatus.PartiallyReceived
            : PurchaseOrderStatus.Confirmed;
    }
}

public class BoyahanePurchaseOrderLine : AuditableEntity
{
    public int OrderId { get; set; }
    public BoyahanePurchaseOrder Order { get; set; } = null!;

    public int DyeChemicalId { get; set; }
    public BoyahaneDyeChemical DyeChemical { get; set; } = null!;

    public decimal Kilograms { get; set; }

    /// <summary>Kilogram fiyatı; teslimde lotun birim maliyeti olur.</summary>
    public decimal? UnitPrice { get; set; }

    /// <summary>Teslim alınan toplam; lotlardan hesaplanır.</summary>
    public decimal ReceivedKilograms { get; set; }
}
