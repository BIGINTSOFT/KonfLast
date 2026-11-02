using ERP.Boyahane.Application.Features.Returns;
using ERP.Boyahane.Domain.Authorization;
using ERP.Core.Application.Common;
using ERP.Core.Infrastructure.Web;
using ERP.Core.Infrastructure.Web.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Boyahane.Infrastructure.Api.Controllers;

/// <summary>Reddedilen malın müşteriye iadesi ya da hurdası. Sevkiyat işi olduğu için irsaliye yetkileriyle çalışır.</summary>
[Route("api/v1/boyahane-goods-returns")]
public sealed class BoyahaneGoodsReturnsController(IBoyahaneGoodsReturnService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(BoyahanePermissions.Shipment.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<BoyahaneGoodsReturnListItemDto>>>> GetList([FromQuery] BoyahaneGoodsReturnQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetListAsync(query, cancellationToken), HttpContext));

    [HttpGet("candidates")]
    [HasPermission(BoyahanePermissions.Shipment.View)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<BoyahaneGoodsReturnCandidateDto>>>> Candidates([FromQuery] int? returnId, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetCandidatesAsync(returnId, cancellationToken), HttpContext));

    [HttpGet("{id:int}")]
    [HasPermission(BoyahanePermissions.Shipment.View)]
    public async Task<ActionResult<ApiResponse<BoyahaneGoodsReturnDto>>> Get(int id, CancellationToken cancellationToken) =>
        FromResult(await service.GetAsync(id, cancellationToken));

    [HttpPost]
    [HasPermission(BoyahanePermissions.Shipment.Manage)]
    public async Task<ActionResult<ApiResponse<BoyahaneGoodsReturnDto>>> Create(BoyahaneGoodsReturnRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.SaveAsync(null, request, cancellationToken));

    [HttpPut("{id:int}")]
    [HasPermission(BoyahanePermissions.Shipment.Manage)]
    public async Task<ActionResult<ApiResponse<BoyahaneGoodsReturnDto>>> Update(int id, BoyahaneGoodsReturnRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.SaveAsync(id, request, cancellationToken));

    [HttpPost("{id:int}/confirm")]
    [HasPermission(BoyahanePermissions.Shipment.Manage)]
    public async Task<ActionResult<ApiResponse<BoyahaneGoodsReturnDto>>> Confirm(int id, CancellationToken cancellationToken) =>
        FromResult(await service.ConfirmAsync(id, cancellationToken));

    [HttpPost("{id:int}/reopen")]
    [HasPermission(BoyahanePermissions.Shipment.Manage)]
    public async Task<ActionResult<ApiResponse<BoyahaneGoodsReturnDto>>> Reopen(int id, CancellationToken cancellationToken) =>
        FromResult(await service.ReopenAsync(id, cancellationToken));

    [HttpPost("{id:int}/cancel")]
    [HasPermission(BoyahanePermissions.Shipment.Manage)]
    public async Task<ActionResult<ApiResponse<BoyahaneGoodsReturnDto>>> Cancel(int id, CancellationToken cancellationToken) =>
        FromResult(await service.CancelAsync(id, cancellationToken));
}
