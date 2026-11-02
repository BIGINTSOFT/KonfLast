namespace ERP.Konfeksiyon.Domain.Enums;

public enum CuttingPlanStatus
{
    /// <summary>Hazırlanıyor; pastal, oran ve toplar değiştirilebilir.</summary>
    Draft,

    /// <summary>Kesildi: gerçek kat ve adetler girildi, demetler açıldı, kumaş düşüldü. Değiştirilemez.</summary>
    Cut,
    Cancelled,
}
