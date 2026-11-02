using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace ERP.Core.Application.Features.Integrations;

/// <summary>Entegrasyon verisinin JSON biçimi: camelCase alanlar, BÜYÜK_SNAKE enum değerleri.</summary>
public static class IntegrationJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseUpper) },
    };
}

/// <summary>Bir kanala gönderilen olay: kimlik, tür, ait olduğu kayıt ve veri.</summary>
public sealed record IntegrationEnvelope(
    Guid EventId,
    long Sequence,
    string EventType,
    DateTime OccurredAt,
    string EntityName,
    string EntityId,
    string? Reference,
    string PayloadJson,
    long DeliveryId,
    int Attempt)
{
    /// <summary>
    /// Olayın dış sistemlere giden gövdesi. Aynı olay her denemede ve her kanalda aynı gövdeyle gider;
    /// teslim / deneme bilgisi gövdede değil, kanalın kendi üst bilgisinde taşınır.
    /// </summary>
    public string ToJson() => JsonSerializer.Serialize(
        new
        {
            id = EventId,
            sequence = Sequence,
            type = EventType,
            occurredAt = OccurredAt,
            entity = new { name = EntityName, id = EntityId, reference = Reference },
            data = JsonSerializer.Deserialize<JsonElement>(PayloadJson),
        },
        IntegrationJson.Options);
}

public sealed record IntegrationSendResult(bool Succeeded, string? Error)
{
    public static readonly IntegrationSendResult Success = new(true, null);

    public static IntegrationSendResult Failure(string error) => new(false, error);
}

/// <summary>
/// Olayların gönderildiği hedef: bir webhook adresi ya da ileride bir e-İrsaliye / e-Fatura entegratörü,
/// SAP, muhasebe, kargo ya da banka bağlantısı. Yeni bir dış sistem, bu arayüzün bir uygulaması olarak
/// eklenir; iş kuralları ve olayı doğuran servisler değişmez.
/// </summary>
public interface IIntegrationChannel
{
    /// <summary>Kalıcı anahtar (teslim kayıtlarında saklanır), örn. <c>webhook:muhasebe</c>.</summary>
    string Key { get; }

    /// <summary>Ekranda gösterilen ad.</summary>
    string Name { get; }

    /// <summary>Ekranda gösterilen hedef (adres vb.); gizli bilgi içermez.</summary>
    string? Target { get; }

    /// <summary>Kanalın istediği olay türleri; boşsa tüm olaylar.</summary>
    IReadOnlyCollection<string> EventTypes { get; }

    /// <summary>
    /// Olayı gönderir. Hedefe ulaşılamaması ya da hedefin reddetmesi hata fırlatılmadan başarısız sonuç olarak
    /// döndürülür. Aynı olay birden fazla kez gönderilebilir; hedef <see cref="IntegrationEnvelope.EventId"/> ile ayırt eder.
    /// </summary>
    Task<IntegrationSendResult> SendAsync(IntegrationEnvelope envelope, CancellationToken cancellationToken);
}

/// <summary>Yapılandırmadan birden fazla kanal üreten kaynak (ör. tanımlı her webhook için bir kanal).</summary>
public interface IIntegrationChannelProvider
{
    IEnumerable<IIntegrationChannel> CreateChannels();
}

public interface IIntegrationChannelRegistry
{
    IReadOnlyList<IIntegrationChannel> Channels { get; }
    IIntegrationChannel? Find(string key);
}

public static class IntegrationChannelExtensions
{
    public static bool Handles(this IIntegrationChannel channel, string eventType) =>
        channel.EventTypes.Count == 0 || channel.EventTypes.Contains(eventType);
}

public sealed class IntegrationChannelRegistry : IIntegrationChannelRegistry
{
    private readonly Dictionary<string, IIntegrationChannel> _byKey = new(StringComparer.Ordinal);

    public IntegrationChannelRegistry(
        IEnumerable<IIntegrationChannel> channels,
        IEnumerable<IIntegrationChannelProvider> providers,
        ILogger<IntegrationChannelRegistry> logger)
    {
        var all = new List<IIntegrationChannel>();
        foreach (var channel in channels.Concat(providers.SelectMany(p => p.CreateChannels())))
        {
            // Olay yazımı (ve dolayısıyla iş işlemleri) hatalı kanal tanımı yüzünden durmamalı: ilki geçerlidir.
            if (_byKey.TryAdd(channel.Key, channel))
            {
                all.Add(channel);
            }
            else
            {
                logger.LogError("'{Channel}' anahtarlı birden fazla entegrasyon kanalı tanımlı; ilki kullanılıyor.", channel.Key);
            }
        }

        Channels = all;
    }

    public IReadOnlyList<IIntegrationChannel> Channels { get; }

    public IIntegrationChannel? Find(string key) => _byKey.GetValueOrDefault(key);
}
