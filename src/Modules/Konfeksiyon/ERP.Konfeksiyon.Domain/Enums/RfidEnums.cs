namespace ERP.Konfeksiyon.Domain.Enums;

public enum RfidCardType
{
    /// <summary>Demete takılan, yeniden kullanılan kart.</summary>
    Bundle,

    /// <summary>Operatörün personel kartı.</summary>
    Operator,
}

/// <summary>Tabletin kartı hangi amaçla okuttuğu.</summary>
public enum RfidReadKind
{
    Operator,
    Bundle,
}
