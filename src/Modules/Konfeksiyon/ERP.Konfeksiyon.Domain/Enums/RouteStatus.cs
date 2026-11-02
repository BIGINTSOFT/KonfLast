namespace ERP.Konfeksiyon.Domain.Enums;

public enum RouteStatus
{
    /// <summary>Hazırlanıyor; düzenlenebilir ve silinebilir.</summary>
    Draft,

    /// <summary>Modelin geçerli rotası; içeriği kilitlidir.</summary>
    Active,

    /// <summary>Yerini yeni bir sürüme bırakmış ya da kullanımdan kaldırılmış rota.</summary>
    Archived,
}
