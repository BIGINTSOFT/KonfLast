using ERP.Boyahane.Domain.Enums;
using ERP.Core.Domain.Common;
using ERP.Core.Domain.Enums;

namespace ERP.Boyahane.Domain.Entities;

/// <summary>Boyahane siparişi. Ham kumaş siparişten önce gelebildiği için toplar satırlara sonradan bağlanır.</summary>
public class BoyahaneOrder : AuditableEntity
{
    public string OrderNo { get; set; } = null!;
    public DateOnly OrderDate { get; set; }

    /// <summary>Fason işte zorunlu; kendi malında boşsa stoğa üretimdir.</summary>
    public int? CustomerId { get; set; }
    public BoyahaneCustomer? Customer { get; set; }

    /// <summary>Müşterinin kendi sipariş numarası.</summary>
    public string? CustomerOrderNo { get; set; }

    /// <summary>Fason: kumaş müşterinin. Kendi malı: kumaş işletmenin, boyanıp müşteriye satılır.</summary>
    public OrderOwnership Ownership { get; set; } = OrderOwnership.Commission;

    public DateOnly? DueDate { get; set; }
    public string Currency { get; set; } = "TRY";
    public string? Notes { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Draft;

    /// <summary>Kapatma zamanı, kapatan kullanıcı ve isteğe bağlı notu; kapatılmamışsa boş.</summary>
    public DateTime? ClosedAt { get; set; }
    public string? ClosedBy { get; set; }
    public string? CloseNote { get; set; }

    public ICollection<BoyahaneOrderLine> Lines { get; } = [];
}

/// <summary>Sipariş satırı: bir kumaş ya da iplik, bir işlem türü ve bir renk / desen.</summary>
public class BoyahaneOrderLine : AuditableEntity
{
    public int OrderId { get; set; }
    public BoyahaneOrder Order { get; set; } = null!;

    public OrderProcessType ProcessType { get; set; } = OrderProcessType.FabricDyeing;

    /// <summary>İplik boyama dışındaki işlerde kumaş kartı.</summary>
    public int? FabricId { get; set; }
    public BoyahaneFabric? Fabric { get; set; }

    /// <summary>İplik boyamada iplik kartı.</summary>
    public int? YarnId { get; set; }
    public BoyahaneYarn? Yarn { get; set; }

    /// <summary>Renk ya da baskı deseni adı; terbiye / aprede boş olabilir.</summary>
    public string? ColorName { get; set; }

    /// <summary>Müşterinin renk ya da desen kodu (Pantone vb.).</summary>
    public string? ColorCode { get; set; }

    /// <summary>Arşivdeki renk kartı; seçilirse renk adı ve kodu karttan gelir. Boyama satırında kullanılır.</summary>
    public int? ColorCardId { get; set; }
    public BoyahaneColorCard? ColorCard { get; set; }

    /// <summary>Baskı satırında desen kartı; seçilirse desen adı ve kodu karttan gelir.</summary>
    public int? PatternId { get; set; }
    public BoyahanePattern? Pattern { get; set; }

    /// <summary>Baskı satırında desenin renk yolu; baskı reçetesi buradan alınır.</summary>
    public int? ColorwayId { get; set; }
    public BoyahanePatternColorway? Colorway { get; set; }

    public decimal Quantity { get; set; }

    /// <summary>Metre ya da kilogram; iplik boyamada kilogram.</summary>
    public StockUnit Unit { get; set; } = StockUnit.Meter;

    /// <summary>Birim fason ya da satış fiyatı; henüz belli değilse boş.</summary>
    public decimal? UnitPrice { get; set; }

    /// <summary>Bu satırın fatura ölçüsü; boşsa müşteri ayarı geçerlidir.</summary>
    public BillingUnit? BillingUnit { get; set; }

    public string? Notes { get; set; }

    public ICollection<BoyahaneFabricRoll> Rolls { get; } = [];

    public ICollection<BoyahaneYarnLot> YarnLots { get; } = [];
}
