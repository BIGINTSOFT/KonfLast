using ERP.Core.Domain.Common;

namespace ERP.Core.Domain.Entities.Identity;

public class User : SoftDeletableEntity
{
    public string UserName { get; set; } = null!;

    /// <summary>Büyük/küçük harf ve kültürden bağımsız benzersizlik için (ToUpperInvariant).</summary>
    public string NormalizedUserName { get; set; } = null!;

    public string FullName { get; set; } = null!;
    public string? Email { get; set; }
    [AuditMasked] public string PasswordHash { get; set; } = null!;
    public bool IsActive { get; set; } = true;

    // Her girişte değişen sayaçlar audit log'u doldurmasın; girişler uygulama loglarında izlenir.
    [AuditIgnore] public int FailedLoginCount { get; private set; }
    [AuditIgnore] public DateTime? LockoutEndAt { get; private set; }
    [AuditIgnore] public DateTime? LastLoginAt { get; private set; }

    public ICollection<UserRole> UserRoles { get; } = new List<UserRole>();
    public ICollection<RefreshToken> RefreshTokens { get; } = new List<RefreshToken>();

    public static string NormalizeUserName(string userName) => userName.Trim().ToUpperInvariant();

    public bool IsLockedOut(DateTime utcNow) => LockoutEndAt.HasValue && LockoutEndAt.Value > utcNow;

    /// <summary>Hatalı girişi kaydeder; limit dolduysa hesabı kilitler ve true döner.</summary>
    public bool RegisterFailedLogin(DateTime utcNow, int maxFailedAttempts, TimeSpan lockoutDuration)
    {
        FailedLoginCount++;
        if (FailedLoginCount < maxFailedAttempts)
        {
            return false;
        }

        LockoutEndAt = utcNow.Add(lockoutDuration);
        FailedLoginCount = 0;
        return true;
    }

    /// <summary>Yönetici parolayı sıfırladığında kilit de kalkar.</summary>
    public void ClearLockout()
    {
        FailedLoginCount = 0;
        LockoutEndAt = null;
    }

    public void RegisterSuccessfulLogin(DateTime utcNow)
    {
        FailedLoginCount = 0;
        LockoutEndAt = null;
        LastLoginAt = utcNow;
    }
}
