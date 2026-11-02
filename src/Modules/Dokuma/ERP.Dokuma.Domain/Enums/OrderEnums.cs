namespace ERP.Dokuma.Domain.Enums;

public enum OrderOwnership
{
    /// <summary>Fason: iplik müşterinin, işletme dokuma hizmeti verir.</summary>
    Commission,

    /// <summary>Kendi malı: iplik işletmenin, kumaş dokunup satılır.</summary>
    OwnGoods,
}

public enum OrderStatus
{
    Draft,
    Confirmed,
    Cancelled,
}
