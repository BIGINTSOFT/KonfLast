using ERP.Core.Infrastructure.Licensing;
using Microsoft.Extensions.Configuration;
using System.Reflection;
using System.Runtime.Loader;

namespace ERP.Core.Infrastructure.Modules;

/// <summary>
/// Kurulum klasöründeki modül assembly'lerini bulur ve yükler. Modüller uygulama klasöründe ya da
/// <c>modules</c> alt klasöründe <c>ERP.&lt;Modül&gt;.Infrastructure.dll</c> olarak durur; satın alınmayan
/// modülün dosyaları kuruluma hiç konmaz.
/// </summary>
public static class ErpModuleLoader
{
    public const string ModulesFolder = "modules";
    private const string CoreInfrastructure = "ERP.Core.Infrastructure";

    private static int _resolverInstalled;

    /// <summary>
    /// Devreye alınacak modüller: kurulumda bulunan VE lisansta yazan modüller; <c>Modules:Enabled</c> verilmişse
    /// ayrıca yalnızca o listedekiler (geliştirmede "yalnızca Konfeksiyon" gibi çalıştırma profilleri için).
    /// Lisans geçersizse hiçbir modül devreye alınmaz; çekirdek çalışır, durum <see cref="ErpModuleCatalog.License"/>'tadır.
    /// </summary>
    public static ErpModuleCatalog Load(IConfiguration configuration, string? baseDirectory = null)
    {
        var installed = Discover(baseDirectory);
        var license = LicenseValidator.Check(configuration, baseDirectory);

        var modules = installed.Where(m => license.Allows(m.Key)).ToList();

        var enabled = configuration.GetSection("Modules:Enabled").Get<string[]>();
        if (enabled is { Length: > 0 })
        {
            modules = modules.Where(m => enabled.Contains(m.Key, StringComparer.OrdinalIgnoreCase)).ToList();
        }

        return new ErpModuleCatalog(modules, installed, license);
    }

    /// <summary>Bulunan tüm modüller (lisans ve yapılandırma süzmesi uygulanmadan).</summary>
    public static IReadOnlyList<ErpModule> Discover(string? baseDirectory = null)
    {
        baseDirectory ??= AppContext.BaseDirectory;
        var directories = new[] { baseDirectory, Path.Combine(baseDirectory, ModulesFolder) }
            .Where(Directory.Exists)
            .ToList();
        InstallResolver(directories);

        var modules = new List<ErpModule>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in directories.SelectMany(d => Directory.EnumerateFiles(d, "ERP.*.Infrastructure.dll")))
        {
            var name = Path.GetFileNameWithoutExtension(file);
            if (name.Equals(CoreInfrastructure, StringComparison.OrdinalIgnoreCase) || !seen.Add(name))
            {
                continue;
            }

            var assembly = LoadAssembly(name, file);
            modules.AddRange(assembly.GetExportedTypes()
                .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(ErpModule).IsAssignableFrom(t))
                .Select(t => (ErpModule)Activator.CreateInstance(t)!));
        }

        return modules.OrderBy(m => m.Key, StringComparer.Ordinal).ToList();
    }

    private static Assembly LoadAssembly(string name, string path)
    {
        // Derleme zaten yüklenmişse aynı kopya kullanılır; aksi halde dosyadan yüklenir.
        var loaded = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == name);
        if (loaded is not null)
        {
            return loaded;
        }

        try
        {
            return Assembly.Load(new AssemblyName(name));
        }
        catch (FileNotFoundException)
        {
            return AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(path));
        }
    }

    /// <summary>Modülün kendi Domain / Application assembly'leri aynı klasörden çözülür.</summary>
    private static void InstallResolver(IReadOnlyList<string> directories)
    {
        if (Interlocked.Exchange(ref _resolverInstalled, 1) == 1)
        {
            return;
        }

        AssemblyLoadContext.Default.Resolving += (context, assemblyName) =>
        {
            foreach (var directory in directories)
            {
                var candidate = Path.Combine(directory, assemblyName.Name + ".dll");
                if (File.Exists(candidate))
                {
                    return context.LoadFromAssemblyPath(Path.GetFullPath(candidate));
                }
            }

            return null;
        };
    }
}
