using ERP.Core.Infrastructure.Licensing;

namespace ERP.Core.Infrastructure.Modules;

/// <summary>Bu kurulumda devrede olan modüller (açılışta bir kez belirlenir, uygulama boyunca değişmez).</summary>
public sealed class ErpModuleCatalog(
    IReadOnlyList<ErpModule> modules,
    IReadOnlyList<ErpModule>? installed = null,
    LicenseCheck? license = null)
{
    public static readonly ErpModuleCatalog Empty = new([]);

    /// <summary>Devredeki modüller (kurulu, lisanslı ve yapılandırmada açık).</summary>
    public IReadOnlyList<ErpModule> Modules { get; } = modules;

    /// <summary>Kurulum klasöründe bulunan modüller (lisans süzmesinden önce).</summary>
    public IReadOnlyList<ErpModule> Installed { get; } = installed ?? modules;

    /// <summary>Lisans durumu; doğrudan kurulan kataloglarda yoktur.</summary>
    public LicenseCheck? License { get; } = license;

    public bool IsActive(string key) => Modules.Any(m => string.Equals(m.Key, key, StringComparison.OrdinalIgnoreCase));

    /// <summary>Model önbelleği ve log için kısa imza, örn. "Konfeksiyon,Boyahane".</summary>
    public string Signature => string.Join(',', Modules.Select(m => m.Key));
}
