using System.Reflection;
using ERP.Core.Domain.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ERP.Core.Infrastructure.Modules;

/// <summary>
/// Sektör modülünün (Konfeksiyon, Boyahane, Dokuma ...) çekirdeğe tanıttığı sözleşme. Modül assembly'si
/// API'ye derleme anında bağlı değildir: kurulumda bulunan modüller açılışta yüklenir, lisansta olmayan
/// modül hiç devreye alınmaz.
/// </summary>
public abstract class ErpModule
{
    /// <summary>Lisans ve yapılandırmada geçen sabit anahtar, örn. "Konfeksiyon".</summary>
    public abstract string Key { get; }

    /// <summary>Arayüzlerde görünen ad.</summary>
    public abstract string DisplayName { get; }

    /// <summary>Modül tablolarının veritabanı şeması (Core tablolarıyla aynı veritabanında).</summary>
    public virtual string Schema => Key;

    /// <summary>Modülün entity'lerinin bulunduğu assembly; bu assembly'deki entity'ler modülün şemasına yerleşir.</summary>
    public abstract Assembly DomainAssembly { get; }

    /// <summary>Modülün permission'ları (rol ekranı ve kurulum kontrolleri için).</summary>
    public virtual IReadOnlyList<PermissionDefinition> Permissions => [];

    /// <summary>Modülün uygulama ve altyapı servisleri, arka plan işleri.</summary>
    public abstract void AddServices(IServiceCollection services, IConfiguration configuration);

    /// <summary>Modülün EF konfigürasyonları. Varsayılan: modül assembly'sindeki tüm IEntityTypeConfiguration'lar.</summary>
    public virtual void ConfigureModel(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);
}
