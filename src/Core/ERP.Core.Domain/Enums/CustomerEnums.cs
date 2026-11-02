namespace ERP.Core.Domain.Enums;

/// <summary>Müşterinin hukuki türü; e-belgede tüzel kişi unvanla, gerçek kişi ad-soyadla yer alır.</summary>
public enum PartyType
{
    /// <summary>Tüzel kişi (şirket): vergi kimlik numarası (VKN, 10 hane).</summary>
    Company,

    /// <summary>Gerçek kişi (şahıs şirketi, bireysel): T.C. kimlik numarası (TCKN, 11 hane).</summary>
    Individual,
}

/// <summary>e-Fatura senaryosu (GİB profili).</summary>
public enum EInvoiceProfile
{
    /// <summary>TEMELFATURA: alıcı kabul / ret yanıtı vermez.</summary>
    Basic,

    /// <summary>TICARIFATURA: alıcı 8 gün içinde kabul ya da ret yanıtı verir.</summary>
    Commercial,
}

/// <summary>e-Fatura mükellefi olmayan müşteriye kesilen e-Arşiv faturanın teslim şekli.</summary>
public enum EArchiveDelivery
{
    /// <summary>Fatura e-posta ile gönderilir.</summary>
    Electronic,

    /// <summary>Fatura kâğıda basılıp teslim edilir.</summary>
    Paper,
}

/// <summary>Müşteriye basılan belge ve etiketlerin dili (çeki listesi, irsaliye, fatura, proforma, etiket).</summary>
public enum DocumentLanguage
{
    Turkish,
    English,
}
