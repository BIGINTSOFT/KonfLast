using System.Globalization;
using System.Text.Json;
using ERP.Core.Application.Abstractions;
using ERP.Core.Domain.Entities;
using ERP.Core.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ERP.Core.Application.Features.Integrations;

/// <summary>
/// İş olaylarını dış sistemlere güvenilir biçimde aktarmak için outbox. Olay, onu doğuran değişikliklerle
/// birlikte (çağıranın SaveChanges'i ve transaction'ı ile) yazılır; dış sisteme gönderim sonradan, arka
/// planda yapılır. Böylece dış sistemin yavaş ya da kapalı olması üretim / depo işlemlerini etkilemez.
/// </summary>
public interface IIntegrationOutbox
{
    /// <summary>
    /// Olayı kaydedilmek üzere ekler; kalıcı olması için çağıran SaveChanges yapmalıdır. Kimliği veritabanında
    /// üretilen kayıtlar için önce kayıt kaydedilir, sonra olay aynı transaction içinde eklenir.
    /// </summary>
    void Enqueue<TPayload>(string eventType, string entityName, IFormattable entityId, string? reference, TPayload payload);
}

public sealed class IntegrationOutbox(
    IAppDbContext db, IIntegrationChannelRegistry registry, ICurrentUser currentUser, TimeProvider timeProvider)
    : IIntegrationOutbox
{
    private const string SystemUser = "system";

    public void Enqueue<TPayload>(
        string eventType, string entityName, IFormattable entityId, string? reference, TPayload payload)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var message = new IntegrationMessage
        {
            EventId = Guid.NewGuid(),
            EventType = eventType,
            EntityName = entityName,
            EntityId = entityId.ToString(null, CultureInfo.InvariantCulture),
            Reference = reference,
            Payload = JsonSerializer.Serialize(payload, IntegrationJson.Options),
            OccurredAt = now,
            CreatedBy = currentUser.UserName ?? SystemUser,
        };

        // Kanal tanımlı olmasa da olay yazılır: dış sistemler olayları API'den sırayla da okuyabilir.
        foreach (var channel in registry.Channels.Where(c => c.Handles(eventType)))
        {
            message.Deliveries.Add(IntegrationDelivery.Create(channel.Key, now));
        }

        db.IntegrationMessages.Add(message);
    }
}

public interface IIntegrationDispatcher
{
    /// <summary>Zamanı gelmiş teslimleri (en fazla bir tur kadar) gönderir; işlenen teslim sayısını döndürür.</summary>
    Task<int> DispatchDueAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Bekleyen teslimleri kanallarına gönderir. Her teslim önce kısa süreli kilitlenir (aynı anda çalışan ikinci
/// bir uygulama örneği aynı teslimi almasın), sonra gönderilir ve sonucu kaydedilir. Gönderim ile sonucun
/// kaydı arasında uygulama çökerse teslim kilit süresi dolunca yeniden gönderilir: teslim "en az bir kez"dir.
/// </summary>
public sealed class IntegrationDispatcher(
    IAppDbContext db,
    IIntegrationChannelRegistry registry,
    TimeProvider timeProvider,
    IOptions<IntegrationOptions> options,
    ILogger<IntegrationDispatcher> logger) : IIntegrationDispatcher
{
    private readonly IntegrationOptions _options = options.Value;

    public async Task<int> DispatchDueAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var dueIds = await db.IntegrationDeliveries
            .Where(d => d.Status == IntegrationDeliveryStatus.Pending
                && d.NextAttemptAt <= now
                && (d.LockedUntil == null || d.LockedUntil < now))
            .OrderBy(d => d.NextAttemptAt)
            .ThenBy(d => d.Id)
            .Select(d => d.Id)
            .Take(_options.BatchSize)
            .ToListAsync(cancellationToken);

        var processed = 0;
        foreach (var id in dueIds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (await DispatchAsync(id, cancellationToken))
            {
                processed++;
            }
        }

        return processed;
    }

    private async Task<bool> DispatchAsync(long deliveryId, CancellationToken cancellationToken)
    {
        // Kilit tek bir UPDATE ile alınır: iki gönderici aynı teslimi isterse yalnızca biri satırı günceller.
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var lockedUntil = now.AddSeconds(_options.LeaseSeconds);
        var claimed = await db.IntegrationDeliveries
            .Where(d => d.Id == deliveryId
                && d.Status == IntegrationDeliveryStatus.Pending
                && (d.LockedUntil == null || d.LockedUntil < now))
            .ExecuteUpdateAsync(update => update.SetProperty(d => d.LockedUntil, lockedUntil), cancellationToken);
        if (claimed == 0)
        {
            return false;
        }

        var delivery = await db.IntegrationDeliveries
            .Include(d => d.Message)
            .SingleAsync(d => d.Id == deliveryId, cancellationToken);
        var message = delivery.Message;

        var result = await SendAsync(delivery, message, cancellationToken);

        now = timeProvider.GetUtcNow().UtcDateTime;
        if (result.Succeeded)
        {
            delivery.MarkSucceeded(now);
        }
        else
        {
            delivery.MarkFailed(now, result.Error ?? "Bilinmeyen hata.", _options.RetryPolicy);
            logger.LogWarning(
                "Entegrasyon teslimi başarısız: olay {EventType} #{Sequence}, kanal {Channel}, deneme {Attempt}, durum {Status}. {Error}",
                message.EventType, message.Id, delivery.Channel, delivery.AttemptCount, delivery.Status, delivery.LastError);
        }

        // Gönderim yapıldıysa sonucu, uygulama kapanıyor olsa bile kaydedilir; yoksa olay boşuna yeniden gider.
        await db.SaveChangesAsync(CancellationToken.None);
        return true;
    }

    private async Task<IntegrationSendResult> SendAsync(
        IntegrationDelivery delivery, IntegrationMessage message, CancellationToken cancellationToken)
    {
        var channel = registry.Find(delivery.Channel);
        if (channel is null)
        {
            return IntegrationSendResult.Failure(
                $"'{delivery.Channel}' kanalı yapılandırmada tanımlı değil ya da kapalı.");
        }

        var envelope = new IntegrationEnvelope(
            message.EventId, message.Id, message.EventType, message.OccurredAt, message.EntityName, message.EntityId,
            message.Reference, message.Payload, delivery.Id, delivery.AttemptCount + 1);

        try
        {
            return await channel.SendAsync(envelope, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Uygulama kapanıyor: deneme sayılmaz, kilit süresi dolunca teslim yeniden alınır.
            throw;
        }
        catch (Exception exception)
        {
            // Kanal hata fırlatmamalı; fırlatırsa da tek bir teslim tüm kuyruğu durdurmamalı.
            return IntegrationSendResult.Failure($"{exception.GetType().Name}: {exception.Message}");
        }
    }
}
