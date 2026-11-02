using ERP.Core.Domain.Common;
using ERP.Core.Domain.Enums;

namespace ERP.Core.Domain.Entities;

/// <summary>
/// Dış sistemlere duyurulacak iş olayı (outbox kaydı). Olayı doğuran işlemle AYNI transaction'da yazılır:
/// işlem geri alınırsa olay da yazılmaz, işlem kalıcı olduysa olay kaybolmaz. İçeriği sonradan değişmez.
/// </summary>
[NotAudited]
public class IntegrationMessage
{
    /// <summary>Artan sıra numarası; dış sistemler olayları bununla sıralar ve kaldığı yerden okur.</summary>
    public long Id { get; set; }

    /// <summary>Olayın evrensel kimliği; alıcı aynı olayı ikinci kez işlememek için bunu kullanır.</summary>
    public Guid EventId { get; set; }

    /// <summary>Olay türü, örn. <c>shipment.shipped</c>.</summary>
    public string EventType { get; set; } = null!;

    /// <summary>Olayın ait olduğu kayıt türü (ör. Shipment) ve kimliği.</summary>
    public string EntityName { get; set; } = null!;
    public string EntityId { get; set; } = null!;

    /// <summary>Kaydın kullanıcıya görünen numarası (sevkiyat no, sipariş no).</summary>
    public string? Reference { get; set; }

    /// <summary>Olayın verisi (JSON); olay anındaki değerlerin kopyasıdır.</summary>
    public string Payload { get; set; } = null!;

    public DateTime OccurredAt { get; set; }
    public string CreatedBy { get; set; } = null!;

    public ICollection<IntegrationDelivery> Deliveries { get; } = new List<IntegrationDelivery>();
}

/// <summary>
/// Bir olayın bir kanala (ör. bir webhook adresine) teslimi. Başarısız teslim, deneme hakkı bitene kadar
/// artan aralıklarla yeniden denenir.
/// </summary>
[NotAudited]
public class IntegrationDelivery
{
    private const int MaxErrorLength = 1000;

    public long Id { get; set; }

    public long IntegrationMessageId { get; set; }
    public IntegrationMessage Message { get; set; } = null!;

    /// <summary>Hedef kanalın anahtarı, örn. <c>webhook:muhasebe</c>.</summary>
    public string Channel { get; set; } = null!;

    public IntegrationDeliveryStatus Status { get; private set; } = IntegrationDeliveryStatus.Pending;

    /// <summary>Yapılan deneme sayısı (yeniden kuyruğa alınınca sıfırlanır).</summary>
    public int AttemptCount { get; private set; }

    /// <summary>Bu andan önce gönderilmez.</summary>
    public DateTime NextAttemptAt { get; private set; }

    /// <summary>
    /// Teslimi şu an bir gönderici işliyorsa kilidin bitişi. Gönderici çökerse kilit süresi dolunca teslim
    /// yeniden alınır (bu yüzden bir olay alıcıya birden fazla kez ulaşabilir).
    /// </summary>
    public DateTime? LockedUntil { get; private set; }

    public DateTime? LastAttemptAt { get; private set; }
    public string? LastError { get; private set; }
    public DateTime? DeliveredAt { get; private set; }

    public static IntegrationDelivery Create(string channel, DateTime utcNow) =>
        new() { Channel = channel, NextAttemptAt = utcNow };

    public void MarkSucceeded(DateTime utcNow)
    {
        EnsurePending();
        AttemptCount++;
        LastAttemptAt = utcNow;
        DeliveredAt = utcNow;
        LastError = null;
        LockedUntil = null;
        Status = IntegrationDeliveryStatus.Succeeded;
    }

    /// <summary>Başarısız denemeyi kaydeder: hak kaldıysa bir sonraki denemeyi planlar, kalmadıysa teslimi bırakır.</summary>
    public void MarkFailed(DateTime utcNow, string error, IntegrationRetryPolicy policy)
    {
        EnsurePending();
        AttemptCount++;
        LastAttemptAt = utcNow;
        LastError = error.Length > MaxErrorLength ? error[..MaxErrorLength] : error;
        LockedUntil = null;

        if (AttemptCount >= policy.MaxAttempts)
        {
            Status = IntegrationDeliveryStatus.Failed;
        }
        else
        {
            NextAttemptAt = utcNow + policy.DelayAfter(AttemptCount);
        }
    }

    /// <summary>Bitmiş (başarılı ya da bırakılmış) teslimi yeniden kuyruğa alır; deneme hakkı baştan başlar.</summary>
    public void Requeue(DateTime utcNow)
    {
        if (Status == IntegrationDeliveryStatus.Pending)
        {
            throw new InvalidOperationException("Teslim zaten kuyrukta.");
        }

        Status = IntegrationDeliveryStatus.Pending;
        AttemptCount = 0;
        NextAttemptAt = utcNow;
        LockedUntil = null;
        DeliveredAt = null;
    }

    private void EnsurePending()
    {
        if (Status != IntegrationDeliveryStatus.Pending)
        {
            throw new InvalidOperationException($"{Status} durumundaki teslim yeniden sonuçlandırılamaz.");
        }
    }
}

/// <summary>
/// Başarısız teslimlerin yeniden deneme kuralı: bekleme her denemede ikiye katlanır, üst sınırı aşmaz.
/// </summary>
public sealed record IntegrationRetryPolicy(int MaxAttempts, TimeSpan BaseDelay, TimeSpan MaxDelay)
{
    /// <summary>Verilen sayıda başarısız denemeden sonra beklenecek süre (1. denemeden sonra temel süre).</summary>
    public TimeSpan DelayAfter(int failedAttempts)
    {
        if (failedAttempts < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(failedAttempts));
        }

        // 2^n taşmasın: üst sınıra zaten çok önce ulaşılır.
        var factor = Math.Pow(2, Math.Min(failedAttempts - 1, 30));
        var delay = BaseDelay.TotalSeconds * factor;
        return delay >= MaxDelay.TotalSeconds ? MaxDelay : TimeSpan.FromSeconds(delay);
    }
}
