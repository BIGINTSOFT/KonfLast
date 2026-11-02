using ERP.Konfeksiyon.Domain.Authorization;
using ERP.Core.Infrastructure.Web.Authorization;
using ERP.Core.Infrastructure.Web;
using ERP.Core.Application.Features.MasterData;
using ERP.Konfeksiyon.Application.Features.MasterData;
using ERP.Konfeksiyon.Application.Features.Shifts;
using ERP.Core.Domain.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Konfeksiyon.Infrastructure.Api.Controllers;

[Route("api/v1/machine-types")]
[ResourcePermissions(Permissions.MasterData.View, Permissions.MasterData.Manage)]
public sealed class MachineTypesController(IMasterDataService<MachineTypeDto, MachineTypeRequest> service)
    : MasterDataController<MachineTypeDto, MachineTypeRequest>(service);

[Route("api/v1/operations")]
[ResourcePermissions(Permissions.MasterData.View, Permissions.MasterData.Manage)]
public sealed class OperationsController(IMasterDataService<OperationDto, OperationRequest> service)
    : MasterDataController<OperationDto, OperationRequest>(service);

[Route("api/v1/machines")]
[ResourcePermissions(KonfeksiyonPermissions.Machine.View, KonfeksiyonPermissions.Machine.Manage)]
public sealed class MachinesController(IMasterDataService<MachineDto, MachineRequest> service)
    : MasterDataController<MachineDto, MachineRequest>(service);

/// <summary>Makinenin ana operasyonuna ek olarak yapabildiği operasyonlar.</summary>
[Route("api/v1/machines/{machineId:int}/operations")]
public sealed class MachineOperationsController(IMachineOperationService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(KonfeksiyonPermissions.Machine.View)]
    public async Task<ActionResult<ApiResponse<MachineOperationsDto>>> Get(int machineId, CancellationToken cancellationToken) =>
        FromResult(await service.GetAsync(machineId, cancellationToken));

    [HttpPut]
    [HasPermission(KonfeksiyonPermissions.Machine.Manage)]
    public async Task<ActionResult<ApiResponse<MachineOperationsDto>>> Save(
        int machineId, UpdateMachineOperationsRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.SaveAsync(machineId, request, cancellationToken));
}

[Route("api/v1/operators")]
[ResourcePermissions(KonfeksiyonPermissions.Operator.View, KonfeksiyonPermissions.Operator.Update)]
public sealed class OperatorsController(IMasterDataService<OperatorDto, OperatorRequest> service)
    : MasterDataController<OperatorDto, OperatorRequest>(service);

[Route("api/v1/factories")]
[ResourcePermissions(Permissions.MasterData.View, Permissions.MasterData.Manage)]
public sealed class FactoriesController(IMasterDataService<FactoryDto, FactoryRequest> service)
    : MasterDataController<FactoryDto, FactoryRequest>(service);

[Route("api/v1/production-areas")]
[ResourcePermissions(Permissions.MasterData.View, Permissions.MasterData.Manage)]
public sealed class ProductionAreasController(IMasterDataService<ProductionAreaDto, ProductionAreaRequest> service)
    : MasterDataController<ProductionAreaDto, ProductionAreaRequest>(service);

[Route("api/v1/production-lines")]
[ResourcePermissions(KonfeksiyonPermissions.ProductionLine.View, KonfeksiyonPermissions.ProductionLine.Update)]
public sealed class ProductionLinesController(IMasterDataService<ProductionLineDto, ProductionLineRequest> service)
    : MasterDataController<ProductionLineDto, ProductionLineRequest>(service);

[Route("api/v1/shifts")]
[ResourcePermissions(Permissions.MasterData.View, Permissions.MasterData.Manage)]
public sealed class ShiftsController(IMasterDataService<ShiftDto, ShiftRequest> service)
    : MasterDataController<ShiftDto, ShiftRequest>(service);

[Route("api/v1/break-groups")]
[ResourcePermissions(Permissions.MasterData.View, Permissions.MasterData.Manage)]
public sealed class BreakGroupsController(IMasterDataService<BreakGroupDto, BreakGroupRequest> service)
    : MasterDataController<BreakGroupDto, BreakGroupRequest>(service);

[Route("api/v1/break-schedules")]
[ResourcePermissions(Permissions.MasterData.View, Permissions.MasterData.Manage)]
public sealed class BreakSchedulesController(IMasterDataService<BreakScheduleDto, BreakScheduleRequest> service)
    : MasterDataController<BreakScheduleDto, BreakScheduleRequest>(service);

/// <summary>Duruş nedenleri: tabletteki "Ara ver" ve "Arıza bildir" seçenekleri.</summary>
[Route("api/v1/stop-reasons")]
[ResourcePermissions(Permissions.MasterData.View, Permissions.MasterData.Manage)]
public sealed class StopReasonsController(IMasterDataService<StopReasonDto, StopReasonRequest> service)
    : MasterDataController<StopReasonDto, StopReasonRequest>(service);
