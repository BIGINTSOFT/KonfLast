using ERP.Orme.Application.Features.Customers;
using ERP.Orme.Domain.Authorization;
using ERP.Core.Application.Features.MasterData;
using ERP.Core.Infrastructure.Web;
using ERP.Core.Infrastructure.Web.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Orme.Infrastructure.Api.Controllers;

[Route("api/v1/orme-customers")]
[ResourcePermissions(OrmePermissions.Customer.View, OrmePermissions.Customer.Manage)]
public sealed class OrmeCustomersController(IMasterDataService<OrmeCustomerDto, OrmeCustomerRequest> service)
    : MasterDataController<OrmeCustomerDto, OrmeCustomerRequest>(service);

/// <summary>Müşterinin adresleri ve yetkilileri; liste bütün olarak kaydedilir, gönderilmeyenler silinir.</summary>
[Route("api/v1/orme-customers/{customerId:int}")]
public sealed class OrmeCustomerDetailsController(IOrmeCustomerDetailService service) : ApiControllerBase
{
    [HttpGet("addresses")]
    [HasPermission(OrmePermissions.Customer.View)]
    public async Task<ActionResult<ApiResponse<OrmeCustomerAddressesDto>>> GetAddresses(int customerId, CancellationToken cancellationToken) =>
        FromResult(await service.GetAddressesAsync(customerId, cancellationToken));

    [HttpPut("addresses")]
    [HasPermission(OrmePermissions.Customer.Manage)]
    public async Task<ActionResult<ApiResponse<OrmeCustomerAddressesDto>>> SaveAddresses(
        int customerId, SaveOrmeCustomerAddressesRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.SaveAddressesAsync(customerId, request, cancellationToken));

    [HttpGet("contacts")]
    [HasPermission(OrmePermissions.Customer.View)]
    public async Task<ActionResult<ApiResponse<OrmeCustomerContactsDto>>> GetContacts(int customerId, CancellationToken cancellationToken) =>
        FromResult(await service.GetContactsAsync(customerId, cancellationToken));

    [HttpPut("contacts")]
    [HasPermission(OrmePermissions.Customer.Manage)]
    public async Task<ActionResult<ApiResponse<OrmeCustomerContactsDto>>> SaveContacts(
        int customerId, SaveOrmeCustomerContactsRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.SaveContactsAsync(customerId, request, cancellationToken));
}
