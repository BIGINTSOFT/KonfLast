using ERP.Core.Application.Common;
using ERP.Core.Infrastructure.Web;
using ERP.Core.Infrastructure.Web.Authorization;
using ERP.Dokuma.Application.Features.Packing;
using ERP.Dokuma.Application.Features.Receipts;
using ERP.Dokuma.Application.Features.Shipping;
using ERP.Dokuma.Domain.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Dokuma.Infrastructure.Api.Controllers;

/// <summary>Çeki listeleri: bir siparişin 1. ve 2. kalite topları; irsaliyeye ya da Boyahane'ye gider.</summary>
[Route("api/v1/dokuma-packing-lists")]
public sealed class DokumaPackingListsController(IDokumaPackingService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(DokumaPermissions.Packing.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<DokumaPackingListItemDto>>>> GetList([FromQuery] DokumaPackingListQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetListAsync(query, cancellationToken), HttpContext));

    [HttpGet("candidates")]
    [HasPermission(DokumaPermissions.Packing.View)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DokumaPackingCandidateDto>>>> GetCandidates(int? packingListId, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetCandidatesAsync(packingListId, cancellationToken), HttpContext));

    [HttpGet("{id:int}")]
    [HasPermission(DokumaPermissions.Packing.View)]
    public async Task<ActionResult<ApiResponse<DokumaPackingListDto>>> Get(int id, CancellationToken cancellationToken) =>
        FromResult(await service.GetAsync(id, cancellationToken));

    [HttpPost]
    [HasPermission(DokumaPermissions.Packing.Manage)]
    public async Task<ActionResult<ApiResponse<DokumaPackingListDto>>> Create(DokumaPackingListRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken);
        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, ApiResponse.Success(result.Value, HttpContext))
            : FromResult(result);
    }

    [HttpPut("{id:int}")]
    [HasPermission(DokumaPermissions.Packing.Manage)]
    public async Task<ActionResult<ApiResponse<DokumaPackingListDto>>> Update(int id, DokumaPackingListRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.UpdateAsync(id, request, cancellationToken));

    [HttpPut("{id:int}/sale-customer")]
    [HasPermission(DokumaPermissions.Packing.Manage)]
    public async Task<ActionResult<ApiResponse<DokumaPackingListDto>>> SetSaleCustomer(int id, SetDokumaPackingSaleCustomerRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.SetSaleCustomerAsync(id, request, cancellationToken));

    [HttpPost("{id:int}/confirm")]
    [HasPermission(DokumaPermissions.Packing.Manage)]
    public async Task<ActionResult<ApiResponse<DokumaPackingListDto>>> Confirm(int id, CancellationToken cancellationToken) =>
        FromResult(await service.ConfirmAsync(id, cancellationToken));

    [HttpPost("{id:int}/reopen")]
    [HasPermission(DokumaPermissions.Packing.Manage)]
    public async Task<ActionResult<ApiResponse<DokumaPackingListDto>>> Reopen(int id, CancellationToken cancellationToken) =>
        FromResult(await service.ReopenAsync(id, cancellationToken));

    [HttpPost("{id:int}/cancel")]
    [HasPermission(DokumaPermissions.Packing.Manage)]
    public async Task<ActionResult<ApiResponse<DokumaPackingListDto>>> Cancel(int id, CancellationToken cancellationToken) =>
        FromResult(await service.CancelAsync(id, cancellationToken));

    /// <summary>Onaylı listeyi Boyahane'ye gönderir: orada kumaş kabul belgesi oluşur. Yalnız Boyahane modülü yüklüyse çalışır.</summary>
    [HttpPost("{id:int}/send-to-boyahane")]
    [HasPermission(DokumaPermissions.Packing.Manage)]
    public async Task<ActionResult<ApiResponse<DokumaPackingListDto>>> SendToBoyahane(int id, SendDokumaPackingToBoyahaneRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.SendToBoyahaneAsync(id, request, cancellationToken));
}

/// <summary>İrsaliyeler: aynı müşterinin onaylı çeki listeleri. e-İrsaliye gönderimi henüz yok.</summary>
[Route("api/v1/dokuma-despatches")]
public sealed class DokumaDespatchesController(IDokumaShippingService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(DokumaPermissions.Shipment.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<DokumaDespatchListItemDto>>>> GetList([FromQuery] DokumaShippingQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetDespatchesAsync(query, cancellationToken), HttpContext));

    [HttpGet("candidates")]
    [HasPermission(DokumaPermissions.Shipment.View)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DokumaDespatchCandidateDto>>>> GetCandidates(int? despatchId, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetDespatchCandidatesAsync(despatchId, cancellationToken), HttpContext));

    [HttpGet("{id:int}")]
    [HasPermission(DokumaPermissions.Shipment.View)]
    public async Task<ActionResult<ApiResponse<DokumaDespatchDto>>> Get(int id, CancellationToken cancellationToken) =>
        FromResult(await service.GetDespatchAsync(id, cancellationToken));

    [HttpPost]
    [HasPermission(DokumaPermissions.Shipment.Manage)]
    public async Task<ActionResult<ApiResponse<DokumaDespatchDto>>> Create(DokumaDespatchRequest request, CancellationToken cancellationToken)
    {
        var result = await service.SaveDespatchAsync(null, request, cancellationToken);
        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, ApiResponse.Success(result.Value, HttpContext))
            : FromResult(result);
    }

    [HttpPut("{id:int}")]
    [HasPermission(DokumaPermissions.Shipment.Manage)]
    public async Task<ActionResult<ApiResponse<DokumaDespatchDto>>> Update(int id, DokumaDespatchRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.SaveDespatchAsync(id, request, cancellationToken));

    [HttpPost("{id:int}/confirm")]
    [HasPermission(DokumaPermissions.Shipment.Manage)]
    public async Task<ActionResult<ApiResponse<DokumaDespatchDto>>> Confirm(int id, CancellationToken cancellationToken) =>
        FromResult(await service.ConfirmDespatchAsync(id, cancellationToken));

    [HttpPost("{id:int}/reopen")]
    [HasPermission(DokumaPermissions.Shipment.Manage)]
    public async Task<ActionResult<ApiResponse<DokumaDespatchDto>>> Reopen(int id, CancellationToken cancellationToken) =>
        FromResult(await service.ReopenDespatchAsync(id, cancellationToken));

    [HttpPost("{id:int}/cancel")]
    [HasPermission(DokumaPermissions.Shipment.Manage)]
    public async Task<ActionResult<ApiResponse<DokumaDespatchDto>>> Cancel(int id, CancellationToken cancellationToken) =>
        FromResult(await service.CancelDespatchAsync(id, cancellationToken));
}

/// <summary>Faturalar: aynı müşterinin onaylı irsaliyeleri, sipariş satırı ve kalite bazında metre. e-Fatura gönderimi henüz yok.</summary>
[Route("api/v1/dokuma-invoices")]
public sealed class DokumaInvoicesController(IDokumaShippingService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(DokumaPermissions.Invoice.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<DokumaInvoiceListItemDto>>>> GetList([FromQuery] DokumaShippingQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetInvoicesAsync(query, cancellationToken), HttpContext));

    [HttpGet("candidates")]
    [HasPermission(DokumaPermissions.Invoice.View)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DokumaInvoiceCandidateDto>>>> GetCandidates(int? invoiceId, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetInvoiceCandidatesAsync(invoiceId, cancellationToken), HttpContext));

    [HttpGet("{id:int}")]
    [HasPermission(DokumaPermissions.Invoice.View)]
    public async Task<ActionResult<ApiResponse<DokumaInvoiceDto>>> Get(int id, CancellationToken cancellationToken) =>
        FromResult(await service.GetInvoiceAsync(id, cancellationToken));

    [HttpPost]
    [HasPermission(DokumaPermissions.Invoice.Manage)]
    public async Task<ActionResult<ApiResponse<DokumaInvoiceDto>>> Create(DokumaInvoiceRequest request, CancellationToken cancellationToken)
    {
        var result = await service.SaveInvoiceAsync(null, request, cancellationToken);
        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, ApiResponse.Success(result.Value, HttpContext))
            : FromResult(result);
    }

    [HttpPut("{id:int}")]
    [HasPermission(DokumaPermissions.Invoice.Manage)]
    public async Task<ActionResult<ApiResponse<DokumaInvoiceDto>>> Update(int id, DokumaInvoiceRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.SaveInvoiceAsync(id, request, cancellationToken));

    [HttpPost("{id:int}/confirm")]
    [HasPermission(DokumaPermissions.Invoice.Manage)]
    public async Task<ActionResult<ApiResponse<DokumaInvoiceDto>>> Confirm(int id, CancellationToken cancellationToken) =>
        FromResult(await service.ConfirmInvoiceAsync(id, cancellationToken));

    [HttpPost("{id:int}/reopen")]
    [HasPermission(DokumaPermissions.Invoice.Manage)]
    public async Task<ActionResult<ApiResponse<DokumaInvoiceDto>>> Reopen(int id, CancellationToken cancellationToken) =>
        FromResult(await service.ReopenInvoiceAsync(id, cancellationToken));

    [HttpPost("{id:int}/cancel")]
    [HasPermission(DokumaPermissions.Invoice.Manage)]
    public async Task<ActionResult<ApiResponse<DokumaInvoiceDto>>> Cancel(int id, CancellationToken cancellationToken) =>
        FromResult(await service.CancelInvoiceAsync(id, cancellationToken));
}

/// <summary>Fasonda müşteriye geri verilen iplik: lotun kalanından düşer.</summary>
[Route("api/v1/dokuma-yarn-returns")]
public sealed class DokumaYarnReturnsController(IDokumaYarnReturnService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(DokumaPermissions.Receipt.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<DokumaYarnReturnDto>>>> GetList([FromQuery] DokumaYarnReturnQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetListAsync(query, cancellationToken), HttpContext));

    [HttpPost]
    [HasPermission(DokumaPermissions.Receipt.Manage)]
    public async Task<ActionResult<ApiResponse<DokumaYarnReturnDto>>> Create(DokumaYarnReturnRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken);
        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, ApiResponse.Success(result.Value, HttpContext))
            : FromResult(result);
    }

    [HttpDelete("{id:int}")]
    [HasPermission(DokumaPermissions.Receipt.Manage)]
    public async Task<ActionResult<ApiResponse<object?>>> Delete(int id, CancellationToken cancellationToken) =>
        FromResult(await service.DeleteAsync(id, cancellationToken));
}
