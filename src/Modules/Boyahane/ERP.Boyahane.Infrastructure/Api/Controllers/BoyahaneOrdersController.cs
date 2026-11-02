using ERP.Boyahane.Application.Features.Orders;
using ERP.Boyahane.Domain.Authorization;
using ERP.Core.Application.Common;
using ERP.Core.Infrastructure.Web;
using ERP.Core.Infrastructure.Web.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Boyahane.Infrastructure.Api.Controllers;

/// <summary>Boyahane siparişleri. Taslak düzenlenir ve silinir; onaylanan sipariş iptal edilir, işi bitince kapatılır. Toplar satırlara sonradan bağlanır.</summary>
[Route("api/v1/boyahane-orders")]
public sealed class BoyahaneOrdersController(IBoyahaneOrderService service, IBoyahaneProformaService proformas) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(BoyahanePermissions.Order.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<BoyahaneOrderListItemDto>>>> GetList(
        [FromQuery] BoyahaneOrderQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetListAsync(query, cancellationToken), HttpContext));

    [HttpGet("{id:int}")]
    [HasPermission(BoyahanePermissions.Order.View)]
    public async Task<ActionResult<ApiResponse<BoyahaneOrderDto>>> Get(int id, CancellationToken cancellationToken) =>
        FromResult(await service.GetAsync(id, cancellationToken));

    /// <summary>Proforma / sipariş teyidi baskısının verisi: müşteri, satırlar, KDV ve toplam. Taslak ve onaylı siparişte.</summary>
    [HttpGet("{id:int}/proforma")]
    [HasPermission(BoyahanePermissions.Order.View)]
    public async Task<ActionResult<ApiResponse<BoyahaneProformaDto>>> GetProforma(int id, CancellationToken cancellationToken) =>
        FromResult(await proformas.GetAsync(id, cancellationToken));

    [HttpPost]
    [HasPermission(BoyahanePermissions.Order.Manage)]
    public async Task<ActionResult<ApiResponse<BoyahaneOrderDto>>> Create(BoyahaneOrderRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken);
        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, ApiResponse.Success(result.Value, HttpContext))
            : FromResult(result);
    }

    [HttpPut("{id:int}")]
    [HasPermission(BoyahanePermissions.Order.Manage)]
    public async Task<ActionResult<ApiResponse<BoyahaneOrderDto>>> Update(int id, BoyahaneOrderRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.UpdateAsync(id, request, cancellationToken));

    [HttpPost("{id:int}/confirm")]
    [HasPermission(BoyahanePermissions.Order.Manage)]
    public async Task<ActionResult<ApiResponse<BoyahaneOrderDto>>> Confirm(int id, CancellationToken cancellationToken) =>
        FromResult(await service.ConfirmAsync(id, cancellationToken));

    /// <summary>Onaylı siparişi düzeltmek için taslağa döndürür; numara ve bağlı mal korunur.</summary>
    [HttpPost("{id:int}/reopen")]
    [HasPermission(BoyahanePermissions.Order.Manage)]
    public async Task<ActionResult<ApiResponse<BoyahaneOrderDto>>> Reopen(int id, CancellationToken cancellationToken) =>
        FromResult(await service.ReopenAsync(id, cancellationToken));

    /// <summary>Siparişi iptal eder; bağlı toplar ve lotlar boşa çıkar.</summary>
    [HttpPost("{id:int}/cancel")]
    [HasPermission(BoyahanePermissions.Order.Manage)]
    public async Task<ActionResult<ApiResponse<BoyahaneOrderDto>>> Cancel(int id, CancellationToken cancellationToken) =>
        FromResult(await service.CancelAsync(id, cancellationToken));

    /// <summary>İşi biten onaylı siparişi kapatır; planlı ya da işlemdeki partisi olmamalıdır. Kapatılan sipariş salt okunurdur.</summary>
    [HttpPost("{id:int}/close")]
    [HasPermission(BoyahanePermissions.Order.Manage)]
    public async Task<ActionResult<ApiResponse<BoyahaneOrderDto>>> Close(int id, CloseBoyahaneOrderRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.CloseAsync(id, request, cancellationToken));

    [HttpDelete("{id:int}")]
    [HasPermission(BoyahanePermissions.Order.Manage)]
    public async Task<ActionResult<ApiResponse<object?>>> Delete(int id, CancellationToken cancellationToken) =>
        FromResult(await service.DeleteAsync(id, cancellationToken));

    /// <summary>Satıra bağlanabilecek toplar: aynı kumaş kartı, aynı sahip, reddedilmemiş, boşta ya da zaten bu satırda.</summary>
    [HttpGet("{id:int}/lines/{lineId:int}/assignable-rolls")]
    [HasPermission(BoyahanePermissions.Order.View)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<BoyahaneAssignableRollDto>>>> GetAssignableRolls(
        int id, int lineId, CancellationToken cancellationToken) =>
        FromResult(await service.GetAssignableRollsAsync(id, lineId, cancellationToken));

    /// <summary>Satırın toplarını verilen listeyle değiştirir.</summary>
    [HttpPut("{id:int}/lines/{lineId:int}/rolls")]
    [HasPermission(BoyahanePermissions.Order.Manage)]
    public async Task<ActionResult<ApiResponse<BoyahaneOrderDto>>> AssignRolls(
        int id, int lineId, AssignOrderLineRollsRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.AssignRollsAsync(id, lineId, request, cancellationToken));

    /// <summary>İplik satırına bağlanabilecek lotlar: aynı iplik kartı, aynı sahip, boşta ya da zaten bu satırda.</summary>
    [HttpGet("{id:int}/lines/{lineId:int}/assignable-yarn-lots")]
    [HasPermission(BoyahanePermissions.Order.View)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<BoyahaneAssignableYarnLotDto>>>> GetAssignableYarnLots(
        int id, int lineId, CancellationToken cancellationToken) =>
        FromResult(await service.GetAssignableYarnLotsAsync(id, lineId, cancellationToken));

    /// <summary>İplik satırının lotlarını verilen listeyle değiştirir.</summary>
    [HttpPut("{id:int}/lines/{lineId:int}/yarn-lots")]
    [HasPermission(BoyahanePermissions.Order.Manage)]
    public async Task<ActionResult<ApiResponse<BoyahaneOrderDto>>> AssignYarnLots(
        int id, int lineId, AssignOrderLineYarnLotsRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.AssignYarnLotsAsync(id, lineId, request, cancellationToken));
}
