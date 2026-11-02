using ERP.Core.Domain.Common;

namespace ERP.Core.Domain.Entities.Identity;

/// <summary>
/// Refresh token'ın kendisi saklanmaz, sadece SHA-256 hash'i tutulur.
/// Her yenilemede token döndürülür (rotation); iptal edilmiş bir token tekrar
/// kullanılırsa kullanıcının tüm oturumları kapatılır.
/// </summary>
[NotAudited]
public class RefreshToken : BaseEntity
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public string TokenHash { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string? CreatedByIp { get; set; }
    public string? DeviceInfo { get; set; }

    /// <summary>"Beni hatırla" ile açılmış kalıcı oturum; yenilemede de kalıcı kalır ve daha uzun geçerlidir.</summary>
    public bool IsPersistent { get; set; }

    public DateTime? RevokedAt { get; private set; }
    public string? RevokedReason { get; private set; }
    public string? ReplacedByTokenHash { get; private set; }

    /// <summary>Aynı token'ın eşzamanlı iki kez döndürülmesini engeller.</summary>
    public byte[] RowVersion { get; set; } = null!;

    public bool IsRevoked => RevokedAt.HasValue;
    public bool IsExpired(DateTime utcNow) => ExpiresAt <= utcNow;
    public bool IsActive(DateTime utcNow) => !IsRevoked && !IsExpired(utcNow);

    public void Revoke(DateTime utcNow, string reason, string? replacedByTokenHash = null)
    {
        if (IsRevoked)
        {
            return;
        }

        RevokedAt = utcNow;
        RevokedReason = reason;
        ReplacedByTokenHash = replacedByTokenHash;
    }
}

public static class RefreshTokenRevokeReasons
{
    public const string Rotated = "ROTATED";
    public const string Logout = "LOGOUT";
    public const string ReuseDetected = "REUSE_DETECTED";
    public const string PasswordChanged = "PASSWORD_CHANGED";
    public const string UserDeactivated = "USER_DEACTIVATED";
    public const string UserDeleted = "USER_DELETED";
    public const string PasswordReset = "PASSWORD_RESET";
}
