using ERP.Core.Application.Abstractions;
using ERP.Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace ERP.Core.Infrastructure.Security;

/// <summary>
/// Rol → permission eşlemesini veritabanından okur. Her istekte sorgu çalışmaması için
/// tablonun tamamı (küçük bir tablo) kısa süreli bellek cache'inde tutulur.
/// </summary>
public sealed class PermissionService(AppDbContext db, IMemoryCache cache, IActivePermissions active) : IPermissionService
{
    private const string CacheKey = "auth:role-permissions";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public async Task<IReadOnlySet<string>> GetPermissionsAsync(
        IEnumerable<string> roleCodes, CancellationToken cancellationToken = default)
    {
        var byRole = await cache.GetOrCreateAsync(CacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;

            var rows = await db.RolePermissions
                .AsNoTracking()
                .Select(rp => new { Role = rp.Role.Code, Permission = rp.Permission.Code })
                .ToListAsync(cancellationToken);

            return rows
                .GroupBy(r => r.Role)
                .ToDictionary(g => g.Key, g => g.Select(r => r.Permission).Where(active.Contains).ToHashSet());
        });

        var permissions = new HashSet<string>();
        foreach (var role in roleCodes)
        {
            if (byRole!.TryGetValue(role, out var rolePermissions))
            {
                permissions.UnionWith(rolePermissions);
            }
        }

        return permissions;
    }

    public void InvalidateCache() => cache.Remove(CacheKey);
}
