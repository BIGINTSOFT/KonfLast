namespace ERP.Konfeksiyon.Domain.Enums;

public enum ProductionOrderStatus
{
    /// <summary>Hazırlanıyor; düzenlenebilir ve silinebilir.</summary>
    Draft,

    /// <summary>Onaylandı, üretime açık; içeriği kilitlidir.</summary>
    Released,

    /// <summary>Üretimi başladı (ilk demet üretime girdiğinde).</summary>
    InProduction,

    Completed,
    Cancelled,
}
