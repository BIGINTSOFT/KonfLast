using ERP.Core.Domain.Entities.Identity;

namespace ERP.Core.Application.Abstractions;

/// <summary>İsteği yapan kullanıcı ve cihaz bilgisi.</summary>
public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    int? UserId { get; }

    /// <summary>İstek bir tabletten (cihaz token'ı ile) geliyorsa o tablet.</summary>
    int? TabletId { get; }
    string? UserName { get; }
    IReadOnlyCollection<string> Roles { get; }
    string? IpAddress { get; }
    string? DeviceInfo { get; }
    string? CorrelationId { get; }
}

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string passwordHash, string password);
}

public sealed record AccessToken(string Token, DateTime ExpiresAt);

public sealed record GeneratedRefreshToken(string Token, string TokenHash, DateTime ExpiresAt);

public interface ITokenService
{
    AccessToken CreateAccessToken(User user, IReadOnlyCollection<string> roleCodes);

    /// <summary>Tablet cihazı için kısa ömürlü token; kullanıcı yetkisi taşımaz, yalnızca tablet uçlarında geçerlidir.</summary>
    /// <param name="moduleKey">Tabletin projesi; token yalnızca o projenin tablet uçlarında geçer.</param>
    AccessToken CreateTabletAccessToken(int tabletId, string tabletCode, string moduleKey);
    GeneratedRefreshToken CreateRefreshToken();
    string HashRefreshToken(string refreshToken);
}

public interface IDocumentNumberGenerator
{
    /// <summary>
    /// Verilen seri ve yıl için sıradaki numarayı döner. Numarayı kullanan kayıtla aynı transaction
    /// içinde çağrılmalıdır; transaction geri alınırsa numara da geri alınır.
    /// </summary>
    Task<int> NextAsync(string series, int year, CancellationToken cancellationToken);

    /// <summary>
    /// Ardışık <paramref name="count"/> numarayı tek seferde ayırır ve ilkini döner (toplu kayıtlar için).
    /// </summary>
    Task<int> NextRangeAsync(string series, int year, int count, CancellationToken cancellationToken);
}

public interface IAppLock
{
    /// <summary>
    /// Adı verilen kaynak için özel (exclusive) kilit alır; kilit, içinde bulunulan transaction bitince
    /// bırakılır. "Kontrol et, sonra yaz" türü kuralların eşzamanlı isteklerde delinmemesi için kullanılır.
    /// Süre içinde alınamazsa false döner.
    /// </summary>
    Task<bool> TryAcquireAsync(string resource, TimeSpan timeout, CancellationToken cancellationToken);
}

/// <summary>
/// Bu kurulumda devrede olan permission kodları: çekirdeğinkiler ve lisanslı / açık modüllerinkiler. Veritabanında
/// kurulu ama devrede olmayan bir modülün yetkileri kimseye verilmez ve rol ekranında görünmez.
/// </summary>
public interface IActivePermissions
{
    bool Contains(string code);

    /// <summary>Yetkinin ait olduğu modül; çekirdek yetkide null.</summary>
    string? ModuleOf(string code);
}

public interface IPermissionService
{
    /// <summary>Verilen rollerin sahip olduğu permission kodlarının birleşimi (cache'li).</summary>
    Task<IReadOnlySet<string>> GetPermissionsAsync(
        IEnumerable<string> roleCodes, CancellationToken cancellationToken = default);

    /// <summary>Rol yetkileri değiştiğinde cache'i temizler.</summary>
    void InvalidateCache();
}

/// <summary>
/// İsteği yapan kullanıcının yetkileri. Uç noktanın yetkisi dışındaki alan bazlı kurallar için: maliyet / fiyat
/// alanlarını gizleme, belirli bir alanın değiştirilmesine izin verme. İstek boyunca bir kez okunur.
/// </summary>
public interface ICurrentPermissions
{
    Task<bool> HasAsync(string permission, CancellationToken cancellationToken = default);
}

public sealed class CurrentPermissions(ICurrentUser currentUser, IPermissionService permissions) : ICurrentPermissions
{
    private IReadOnlySet<string>? _granted;

    public async Task<bool> HasAsync(string permission, CancellationToken cancellationToken = default)
    {
        // Tablet istekleri ve arka plan işleri kullanıcı rolü taşımaz: yetki yoktur.
        _granted ??= currentUser.Roles.Count == 0
            ? new HashSet<string>()
            : await permissions.GetPermissionsAsync(currentUser.Roles, cancellationToken);
        return _granted.Contains(permission);
    }
}
