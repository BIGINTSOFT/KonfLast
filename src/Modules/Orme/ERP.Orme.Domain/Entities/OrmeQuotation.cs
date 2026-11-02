using ERP.Core.Domain.Common;
using ERP.Core.Domain.Enums;
using ERP.Orme.Domain.Enums;

namespace ERP.Orme.Domain.Entities;

/// <summary>
/// Ön maliyet parametreleri (tek kayıt). Saatlik tutarlar yerel para biriminde (TRY) girilir; teklifte teklifin para
/// birimine çevrilir. Makineye özel saatlik maliyet <see cref="OrmeMachineCostRate"/> ile verilir.
/// </summary>
public class OrmeCostSettings : AuditableEntity
{
    /// <summary>Varsayılan makine saat maliyeti (amortisman, enerji, bakım).</summary>
    public decimal MachineHourlyCost { get; set; }

    /// <summary>Makine saati başına işçilik maliyeti.</summary>
    public decimal LaborHourlyCost { get; set; }

    /// <summary>Genel gider (%): doğrudan maliyetin (iplik + örme) üzerine eklenir.</summary>
    public decimal OverheadPercent { get; set; }

    /// <summary>Kumaş kartında fire girilmemişse kullanılan iplik firesi (%).</summary>
    public decimal DefaultWastePercent { get; set; } = 3;

    /// <summary>Varsayılan kâr (%): toplam maliyetin üzerine eklenir.</summary>
    public decimal DefaultMarginPercent { get; set; } = 15;
}

/// <summary>Makineye özel saat maliyeti (TRY); tanımlı değilse genel makine saat maliyeti kullanılır.</summary>
public class OrmeMachineCostRate : AuditableEntity
{
    public int MachineId { get; set; }
    public OrmeMachine Machine { get; set; } = null!;

    public decimal HourlyCost { get; set; }
}

/// <summary>
/// Satış teklifi: müşteriye (ya da henüz kartı açılmamış aday firmaya) kumaş başına kilo fiyatı. Satır maliyetleri teklif
/// hazırlanırken anlık görüntü olarak saklanır; iplik fiyatı ya da parametre sonradan değişse de teklif değişmez.
/// Kabul edilen teklifin satırlarından taslak örme siparişi açılır.
/// </summary>
public class OrmeQuotation : AuditableEntity
{
    public string QuotationNo { get; set; } = null!;
    public DateOnly QuotationDate { get; set; }

    /// <summary>Teklifin geçerli olduğu son gün; gönderilmiş teklif bu tarihten sonra süresi dolmuş sayılır.</summary>
    public DateOnly ValidUntil { get; set; }

    /// <summary>Kartı olan müşteri; boşsa <see cref="ProspectName"/> aday firmadır.</summary>
    public int? CustomerId { get; set; }
    public OrmeCustomer? Customer { get; set; }

    /// <summary>Müşteri kartı olmayan aday firmanın adı.</summary>
    public string? ProspectName { get; set; }

    /// <summary>Fiyatların ve maliyetlerin para birimi.</summary>
    public string Currency { get; set; } = "TRY";

    /// <summary>Belgenin basılacağı dil; boşsa müşterinin belge dili (aday firmada Türkçe).</summary>
    public DocumentLanguage? DocumentLanguage { get; set; }

    public QuotationStatus Status { get; set; } = QuotationStatus.Draft;
    public string? Notes { get; set; }

    public ICollection<OrmeQuotationLine> Lines { get; } = [];
}

/// <summary>
/// Teklif satırı: kumaş, kilo ve kilo başına maliyet kırılımı. Fasonda (iplik müşteriden) iplik maliyeti sıfırdır, fiyat
/// yalnızca örme ücretidir. Maliyet: iplik (oran × fiyat × (1 + fire)) + örme ((makine + işçilik) / kg/saat),
/// üzerine genel gider; fiyat = maliyet × (1 + kâr).
/// </summary>
public class OrmeQuotationLine : AuditableEntity
{
    public int QuotationId { get; set; }
    public OrmeQuotation Quotation { get; set; } = null!;

    /// <summary>Sıra (1'den başlar).</summary>
    public int Sequence { get; set; }

    public int FabricId { get; set; }
    public OrmeFabric Fabric { get; set; } = null!;

    public decimal Kilograms { get; set; }

    /// <summary>Fason: iplik müşteriden gelir, iplik maliyeti sıfırdır.</summary>
    public bool IsCommission { get; set; }

    /// <summary>Saat maliyeti için seçilen makine; boşsa genel makine saat maliyeti.</summary>
    public int? MachineId { get; set; }
    public OrmeMachine? Machine { get; set; }

    /// <summary>İplik firesi (%).</summary>
    public decimal WastePercent { get; set; }

    /// <summary>Saatte örülen kilo; boşsa örme maliyeti hesaplanamaz.</summary>
    public decimal? KilogramsPerHour { get; set; }

    /// <summary>Makine saat maliyeti (teklifin para biriminde).</summary>
    public decimal MachineHourlyCost { get; set; }

    /// <summary>Makine saati başına işçilik (teklifin para biriminde).</summary>
    public decimal LaborHourlyCost { get; set; }

    public decimal OverheadPercent { get; set; }

    /// <summary>Toplam maliyet üzerine kâr (%); fiyat elle girilirse fiyattan yeniden hesaplanır.</summary>
    public decimal MarginPercent { get; set; }

    /// <summary>Kilo başına teklif fiyatı.</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>Müşterinin bu kumaşa verdiği kendi kodu; siparişe aktarılır.</summary>
    public string? CustomerFabricCode { get; set; }

    public string? Notes { get; set; }

    /// <summary>Teklif kabul edildiğinde açılan sipariş satırı; sipariş silinirse boşalır.</summary>
    public int? OrderLineId { get; set; }
    public OrmeOrderLine? OrderLine { get; set; }

    public ICollection<OrmeQuotationLineYarn> Yarns { get; } = [];
}

/// <summary>Teklif satırındaki iplik: kumaş reçetesindeki oranı ve teklifte kullanılan kilo fiyatı.</summary>
public class OrmeQuotationLineYarn : AuditableEntity
{
    public int LineId { get; set; }
    public OrmeQuotationLine Line { get; set; } = null!;

    public int YarnId { get; set; }
    public OrmeYarn Yarn { get; set; } = null!;

    /// <summary>Kumaş ağırlığındaki oran (%); teklif anındaki reçeteden.</summary>
    public decimal Percent { get; set; }

    /// <summary>Kilo fiyatı (teklifin para biriminde); bulunamadıysa boş.</summary>
    public decimal? UnitPrice { get; set; }

    public QuotationPriceSource PriceSource { get; set; } = QuotationPriceSource.None;

    /// <summary>Fiyatın kaynağı olan belge (satın alma siparişi ya da lot no, orijinal fiyatıyla).</summary>
    public string? PriceReference { get; set; }
}
