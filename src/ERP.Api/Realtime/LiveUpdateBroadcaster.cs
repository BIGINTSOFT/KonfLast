using System.ComponentModel.DataAnnotations;
using System.Threading.Channels;
using ERP.Core.Application.Abstractions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace ERP.Api.Realtime;

public sealed class LiveUpdateOptions
{
    public const string SectionName = "LiveUpdates";

    /// <summary>
    /// İki yayın arasındaki en kısa süre. Yoğun üretimde (saniyede onlarca bildirim) değişiklikler bu
    /// aralıkta tek mesajda birleştirilir; ekranlar gereğinden sık yenilenmez.
    /// </summary>
    [Range(50, 60_000)]
    public int MinIntervalMilliseconds { get; set; } = 500;
}

/// <summary>
/// İşlemlerin bıraktığı bildirimleri toplar. Yazmak bloklamaz ve başarısız olmaz; yayın ayrı bir
/// arka plan işinde yapılır, böylece SignalR'daki bir sorun üretim işlemini yavaşlatamaz ya da bozamaz.
/// </summary>
public sealed class LiveUpdateQueue : ILiveNotifier
{
    private readonly Channel<LiveTopic> _channel = Channel.CreateUnbounded<LiveTopic>(
        new UnboundedChannelOptions { SingleReader = true });

    public void Publish(IReadOnlyCollection<LiveTopic> topics)
    {
        foreach (var topic in topics)
        {
            _channel.Writer.TryWrite(topic);
        }
    }

    /// <summary>En az bir bildirim gelene kadar bekler, sonra birikenlerin hepsini (tekrarsız) döndürür.</summary>
    public async Task<IReadOnlyList<LiveTopic>> ReadBatchAsync(CancellationToken cancellationToken)
    {
        await _channel.Reader.WaitToReadAsync(cancellationToken);

        var topics = new SortedSet<LiveTopic>();
        while (_channel.Reader.TryRead(out var topic))
        {
            topics.Add(topic);
        }

        return [.. topics];
    }
}

/// <summary>Biriken değişiklikleri bağlı tüm istemcilere yayınlar.</summary>
public sealed class LiveUpdateBroadcaster(
    LiveUpdateQueue queue,
    IHubContext<ProductionHub, IProductionHubClient> hub,
    IOptions<LiveUpdateOptions> options,
    TimeProvider timeProvider,
    ILogger<LiveUpdateBroadcaster> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMilliseconds(options.Value.MinIntervalMilliseconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var topics = await queue.ReadBatchAsync(stoppingToken);
                await hub.Clients.All.Changed(new LiveChangeMessage(topics, timeProvider.GetUtcNow().UtcDateTime));
                await Task.Delay(interval, timeProvider, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                // Yayın kaçsa da veri kaybolmaz: ekranlar düzenli yoklamayla kendini yeniler.
                logger.LogWarning(exception, "Canlı üretim bildirimi yayınlanamadı.");
            }
        }
    }
}
