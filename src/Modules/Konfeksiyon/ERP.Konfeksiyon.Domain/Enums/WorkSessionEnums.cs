namespace ERP.Konfeksiyon.Domain.Enums;

public enum WorkSessionStatus
{
    /// <summary>Operatör makine başında, çalışmaya hazır ya da çalışıyor.</summary>
    Active,
    LoggedOut,

    /// <summary>Planlı molada.</summary>
    Break,

    /// <summary>Operatör kendi isteğiyle ara verdi.</summary>
    Paused,
    MachineBreakdown,
    WaitingForBundle,
}

public enum WorkSessionEndReason
{
    /// <summary>Operatör tabletten çıkış yaptı.</summary>
    Logout,

    /// <summary>Makineye başka bir operatör giriş yaptı.</summary>
    TakenOver,

    /// <summary>Operatör başka bir makinede giriş yaptı.</summary>
    MovedToAnotherMachine,

    /// <summary>Yetkili kullanıcı oturumu yönetim ekranından kapattı.</summary>
    ClosedBySupervisor,

    /// <summary>Vardiya bittikten sonra açık kalan oturum sistem tarafından kapatıldı.</summary>
    ShiftEnded,
}

/// <summary>Oturum içindeki duraklamanın türü. Her tür ayrı bir oturum durumuna karşılık gelir.</summary>
public enum WorkSessionPauseType
{
    /// <summary>Planlı mola (mola planına bağlı).</summary>
    Break,

    /// <summary>Operatörün kendi isteğiyle verdiği ara.</summary>
    Pause,
    MachineBreakdown,
    WaitingForBundle,
}

/// <summary>Duruş nedeninin kullanıldığı yer.</summary>
public enum StopReasonCategory
{
    /// <summary>Operatörün kendi isteğiyle verdiği aranın nedeni (su, tuvalet ...).</summary>
    Pause,

    /// <summary>Makine arızasının nedeni (iğne kırıldı, motor arızası ...).</summary>
    Breakdown,
}

/// <summary>Mola saati geldiğinde elinde işi olan operatörün ne yapacağı.</summary>
public enum BreakPolicy
{
    /// <summary>İş yarıda bırakılıp hemen molaya çıkılır.</summary>
    Immediate,

    /// <summary>Eldeki demet bitirilip adedi bildirildikten sonra.</summary>
    AfterBundle,

    /// <summary>O ana kadar yapılan adet bildirildikten sonra (demet yarım kalabilir).</summary>
    AfterQuantity,

    /// <summary>Eldeki operasyon tamamlandıktan sonra.</summary>
    AfterOperation,
}

/// <summary>Personelin sahadaki görevi; tabletteki ortak yetkileri görev başına tanımlanır.</summary>
public enum StaffRole
{
    Operator,

    LineChief,

    Maintenance,
}

/// <summary>Tablette yetki gerektiren saha işlemleri.</summary>
public enum FloorAction
{
    /// <summary>Operatörün kendi isteğiyle ara vermesi.</summary>
    Pause,
    ReportBreakdown,

    /// <summary>Arızanın giderildiğini bildirme (makineyi yeniden üretime açma).</summary>
    ResolveBreakdown,

    /// <summary>Adet bildirirken hatalı adet girme.</summary>
    ReportReject,
}
