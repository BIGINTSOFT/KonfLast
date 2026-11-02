using System.ComponentModel.DataAnnotations;
using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Core.Domain.Entities;
using ERP.Core.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ERP.Core.Application.Features.Finance;

public sealed class ExchangeRateOptions
{
    public const string SectionName = "ExchangeRates";

    /// <summary>Açıksa günün TCMB kurları arka planda, <see cref="FetchAfter"/> saatinden sonra otomatik çekilir.</summary>
    public bool AutoFetch { get; set; }

    /// <summary>TCMB kurları 15:30'da ilan eder; otomatik çekme bu yerel saatten sonra denenir.</summary>
    public TimeOnly FetchAfter { get; set; } = new(15, 45);

    /// <summary>Otomatik çekmede günün bülteni gelmemişse yeniden deneme aralığı.</summary>
    [Range(5, 1440)]
    public int RetryMinutes { get; set; } = 30;

    /// <summary>TCMB kur sayfasının adresi (kurum ağında vekil sunucu ile değiştirilebilir).</summary>
    [Required, Url]
    public string TcmbBaseUrl { get; set; } = "https://www.tcmb.gov.tr/kurlar/";

    [Range(1, 120)]
    public int TimeoutSeconds { get; set; } = 20;
}

/// <summary>Kur sağlayıcısından gelen bir dövizin kuru; değerler <see cref="Unit"/> birim döviz içindir.</summary>
public sealed record ProviderRate(
    string CurrencyCode, int Unit, decimal ForexBuying, decimal ForexSelling, decimal? BanknoteBuying, decimal? BanknoteSelling);

/// <param name="Date">Bültenin tarihi.</param>
public sealed record ProviderBulletin(DateOnly Date, IReadOnlyList<ProviderRate> Rates);

/// <summary>Dış kur kaynağı (TCMB). Kaynağa ait ayrıntılar (adres, XML biçimi) altyapı katmanındadır.</summary>
public interface IExchangeRateProvider
{
    /// <summary>
    /// Verilen günün bültenini (gün verilmezse en son bülteni) getirir. O gün bülten yayımlanmamışsa (hafta sonu,
    /// tatil) null döner; kaynağa ulaşılamazsa <see cref="ExchangeRateProviderException"/> fırlatır.
    /// </summary>
    Task<ProviderBulletin?> GetAsync(DateOnly? date, CancellationToken cancellationToken);
}

public sealed class ExchangeRateProviderException(string message, Exception? inner = null) : Exception(message, inner);

/// <param name="RateDate">Gösterilen kurun tarihi; istenen günde kur yoksa önceki son kurun tarihi.</param>
/// <param name="IsCarriedOver">İstenen günde kur yok; önceki günün kuru gösteriliyor.</param>
public sealed record ExchangeRateRowDto(
    int CurrencyId,
    string Code,
    string Name,
    bool FetchFromTcmb,
    int? Id,
    DateOnly? RateDate,
    decimal? ForexBuying,
    decimal? ForexSelling,
    decimal? BanknoteBuying,
    decimal? BanknoteSelling,
    ExchangeRateSource? Source,
    bool IsCarriedOver,
    DateTime? UpdatedAt,
    string? UpdatedBy);

public sealed record ExchangeRateDayDto(DateOnly Date, string LocalCurrency, IReadOnlyList<ExchangeRateRowDto> Rates);

public sealed record ExchangeRateHistoryItemDto(
    int Id, DateOnly RateDate, decimal ForexBuying, decimal ForexSelling, decimal? BanknoteBuying, decimal? BanknoteSelling,
    ExchangeRateSource Source);

public sealed record ExchangeRateEntryRequest(
    int CurrencyId, decimal ForexBuying, decimal ForexSelling, decimal? BanknoteBuying = null, decimal? BanknoteSelling = null);

/// <summary>Bir günün kurlarını elle girer ya da düzeltir; girilen kurlar "elle" olarak işaretlenir.</summary>
public sealed record SaveExchangeRatesRequest(DateOnly Date, IReadOnlyList<ExchangeRateEntryRequest> Rates);

/// <param name="Date">Çekilecek gün; boşsa TCMB'nin en son bülteni.</param>
/// <param name="OverwriteManual">Açıksa elle girilmiş kurların üzerine de yazılır.</param>
public sealed record FetchExchangeRatesRequest(DateOnly? Date = null, bool OverwriteManual = false);

/// <param name="BulletinDate">Bültenin tarihi; o gün bülten yoksa null.</param>
/// <param name="SkippedManual">Elle girildiği için üzerine yazılmayan dövizler.</param>
/// <param name="NotListed">TCMB'den çekilecek olarak işaretli olup bültende bulunmayan dövizler.</param>
public sealed record FetchExchangeRatesResultDto(
    DateOnly? BulletinDate, int Imported, IReadOnlyList<string> SkippedManual, IReadOnlyList<string> NotListed);

public sealed class SaveExchangeRatesRequestValidator : AbstractValidator<SaveExchangeRatesRequest>
{
    public const decimal MaxRate = 1_000_000m;

    public SaveExchangeRatesRequestValidator()
    {
        RuleFor(x => x.Date).NotEqual(default(DateOnly)).WithMessage("Tarih zorunludur.");
        RuleFor(x => x.Rates).NotNull().WithMessage("Kur listesi zorunludur.");
        When(x => x.Rates is not null, () =>
        {
            RuleFor(x => x.Rates)
                .Must(rates => rates.Select(r => r.CurrencyId).Distinct().Count() == rates.Count)
                .WithMessage("Bir döviz için tek kur girilebilir.")
                .Must(rates => rates.All(r => Valid(r.ForexBuying) && Valid(r.ForexSelling)
                    && (r.BanknoteBuying is null || Valid(r.BanknoteBuying.Value))
                    && (r.BanknoteSelling is null || Valid(r.BanknoteSelling.Value))))
                .WithMessage("Kurlar sıfırdan büyük olmalı ve en fazla 6 ondalık basamak içermelidir.")
                .Must(rates => rates.All(r => r.ForexSelling >= r.ForexBuying))
                .WithMessage("Döviz satış kuru alış kurundan küçük olamaz.");
        });
    }

    private static bool Valid(decimal rate) => rate > 0 && rate <= MaxRate && decimal.Round(rate, 6) == rate;
}

public interface IExchangeRateService
{
    /// <summary>Aktif dövizlerin verilen gündeki kurları; o gün kuru olmayan dövizde önceki son kur gösterilir.</summary>
    Task<ExchangeRateDayDto> GetDayAsync(DateOnly date, CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<ExchangeRateHistoryItemDto>>> GetHistoryAsync(
        int currencyId, DateOnly from, DateOnly to, CancellationToken cancellationToken);

    Task<Result<ExchangeRateDayDto>> SaveAsync(SaveExchangeRatesRequest request, CancellationToken cancellationToken);
    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken);
    Task<Result<FetchExchangeRatesResultDto>> FetchAsync(FetchExchangeRatesRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// 1 birim dövizin verilen gündeki (yoksa önceki son) yerel para karşılığı; yerel para biriminde 1. Hiç kur yoksa null.
    /// </summary>
    Task<decimal?> GetRateAsync(string currencyCode, DateOnly date, ExchangeRateType type, CancellationToken cancellationToken);

    /// <summary>Tutarı bir para biriminden diğerine çevirir (çapraz kur yerel para üzerinden); kur yoksa null.</summary>
    Task<decimal?> ConvertAsync(
        decimal amount, string from, string to, DateOnly date, ExchangeRateType type, CancellationToken cancellationToken);
}

/// <summary>Döviz kurları: TCMB'den çekme, elle giriş ve tarihe göre kur bulma.</summary>
public sealed class ExchangeRateService(
    IAppDbContext db, IExchangeRateProvider provider, TimeProvider timeProvider) : IExchangeRateService
{
    private static readonly Error CurrencyNotFound = new(ErrorCodes.NotFound, "Para birimi bulunamadı.", ErrorType.NotFound);

    public async Task<ExchangeRateDayDto> GetDayAsync(DateOnly date, CancellationToken cancellationToken)
    {
        var currencies = await db.Currencies.AsNoTracking()
            .Where(c => c.IsActive && c.Code != Currency.LocalCode)
            .OrderBy(c => c.Code)
            .Select(c => new { c.Id, c.Code, c.Name, c.FetchFromTcmb })
            .ToListAsync(cancellationToken);
        var ids = currencies.Select(c => c.Id).ToList();

        // Her döviz için o gün ya da öncesindeki son kur.
        var latest = await db.ExchangeRates.AsNoTracking()
            .Where(r => ids.Contains(r.CurrencyId) && r.RateDate <= date)
            .GroupBy(r => r.CurrencyId)
            .Select(g => g.OrderByDescending(r => r.RateDate).First())
            .ToListAsync(cancellationToken);

        var rows = currencies.Select(c =>
        {
            var rate = latest.SingleOrDefault(r => r.CurrencyId == c.Id);
            return new ExchangeRateRowDto(
                c.Id, c.Code, c.Name, c.FetchFromTcmb, rate?.Id, rate?.RateDate, rate?.ForexBuying, rate?.ForexSelling,
                rate?.BanknoteBuying, rate?.BanknoteSelling, rate?.Source, rate is not null && rate.RateDate != date,
                rate?.UpdatedAt ?? rate?.CreatedAt, rate?.UpdatedBy ?? rate?.CreatedBy);
        }).ToList();

        return new ExchangeRateDayDto(date, Currency.LocalCode, rows);
    }

    public async Task<Result<IReadOnlyList<ExchangeRateHistoryItemDto>>> GetHistoryAsync(
        int currencyId, DateOnly from, DateOnly to, CancellationToken cancellationToken)
    {
        if (!await db.Currencies.AnyAsync(c => c.Id == currencyId, cancellationToken))
        {
            return CurrencyNotFound;
        }

        if (to < from || to.DayNumber - from.DayNumber > 366)
        {
            return ReferenceValidation.InvalidReference("to", "Bitiş başlangıçtan önce olamaz ve aralık en fazla bir yıl olabilir.");
        }

        return await db.ExchangeRates.AsNoTracking()
            .Where(r => r.CurrencyId == currencyId && r.RateDate >= from && r.RateDate <= to)
            .OrderByDescending(r => r.RateDate)
            .Select(r => new ExchangeRateHistoryItemDto(
                r.Id, r.RateDate, r.ForexBuying, r.ForexSelling, r.BanknoteBuying, r.BanknoteSelling, r.Source))
            .ToListAsync(cancellationToken);
    }

    public async Task<Result<ExchangeRateDayDto>> SaveAsync(SaveExchangeRatesRequest request, CancellationToken cancellationToken)
    {
        var ids = request.Rates.Select(r => r.CurrencyId).ToList();
        var currencies = await db.Currencies.AsNoTracking()
            .Where(c => ids.Contains(c.Id))
            .Select(c => new { c.Id, c.Code })
            .ToListAsync(cancellationToken);
        if (currencies.Count != ids.Count)
        {
            return ReferenceValidation.InvalidReference("rates", "Listede tanımlı olmayan bir para birimi var.");
        }

        if (currencies.Any(c => c.Code == Currency.LocalCode))
        {
            return ReferenceValidation.InvalidReference("rates", $"Yerel para biriminin ({Currency.LocalCode}) kuru girilmez.");
        }

        var existing = await db.ExchangeRates
            .Where(r => ids.Contains(r.CurrencyId) && r.RateDate == request.Date)
            .ToListAsync(cancellationToken);
        foreach (var entry in request.Rates)
        {
            var rate = existing.SingleOrDefault(r => r.CurrencyId == entry.CurrencyId);
            if (rate is null)
            {
                rate = new ExchangeRate { CurrencyId = entry.CurrencyId, RateDate = request.Date };
                db.ExchangeRates.Add(rate);
            }
            else if (rate.ForexBuying == entry.ForexBuying && rate.ForexSelling == entry.ForexSelling
                && rate.BanknoteBuying == entry.BanknoteBuying && rate.BanknoteSelling == entry.BanknoteSelling)
            {
                // Değişmeyen TCMB kuru elle girilmiş sayılmaz.
                continue;
            }

            rate.ForexBuying = entry.ForexBuying;
            rate.ForexSelling = entry.ForexSelling;
            rate.BanknoteBuying = entry.BanknoteBuying;
            rate.BanknoteSelling = entry.BanknoteSelling;
            rate.Source = ExchangeRateSource.Manual;
        }

        await db.SaveChangesAsync(cancellationToken);
        return await GetDayAsync(request.Date, cancellationToken);
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var rate = await db.ExchangeRates.SingleOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (rate is null)
        {
            return new Error(ErrorCodes.NotFound, "Kur bulunamadı.", ErrorType.NotFound);
        }

        db.ExchangeRates.Remove(rate);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<FetchExchangeRatesResultDto>> FetchAsync(
        FetchExchangeRatesRequest request, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);
        if (request.Date > today)
        {
            return ReferenceValidation.InvalidReference("date", "İleri tarihli kur çekilemez.");
        }

        ProviderBulletin? bulletin;
        try
        {
            // Bugünün bülteni 15:30'dan önce yayımlanmamıştır; bugün için en son bülten alınır.
            bulletin = await provider.GetAsync(request.Date == today ? null : request.Date, cancellationToken);
        }
        catch (ExchangeRateProviderException exception)
        {
            return new Error(ErrorCodes.ExchangeRateSourceUnavailable, exception.Message, ErrorType.BusinessRule);
        }

        if (bulletin is null)
        {
            return new FetchExchangeRatesResultDto(null, 0, [], []);
        }

        var currencies = await db.Currencies.AsNoTracking()
            .Where(c => c.IsActive && c.FetchFromTcmb && c.Code != Currency.LocalCode)
            .Select(c => new { c.Id, c.Code })
            .ToListAsync(cancellationToken);
        var ids = currencies.Select(c => c.Id).ToList();
        var existing = await db.ExchangeRates
            .Where(r => ids.Contains(r.CurrencyId) && r.RateDate == bulletin.Date)
            .ToListAsync(cancellationToken);

        var imported = 0;
        var skipped = new List<string>();
        var notListed = new List<string>();
        foreach (var currency in currencies.OrderBy(c => c.Code))
        {
            var source = bulletin.Rates.FirstOrDefault(r => string.Equals(r.CurrencyCode, currency.Code, StringComparison.OrdinalIgnoreCase));
            if (source is null || source.Unit < 1 || source.ForexBuying <= 0 || source.ForexSelling <= 0)
            {
                notListed.Add(currency.Code);
                continue;
            }

            var rate = existing.SingleOrDefault(r => r.CurrencyId == currency.Id);
            if (rate is { Source: ExchangeRateSource.Manual } && !request.OverwriteManual)
            {
                skipped.Add(currency.Code);
                continue;
            }

            if (rate is null)
            {
                rate = new ExchangeRate { CurrencyId = currency.Id, RateDate = bulletin.Date };
                db.ExchangeRates.Add(rate);
            }

            // TCMB bazı dövizleri 100 birim için verir (ör. JPY); kur 1 birim için saklanır.
            rate.ForexBuying = PerUnit(source.ForexBuying, source.Unit)!.Value;
            rate.ForexSelling = PerUnit(source.ForexSelling, source.Unit)!.Value;
            rate.BanknoteBuying = PerUnit(source.BanknoteBuying, source.Unit);
            rate.BanknoteSelling = PerUnit(source.BanknoteSelling, source.Unit);
            rate.Source = ExchangeRateSource.Tcmb;
            imported++;
        }

        await db.SaveChangesAsync(cancellationToken);
        return new FetchExchangeRatesResultDto(bulletin.Date, imported, skipped, notListed);
    }

    public async Task<decimal?> GetRateAsync(
        string currencyCode, DateOnly date, ExchangeRateType type, CancellationToken cancellationToken)
    {
        var code = currencyCode.Trim().ToUpperInvariant();
        if (code == Currency.LocalCode)
        {
            return 1m;
        }

        var rate = await db.ExchangeRates.AsNoTracking()
            .Where(r => r.Currency.Code == code && r.RateDate <= date)
            .OrderByDescending(r => r.RateDate)
            .FirstOrDefaultAsync(cancellationToken);
        return rate?.Rate(type);
    }

    public async Task<decimal?> ConvertAsync(
        decimal amount, string from, string to, DateOnly date, ExchangeRateType type, CancellationToken cancellationToken)
    {
        if (string.Equals(from.Trim(), to.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return amount;
        }

        var fromRate = await GetRateAsync(from, date, type, cancellationToken);
        var toRate = await GetRateAsync(to, date, type, cancellationToken);
        return fromRate is null || toRate is null ? null : amount * fromRate.Value / toRate.Value;
    }

    private static decimal? PerUnit(decimal? value, int unit) =>
        value is null or <= 0 ? null : decimal.Round(value.Value / unit, 6);
}
