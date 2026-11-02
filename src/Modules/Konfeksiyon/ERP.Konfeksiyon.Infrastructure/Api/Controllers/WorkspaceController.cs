using ERP.Core.Infrastructure.Web.Authorization;
using ERP.Core.Infrastructure.Web;
using ERP.Konfeksiyon.Application.Features.Workspace;
using ERP.Core.Domain.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Konfeksiyon.Infrastructure.Api.Controllers;

/// <summary>
/// Web'in ana sayfası, genel araması ve kurulum rehberi. Oturum açmış her kullanıcıya açıktır; sonuçlar
/// kullanıcının görme yetkisi olan modüllerle sınırlıdır.
/// </summary>
[Route("api/v1/workspace")]
public sealed class WorkspaceController(IWorkspaceService service) : ApiControllerBase
{
    /// <summary>Şu an bekleyen işler: onay bekleyen siparişler, kartsız demetler, mal kabul bekleyenler vb.</summary>
    [HttpGet("tasks")]
    [AnyAuthenticatedUser]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<WorkTaskDto>>>> GetTasks(CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetTasksAsync(cancellationToken), HttpContext));

    /// <summary>Kayıt araması (en az 2 karakter): sipariş, demet, satın alma, sevkiyat, personel, ürün, stok kartı, makine.</summary>
    [HttpGet("search")]
    [AnyAuthenticatedUser]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SearchHitDto>>>> Search(
        [FromQuery] string? q, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.SearchAsync(q, cancellationToken), HttpContext));

    /// <summary>İlk kurulum adımları ve tamamlanma durumu.</summary>
    [HttpGet("setup")]
    [HasPermission(Permissions.MasterData.View)]
    public async Task<ActionResult<ApiResponse<SetupStatusDto>>> GetSetup(CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetSetupStatusAsync(cancellationToken), HttpContext));
}
