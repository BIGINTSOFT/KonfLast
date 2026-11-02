using ERP.Core.Application.Features.MasterData;
using ERP.Core.Infrastructure.Web;
using ERP.Core.Infrastructure.Web.Authorization;
using ERP.Orme.Application.Features.Materials;
using ERP.Orme.Domain.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Orme.Infrastructure.Api.Controllers;

[Route("api/v1/orme-suppliers")]
[ResourcePermissions(OrmePermissions.Purchase.View, OrmePermissions.Purchase.Manage)]
public sealed class OrmeSuppliersController(IMasterDataService<OrmeSupplierDto, OrmeSupplierRequest> service)
    : MasterDataController<OrmeSupplierDto, OrmeSupplierRequest>(service);

[Route("api/v1/orme-yarns")]
[ResourcePermissions(OrmePermissions.Material.View, OrmePermissions.Material.Manage)]
public sealed class OrmeYarnsController(IMasterDataService<OrmeYarnDto, OrmeYarnRequest> service)
    : MasterDataController<OrmeYarnDto, OrmeYarnRequest>(service);

[Route("api/v1/orme-machines")]
[ResourcePermissions(OrmePermissions.Machine.View, OrmePermissions.Machine.Manage)]
public sealed class OrmeMachinesController(IMasterDataService<OrmeMachineDto, OrmeMachineRequest> service)
    : MasterDataController<OrmeMachineDto, OrmeMachineRequest>(service);

[Route("api/v1/orme-fabrics")]
[ResourcePermissions(OrmePermissions.Material.View, OrmePermissions.Material.Manage)]
public sealed class OrmeFabricsController(IMasterDataService<OrmeFabricDto, OrmeFabricRequest> service)
    : MasterDataController<OrmeFabricDto, OrmeFabricRequest>(service);

/// <summary>Kumaşın iplikleri ve oranları; liste bütün olarak kaydedilir, gönderilmeyenler silinir.</summary>
[Route("api/v1/orme-fabrics/{fabricId:int}/yarns")]
public sealed class OrmeFabricYarnsController(IOrmeFabricYarnService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(OrmePermissions.Material.View)]
    public async Task<ActionResult<ApiResponse<OrmeFabricYarnsDto>>> Get(int fabricId, CancellationToken cancellationToken) =>
        FromResult(await service.GetAsync(fabricId, cancellationToken));

    [HttpPut]
    [HasPermission(OrmePermissions.Material.Manage)]
    public async Task<ActionResult<ApiResponse<OrmeFabricYarnsDto>>> Save(int fabricId, SaveOrmeFabricYarnsRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.SaveAsync(fabricId, request, cancellationToken));
}
