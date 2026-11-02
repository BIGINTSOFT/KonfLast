using ERP.Core.Domain.Common;
using ERP.Orme.Domain.Enums;

namespace ERP.Orme.Domain.Entities;

/// <summary>Örme siparişi: hangi kumaştan kaç kilo örüleceği.</summary>
public class OrmeOrder : AuditableEntity
{
    public string OrderNo { get; set; } = null!;
    public DateOnly OrderDate { get; set; }

    /// <summary>Fason işte zorunlu; kendi malında boşsa stoğa üretimdir.</summary>
    public int? CustomerId { get; set; }
    public OrmeCustomer? Customer { get; set; }

    /// <summary>Müşterinin kendi sipariş numarası.</summary>
    public string? CustomerOrderNo { get; set; }

    /// <summary>Fason: iplik müşterinin. Kendi malı: iplik işletmenin.</summary>
    public OrderOwnership Ownership { get; set; } = OrderOwnership.Commission;

    public DateOnly? DueDate { get; set; }
    public string Currency { get; set; } = "TRY";
    public string? Notes { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Draft;

    public ICollection<OrmeOrderLine> Lines { get; } = [];
}

/// <summary>Sipariş satırı: bir kumaştan kilo.</summary>
public class OrmeOrderLine : AuditableEntity
{
    public int OrderId { get; set; }
    public OrmeOrder Order { get; set; } = null!;

    public int FabricId { get; set; }
    public OrmeFabric Fabric { get; set; } = null!;

    public decimal Kilograms { get; set; }

    /// <summary>Kilo başına fason ya da satış fiyatı; henüz belli değilse boş.</summary>
    public decimal? UnitPrice { get; set; }

    /// <summary>Müşterinin bu kumaşa verdiği kendi kodu; etiket, çeki listesi, irsaliye ve faturada basılır.</summary>
    public string? CustomerFabricCode { get; set; }

    public string? Notes { get; set; }
}

/// <summary>İplik girişi: kilo ve lot bazında takip edilir. Malın sahibi lotta tutulur.</summary>
public class OrmeYarnLot : AuditableEntity
{
    public string LotNo { get; set; } = null!;
    public DateOnly ReceiptDate { get; set; }

    /// <summary>Malın sahibi olan müşteri (fason); boşsa mal işletmenindir.</summary>
    public int? CustomerId { get; set; }
    public OrmeCustomer? Customer { get; set; }

    /// <summary>İpliğin alındığı tedarikçi (isteğe bağlı).</summary>
    public int? SupplierId { get; set; }
    public OrmeSupplier? Supplier { get; set; }

    /// <summary>Lot bir satın alma siparişinden teslim alındıysa o satır; ipliği, sahibi ve tedarikçisi siparişle aynı kalır.</summary>
    public int? PurchaseOrderLineId { get; set; }
    public OrmePurchaseOrderLine? PurchaseOrderLine { get; set; }

    public int YarnId { get; set; }
    public OrmeYarn Yarn { get; set; } = null!;

    public string? DespatchNo { get; set; }

    /// <summary>İplik üreticisinin lot numarası; farklı üretici lotları aynı kumaşta karışırsa renk farkı çıkabilir.</summary>
    public string? ProducerLotNo { get; set; }

    /// <summary>Bobin sayısı.</summary>
    public int PackageCount { get; set; }
    public decimal Kilograms { get; set; }
    public string? Notes { get; set; }

    public ICollection<OrmeYarnIssue> Issues { get; } = [];
    public ICollection<OrmeYarnTakeBack> TakeBacks { get; } = [];
    public ICollection<OrmeYarnReturn> Returns { get; } = [];
}
