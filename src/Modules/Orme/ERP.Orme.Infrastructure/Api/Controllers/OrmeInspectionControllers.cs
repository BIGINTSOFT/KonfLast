using ERP.Core.Application.Common;
using ERP.Core.Application.Features.MasterData;
using ERP.Core.Infrastructure.Web;
using ERP.Core.Infrastructure.Web.Authorization;
using ERP.Orme.Application.Features.Inspection;
using ERP.Orme.Application.Features.Knitting;
using ERP.Orme.Domain.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Orme.Infrastructure.Api.Controllers;

[Route("api/v1/orme-defect-types")]
[ResourcePermissions(OrmePermissions.Inspection.View, OrmePermissions.Inspection.Manage)]
public sealed class OrmeDefectTypesController(IMasterDataService<OrmeDefectTypeDto, OrmeDefectTypeRequest> service)
    : MasterDataController<OrmeDefectTypeDto, OrmeDefectTypeRequest>(service);

/// <summary>Ham kontrol (4 puan): hatalar, puan, kalite, en ve gramaj ölçümü, yetkili kararı.</summary>
[Route("api/v1/orme-inspection")]
public sealed class OrmeInspectionController(IOrmeInspectionService service, IOrmeKnitOrderService knitting) : ApiControllerBase
{
    [HttpGet("rolls")]
    [HasPermission(OrmePermissions.Inspection.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<OrmeFabricRollDto>>>> GetRolls([FromQuery] OrmeFabricRollQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await knitting.GetRollsAsync(query, cancellationToken), HttpContext));

    [HttpGet("rolls/{id:int}")]
    [HasPermission(OrmePermissions.Inspection.View)]
    public async Task<ActionResult<ApiResponse<OrmeRollInspectionDto>>> Get(int id, CancellationToken cancellationToken) =>
        FromResult(await service.GetAsync(id, cancellationToken));

    /// <summary>Kontrolü kaydeder; yeniden kontrol önceki hataları ve yetkili kararını siler.</summary>
    [HttpPut("rolls/{id:int}")]
    [HasPermission(OrmePermissions.Inspection.Manage)]
    public async Task<ActionResult<ApiResponse<OrmeRollInspectionDto>>> Save(int id, SaveOrmeRollInspectionRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.SaveAsync(id, request, cancellationToken));

    [HttpPost("rolls/{id:int}/override")]
    [HasPermission(OrmePermissions.Inspection.Override)]
    public async Task<ActionResult<ApiResponse<OrmeRollInspectionDto>>> Override(int id, OverrideOrmeRollGradeRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.OverrideAsync(id, request, cancellationToken));

    [HttpGet("settings")]
    [HasPermission(OrmePermissions.Inspection.View)]
    public async Task<ActionResult<ApiResponse<OrmeQualitySettingsDto>>> GetSettings(CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetSettingsAsync(cancellationToken), HttpContext));

    [HttpPut("settings")]
    [HasPermission(OrmePermissions.Inspection.Manage)]
    public async Task<ActionResult<ApiResponse<OrmeQualitySettingsDto>>> SaveSettings(OrmeQualitySettingsDto request, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.SaveSettingsAsync(request, cancellationToken), HttpContext));
}
