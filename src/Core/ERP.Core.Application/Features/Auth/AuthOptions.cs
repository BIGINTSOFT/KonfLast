using System.ComponentModel.DataAnnotations;

namespace ERP.Core.Application.Features.Auth;

public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    /// <summary>Hesap kilitlenmeden önce izin verilen ardışık hatalı giriş sayısı.</summary>
    [Range(1, 100)]
    public int MaxFailedLoginAttempts { get; set; } = 5;

    [Range(1, 1440)]
    public int LockoutMinutes { get; set; } = 15;

    /// <summary>"Beni hatırla" ile açılan oturumun (yenilenmeden) geçerli kaldığı gün.</summary>
    [Range(1, 365)]
    public int RememberMeDays { get; set; } = 30;

    /// <summary>"Beni hatırla" işaretlenmeden açılan oturumun (yenilenmeden) geçerli kaldığı saat.</summary>
    [Range(1, 168)]
    public int SessionHours { get; set; } = 12;

    /// <summary>"Şifremi unuttum" bağlantısının geçerli olduğu dakika.</summary>
    [Range(5, 1440)]
    public int PasswordResetMinutes { get; set; } = 30;

    /// <summary>Web uygulamasının adresi; parola sıfırlama bağlantısı bununla kurulur (örn. https://erp.firma.com.tr).</summary>
    public string WebBaseUrl { get; set; } = "http://localhost:5173";
}
