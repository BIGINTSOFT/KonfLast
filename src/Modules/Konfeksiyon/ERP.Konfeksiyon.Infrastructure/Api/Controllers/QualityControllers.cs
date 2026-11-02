using ERP.Konfeksiyon.Domain.Authorization;
using ERP.Core.Infrastructure.Web.Authorization;
using ERP.Core.Infrastructure.Web;
using ERP.Core.Application.Common;
using ERP.Core.Application.Features.MasterData;
using ERP.Konfeksiyon.Application.Features.Quality;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Konfeksiyon.Infrastructure.Api.Controllers;

[Route("api/v1/defect-types")]
[ResourcePermissions(KonfeksiyonPermissions.Quality.View, KonfeksiyonPermissions.Quality.ManageDefectTypes)]
public sealed class DefectTypesController(IMasterDataService<DefectTypeDto, DefectTypeRequest> service)
    : MasterDataController<DefectTypeDto, DefectTypeRequest>(service);

[Route("api/v1")]
public sealed class QualityController(IQualityService service) : ApiControllerBase
{
    /// <summary>Kalite kontrolleri, en yeniden eskiye (sayfalı; demet / sipariş no, sonuç, tarih filtresi).</summary>
    [HttpGet("quality/inspections")]
    [HasPermission(KonfeksiyonPermissions.Quality.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<InspectionDto>>>> GetInspections(
        [FromQuery] InspectionListQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetInspectionsAsync(query, cancellationToken), HttpContext));

    /// <summary>
    /// Kalite kontrolü kaydeder. Ret: parçalar operasyonun "hatalı" adedine geçer. Tamir: parçalar operasyona
    /// yeniden işlenmek üzere döner ve bir tamir işi açılır. Bekletme: demetin hiçbir operasyonu başlatılamaz.
    /// Hatalar: QUALITY_QUANTITY_INVALID, QUALITY_CONSUMED_DOWNSTREAM, WORK_IN_PROGRESS, REWORK_NOT_DONE,
    /// BUNDLE_NOT_ACTIVE.
    /// </summary>
    [HttpPost("quality/inspections")]
    [HasPermission(KonfeksiyonPermissions.Quality.Create)]
    public async Task<ActionResult<ApiResponse<InspectionDto>>> Inspect(
        InspectionRequest request, CancellationToken cancellationToken)
    {
        var result = await service.InspectAsync(request, cancellationToken);
        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, ApiResponse.Success(result.Value, HttpContext))
            : FromResult(result);
    }

    /// <summary>Tamir işleri, en yeniden eskiye (sayfalı; durum / bekleyenler / demet filtresi).</summary>
    [HttpGet("quality/rework-tasks")]
    [HasPermission(KonfeksiyonPermissions.Quality.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<ReworkTaskDto>>>> GetReworkTasks(
        [FromQuery] ReworkTaskListQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetReworkTasksAsync(query, cancellationToken), HttpContext));

    /// <summary>Tarih aralığında (UTC; from dahil, to hariç) hata türlerine göre dağılım ve hata oranı.</summary>
    [HttpGet("quality/defect-summary")]
    [HasPermission(KonfeksiyonPermissions.Quality.View)]
    public async Task<ActionResult<ApiResponse<DefectSummaryDto>>> GetDefectSummary(
        [FromQuery] DefectSummaryQuery query, CancellationToken cancellationToken) =>
        FromResult(await service.GetDefectSummaryAsync(query, cancellationToken));

    /// <summary>Demetin kalite durumu: kontrol girilebilecek operasyonlar, kontrol geçmişi ve tamir işleri.</summary>
    [HttpGet("bundles/{bundleId:int}/quality")]
    [HasPermission(KonfeksiyonPermissions.Quality.View, KonfeksiyonPermissions.Bundle.View)]
    public async Task<ActionResult<ApiResponse<BundleQualityDto>>> GetBundleQuality(
        int bundleId, CancellationToken cancellationToken) =>
        FromResult(await service.GetBundleAsync(bundleId, cancellationToken));

    /// <summary>Demeti kalite bekletmesinden çıkarır; operasyonları yeniden başlatılabilir.</summary>
    [HttpPost("bundles/{bundleId:int}/quality/release-hold")]
    [HasPermission(KonfeksiyonPermissions.Quality.ReleaseHold)]
    public async Task<ActionResult<ApiResponse<BundleQualityDto>>> ReleaseHold(
        int bundleId, CancellationToken cancellationToken) =>
        FromResult(await service.ReleaseHoldAsync(bundleId, cancellationToken));
}
