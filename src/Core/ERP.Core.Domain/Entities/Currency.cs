using ERP.Core.Domain.Common;
using ERP.Core.Domain.Enums;

namespace ERP.Core.Domain.Entities;

/// <summary>
/// Para birimi (ISO 4217). Kurlar yerel para birimi (TRY) karşılığı olarak tutulur: 1 birim döviz = X TRY.
/// </summary>
public class Currency : SoftDeletableEntity, IActivatable
{
    /// <summary>Yerel para birimi; kuru tutulmaz, silinemez ve pasif yapılamaz.</summary>
    public const string LocalCode = "TRY";

    /// <summary>Üç harfli ISO 4217 kodu (TRY, USD, EUR …).</summary>
    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    /// <summary>Gösterim simgesi (₺, $, € …).</summary>
    public string? Symbol { get; set; }

    /// <summary>Tutarların gösterildiği ondalık hane sayısı.</summary>
    public int DecimalPlaces { get; set; } = 2;

    /// <summary>Açıksa kur TCMB'den otomatik çekilir (TCMB listesinde olan dövizler).</summary>
    public bool FetchFromTcmb { get; set; } = true;

    public bool IsActive { get; set; } = true;

    public bool IsLocal => Code == LocalCode;
}

/// <summary>
/// Bir dövizin belirli bir günkü kuru (1 birim döviz kaç TRY). Bir gün için kur yoksa o günden önceki son kur
/// kullanılır (hafta sonu, tatil).
/// </summary>
public class ExchangeRate : AuditableEntity
{
    public int CurrencyId { get; set; }
    public Currency Currency { get; set; } = null!;

    /// <summary>Kurun geçerli olduğu gün (TCMB'de bültenin tarihi).</summary>
    public DateOnly RateDate { get; set; }

    public decimal ForexBuying { get; set; }
    public decimal ForexSelling { get; set; }
    public decimal? BanknoteBuying { get; set; }
    public decimal? BanknoteSelling { get; set; }

    public ExchangeRateSource Source { get; set; }

    /// <summary>İstenen kur türünün değeri; efektif kur yoksa döviz kuru kullanılır.</summary>
    public decimal Rate(ExchangeRateType type) => type switch
    {
        ExchangeRateType.ForexBuying => ForexBuying,
        ExchangeRateType.ForexSelling => ForexSelling,
        ExchangeRateType.BanknoteBuying => BanknoteBuying ?? ForexBuying,
        ExchangeRateType.BanknoteSelling => BanknoteSelling ?? ForexSelling,
        _ => throw new ArgumentOutOfRangeException(nameof(type)),
    };
}
