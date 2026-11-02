using ERP.Dokuma.Application.Features.Accounts;
using ERP.Dokuma.Domain.Authorization;
using ERP.Core.Application.Common;
using ERP.Core.Infrastructure.Web;
using ERP.Core.Infrastructure.Web.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Dokuma.Infrastructure.Api.Controllers;

/// <summary>Tahsilat ve faturalara dağıtım.</summary>
[Route("api/v1/dokuma-payments")]
public sealed class DokumaPaymentsController(IDokumaAccountService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(DokumaPermissions.Account.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<DokumaPaymentListItemDto>>>> GetList([FromQuery] DokumaPaymentQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetPaymentsAsync(query, cancellationToken), HttpContext));

    [HttpGet("{id:int}")]
    [HasPermission(DokumaPermissions.Account.View)]
    public async Task<ActionResult<ApiResponse<DokumaPaymentDto>>> Get(int id, CancellationToken cancellationToken) =>
        FromResult(await service.GetPaymentAsync(id, cancellationToken));

    [HttpPost]
    [HasPermission(DokumaPermissions.Account.Manage)]
    public async Task<ActionResult<ApiResponse<DokumaPaymentDto>>> Create(DokumaPaymentRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.CreatePaymentAsync(request, cancellationToken));

    /// <summary>Tahsilatı iptal eder, dağıtımlar kalkar. Karşılıksız çek de böyle işlenir.</summary>
    [HttpPost("{id:int}/cancel")]
    [HasPermission(DokumaPermissions.Account.Manage)]
    public async Task<ActionResult<ApiResponse<DokumaPaymentDto>>> Cancel(int id, DokumaPaymentCancelRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.CancelPaymentAsync(id, request, cancellationToken));

    [HttpPost("{id:int}/allocations")]
    [HasPermission(DokumaPermissions.Account.Manage)]
    public async Task<ActionResult<ApiResponse<DokumaPaymentDto>>> Allocate(int id, DokumaAllocationRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.AllocateAsync(id, request, cancellationToken));

    [HttpDelete("{id:int}/allocations/{allocationId:int}")]
    [HasPermission(DokumaPermissions.Account.Manage)]
    public async Task<ActionResult<ApiResponse<DokumaPaymentDto>>> RemoveAllocation(int id, int allocationId, CancellationToken cancellationToken) =>
        FromResult(await service.RemoveAllocationAsync(id, allocationId, cancellationToken));
}

/// <summary>Müşteri cari bakiyeleri, açık faturalar ve ekstre.</summary>
[Route("api/v1/dokuma-accounts")]
public sealed class DokumaAccountsController(IDokumaAccountService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(DokumaPermissions.Account.View)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DokumaAccountDto>>>> GetList([FromQuery] string? search, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetAccountsAsync(search, cancellationToken), HttpContext));

    [HttpGet("openings")]
    [HasPermission(DokumaPermissions.Account.View)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DokumaOpeningBalanceDto>>>> Openings([FromQuery] string? search, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetOpeningBalancesAsync(search, cancellationToken), HttpContext));

    [HttpPost("openings")]
    [HasPermission(DokumaPermissions.Account.Manage)]
    public async Task<ActionResult<ApiResponse<int>>> CreateOpening(DokumaOpeningBalanceRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.CreateOpeningBalanceAsync(request, cancellationToken));

    [HttpDelete("openings/debit/{id:int}")]
    [HasPermission(DokumaPermissions.Account.Manage)]
    public async Task<ActionResult<ApiResponse<object>>> DeleteOpeningDebit(int id, CancellationToken cancellationToken) =>
        FromResult(await service.DeleteOpeningBalanceAsync(false, id, cancellationToken));

    [HttpDelete("openings/credit/{id:int}")]
    [HasPermission(DokumaPermissions.Account.Manage)]
    public async Task<ActionResult<ApiResponse<object>>> DeleteOpeningCredit(int id, CancellationToken cancellationToken) =>
        FromResult(await service.DeleteOpeningBalanceAsync(true, id, cancellationToken));

    [HttpGet("open-invoices")]
    [HasPermission(DokumaPermissions.Account.View)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DokumaOpenInvoiceDto>>>> OpenInvoices([FromQuery] DokumaOpenInvoiceQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetOpenInvoicesAsync(query, cancellationToken), HttpContext));

    [HttpGet("{customerId:int}/statement")]
    [HasPermission(DokumaPermissions.Account.View)]
    public async Task<ActionResult<ApiResponse<DokumaStatementDto>>> Statement(int customerId, [FromQuery] DokumaStatementQuery query, CancellationToken cancellationToken) =>
        FromResult(await service.GetStatementAsync(customerId, query, cancellationToken));
}
