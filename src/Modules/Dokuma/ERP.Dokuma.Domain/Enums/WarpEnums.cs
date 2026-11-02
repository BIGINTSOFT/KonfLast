namespace ERP.Dokuma.Domain.Enums;

public enum WarpOrderStatus
{
    /// <summary>Çözgü sürüyor: iplik verilir, levent eklenir.</summary>
    Open,

    Completed,
    Cancelled,
}

/// <summary>Leventin haşıl şekli; levent eklenirken seçilir.</summary>
public enum SizingMode
{
    /// <summary>Haşılsız: levent doğrudan tezgâha hazırlanır.</summary>
    None,

    InHouse,

    /// <summary>Fason: levent haşılcıya gönderilir, geri alınır.</summary>
    Commission,
}

public enum BeamStatus
{
    /// <summary>Çözgüden çıktı; haşıl ya da tahar / düğüm bekliyor.</summary>
    Warped,

    /// <summary>Fason haşılcıda.</summary>
    AtSizing,

    /// <summary>Tahar ya da düğüm yapıldı, tezgâha bağlanmayı bekliyor.</summary>
    Ready,

    OnLoom,
    Finished,
}

/// <summary>Ham topun kalite durumu: 4 puan kontrolünün sonucu ya da yetkilinin kararı.</summary>
public enum FabricRollStatus
{
    PendingInspection,
    FirstQuality,
    SecondQuality,
    Rejected,
}

/// <summary>Leventin tezgâha hazırlanma şekli.</summary>
public enum DrawingMethod
{
    /// <summary>Tahar: teller gücü ve tarağa tek tek geçirilir.</summary>
    Drawing,

    /// <summary>Düğüm: yeni çözgü tezgâhtaki eskisine düğümlenir.</summary>
    Tying,
}
