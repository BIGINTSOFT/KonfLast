using ERP.Boyahane.Application.Features.Lab;
using ERP.Boyahane.Application.Features.Warehousing;
using ERP.Boyahane.Domain.Authorization;
using ERP.Core.Application.Common;
using ERP.Core.Application.Features.MasterData;
using ERP.Core.Infrastructure.Web;
using ERP.Core.Infrastructure.Web.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Boyahane.Infrastructure.Api.Controllers;

[Route("api/v1/boyahane-warehouses")]
[ResourcePermissions(BoyahanePermissions.ChemicalStock.View, BoyahanePermissions.ChemicalStock.Manage)]
public sealed class BoyahaneWarehousesController(IMasterDataService<BoyahaneWarehouseDto, BoyahaneWarehouseRequest> service)
    : MasterDataController<BoyahaneWarehouseDto, BoyahaneWarehouseRequest>(service);

[Route("api/v1/boyahane-warehouse-locations")]
[ResourcePermissions(BoyahanePermissions.ChemicalStock.View, BoyahanePermissions.ChemicalStock.Manage)]
public sealed class BoyahaneWarehouseLocationsController(IMasterDataService<BoyahaneWarehouseLocationDto, BoyahaneWarehouseLocationRequest> service)
    : MasterDataController<BoyahaneWarehouseLocationDto, BoyahaneWarehouseLocationRequest>(service);

/// <summary>Adres bazında depodaki mal, adres değişimi ve depolar arası transfer.</summary>
[Route("api/v1/boyahane-warehousing")]
public sealed class BoyahaneWarehousingController(IBoyahaneWarehousingService service) : ApiControllerBase
{
    [HttpGet("goods")]
    [HasPermission(BoyahanePermissions.ChemicalStock.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<BoyahaneWarehouseGoodsDto>>>> Goods([FromQuery] BoyahaneWarehouseGoodsQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetGoodsAsync(query, cancellationToken), HttpContext));

    [HttpGet("locations")]
    [HasPermission(BoyahanePermissions.ChemicalStock.View)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<BoyahaneLocationOptionDto>>>> Locations([FromQuery] int warehouseId, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetLocationsAsync(warehouseId, cancellationToken), HttpContext));

    [HttpGet("movements")]
    [HasPermission(BoyahanePermissions.ChemicalStock.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<BoyahaneLocationMovementDto>>>> Movements([FromQuery] BoyahaneLocationMovementQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetMovementsAsync(query, cancellationToken), HttpContext));

    [HttpPost("move")]
    [HasPermission(BoyahanePermissions.ChemicalStock.Manage)]
    public async Task<ActionResult<ApiResponse<int>>> Move(BoyahaneMoveGoodsRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.MoveAsync(request, cancellationToken));
}

/// <summary>Onaylı reçetelerin listesi.</summary>
[Route("api/v1/boyahane-recipes")]
public sealed class BoyahaneRecipesController(IBoyahaneRecipeService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(BoyahanePermissions.Lab.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<BoyahaneRecipeDto>>>> GetList([FromQuery] BoyahaneRecipeQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetListAsync(query, cancellationToken), HttpContext));
}
