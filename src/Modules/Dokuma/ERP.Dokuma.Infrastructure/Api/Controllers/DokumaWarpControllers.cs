using ERP.Core.Application.Common;
using ERP.Core.Infrastructure.Web;
using ERP.Core.Infrastructure.Web.Authorization;
using ERP.Dokuma.Application.Features.Receipts;
using ERP.Dokuma.Application.Features.Warping;
using ERP.Dokuma.Domain.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Dokuma.Infrastructure.Api.Controllers;

/// <summary>Çözgü emirleri: onaylı sipariş satırından açılır; iplik lottan kesin düşülür, leventler kaydedilir.</summary>
[Route("api/v1/dokuma-warp-orders")]
public sealed class DokumaWarpOrdersController(IDokumaWarpService service, IDokumaYarnTakeBackService takeBacks) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(DokumaPermissions.Warp.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<DokumaWarpOrderListItemDto>>>> GetList(
        [FromQuery] DokumaWarpOrderQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetListAsync(query, cancellationToken), HttpContext));

    [HttpGet("{id:int}")]
    [HasPermission(DokumaPermissions.Warp.View)]
    public async Task<ActionResult<ApiResponse<DokumaWarpOrderDto>>> Get(int id, CancellationToken cancellationToken) =>
        FromResult(await service.GetAsync(id, cancellationToken));

    [HttpPost]
    [HasPermission(DokumaPermissions.Warp.Manage)]
    public async Task<ActionResult<ApiResponse<DokumaWarpOrderDto>>> Create(CreateDokumaWarpOrderRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken);
        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, ApiResponse.Success(result.Value, HttpContext))
            : FromResult(result);
    }

    [HttpPut("{id:int}")]
    [HasPermission(DokumaPermissions.Warp.Manage)]
    public async Task<ActionResult<ApiResponse<DokumaWarpOrderDto>>> Update(int id, UpdateDokumaWarpOrderRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.UpdateAsync(id, request, cancellationToken));

    [HttpPost("{id:int}/complete")]
    [HasPermission(DokumaPermissions.Warp.Manage)]
    public async Task<ActionResult<ApiResponse<DokumaWarpOrderDto>>> Complete(int id, CancellationToken cancellationToken) =>
        FromResult(await service.CompleteAsync(id, cancellationToken));

    /// <summary>Levent kaydedilmemiş çözgü emrini iptal eder; verilen iplik lotlara geri döner.</summary>
    [HttpPost("{id:int}/cancel")]
    [HasPermission(DokumaPermissions.Warp.Manage)]
    public async Task<ActionResult<ApiResponse<DokumaWarpOrderDto>>> Cancel(int id, CancellationToken cancellationToken) =>
        FromResult(await service.CancelAsync(id, cancellationToken));

    /// <summary>Çözgüye verilebilecek lotlar: artikelin çözgü iplikleri, sahibi siparişle aynı, kalanı olan.</summary>
    [HttpGet("{id:int}/issuable-lots")]
    [HasPermission(DokumaPermissions.Warp.View)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DokumaIssuableLotDto>>>> GetIssuableLots(int id, CancellationToken cancellationToken) =>
        FromResult(await service.GetIssuableLotsAsync(id, cancellationToken));

    [HttpPost("{id:int}/issues")]
    [HasPermission(DokumaPermissions.Warp.Manage)]
    public async Task<ActionResult<ApiResponse<DokumaWarpOrderDto>>> AddIssue(int id, DokumaWarpIssueRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.AddIssueAsync(id, request, cancellationToken));

    [HttpDelete("{id:int}/issues/{issueId:int}")]
    [HasPermission(DokumaPermissions.Warp.Manage)]
    public async Task<ActionResult<ApiResponse<DokumaWarpOrderDto>>> RemoveIssue(int id, int issueId, CancellationToken cancellationToken) =>
        FromResult(await service.RemoveIssueAsync(id, issueId, cancellationToken));

    /// <summary>Çözgüden artan ipliği geri alır: verilenden düşer, lota döner. Yalnız biten çözgüde.</summary>
    [HttpPost("{id:int}/take-backs")]
    [HasPermission(DokumaPermissions.Warp.Manage)]
    public async Task<ActionResult<ApiResponse<object?>>> AddTakeBack(int id, DokumaYarnTakeBackRequest request, CancellationToken cancellationToken) =>
        FromResult(await takeBacks.AddToWarpAsync(id, request, cancellationToken));

    [HttpDelete("{id:int}/take-backs/{takeBackId:int}")]
    [HasPermission(DokumaPermissions.Warp.Manage)]
    public async Task<ActionResult<ApiResponse<object?>>> RemoveTakeBack(int id, int takeBackId, CancellationToken cancellationToken) =>
        FromResult(await takeBacks.RemoveFromWarpAsync(id, takeBackId, cancellationToken));

    [HttpPost("{id:int}/beams")]
    [HasPermission(DokumaPermissions.Warp.Manage)]
    public async Task<ActionResult<ApiResponse<DokumaWarpOrderDto>>> AddBeam(int id, DokumaBeamRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.AddBeamAsync(id, request, cancellationToken));

    [HttpPut("{id:int}/beams/{beamId:int}")]
    [HasPermission(DokumaPermissions.Warp.Manage)]
    public async Task<ActionResult<ApiResponse<DokumaWarpOrderDto>>> UpdateBeam(int id, int beamId, DokumaBeamRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.UpdateBeamAsync(id, beamId, request, cancellationToken));

    [HttpDelete("{id:int}/beams/{beamId:int}")]
    [HasPermission(DokumaPermissions.Warp.Manage)]
    public async Task<ActionResult<ApiResponse<DokumaWarpOrderDto>>> RemoveBeam(int id, int beamId, CancellationToken cancellationToken) =>
        FromResult(await service.RemoveBeamAsync(id, beamId, cancellationToken));
}

/// <summary>Leventler: haşıl (içeride ya da fason) ve tahar / düğümle tezgâha hazırlanır.</summary>
[Route("api/v1/dokuma-beams")]
public sealed class DokumaBeamsController(IDokumaWarpService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(DokumaPermissions.Warp.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<DokumaBeamDto>>>> GetList([FromQuery] DokumaBeamQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetBeamsAsync(query, cancellationToken), HttpContext));

    [HttpPost("{id:int}/send-to-sizing")]
    [HasPermission(DokumaPermissions.Warp.Manage)]
    public async Task<ActionResult<ApiResponse<DokumaBeamDto>>> SendToSizing(int id, SendDokumaBeamToSizingRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.SendToSizingAsync(id, request, cancellationToken));

    [HttpPost("{id:int}/receive-from-sizing")]
    [HasPermission(DokumaPermissions.Warp.Manage)]
    public async Task<ActionResult<ApiResponse<DokumaBeamDto>>> ReceiveFromSizing(int id, ReceiveDokumaBeamFromSizingRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.ReceiveFromSizingAsync(id, request, cancellationToken));

    /// <summary>İçeride haşıl: reçete, gerçekleşen alım / viskozite ve kullanılan kimyasallar (malzeme stoğundan düşülür).</summary>
    [HttpPost("{id:int}/mark-sized")]
    [HasPermission(DokumaPermissions.Warp.Manage)]
    public async Task<ActionResult<ApiResponse<DokumaBeamDto>>> MarkSized(int id, MarkDokumaBeamSizedRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.MarkSizedAsync(id, request, cancellationToken));

    /// <summary>Haşıl için artikelin reçeteleri, leventin tahmini ipliği ve kimyasal stoğu.</summary>
    [HttpGet("{id:int}/sizing-plan")]
    [HasPermission(DokumaPermissions.Warp.View)]
    public async Task<ActionResult<ApiResponse<DokumaBeamSizingPlanDto>>> SizingPlan(int id, CancellationToken cancellationToken) =>
        FromResult(await service.GetSizingPlanAsync(id, cancellationToken));

    /// <summary>Haşıl belgesi (haşılcıya gönderme formu).</summary>
    [HttpGet("{id:int}/sizing-document")]
    [HasPermission(DokumaPermissions.Warp.View)]
    public async Task<ActionResult<ApiResponse<DokumaBeamSizingDocumentDto>>> SizingDocument(int id, CancellationToken cancellationToken) =>
        FromResult(await service.GetSizingDocumentAsync(id, cancellationToken));

    [HttpPost("{id:int}/ready")]
    [HasPermission(DokumaPermissions.Warp.Manage)]
    public async Task<ActionResult<ApiResponse<DokumaBeamDto>>> Ready(int id, DokumaBeamReadyRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.MarkReadyAsync(id, request, cancellationToken));

    [HttpPost("{id:int}/undo-ready")]
    [HasPermission(DokumaPermissions.Warp.Manage)]
    public async Task<ActionResult<ApiResponse<DokumaBeamDto>>> UndoReady(int id, CancellationToken cancellationToken) =>
        FromResult(await service.UndoReadyAsync(id, cancellationToken));
}
