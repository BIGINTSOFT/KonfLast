using ERP.Core.Application.Common;
using ERP.Core.Infrastructure.Web;
using ERP.Core.Infrastructure.Web.Authorization;
using ERP.Orme.Application.Features.Orders;
using ERP.Orme.Application.Features.Receipts;
using ERP.Orme.Domain.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Orme.Infrastructure.Api.Controllers;

/// <summary>Örme siparişleri. Taslak düzenlenir ve silinir; onaylanan sipariş iptal edilir.</summary>
[Route("api/v1/orme-orders")]
public sealed class OrmeOrdersController(IOrmeOrderService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(OrmePermissions.Order.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<OrmeOrderListItemDto>>>> GetList(
        [FromQuery] OrmeOrderQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetListAsync(query, cancellationToken), HttpContext));

    [HttpGet("{id:int}")]
    [HasPermission(OrmePermissions.Order.View)]
    public async Task<ActionResult<ApiResponse<OrmeOrderDto>>> Get(int id, CancellationToken cancellationToken) =>
        FromResult(await service.GetAsync(id, cancellationToken));

    [HttpPost]
    [HasPermission(OrmePermissions.Order.Manage)]
    public async Task<ActionResult<ApiResponse<OrmeOrderDto>>> Create(OrmeOrderRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken);
        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, ApiResponse.Success(result.Value, HttpContext))
            : FromResult(result);
    }

    [HttpPut("{id:int}")]
    [HasPermission(OrmePermissions.Order.Manage)]
    public async Task<ActionResult<ApiResponse<OrmeOrderDto>>> Update(int id, OrmeOrderRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.UpdateAsync(id, request, cancellationToken));

    [HttpPost("{id:int}/confirm")]
    [HasPermission(OrmePermissions.Order.Manage)]
    public async Task<ActionResult<ApiResponse<OrmeOrderDto>>> Confirm(int id, CancellationToken cancellationToken) =>
        FromResult(await service.ConfirmAsync(id, cancellationToken));

    /// <summary>Onaylı siparişi düzeltmek için taslağa döndürür; numara korunur.</summary>
    [HttpPost("{id:int}/reopen")]
    [HasPermission(OrmePermissions.Order.Manage)]
    public async Task<ActionResult<ApiResponse<OrmeOrderDto>>> Reopen(int id, CancellationToken cancellationToken) =>
        FromResult(await service.ReopenAsync(id, cancellationToken));

    [HttpPost("{id:int}/cancel")]
    [HasPermission(OrmePermissions.Order.Manage)]
    public async Task<ActionResult<ApiResponse<OrmeOrderDto>>> Cancel(int id, CancellationToken cancellationToken) =>
        FromResult(await service.CancelAsync(id, cancellationToken));

    [HttpDelete("{id:int}")]
    [HasPermission(OrmePermissions.Order.Manage)]
    public async Task<ActionResult<ApiResponse<object?>>> Delete(int id, CancellationToken cancellationToken) =>
        FromResult(await service.DeleteAsync(id, cancellationToken));

    /// <summary>Siparişin iplik ihtiyacı ve malın sahibine ait iplik girişleri.</summary>
    [HttpGet("{id:int}/yarn-requirements")]
    [HasPermission(OrmePermissions.Order.View)]
    public async Task<ActionResult<ApiResponse<OrmeOrderRequirementsDto>>> GetRequirements(int id, CancellationToken cancellationToken) =>
        FromResult(await service.GetRequirementsAsync(id, cancellationToken));

    /// <summary>Verilen iplik ile örülen kilonun gerektirdiği iplik karşılaştırması.</summary>
    [HttpGet("{id:int}/waste")]
    [HasPermission(OrmePermissions.Order.View)]
    public async Task<ActionResult<ApiResponse<OrmeOrderWasteDto>>> GetWaste(int id, CancellationToken cancellationToken) =>
        FromResult(await service.GetWasteAsync(id, cancellationToken));

    /// <summary>Proforma / sipariş teyidi: müşterinin belge dilinde basılır.</summary>
    [HttpGet("{id:int}/proforma")]
    [HasPermission(OrmePermissions.Order.View)]
    public async Task<ActionResult<ApiResponse<OrmeProformaDto>>> GetProforma(int id, CancellationToken cancellationToken) =>
        FromResult(await service.GetProformaAsync(id, cancellationToken));
}

/// <summary>İplik girişleri: kilo ve lot bazında, bobin sayısıyla.</summary>
[Route("api/v1/orme-yarn-lots")]
public sealed class OrmeYarnLotsController(IOrmeYarnLotService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(OrmePermissions.Receipt.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<OrmeYarnLotDto>>>> GetList(
        [FromQuery] OrmeYarnLotQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetListAsync(query, cancellationToken), HttpContext));

    [HttpGet("{id:int}")]
    [HasPermission(OrmePermissions.Receipt.View)]
    public async Task<ActionResult<ApiResponse<OrmeYarnLotDto>>> Get(int id, CancellationToken cancellationToken) =>
        FromResult(await service.GetAsync(id, cancellationToken));

    [HttpPost]
    [HasPermission(OrmePermissions.Receipt.Manage)]
    public async Task<ActionResult<ApiResponse<OrmeYarnLotDto>>> Create(OrmeYarnLotRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken);
        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, ApiResponse.Success(result.Value, HttpContext))
            : FromResult(result);
    }

    [HttpPut("{id:int}")]
    [HasPermission(OrmePermissions.Receipt.Manage)]
    public async Task<ActionResult<ApiResponse<OrmeYarnLotDto>>> Update(int id, OrmeYarnLotRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.UpdateAsync(id, request, cancellationToken));

    [HttpDelete("{id:int}")]
    [HasPermission(OrmePermissions.Receipt.Manage)]
    public async Task<ActionResult<ApiResponse<object?>>> Delete(int id, CancellationToken cancellationToken) =>
        FromResult(await service.DeleteAsync(id, cancellationToken));
}
