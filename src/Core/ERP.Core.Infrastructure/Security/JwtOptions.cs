using System.ComponentModel.DataAnnotations;

namespace ERP.Core.Infrastructure.Security;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Issuer { get; set; } = null!;

    [Required]
    public string Audience { get; set; } = null!;

    /// <summary>HS256 imza anahtarı; en az 32 byte. Production'da ortam değişkeninden verilmelidir.</summary>
    [Required, MinLength(32)]
    public string Key { get; set; } = null!;

    [Range(1, 1440)]
    public int AccessTokenMinutes { get; set; } = 15;

    [Range(1, 365)]
    public int RefreshTokenDays { get; set; } = 7;
}
