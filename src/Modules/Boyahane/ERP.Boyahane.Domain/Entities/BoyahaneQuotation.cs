using ERP.Boyahane.Domain.Enums;
using ERP.Core.Domain.Common;
using ERP.Core.Domain.Enums;

namespace ERP.Boyahane.Domain.Entities;

/// <summary>
/// Fiyat teklifi. Müşteriye ya da henüz kartı açılmamış adaya verilir; satırların maliyeti reçete, makine saati ve işçilikten
/// hesaplanır, fiyat maliyet üzerine kâr payıyla önerilir. Kabul edilen teklif taslak siparişe dönüşür.
/// </summary>
public class BoyahaneQuotation : AuditableEntity
{
    public string QuotationNo { get; set; } = null!;
    public DateOnly QuotationDate { get; set; }

    /// <summary>Teklifin son geçerlilik günü.</summary>
    public DateOnly ValidUntil { get; set; }

    /// <summary>Kayıtlı müşteri; boşsa <see cref="ProspectName"/> yazılır.</summary>
    public int? CustomerId { get; set; }
    public BoyahaneCustomer? Customer { get; set; }

    /// <summary>Kartı açılmamış aday müşterinin adı; müşteri seçiliyse boş.</summary>
    public string? ProspectName { get; set; }

    /// <summary>Fason: kumaş müşterinin, fiyat işçiliktir. Kendi malı: kumaş bedeli fiyata ayrıca eklenmelidir.</summary>
    public OrderOwnership Ownership { get; set; } = OrderOwnership.Commission;

    /// <summary>Müşterinin istediği termin; sipariş açılırken siparişe geçer.</summary>
    public DateOnly? DueDate { get; set; }

    public string Currency { get; set; } = "TRY";

    /// <summary>Basılan teklifin dili; müşteri seçiliyse müşterinin belge dilinden gelir.</summary>
    public DocumentLanguage DocumentLanguage { get; set; } = DocumentLanguage.Turkish;

    public string? Notes { get; set; }
    public QuotationStatus Status { get; set; } = QuotationStatus.Draft;

    public DateTime? SentAt { get; set; }

    /// <summary>Kabul, ret ya da süre dolumu kaydı.</summary>
    public DateTime? DecidedAt { get; set; }
    public string? DecidedBy { get; set; }
    public string? DecisionNote { get; set; }

    /// <summary>Kabulde açılan taslak sipariş.</summary>
    public int? OrderId { get; set; }
    public BoyahaneOrder? Order { get; set; }

    public ICollection<BoyahaneQuotationLine> Lines { get; } = [];
}

/// <summary>
/// Teklif satırı ve maliyet dökümü. Hesaplanan birim maliyetler kayıt anındaki değerlerdir: lot fiyatı ya da parametre
/// sonradan değişse basılan teklif aynı kalır; taslakta yeniden hesaplanabilir. Tutarlar teklif para birimindedir ve
/// satırın biriminin (kg ya da m) birim başınadır.
/// </summary>
public class BoyahaneQuotationLine : AuditableEntity
{
    public int QuotationId { get; set; }
    public BoyahaneQuotation Quotation { get; set; } = null!;

    public OrderProcessType ProcessType { get; set; } = OrderProcessType.FabricDyeing;

    public int? FabricId { get; set; }
    public BoyahaneFabric? Fabric { get; set; }

    public int? YarnId { get; set; }
    public BoyahaneYarn? Yarn { get; set; }

    public string? ColorName { get; set; }
    public string? ColorCode { get; set; }

    public int? ColorCardId { get; set; }
    public BoyahaneColorCard? ColorCard { get; set; }

    public int? PatternId { get; set; }
    public BoyahanePattern? Pattern { get; set; }

    public int? ColorwayId { get; set; }
    public BoyahanePatternColorway? Colorway { get; set; }

    public decimal Quantity { get; set; }

    /// <summary>Metre ya da kilogram; iplikte kilogram. Birim fiyat bu birim içindir.</summary>
    public StockUnit Unit { get; set; } = StockUnit.Kilogram;

    /// <summary>Maliyetin dayandığı reçete (onaylı lab denemesi); seçilmemişse renk kartının son reçetesi kullanılmıştır.</summary>
    public int? RecipeTrialId { get; set; }
    public BoyahaneLabTrial? RecipeTrial { get; set; }

    /// <summary>Saat maliyeti ve parti büyüklüğü için seçilen makine; boşsa varsayılan saat maliyeti kullanılır.</summary>
    public int? MachineId { get; set; }
    public BoyahaneMachine? Machine { get; set; }

    /// <summary>Elle girilen parti büyüklüğü (kg); boşsa makinenin kapasitesi, o da yoksa varsayılan parti büyüklüğü.</summary>
    public decimal? BatchKilogramsInput { get; set; }

    /// <summary>Elle girilen flotte oranı; boşsa makinenin, o da yoksa parametrelerin varsayılanı.</summary>
    public decimal? LiquorRatioInput { get; set; }

    /// <summary>Elle girilen parti süresi (dakika); boşsa rotanın standart süresi, o da yoksa işlem türünün varsayılanı.</summary>
    public decimal? ProcessMinutesInput { get; set; }

    /// <summary>Elle girilen oranlar (%); boşsa parametrelerin varsayılanı.</summary>
    public decimal? FirePercentInput { get; set; }
    public decimal? OverheadPercentInput { get; set; }
    public decimal? MarginPercentInput { get; set; }

    /// <summary>Elle girilen birim fiyat; boşsa fiyat maliyet ve kâr payından hesaplanır.</summary>
    public decimal? UnitPriceInput { get; set; }

    // Hesaplanan değerler (kayıt anında).
    public decimal BatchKilograms { get; set; }
    public decimal? LiquorRatio { get; set; }
    public decimal ProcessMinutes { get; set; }
    public ProcessMinutesSource MinutesSource { get; set; } = ProcessMinutesSource.Default;

    /// <summary>Metre satırında 1 metrenin kilosu (ham en ve gramajdan); kilo satırında boş.</summary>
    public decimal? KilogramsPerMeter { get; set; }

    public decimal ChemicalCost { get; set; }
    public decimal PrintCost { get; set; }
    public decimal MachineCost { get; set; }
    public decimal LaborCost { get; set; }
    public decimal FirePercent { get; set; }
    public decimal FireCost { get; set; }
    public decimal OverheadPercent { get; set; }
    public decimal OverheadCost { get; set; }
    public decimal UnitCost { get; set; }

    /// <summary>Maliyet üzerine kâr payı (%); fiyat elle girildiyse fiyattan geri hesaplanır.</summary>
    public decimal? MarginPercent { get; set; }
    public decimal UnitPrice { get; set; }

    /// <summary>Hesapta eksik kalan bilgi (fiyatsız lot, kur, gramaj vb.); yoksa boş.</summary>
    public string? CostWarning { get; set; }

    public string? Notes { get; set; }
}

/// <summary>Ön maliyet ve planlama parametreleri (tek kayıt). Tutarlar <see cref="Currency"/> para birimindedir.</summary>
public class BoyahaneCostSettings : AuditableEntity
{
    public string Currency { get; set; } = "TRY";

    /// <summary>Makine saati başına enerji + su maliyeti; makineye özel maliyet girilmemişse kullanılır.</summary>
    public decimal DefaultMachineHourlyCost { get; set; }

    /// <summary>Dakika başına işçilik maliyeti.</summary>
    public decimal LaborCostPerMinute { get; set; }

    /// <summary>Makine ve satırda flotte yoksa kullanılan oran (1:8 için 8).</summary>
    public decimal DefaultLiquorRatio { get; set; } = 8;

    /// <summary>Makine seçilmemiş ve parti büyüklüğü girilmemiş teklif satırında parti büyüklüğü (kg).</summary>
    public decimal DefaultBatchKilograms { get; set; } = 500;

    public decimal OverheadPercent { get; set; } = 10;
    public decimal FirePercent { get; set; } = 3;
    public decimal MarginPercent { get; set; } = 20;

    /// <summary>Boya ve kimyasalın kilogram maliyeti: son lot ya da eldeki lotların ağırlıklı ortalaması.</summary>
    public ChemicalCostMethod ChemicalCostMethod { get; set; } = ChemicalCostMethod.LastLot;

    /// <summary>Rotada standart süre yoksa işlem türünün parti süresi (dakika); teklif ve kapasite planı kullanır.</summary>
    public decimal FabricDyeingMinutes { get; set; } = 360;
    public decimal YarnDyeingMinutes { get; set; } = 480;
    public decimal FinishingMinutes { get; set; } = 90;
    public decimal PrintingMinutes { get; set; } = 120;

    public decimal DefaultMinutes(OrderProcessType process) => process switch
    {
        OrderProcessType.YarnDyeing => YarnDyeingMinutes,
        OrderProcessType.Finishing => FinishingMinutes,
        OrderProcessType.Printing => PrintingMinutes,
        _ => FabricDyeingMinutes,
    };
}

/// <summary>Makineye özel saat maliyeti (enerji + su); kaydı olmayan makinede varsayılan kullanılır.</summary>
[NotAudited]
public class BoyahaneMachineCostRate
{
    public int MachineId { get; set; }
    public BoyahaneMachine Machine { get; set; } = null!;

    /// <summary>Parametrelerin para biriminde, makine saati başına.</summary>
    public decimal HourlyCost { get; set; }

    public DateTime UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
