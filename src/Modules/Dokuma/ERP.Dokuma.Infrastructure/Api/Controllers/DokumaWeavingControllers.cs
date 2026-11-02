using ERP.Core.Application.Common;
using ERP.Core.Infrastructure.Web;
using ERP.Core.Infrastructure.Web.Authorization;
using ERP.Dokuma.Application.Features.Receipts;
using ERP.Dokuma.Application.Features.Warping;
using ERP.Dokuma.Application.Features.Weaving;
using ERP.Dokuma.Domain.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Dokuma.Infrastructure.Api.Controllers;

/// <summary>Dokuma: leventin tezgâha bağlanması, atkı ipliği, top kesimi ve leventin bitirilmesi.</summary>
[Route("api/v1/dokuma-weaving")]
public sealed class DokumaWeavingController(IDokumaWeavingService service, IDokumaYarnTakeBackService takeBacks) : ApiControllerBase
{
    /// <summary>Etkin tezgâhlar ve üzerlerindeki levent: dokunan ve beklenen metre.</summary>
    [HttpGet("board")]
    [HasPermission(DokumaPermissions.Weaving.View)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DokumaLoomBoardItemDto>>>> GetBoard(CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetBoardAsync(cancellationToken), HttpContext));

    [HttpGet("beams/{id:int}")]
    [HasPermission(DokumaPermissions.Weaving.View)]
    public async Task<ActionResult<ApiResponse<DokumaBeamWeavingDto>>> GetBeam(int id, CancellationToken cancellationToken) =>
        FromResult(await service.GetBeamAsync(id, cancellationToken));

    [HttpPost("beams/{id:int}/mount")]
    [HasPermission(DokumaPermissions.Weaving.Manage)]
    public async Task<ActionResult<ApiResponse<DokumaBeamWeavingDto>>> Mount(int id, MountDokumaBeamRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.MountAsync(id, request, cancellationToken));

    /// <summary>Yanlış bağlanan leventi, top kesilmeden ve atkı verilmeden geri alır.</summary>
    [HttpPost("beams/{id:int}/unmount")]
    [HasPermission(DokumaPermissions.Weaving.Manage)]
    public async Task<ActionResult<ApiResponse<DokumaBeamWeavingDto>>> Unmount(int id, CancellationToken cancellationToken) =>
        FromResult(await service.UnmountAsync(id, cancellationToken));

    [HttpPost("beams/{id:int}/finish")]
    [HasPermission(DokumaPermissions.Weaving.Manage)]
    public async Task<ActionResult<ApiResponse<DokumaBeamWeavingDto>>> Finish(int id, FinishDokumaBeamRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.FinishAsync(id, request, cancellationToken));

    [HttpGet("beams/{id:int}/issuable-weft-lots")]
    [HasPermission(DokumaPermissions.Weaving.View)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DokumaIssuableLotDto>>>> GetIssuableWeftLots(int id, CancellationToken cancellationToken) =>
        FromResult(await service.GetIssuableWeftLotsAsync(id, cancellationToken));

    [HttpPost("beams/{id:int}/weft-issues")]
    [HasPermission(DokumaPermissions.Weaving.Manage)]
    public async Task<ActionResult<ApiResponse<DokumaBeamWeavingDto>>> AddWeftIssue(int id, DokumaWeftIssueRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.AddWeftIssueAsync(id, request, cancellationToken));

    [HttpDelete("beams/{id:int}/weft-issues/{issueId:int}")]
    [HasPermission(DokumaPermissions.Weaving.Manage)]
    public async Task<ActionResult<ApiResponse<DokumaBeamWeavingDto>>> RemoveWeftIssue(int id, int issueId, CancellationToken cancellationToken) =>
        FromResult(await service.RemoveWeftIssueAsync(id, issueId, cancellationToken));

    /// <summary>Atkıdan artan ipliği geri alır: verilenden düşer, lota döner.</summary>
    [HttpPost("beams/{id:int}/take-backs")]
    [HasPermission(DokumaPermissions.Weaving.Manage)]
    public async Task<ActionResult<ApiResponse<object?>>> AddTakeBack(int id, DokumaYarnTakeBackRequest request, CancellationToken cancellationToken) =>
        FromResult(await takeBacks.AddToBeamAsync(id, request, cancellationToken));

    [HttpDelete("beams/{id:int}/take-backs/{takeBackId:int}")]
    [HasPermission(DokumaPermissions.Weaving.Manage)]
    public async Task<ActionResult<ApiResponse<object?>>> RemoveTakeBack(int id, int takeBackId, CancellationToken cancellationToken) =>
        FromResult(await takeBacks.RemoveFromBeamAsync(id, takeBackId, cancellationToken));

    [HttpPost("beams/{id:int}/rolls")]
    [HasPermission(DokumaPermissions.Weaving.Manage)]
    public async Task<ActionResult<ApiResponse<DokumaBeamWeavingDto>>> AddRoll(int id, DokumaFabricRollRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.AddRollAsync(id, request, cancellationToken));

    [HttpDelete("beams/{id:int}/rolls/{rollId:int}")]
    [HasPermission(DokumaPermissions.Weaving.Manage)]
    public async Task<ActionResult<ApiResponse<DokumaBeamWeavingDto>>> RemoveRoll(int id, int rollId, CancellationToken cancellationToken) =>
        FromResult(await service.RemoveRollAsync(id, rollId, cancellationToken));

    [HttpGet("rolls")]
    [HasPermission(DokumaPermissions.Weaving.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<DokumaFabricRollDto>>>> GetRolls([FromQuery] DokumaFabricRollQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetRollsAsync(query, cancellationToken), HttpContext));
}
