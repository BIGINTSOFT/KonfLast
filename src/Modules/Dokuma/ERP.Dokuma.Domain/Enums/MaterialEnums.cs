namespace ERP.Dokuma.Domain.Enums;

/// <summary>İplik numara sistemi.</summary>
public enum YarnCountSystem
{
    Ne,
    Nm,
    Dtex,
    Denier,
}

/// <summary>Atkı atma sistemine göre tezgâh türü.</summary>
public enum LoomType
{
    AirJet,
    Rapier,
    WaterJet,
    Projectile,
    Shuttle,
}

/// <summary>Ağızlık açma sistemi; tezgâhta ne dokunabileceğini, artikelde ne gerektiğini belirtir.</summary>
public enum SheddingType
{
    /// <summary>Eksantrik (kamlı): bezayağı, basit dimi.</summary>
    Cam,

    /// <summary>Armür.</summary>
    Dobby,

    Jacquard,
}

/// <summary>Artikeldeki ipliğin yeri.</summary>
public enum ArticleYarnRole
{
    Warp,
    Weft,
}
