namespace ERP.Orme.Domain.Enums;

public enum OrmeOperatorRole
{
    Knitter,

    /// <summary>Arızayı kapatabilir.</summary>
    Supervisor,

    /// <summary>Arızayı kapatabilir.</summary>
    Maintenance,
}

/// <summary>Kart isteyen tablet işlemleri.</summary>
public enum OrmeFloorAction
{
    Pause,
    ReportBreakdown,
    ResolveBreakdown,
}

/// <summary>Personelin oturumdaki duraklaması; makine duruşu değildir, verimi etkilemez.</summary>
public enum OrmePauseKind
{
    /// <summary>Emir bekliyor: bekleme süresi çalışılmış sayılmaz, top kaydedilince ya da duruş açılınca biter.</summary>
    WaitingForOrder,

    /// <summary>Mola.</summary>
    Break,

    /// <summary>Kısa ara; neden zorunludur, kartla verilir ve kartla dönülür.</summary>
    Pause,
}

public enum OrmeStopCategory
{
    /// <summary>Personelin arası (tuvalet, su ...): oturumda tutulur, makine duruşu sayılmaz.</summary>
    Pause,

    /// <summary>Üretim duruşu (iplik bekleme, kopuş ...): çalışan süreden düşer, personel kapatır.</summary>
    Stop,

    /// <summary>Arıza: çalışan süreden düşer, yalnızca vardiya amiri ya da bakımcı kapatır.</summary>
    Breakdown,

    /// <summary>Planlı duruş (bakım, temizlik): kullanılabilir süreden düşer, verimi etkilemez.</summary>
    Planned,
}

public enum OrmeSessionEndReason
{
    Logout,

    /// <summary>Vardiya bitti; oturum kendiliğinden kapandı.</summary>
    ShiftEnded,
    TakenOver,
    MovedAway,
    ClosedByOffice,
}
