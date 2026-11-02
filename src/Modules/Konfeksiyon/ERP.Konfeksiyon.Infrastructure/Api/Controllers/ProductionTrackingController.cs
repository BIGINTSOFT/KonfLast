using ERP.Konfeksiyon.Domain.Authorization;
using ERP.Core.Infrastructure.Web.Authorization;
using ERP.Core.Infrastructure.Web;
using ERP.Core.Application.Common;
using ERP.Konfeksiyon.Application.Features.ProductionTracking;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Konfeksiyon.Infrastructure.Api.Controllers;

[Route("api/v1")]
public sealed class ProductionTrackingController(IProductionTrackingService service, IPerformanceService performance)
    : ApiControllerBase
{
    /// <summary>
    /// Siparişin üretim ilerlemesi: sipariş / demetlenen / biten / hatalı adetler, operasyon ve renk-beden
    /// bazında döküm, siparişin tamamlanabilir olup olmadığı.
    /// </summary>
    [HttpGet("production-orders/{orderId:int}/progress")]
    [HasPermission(KonfeksiyonPermissions.ProductionOrder.View, KonfeksiyonPermissions.Production.View)]
    public async Task<ActionResult<ApiResponse<OrderProgressDto>>> GetOrderProgress(
        int orderId, CancellationToken cancellationToken) =>
        FromResult(await service.GetOrderProgressAsync(orderId, cancellationToken));

    /// <summary>Operasyon bazında ara stok: sahada bekleyen, işlemdeki ve yarım bırakılmış demetler.</summary>
    [HttpGet("production-tracking/wip")]
    [HasPermission(KonfeksiyonPermissions.Production.View)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<WipOperationDto>>>> GetWip(
        [FromQuery] WipQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetWipAsync(query, cancellationToken), HttpContext));

    /// <summary>Bir operasyonun önündeki / üzerindeki demetler (sayfalı; işlemdekiler üstte).</summary>
    [HttpGet("production-tracking/wip/bundles")]
    [HasPermission(KonfeksiyonPermissions.Production.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<WipBundleDto>>>> GetWipBundles(
        [FromQuery] WipBundleQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetWipBundlesAsync(query, cancellationToken), HttpContext));

    /// <summary>Aktif makinelerin şu anki durumu, başındaki operatör ve süren işi.</summary>
    [HttpGet("production-tracking/machines")]
    [HasPermission(KonfeksiyonPermissions.Production.View)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<MachineActivityDto>>>> GetMachines(
        CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetMachinesAsync(cancellationToken), HttpContext));

    /// <summary>
    /// Tarih aralığında (UTC; from dahil, to hariç) operatör, makine ya da operasyon bazında üretilen ve
    /// hatalı adet. Üretim olaylarından hesaplanır.
    /// </summary>
    [HttpGet("production-tracking/output")]
    [HasPermission(KonfeksiyonPermissions.Production.View)]
    public async Task<ActionResult<ApiResponse<OutputReportDto>>> GetOutput(
        [FromQuery] OutputQuery query, CancellationToken cancellationToken) =>
        FromResult(await service.GetOutputAsync(query, cancellationToken));

    /// <summary>
    /// Tarih aralığında (UTC; en fazla 31 gün) operatör, makine, bant ya da operasyon bazında performans:
    /// mevcudiyet, planlı mola, ara, makine arızası, demet bekleme, net çalışma, aktif üretim, üretilen / hatalı
    /// adet, kazanılan dakika (adet × SAM), hedef adet ve verim. Satırlar verimi en düşük olandan başlar.
    /// </summary>
    [HttpGet("production-tracking/performance")]
    [HasPermission(KonfeksiyonPermissions.Production.View)]
    public async Task<ActionResult<ApiResponse<PerformanceReportDto>>> GetPerformance(
        [FromQuery] PerformanceQuery query, CancellationToken cancellationToken) =>
        FromResult(await performance.GetAsync(query, cancellationToken));
}