namespace ERP.Konfeksiyon.Domain.Enums;

/// <summary>Fason gönderim fişinin durumu; dönüş fişleriyle ilerler.</summary>
public enum SubcontractSendStatus
{
    /// <summary>Gönderildi, henüz hiçbir dönüş işlenmedi.</summary>
    Open,

    /// <summary>Bir kısmı döndü; fasoncuda hâlâ parça var.</summary>
    PartiallyReturned,

    /// <summary>Gönderilen bütün parçalar döndü (sağlam, hatalı ya da eksik olarak).</summary>
    Closed,

    /// <summary>Hiç dönüş işlenmeden iptal edildi; demetler sahaya döndü.</summary>
    Cancelled,
}

/// <summary>Teklifin durumu.</summary>
public enum QuotationStatus
{
    Draft,

    /// <summary>Müşteriye gönderildi; içerik kilitlidir.</summary>
    Sent,

    /// <summary>Müşteri kabul etti; taslak müşteri siparişi açıldı.</summary>
    Accepted,

    Rejected,

    /// <summary>Geçerlilik süresi doldu.</summary>
    Expired,
}
