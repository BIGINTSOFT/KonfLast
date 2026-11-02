using ERP.Core.Domain.Enums;
using ERP.Konfeksiyon.Domain.Enums;
using ERP.Core.Domain.Common;

namespace ERP.Konfeksiyon.Domain.Entities;

public class Customer : SoftDeletableEntity, IActivatable
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

    /// <summary>Müşteriye verilen siparişlerin varsayılan para birimi (ISO 4217, örn. EUR).</summary>
    public string Currency { get; set; } = "TRY";

    /// <summary>Ödeme vadesi (gün); faturanın vadesi buradan hesaplanır. Boşsa vade faturada girilir.</summary>
    public int? PaymentTermDays { get; set; }

    /// <summary>
    /// Açıksa müşterinin kendi sipariş numarası tekildir: aynı numara iptal edilmemiş ikinci bir siparişte
    /// kullanılamaz. Kapalıysa müşteri aynı numarayı birden fazla siparişte kullanabilir.
    /// </summary>
    public bool RequireUniqueCustomerOrderNo { get; set; }

    /// <summary>Müşteriye basılan belgelerin dili (proforma, fatura, irsaliye, çeki listesi, koli etiketi).</summary>
    public DocumentLanguage DocumentLanguage { get; set; } = DocumentLanguage.Turkish;

    public bool IsActive { get; set; } = true;

    public ICollection<CustomerAddress> Addresses { get; } = [];

    /// <summary>Müşteri adına sipariş veren alıcılar (buyer / alt müşteri).</summary>
    public ICollection<CustomerBuyer> Buyers { get; } = [];
}
