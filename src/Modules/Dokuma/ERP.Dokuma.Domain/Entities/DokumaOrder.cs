using ERP.Core.Domain.Common;
using ERP.Dokuma.Domain.Enums;

namespace ERP.Dokuma.Domain.Entities;

/// <summary>Dokuma siparişi: hangi artikelden kaç metre dokunacağı.</summary>
public class DokumaOrder : AuditableEntity
{
    public string OrderNo { get; set; } = null!;
    public DateOnly OrderDate { get; set; }

    /// <summary>Fason işte zorunlu; kendi malında boşsa stoğa üretimdir.</summary>
    public int? CustomerId { get; set; }
    public DokumaCustomer? Customer { get; set; }

    /// <summary>Müşterinin kendi sipariş numarası.</summary>
    public string? CustomerOrderNo { get; set; }

    /// <summary>Fason: iplik müşterinin. Kendi malı: iplik işletmenin.</summary>
    public OrderOwnership Ownership { get; set; } = OrderOwnership.Commission;

    public DateOnly? DueDate { get; set; }
    public string Currency { get; set; } = "TRY";
    public string? Notes { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Draft;

    public ICollection<DokumaOrderLine> Lines { get; } = [];
}

/// <summary>Sipariş satırı: bir artikelden metre.</summary>
public class DokumaOrderLine : AuditableEntity
{
    public int OrderId { get; set; }
    public DokumaOrder Order { get; set; } = null!;

    public int ArticleId { get; set; }
    public DokumaArticle Article { get; set; } = null!;

    public decimal Meters { get; set; }

    /// <summary>Müşterinin bu artikel için kullandığı kendi kodu; etikete ve sevk belgelerine basılır.</summary>
    public string? CustomerArticleCode { get; set; }

    /// <summary>Metre başına fason ya da satış fiyatı; henüz belli değilse boş.</summary>
    public decimal? UnitPrice { get; set; }

    public string? Notes { get; set; }
}

/// <summary>İplik girişi: kilo ve lot bazında takip edilir. Malın sahibi lotta tutulur.</summary>
public class DokumaYarnLot : AuditableEntity
{
    public string LotNo { get; set; } = null!;
    public DateOnly ReceiptDate { get; set; }

    /// <summary>Malın sahibi olan müşteri (fason); boşsa mal işletmenindir.</summary>
    public int? CustomerId { get; set; }
    public DokumaCustomer? Customer { get; set; }

    /// <summary>İpliğin alındığı tedarikçi (isteğe bağlı).</summary>
    public int? SupplierId { get; set; }
    public DokumaSupplier? Supplier { get; set; }

    public int YarnId { get; set; }
    public DokumaYarn Yarn { get; set; } = null!;

    public string? DespatchNo { get; set; }

    /// <summary>İplik üreticisinin lot numarası; farklı üretici lotları aynı çözgüde karışırsa renk farkı çıkabilir.</summary>
    public string? ProducerLotNo { get; set; }

    /// <summary>Bobin sayısı.</summary>
    public int PackageCount { get; set; }
    public decimal Kilograms { get; set; }
    public string? Notes { get; set; }

    /// <summary>Satın alma siparişinden teslim alındıysa sipariş satırı; lot işletme malıdır.</summary>
    public int? PurchaseOrderLineId { get; set; }
    public DokumaPurchaseOrderLine? PurchaseOrderLine { get; set; }

    /// <summary>Çözgüye verilen miktarlar; kalan = kilo − toplamı.</summary>
    public ICollection<DokumaWarpIssue> Issues { get; } = [];

    /// <summary>Tezgâhta atkıya verilen miktarlar; kalan = kilo − çözgü ve atkı çıkışları.</summary>
    public ICollection<DokumaWeftIssue> WeftIssues { get; } = [];

    /// <summary>Müşteriye geri verilen miktarlar; kalandan düşer.</summary>
    public ICollection<DokumaYarnReturn> Returns { get; } = [];

    /// <summary>Üretimden geri alınan miktarlar; kalana eklenir.</summary>
    public ICollection<DokumaYarnTakeBack> TakeBacks { get; } = [];
}
