using ERP.Core.Application.Common;
using ERP.Core.Infrastructure.Web;
using ERP.Core.Infrastructure.Web.Authorization;
using ERP.Orme.Application.Features.Packing;
using ERP.Orme.Application.Features.Receipts;
using ERP.Orme.Application.Features.Shipping;
using ERP.Orme.Domain.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Orme.Infrastructure.Api.Controllers;

/// <summary>Çeki listeleri: bir siparişin 1. ve 2. kalite topları; irsaliyeye ya da Boyahane'ye gider.</summary>
[Route("api/v1/orme-packing-lists")]
public sealed class OrmePackingListsController(IOrmePackingService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(OrmePermissions.Packing.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<OrmePackingListItemDto>>>> GetList([FromQuery] OrmePackingListQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetListAsync(query, cancellationToken), HttpContext));

    [HttpGet("candidates")]
    [HasPermission(OrmePermissions.Packing.View)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<OrmePackingCandidateDto>>>> GetCandidates(int? packingListId, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetCandidatesAsync(packingListId, cancellationToken), HttpContext));

    [HttpGet("{id:int}")]
    [HasPermission(OrmePermissions.Packing.View)]
    public async Task<ActionResult<ApiResponse<OrmePackingListDto>>> Get(int id, CancellationToken cancellationToken) =>
        FromResult(await service.GetAsync(id, cancellationToken));

    [HttpPost]
    [HasPermission(OrmePermissions.Packing.Manage)]
    public async Task<ActionResult<ApiResponse<OrmePackingListDto>>> Create(OrmePackingListRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken);
        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, ApiResponse.Success(result.Value, HttpContext))
            : FromResult(result);
    }

    [HttpPut("{id:int}")]
    [HasPermission(OrmePermissions.Packing.Manage)]
    public async Task<ActionResult<ApiResponse<OrmePackingListDto>>> Update(int id, OrmePackingListRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.UpdateAsync(id, request, cancellationToken));

    [HttpPut("{id:int}/sale-customer")]
    [HasPermission(OrmePermissions.Packing.Manage)]
    public async Task<ActionResult<ApiResponse<OrmePackingListDto>>> SetSaleCustomer(int id, SetOrmePackingSaleCustomerRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.SetSaleCustomerAsync(id, request, cancellationToken));

    [HttpPost("{id:int}/confirm")]
    [HasPermission(OrmePermissions.Packing.Manage)]
    public async Task<ActionResult<ApiResponse<OrmePackingListDto>>> Confirm(int id, CancellationToken cancellationToken) =>
        FromResult(await service.ConfirmAsync(id, cancellationToken));

    [HttpPost("{id:int}/reopen")]
    [HasPermission(OrmePermissions.Packing.Manage)]
    public async Task<ActionResult<ApiResponse<OrmePackingListDto>>> Reopen(int id, CancellationToken cancellationToken) =>
        FromResult(await service.ReopenAsync(id, cancellationToken));

    [HttpPost("{id:int}/cancel")]
    [HasPermission(OrmePermissions.Packing.Manage)]
    public async Task<ActionResult<ApiResponse<OrmePackingListDto>>> Cancel(int id, CancellationToken cancellationToken) =>
        FromResult(await service.CancelAsync(id, cancellationToken));

    /// <summary>Onaylı listeyi Boyahane'ye gönderir: orada kumaş kabul belgesi oluşur. Yalnız Boyahane modülü yüklüyse çalışır.</summary>
    [HttpPost("{id:int}/send-to-boyahane")]
    [HasPermission(OrmePermissions.Packing.Manage)]
    public async Task<ActionResult<ApiResponse<OrmePackingListDto>>> SendToBoyahane(int id, SendOrmePackingToBoyahaneRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.SendToBoyahaneAsync(id, request, cancellationToken));
}

/// <summary>İrsaliyeler: aynı müşterinin onaylı çeki listeleri. e-İrsaliye gönderimi henüz yok.</summary>
[Route("api/v1/orme-despatches")]
public sealed class OrmeDespatchesController(IOrmeShippingService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(OrmePermissions.Shipment.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<OrmeDespatchListItemDto>>>> GetList([FromQuery] OrmeDespatchQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetDespatchesAsync(query, cancellationToken), HttpContext));

    [HttpGet("candidates")]
    [HasPermission(OrmePermissions.Shipment.View)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<OrmeDespatchCandidateDto>>>> GetCandidates(int? despatchId, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetDespatchCandidatesAsync(despatchId, cancellationToken), HttpContext));

    [HttpGet("{id:int}")]
    [HasPermission(OrmePermissions.Shipment.View)]
    public async Task<ActionResult<ApiResponse<OrmeDespatchDto>>> Get(int id, CancellationToken cancellationToken) =>
        FromResult(await service.GetDespatchAsync(id, cancellationToken));

    [HttpPost]
    [HasPermission(OrmePermissions.Shipment.Manage)]
    public async Task<ActionResult<ApiResponse<OrmeDespatchDto>>> Create(OrmeDespatchRequest request, CancellationToken cancellationToken)
    {
        var result = await service.SaveDespatchAsync(null, request, cancellationToken);
        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, ApiResponse.Success(result.Value, HttpContext))
            : FromResult(result);
    }

    [HttpPut("{id:int}")]
    [HasPermission(OrmePermissions.Shipment.Manage)]
    public async Task<ActionResult<ApiResponse<OrmeDespatchDto>>> Update(int id, OrmeDespatchRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.SaveDespatchAsync(id, request, cancellationToken));

    [HttpPost("{id:int}/confirm")]
    [HasPermission(OrmePermissions.Shipment.Manage)]
    public async Task<ActionResult<ApiResponse<OrmeDespatchDto>>> Confirm(int id, CancellationToken cancellationToken) =>
        FromResult(await service.ConfirmDespatchAsync(id, cancellationToken));

    [HttpPost("{id:int}/reopen")]
    [HasPermission(OrmePermissions.Shipment.Manage)]
    public async Task<ActionResult<ApiResponse<OrmeDespatchDto>>> Reopen(int id, CancellationToken cancellationToken) =>
        FromResult(await service.ReopenDespatchAsync(id, cancellationToken));

    [HttpPost("{id:int}/cancel")]
    [HasPermission(OrmePermissions.Shipment.Manage)]
    public async Task<ActionResult<ApiResponse<OrmeDespatchDto>>> Cancel(int id, CancellationToken cancellationToken) =>
        FromResult(await service.CancelDespatchAsync(id, cancellationToken));
}

/// <summary>Faturalar: aynı müşterinin onaylı irsaliyeleri, sipariş satırı ve kalite bazında kilo. e-Fatura gönderimi henüz yok.</summary>
[Route("api/v1/orme-invoices")]
public sealed class OrmeInvoicesController(IOrmeShippingService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(OrmePermissions.Invoice.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<OrmeInvoiceListItemDto>>>> GetList([FromQuery] OrmeShippingQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetInvoicesAsync(query, cancellationToken), HttpContext));

    [HttpGet("candidates")]
    [HasPermission(OrmePermissions.Invoice.View)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<OrmeInvoiceCandidateDto>>>> GetCandidates(int? invoiceId, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetInvoiceCandidatesAsync(invoiceId, cancellationToken), HttpContext));

    [HttpGet("{id:int}")]
    [HasPermission(OrmePermissions.Invoice.View)]
    public async Task<ActionResult<ApiResponse<OrmeInvoiceDto>>> Get(int id, CancellationToken cancellationToken) =>
        FromResult(await service.GetInvoiceAsync(id, cancellationToken));

    [HttpPost]
    [HasPermission(OrmePermissions.Invoice.Manage)]
    public async Task<ActionResult<ApiResponse<OrmeInvoiceDto>>> Create(OrmeInvoiceRequest request, CancellationToken cancellationToken)
    {
        var result = await service.SaveInvoiceAsync(null, request, cancellationToken);
        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, ApiResponse.Success(result.Value, HttpContext))
            : FromResult(result);
    }

    [HttpPut("{id:int}")]
    [HasPermission(OrmePermissions.Invoice.Manage)]
    public async Task<ActionResult<ApiResponse<OrmeInvoiceDto>>> Update(int id, OrmeInvoiceRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.SaveInvoiceAsync(id, request, cancellationToken));

    [HttpPost("{id:int}/confirm")]
    [HasPermission(OrmePermissions.Invoice.Manage)]
    public async Task<ActionResult<ApiResponse<OrmeInvoiceDto>>> Confirm(int id, CancellationToken cancellationToken) =>
        FromResult(await service.ConfirmInvoiceAsync(id, cancellationToken));

    [HttpPost("{id:int}/reopen")]
    [HasPermission(OrmePermissions.Invoice.Manage)]
    public async Task<ActionResult<ApiResponse<OrmeInvoiceDto>>> Reopen(int id, CancellationToken cancellationToken) =>
        FromResult(await service.ReopenInvoiceAsync(id, cancellationToken));

    [HttpPost("{id:int}/cancel")]
    [HasPermission(OrmePermissions.Invoice.Manage)]
    public async Task<ActionResult<ApiResponse<OrmeInvoiceDto>>> Cancel(int id, CancellationToken cancellationToken) =>
        FromResult(await service.CancelInvoiceAsync(id, cancellationToken));
}

/// <summary>Fasonda müşteriye geri verilen iplik: lotun kalanından düşer.</summary>
[Route("api/v1/orme-yarn-returns")]
public sealed class OrmeYarnReturnsController(IOrmeYarnReturnService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(OrmePermissions.Receipt.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<OrmeYarnReturnDto>>>> GetList([FromQuery] OrmeYarnReturnQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetListAsync(query, cancellationToken), HttpContext));

    [HttpPost]
    [HasPermission(OrmePermissions.Receipt.Manage)]
    public async Task<ActionResult<ApiResponse<OrmeYarnReturnDto>>> Create(OrmeYarnReturnRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken);
        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, ApiResponse.Success(result.Value, HttpContext))
            : FromResult(result);
    }

    [HttpDelete("{id:int}")]
    [HasPermission(OrmePermissions.Receipt.Manage)]
    public async Task<ActionResult<ApiResponse<object?>>> Delete(int id, CancellationToken cancellationToken) =>
        FromResult(await service.DeleteAsync(id, cancellationToken));
}
