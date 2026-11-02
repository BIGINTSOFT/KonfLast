using ERP.Boyahane.Application.Features.Customers;
using ERP.Boyahane.Domain.Authorization;
using ERP.Core.Application.Features.MasterData;
using ERP.Core.Infrastructure.Web;
using ERP.Core.Infrastructure.Web.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Boyahane.Infrastructure.Api.Controllers;

[Route("api/v1/boyahane-customers")]
[ResourcePermissions(BoyahanePermissions.Customer.View, BoyahanePermissions.Customer.Manage)]
public sealed class BoyahaneCustomersController(IMasterDataService<BoyahaneCustomerDto, BoyahaneCustomerRequest> service)
    : MasterDataController<BoyahaneCustomerDto, BoyahaneCustomerRequest>(service);

/// <summary>Müşterinin adresleri ve yetkilileri; liste bütün olarak kaydedilir, gönderilmeyenler silinir.</summary>
[Route("api/v1/boyahane-customers/{customerId:int}")]
public sealed class BoyahaneCustomerDetailsController(IBoyahaneCustomerDetailService service) : ApiControllerBase
{
    [HttpGet("addresses")]
    [HasPermission(BoyahanePermissions.Customer.View)]
    public async Task<ActionResult<ApiResponse<BoyahaneCustomerAddressesDto>>> GetAddresses(int customerId, CancellationToken cancellationToken) =>
        FromResult(await service.GetAddressesAsync(customerId, cancellationToken));

    [HttpPut("addresses")]
    [HasPermission(BoyahanePermissions.Customer.Manage)]
    public async Task<ActionResult<ApiResponse<BoyahaneCustomerAddressesDto>>> SaveAddresses(
        int customerId, SaveBoyahaneCustomerAddressesRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.SaveAddressesAsync(customerId, request, cancellationToken));

    [HttpGet("contacts")]
    [HasPermission(BoyahanePermissions.Customer.View)]
    public async Task<ActionResult<ApiResponse<BoyahaneCustomerContactsDto>>> GetContacts(int customerId, CancellationToken cancellationToken) =>
        FromResult(await service.GetContactsAsync(customerId, cancellationToken));

    [HttpPut("contacts")]
    [HasPermission(BoyahanePermissions.Customer.Manage)]
    public async Task<ActionResult<ApiResponse<BoyahaneCustomerContactsDto>>> SaveContacts(
        int customerId, SaveBoyahaneCustomerContactsRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.SaveContactsAsync(customerId, request, cancellationToken));
}
