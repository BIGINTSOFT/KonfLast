namespace ERP.Orme.Domain.Enums;

/// <summary>İplik satın alma siparişinin durumu. Teslim durumları lotlardan hesaplanır.</summary>
public enum PurchaseOrderStatus
{
    /// <summary>Hazırlanıyor; düzenlenir ve silinmeden iptal edilir.</summary>
    Draft,

    /// <summary>Onaylandı, tedarikçiye gönderildi; teslim bekleniyor.</summary>
    Approved,

    PartiallyReceived,
    Received,

    /// <summary>Kısmi teslimden sonra kalan artık beklenmiyor.</summary>
    Closed,

    Cancelled,
}
