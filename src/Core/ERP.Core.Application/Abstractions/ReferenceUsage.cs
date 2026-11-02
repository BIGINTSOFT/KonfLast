namespace ERP.Core.Application.Abstractions;

/// <summary>
/// Bir modülün, bir kaydın (para birimi; modül içinde müşteri, ürün, renk ...) kendi verisinde kullanılıp
/// kullanılmadığını bildirmesi. Silme ve benzeri işlemlerde modüllerin tabloları bilinmeden bu problar sorulur;
/// kurulu olmayan modül sorulmaz.
/// </summary>
public interface IReferenceUsageProbe
{
    /// <param name="entityType">Sorulan tip, örn. <c>typeof(Currency)</c>.</param>
    /// <param name="key">Kaydın kimliği: çoğunlukla Id, para biriminde kod.</param>
    /// <returns>Kaydın kullanıldığı yerin okunur adı (örn. "üretim siparişleri") ya da kullanılmıyorsa null.</returns>
    Task<string?> FindUsageAsync(Type entityType, object key, CancellationToken cancellationToken);
}

/// <summary>Kurulu tüm modüllerin problarını sırayla sorar.</summary>
public interface IReferenceUsage
{
    Task<string?> FindAsync<TEntity>(object key, CancellationToken cancellationToken) where TEntity : class;
}

public sealed class ReferenceUsage(IEnumerable<IReferenceUsageProbe> probes) : IReferenceUsage
{
    public async Task<string?> FindAsync<TEntity>(object key, CancellationToken cancellationToken) where TEntity : class
    {
        foreach (var probe in probes)
        {
            if (await probe.FindUsageAsync(typeof(TEntity), key, cancellationToken) is { } usage)
            {
                return usage;
            }
        }

        return null;
    }
}
