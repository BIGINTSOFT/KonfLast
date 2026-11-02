using ERP.Boyahane.Application.Features.Planning;
using ERP.Boyahane.Application.Features.Quotations;
using ERP.Boyahane.Domain.Authorization;
using ERP.Core.Application.Common;
using ERP.Core.Infrastructure.Web;
using ERP.Core.Infrastructure.Web.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Boyahane.Infrastructure.Api.Controllers;

/// <summary>
/// Fiyat teklifleri ve ön maliyet. Taslak düzenlenir ve silinir; gönderilen teklif kabul, ret ya da süre dolumuyla kapanır.
/// Kabul edilen teklif taslak sipariş açar.
/// </summary>
[Route("api/v1/boyahane-quotations")]
public sealed class BoyahaneQuotationsController(IBoyahaneQuotationService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(BoyahanePermissions.Quotation.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<BoyahaneQuotationListItemDto>>>> GetList(
        [FromQuery] BoyahaneQuotationQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetListAsync(query, cancellationToken), HttpContext));

    [HttpGet("{id:int}")]
    [HasPermission(BoyahanePermissions.Quotation.View)]
    public async Task<ActionResult<ApiResponse<BoyahaneQuotationDto>>> Get(int id, CancellationToken cancellationToken) =>
        FromResult(await service.GetAsync(id, cancellationToken));

    /// <summary>Kaydetmeden maliyet hesabı: satırların maliyet dökümü, önerilen fiyat ve uyarılar.</summary>
    [HttpPost("calculate")]
    [HasPermission(BoyahanePermissions.Quotation.View)]
    public async Task<ActionResult<ApiResponse<BoyahaneQuotationCalculationDto>>> Calculate(BoyahaneQuotationRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.CalculateAsync(request, cancellationToken));

    /// <summary>Teklif satırında seçilebilecek onaylı reçeteler; renk kartı verilirse o kartın reçeteleri.</summary>
    [HttpGet("recipe-options")]
    [HasPermission(BoyahanePermissions.Quotation.View)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<BoyahaneQuotationRecipeOptionDto>>>> GetRecipeOptions(
        [FromQuery] BoyahaneQuotationRecipeQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetRecipeOptionsAsync(query, cancellationToken), HttpContext));

    [HttpPost]
    [HasPermission(BoyahanePermissions.Quotation.Manage)]
    public async Task<ActionResult<ApiResponse<BoyahaneQuotationDto>>> Create(BoyahaneQuotationRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken);
        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, ApiResponse.Success(result.Value, HttpContext))
            : FromResult(result);
    }

    [HttpPut("{id:int}")]
    [HasPermission(BoyahanePermissions.Quotation.Manage)]
    public async Task<ActionResult<ApiResponse<BoyahaneQuotationDto>>> Update(int id, BoyahaneQuotationRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.UpdateAsync(id, request, cancellationToken));

    /// <summary>Taslağın maliyetini güncel lot fiyatları, kur ve parametrelerle yeniden hesaplar.</summary>
    [HttpPost("{id:int}/recalculate")]
    [HasPermission(BoyahanePermissions.Quotation.Manage)]
    public async Task<ActionResult<ApiResponse<BoyahaneQuotationDto>>> Recalculate(int id, CancellationToken cancellationToken) =>
        FromResult(await service.RecalculateAsync(id, cancellationToken));

    [HttpPost("{id:int}/send")]
    [HasPermission(BoyahanePermissions.Quotation.Manage)]
    public async Task<ActionResult<ApiResponse<BoyahaneQuotationDto>>> Send(int id, CancellationToken cancellationToken) =>
        FromResult(await service.SendAsync(id, cancellationToken));

    /// <summary>Gönderilmiş teklifi kabul eder ve taslak sipariş açar; sipariş açmak ayrıca sipariş yönetimi yetkisi ister.</summary>
    [HttpPost("{id:int}/accept")]
    [HasPermission(BoyahanePermissions.Quotation.Manage)]
    [HasPermission(BoyahanePermissions.Order.Manage)]
    public async Task<ActionResult<ApiResponse<BoyahaneQuotationDto>>> Accept(int id, AcceptBoyahaneQuotationRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.AcceptAsync(id, request, cancellationToken));

    [HttpPost("{id:int}/reject")]
    [HasPermission(BoyahanePermissions.Quotation.Manage)]
    public async Task<ActionResult<ApiResponse<BoyahaneQuotationDto>>> Reject(int id, BoyahaneQuotationDecisionRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.RejectAsync(id, request, cancellationToken));

    [HttpPost("{id:int}/expire")]
    [HasPermission(BoyahanePermissions.Quotation.Manage)]
    public async Task<ActionResult<ApiResponse<BoyahaneQuotationDto>>> Expire(int id, BoyahaneQuotationDecisionRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.ExpireAsync(id, request, cancellationToken));

    /// <summary>Gönderilmiş, reddedilmiş ya da süresi dolmuş teklifi düzeltmek için taslağa alır.</summary>
    [HttpPost("{id:int}/reopen")]
    [HasPermission(BoyahanePermissions.Quotation.Manage)]
    public async Task<ActionResult<ApiResponse<BoyahaneQuotationDto>>> Reopen(int id, CancellationToken cancellationToken) =>
        FromResult(await service.ReopenAsync(id, cancellationToken));

    [HttpDelete("{id:int}")]
    [HasPermission(BoyahanePermissions.Quotation.Manage)]
    public async Task<ActionResult<ApiResponse<object?>>> Delete(int id, CancellationToken cancellationToken) =>
        FromResult(await service.DeleteAsync(id, cancellationToken));
}

/// <summary>Ön maliyet ve planlama parametreleri: saat ve işçilik maliyeti, varsayılan oranlar, işlem türü süreleri, makine saat maliyetleri.</summary>
[Route("api/v1/boyahane-cost-settings")]
public sealed class BoyahaneCostSettingsController(IBoyahaneCostSettingsService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(BoyahanePermissions.Quotation.View)]
    public async Task<ActionResult<ApiResponse<BoyahaneCostSettingsDto>>> Get(CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetAsync(cancellationToken), HttpContext));

    [HttpPut]
    [HasPermission(BoyahanePermissions.Quotation.Manage)]
    public async Task<ActionResult<ApiResponse<BoyahaneCostSettingsDto>>> Save(SaveBoyahaneCostSettingsRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.SaveAsync(request, cancellationToken));
}

/// <summary>İleriye dönük makine kapasite planı: makine kuyrukları, tahmini bitişler, doluluk, kapasite ve termin riski.</summary>
[Route("api/v1/boyahane-capacity-plan")]
public sealed class BoyahaneCapacityPlanController(IBoyahaneCapacityPlanService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(BoyahanePermissions.Batch.View)]
    public async Task<ActionResult<ApiResponse<BoyahaneCapacityPlanDto>>> Get([FromQuery] BoyahaneCapacityPlanQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetAsync(query, cancellationToken), HttpContext));

    /// <summary>Planlı partinin taşınabileceği makineler (asıl operasyonu yapabilen) ve kuyruklarının bitişi.</summary>
    [HttpGet("batches/{batchId:int}/options")]
    [HasPermission(BoyahanePermissions.Batch.View)]
    public async Task<ActionResult<ApiResponse<BoyahaneCapacityOptionsDto>>> GetOptions(int batchId, CancellationToken cancellationToken) =>
        FromResult(await service.GetOptionsAsync(batchId, cancellationToken));

    /// <summary>Planlı partiyi makine kuyruğuna alır ya da başka makineye taşır; kapasite aşımı BATCH_OVER_CAPACITY döner.</summary>
    [HttpPut("batches/{batchId:int}")]
    [HasPermission(BoyahanePermissions.Batch.Manage)]
    public async Task<ActionResult<ApiResponse<object?>>> Plan(int batchId, BoyahanePlanBatchRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.PlanAsync(batchId, request, cancellationToken));

    /// <summary>Makinenin planlı parti kuyruğunun sırasını değiştirir.</summary>
    [HttpPut("machines/{machineId:int}/queue")]
    [HasPermission(BoyahanePermissions.Batch.Manage)]
    public async Task<ActionResult<ApiResponse<object?>>> Reorder(int machineId, BoyahaneMachineQueueRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.ReorderAsync(machineId, request, cancellationToken));
}
