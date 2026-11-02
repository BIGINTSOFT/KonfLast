using ERP.Konfeksiyon.Domain.Authorization;
using ERP.Core.Infrastructure.Web.Authorization;
using ERP.Core.Infrastructure.Web;
using ERP.Konfeksiyon.Application.Features.ProductionTracking;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Konfeksiyon.Infrastructure.Api.Controllers;

[Route("api/v1/dashboard")]
[HasPermission(KonfeksiyonPermissions.Dashboard.View)]
public sealed class DashboardController(ILiveDashboardService service) : ApiControllerBase
{
    /// <summary>
    /// Canlı pano: bant bazında yürüyen plan, bugünkü üretim / hedef / verim ve makinelerin anlık durumu;
    /// sahadaki demetlerin durumu ve operasyon önündeki ara stok. Değişiklikler /hubs/production
    /// SignalR hub'ından "Changed" mesajıyla duyurulur; istemci bu ucu yeniden okur.
    /// </summary>
    [HttpGet("live")]
    public async Task<ActionResult<ApiResponse<LiveDashboardDto>>> GetLive(CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetAsync(cancellationToken), HttpContext));
}
