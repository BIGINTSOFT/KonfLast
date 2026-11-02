using ERP.Core.Domain.Common;

namespace ERP.Core.Domain.Entities.Identity;

/// <summary>
/// "Şifremi unuttum" bağlantısı. Token'ın kendisi saklanmaz, SHA-256 hash'i tutulur; süreli ve tek kullanımlıktır.
/// Yeni istek, kullanıcının önceki kullanılmamış bağlantılarını geçersiz kılar.
/// </summary>
[NotAudited]
public class PasswordResetToken : BaseEntity
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public string TokenHash { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string? RequestedByIp { get; set; }

    /// <summary>Parola bu bağlantıyla değiştirildi ya da bağlantı yenisiyle geçersiz kılındı.</summary>
    public DateTime? UsedAt { get; set; }

    public bool IsUsable(DateTime utcNow) => UsedAt is null && ExpiresAt > utcNow;
}
