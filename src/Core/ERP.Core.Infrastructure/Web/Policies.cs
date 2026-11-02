namespace ERP.Core.Infrastructure.Web;

public static class RateLimitPolicies
{
    public const string Auth = "auth";
}

public static class AuthorizationPolicies
{
    /// <summary>Yalnızca Konfeksiyon tabletinin cihaz token'ı ile gelen istekler.</summary>
    public const string TabletDevice = "TabletDevice";

    /// <summary>Yalnızca Boyahane tabletinin cihaz token'ı ile gelen istekler.</summary>
    public const string BoyahaneTablet = "BoyahaneTablet";
    public const string DokumaTablet = "DokumaTablet";
    public const string OrmeTablet = "OrmeTablet";

    /// <summary>Oturum açmış kullanıcı (tablet cihaz token'ı değil). Varsayılan (fallback) policy ile aynıdır.</summary>
    public const string User = "User";
}

public static class CorsPolicies
{
    public const string Web = "web";
}
