using ERP.Boyahane.Application.Features.Packing;
using ERP.Boyahane.Domain.Authorization;
using ERP.Core.Application.Common;
using ERP.Core.Infrastructure.Web;
using ERP.Core.Infrastructure.Web.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Boyahane.Infrastructure.Api.Controllers;

/// <summary>Çeki listeleri: final kalitede uygun mal sipariş bazında paketlenir.</summary>
[Route("api/v1/boyahane-packing-lists")]
public sealed class BoyahanePackingListsController(IBoyahanePackingService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(BoyahanePermissions.Packing.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<BoyahanePackingListItemDto>>>> GetList([FromQuery] BoyahanePackingListQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetListAsync(query, cancellationToken), HttpContext));

    /// <summary>Listeye alınabilecek mal; düzenlemede listenin kendi malı da gelir.</summary>
    [HttpGet("candidates")]
    [HasPermission(BoyahanePermissions.Packing.View)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<BoyahanePackingCandidateDto>>>> GetCandidates(int? packingListId, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetCandidatesAsync(packingListId, cancellationToken), HttpContext));

    [HttpGet("{id:int}")]
    [HasPermission(BoyahanePermissions.Packing.View)]
    public async Task<ActionResult<ApiResponse<BoyahanePackingListDto>>> Get(int id, CancellationToken cancellationToken) =>
        FromResult(await service.GetAsync(id, cancellationToken));

    [HttpPost]
    [HasPermission(BoyahanePermissions.Packing.Manage)]
    public async Task<ActionResult<ApiResponse<BoyahanePackingListDto>>> Create(BoyahanePackingListRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken);
        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, ApiResponse.Success(result.Value, HttpContext))
            : FromResult(result);
    }

    [HttpPut("{id:int}")]
    [HasPermission(BoyahanePermissions.Packing.Manage)]
    public async Task<ActionResult<ApiResponse<BoyahanePackingListDto>>> Update(int id, BoyahanePackingListRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.UpdateAsync(id, request, cancellationToken));

    /// <summary>Stoğa üretilen malın satıldığı müşteri.</summary>
    [HttpPut("{id:int}/sale-customer")]
    [HasPermission(BoyahanePermissions.Packing.Manage)]
    public async Task<ActionResult<ApiResponse<BoyahanePackingListDto>>> SetSaleCustomer(int id, SetPackingSaleCustomerRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.SetSaleCustomerAsync(id, request, cancellationToken));

    [HttpPost("{id:int}/confirm")]
    [HasPermission(BoyahanePermissions.Packing.Manage)]
    public async Task<ActionResult<ApiResponse<BoyahanePackingListDto>>> Confirm(int id, CancellationToken cancellationToken) =>
        FromResult(await service.ConfirmAsync(id, cancellationToken));

    [HttpPost("{id:int}/reopen")]
    [HasPermission(BoyahanePermissions.Packing.Manage)]
    public async Task<ActionResult<ApiResponse<BoyahanePackingListDto>>> Reopen(int id, CancellationToken cancellationToken) =>
        FromResult(await service.ReopenAsync(id, cancellationToken));

    /// <summary>Taslak listeyi iptal eder; mal yeniden listeye alınabilir.</summary>
    [HttpPost("{id:int}/cancel")]
    [HasPermission(BoyahanePermissions.Packing.Manage)]
    public async Task<ActionResult<ApiResponse<BoyahanePackingListDto>>> Cancel(int id, CancellationToken cancellationToken) =>
        FromResult(await service.CancelAsync(id, cancellationToken));
}
