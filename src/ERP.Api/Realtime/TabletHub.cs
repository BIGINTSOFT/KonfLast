using ERP.Core.Infrastructure.Security;
using ERP.Core.Infrastructure.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ERP.Api.Realtime;

/// <param name="At">Bildirimin gönderildiği an (UTC).</param>
public sealed record TabletChangeMessage(DateTime At);

public interface ITabletHubClient
{
    /// <summary>Tabletin makinesinde kalıcı bir değişiklik oldu; tablet ekranını API'den yeniden okur.</summary>
    Task Changed(TabletChangeMessage message);
}

/// <summary>
/// Tablet canlı bildirimleri. Her tablet yalnızca kendi grubuna eklenir ve yalnızca kendi makinesindeki
/// değişiklik duyurulur (ör. web'den elle adet bildirimi). Hub veri taşımaz, istemciden komut kabul etmez;
/// tablet güncel veriyi cihaz token'ıyla API'den okur. Kullanıcı token'ı ile bağlanılamaz.
/// </summary>
[Authorize(Policy = AuthorizationPolicies.AnyTablet)]
public sealed class TabletHub : Hub<ITabletHubClient>
{
    public const string Path = "/hubs/tablet";

    public static string GroupOf(string module, int tabletId) => $"tablet:{module}:{tabletId}";

    public override async Task OnConnectedAsync()
    {
        var module = Context.User?.FindFirst(JwtTokenService.TabletModuleClaimType)?.Value;
        var tabletId = Context.User?.FindFirst(JwtTokenService.TabletIdClaimType)?.Value;
        if (string.IsNullOrEmpty(module) || !int.TryParse(tabletId, out var id))
        {
            Context.Abort();
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupOf(module, id));
        await base.OnConnectedAsync();
    }
}
