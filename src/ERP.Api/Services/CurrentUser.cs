using ERP.Core.Application.Abstractions;
using ERP.Core.Infrastructure.Security;
using Microsoft.IdentityModel.JsonWebTokens;

namespace ERP.Api.Services;

public sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private const int MaxDeviceInfoLength = 300;

    private HttpContext? Context => httpContextAccessor.HttpContext;

    public bool IsAuthenticated => Context?.User.Identity?.IsAuthenticated ?? false;

    public int? UserId =>
        int.TryParse(Context?.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var id) ? id : null;

    public int? TabletId =>
        int.TryParse(Context?.User.FindFirst(JwtTokenService.TabletIdClaimType)?.Value, out var id) ? id : null;

    public string? UserName => Context?.User.FindFirst(JwtRegisteredClaimNames.UniqueName)?.Value;

    public IReadOnlyCollection<string> Roles =>
        Context?.User.FindAll(JwtTokenService.RoleClaimType).Select(c => c.Value).ToArray() ?? [];

    public string? IpAddress => Context?.Connection.RemoteIpAddress?.ToString();

    public string? CorrelationId => Context?.TraceIdentifier;

    public string? DeviceInfo
    {
        get
        {
            var userAgent = Context?.Request.Headers.UserAgent.ToString();
            if (string.IsNullOrWhiteSpace(userAgent))
            {
                return null;
            }

            return userAgent.Length <= MaxDeviceInfoLength ? userAgent : userAgent[..MaxDeviceInfoLength];
        }
    }
}
