using ERP.Core.Domain.Common;
using ERP.Orme.Domain.Enums;

namespace ERP.Orme.Domain.Entities;

/// <summary>Örme operatörü. Kart numarası tablette oturum açmak ve işlem onaylamak içindir.</summary>
public class OrmeOperator : SoftDeletableEntity, IActivatable
{
    public string Code { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;

    /// <summary>Ad ve soyaddan oluşur; listelerde ve kayıtlarda bu gösterilir.</summary>
    public string Name { get; set; } = null!;

    /// <summary>Sahadaki görevi; ortak tablet yetkileri görevden gelir.</summary>
    public OrmeOperatorRole Role { get; set; } = OrmeOperatorRole.Knitter;

    /// <summary>Kart listesinde tanımlı personel kartının numarası.</summary>
    public string? CardUid { get; set; }

    /// <summary>Molaya birlikte çıktığı grup; boşsa yalnızca herkese açık molaları görür.</summary>
    public int? BreakGroupId { get; set; }
    public OrmeBreakGroup? BreakGroup { get; set; }

    /// <summary>Kişiye özel ek yetki: görevi vermese de ara verebilir.</summary>
    public bool CanPause { get; set; }

    /// <summary>Kişiye özel ek yetki: görevi vermese de arıza bildirebilir.</summary>
    public bool CanReportBreakdown { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>Görevi bu işleme yetkili mi? Görevle yetkili kişi kartıyla her tablette işlem yapar.</summary>
    public bool RoleAllows(OrmeFloorAction action) => action switch
    {
        OrmeFloorAction.Pause => Role == OrmeOperatorRole.Supervisor,
        OrmeFloorAction.ReportBreakdown or OrmeFloorAction.ResolveBreakdown => Role != OrmeOperatorRole.Knitter,
        _ => false,
    };

    /// <summary>Kişiye özel yetkisi var mı? Yalnızca kendi oturumunun açık olduğu tablette geçer.</summary>
    public bool PersonallyAllows(OrmeFloorAction action) => action switch
    {
        OrmeFloorAction.Pause => CanPause,
        OrmeFloorAction.ReportBreakdown => CanReportBreakdown,
        _ => false,
    };
}

/// <summary>Operatörün fotoğrafı; listelerde gereksiz yere taşınmasın diye ayrı tutulur.</summary>
[NotAudited]
public class OrmeOperatorPhoto
{
    public int OperatorId { get; set; }
    public OrmeOperator Operator { get; set; } = null!;

    public byte[] Content { get; set; } = null!;

    /// <summary>image/jpeg ya da image/png.</summary>
    public string ContentType { get; set; } = null!;

    public DateTime UpdatedAt { get; set; }
}

/// <summary>Operatörün çalışabileceği makine; operatörde hiç kayıt yoksa makine kısıtı yoktur.</summary>
public class OrmeOperatorMachine
{
    public int OperatorId { get; set; }
    public OrmeOperator Operator { get; set; } = null!;

    public int MachineId { get; set; }
    public OrmeMachine Machine { get; set; } = null!;
}

/// <summary>
/// Örme tableti: makineye bağlıysa yalnızca o makineyi, bağlı değilse (salon tableti) bütün makineleri gösterir. Bir örme operatörü
/// birden çok makineye baktığı için iki kullanım da vardır. Eşleştirme Boyahane tabletiyle aynı yöntemdir; kayıtlar ayrıdır.
/// </summary>
public class OrmeTablet : SoftDeletableEntity, IActivatable
{
    public string TabletCode { get; set; } = null!;
    public string? Name { get; set; }

    public int? MachineId { get; set; }
    public OrmeMachine? Machine { get; set; }

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

/// <summary>Tabletteki personel oturumu. Elde süren iş olmadığı için yeni kart oturumu devralır.</summary>
[NotAudited]
public class OrmeTabletSession
{
    public long Id { get; set; }

    public int TabletId { get; set; }
    public OrmeTablet Tablet { get; set; } = null!;

    public int OperatorId { get; set; }
    public OrmeOperator Operator { get; set; } = null!;

    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public OrmeSessionEndReason? EndReason { get; set; }

    public ICollection<OrmeSessionPause> Pauses { get; } = [];
}

/// <summary>Oturumdaki mola ya da ara süresi; makine çalışmayı sürdürür, verimi etkilemez.</summary>
[NotAudited]
public class OrmeSessionPause
{
    public long Id { get; set; }

    public long SessionId { get; set; }
    public OrmeTabletSession Session { get; set; } = null!;

    public OrmePauseKind Kind { get; set; }

    public int? ReasonId { get; set; }
    public OrmeStopReason? Reason { get; set; }

    /// <summary>Yalnızca planlı molada dolu: hangi mola planı.</summary>
    public int? BreakScheduleId { get; set; }
    public OrmeBreakSchedule? BreakSchedule { get; set; }

    public string? Note { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
}

/// <summary>RFID kartı. Kart önce buraya tanımlanır; sonra personele verilir.</summary>
public class OrmeRfidCard : SoftDeletableEntity, IActivatable
{
    public string Uid { get; set; } = null!;

    /// <summary>Kartın üzerinde yazan numara.</summary>
    public string? Label { get; set; }

    public bool IsActive { get; set; } = true;
}

/// <summary>Tabletteki ara, duruş ve arıza nedenleri.</summary>
public class OrmeStopReason : SoftDeletableEntity, IActivatable
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public OrmeStopCategory Category { get; set; }

    /// <summary>Tabletteki sıra; küçük olan önce gösterilir.</summary>
    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;
}

/// <summary>Makinenin durduğu süre; verim raporu buradan okunur. Kayıtlar silinmez; makinede aynı anda tek açık duruş olur.</summary>
[NotAudited]
public class OrmeMachineStop
{
    public long Id { get; set; }

    public int MachineId { get; set; }
    public OrmeMachine Machine { get; set; } = null!;

    /// <summary>Duruş başladığında makinede çalışan emir.</summary>
    public int? KnitOrderId { get; set; }
    public OrmeKnitOrder? KnitOrder { get; set; }

    public int ReasonId { get; set; }
    public OrmeStopReason Reason { get; set; } = null!;

    /// <summary>Başladığı andaki tür; neden kartı sonradan değişse de geçmiş rapor bozulmaz.</summary>
    public OrmeStopCategory Category { get; set; }

    public string? Note { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public string StartedBy { get; set; } = null!;
    public string? EndedBy { get; set; }
    public int? TabletId { get; set; }
}
