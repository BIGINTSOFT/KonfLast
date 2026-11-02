using ERP.Boyahane.Application.Features.Accounts;
using ERP.Boyahane.Domain.Authorization;
using ERP.Core.Application.Common;
using ERP.Core.Infrastructure.Web;
using ERP.Core.Infrastructure.Web.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Boyahane.Infrastructure.Api.Controllers;

/// <summary>Tahsilat ve faturalara dağıtım.</summary>
[Route("api/v1/boyahane-payments")]
public sealed class BoyahanePaymentsController(IBoyahaneAccountService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(BoyahanePermissions.Account.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<BoyahanePaymentListItemDto>>>> GetList([FromQuery] BoyahanePaymentQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetPaymentsAsync(query, cancellationToken), HttpContext));

    [HttpGet("{id:int}")]
    [HasPermission(BoyahanePermissions.Account.View)]
    public async Task<ActionResult<ApiResponse<BoyahanePaymentDto>>> Get(int id, CancellationToken cancellationToken) =>
        FromResult(await service.GetPaymentAsync(id, cancellationToken));

    [HttpPost]
    [HasPermission(BoyahanePermissions.Account.Manage)]
    public async Task<ActionResult<ApiResponse<BoyahanePaymentDto>>> Create(BoyahanePaymentRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.CreatePaymentAsync(request, cancellationToken));

    /// <summary>Tahsilatı iptal eder, dağıtımlar kalkar. Karşılıksız çek de böyle işlenir.</summary>
    [HttpPost("{id:int}/cancel")]
    [HasPermission(BoyahanePermissions.Account.Manage)]
    public async Task<ActionResult<ApiResponse<BoyahanePaymentDto>>> Cancel(int id, BoyahanePaymentCancelRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.CancelPaymentAsync(id, request, cancellationToken));

    [HttpPost("{id:int}/allocations")]
    [HasPermission(BoyahanePermissions.Account.Manage)]
    public async Task<ActionResult<ApiResponse<BoyahanePaymentDto>>> Allocate(int id, BoyahaneAllocationRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.AllocateAsync(id, request, cancellationToken));

    [HttpDelete("{id:int}/allocations/{allocationId:int}")]
    [HasPermission(BoyahanePermissions.Account.Manage)]
    public async Task<ActionResult<ApiResponse<BoyahanePaymentDto>>> RemoveAllocation(int id, int allocationId, CancellationToken cancellationToken) =>
        FromResult(await service.RemoveAllocationAsync(id, allocationId, cancellationToken));
}

/// <summary>Müşteri cari bakiyeleri, açık faturalar ve ekstre.</summary>
[Route("api/v1/boyahane-accounts")]
public sealed class BoyahaneAccountsController(IBoyahaneAccountService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(BoyahanePermissions.Account.View)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<BoyahaneAccountDto>>>> GetList([FromQuery] string? search, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetAccountsAsync(search, cancellationToken), HttpContext));

    [HttpGet("openings")]
    [HasPermission(BoyahanePermissions.Account.View)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<BoyahaneOpeningBalanceDto>>>> Openings([FromQuery] string? search, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetOpeningBalancesAsync(search, cancellationToken), HttpContext));

    [HttpPost("openings")]
    [HasPermission(BoyahanePermissions.Account.Manage)]
    public async Task<ActionResult<ApiResponse<int>>> CreateOpening(BoyahaneOpeningBalanceRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.CreateOpeningBalanceAsync(request, cancellationToken));

    [HttpDelete("openings/debit/{id:int}")]
    [HasPermission(BoyahanePermissions.Account.Manage)]
    public async Task<ActionResult<ApiResponse<object>>> DeleteOpeningDebit(int id, CancellationToken cancellationToken) =>
        FromResult(await service.DeleteOpeningBalanceAsync(false, id, cancellationToken));

    [HttpDelete("openings/credit/{id:int}")]
    [HasPermission(BoyahanePermissions.Account.Manage)]
    public async Task<ActionResult<ApiResponse<object>>> DeleteOpeningCredit(int id, CancellationToken cancellationToken) =>
        FromResult(await service.DeleteOpeningBalanceAsync(true, id, cancellationToken));

    [HttpGet("open-invoices")]
    [HasPermission(BoyahanePermissions.Account.View)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<BoyahaneOpenInvoiceDto>>>> OpenInvoices([FromQuery] BoyahaneOpenInvoiceQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetOpenInvoicesAsync(query, cancellationToken), HttpContext));

    [HttpGet("{customerId:int}/statement")]
    [HasPermission(BoyahanePermissions.Account.View)]
    public async Task<ActionResult<ApiResponse<BoyahaneStatementDto>>> Statement(int customerId, [FromQuery] BoyahaneStatementQuery query, CancellationToken cancellationToken) =>
        FromResult(await service.GetStatementAsync(customerId, query, cancellationToken));
}
