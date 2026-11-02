using ERP.Core.Application.Features.Workspace;
using ERP.Core.Infrastructure.Web;
using ERP.Core.Infrastructure.Web.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Api.Controllers;

/// <summary>Web ana sayfasının kullanıcıya özel kısayolları. Oturum açmış her kullanıcıya açıktır.</summary>
[Route("api/v1/workspace/shortcuts")]
public sealed class ShortcutsController(IShortcutService service) : ApiControllerBase
{
    /// <summary>Oturumdaki kullanıcının ana sayfa kısayolları: ekran adresleri, gösterilecekleri sırayla.</summary>
    [HttpGet]
    [AnyAuthenticatedUser]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<string>>>> GetShortcuts(CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetShortcutsAsync(cancellationToken), HttpContext));

    /// <summary>Kullanıcının kısayollarını verilen listeyle değiştirir (en fazla 24; yalnızca uygulama içi adresler).</summary>
    [HttpPut]
    [AnyAuthenticatedUser]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<string>>>> SaveShortcuts(
        SaveShortcutsRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.SaveShortcutsAsync(request, cancellationToken));
}
