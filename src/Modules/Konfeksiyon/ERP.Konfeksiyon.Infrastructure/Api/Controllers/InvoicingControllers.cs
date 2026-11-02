using ERP.Konfeksiyon.Application.Features.Invoicing;
using ERP.Konfeksiyon.Domain.Authorization;
using ERP.Core.Application.Common;
using ERP.Core.Infrastructure.Web;
using ERP.Core.Infrastructure.Web.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Konfeksiyon.Infrastructure.Api.Controllers;

/// <summary>Tahsilat ve faturalara dağıtım.</summary>
[Route("api/v1/customer-payments")]
public sealed class CustomerPaymentsController(ICustomerAccountService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(KonfeksiyonPermissions.Account.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<SalesPaymentListItemDto>>>> GetList([FromQuery] SalesPaymentQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetPaymentsAsync(query, cancellationToken), HttpContext));

    [HttpGet("{id:int}")]
    [HasPermission(KonfeksiyonPermissions.Account.View)]
    public async Task<ActionResult<ApiResponse<SalesPaymentDto>>> Get(int id, CancellationToken cancellationToken) =>
        FromResult(await service.GetPaymentAsync(id, cancellationToken));

    [HttpPost]
    [HasPermission(KonfeksiyonPermissions.Account.Manage)]
    public async Task<ActionResult<ApiResponse<SalesPaymentDto>>> Create(SalesPaymentRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.CreatePaymentAsync(request, cancellationToken));

    /// <summary>Tahsilatı iptal eder, dağıtımlar kalkar. Karşılıksız çek de böyle işlenir.</summary>
    [HttpPost("{id:int}/cancel")]
    [HasPermission(KonfeksiyonPermissions.Account.Manage)]
    public async Task<ActionResult<ApiResponse<SalesPaymentDto>>> Cancel(int id, SalesPaymentCancelRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.CancelPaymentAsync(id, request, cancellationToken));

    [HttpPost("{id:int}/allocations")]
    [HasPermission(KonfeksiyonPermissions.Account.Manage)]
    public async Task<ActionResult<ApiResponse<SalesPaymentDto>>> Allocate(int id, SalesAllocationRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.AllocateAsync(id, request, cancellationToken));

    [HttpDelete("{id:int}/allocations/{allocationId:int}")]
    [HasPermission(KonfeksiyonPermissions.Account.Manage)]
    public async Task<ActionResult<ApiResponse<SalesPaymentDto>>> RemoveAllocation(int id, int allocationId, CancellationToken cancellationToken) =>
        FromResult(await service.RemoveAllocationAsync(id, allocationId, cancellationToken));
}

/// <summary>Müşteri cari bakiyeleri, açık faturalar ve ekstre.</summary>
[Route("api/v1/customer-accounts")]
public sealed class CustomerAccountsController(ICustomerAccountService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(KonfeksiyonPermissions.Account.View)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SalesAccountDto>>>> GetList([FromQuery] string? search, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetAccountsAsync(search, cancellationToken), HttpContext));

    [HttpGet("openings")]
    [HasPermission(KonfeksiyonPermissions.Account.View)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SalesOpeningBalanceDto>>>> Openings([FromQuery] string? search, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetOpeningBalancesAsync(search, cancellationToken), HttpContext));

    [HttpPost("openings")]
    [HasPermission(KonfeksiyonPermissions.Account.Manage)]
    public async Task<ActionResult<ApiResponse<int>>> CreateOpening(SalesOpeningBalanceRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.CreateOpeningBalanceAsync(request, cancellationToken));

    [HttpDelete("openings/debit/{id:int}")]
    [HasPermission(KonfeksiyonPermissions.Account.Manage)]
    public async Task<ActionResult<ApiResponse<object>>> DeleteOpeningDebit(int id, CancellationToken cancellationToken) =>
        FromResult(await service.DeleteOpeningBalanceAsync(false, id, cancellationToken));

    [HttpDelete("openings/credit/{id:int}")]
    [HasPermission(KonfeksiyonPermissions.Account.Manage)]
    public async Task<ActionResult<ApiResponse<object>>> DeleteOpeningCredit(int id, CancellationToken cancellationToken) =>
        FromResult(await service.DeleteOpeningBalanceAsync(true, id, cancellationToken));

    [HttpGet("open-invoices")]
    [HasPermission(KonfeksiyonPermissions.Account.View)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SalesOpenInvoiceDto>>>> OpenInvoices([FromQuery] SalesOpenInvoiceQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetOpenInvoicesAsync(query, cancellationToken), HttpContext));

    [HttpGet("{customerId:int}/statement")]
    [HasPermission(KonfeksiyonPermissions.Account.View)]
    public async Task<ActionResult<ApiResponse<SalesStatementDto>>> Statement(int customerId, [FromQuery] SalesStatementQuery query, CancellationToken cancellationToken) =>
        FromResult(await service.GetStatementAsync(customerId, query, cancellationToken));
}

/// <summary>Satış faturaları: sevk edilmiş sevkiyatlardan.</summary>
[Route("api/v1/invoices")]
public sealed class InvoicesController(IInvoiceService service) : ApiControllerBase
{
    /// <summary>Faturalar, en yeniden eskiye (açılış bakiyeleri hariç).</summary>
    [HttpGet]
    [HasPermission(KonfeksiyonPermissions.Invoice.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<InvoiceListItemDto>>>> GetList([FromQuery] InvoiceQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetListAsync(query, cancellationToken), HttpContext));

    /// <summary>Faturaya alınabilecek sevkiyatlar: sevk edilmiş, faturasız (ya da bu faturadaki).</summary>
    [HttpGet("candidates")]
    [HasPermission(KonfeksiyonPermissions.Invoice.View)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<InvoiceCandidateDto>>>> GetCandidates(int? invoiceId, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetCandidatesAsync(invoiceId, cancellationToken), HttpContext));

    [HttpGet("{id:int}")]
    [HasPermission(KonfeksiyonPermissions.Invoice.View)]
    public async Task<ActionResult<ApiResponse<InvoiceDto>>> Get(int id, CancellationToken cancellationToken) =>
        FromResult(await service.GetAsync(id, cancellationToken));

    /// <summary>Taslak fatura; satırlar seçilen sevkiyatlardan, fiyatlar siparişten ya da fiyat listesinden gelir.</summary>
    [HttpPost]
    [HasPermission(KonfeksiyonPermissions.Invoice.Manage)]
    public async Task<ActionResult<ApiResponse<InvoiceDto>>> Create(InvoiceRequest request, CancellationToken cancellationToken)
    {
        var result = await service.SaveAsync(null, request, cancellationToken);
        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, ApiResponse.Success(result.Value, HttpContext))
            : FromResult(result);
    }

    [HttpPut("{id:int}")]
    [HasPermission(KonfeksiyonPermissions.Invoice.Manage)]
    public async Task<ActionResult<ApiResponse<InvoiceDto>>> Update(int id, InvoiceRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.SaveAsync(id, request, cancellationToken));

    /// <summary>Onaylar; bütün satırların fiyatı girilmiş olmalıdır. Onaylı fatura cariye borç yazar.</summary>
    [HttpPost("{id:int}/confirm")]
    [HasPermission(KonfeksiyonPermissions.Invoice.Manage)]
    public async Task<ActionResult<ApiResponse<InvoiceDto>>> Confirm(int id, CancellationToken cancellationToken) =>
        FromResult(await service.ConfirmAsync(id, cancellationToken));

    /// <summary>Onaylı faturayı taslağa alır; tahsilat dağıtılmışsa alınmaz.</summary>
    [HttpPost("{id:int}/reopen")]
    [HasPermission(KonfeksiyonPermissions.Invoice.Manage)]
    public async Task<ActionResult<ApiResponse<InvoiceDto>>> Reopen(int id, CancellationToken cancellationToken) =>
        FromResult(await service.ReopenAsync(id, cancellationToken));

    /// <summary>Taslak faturayı iptal eder; sevkiyatları yeniden faturalanabilir olur.</summary>
    [HttpPost("{id:int}/cancel")]
    [HasPermission(KonfeksiyonPermissions.Invoice.Manage)]
    public async Task<ActionResult<ApiResponse<InvoiceDto>>> Cancel(int id, CancellationToken cancellationToken) =>
        FromResult(await service.CancelAsync(id, cancellationToken));
}
