using ERP.Core.Domain.Common;
using ERP.Konfeksiyon.Domain.Enums;

namespace ERP.Konfeksiyon.Domain.Entities;

/// <summary>
/// Üretim olayı: bir demet operasyonunda ne oldu, kim, hangi makinede, ne zaman. Kayıtlar yalnızca
/// eklenir; güncellenmez ve silinmez (veritabanında trigger ile de engellenir). Performans, SAM ve
/// izlenebilirlik raporları bu kayıtlardan üretilir.
/// </summary>
[NotAudited]
public class BundleOperationEvent
{
    public long Id { get; init; }

    /// <summary>
    /// Olayı doğuran isteğin istemcide üretilen kimliği. Aynı istek yeniden gelirse (ağ tekrarı,
    /// çevrimdışı kuyruk) olay ikinci kez yazılmaz.
    /// </summary>
    public Guid RequestId { get; init; }

    public BundleOperationEventType EventType { get; init; }

    public int BundleId { get; init; }
    public Bundle Bundle { get; init; } = null!;

    public long BundleOperationId { get; init; }
    public BundleOperation BundleOperation { get; init; } = null!;

    public int MachineId { get; init; }
    public Machine Machine { get; init; } = null!;

    /// <summary>İşi yapan operatör; ofisten operatörsüz kapatılan işte (ofis kapatması) boş.</summary>
    public int? OperatorId { get; init; }
    public Operator? Operator { get; init; }

    /// <summary>
    /// Adet operatörün performansına (verim, kazanılan dakika) ve primine sayılır mı. Tabletten gelen olaylarda her
    /// zaman evet; ofisten elle bildirimde seçilir, operatörsüz bildirimde hayır. Bant / sipariş adetleri her durumda sayılır.
    /// </summary>
    public bool CountsForPerformance { get; init; } = true;

    /// <summary>Olay sırasında operatörün açık çalışma oturumu (varsa).</summary>
    public long? WorkSessionId { get; init; }
    public WorkSession? WorkSession { get; init; }

    /// <summary>Olayı bildiren tablet.</summary>
    public int? TabletId { get; init; }

    /// <summary>
    /// Olay ofisten elle girildiyse (tablet / kart olmadan) kaydı giren kullanıcı; tabletten gelen olaylarda boş.
    /// </summary>
    public string? EnteredBy { get; init; }

    /// <summary>Yalnızca <see cref="BundleOperationEventType.QuantityReported"/> olayında dolu.</summary>
    public int ProducedQuantity { get; init; }

    /// <summary>Yalnızca <see cref="BundleOperationEventType.RejectionReported"/> olayında dolu.</summary>
    public int RejectedQuantity { get; init; }

    /// <summary>
    /// Yalnızca bildirimi kapatan olayda (tamamlandı / yarım bırakıldı) dolu: son başlatmadan bu yana geçen net
    /// süre (saniye); mola, ara ve arıza düşülmüştür.
    /// </summary>
    public int? RunSeconds { get; init; }

    /// <summary>
    /// Yalnızca bildirimi kapatan olayda dolu: bildirilen sağlam adedin standart süresinin harcanan net süreye
    /// oranı (%). Sağlam adet bildirilmediyse boş.
    /// </summary>
    public int? EfficiencyPercent { get; init; }

    /// <summary>Sunucu saati; cihaz saati kullanılmaz.</summary>
    public DateTime OccurredAt { get; init; }
}
