namespace ERP.Orme.Domain.Enums;

/// <summary>İplik numara sistemi.</summary>
public enum YarnCountSystem
{
    Ne,
    Nm,
    Dtex,
    Denier,
}

/// <summary>Makinenin plaka yapısı; makinede ne örülebileceğini, kumaşta ne gerektiğini belirler.</summary>
public enum KnitBedType
{
    /// <summary>Tek plaka: süprem, iki iplik, üç iplik.</summary>
    Single,

    /// <summary>Çift plaka: ribana, interlok.</summary>
    Double,
}
