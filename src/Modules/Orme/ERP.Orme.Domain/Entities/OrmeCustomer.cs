using ERP.Core.Domain.Common;
using ERP.Core.Domain.Enums;

namespace ERP.Orme.Domain.Entities;

/// <summary>Orme işletmesinin müşterisi; fason işte ipliğin sahibi, kendi malında kumaşın alıcısıdır. Diğer projelerin müşterilerinden ayrıdır.</summary>
public class OrmeCustomer : SoftDeletableEntity, IActivatable
{
    public string CustomerCode { get; set; } = null!;

    /// <summary>Ticari unvan (tüzel kişi) ya da ekranda görünen ad.</summary>
    public string Name { get; set; } = null!;

    public PartyType PartyType { get; set; } = PartyType.Company;

    /// <summary>Gerçek kişi müşteride e-belgeye yazılan ad ve soyad.</summary>
    public string? FirstName { get; set; }
    public string? LastName { get; set; }

    /// <summary>VKN (10 hane), TCKN (11 hane) ya da yurt dışı müşterinin vergi numarası.</summary>
    public string? TaxNumber { get; set; }
    public string? TaxOffice { get; set; }
    public string? MersisNo { get; set; }
    public string? TradeRegistryNo { get; set; }

    public string? Phone { get; set; }
    public string? Email { get; set; }

    /// <summary>Müşteri e-Fatura mükellefi mi (GİB kayıtlı kullanıcı listesinde).</summary>
    public bool IsEInvoiceUser { get; set; }

    /// <summary>e-Fatura posta kutusu etiketi (örn. urn:mail:defaultpk@firma.com.tr).</summary>
    public string? EInvoiceAlias { get; set; }
    public EInvoiceProfile EInvoiceProfile { get; set; } = EInvoiceProfile.Basic;

    /// <summary>Müşteri e-İrsaliye mükellefi mi.</summary>
    public bool IsEDespatchUser { get; set; }

    /// <summary>e-İrsaliye posta kutusu etiketi.</summary>
    public string? EDespatchAlias { get; set; }

    /// <summary>e-Fatura mükellefi değilse kesilen e-Arşiv faturanın teslim şekli.</summary>
    public EArchiveDelivery EArchiveDelivery { get; set; } = EArchiveDelivery.Paper;

    /// <summary>e-Arşiv faturanın gönderileceği e-posta; boşsa genel e-posta kullanılır.</summary>
    public string? EArchiveEmail { get; set; }

    /// <summary>Müşteriyle çalışılan para birimi (ISO 4217, örn. EUR).</summary>
    public string Currency { get; set; } = "TRY";

    /// <summary>Fason işte kabul edilen iplik firesi (%); tanımlı değilse boş.</summary>
    public decimal? FireTolerancePercent { get; set; }
    /// <summary>Faturaya gelen varsayılan KDV oranı (%); boşsa faturada girilir.</summary>
    public decimal? VatRate { get; set; }

    /// <summary>Ödeme vadesi (gün); tanımlı değilse boş.</summary>
    public int? PaymentTermDays { get; set; }
    /// <summary>Müşteriye özel 1. kalite üst sınırı (100 m² başına puan); boşsa genel sınır.</summary>
    public decimal? FirstQualityMaxPoints { get; set; }

    /// <summary>Müşteriye özel 2. kalite üst sınırı (100 m² başına puan); boşsa genel sınır.</summary>
    public decimal? SecondQualityMaxPoints { get; set; }

    /// <summary>2. kalite kumaşta faturaya gelen indirim (%); boşsa indirim yok.</summary>
    public decimal? SecondQualityDiscountPercent { get; set; }

    /// <summary>Müşteriye basılan belge ve etiketlerin dili (çeki listesi, irsaliye, fatura, proforma, top etiketi).</summary>
    public DocumentLanguage DocumentLanguage { get; set; } = DocumentLanguage.Turkish;

    public bool IsActive { get; set; } = true;

    public ICollection<OrmeCustomerAddress> Addresses { get; } = [];

    public ICollection<OrmeCustomerContact> Contacts { get; } = [];
}

/// <summary>Müşteri adresi; biri varsayılan fatura, biri varsayılan teslimat adresidir (aynı adres ikisi de olabilir).</summary>
public class OrmeCustomerAddress : AuditableEntity
{
    public int CustomerId { get; set; }
    public OrmeCustomer Customer { get; set; } = null!;

    /// <summary>Adresin kısa adı (örn. "Merkez", "Depo").</summary>
    public string Title { get; set; } = null!;

    /// <summary>Mahalle, cadde / sokak, bina ve kapı numarası.</summary>
    public string AddressLine { get; set; } = null!;
    public string District { get; set; } = null!;
    public string City { get; set; } = null!;
    public string? PostalCode { get; set; }

    /// <summary>ISO 3166-1 alfa-2 ülke kodu (TR, DE …).</summary>
    public string CountryCode { get; set; } = "TR";

    public bool IsDefaultInvoice { get; set; }
    public bool IsDefaultDelivery { get; set; }
}

/// <summary>Müşterinin yetkili kişisi.</summary>
public class OrmeCustomerContact : AuditableEntity
{
    public int CustomerId { get; set; }
    public OrmeCustomer Customer { get; set; } = null!;

    public string Name { get; set; } = null!;

    /// <summary>Görevi (örn. "Satın alma müdürü").</summary>
    public string? Title { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
}
