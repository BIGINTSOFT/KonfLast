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

/// <summary>Bir yayında birleştirilen değişiklikler: web için alanlar, tabletler için makine / tablet hedefleri.</summary>
public sealed record LiveChangeBatch(IReadOnlyList<LiveTopic> Topics, IReadOnlyList<LiveTarget> Targets);

/// <summary>
/// İşlemlerin bıraktığı bildirimleri toplar. Yazmak bloklamaz ve başarısız olmaz; yayın ayrı bir
/// arka plan işinde yapılır, böylece SignalR'daki bir sorun üretim işlemini yavaşlatamaz ya da bozamaz.
/// </summary>
public sealed class LiveUpdateQueue : ILiveNotifier
{
    private readonly Channel<(IReadOnlyCollection<LiveTopic> Topics, IReadOnlyCollection<LiveTarget> Targets)> _channel =
        Channel.CreateUnbounded<(IReadOnlyCollection<LiveTopic>, IReadOnlyCollection<LiveTarget>)>(
            new UnboundedChannelOptions { SingleReader = true });

    public void Publish(IReadOnlyCollection<LiveTopic> topics, IReadOnlyCollection<LiveTarget>? targets = null)
    {
        if (topics.Count > 0 || targets is { Count: > 0 })
        {
            _channel.Writer.TryWrite((topics, targets ?? []));
        }
    }

    /// <summary>En az bir bildirim gelene kadar bekler, sonra birikenlerin hepsini (tekrarsız) döndürür.</summary>
    public async Task<LiveChangeBatch> ReadBatchAsync(CancellationToken cancellationToken)
    {
        await _channel.Reader.WaitToReadAsync(cancellationToken);

        var topics = new SortedSet<LiveTopic>();
        var targets = new HashSet<LiveTarget>();
        while (_channel.Reader.TryRead(out var change))
        {
            topics.UnionWith(change.Topics);
            targets.UnionWith(change.Targets);
        }

        return new LiveChangeBatch([.. topics], [.. targets]);
    }
}

/// <summary>
/// Biriken değişiklikleri yayınlar: alanları bağlı tüm web istemcilerine, makine / tablet hedeflerini yalnızca
/// ilgili tabletlere. Makineye bağlı tabletler yayın anında bulunur (tablet başka makineye taşınmış olabilir).
/// </summary>
public sealed class LiveUpdateBroadcaster(
    LiveUpdateQueue queue,
    IHubContext<ProductionHub, IProductionHubClient> hub,
    IHubContext<TabletHub, ITabletHubClient> tabletHub,
    IServiceScopeFactory scopeFactory,
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
                var batch = await queue.ReadBatchAsync(stoppingToken);
                var now = timeProvider.GetUtcNow().UtcDateTime;
                if (batch.Topics.Count > 0)
                {
                    await hub.Clients.All.Changed(new LiveChangeMessage(batch.Topics, now));
                }

                await NotifyTabletsAsync(batch.Targets, now, stoppingToken);
                await Task.Delay(interval, timeProvider, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                // Yayın kaçsa da veri kaybolmaz: ekranlar ve tabletler düzenli yoklamayla kendini yeniler.
                logger.LogWarning(exception, "Canlı üretim bildirimi yayınlanamadı.");
            }
        }
    }

    private async Task NotifyTabletsAsync(IReadOnlyList<LiveTarget> targets, DateTime now, CancellationToken cancellationToken)
    {
        if (targets.Count == 0)
        {
            return;
        }

        var groups = targets
            .Where(t => t.Kind == LiveTargetKind.Tablet)
            .Select(t => TabletHub.GroupOf(t.Module, t.Id))
            .ToHashSet();

        var machinesByModule = targets
            .Where(t => t.Kind == LiveTargetKind.Machine)
            .GroupBy(t => t.Module, t => t.Id);
        if (machinesByModule.Any())
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var directories = scope.ServiceProvider.GetServices<ILiveTabletDirectory>().ToDictionary(d => d.Module);
            foreach (var module in machinesByModule)
            {
                if (directories.TryGetValue(module.Key, out var directory))
                {
                    var tablets = await directory.TabletsOfMachinesAsync([.. module.Distinct()], cancellationToken);
                    groups.UnionWith(tablets.Select(id => TabletHub.GroupOf(module.Key, id)));
                }
            }
        }

        if (groups.Count > 0)
        {
            await tabletHub.Clients.Groups([.. groups]).Changed(new TabletChangeMessage(now));
        }
    }
}
