using System.Buffers.Text;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ERP.Core.Application.Abstractions;
using ERP.Core.Domain.Entities.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace ERP.Core.Infrastructure.Security;

public sealed class JwtTokenService(IOptions<JwtOptions> options, TimeProvider timeProvider) : ITokenService
{
    public const string RoleClaimType = "role";
    public const string TabletIdClaimType = "tablet_id";
    public const string TabletDeviceRole = "TABLET_DEVICE";

    /// <summary>Tabletin ait olduğu proje; tablet uçları buna göre ayrılır.</summary>
    public const string TabletModuleClaimType = "tablet_module";

    private static readonly JsonWebTokenHandler Handler = new();
    private readonly JwtOptions _options = options.Value;

    public AccessToken CreateAccessToken(User user, IReadOnlyCollection<string> roleCodes)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, user.UserName),
            new(JwtRegisteredClaimNames.Name, user.FullName),
        };
        claims.AddRange(roleCodes.Select(role => new Claim(RoleClaimType, role)));

        return CreateToken(claims);
    }

    public AccessToken CreateTabletAccessToken(int tabletId, string tabletCode, string moduleKey) => CreateToken(
    [
        // sub sayısal değildir: tablet token'ı hiçbir zaman bir kullanıcı kimliğine çözülmez.
        new(JwtRegisteredClaimNames.Sub, $"tablet:{tabletId}"),
        new(JwtRegisteredClaimNames.UniqueName, tabletCode),
        new(TabletIdClaimType, tabletId.ToString()),
        new(TabletModuleClaimType, moduleKey),
        new(RoleClaimType, TabletDeviceRole),
    ]);

    private AccessToken CreateToken(List<Claim> claims)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var expiresAt = now.AddMinutes(_options.AccessTokenMinutes);
        claims.Add(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")));

        var token = Handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            Subject = new ClaimsIdentity(claims),
            IssuedAt = now,
            NotBefore = now,
            Expires = expiresAt,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key)), SecurityAlgorithms.HmacSha256),
        });

        return new AccessToken(token, expiresAt);
    }

    public GeneratedRefreshToken CreateRefreshToken()
    {
        var token = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(64));
        var expiresAt = timeProvider.GetUtcNow().UtcDateTime.AddDays(_options.RefreshTokenDays);
        return new GeneratedRefreshToken(token, HashRefreshToken(token), expiresAt);
    }

    public string HashRefreshToken(string refreshToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
}
