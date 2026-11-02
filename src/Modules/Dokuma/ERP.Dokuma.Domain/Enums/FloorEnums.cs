namespace ERP.Dokuma.Domain.Enums;

public enum DokumaOperatorRole
{
    Weaver,

    /// <summary>Arızayı kapatabilir.</summary>
    Supervisor,

    /// <summary>Arızayı kapatabilir.</summary>
    Maintenance,
}

/// <summary>Kart isteyen tablet işlemleri.</summary>
public enum DokumaFloorAction
{
    Pause,
    ReportBreakdown,
    ResolveBreakdown,
}

/// <summary>Personelin oturumdaki duraklaması; tezgâh duruşu değildir, verimi etkilemez.</summary>
public enum DokumaPauseKind
{
    /// <summary>Levent bekliyor: bekleme süresi çalışılmış sayılmaz, top kesilince ya da duruş açılınca biter.</summary>
    WaitingForBeam,

    /// <summary>Mola.</summary>
    Break,

    /// <summary>Kısa ara; neden zorunludur, kartla verilir ve kartla dönülür.</summary>
    Pause,
}

public enum DokumaStopCategory
{
    /// <summary>Personelin arası (tuvalet, su ...): oturumda tutulur, tezgâh duruşu sayılmaz.</summary>
    Pause,

    /// <summary>Üretim duruşu (iplik bekleme, kopuş ...): çalışan süreden düşer, personel kapatır.</summary>
    Stop,

    /// <summary>Arıza: çalışan süreden düşer, yalnızca vardiya amiri ya da bakımcı kapatır.</summary>
    Breakdown,

    /// <summary>Planlı duruş (bakım, temizlik): kullanılabilir süreden düşer, verimi etkilemez.</summary>
    Planned,
}

public enum DokumaSessionEndReason
{
    Logout,

    /// <summary>Vardiya bitti; oturum kendiliğinden kapandı.</summary>
    ShiftEnded,
    TakenOver,
    MovedAway,
    ClosedByOffice,
}
