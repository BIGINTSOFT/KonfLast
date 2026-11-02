using ERP.Konfeksiyon.Domain.Authorization;
using ERP.Core.Infrastructure.Web.Authorization;
using ERP.Core.Infrastructure.Web;
using ERP.Core.Application.Common;
using ERP.Konfeksiyon.Application.Features.ProductionLines;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Konfeksiyon.Infrastructure.Api.Controllers;

/// <summary>Bantın makineleri ve tek ekranlık özeti.</summary>
[Route("api/v1/production-lines/{lineId:int}")]
public sealed class LineMachinesController(ILineMachineService service) : ApiControllerBase
{
    /// <summary>Bant özeti: makineler (sırayla), şu anki plan ve sıradaki planlar.</summary>
    [HttpGet("overview")]
    [HasPermission(KonfeksiyonPermissions.ProductionLine.View)]
    public async Task<ActionResult<ApiResponse<LineOverviewDto>>> GetOverview(
        int lineId, CancellationToken cancellationToken) =>
        FromResult(await service.GetOverviewAsync(lineId, cancellationToken));

    /// <summary>Makineyi bantın sonuna ekler. Makine başka banttaysa <c>transfer: true</c> ile taşınır.</summary>
    [HttpPost("machines")]
    [HasPermission(KonfeksiyonPermissions.ProductionLine.Update)]
    public async Task<ActionResult<ApiResponse<LineOverviewDto>>> Assign(
        int lineId, AssignMachineRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.AssignAsync(lineId, request, cancellationToken));

    /// <summary>Makineyi banttan çıkarır (atama geçmişte kalır).</summary>
    [HttpDelete("machines/{machineId:int}")]
    [HasPermission(KonfeksiyonPermissions.ProductionLine.Update)]
    public async Task<ActionResult<ApiResponse<LineOverviewDto>>> Unassign(
        int lineId, int machineId, CancellationToken cancellationToken) =>
        FromResult(await service.UnassignAsync(lineId, machineId, cancellationToken));

    /// <summary>Banttaki makinelerin sırasını değiştirir.</summary>
    [HttpPut("machines/order")]
    [HasPermission(KonfeksiyonPermissions.ProductionLine.Update)]
    public async Task<ActionResult<ApiResponse<LineOverviewDto>>> Reorder(
        int lineId, ReorderMachinesRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.ReorderAsync(lineId, request, cancellationToken));
}

/// <summary>Bant üretim planları: hangi bant, hangi zaman aralığında, hangi siparişten kaç adet üretecek.</summary>
[Route("api/v1/line-plans")]
public sealed class LinePlansController(ILinePlanService service) : ApiControllerBase
{
    /// <summary>Planlar, başlangıç zamanına göre (sayfalı; bant, sipariş, durum ve zaman aralığı filtresi).</summary>
    [HttpGet]
    [HasPermission(KonfeksiyonPermissions.ProductionLine.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<LinePlanDto>>>> GetList(
        [FromQuery] LinePlanListQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetListAsync(query, cancellationToken), HttpContext));

    [HttpGet("{id:int}")]
    [HasPermission(KonfeksiyonPermissions.ProductionLine.View)]
    public async Task<ActionResult<ApiResponse<LinePlanDto>>> Get(int id, CancellationToken cancellationToken) =>
        FromResult(await service.GetAsync(id, cancellationToken));

    /// <summary>Banta plan ekler; aynı bantta zaman çakışması ve sipariş adedinin aşılması engellenir.</summary>
    [HttpPost]
    [HasPermission(KonfeksiyonPermissions.LinePlan.Manage)]
    public async Task<ActionResult<ApiResponse<LinePlanDto>>> Create(
        LinePlanRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(Get), new { id = result.Value.Id }, ApiResponse.Success(result.Value, HttpContext))
            : FromResult(result);
    }

    /// <summary>Planın zamanını, adedini ve notunu günceller (rowVersion zorunlu; bant ve sipariş değişmez).</summary>
    [HttpPut("{id:int}")]
    [HasPermission(KonfeksiyonPermissions.LinePlan.Manage)]
    public async Task<ActionResult<ApiResponse<LinePlanDto>>> Update(
        int id, LinePlanRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.UpdateAsync(id, request, cancellationToken));

    /// <summary>Planı iptal eder; bant o zaman aralığı için yeniden planlanabilir.</summary>
    [HttpPost("{id:int}/cancel")]
    [HasPermission(KonfeksiyonPermissions.LinePlan.Manage)]
    public async Task<ActionResult<ApiResponse<LinePlanDto>>> Cancel(
        int id, LinePlanVersionRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.CancelAsync(id, request.RowVersion, cancellationToken));
}

/// <summary>Bant dengeleme hesabı (hiçbir şey kaydetmez).</summary>
[Route("api/v1/line-balancing")]
public sealed class LineBalancingController(ILineBalancingService service) : ApiControllerBase
{
    /// <summary>
    /// Modelin aktif rotası için, hedef adet / saat ve beklenen verime göre operasyon başına gereken istasyon
    /// sayısını hesaplar; eldeki makinelerle (bant verilirse yalnızca o bantın makineleri) ve yetkin
    /// operatörlerle karşılaştırır, darboğazı ve önerileri döndürür. Hatalar: ROUTE_REQUIRED.
    /// </summary>
    [HttpGet]
    [HasPermission(KonfeksiyonPermissions.ProductionLine.View)]
    public async Task<ActionResult<ApiResponse<LineBalanceDto>>> Calculate(
        [FromQuery] LineBalanceQuery query, CancellationToken cancellationToken) =>
        FromResult(await service.CalculateAsync(query, cancellationToken));
}
