using ERP.Boyahane.Application.Features.Inspection;
using ERP.Boyahane.Domain.Authorization;
using ERP.Core.Application.Common;
using ERP.Core.Application.Features.MasterData;
using ERP.Core.Infrastructure.Web;
using ERP.Core.Infrastructure.Web.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Boyahane.Infrastructure.Api.Controllers;

[Route("api/v1/boyahane-defect-types")]
[ResourcePermissions(BoyahanePermissions.Inspection.View, BoyahanePermissions.Inspection.Manage)]
public sealed class BoyahaneDefectTypesController(IMasterDataService<BoyahaneDefectTypeDto, BoyahaneDefectTypeRequest> service)
    : MasterDataController<BoyahaneDefectTypeDto, BoyahaneDefectTypeRequest>(service);

/// <summary>Ham kontrol: top barkodla açılır, ölçü ve hatalar girilir, uygun / şartlı / red kararı verilir.</summary>
[Route("api/v1/boyahane-fabric-rolls")]
public sealed class BoyahaneFabricRollsController(IBoyahaneInspectionService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(BoyahanePermissions.Inspection.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<BoyahaneFabricRollListItemDto>>>> GetList(
        [FromQuery] BoyahaneFabricRollQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetRollsAsync(query, cancellationToken), HttpContext));

    [HttpGet("{id:int}")]
    [HasPermission(BoyahanePermissions.Inspection.View)]
    public async Task<ActionResult<ApiResponse<BoyahaneFabricRollDetailDto>>> Get(int id, CancellationToken cancellationToken) =>
        FromResult(await service.GetRollAsync(id, cancellationToken));

    /// <summary>Okutulan barkoddaki top numarasıyla topu getirir.</summary>
    [HttpGet("by-no/{rollNo}")]
    [HasPermission(BoyahanePermissions.Inspection.View)]
    public async Task<ActionResult<ApiResponse<BoyahaneFabricRollDetailDto>>> GetByNo(string rollNo, CancellationToken cancellationToken) =>
        FromResult(await service.GetRollByNoAsync(rollNo, cancellationToken));

    /// <summary>Kontrol kararını kaydeder. Kabul ölçüleri korunur, ölçülen değerler yanına yazılır.</summary>
    [HttpPost("{id:int}/inspection")]
    [HasPermission(BoyahanePermissions.Inspection.Manage)]
    public async Task<ActionResult<ApiResponse<BoyahaneFabricRollDetailDto>>> Inspect(
        int id, InspectBoyahaneFabricRollRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.InspectAsync(id, request, cancellationToken));

    /// <summary>Örnek top kontrolünden sonra lotun kontrol bekleyen bütün toplarını uygun yapar.</summary>
    [HttpPost("approve-remaining")]
    [HasPermission(BoyahanePermissions.Inspection.Manage)]
    public async Task<ActionResult<ApiResponse<ApproveRemainingRollsResultDto>>> ApproveRemaining(
        ApproveRemainingRollsRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.ApproveRemainingAsync(request, cancellationToken));

    /// <summary>Şartlı top için müşterinin kararını kaydeder; kontrol yetkisinden ayrı bir yetki ister.</summary>
    [HttpPost("{id:int}/customer-decision")]
    [HasPermission(BoyahanePermissions.Inspection.CustomerDecision)]
    public async Task<ActionResult<ApiResponse<BoyahaneFabricRollDetailDto>>> RecordCustomerDecision(
        int id, BoyahaneRollCustomerDecisionRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.RecordCustomerDecisionAsync(id, request, cancellationToken));
}
