using ERP.Core.Domain.Common;
using ERP.Core.Domain.Enums;
using ERP.Dokuma.Domain.Enums;

namespace ERP.Dokuma.Domain.Entities;

/// <summary>
/// Ön maliyet parametreleri (tek kayıt). Tutarlar <see cref="Currency"/> para birimindedir; teklifin para birimi farklıysa
/// teklif tarihindeki kurla çevrilir. Tezgâha özel saat maliyeti ve verim <see cref="DokumaLoomCostRate"/> ile verilir.
/// </summary>
public class DokumaCostSettings : AuditableEntity
{
    /// <summary>Maliyet tutarlarının para birimi.</summary>
    public string Currency { get; set; } = "TRY";

    /// <summary>Tezgâhın bir çalışma saatinin enerji ve amortisman maliyeti (varsayılan; tezgâha özel değer önce gelir).</summary>
    public decimal LoomHourlyCost { get; set; }

    /// <summary>Tezgâh saati başına işçilik (dokumacı, usta, yardımcı payı).</summary>
    public decimal LaborHourlyCost { get; set; }

    /// <summary>Bir metre çözgünün çözgü (çözgü makinesi) maliyeti.</summary>
    public decimal WarpingCostPerMeter { get; set; }

    /// <summary>
    /// Bir metre çözgünün haşıl maliyeti. <see cref="UseSizingRecipe"/> açıksa ve artikelin varsayılan reçetesinden kimyasal
    /// maliyeti hesaplanabiliyorsa bunun yerine reçete kullanılır.
    /// </summary>
    public decimal SizingCostPerMeter { get; set; }

    /// <summary>Haşıl maliyeti artikelin varsayılan reçetesindeki kimyasallardan (son lot fiyatıyla) hesaplanır.</summary>
    public bool UseSizingRecipe { get; set; }

    /// <summary>Genel gider payı (%): doğrudan maliyetin üzerine eklenir.</summary>
    public decimal OverheadPercent { get; set; }

    /// <summary>Varsayılan kumaş firesi (%): ikinci kalite, kesim başı ve kayıp payı.</summary>
    public decimal DefaultWastePercent { get; set; }

    /// <summary>Varsayılan kâr marjı (%).</summary>
    public decimal DefaultMarginPercent { get; set; }

    /// <summary>Tezgâha özel verim yoksa kullanılan verim (%); ön maliyette ve kapasite planında.</summary>
    public decimal DefaultEfficiencyPercent { get; set; } = 85;

    /// <summary>Yeni teklifin geçerlilik süresi (gün).</summary>
    public int QuotationValidityDays { get; set; } = 15;
}

/// <summary>Tezgâha özel saat maliyeti ve verim; boş alan genel parametreden alınır.</summary>
public class DokumaLoomCostRate : AuditableEntity
{
    public int LoomId { get; set; }
    public DokumaLoom Loom { get; set; } = null!;

    /// <summary>Tezgâhın saat maliyeti (enerji + amortisman); boşsa genel değer.</summary>
    public decimal? HourlyCost { get; set; }

    /// <summary>Tezgâhın verimi (%); boşsa genel değer.</summary>
    public decimal? EfficiencyPercent { get; set; }
}

/// <summary>
/// Dokuma teklifi: müşteriye (ya da henüz kartı açılmamış adaya) artikel başına metre fiyatı. Satırlarda ön maliyet dökümü
/// saklanır; kabul edilen tekliften taslak sipariş açılır.
/// </summary>
public class DokumaQuotation : AuditableEntity
{
    public string QuotationNo { get; set; } = null!;
    public DateOnly QuotationDate { get; set; }
    public DateOnly ValidUntil { get; set; }

    /// <summary>Kayıtlı müşteri; boşsa <see cref="ProspectName"/> yazılır, kabulde müşteri seçilir.</summary>
    public int? CustomerId { get; set; }
    public DokumaCustomer? Customer { get; set; }

    /// <summary>Müşteri kartı olmayan aday firmanın adı.</summary>
    public string? ProspectName { get; set; }

    /// <summary>Teklifin gönderildiği kişi.</summary>
    public string? ContactName { get; set; }

    /// <summary>Fason: iplik müşterinin, iplik maliyeti fiyata girmez. Kendi malı: iplik fiyata dahildir.</summary>
    public OrderOwnership Ownership { get; set; } = OrderOwnership.OwnGoods;

    public string Currency { get; set; } = "TRY";

    /// <summary>Basılan teklifin dili; müşteri seçiliyse müşteri kartından gelir.</summary>
    public DocumentLanguage DocumentLanguage { get; set; } = DocumentLanguage.Turkish;

    /// <summary>Ödeme vadesi (gün).</summary>
    public int? PaymentTermDays { get; set; }

    /// <summary>Teslim süresi açıklaması (örn. "Siparişten itibaren 4 hafta").</summary>
    public string? DeliveryTerms { get; set; }

    public string? Notes { get; set; }

    public QuotationStatus Status { get; set; } = QuotationStatus.Draft;

    public DateTime? SentAt { get; set; }
    public DateTime? DecidedAt { get; set; }
    public string? DecidedBy { get; set; }

    /// <summary>Ret nedeni.</summary>
    public string? RejectionReason { get; set; }

    /// <summary>Kabulde açılan sipariş.</summary>
    public int? OrderId { get; set; }
    public DokumaOrder? Order { get; set; }

    public ICollection<DokumaQuotationLine> Lines { get; } = [];
}

/// <summary>
/// Teklif satırı: artikel, metre ve ön maliyet dökümü. Hesap girdileri (hız, verim, sıklık, maliyetler, oranlar) satırda
/// saklanır; parametre sonradan değişse de teklifin dökümü değişmez. Tutarlar teklifin para birimindedir.
/// </summary>
public class DokumaQuotationLine : AuditableEntity
{
    public int QuotationId { get; set; }
    public DokumaQuotation Quotation { get; set; } = null!;

    /// <summary>Satırın sırası (1'den başlar).</summary>
    public int Sequence { get; set; }

    public int ArticleId { get; set; }
    public DokumaArticle Article { get; set; } = null!;

    public decimal Meters { get; set; }

    /// <summary>Müşterinin bu artikel için kullandığı kendi kodu.</summary>
    public string? CustomerArticleCode { get; set; }

    /// <summary>Hız, verim ve saat maliyetinin alındığı tezgâh; boşsa satırdaki değerler elle girilmiştir.</summary>
    public int? LoomId { get; set; }
    public DokumaLoom? Loom { get; set; }

    /// <summary>Tezgâh hızı (devir/dk).</summary>
    public decimal SpeedRpm { get; set; }

    /// <summary>Verim (%).</summary>
    public decimal EfficiencyPercent { get; set; }

    /// <summary>Atkı sıklığı (atkı/cm); artikelden gelir, değiştirilebilir.</summary>
    public decimal PicksPerCm { get; set; }

    /// <summary>Bir metre kumaş için çözgü ipliği (kg, artikel firesi dahil).</summary>
    public decimal WarpKilogramsPerMeter { get; set; }

    /// <summary>Bir metre kumaş için atkı ipliği (kg, artikel firesi dahil).</summary>
    public decimal WeftKilogramsPerMeter { get; set; }

    /// <summary>Metre başına iplik maliyeti; fasonda sıfır.</summary>
    public decimal YarnCost { get; set; }

    /// <summary>Haşıl maliyeti (bir metre çözgü için).</summary>
    public decimal SizingCostPerWarpMeter { get; set; }

    /// <summary>Çözgü maliyeti (bir metre çözgü için).</summary>
    public decimal WarpingCostPerWarpMeter { get; set; }

    /// <summary>Metre kumaş başına haşıl maliyeti: çözgü metresi × (1 + çözgü çekmesi).</summary>
    public decimal SizingCost { get; set; }

    /// <summary>Metre kumaş başına çözgü maliyeti: çözgü metresi × (1 + çözgü çekmesi).</summary>
    public decimal WarpingCost { get; set; }

    public decimal LoomHourlyCost { get; set; }
    public decimal LaborHourlyCost { get; set; }

    /// <summary>Bir metre kumaşın dokuma süresi (saat).</summary>
    public decimal WeavingHoursPerMeter { get; set; }

    /// <summary>Metre başına dokuma maliyeti: süre × (tezgâh + işçilik saat maliyeti).</summary>
    public decimal WeavingCost { get; set; }

    public decimal WastePercent { get; set; }
    public decimal OverheadPercent { get; set; }
    public decimal MarginPercent { get; set; }

    /// <summary>Fire ve genel gider eklenmiş metre maliyeti (kâr hariç).</summary>
    public decimal CostPerMeter { get; set; }

    /// <summary>Maliyet × (1 + kâr marjı).</summary>
    public decimal CalculatedPrice { get; set; }

    /// <summary>Teklif edilen metre fiyatı; elle değiştirilebilir.</summary>
    public decimal UnitPrice { get; set; }

    public string? Notes { get; set; }

    public ICollection<DokumaQuotationLineYarn> Yarns { get; } = [];
}

/// <summary>Teklif satırındaki ipliğin metre başına kilosu ve kilo fiyatı (teklifin para biriminde).</summary>
public class DokumaQuotationLineYarn : AuditableEntity
{
    public int LineId { get; set; }
    public DokumaQuotationLine Line { get; set; } = null!;

    public int YarnId { get; set; }
    public DokumaYarn Yarn { get; set; } = null!;

    public bool UsedInWarp { get; set; }
    public bool UsedInWeft { get; set; }

    /// <summary>Bir metre kumaş için bu iplikten gereken (kg, artikel firesi dahil).</summary>
    public decimal KilogramsPerMeter { get; set; }

    /// <summary>Kilo fiyatı (teklifin para biriminde); son alış fiyatından gelir, değiştirilebilir.</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>Fiyatın nereden geldiği (son alış siparişi ya da elle).</summary>
    public string? PriceSource { get; set; }
}
