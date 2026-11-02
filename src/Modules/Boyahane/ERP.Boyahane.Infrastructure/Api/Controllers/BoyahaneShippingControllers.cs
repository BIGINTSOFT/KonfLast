using ERP.Boyahane.Application.Features.Shipping;
using ERP.Boyahane.Domain.Authorization;
using ERP.Core.Application.Common;
using ERP.Core.Infrastructure.Web;
using ERP.Core.Infrastructure.Web.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Boyahane.Infrastructure.Api.Controllers;

/// <summary>İrsaliyeler: aynı müşterinin onaylı çeki listeleri. e-İrsaliye gönderimi henüz yok.</summary>
[Route("api/v1/boyahane-despatches")]
public sealed class BoyahaneDespatchesController(IBoyahaneShippingService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(BoyahanePermissions.Shipment.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<BoyahaneDespatchListItemDto>>>> GetList([FromQuery] BoyahaneShippingQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetDespatchesAsync(query, cancellationToken), HttpContext));

    [HttpGet("candidates")]
    [HasPermission(BoyahanePermissions.Shipment.View)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<BoyahaneDespatchCandidateDto>>>> GetCandidates(int? despatchId, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetDespatchCandidatesAsync(despatchId, cancellationToken), HttpContext));

    [HttpGet("{id:int}")]
    [HasPermission(BoyahanePermissions.Shipment.View)]
    public async Task<ActionResult<ApiResponse<BoyahaneDespatchDto>>> Get(int id, CancellationToken cancellationToken) =>
        FromResult(await service.GetDespatchAsync(id, cancellationToken));

    [HttpPost]
    [HasPermission(BoyahanePermissions.Shipment.Manage)]
    public async Task<ActionResult<ApiResponse<BoyahaneDespatchDto>>> Create(BoyahaneDespatchRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.SaveDespatchAsync(null, request, cancellationToken));

    [HttpPut("{id:int}")]
    [HasPermission(BoyahanePermissions.Shipment.Manage)]
    public async Task<ActionResult<ApiResponse<BoyahaneDespatchDto>>> Update(int id, BoyahaneDespatchRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.SaveDespatchAsync(id, request, cancellationToken));

    [HttpPost("{id:int}/confirm")]
    [HasPermission(BoyahanePermissions.Shipment.Manage)]
    public async Task<ActionResult<ApiResponse<BoyahaneDespatchDto>>> Confirm(int id, CancellationToken cancellationToken) =>
        FromResult(await service.ConfirmDespatchAsync(id, cancellationToken));

    [HttpPost("{id:int}/reopen")]
    [HasPermission(BoyahanePermissions.Shipment.Manage)]
    public async Task<ActionResult<ApiResponse<BoyahaneDespatchDto>>> Reopen(int id, CancellationToken cancellationToken) =>
        FromResult(await service.ReopenDespatchAsync(id, cancellationToken));

    [HttpPost("{id:int}/cancel")]
    [HasPermission(BoyahanePermissions.Shipment.Manage)]
    public async Task<ActionResult<ApiResponse<BoyahaneDespatchDto>>> Cancel(int id, CancellationToken cancellationToken) =>
        FromResult(await service.CancelDespatchAsync(id, cancellationToken));
}

/// <summary>Faturalar: aynı müşterinin onaylı irsaliyeleri. e-Fatura gönderimi henüz yok.</summary>
[Route("api/v1/boyahane-invoices")]
public sealed class BoyahaneInvoicesController(IBoyahaneShippingService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(BoyahanePermissions.Invoice.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<BoyahaneInvoiceListItemDto>>>> GetList([FromQuery] BoyahaneShippingQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetInvoicesAsync(query, cancellationToken), HttpContext));

    [HttpGet("candidates")]
    [HasPermission(BoyahanePermissions.Invoice.View)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<BoyahaneInvoiceCandidateDto>>>> GetCandidates(int? invoiceId, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetInvoiceCandidatesAsync(invoiceId, cancellationToken), HttpContext));

    [HttpGet("{id:int}")]
    [HasPermission(BoyahanePermissions.Invoice.View)]
    public async Task<ActionResult<ApiResponse<BoyahaneInvoiceDto>>> Get(int id, CancellationToken cancellationToken) =>
        FromResult(await service.GetInvoiceAsync(id, cancellationToken));

    [HttpPost]
    [HasPermission(BoyahanePermissions.Invoice.Manage)]
    public async Task<ActionResult<ApiResponse<BoyahaneInvoiceDto>>> Create(BoyahaneInvoiceRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.SaveInvoiceAsync(null, request, cancellationToken));

    [HttpPut("{id:int}")]
    [HasPermission(BoyahanePermissions.Invoice.Manage)]
    public async Task<ActionResult<ApiResponse<BoyahaneInvoiceDto>>> Update(int id, BoyahaneInvoiceRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.SaveInvoiceAsync(id, request, cancellationToken));

    [HttpPost("{id:int}/confirm")]
    [HasPermission(BoyahanePermissions.Invoice.Manage)]
    public async Task<ActionResult<ApiResponse<BoyahaneInvoiceDto>>> Confirm(int id, CancellationToken cancellationToken) =>
        FromResult(await service.ConfirmInvoiceAsync(id, cancellationToken));

    [HttpPost("{id:int}/reopen")]
    [HasPermission(BoyahanePermissions.Invoice.Manage)]
    public async Task<ActionResult<ApiResponse<BoyahaneInvoiceDto>>> Reopen(int id, CancellationToken cancellationToken) =>
        FromResult(await service.ReopenInvoiceAsync(id, cancellationToken));

    [HttpPost("{id:int}/cancel")]
    [HasPermission(BoyahanePermissions.Invoice.Manage)]
    public async Task<ActionResult<ApiResponse<BoyahaneInvoiceDto>>> Cancel(int id, CancellationToken cancellationToken) =>
        FromResult(await service.CancelInvoiceAsync(id, cancellationToken));
}
