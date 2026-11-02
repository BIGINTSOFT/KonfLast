namespace ERP.Core.Domain.Enums;

/// <summary>Kurun nereden geldiği.</summary>
public enum ExchangeRateSource
{
    /// <summary>TCMB gösterge niteliğindeki kurlar (tcmb.gov.tr/kurlar).</summary>
    Tcmb,

    /// <summary>Kullanıcının elle girdiği ya da düzelttiği kur; TCMB'den yeniden çekmede üzerine yazılmaz.</summary>
    Manual,
}

/// <summary>Dönüştürmede kullanılan kur türü.</summary>
public enum ExchangeRateType
{
    ForexBuying,
    ForexSelling,
    BanknoteBuying,
    BanknoteSelling,
}
