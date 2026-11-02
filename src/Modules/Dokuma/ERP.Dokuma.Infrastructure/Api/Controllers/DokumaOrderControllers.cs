using ERP.Core.Application.Common;
using ERP.Core.Infrastructure.Web;
using ERP.Core.Infrastructure.Web.Authorization;
using ERP.Dokuma.Application.Features.Orders;
using ERP.Dokuma.Application.Features.Receipts;
using ERP.Dokuma.Domain.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Dokuma.Infrastructure.Api.Controllers;

/// <summary>Dokuma siparişleri. Taslak düzenlenir ve silinir; onaylanan sipariş iptal edilir.</summary>
[Route("api/v1/dokuma-orders")]
public sealed class DokumaOrdersController(IDokumaOrderService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(DokumaPermissions.Order.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<DokumaOrderListItemDto>>>> GetList(
        [FromQuery] DokumaOrderQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetListAsync(query, cancellationToken), HttpContext));

    [HttpGet("{id:int}")]
    [HasPermission(DokumaPermissions.Order.View)]
    public async Task<ActionResult<ApiResponse<DokumaOrderDto>>> Get(int id, CancellationToken cancellationToken) =>
        FromResult(await service.GetAsync(id, cancellationToken));

    /// <summary>Proforma / sipariş teyidi çıktısı; taslak ve onaylı siparişte, müşterinin belge dilinde.</summary>
    [HttpGet("{id:int}/proforma")]
    [HasPermission(DokumaPermissions.Order.View)]
    public async Task<ActionResult<ApiResponse<DokumaProformaDto>>> GetProforma(int id, CancellationToken cancellationToken) =>
        FromResult(await service.GetProformaAsync(id, cancellationToken));

    [HttpPost]
    [HasPermission(DokumaPermissions.Order.Manage)]
    public async Task<ActionResult<ApiResponse<DokumaOrderDto>>> Create(DokumaOrderRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken);
        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, ApiResponse.Success(result.Value, HttpContext))
            : FromResult(result);
    }

    [HttpPut("{id:int}")]
    [HasPermission(DokumaPermissions.Order.Manage)]
    public async Task<ActionResult<ApiResponse<DokumaOrderDto>>> Update(int id, DokumaOrderRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.UpdateAsync(id, request, cancellationToken));

    [HttpPost("{id:int}/confirm")]
    [HasPermission(DokumaPermissions.Order.Manage)]
    public async Task<ActionResult<ApiResponse<DokumaOrderDto>>> Confirm(int id, CancellationToken cancellationToken) =>
        FromResult(await service.ConfirmAsync(id, cancellationToken));

    /// <summary>Onaylı siparişi düzeltmek için taslağa döndürür; numara korunur.</summary>
    [HttpPost("{id:int}/reopen")]
    [HasPermission(DokumaPermissions.Order.Manage)]
    public async Task<ActionResult<ApiResponse<DokumaOrderDto>>> Reopen(int id, CancellationToken cancellationToken) =>
        FromResult(await service.ReopenAsync(id, cancellationToken));

    [HttpPost("{id:int}/cancel")]
    [HasPermission(DokumaPermissions.Order.Manage)]
    public async Task<ActionResult<ApiResponse<DokumaOrderDto>>> Cancel(int id, CancellationToken cancellationToken) =>
        FromResult(await service.CancelAsync(id, cancellationToken));

    [HttpDelete("{id:int}")]
    [HasPermission(DokumaPermissions.Order.Manage)]
    public async Task<ActionResult<ApiResponse<object?>>> Delete(int id, CancellationToken cancellationToken) =>
        FromResult(await service.DeleteAsync(id, cancellationToken));

    /// <summary>Siparişin iplik ihtiyacı (fire hariç) ve malın sahibine ait iplik girişleri.</summary>
    [HttpGet("{id:int}/yarn-requirements")]
    [HasPermission(DokumaPermissions.Order.View)]
    public async Task<ActionResult<ApiResponse<DokumaOrderRequirementsDto>>> GetRequirements(int id, CancellationToken cancellationToken) =>
        FromResult(await service.GetRequirementsAsync(id, cancellationToken));

    /// <summary>Gerçek fire: verilen iplik ile dokunan metre için hesaplanan iplik; müşterinin kabul ettiği fireyle karşılaştırılır.</summary>
    [HttpGet("{id:int}/yarn-waste")]
    [HasPermission(DokumaPermissions.Order.View)]
    public async Task<ActionResult<ApiResponse<DokumaOrderWasteDto>>> GetWaste(int id, CancellationToken cancellationToken) =>
        FromResult(await service.GetWasteAsync(id, cancellationToken));
}

/// <summary>İplik girişleri: kilo ve lot bazında, bobin sayısıyla.</summary>
[Route("api/v1/dokuma-yarn-lots")]
public sealed class DokumaYarnLotsController(IDokumaYarnLotService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(DokumaPermissions.Receipt.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<DokumaYarnLotDto>>>> GetList(
        [FromQuery] DokumaYarnLotQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetListAsync(query, cancellationToken), HttpContext));

    [HttpGet("{id:int}")]
    [HasPermission(DokumaPermissions.Receipt.View)]
    public async Task<ActionResult<ApiResponse<DokumaYarnLotDto>>> Get(int id, CancellationToken cancellationToken) =>
        FromResult(await service.GetAsync(id, cancellationToken));

    [HttpPost]
    [HasPermission(DokumaPermissions.Receipt.Manage)]
    public async Task<ActionResult<ApiResponse<DokumaYarnLotDto>>> Create(DokumaYarnLotRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken);
        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, ApiResponse.Success(result.Value, HttpContext))
            : FromResult(result);
    }

    [HttpPut("{id:int}")]
    [HasPermission(DokumaPermissions.Receipt.Manage)]
    public async Task<ActionResult<ApiResponse<DokumaYarnLotDto>>> Update(int id, DokumaYarnLotRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.UpdateAsync(id, request, cancellationToken));

    [HttpDelete("{id:int}")]
    [HasPermission(DokumaPermissions.Receipt.Manage)]
    public async Task<ActionResult<ApiResponse<object?>>> Delete(int id, CancellationToken cancellationToken) =>
        FromResult(await service.DeleteAsync(id, cancellationToken));
}
