using ERP.Orme.Application.Features.Accounts;
using ERP.Orme.Domain.Authorization;
using ERP.Core.Application.Common;
using ERP.Core.Infrastructure.Web;
using ERP.Core.Infrastructure.Web.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Orme.Infrastructure.Api.Controllers;

/// <summary>Tahsilat ve faturalara dağıtım.</summary>
[Route("api/v1/orme-payments")]
public sealed class OrmePaymentsController(IOrmeAccountService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(OrmePermissions.Account.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<OrmePaymentListItemDto>>>> GetList([FromQuery] OrmePaymentQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetPaymentsAsync(query, cancellationToken), HttpContext));

    [HttpGet("{id:int}")]
    [HasPermission(OrmePermissions.Account.View)]
    public async Task<ActionResult<ApiResponse<OrmePaymentDto>>> Get(int id, CancellationToken cancellationToken) =>
        FromResult(await service.GetPaymentAsync(id, cancellationToken));

    [HttpPost]
    [HasPermission(OrmePermissions.Account.Manage)]
    public async Task<ActionResult<ApiResponse<OrmePaymentDto>>> Create(OrmePaymentRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.CreatePaymentAsync(request, cancellationToken));

    /// <summary>Tahsilatı iptal eder, dağıtımlar kalkar. Karşılıksız çek de böyle işlenir.</summary>
    [HttpPost("{id:int}/cancel")]
    [HasPermission(OrmePermissions.Account.Manage)]
    public async Task<ActionResult<ApiResponse<OrmePaymentDto>>> Cancel(int id, OrmePaymentCancelRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.CancelPaymentAsync(id, request, cancellationToken));

    [HttpPost("{id:int}/allocations")]
    [HasPermission(OrmePermissions.Account.Manage)]
    public async Task<ActionResult<ApiResponse<OrmePaymentDto>>> Allocate(int id, OrmeAllocationRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.AllocateAsync(id, request, cancellationToken));

    [HttpDelete("{id:int}/allocations/{allocationId:int}")]
    [HasPermission(OrmePermissions.Account.Manage)]
    public async Task<ActionResult<ApiResponse<OrmePaymentDto>>> RemoveAllocation(int id, int allocationId, CancellationToken cancellationToken) =>
        FromResult(await service.RemoveAllocationAsync(id, allocationId, cancellationToken));
}

/// <summary>Müşteri cari bakiyeleri, açık faturalar ve ekstre.</summary>
[Route("api/v1/orme-accounts")]
public sealed class OrmeAccountsController(IOrmeAccountService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(OrmePermissions.Account.View)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<OrmeAccountDto>>>> GetList([FromQuery] string? search, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetAccountsAsync(search, cancellationToken), HttpContext));

    [HttpGet("openings")]
    [HasPermission(OrmePermissions.Account.View)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<OrmeOpeningBalanceDto>>>> Openings([FromQuery] string? search, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetOpeningBalancesAsync(search, cancellationToken), HttpContext));

    [HttpPost("openings")]
    [HasPermission(OrmePermissions.Account.Manage)]
    public async Task<ActionResult<ApiResponse<int>>> CreateOpening(OrmeOpeningBalanceRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.CreateOpeningBalanceAsync(request, cancellationToken));

    [HttpDelete("openings/debit/{id:int}")]
    [HasPermission(OrmePermissions.Account.Manage)]
    public async Task<ActionResult<ApiResponse<object>>> DeleteOpeningDebit(int id, CancellationToken cancellationToken) =>
        FromResult(await service.DeleteOpeningBalanceAsync(false, id, cancellationToken));

    [HttpDelete("openings/credit/{id:int}")]
    [HasPermission(OrmePermissions.Account.Manage)]
    public async Task<ActionResult<ApiResponse<object>>> DeleteOpeningCredit(int id, CancellationToken cancellationToken) =>
        FromResult(await service.DeleteOpeningBalanceAsync(true, id, cancellationToken));

    [HttpGet("open-invoices")]
    [HasPermission(OrmePermissions.Account.View)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<OrmeOpenInvoiceDto>>>> OpenInvoices([FromQuery] OrmeOpenInvoiceQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetOpenInvoicesAsync(query, cancellationToken), HttpContext));

    [HttpGet("{customerId:int}/statement")]
    [HasPermission(OrmePermissions.Account.View)]
    public async Task<ActionResult<ApiResponse<OrmeStatementDto>>> Statement(int customerId, [FromQuery] OrmeStatementQuery query, CancellationToken cancellationToken) =>
        FromResult(await service.GetStatementAsync(customerId, query, cancellationToken));
}
