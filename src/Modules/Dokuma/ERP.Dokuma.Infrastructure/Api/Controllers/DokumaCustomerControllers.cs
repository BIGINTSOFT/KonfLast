using ERP.Dokuma.Application.Features.Customers;
using ERP.Dokuma.Domain.Authorization;
using ERP.Core.Application.Features.MasterData;
using ERP.Core.Infrastructure.Web;
using ERP.Core.Infrastructure.Web.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Dokuma.Infrastructure.Api.Controllers;

[Route("api/v1/dokuma-customers")]
[ResourcePermissions(DokumaPermissions.Customer.View, DokumaPermissions.Customer.Manage)]
public sealed class DokumaCustomersController(IMasterDataService<DokumaCustomerDto, DokumaCustomerRequest> service)
    : MasterDataController<DokumaCustomerDto, DokumaCustomerRequest>(service);

/// <summary>Müşterinin adresleri ve yetkilileri; liste bütün olarak kaydedilir, gönderilmeyenler silinir.</summary>
[Route("api/v1/dokuma-customers/{customerId:int}")]
public sealed class DokumaCustomerDetailsController(IDokumaCustomerDetailService service) : ApiControllerBase
{
    [HttpGet("addresses")]
    [HasPermission(DokumaPermissions.Customer.View)]
    public async Task<ActionResult<ApiResponse<DokumaCustomerAddressesDto>>> GetAddresses(int customerId, CancellationToken cancellationToken) =>
        FromResult(await service.GetAddressesAsync(customerId, cancellationToken));

    [HttpPut("addresses")]
    [HasPermission(DokumaPermissions.Customer.Manage)]
    public async Task<ActionResult<ApiResponse<DokumaCustomerAddressesDto>>> SaveAddresses(
        int customerId, SaveDokumaCustomerAddressesRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.SaveAddressesAsync(customerId, request, cancellationToken));

    [HttpGet("contacts")]
    [HasPermission(DokumaPermissions.Customer.View)]
    public async Task<ActionResult<ApiResponse<DokumaCustomerContactsDto>>> GetContacts(int customerId, CancellationToken cancellationToken) =>
        FromResult(await service.GetContactsAsync(customerId, cancellationToken));

    [HttpPut("contacts")]
    [HasPermission(DokumaPermissions.Customer.Manage)]
    public async Task<ActionResult<ApiResponse<DokumaCustomerContactsDto>>> SaveContacts(
        int customerId, SaveDokumaCustomerContactsRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.SaveContactsAsync(customerId, request, cancellationToken));
}
