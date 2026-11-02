using ERP.Core.Application.Common;
using ERP.Core.Application.Features.MasterData;
using ERP.Core.Infrastructure.Web;
using ERP.Core.Infrastructure.Web.Authorization;
using ERP.Dokuma.Application.Features.Inspection;
using ERP.Dokuma.Application.Features.Weaving;
using ERP.Dokuma.Domain.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Dokuma.Infrastructure.Api.Controllers;

[Route("api/v1/dokuma-defect-types")]
[ResourcePermissions(DokumaPermissions.Inspection.View, DokumaPermissions.Inspection.Manage)]
public sealed class DokumaDefectTypesController(IMasterDataService<DokumaDefectTypeDto, DokumaDefectTypeRequest> service)
    : MasterDataController<DokumaDefectTypeDto, DokumaDefectTypeRequest>(service);

/// <summary>Ham kontrol (4 puan): hatalar, puan, kalite ve yetkili kararı.</summary>
[Route("api/v1/dokuma-inspection")]
public sealed class DokumaInspectionController(IDokumaInspectionService service, IDokumaWeavingService weaving) : ApiControllerBase
{
    [HttpGet("rolls")]
    [HasPermission(DokumaPermissions.Inspection.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<DokumaFabricRollDto>>>> GetRolls([FromQuery] DokumaFabricRollQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await weaving.GetRollsAsync(query, cancellationToken), HttpContext));

    [HttpGet("rolls/{id:int}")]
    [HasPermission(DokumaPermissions.Inspection.View)]
    public async Task<ActionResult<ApiResponse<DokumaRollInspectionDto>>> Get(int id, CancellationToken cancellationToken) =>
        FromResult(await service.GetAsync(id, cancellationToken));

    /// <summary>Kontrolü kaydeder; yeniden kontrol önceki hataları ve yetkili kararını siler.</summary>
    [HttpPut("rolls/{id:int}")]
    [HasPermission(DokumaPermissions.Inspection.Manage)]
    public async Task<ActionResult<ApiResponse<DokumaRollInspectionDto>>> Save(int id, SaveDokumaRollInspectionRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.SaveAsync(id, request, cancellationToken));

    [HttpPost("rolls/{id:int}/override")]
    [HasPermission(DokumaPermissions.Inspection.Override)]
    public async Task<ActionResult<ApiResponse<DokumaRollInspectionDto>>> Override(int id, OverrideDokumaRollGradeRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.OverrideAsync(id, request, cancellationToken));

    [HttpGet("settings")]
    [HasPermission(DokumaPermissions.Inspection.View)]
    public async Task<ActionResult<ApiResponse<DokumaQualitySettingsDto>>> GetSettings(CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetSettingsAsync(cancellationToken), HttpContext));

    [HttpPut("settings")]
    [HasPermission(DokumaPermissions.Inspection.Manage)]
    public async Task<ActionResult<ApiResponse<DokumaQualitySettingsDto>>> SaveSettings(DokumaQualitySettingsDto request, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.SaveSettingsAsync(request, cancellationToken), HttpContext));
}
