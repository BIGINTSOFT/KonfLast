using ERP.Boyahane.Domain.Enums;
using ERP.Core.Domain.Common;

namespace ERP.Boyahane.Domain.Entities;

/// <summary>Boyahane operatörü (saha personeli). Kart numarası tablette oturum açmak içindir.</summary>
public class BoyahaneOperator : SoftDeletableEntity, IActivatable
{
    public string Code { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;

    /// <summary>Ad ve soyaddan oluşur; listelerde ve kayıtlarda bu gösterilir.</summary>
    public string Name { get; set; } = null!;

    /// <summary>Sahadaki görevi; ortak tablet yetkileri görevden gelir.</summary>
    public BoyahaneOperatorRole Role { get; set; } = BoyahaneOperatorRole.Operator;

    /// <summary>Kart listesinde tanımlı personel kartının numarası.</summary>
    public string? CardUid { get; set; }

    /// <summary>Molaya birlikte çıktığı grup; boşsa yalnızca herkese açık molaları görür.</summary>
    public int? BreakGroupId { get; set; }
    public BoyahaneBreakGroup? BreakGroup { get; set; }

    /// <summary>Kişiye özel ek yetki: görevi vermese de ara verebilir.</summary>
    public bool CanPause { get; set; }

    /// <summary>Kişiye özel ek yetki: görevi vermese de arıza bildirebilir.</summary>
    public bool CanReportBreakdown { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>Görevi bu işleme yetkili mi? Görevle yetkili kişi kartıyla her tablette işlem yapar.</summary>
    public bool RoleAllows(BoyahaneFloorAction action) => action switch
    {
        BoyahaneFloorAction.Pause => Role == BoyahaneOperatorRole.Supervisor,
        BoyahaneFloorAction.ReportBreakdown or BoyahaneFloorAction.ResolveBreakdown => Role != BoyahaneOperatorRole.Operator,
        _ => false,
    };

    /// <summary>Kişiye özel yetkisi var mı? Yalnızca kendi oturumunun açık olduğu tablette geçer.</summary>
    public bool PersonallyAllows(BoyahaneFloorAction action) => action switch
    {
        BoyahaneFloorAction.Pause => CanPause,
        BoyahaneFloorAction.ReportBreakdown => CanReportBreakdown,
        _ => false,
    };
}

/// <summary>Operatörün fotoğrafı; listelerde gereksiz yere taşınmasın diye ayrı tutulur.</summary>
[NotAudited]
public class BoyahaneOperatorPhoto
{
    public int OperatorId { get; set; }
    public BoyahaneOperator Operator { get; set; } = null!;

    public byte[] Content { get; set; } = null!;

    /// <summary>image/jpeg ya da image/png.</summary>
    public string ContentType { get; set; } = null!;

    public DateTime UpdatedAt { get; set; }
}

/// <summary>Operatörün yapabildiği operasyon.</summary>
public class BoyahaneOperatorOperation
{
    public int OperatorId { get; set; }
    public BoyahaneOperator Operator { get; set; } = null!;

    public int OperationId { get; set; }
    public BoyahaneOperation Operation { get; set; } = null!;
}

/// <summary>Operatörün çalışabileceği makine; liste boşsa makine kısıtı yoktur.</summary>
public class BoyahaneOperatorMachine
{
    public int OperatorId { get; set; }
    public BoyahaneOperator Operator { get; set; } = null!;

    public int MachineId { get; set; }
    public BoyahaneMachine Machine { get; set; } = null!;
}

/// <summary>
/// Boyahane tableti: makineye bağlıysa o makinenin adımlarını, bağlı değilse kontrol noktası olarak makinesiz adımları
/// yürütür. Eşleştirme Konfeksiyon tabletiyle aynı yöntemdir; kayıtlar ayrıdır.
/// </summary>
public class BoyahaneTablet : SoftDeletableEntity, IActivatable
{
    public string TabletCode { get; set; } = null!;
    public string? Name { get; set; }

    public int? MachineId { get; set; }
    public BoyahaneMachine? Machine { get; set; }

    public bool IsActive { get; set; } = true;

    public string? DeviceIdentifier { get; private set; }

    [AuditMasked] public string? DeviceKeyHash { get; private set; }

    [AuditIgnore] public DateTime? LastSeenAt { get; set; }

    public bool IsEnrolled => DeviceKeyHash is not null;

    public void Enroll(string deviceIdentifier, string deviceKeyHash)
    {
        if (IsEnrolled)
        {
            throw new InvalidOperationException("Tablet zaten bir cihazla eşleştirilmiş.");
        }

        DeviceIdentifier = deviceIdentifier;
        DeviceKeyHash = deviceKeyHash;
    }

    public void ResetDevice()
    {
        DeviceIdentifier = null;
        DeviceKeyHash = null;
    }
}

/// <summary>
/// Tabletteki personel oturumu. Personelin süren adımı varken oturum kapanmaz ve devralınmaz; gerekirse ofisten kapatılır,
/// adım işlemde kalır ve yeni oturumdan bitirilir.
/// </summary>
[NotAudited]
public class BoyahaneTabletSession
{
    public long Id { get; set; }

    public int TabletId { get; set; }
    public BoyahaneTablet Tablet { get; set; } = null!;

    public int OperatorId { get; set; }
    public BoyahaneOperator Operator { get; set; } = null!;

    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public BoyahaneSessionEndReason? EndReason { get; set; }

    public ICollection<BoyahaneSessionPause> Pauses { get; } = [];
}

/// <summary>Partinin bir adımı. Adımlar sırayla yürür; aynı anda partide ve makinede tek adım işlemde olur.</summary>
public class BoyahaneBatchStep : AuditableEntity
{
    public int BatchId { get; set; }
    public BoyahaneBatch Batch { get; set; } = null!;

    public int Sequence { get; set; }
    public int OperationId { get; set; }
    public BoyahaneOperation Operation { get; set; } = null!;

    /// <summary>Renk kontrolünün eklediği adım: ilave, yeniden işlem ya da söküm (söküm adımı ve ardından gelen boyama). Normal adımda boştur.</summary>
    public ColorCheckResult? ReworkKind { get; set; }

    /// <summary>Söküm adımında uygulanacak söküm reçetesi; renk kontrolünde seçilir. Diğer adımlarda boştur.</summary>
    public int? StrippingRecipeId { get; set; }
    public BoyahaneStrippingRecipe? StrippingRecipe { get; set; }

    public BatchStepStatus Status { get; set; } = BatchStepStatus.Pending;

    public int? MachineId { get; set; }
    public BoyahaneMachine? Machine { get; set; }

    public int? TabletId { get; set; }
    public BoyahaneTablet? Tablet { get; set; }

    public int? StartedByOperatorId { get; set; }
    public BoyahaneOperator? StartedByOperator { get; set; }

    public int? CompletedByOperatorId { get; set; }
    public BoyahaneOperator? CompletedByOperator { get; set; }

    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    /// <summary>Renk kontrolü adımının sonucu.</summary>
    public ColorCheckResult? ColorResult { get; set; }
    public string? Note { get; set; }

    public ICollection<BoyahaneBatchConsumption> Consumptions { get; } = [];
}

/// <summary>Boyama, söküm ya da baskı adımının boya ve kimyasal tüketimi. Hesaplanan ve gerçek miktar ayrı tutulur; gerçek miktar lotlardan düşer.</summary>
public class BoyahaneBatchConsumption : AuditableEntity
{
    public int StepId { get; set; }
    public BoyahaneBatchStep Step { get; set; } = null!;

    public int DyeChemicalId { get; set; }
    public BoyahaneDyeChemical DyeChemical { get; set; } = null!;

    /// <summary>Reçeteden hesaplanan miktar (kg); ilavede sıfırdır.</summary>
    public decimal PlannedKilograms { get; set; }
    public decimal ActualKilograms { get; set; }

    /// <summary>Tüketimin düştüğü lotlar; stok yetmediyse toplamı gerçek miktardan azdır.</summary>
    public ICollection<BoyahaneConsumptionLot> Lots { get; } = [];
}

/// <summary>Tabletten gelen işlem kimliği: bağlantı kesilince yeniden gönderilen işlem ikinci kez yapılmaz.</summary>
[NotAudited]
public class BoyahaneStepRequest
{
    public long Id { get; set; }
    public Guid RequestId { get; set; }
    public int StepId { get; set; }
    public DateTime ReceivedAt { get; set; }
}

/// <summary>RFID kartı. Kart önce buraya tanımlanır; sonra personele ya da partiye verilir.</summary>
public class BoyahaneRfidCard : SoftDeletableEntity, IActivatable
{
    public string Uid { get; set; } = null!;
    public BoyahaneCardType CardType { get; set; }

    /// <summary>Kartın üzerinde yazan numara.</summary>
    public string? Label { get; set; }

    public bool IsActive { get; set; } = true;
}

/// <summary>Tabletteki ara ve arıza nedenleri.</summary>
public class BoyahaneStopReason : SoftDeletableEntity, IActivatable
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public BoyahaneStopReasonCategory Category { get; set; }

    /// <summary>Tabletteki sıra; küçük olan önce gösterilir.</summary>
    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;
}

/// <summary>Oturumdaki mola, ara ya da arıza süresi.</summary>
[NotAudited]
public class BoyahaneSessionPause
{
    public long Id { get; set; }

    public long SessionId { get; set; }
    public BoyahaneTabletSession Session { get; set; } = null!;

    public BoyahanePauseKind Kind { get; set; }

    public int? ReasonId { get; set; }
    public BoyahaneStopReason? Reason { get; set; }

    /// <summary>Yalnızca planlı molada dolu: hangi mola planı.</summary>
    public int? BreakScheduleId { get; set; }
    public BoyahaneBreakSchedule? BreakSchedule { get; set; }

    public string? Note { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
}

/// <summary>Makinenin bir durumda geçirdiği süre; arıza ve bakım süreleri buradan okunur. Kayıtlar silinmez.</summary>
[NotAudited]
public class BoyahaneMachineStatusLog
{
    public long Id { get; set; }

    public int MachineId { get; set; }
    public BoyahaneMachine Machine { get; set; } = null!;

    public DyeMachineStatus Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public string? Reason { get; set; }
    public string ChangedBy { get; set; } = null!;
    public int? TabletId { get; set; }
}
