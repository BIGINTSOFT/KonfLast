using System.Globalization;
using System.Net;
using System.Xml.Linq;
using ERP.Core.Application.Features.Finance;
using Microsoft.Extensions.Options;

namespace ERP.Core.Infrastructure.Finance;

/// <summary>
/// TCMB gösterge niteliğindeki döviz kurları: <c>{taban}/today.xml</c> en son bülten,
/// <c>{taban}/yyyyMM/ddMMyyyy.xml</c> belirli bir günün bülteni. O gün bülten yoksa TCMB 404 döner.
/// </summary>
public sealed class TcmbExchangeRateProvider(HttpMessageInvoker http, IOptions<ExchangeRateOptions> options)
    : IExchangeRateProvider
{
    public async Task<ProviderBulletin?> GetAsync(DateOnly? date, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var baseUrl = settings.TcmbBaseUrl.EndsWith('/') ? settings.TcmbBaseUrl : settings.TcmbBaseUrl + "/";
        var path = date is { } day
            ? $"{day:yyyyMM}/{day:ddMMyyyy}.xml"
            : "today.xml";

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(settings.TimeoutSeconds));

        string xml;
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(new Uri(baseUrl), path));
            using var response = await http.SendAsync(request, timeout.Token);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new ExchangeRateProviderException($"TCMB kur sayfası hata döndü ({(int)response.StatusCode}).");
            }

            xml = await response.Content.ReadAsStringAsync(timeout.Token);
        }
        catch (Exception exception) when (exception is HttpRequestException
            || (exception is OperationCanceledException && !cancellationToken.IsCancellationRequested))
        {
            throw new ExchangeRateProviderException(
                "TCMB kur sayfasına ulaşılamadı. Ağ / vekil sunucu ayarlarını kontrol edin ya da kurları elle girin.", exception);
        }

        return Parse(xml);
    }

    /// <summary>TCMB bülten XML'ini okur (sayılar nokta ondalık ayraçlı, boş değer olabilir).</summary>
    public static ProviderBulletin Parse(string xml)
    {
        XDocument document;
        try
        {
            document = XDocument.Parse(xml);
        }
        catch (System.Xml.XmlException exception)
        {
            throw new ExchangeRateProviderException("TCMB kur bülteni okunamadı.", exception);
        }

        var root = document.Root ?? throw new ExchangeRateProviderException("TCMB kur bülteni boş.");
        // Date="10/21/2026" (ay/gün/yıl); yoksa Tarih="21.10.2026".
        var date = DateOnly.TryParseExact((string?)root.Attribute("Date"), "MM/dd/yyyy", CultureInfo.InvariantCulture,
            DateTimeStyles.None, out var parsed)
            ? parsed
            : DateOnly.ParseExact((string?)root.Attribute("Tarih") ?? throw new ExchangeRateProviderException("Bülten tarihi yok."),
                "dd.MM.yyyy", CultureInfo.InvariantCulture);

        var rates = root.Elements("Currency")
            .Select(element => new ProviderRate(
                ((string?)element.Attribute("CurrencyCode") ?? string.Empty).Trim().ToUpperInvariant(),
                int.TryParse((string?)element.Element("Unit"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var unit) ? unit : 1,
                Number(element, "ForexBuying") ?? 0,
                Number(element, "ForexSelling") ?? 0,
                Number(element, "BanknoteBuying"),
                Number(element, "BanknoteSelling")))
            .Where(rate => rate.CurrencyCode.Length == 3)
            .ToList();

        return new ProviderBulletin(date, rates);
    }

    private static decimal? Number(XElement element, string name) =>
        decimal.TryParse((string?)element.Element(name), NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;
}
