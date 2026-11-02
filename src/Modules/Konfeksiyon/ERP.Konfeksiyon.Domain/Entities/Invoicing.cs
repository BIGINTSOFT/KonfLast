using ERP.Core.Domain.Common;
using ERP.Konfeksiyon.Domain.Enums;

namespace ERP.Konfeksiyon.Domain.Entities;

/// <summary>
/// Satış faturası: aynı müşterinin sevk edilmiş sevkiyatlarından, mamul kartı ve sipariş kalemi bazında adet. Taslakken
/// fiyat, iskonto ve KDV değiştirilebilir; onaylı fatura cariye borç yazar. e-Belge entegrasyonuna kadar çıktısı resmî
/// fatura değildir.
/// </summary>
public class Invoice : AuditableEntity
{
    public string InvoiceNo { get; set; } = null!;

    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public DateOnly InvoiceDate { get; set; }
    public DateOnly? DueDate { get; set; }
    public string Currency { get; set; } = "TRY";

    /// <summary>KDV'nin tevkif edilen kısmı (%), örneğin 5/10 için 50; yoksa boş.</summary>
    public decimal? WithholdingPercent { get; set; }

    public string? Notes { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;

    /// <summary>Sisteme geçişteki açılış borcu; doluysa kayıt fatura değildir, satırı olmaz, ödenecek tutar budur.</summary>
    public decimal? OpeningAmount { get; set; }

    public ICollection<InvoiceLine> Lines { get; } = [];
    public ICollection<Shipment> Shipments { get; } = [];
}

/// <summary>Faturada bir mamul kartının (ürün + renk + beden) bir sipariş kalemine düşen sevk adedi.</summary>
public class InvoiceLine : AuditableEntity
{
    public int InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;

    public int StockItemId { get; set; }
    public StockItem StockItem { get; set; } = null!;

    /// <summary>Sevkiyat müşteri siparişine bağlıysa adedin karşıladığı sipariş kalemi; fiyat oradan gelir.</summary>
    public int? SalesOrderLineId { get; set; }
    public SalesOrderLine? SalesOrderLine { get; set; }

    public string Description { get; set; } = null!;

    /// <summary>Adet.</summary>
    public decimal Quantity { get; set; }

    /// <summary>Birim fiyat; sipariş kaleminden ya da fiyat listesinden gelir, taslakta değiştirilebilir.</summary>
    public decimal? UnitPrice { get; set; }

    /// <summary>İskonto (%); siparişsiz sevkiyatta müşteri iskontosundan gelir, taslakta değiştirilebilir.</summary>
    public decimal? DiscountPercent { get; set; }

    /// <summary>KDV oranı (%); ürün kartından gelir, taslakta değiştirilebilir.</summary>
    public decimal VatRate { get; set; }
}

/// <summary>Müşteriden tahsilat. Düzenlenmez; yanlış kayıt gerekçeyle iptal edilip yeniden girilir.</summary>
public class CustomerPayment : AuditableEntity
{
    public string PaymentNo { get; set; } = null!;

    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public DateOnly PaymentDate { get; set; }
    public PaymentMethod Method { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "TRY";

    /// <summary>Çek no ya da dekont no.</summary>
    public string? ReferenceNo { get; set; }

    /// <summary>Çekin vadesi.</summary>
    public DateOnly? DueDate { get; set; }

    public string? Notes { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Active;
    public string? CancelReason { get; set; }

    public ICollection<PaymentAllocation> Allocations { get; } = [];
}

/// <summary>Tahsilatın bir faturaya dağıtılan kısmı. Dağıtılmayan kısım müşterinin avansıdır.</summary>
public class PaymentAllocation : AuditableEntity
{
    public int PaymentId { get; set; }
    public CustomerPayment Payment { get; set; } = null!;

    public int InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;

    public decimal Amount { get; set; }
}
