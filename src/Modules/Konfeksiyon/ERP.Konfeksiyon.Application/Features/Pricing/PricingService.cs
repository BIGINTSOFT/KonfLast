using ERP.Konfeksiyon.Application.Abstractions;
using ERP.Core.Application.Features.Finance;
using ERP.Core.Domain.Entities;
using ERP.Core.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ERP.Konfeksiyon.Application.Features.Pricing;

public enum PriceSource
{
    /// <summary>Müşteriye özel fiyat listesi.</summary>
    CustomerPriceList,

    /// <summary>Genel fiyat listesi.</summary>
    GeneralPriceList,

    /// <summary>Ürün kartındaki liste fiyatı.</summary>
    ProductListPrice,
}

public sealed record PriceQuoteItemRequest(int ProductId, int ColorId, int SizeId);

/// <param name="Currency">Siparişin para birimi; fiyatlar bu para birimine çevrilir. Boşsa TRY.</param>
/// <param name="Date">Fiyat ve iskontonun geçerli olacağı gün (sipariş tarihi).</param>
public sealed record PriceQuoteRequest(int CustomerId, string? Currency, DateOnly Date, IReadOnlyList<PriceQuoteItemRequest> Items);

/// <param name="ListPrice">Siparişin para biriminde iskontosuz fiyat; fiyat bulunamazsa null.</param>
/// <param name="DiscountPercent">Uygulanan müşteri iskontosu (yüzde); yoksa null.</param>
/// <param name="UnitPrice">İskonto sonrası birim fiyat (4 ondalık).</param>
/// <param name="SourceName">Fiyatın geldiği liste (kod — ad) ya da "Ürün liste fiyatı".</param>
/// <param name="SourcePrice">Kaynaktaki fiyat, kaynağın para biriminde.</param>
/// <param name="ExchangeRate">Para birimi çevrildiyse kullanılan çapraz kur (1 kaynak = X sipariş para birimi).</param>
/// <param name="Problem">Fiyat bulundu ama kullanılamıyorsa nedeni (ör. kur yok).</param>
public sealed record PriceQuoteDto(
    int ProductId,
    int ColorId,
    int SizeId,
    decimal? ListPrice,
    decimal? DiscountPercent,
    decimal? UnitPrice,
    PriceSource? Source,
    string? SourceName,
    decimal? SourcePrice,
    string? SourceCurrency,
    decimal? ExchangeRate,
    string? Problem);

public sealed class PriceQuoteRequestValidator : AbstractValidator<PriceQuoteRequest>
{
    public const int MaxItems = 2000;

    public PriceQuoteRequestValidator()
    {
        RuleFor(x => x.CustomerId).GreaterThan(0).WithMessage("Müşteri seçilmelidir.");
        RuleFor(x => x.Date).NotEqual(default(DateOnly)).WithMessage("Tarih zorunludur.");
        RuleFor(x => x.Items).NotNull().Must(items => items.Count <= MaxItems)
            .WithMessage($"Tek seferde en fazla {MaxItems} kalem sorulabilir.");
    }
}

public interface IPricingService
{
    /// <summary>
    /// Müşteriye, güne ve para birimine göre önerilen fiyatlar. Sıra: müşteriye özel fiyat listesi, genel fiyat
    /// listesi, ürünün liste fiyatı. Fiyat sipariş para birimine kurla çevrilir, sonra müşteri iskontosu uygulanır.
    /// </summary>
    Task<IReadOnlyList<PriceQuoteDto>> QuoteAsync(PriceQuoteRequest request, CancellationToken cancellationToken);
}

public sealed class PricingService(IKonfeksiyonDbContext db, IExchangeRateService exchangeRates) : IPricingService
{
    /// <summary>Satış fiyatı çevrilirken kullanılan kur: TCMB döviz alış (faturada kullanılan kurla aynı).</summary>
    public const ExchangeRateType ConversionRate = ExchangeRateType.ForexBuying;

    public async Task<IReadOnlyList<PriceQuoteDto>> QuoteAsync(PriceQuoteRequest request, CancellationToken cancellationToken)
    {
        var currency = string.IsNullOrWhiteSpace(request.Currency) ? Currency.LocalCode : request.Currency.Trim().ToUpperInvariant();
        var date = request.Date;
        var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();

        // O gün geçerli listeler: müşteriye özel olanlar önce, aynı türde başlangıcı en yeni olan önce.
        var lists = await db.PriceLists.AsNoTracking()
            .Where(p => p.IsActive && p.ValidFrom <= date && (p.ValidTo == null || p.ValidTo >= date)
                && (p.CustomerId == null || p.CustomerId == request.CustomerId))
            .OrderByDescending(p => p.CustomerId != null).ThenByDescending(p => p.ValidFrom).ThenByDescending(p => p.Id)
            .Select(p => new { p.Id, p.Code, p.Name, p.Currency, IsCustomer = p.CustomerId != null })
            .ToListAsync(cancellationToken);
        var listIds = lists.Select(l => l.Id).ToList();
        var items = await db.PriceListItems.AsNoTracking()
            .Where(i => listIds.Contains(i.PriceListId) && productIds.Contains(i.ProductId))
            .ToListAsync(cancellationToken);
        var products = await db.Products.AsNoTracking().IgnoreQueryFilters()
            .Where(p => productIds.Contains(p.Id))
            .Select(p => new { p.Id, p.ListPrice, p.Currency })
            .ToDictionaryAsync(p => p.Id, cancellationToken);
        var discounts = await db.CustomerDiscounts.AsNoTracking()
            .Where(d => d.CustomerId == request.CustomerId && d.ValidFrom <= date && (d.ValidTo == null || d.ValidTo >= date)
                && (d.ProductId == null || productIds.Contains(d.ProductId.Value)))
            .ToListAsync(cancellationToken);
        var general = discounts.FirstOrDefault(d => d.ProductId is null)?.DiscountPercent;

        var rates = new Dictionary<string, decimal?>();
        async Task<decimal?> RateAsync(string from)
        {
            if (!rates.TryGetValue(from, out var rate))
            {
                rate = await exchangeRates.ConvertAsync(1m, from, currency, date, ConversionRate, cancellationToken);
                rates[from] = rate;
            }

            return rate;
        }

        var result = new List<PriceQuoteDto>(request.Items.Count);
        foreach (var item in request.Items)
        {
            var discount = discounts.FirstOrDefault(d => d.ProductId == item.ProductId)?.DiscountPercent ?? general;

            // Listelerin öncelik sırasıyla, her listede en özel satır (renk + beden > beden > renk > genel).
            (decimal Price, string Currency, PriceSource Source, string Name)? found = null;
            foreach (var list in lists)
            {
                var match = items
                    .Where(i => i.PriceListId == list.Id && i.ProductId == item.ProductId
                        && (i.ColorId == null || i.ColorId == item.ColorId) && (i.SizeId == null || i.SizeId == item.SizeId))
                    .OrderByDescending(i => i.Specificity)
                    .FirstOrDefault();
                if (match is not null)
                {
                    found = (match.UnitPrice, list.Currency,
                        list.IsCustomer ? PriceSource.CustomerPriceList : PriceSource.GeneralPriceList, $"{list.Code} — {list.Name}");
                    break;
                }
            }

            if (found is null && products.TryGetValue(item.ProductId, out var product) && product.ListPrice is { } listPrice)
            {
                found = (listPrice, product.Currency, PriceSource.ProductListPrice, "Ürün liste fiyatı");
            }

            if (found is not { } source)
            {
                result.Add(new PriceQuoteDto(item.ProductId, item.ColorId, item.SizeId, null, discount, null, null, null, null, null, null, null));
                continue;
            }

            var rate = source.Currency == currency ? 1m : await RateAsync(source.Currency);
            if (rate is null)
            {
                result.Add(new PriceQuoteDto(
                    item.ProductId, item.ColorId, item.SizeId, null, discount, null, source.Source, source.Name, source.Price,
                    source.Currency, null, $"{source.Currency} → {currency} kuru bulunamadı; Döviz Kurları ekranından girin."));
                continue;
            }

            var gross = decimal.Round(source.Price * rate.Value, 4);
            var net = discount is { } percent ? decimal.Round(gross * (100 - percent) / 100, 4) : gross;
            result.Add(new PriceQuoteDto(
                item.ProductId, item.ColorId, item.SizeId, gross, discount, net, source.Source, source.Name, source.Price,
                source.Currency, source.Currency == currency ? null : decimal.Round(rate.Value, 6), null));
        }

        return result;
    }
}
