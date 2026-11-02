namespace ERP.Dokuma.Domain.Enums;

/// <summary>Teklifin durumu: taslak → gönderildi → kabul / ret / süresi doldu.</summary>
public enum QuotationStatus
{
    /// <summary>Hazırlanıyor; düzenlenir ve silinir.</summary>
    Draft,

    /// <summary>Müşteriye gönderildi; kabul ya da ret bekliyor.</summary>
    Sent,

    /// <summary>Kabul edildi; taslak sipariş açıldı.</summary>
    Accepted,

    Rejected,

    /// <summary>Geçerlilik tarihi geçti, cevap gelmedi.</summary>
    Expired,
}
