using ERP.Core.Domain.Common;
using ERP.Dokuma.Domain.Enums;

namespace ERP.Dokuma.Domain.Entities;

/// <summary>Dokuma operatörü. Kart numarası tablette oturum açmak ve işlem onaylamak içindir.</summary>
public class DokumaOperator : SoftDeletableEntity, IActivatable
{
    public string Code { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;

    /// <summary>Ad ve soyaddan oluşur; listelerde ve kayıtlarda bu gösterilir.</summary>
    public string Name { get; set; } = null!;

    /// <summary>Sahadaki görevi; ortak tablet yetkileri görevden gelir.</summary>
    public DokumaOperatorRole Role { get; set; } = DokumaOperatorRole.Weaver;

    /// <summary>Kart listesinde tanımlı personel kartının numarası.</summary>
    public string? CardUid { get; set; }

    /// <summary>Molaya birlikte çıktığı grup; boşsa yalnızca herkese açık molaları görür.</summary>
    public int? BreakGroupId { get; set; }
    public DokumaBreakGroup? BreakGroup { get; set; }

    /// <summary>Kişiye özel ek yetki: görevi vermese de ara verebilir.</summary>
    public bool CanPause { get; set; }

    /// <summary>Kişiye özel ek yetki: görevi vermese de arıza bildirebilir.</summary>
    public bool CanReportBreakdown { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>Görevi bu işleme yetkili mi? Görevle yetkili kişi kartıyla her tablette işlem yapar.</summary>
    public bool RoleAllows(DokumaFloorAction action) => action switch
    {
        DokumaFloorAction.Pause => Role == DokumaOperatorRole.Supervisor,
        DokumaFloorAction.ReportBreakdown or DokumaFloorAction.ResolveBreakdown => Role != DokumaOperatorRole.Weaver,
        _ => false,
    };

    /// <summary>Kişiye özel yetkisi var mı? Yalnızca kendi oturumunun açık olduğu tablette geçer.</summary>
    public bool PersonallyAllows(DokumaFloorAction action) => action switch
    {
        DokumaFloorAction.Pause => CanPause,
        DokumaFloorAction.ReportBreakdown => CanReportBreakdown,
        _ => false,
    };
}

/// <summary>Operatörün fotoğrafı; listelerde gereksiz yere taşınmasın diye ayrı tutulur.</summary>
[NotAudited]
public class DokumaOperatorPhoto
{
    public int OperatorId { get; set; }
    public DokumaOperator Operator { get; set; } = null!;

    public byte[] Content { get; set; } = null!;

    /// <summary>image/jpeg ya da image/png.</summary>
    public string ContentType { get; set; } = null!;

    public DateTime UpdatedAt { get; set; }
}

/// <summary>Operatörün çalışabileceği tezgâh; operatörde hiç kayıt yoksa tezgâh kısıtı yoktur.</summary>
public class DokumaOperatorLoom
{
    public int OperatorId { get; set; }
    public DokumaOperator Operator { get; set; } = null!;

    public int LoomId { get; set; }
    public DokumaLoom Loom { get; set; } = null!;
}

/// <summary>
/// Dokuma tableti: tezgâha bağlıysa yalnızca o tezgâhı, bağlı değilse (salon tableti) bütün tezgâhları gösterir. Bir dokumacı
/// birden çok tezgâha baktığı için iki kullanım da vardır. Eşleştirme Boyahane tabletiyle aynı yöntemdir; kayıtlar ayrıdır.
/// </summary>
public class DokumaTablet : SoftDeletableEntity, IActivatable
{
    public string TabletCode { get; set; } = null!;
    public string? Name { get; set; }

    public int? LoomId { get; set; }
    public DokumaLoom? Loom { get; set; }

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
public class DokumaTabletSession
{
    public long Id { get; set; }

    public int TabletId { get; set; }
    public DokumaTablet Tablet { get; set; } = null!;

    public int OperatorId { get; set; }
    public DokumaOperator Operator { get; set; } = null!;

    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public DokumaSessionEndReason? EndReason { get; set; }

    public ICollection<DokumaSessionPause> Pauses { get; } = [];
}

/// <summary>Oturumdaki mola ya da ara süresi; tezgâh çalışmayı sürdürür, verimi etkilemez.</summary>
[NotAudited]
public class DokumaSessionPause
{
    public long Id { get; set; }

    public long SessionId { get; set; }
    public DokumaTabletSession Session { get; set; } = null!;

    public DokumaPauseKind Kind { get; set; }

    public int? ReasonId { get; set; }
    public DokumaStopReason? Reason { get; set; }

    /// <summary>Yalnızca planlı molada dolu: hangi mola planı.</summary>
    public int? BreakScheduleId { get; set; }
    public DokumaBreakSchedule? BreakSchedule { get; set; }

    public string? Note { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
}

/// <summary>RFID kartı. Kart önce buraya tanımlanır; sonra personele verilir.</summary>
public class DokumaRfidCard : SoftDeletableEntity, IActivatable
{
    public string Uid { get; set; } = null!;

    /// <summary>Kartın üzerinde yazan numara.</summary>
    public string? Label { get; set; }

    public bool IsActive { get; set; } = true;
}

/// <summary>Tabletteki ara, duruş ve arıza nedenleri.</summary>
public class DokumaStopReason : SoftDeletableEntity, IActivatable
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public DokumaStopCategory Category { get; set; }

    /// <summary>Tabletteki sıra; küçük olan önce gösterilir.</summary>
    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;
}

/// <summary>Tezgâhın durduğu süre; verim raporu buradan okunur. Kayıtlar silinmez; tezgâhta aynı anda tek açık duruş olur.</summary>
[NotAudited]
public class DokumaLoomStop
{
    public long Id { get; set; }

    public int LoomId { get; set; }
    public DokumaLoom Loom { get; set; } = null!;

    /// <summary>Duruş başladığında tezgâhtaki levent.</summary>
    public int? BeamId { get; set; }
    public DokumaBeam? Beam { get; set; }

    public int ReasonId { get; set; }
    public DokumaStopReason Reason { get; set; } = null!;

    /// <summary>Başladığı andaki tür; neden kartı sonradan değişse de geçmiş rapor bozulmaz.</summary>
    public DokumaStopCategory Category { get; set; }

    public string? Note { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public string StartedBy { get; set; } = null!;
    public string? EndedBy { get; set; }
    public int? TabletId { get; set; }
}
