using ERP.Core.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace ERP.Core.Application.Features.Auth;

public static class SessionRevocation
{
    /// <summary>
    /// Kullanıcının tüm açık oturumlarını (refresh token) kapatır. Mevcut access token'lar
    /// kısa ömürleri dolana kadar geçerli kalır. Çağıran taraf bunu ilgili değişiklikle
    /// aynı transaction içinde çalıştırmalıdır.
    /// </summary>
    public static Task<int> RevokeUserSessionsAsync(
        this IAppDbContext db, int userId, string reason, DateTime utcNow, CancellationToken cancellationToken) =>
        db.RefreshTokens
            .IgnoreQueryFilters() // silinmiş kullanıcının token'ları da kapsansın
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ExecuteUpdateAsync(
                update => update
                    .SetProperty(t => t.RevokedAt, utcNow)
                    .SetProperty(t => t.RevokedReason, reason),
                cancellationToken);
}
