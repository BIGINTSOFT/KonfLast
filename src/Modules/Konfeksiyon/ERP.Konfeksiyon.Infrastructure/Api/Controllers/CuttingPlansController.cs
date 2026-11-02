using ERP.Core.Application.Common;
using ERP.Core.Infrastructure.Web;
using ERP.Core.Infrastructure.Web.Authorization;
using ERP.Konfeksiyon.Application.Features.Cutting;
using ERP.Konfeksiyon.Domain.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Konfeksiyon.Infrastructure.Api.Controllers;

[Route("api/v1")]
public sealed class CuttingPlansController(ICuttingPlanService service) : ApiControllerBase
{
    /// <summary>Kesim planları, en yeniden eskiye (sayfalı; plan / sipariş no, durum ve sipariş süzgeci).</summary>
    [HttpGet("cutting-plans")]
    [HasPermission(KonfeksiyonPermissions.CuttingPlan.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<CuttingPlanListItemDto>>>> GetList(
        [FromQuery] CuttingPlanListQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetListAsync(query, cancellationToken), HttpContext));

    /// <summary>Plana atanabilecek, stoğu olan kumaş topları (siparişin reçetesindeki kumaşlar önce).</summary>
    [HttpGet("cutting-plans/lot-options")]
    [HasPermission(KonfeksiyonPermissions.CuttingPlan.Manage)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CuttingLotOptionDto>>>> GetLotOptions(
        [FromQuery] CuttingLotQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetLotOptionsAsync(query, cancellationToken), HttpContext));

    /// <summary>Plan ayrıntısı: beden oranları, kalan adetle karşılaştırma, toplar, açılan demetler ve uyarılar.</summary>
    [HttpGet("cutting-plans/{id:int}")]
    [HasPermission(KonfeksiyonPermissions.CuttingPlan.View)]
    public async Task<ActionResult<ApiResponse<CuttingPlanDto>>> Get(int id, CancellationToken cancellationToken) =>
        FromResult(await service.GetAsync(id, cancellationToken));

    /// <summary>Siparişin renk-beden bazında kesilecek kalan adetleri ve kesim planları.</summary>
    [HttpGet("production-orders/{orderId:int}/cutting-summary")]
    [HasPermission(KonfeksiyonPermissions.CuttingPlan.View)]
    public async Task<ActionResult<ApiResponse<OrderCuttingSummaryDto>>> GetOrderSummary(
        int orderId, CancellationToken cancellationToken) =>
        FromResult(await service.GetOrderSummaryAsync(orderId, cancellationToken));

    /// <summary>Taslak kesim planı oluşturur; plan numarası sistem tarafından verilir.</summary>
    [HttpPost("cutting-plans")]
    [HasPermission(KonfeksiyonPermissions.CuttingPlan.Manage)]
    public async Task<ActionResult<ApiResponse<CuttingPlanDto>>> Create(
        CuttingPlanRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(Get), new { id = result.Value.Id }, ApiResponse.Success(result.Value, HttpContext))
            : FromResult(result);
    }

    /// <summary>Taslak planın pastal, oran ve toplarını değiştirir.</summary>
    [HttpPut("cutting-plans/{id:int}")]
    [HasPermission(KonfeksiyonPermissions.CuttingPlan.Manage)]
    public async Task<ActionResult<ApiResponse<CuttingPlanDto>>> Update(
        int id, CuttingPlanRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.UpdateAsync(id, request, cancellationToken));

    /// <summary>Taslak planı iptal eder.</summary>
    [HttpPost("cutting-plans/{id:int}/cancel")]
    [HasPermission(KonfeksiyonPermissions.CuttingPlan.Manage)]
    public async Task<ActionResult<ApiResponse<CuttingPlanDto>>> Cancel(
        int id, CuttingPlanVersionRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.CancelAsync(id, request.RowVersion, cancellationToken));

    /// <summary>
    /// Kesimi kaydeder: gerçek kat ve adetler yazılır, kesilen adetlerden demetler açılır, kullanılan kumaş plana
    /// atanmış toplardan düşülür.
    /// </summary>
    [HttpPost("cutting-plans/{id:int}/cut")]
    [HasPermission(KonfeksiyonPermissions.CuttingPlan.Manage)]
    public async Task<ActionResult<ApiResponse<CuttingPlanDto>>> Cut(
        int id, CuttingPlanCutRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.CutAsync(id, request, cancellationToken));
}
