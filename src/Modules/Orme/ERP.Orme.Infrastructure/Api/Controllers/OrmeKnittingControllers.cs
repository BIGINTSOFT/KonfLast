using ERP.Core.Application.Common;
using ERP.Core.Infrastructure.Web;
using ERP.Core.Infrastructure.Web.Authorization;
using ERP.Orme.Application.Features.Knitting;
using ERP.Orme.Domain.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Orme.Infrastructure.Api.Controllers;

/// <summary>Örme emirleri: makine ataması, iplik verme ve geri alma, top kaydı.</summary>
[Route("api/v1/orme-knit-orders")]
public sealed class OrmeKnitOrdersController(IOrmeKnitOrderService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(OrmePermissions.Knitting.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<OrmeKnitOrderListItemDto>>>> GetList(
        [FromQuery] OrmeKnitOrderQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetListAsync(query, cancellationToken), HttpContext));

    [HttpGet("{id:int}")]
    [HasPermission(OrmePermissions.Knitting.View)]
    public async Task<ActionResult<ApiResponse<OrmeKnitOrderDto>>> Get(int id, CancellationToken cancellationToken) =>
        FromResult(await service.GetAsync(id, cancellationToken));

    [HttpPost]
    [HasPermission(OrmePermissions.Knitting.Manage)]
    public async Task<ActionResult<ApiResponse<OrmeKnitOrderDto>>> Create(CreateOrmeKnitOrderRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken);
        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, ApiResponse.Success(result.Value, HttpContext))
            : FromResult(result);
    }

    [HttpPut("{id:int}")]
    [HasPermission(OrmePermissions.Knitting.Manage)]
    public async Task<ActionResult<ApiResponse<OrmeKnitOrderDto>>> Update(int id, UpdateOrmeKnitOrderRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.UpdateAsync(id, request, cancellationToken));

    [HttpPost("{id:int}/complete")]
    [HasPermission(OrmePermissions.Knitting.Manage)]
    public async Task<ActionResult<ApiResponse<OrmeKnitOrderDto>>> Complete(int id, CancellationToken cancellationToken) =>
        FromResult(await service.CompleteAsync(id, cancellationToken));

    [HttpPost("{id:int}/reopen")]
    [HasPermission(OrmePermissions.Knitting.Manage)]
    public async Task<ActionResult<ApiResponse<OrmeKnitOrderDto>>> Reopen(int id, CancellationToken cancellationToken) =>
        FromResult(await service.ReopenAsync(id, cancellationToken));

    [HttpPost("{id:int}/cancel")]
    [HasPermission(OrmePermissions.Knitting.Manage)]
    public async Task<ActionResult<ApiResponse<OrmeKnitOrderDto>>> Cancel(int id, CancellationToken cancellationToken) =>
        FromResult(await service.CancelAsync(id, cancellationToken));

    [HttpGet("{id:int}/machine-options")]
    [HasPermission(OrmePermissions.Knitting.View)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<OrmeMachineOptionDto>>>> GetMachineOptions(int id, CancellationToken cancellationToken) =>
        FromResult(await service.GetMachineOptionsAsync(id, cancellationToken));

    [HttpPost("{id:int}/machines")]
    [HasPermission(OrmePermissions.Knitting.Manage)]
    public async Task<ActionResult<ApiResponse<OrmeKnitOrderDto>>> AssignMachine(int id, AssignOrmeMachineRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.AssignMachineAsync(id, request, cancellationToken));

    [HttpDelete("{id:int}/machines/{assignmentId:int}")]
    [HasPermission(OrmePermissions.Knitting.Manage)]
    public async Task<ActionResult<ApiResponse<OrmeKnitOrderDto>>> ReleaseMachine(int id, int assignmentId, CancellationToken cancellationToken) =>
        FromResult(await service.ReleaseMachineAsync(id, assignmentId, cancellationToken));

    [HttpGet("{id:int}/issuable-lots")]
    [HasPermission(OrmePermissions.Knitting.View)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<OrmeIssuableLotDto>>>> GetIssuableLots(int id, CancellationToken cancellationToken) =>
        FromResult(await service.GetIssuableLotsAsync(id, cancellationToken));

    [HttpPost("{id:int}/issues")]
    [HasPermission(OrmePermissions.Knitting.Manage)]
    public async Task<ActionResult<ApiResponse<OrmeKnitOrderDto>>> AddIssue(int id, OrmeYarnIssueRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.AddIssueAsync(id, request, cancellationToken));

    [HttpDelete("{id:int}/issues/{issueId:int}")]
    [HasPermission(OrmePermissions.Knitting.Manage)]
    public async Task<ActionResult<ApiResponse<OrmeKnitOrderDto>>> RemoveIssue(int id, int issueId, CancellationToken cancellationToken) =>
        FromResult(await service.RemoveIssueAsync(id, issueId, cancellationToken));

    [HttpPost("{id:int}/take-backs")]
    [HasPermission(OrmePermissions.Knitting.Manage)]
    public async Task<ActionResult<ApiResponse<OrmeKnitOrderDto>>> AddTakeBack(int id, OrmeYarnTakeBackRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.AddTakeBackAsync(id, request, cancellationToken));

    [HttpDelete("{id:int}/take-backs/{takeBackId:int}")]
    [HasPermission(OrmePermissions.Knitting.Manage)]
    public async Task<ActionResult<ApiResponse<OrmeKnitOrderDto>>> RemoveTakeBack(int id, int takeBackId, CancellationToken cancellationToken) =>
        FromResult(await service.RemoveTakeBackAsync(id, takeBackId, cancellationToken));

    [HttpPost("{id:int}/rolls")]
    [HasPermission(OrmePermissions.Knitting.Manage)]
    public async Task<ActionResult<ApiResponse<OrmeKnitOrderDto>>> AddRoll(int id, OrmeFabricRollRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.AddRollAsync(id, request, cancellationToken));

    [HttpDelete("{id:int}/rolls/{rollId:int}")]
    [HasPermission(OrmePermissions.Knitting.Manage)]
    public async Task<ActionResult<ApiResponse<OrmeKnitOrderDto>>> RemoveRoll(int id, int rollId, CancellationToken cancellationToken) =>
        FromResult(await service.RemoveRollAsync(id, rollId, cancellationToken));
}

/// <summary>Ham toplar: makineden çıkan topların listesi.</summary>
[Route("api/v1/orme-rolls")]
public sealed class OrmeRollsController(IOrmeKnitOrderService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(OrmePermissions.Knitting.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<OrmeFabricRollDto>>>> GetList(
        [FromQuery] OrmeFabricRollQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetRollsAsync(query, cancellationToken), HttpContext));
}

/// <summary>Makine panosu: hangi makinede hangi emir var, ne kadar örüldü, ne kadar kaldı.</summary>
[Route("api/v1/orme-machine-board")]
public sealed class OrmeMachineBoardController(IOrmeKnitOrderService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(OrmePermissions.Knitting.View)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<OrmeMachineBoardItemDto>>>> Get(CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetBoardAsync(cancellationToken), HttpContext));
}
