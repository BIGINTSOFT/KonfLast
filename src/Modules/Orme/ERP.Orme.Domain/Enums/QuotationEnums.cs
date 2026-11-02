namespace ERP.Orme.Domain.Enums;

/// <summary>Teklifin durumu: taslak → gönderildi → kabul | ret | süresi doldu.</summary>
public enum QuotationStatus
{
    /// <summary>Hazırlanıyor; satırlar ve maliyetler düzenlenir.</summary>
    Draft,

    /// <summary>Müşteriye gönderildi; kabul ya da ret beklenir.</summary>
    Sent,

    /// <summary>Müşteri kabul etti; satırlardan taslak sipariş açıldı.</summary>
    Accepted,

    Rejected,

    /// <summary>Gönderilmiş teklifin geçerlilik tarihi geçti.</summary>
    Expired,
}

/// <summary>Teklifteki iplik fiyatının nereden geldiği.</summary>
public enum QuotationPriceSource
{
    /// <summary>Fiyat bulunamadı; elle girilmeli.</summary>
    None,

    /// <summary>İpliğin son satın alma siparişindeki fiyatı.</summary>
    PurchaseOrder,

    /// <summary>İpliğin son teslim alınan lotunun (bağlı olduğu satın alma satırından) fiyatı.</summary>
    YarnLot,

    /// <summary>Teklifte elle girildi.</summary>
    Manual,
}
