using ERP.Boyahane.Application.Features.Lab;
using ERP.Boyahane.Domain.Authorization;
using ERP.Core.Application.Common;
using ERP.Core.Application.Features.MasterData;
using ERP.Core.Infrastructure.Web;
using ERP.Core.Infrastructure.Web.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Boyahane.Infrastructure.Api.Controllers;

[Route("api/v1/boyahane-dye-chemicals")]
[ResourcePermissions(BoyahanePermissions.Lab.View, BoyahanePermissions.Lab.Manage)]
public sealed class BoyahaneDyeChemicalsController(IMasterDataService<BoyahaneDyeChemicalDto, BoyahaneDyeChemicalRequest> service)
    : MasterDataController<BoyahaneDyeChemicalDto, BoyahaneDyeChemicalRequest>(service);

/// <summary>Laboratuvar: renk talebi, lab dip denemeleri, müşteri kararı. Kabul edilen deneme sipariş satırının reçetesidir.</summary>
[Route("api/v1/boyahane-lab-requests")]
public sealed class BoyahaneLabRequestsController(IBoyahaneLabService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(BoyahanePermissions.Lab.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<BoyahaneLabRequestListItemDto>>>> GetList(
        [FromQuery] BoyahaneLabRequestQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetListAsync(query, cancellationToken), HttpContext));

    [HttpGet("{id:int}")]
    [HasPermission(BoyahanePermissions.Lab.View)]
    public async Task<ActionResult<ApiResponse<BoyahaneLabRequestDto>>> Get(int id, CancellationToken cancellationToken) =>
        FromResult(await service.GetAsync(id, cancellationToken));

    [HttpPost]
    [HasPermission(BoyahanePermissions.Lab.Manage)]
    public async Task<ActionResult<ApiResponse<BoyahaneLabRequestDto>>> Create(CreateBoyahaneLabRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken);
        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, ApiResponse.Success(result.Value, HttpContext))
            : FromResult(result);
    }

    [HttpPost("{id:int}/cancel")]
    [HasPermission(BoyahanePermissions.Lab.Manage)]
    public async Task<ActionResult<ApiResponse<BoyahaneLabRequestDto>>> Cancel(int id, CancellationToken cancellationToken) =>
        FromResult(await service.CancelAsync(id, cancellationToken));

    /// <summary>Yanlış onayı geri alır; kabul edilen deneme karar bekler duruma döner.</summary>
    [HttpPost("{id:int}/reopen")]
    [HasPermission(BoyahanePermissions.Lab.CustomerDecision)]
    public async Task<ActionResult<ApiResponse<BoyahaneLabRequestDto>>> Reopen(int id, CancellationToken cancellationToken) =>
        FromResult(await service.ReopenAsync(id, cancellationToken));

    [HttpPost("{id:int}/trials")]
    [HasPermission(BoyahanePermissions.Lab.Manage)]
    public async Task<ActionResult<ApiResponse<BoyahaneLabRequestDto>>> AddTrial(int id, BoyahaneLabTrialRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.AddTrialAsync(id, request, cancellationToken));

    [HttpPut("{id:int}/trials/{trialId:int}")]
    [HasPermission(BoyahanePermissions.Lab.Manage)]
    public async Task<ActionResult<ApiResponse<BoyahaneLabRequestDto>>> UpdateTrial(int id, int trialId, BoyahaneLabTrialRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.UpdateTrialAsync(id, trialId, request, cancellationToken));

    [HttpDelete("{id:int}/trials/{trialId:int}")]
    [HasPermission(BoyahanePermissions.Lab.Manage)]
    public async Task<ActionResult<ApiResponse<BoyahaneLabRequestDto>>> DeleteTrial(int id, int trialId, CancellationToken cancellationToken) =>
        FromResult(await service.DeleteTrialAsync(id, trialId, cancellationToken));

    /// <summary>Müşterinin denemeyle ilgili kararı; kabul edilirse talep onaylanır ve deneme reçete olur.</summary>
    [HttpPost("{id:int}/trials/{trialId:int}/decision")]
    [HasPermission(BoyahanePermissions.Lab.CustomerDecision)]
    public async Task<ActionResult<ApiResponse<BoyahaneLabRequestDto>>> Decide(int id, int trialId, BoyahaneLabDecisionRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.DecideAsync(id, trialId, request, cancellationToken));

    /// <summary>Aynı müşteri, kart ve renk için daha önce onaylanmış reçeteler.</summary>
    [HttpGet("previous-recipes")]
    [HasPermission(BoyahanePermissions.Lab.View)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<BoyahanePreviousRecipeDto>>>> GetPreviousRecipes(int orderLineId, CancellationToken cancellationToken) =>
        FromResult(await service.GetPreviousRecipesAsync(orderLineId, cancellationToken));

    /// <summary>Laboratuvarı atlar: seçilen önceki reçeteyle satır için onaylı talep açar.</summary>
    [HttpPost("reuse")]
    [HasPermission(BoyahanePermissions.Lab.CustomerDecision)]
    public async Task<ActionResult<ApiResponse<BoyahaneLabRequestDto>>> Reuse(ReuseBoyahaneRecipeRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.ReuseRecipeAsync(request, cancellationToken));
}
