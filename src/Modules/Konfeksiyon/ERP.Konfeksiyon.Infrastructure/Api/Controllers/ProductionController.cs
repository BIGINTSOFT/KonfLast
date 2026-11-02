using ERP.Konfeksiyon.Domain.Authorization;
using ERP.Core.Infrastructure.Web.Authorization;
using ERP.Core.Infrastructure.Web;
using ERP.Konfeksiyon.Application.Features.Production;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Konfeksiyon.Infrastructure.Api.Controllers;

/// <summary>
/// Tabletten yürütülen üretim işlemleri (tablet token'ı gerekir). Makine, isteği yapan tabletin bağlı
/// olduğu makinedir. İstekler <c>requestId</c> ile bir kez işlenir; tekrar gelen istek mevcut durumu
/// <c>duplicate: true</c> ile döner.
/// </summary>
[Route("api/v1/production")]
[Authorize(Policy = AuthorizationPolicies.TabletDevice)]
public sealed class ProductionController(IProductionService service) : ApiControllerBase
{
    /// <summary>
    /// Demet operasyonunu bu makinede başlatır ya da kalan adet için sürdürür. Hatalar: OPERATION_MACHINE_MISMATCH,
    /// PREDECESSORS_NOT_COMPLETED, OPERATION_ALREADY_COMPLETED, BUNDLE_IN_USE, MACHINE_BUSY, OPERATOR_BUSY,
    /// MACHINE_NOT_AVAILABLE, OPERATOR_INACTIVE, BUNDLE_NOT_RELEASED, BUNDLE_NOT_ACTIVE, BUNDLE_ON_QUALITY_HOLD,
    /// ORDER_NOT_IN_PRODUCTION.
    /// </summary>
    [HttpPost("bundle-operations/{id:long}/start")]
    public async Task<ActionResult<ApiResponse<OperationProgressDto>>> Start(
        long id, StartOperationRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.StartAsync(id, request, cancellationToken));

    /// <summary>
    /// İşlemdeki operasyon için üretilen / hatalı adedi bildirir. Kalan adet biterse operasyon tamamlanır ve
    /// sonraki operasyonlar açılır; kalırsa operasyon bekletilir ve demet yeniden okutulunca kalandan devam edilir.
    /// Hatalar: OPERATION_NOT_IN_PROGRESS, OPERATION_ON_ANOTHER_MACHINE, QUANTITY_EXCEEDS_REMAINING.
    /// </summary>
    [HttpPost("bundle-operations/{id:long}/report")]
    public async Task<ActionResult<ApiResponse<OperationProgressDto>>> Report(
        long id, ReportOperationRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.ReportAsync(id, request, cancellationToken));

    /// <summary>Bu makinede şu an işlemde olan operasyon; yoksa <c>data: null</c>.</summary>
    [HttpGet("current")]
    public async Task<ActionResult<ApiResponse<OperationProgressDto?>>> GetCurrent(CancellationToken cancellationToken) =>
        FromResult(await service.GetCurrentAsync(cancellationToken));
}

[Route("api/v1")]
public sealed class BundleEventsController(IProductionService service) : ApiControllerBase
{
    /// <summary>
    /// Ofisten: işlemde kalmış operasyonu adet bildirmeden bırakır. Tablet arızalandığında, başka makineye
    /// bağlandığında ya da operatör adet bildirmeden ayrıldığında demet, makine ve operatör kilitli kalmasın diye.
    /// Operasyon kalan adediyle "bekliyor"a döner. Hata: OPERATION_NOT_IN_PROGRESS.
    /// </summary>
    [HttpPost("bundle-operations/{id:long}/release")]
    [HasPermission(KonfeksiyonPermissions.Production.ReleaseJob)]
    public async Task<ActionResult<ApiResponse<ReleasedOperationDto>>> Release(long id, CancellationToken cancellationToken) =>
        FromResult(await service.ReleaseAsync(id, cancellationToken));

    /// <summary>Elle bildirim penceresi: operasyonun durumu ve operasyonu yapabilen makineler.</summary>
    [HttpGet("bundle-operations/{id:long}/manual-report")]
    [HasPermission(KonfeksiyonPermissions.Production.ManualReport)]
    public async Task<ActionResult<ApiResponse<ManualReportOptionsDto>>> GetManualReportOptions(
        long id, CancellationToken cancellationToken) =>
        FromResult(await service.GetManualReportOptionsAsync(id, cancellationToken));

    /// <summary>
    /// Ofisten, tablet ve kart olmadan adet bildirir (askıda kalan iş, arızalı tablet). Olaylar kaydı giren
    /// kullanıcıyla işaretlenir. Hatalar: QUANTITY_EXCEEDS_REMAINING, PREDECESSORS_NOT_COMPLETED, BUNDLE_IN_USE,
    /// BUNDLE_NOT_ACTIVE, BUNDLE_ON_QUALITY_HOLD, OPERATION_ALREADY_COMPLETED, OPERATOR_INACTIVE, ORDER_NOT_IN_PRODUCTION.
    /// </summary>
    [HttpPost("bundle-operations/{id:long}/manual-report")]
    [HasPermission(KonfeksiyonPermissions.Production.ManualReport)]
    public async Task<ActionResult<ApiResponse<OperationProgressDto>>> ManualReport(
        long id, ManualReportRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.ManualReportAsync(id, request, cancellationToken));

    /// <summary>Demetin üretim olayları (başlatma, adet bildirimi, tamamlama ...), eskiden yeniye.</summary>
    [HttpGet("bundles/{bundleId:int}/events")]
    [HasPermission(KonfeksiyonPermissions.Bundle.View)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<BundleOperationEventDto>>>> GetEvents(
        int bundleId, CancellationToken cancellationToken) =>
        FromResult(await service.GetBundleEventsAsync(bundleId, cancellationToken));
}
