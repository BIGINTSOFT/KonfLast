using ERP.Konfeksiyon.Application.Features.MasterData;
using ERP.Core.Infrastructure.Web.Authorization;
using ERP.Core.Infrastructure.Web;
using ERP.Core.Application.Features.MasterData;
using ERP.Core.Domain.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Konfeksiyon.Infrastructure.Api.Controllers;

[Route("api/v1/customers")]
[ResourcePermissions(Permissions.MasterData.View, Permissions.MasterData.Manage)]
public sealed class CustomersController(IMasterDataService<CustomerDto, CustomerRequest> service)
    : MasterDataController<CustomerDto, CustomerRequest>(service);

/// <summary>Müşteri adresleri: fatura ve teslimat adresleri (e-Fatura, e-Arşiv, e-İrsaliye belgelerinde kullanılır).</summary>
[Route("api/v1/customers/{customerId:int}/addresses")]
public sealed class CustomerAddressesController(ICustomerAddressService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.MasterData.View)]
    public async Task<ActionResult<ApiResponse<CustomerAddressesDto>>> Get(int customerId, CancellationToken cancellationToken) =>
        FromResult(await service.GetAsync(customerId, cancellationToken));

    /// <summary>
    /// Adresleri verilen listeyle değiştirir; gönderilmeyen adresler silinir. Varsayılan fatura / teslimat adresi
    /// işaretlenmemişse ilk adres varsayılan olur.
    /// </summary>
    [HttpPut]
    [HasPermission(Permissions.MasterData.Manage)]
    public async Task<ActionResult<ApiResponse<CustomerAddressesDto>>> Save(
        int customerId, SaveCustomerAddressesRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.SaveAsync(customerId, request, cancellationToken));
}

/// <summary>Müşterinin buyer'ları (müşteri adına sipariş veren alt müşteriler).</summary>
[Route("api/v1/customers/{customerId:int}/buyers")]
public sealed class CustomerBuyersController(ICustomerBuyerService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.MasterData.View)]
    public async Task<ActionResult<ApiResponse<CustomerBuyersDto>>> Get(int customerId, CancellationToken cancellationToken) =>
        FromResult(await service.GetAsync(customerId, cancellationToken));

    /// <summary>
    /// Buyer'ları verilen listeyle değiştirir; gönderilmeyenler silinir. Siparişte kullanılmış buyer silinemez,
    /// pasif yapılmalıdır.
    /// </summary>
    [HttpPut]
    [HasPermission(Permissions.MasterData.Manage)]
    public async Task<ActionResult<ApiResponse<CustomerBuyersDto>>> Save(
        int customerId, SaveCustomerBuyersRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.SaveAsync(customerId, request, cancellationToken));
}

[Route("api/v1/models")]
[ResourcePermissions(Permissions.MasterData.View, Permissions.MasterData.Manage)]
public sealed class ModelsController(IMasterDataService<ModelDto, ModelRequest> service)
    : MasterDataController<ModelDto, ModelRequest>(service);

[Route("api/v1/product-groups")]
[ResourcePermissions(Permissions.MasterData.View, Permissions.MasterData.Manage)]
public sealed class ProductGroupsController(IMasterDataService<ProductGroupDto, ProductGroupRequest> service)
    : MasterDataController<ProductGroupDto, ProductGroupRequest>(service);

[Route("api/v1/products")]
[ResourcePermissions(Permissions.MasterData.View, Permissions.MasterData.Manage)]
public sealed class ProductsController(IMasterDataService<ProductDto, ProductRequest> service)
    : MasterDataController<ProductDto, ProductRequest>(service);

[Route("api/v1/colors")]
[ResourcePermissions(Permissions.MasterData.View, Permissions.MasterData.Manage)]
public sealed class ColorsController(IMasterDataService<ColorDto, ColorRequest> service)
    : MasterDataController<ColorDto, ColorRequest>(service);

[Route("api/v1/sizes")]
[ResourcePermissions(Permissions.MasterData.View, Permissions.MasterData.Manage)]
public sealed class SizesController(IMasterDataService<SizeDto, SizeRequest> service)
    : MasterDataController<SizeDto, SizeRequest>(service);
