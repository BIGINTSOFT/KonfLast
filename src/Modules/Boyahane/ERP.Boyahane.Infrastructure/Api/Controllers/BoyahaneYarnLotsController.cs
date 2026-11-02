using ERP.Boyahane.Application.Features.Receipts;
using ERP.Boyahane.Domain.Authorization;
using ERP.Core.Application.Common;
using ERP.Core.Infrastructure.Web;
using ERP.Core.Infrastructure.Web.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Boyahane.Infrastructure.Api.Controllers;

/// <summary>İplik girişleri: kilo ve lot bazında, bobin ya da çile sayısıyla.</summary>
[Route("api/v1/boyahane-yarn-lots")]
public sealed class BoyahaneYarnLotsController(IBoyahaneYarnLotService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(BoyahanePermissions.Receipt.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<BoyahaneYarnLotDto>>>> GetList(
        [FromQuery] BoyahaneYarnLotQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetListAsync(query, cancellationToken), HttpContext));

    [HttpGet("{id:int}")]
    [HasPermission(BoyahanePermissions.Receipt.View)]
    public async Task<ActionResult<ApiResponse<BoyahaneYarnLotDto>>> Get(int id, CancellationToken cancellationToken) =>
        FromResult(await service.GetAsync(id, cancellationToken));

    [HttpPost]
    [HasPermission(BoyahanePermissions.Receipt.Manage)]
    public async Task<ActionResult<ApiResponse<BoyahaneYarnLotDto>>> Create(BoyahaneYarnLotRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken);
        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, ApiResponse.Success(result.Value, HttpContext))
            : FromResult(result);
    }

    [HttpPut("{id:int}")]
    [HasPermission(BoyahanePermissions.Receipt.Manage)]
    public async Task<ActionResult<ApiResponse<BoyahaneYarnLotDto>>> Update(int id, BoyahaneYarnLotRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.UpdateAsync(id, request, cancellationToken));

    [HttpDelete("{id:int}")]
    [HasPermission(BoyahanePermissions.Receipt.Manage)]
    public async Task<ActionResult<ApiResponse<object?>>> Delete(int id, CancellationToken cancellationToken) =>
        FromResult(await service.DeleteAsync(id, cancellationToken));

    /// <summary>Hatalı lotu gerekçeyle reddeder; lot siparişten çıkar. Partiye girmiş lot reddedilemez.</summary>
    [HttpPost("{id:int}/reject")]
    [HasPermission(BoyahanePermissions.Inspection.Manage)]
    public async Task<ActionResult<ApiResponse<BoyahaneYarnLotDto>>> Reject(int id, RejectBoyahaneYarnLotRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.RejectAsync(id, request, cancellationToken));

    [HttpPost("{id:int}/approve")]
    [HasPermission(BoyahanePermissions.Inspection.Manage)]
    public async Task<ActionResult<ApiResponse<BoyahaneYarnLotDto>>> Approve(int id, CancellationToken cancellationToken) =>
        FromResult(await service.ApproveAsync(id, cancellationToken));
}
