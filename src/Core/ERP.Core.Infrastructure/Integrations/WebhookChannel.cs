using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using ERP.Core.Application.Features.Integrations;
using Microsoft.Extensions.Options;

namespace ERP.Core.Infrastructure.Integrations;

/// <summary>
/// Webhook isteklerinin imzası. Alıcı aynı hesabı kendi tarafında yapıp karşılaştırarak isteğin gerçekten bu
/// sistemden geldiğini ve gövdenin yolda değişmediğini doğrular; zaman damgası eski isteklerin yeniden
/// oynatılmasını (replay) sınırlamak içindir.
/// </summary>
public static class WebhookSignature
{
    public const string Prefix = "sha256=";

    /// <summary><c>sha256=</c> + HMAC-SHA256(secret, "{timestamp}.{body}") — küçük harf onaltılık.</summary>
    public static string Compute(string secret, long unixTimeSeconds, string body)
    {
        var data = Encoding.UTF8.GetBytes($"{unixTimeSeconds.ToString(CultureInfo.InvariantCulture)}.{body}");
        var hash = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), data);
        return Prefix + Convert.ToHexStringLower(hash);
    }
}

/// <summary>
/// Olayı yapılandırılmış adrese JSON olarak POST eder. 2xx yanıt başarıdır; diğer her şey (hata kodu,
/// bağlantı hatası, zaman aşımı) başarısız teslimdir ve yeniden denenir.
/// </summary>
public sealed class WebhookChannel(WebhookOptions options, HttpMessageInvoker http, TimeProvider timeProvider)
    : IIntegrationChannel
{
    public const string KeyPrefix = "webhook:";
    public const string EventHeader = "X-ERP-Event";
    public const string EventIdHeader = "X-ERP-Event-Id";
    public const string DeliveryHeader = "X-ERP-Delivery";
    public const string AttemptHeader = "X-ERP-Attempt";
    public const string TimestampHeader = "X-ERP-Timestamp";
    public const string SignatureHeader = "X-ERP-Signature";

    private const int MaxErrorBodyLength = 300;

    private readonly Uri _url = new(options.Url, UriKind.Absolute);

    public string Key { get; } = KeyPrefix + options.Name;
    public string Name { get; } = $"Webhook · {options.Name}";

    /// <summary>Sorgu parametreleri gizli anahtar taşıyabilir; ekranda yalnızca adresin yolu gösterilir.</summary>
    public string? Target => _url.GetLeftPart(UriPartial.Path);

    public IReadOnlyCollection<string> EventTypes { get; } = options.EventTypes.ToHashSet(StringComparer.Ordinal);

    public async Task<IntegrationSendResult> SendAsync(IntegrationEnvelope envelope, CancellationToken cancellationToken)
    {
        var body = envelope.ToJson();
        var timestamp = timeProvider.GetUtcNow().ToUnixTimeSeconds();

        using var request = new HttpRequestMessage(HttpMethod.Post, _url)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
        request.Headers.Add(EventHeader, envelope.EventType);
        request.Headers.Add(EventIdHeader, envelope.EventId.ToString());
        request.Headers.Add(DeliveryHeader, envelope.DeliveryId.ToString(CultureInfo.InvariantCulture));
        request.Headers.Add(AttemptHeader, envelope.Attempt.ToString(CultureInfo.InvariantCulture));
        request.Headers.Add(TimestampHeader, timestamp.ToString(CultureInfo.InvariantCulture));
        request.Headers.Add(SignatureHeader, WebhookSignature.Compute(options.Secret, timestamp, body));

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(options.TimeoutSeconds));

        try
        {
            using var response = await http.SendAsync(request, timeout.Token);
            if (response.IsSuccessStatusCode)
            {
                return IntegrationSendResult.Success;
            }

            var responseBody = await response.Content.ReadAsStringAsync(timeout.Token);
            var detail = responseBody.Length > MaxErrorBodyLength ? responseBody[..MaxErrorBodyLength] + "…" : responseBody;
            return IntegrationSendResult.Failure(
                $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}".TrimEnd()
                + (string.IsNullOrWhiteSpace(detail) ? string.Empty : $": {detail.Trim()}"));
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return IntegrationSendResult.Failure($"Zaman aşımı: hedef {options.TimeoutSeconds} sn içinde yanıt vermedi.");
        }
        catch (HttpRequestException exception)
        {
            return IntegrationSendResult.Failure($"Bağlantı hatası: {exception.Message}");
        }
    }
}

/// <summary>Yapılandırmadaki açık her webhook için bir kanal üretir.</summary>
public sealed class WebhookChannelProvider(IOptions<IntegrationOptions> options, TimeProvider timeProvider)
    : IIntegrationChannelProvider, IDisposable
{
    // Tek istemci, bağlantıları paylaşır; bağlantı ömrü sınırlı olduğu için DNS değişiklikleri de yakalanır.
    // Yönlendirme izlenmez: imzalı istek, yapılandırılan adresten başka bir yere gitmemeli.
    private readonly HttpMessageInvoker _http = new(new SocketsHttpHandler
    {
        AllowAutoRedirect = false,
        PooledConnectionLifetime = TimeSpan.FromMinutes(5),
        ConnectTimeout = TimeSpan.FromSeconds(10),
    });

    public IEnumerable<IIntegrationChannel> CreateChannels() =>
        options.Value.Webhooks.Where(w => w.Enabled).Select(w => new WebhookChannel(w, _http, timeProvider));

    public void Dispose() => _http.Dispose();
}
