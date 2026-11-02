using System.Text.Json;
using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Core.Domain.Entities;
using ERP.Core.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ERP.Core.Application.Features.Integrations;

public sealed record IntegrationMessageListItemDto(
    long Id,
    Guid EventId,
    string EventType,
    string EntityName,
    string EntityId,
    string? Reference,
    DateTime OccurredAt,
    string CreatedBy,
    int PendingCount,
    int SucceededCount,
    int FailedCount);

public sealed record IntegrationDeliveryDto(
    long Id,
    string Channel,
    string ChannelName,
    IntegrationDeliveryStatus Status,
    int AttemptCount,
    DateTime? NextAttemptAt,
    DateTime? LastAttemptAt,
    string? LastError,
    DateTime? DeliveredAt,
    bool CanRetry);

/// <summary><c>Data</c>, olayın verisidir; kanallara giden gövdenin <c>data</c> alanıyla aynıdır.</summary>
public sealed record IntegrationMessageDto(
    long Id,
    Guid EventId,
    string EventType,
    string EntityName,
    string EntityId,
    string? Reference,
    DateTime OccurredAt,
    string CreatedBy,
    JsonElement Data,
    IReadOnlyList<IntegrationDeliveryDto> Deliveries);

public sealed record IntegrationChannelDto(
    string Key,
    string Name,
    string? Target,
    IReadOnlyCollection<string> EventTypes,
    bool IsConfigured,
    int PendingCount,
    int SucceededCount,
    int FailedCount);

public sealed record IntegrationOverviewDto(
    bool DispatchEnabled,
    int MaxAttempts,
    IReadOnlyList<IntegrationChannelDto> Channels,
    IReadOnlyList<IntegrationEventTypeDto> EventTypes);

public sealed class IntegrationMessageQuery : PageRequest
{
    /// <summary>Numara (sevkiyat no, sipariş no) ya da kayıt kimliği.</summary>
    public string? Search { get; set; }
    public string? EventType { get; set; }

    /// <summary>Yalnızca bu kanala teslimi olan olaylar.</summary>
    public string? Channel { get; set; }

    /// <summary>Yalnızca bu durumda teslimi olan olaylar (ör. FAILED: gönderilemeyenler).</summary>
    public IntegrationDeliveryStatus? DeliveryStatus { get; set; }

    /// <summary>
    /// Verilirse yalnızca bu sıra numarasından SONRAKİ olaylar, eskiden yeniye döner: dış sistem, son okuduğu
    /// olayın numarasını vererek olayları kaldığı yerden çekebilir. Verilmezse liste yeniden eskiyedir.
    /// </summary>
    public long? AfterId { get; set; }
}

public interface IIntegrationService
{
    Task<PagedResult<IntegrationMessageListItemDto>> GetMessagesAsync(
        IntegrationMessageQuery query, CancellationToken cancellationToken);

    Task<Result<IntegrationMessageDto>> GetMessageAsync(long id, CancellationToken cancellationToken);

    /// <summary>Tanımlı kanallar, teslim sayıları ve olay türleri.</summary>
    Task<IntegrationOverviewDto> GetOverviewAsync(CancellationToken cancellationToken);

    /// <summary>Bitmiş (başarılı ya da bırakılmış) bir teslimi yeniden kuyruğa alır.</summary>
    Task<Result<IntegrationMessageDto>> RetryDeliveryAsync(long deliveryId, CancellationToken cancellationToken);
}

public sealed class IntegrationService(
    IAppDbContext db,
    IIntegrationChannelRegistry registry,
    TimeProvider timeProvider,
    IOptions<IntegrationOptions> options,
    IEnumerable<IIntegrationEventCatalog> eventCatalogs) : IIntegrationService
{
    private static readonly Error MessageNotFound = new(ErrorCodes.NotFound, "Entegrasyon olayı bulunamadı.", ErrorType.NotFound);
    private static readonly Error DeliveryNotFound = new(ErrorCodes.NotFound, "Teslim bulunamadı.", ErrorType.NotFound);

    public Task<PagedResult<IntegrationMessageListItemDto>> GetMessagesAsync(
        IntegrationMessageQuery query, CancellationToken cancellationToken)
    {
        var messages = db.IntegrationMessages.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            messages = messages.Where(m => m.Reference!.Contains(search) || m.EntityId == search);
        }

        if (!string.IsNullOrWhiteSpace(query.EventType))
        {
            messages = messages.Where(m => m.EventType == query.EventType);
        }

        if (!string.IsNullOrWhiteSpace(query.Channel))
        {
            messages = messages.Where(m => m.Deliveries.Any(d => d.Channel == query.Channel));
        }

        if (query.DeliveryStatus is { } status)
        {
            messages = messages.Where(m => m.Deliveries.Any(d => d.Status == status));
        }

        if (query.AfterId is { } afterId)
        {
            messages = messages.Where(m => m.Id > afterId);
        }

        var ordered = query.AfterId is null ? messages.OrderByDescending(m => m.Id) : messages.OrderBy(m => m.Id);

        return ordered
            .Select(m => new IntegrationMessageListItemDto(
                m.Id,
                m.EventId,
                m.EventType,
                m.EntityName,
                m.EntityId,
                m.Reference,
                m.OccurredAt,
                m.CreatedBy,
                m.Deliveries.Count(d => d.Status == IntegrationDeliveryStatus.Pending),
                m.Deliveries.Count(d => d.Status == IntegrationDeliveryStatus.Succeeded),
                m.Deliveries.Count(d => d.Status == IntegrationDeliveryStatus.Failed)))
            .ToPagedResultAsync(query, cancellationToken);
    }

    public async Task<Result<IntegrationMessageDto>> GetMessageAsync(long id, CancellationToken cancellationToken)
    {
        var message = await db.IntegrationMessages
            .AsNoTracking()
            .Include(m => m.Deliveries)
            .SingleOrDefaultAsync(m => m.Id == id, cancellationToken);

        return message is null ? MessageNotFound : ToDto(message);
    }

    public async Task<IntegrationOverviewDto> GetOverviewAsync(CancellationToken cancellationToken)
    {
        var counts = await db.IntegrationDeliveries
            .AsNoTracking()
            .GroupBy(d => new { d.Channel, d.Status })
            .Select(g => new { g.Key.Channel, g.Key.Status, Count = g.Count() })
            .ToListAsync(cancellationToken);

        int CountOf(string channel, IntegrationDeliveryStatus status) =>
            counts.Where(c => c.Channel == channel && c.Status == status).Sum(c => c.Count);

        IntegrationChannelDto Describe(string key, IIntegrationChannel? channel) => new(
            key,
            channel?.Name ?? key,
            channel?.Target,
            channel?.EventTypes ?? [],
            channel is not null,
            CountOf(key, IntegrationDeliveryStatus.Pending),
            CountOf(key, IntegrationDeliveryStatus.Succeeded),
            CountOf(key, IntegrationDeliveryStatus.Failed));

        // Yapılandırmadan kaldırılmış ama teslim kaydı duran kanallar da görünür (bekleyen teslimleri fark edilsin).
        var removed = counts
            .Select(c => c.Channel)
            .Distinct()
            .Where(key => registry.Find(key) is null)
            .OrderBy(key => key, StringComparer.Ordinal);

        return new IntegrationOverviewDto(
            options.Value.DispatchEnabled,
            options.Value.MaxAttempts,
            [.. registry.Channels.Select(c => Describe(c.Key, c)), .. removed.Select(key => Describe(key, null))],
            [.. eventCatalogs.SelectMany(c => c.EventTypes)]);
    }

    public async Task<Result<IntegrationMessageDto>> RetryDeliveryAsync(long deliveryId, CancellationToken cancellationToken)
    {
        var delivery = await db.IntegrationDeliveries.SingleOrDefaultAsync(d => d.Id == deliveryId, cancellationToken);
        if (delivery is null)
        {
            return DeliveryNotFound;
        }

        if (delivery.Status == IntegrationDeliveryStatus.Pending)
        {
            return new Error(
                ErrorCodes.IntegrationDeliveryPending, "Teslim zaten kuyrukta; sıradaki denemede gönderilecek.",
                ErrorType.Conflict);
        }

        delivery.Requeue(timeProvider.GetUtcNow().UtcDateTime);
        await db.SaveChangesAsync(cancellationToken);

        return await GetMessageAsync(delivery.IntegrationMessageId, cancellationToken);
    }

    private IntegrationMessageDto ToDto(IntegrationMessage message) => new(
        message.Id,
        message.EventId,
        message.EventType,
        message.EntityName,
        message.EntityId,
        message.Reference,
        message.OccurredAt,
        message.CreatedBy,
        JsonSerializer.Deserialize<JsonElement>(message.Payload),
        message.Deliveries
            .OrderBy(d => d.Id)
            .Select(d => new IntegrationDeliveryDto(
                d.Id,
                d.Channel,
                registry.Find(d.Channel)?.Name ?? d.Channel,
                d.Status,
                d.AttemptCount,
                d.Status == IntegrationDeliveryStatus.Pending ? d.NextAttemptAt : null,
                d.LastAttemptAt,
                d.LastError,
                d.DeliveredAt,
                d.Status != IntegrationDeliveryStatus.Pending))
            .ToList());
}
