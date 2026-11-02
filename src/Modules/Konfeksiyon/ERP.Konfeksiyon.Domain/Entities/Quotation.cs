using ERP.Core.Domain.Common;
using ERP.Core.Domain.Enums;
using ERP.Konfeksiyon.Domain.Enums;

namespace ERP.Konfeksiyon.Domain.Entities;

/// <summary>
/// Ön maliyet parametreleri (tek satır): teklifte işçilik, enerji, genel gider, fire ve kâr hesabının varsayılanları.
/// Teklif satırında yüzdeler değiştirilebilir; dakika ücretleri teklif kaydedilirken bu ayardan okunur.
/// </summary>
public class CostSetting : AuditableEntity
{
    /// <summary>Ayarlar tek satırdır; kimliği sabittir.</summary>
    public const int SingletonId = 1;

    /// <summary>Bir dakikalık dikim işçiliğinin maliyeti (ücret + yan haklar).</summary>
    public decimal LaborCostPerMinute { get; set; }

    /// <summary>Bir dakikalık dikimin enerji maliyeti; kullanılmıyorsa 0.</summary>
    public decimal EnergyCostPerMinute { get; set; }

    /// <summary>Dakika maliyetlerinin para birimi (ISO 4217).</summary>
    public string Currency { get; set; } = ERP.Core.Domain.Entities.Currency.LocalCode;

    /// <summary>Genel gider payı (%): doğrudan maliyet + fire üzerine eklenir.</summary>
    public decimal OverheadPercent { get; set; }

    /// <summary>Varsayılan fire (%): doğrudan maliyet üzerine eklenir.</summary>
    public decimal DefaultFirePercent { get; set; }

    /// <summary>Varsayılan kâr marjı (%): birim maliyet üzerine eklenir.</summary>
    public decimal DefaultMarginPercent { get; set; }
}

/// <summary>
/// Teklif: müşteriye (ya da henüz müşteri kartı olmayan adaya) verilen fiyat teklifi. Her satırın parça başı maliyeti
/// reçete, rota ve fason fiyatlarından hesaplanır; kâr marjıyla birim fiyat bulunur. Kabul edilen tekliften taslak
/// müşteri siparişi açılır.
/// </summary>
public class Quotation : SoftDeletableEntity
{
    private static readonly IReadOnlyDictionary<QuotationStatus, QuotationStatus[]> Transitions =
        new Dictionary<QuotationStatus, QuotationStatus[]>
        {
            [QuotationStatus.Draft] = [QuotationStatus.Sent, QuotationStatus.Rejected],
            [QuotationStatus.Sent] =
                [QuotationStatus.Accepted, QuotationStatus.Rejected, QuotationStatus.Expired, QuotationStatus.Draft],
            [QuotationStatus.Accepted] = [],
            [QuotationStatus.Rejected] = [QuotationStatus.Draft],
            [QuotationStatus.Expired] = [QuotationStatus.Draft],
        };

    /// <summary>Sistem tarafından üretilir, örn. TK-2026-00001.</summary>
    public string QuotationNo { get; set; } = null!;

    /// <summary>Müşteri kartı; aday müşteride boştur (kabulde seçilir).</summary>
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    /// <summary>Müşteri kartı yoksa adayın adı.</summary>
    public string? ProspectName { get; set; }

    public DateOnly QuotationDate { get; set; }
    public DateOnly ValidUntil { get; set; }

    /// <summary>Fiyat ve maliyetlerin para birimi.</summary>
    public string Currency { get; set; } = ERP.Core.Domain.Entities.Currency.LocalCode;

    /// <summary>Basılı teklifin dili; müşteri kartından gelir.</summary>
    public DocumentLanguage DocumentLanguage { get; set; } = DocumentLanguage.Turkish;

    public QuotationStatus Status { get; private set; } = QuotationStatus.Draft;

    public string? Notes { get; set; }

    /// <summary>Teklif kaydedilirken ayardan okunan dakika maliyetleri (teklifin para biriminde).</summary>
    public decimal LaborCostPerMinute { get; set; }
    public decimal EnergyCostPerMinute { get; set; }

    public DateTime? SentAt { get; private set; }
    public DateTime? DecidedAt { get; private set; }
    public string? DecidedBy { get; private set; }

    /// <summary>Kabulde açılan taslak müşteri siparişi.</summary>
    public int? SalesOrderId { get; private set; }
    public SalesOrder? SalesOrder { get; private set; }

    public byte[] RowVersion { get; set; } = null!;

    public ICollection<QuotationLine> Lines { get; } = new List<QuotationLine>();

    public bool IsEditable => Status == QuotationStatus.Draft;

    public IReadOnlyList<QuotationStatus> AllowedTransitions => Transitions[Status];

    public bool CanTransitionTo(QuotationStatus target) => Transitions[Status].Contains(target);

    public void Send(DateTime utcNow)
    {
        Move(QuotationStatus.Sent);
        SentAt = utcNow;
    }

    /// <summary>Gönderilmiş, reddedilmiş ya da süresi dolmuş teklifi düzeltmek için taslağa alır.</summary>
    public void Reopen()
    {
        Move(QuotationStatus.Draft);
        DecidedAt = null;
        DecidedBy = null;
    }

    public void Reject(DateTime utcNow, string user) => Decide(QuotationStatus.Rejected, utcNow, user);

    public void Expire(DateTime utcNow, string user) => Decide(QuotationStatus.Expired, utcNow, user);

    /// <summary>Kabul: açılan taslak müşteri siparişi teklife bağlanır.</summary>
    public void Accept(int salesOrderId, DateTime utcNow, string user)
    {
        Decide(QuotationStatus.Accepted, utcNow, user);
        SalesOrderId = salesOrderId;
    }

    private void Decide(QuotationStatus target, DateTime utcNow, string user)
    {
        Move(target);
        DecidedAt = utcNow;
        DecidedBy = user;
    }

    private void Move(QuotationStatus target)
    {
        if (!CanTransitionTo(target))
        {
            throw new InvalidOperationException($"{Status} durumundaki teklif {target} durumuna geçemez.");
        }

        Status = target;
    }
}

/// <summary>
/// Teklif satırı: bir ürün (ya da ürün kartı olmayan bir model için serbest açıklama), adet ve parça başı maliyet
/// dökümü. Tutarlar teklifin para birimindedir ve kayıt anındaki hesaptır.
/// </summary>
public class QuotationLine : BaseEntity
{
    public int QuotationId { get; set; }
    public Quotation Quotation { get; set; } = null!;

    public int LineNo { get; set; }

    /// <summary>Var olan ürün; boşsa açıklama ve model referansı girilir.</summary>
    public int? ProductId { get; set; }
    public Product? Product { get; set; }

    public string? Description { get; set; }

    /// <summary>Müşterinin model / artikel referansı.</summary>
    public string? ModelReference { get; set; }

    /// <summary>Renk(ler), serbest metin (ör. "Siyah, Lacivert").</summary>
    public string? Colors { get; set; }

    public int Quantity { get; set; }

    /// <summary>Kullanıcının girdiği dakika (rota yerine); boşsa etkin rotanın fasonsuz SAM toplamı.</summary>
    public decimal? ManualMinutes { get; set; }

    /// <summary>Kullanıcının girdiği fason maliyeti (parça başı); boşsa fason fiyatlarından hesaplanır.</summary>
    public decimal? ManualSubcontractCost { get; set; }

    /// <summary>Kullanıcı birim fiyatı elle verdi; marj fiyattan geri hesaplanır.</summary>
    public bool PriceOverridden { get; set; }

    public decimal FirePercent { get; set; }
    public decimal OverheadPercent { get; set; }
    public decimal MarginPercent { get; set; }

    /// <summary>Etkin rotanın fason olmayan adımlarının SAM toplamı (kayıt anında); rota yoksa boş.</summary>
    public decimal? RouteMinutes { get; set; }

    /// <summary>Rotadaki fason adımların fiyatlarından gelen parça başı fason maliyeti (kayıt anında).</summary>
    public decimal RouteSubcontractCost { get; set; }

    // Parça başı maliyet dökümü (teklifin para biriminde)
    public decimal MaterialCost { get; set; }
    public decimal LaborMinutes { get; set; }
    public decimal LaborCost { get; set; }
    public decimal EnergyCost { get; set; }
    public decimal SubcontractCost { get; set; }
    public decimal FireCost { get; set; }
    public decimal OverheadCost { get; set; }
    public decimal UnitCost { get; set; }
    public decimal UnitPrice { get; set; }

    /// <summary>Hesapta karşılaşılan eksikler (kur yok, rota yok, maliyeti olmayan malzeme ...), satır satır.</summary>
    public string? Warnings { get; set; }

    public ICollection<QuotationLineMaterial> Materials { get; } = new List<QuotationLineMaterial>();

    public decimal Amount => decimal.Round(Quantity * UnitPrice, 2);
}

/// <summary>Teklif satırının malzemesi: reçeteden gelir ya da elle girilir; birim maliyeti değiştirilebilir.</summary>
public class QuotationLineMaterial : BaseEntity
{
    public int QuotationLineId { get; set; }
    public QuotationLine QuotationLine { get; set; } = null!;

    public int SortOrder { get; set; }

    /// <summary>Stok kartı; elle eklenen malzemede boş olabilir.</summary>
    public int? StockItemId { get; set; }
    public StockItem? StockItem { get; set; }

    public string Description { get; set; } = null!;

    public StockUnit? Unit { get; set; }

    /// <summary>Parça başı miktar, fire dahil.</summary>
    public decimal QuantityPerPiece { get; set; }

    /// <summary>Birim maliyet (teklifin para biriminde).</summary>
    public decimal UnitCost { get; set; }

    /// <summary>Birim maliyet elle girildi (stok kartından okunmaz).</summary>
    public bool UnitCostOverridden { get; set; }

    public decimal Cost => decimal.Round(QuantityPerPiece * UnitCost, 4);
}
