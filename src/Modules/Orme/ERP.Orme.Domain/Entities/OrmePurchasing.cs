using ERP.Core.Domain.Common;
using ERP.Orme.Domain.Enums;

namespace ERP.Orme.Domain.Entities;

/// <summary>
/// İplik satın alma siparişi. Yalnızca işletmenin kendi malı olan iplik satın alınır; müşterinin (fason) ipliği satın alınmaz.
/// Teslim alınan miktar iplik girişi (lot) olarak girer ve lot sipariş satırına bağlanır.
/// </summary>
public class OrmePurchaseOrder : AuditableEntity
{
    public string OrderNo { get; set; } = null!;

    public int SupplierId { get; set; }
    public OrmeSupplier Supplier { get; set; } = null!;

    public DateOnly OrderDate { get; set; }
    public string Currency { get; set; } = "TRY";
    public string? Notes { get; set; }
    public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Draft;

    public ICollection<OrmePurchaseOrderLine> Lines { get; } = [];

    /// <summary>
    /// Teslim durumunu satırlara gelen lotlardan yeniden hesaplar. Taslak, kapatılmış ve iptal edilmiş sipariş değişmez.
    /// </summary>
    /// <param name="receivedOf">Satır Id'sine göre teslim alınan kilo.</param>
    public void RefreshReceiptStatus(Func<int, decimal> receivedOf)
    {
        if (Status is PurchaseOrderStatus.Draft or PurchaseOrderStatus.Closed or PurchaseOrderStatus.Cancelled)
        {
            return;
        }

        Status = Lines.All(l => receivedOf(l.Id) >= l.Kilograms) ? PurchaseOrderStatus.Received
            : Lines.Any(l => receivedOf(l.Id) > 0) ? PurchaseOrderStatus.PartiallyReceived
            : PurchaseOrderStatus.Approved;
    }
}

/// <summary>Satın alma satırı: iplik, kilo, birim fiyat ve termin. Teslim alınan miktar bağlı lotların toplamıdır.</summary>
public class OrmePurchaseOrderLine : AuditableEntity
{
    public int OrderId { get; set; }
    public OrmePurchaseOrder Order { get; set; } = null!;

    public int YarnId { get; set; }
    public OrmeYarn Yarn { get; set; } = null!;

    public decimal Kilograms { get; set; }

    /// <summary>Kilogram fiyatı (siparişin para biriminde); henüz belli değilse boş.</summary>
    public decimal? UnitPrice { get; set; }

    /// <summary>Tedarikçiden istenen teslim tarihi.</summary>
    public DateOnly? DueDate { get; set; }

    public string? Notes { get; set; }

    /// <summary>Bu satırdan teslim alınan iplik girişleri.</summary>
    public ICollection<OrmeYarnLot> Lots { get; } = [];
}
