using ERP.Core.Infrastructure.Web;
using ERP.Core.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ERP.Api.Realtime;

/// <param name="Topics">Değişen alanlar.</param>
/// <param name="At">Bildirimin gönderildiği an (UTC).</param>
public sealed record LiveChangeMessage(IReadOnlyList<LiveTopic> Topics, DateTime At);

public interface IProductionHubClient
{
    /// <summary>Sunucuda kalıcı bir değişiklik oldu; istemci ilgili veriyi API'den yeniden okur.</summary>
    Task Changed(LiveChangeMessage message);
}

/// <summary>
/// Canlı üretim bildirimleri. Hub yalnızca "hangi alan değişti" bilgisini yayınlar; üretim verisi taşımaz
/// ve istemciden komut kabul etmez. Bu yüzden oturum açmış her kullanıcı bağlanabilir; veriyi okuyabilmek
/// ise ilgili API ucunun yetkisine bağlıdır. Tablet cihaz token'ı ile bağlanılamaz.
/// </summary>
[Authorize(Policy = AuthorizationPolicies.User)]
public sealed class ProductionHub : Hub<IProductionHubClient>
{
    public const string Path = "/hubs/production";
}
