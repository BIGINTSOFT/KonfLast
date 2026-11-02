namespace ERP.Orme.Domain.Enums;

public enum OrderOwnership
{
    /// <summary>Fason: iplik müşterinin, işletme örme hizmeti verir.</summary>
    Commission,

    /// <summary>Kendi malı: iplik işletmenin, kumaş örülüp satılır.</summary>
    OwnGoods,
}

public enum OrderStatus
{
    Draft,
    Confirmed,
    Cancelled,
}

public enum KnitOrderStatus
{
    /// <summary>Örme sürüyor: makine atanır, iplik verilir, top çıkar.</summary>
    Open,

    Completed,
    Cancelled,
}

public enum FabricRollStatus
{
    PendingInspection,
    FirstQuality,
    SecondQuality,
    Rejected,
}
