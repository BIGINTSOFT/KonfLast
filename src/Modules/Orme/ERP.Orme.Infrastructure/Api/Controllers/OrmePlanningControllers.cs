using ERP.Core.Application.Common;
using ERP.Core.Infrastructure.Web;
using ERP.Core.Infrastructure.Web.Authorization;
using ERP.Orme.Application.Features.Planning;
using ERP.Orme.Application.Features.Purchasing;
using ERP.Orme.Domain.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Orme.Infrastructure.Api.Controllers;

/// <summary>İplik satın alma: sipariş, onay, kısmi teslim (iplik girişi olarak), kapatma; ihtiyaç planından taslak sipariş.</summary>
[Route("api/v1/orme-purchase-orders")]
public sealed class OrmePurchaseOrdersController(IOrmePurchasingService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(OrmePermissions.Purchase.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<OrmePurchaseOrderListItemDto>>>> GetList(
        [FromQuery] OrmePurchaseOrderQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetListAsync(query, cancellationToken), HttpContext));

    [HttpGet("{id:int}")]
    [HasPermission(OrmePermissions.Purchase.View)]
    public async Task<ActionResult<ApiResponse<OrmePurchaseOrderDto>>> Get(int id, CancellationToken cancellationToken) =>
        FromResult(await service.GetAsync(id, cancellationToken));

    [HttpPost]
    [HasPermission(OrmePermissions.Purchase.Manage)]
    public async Task<ActionResult<ApiResponse<OrmePurchaseOrderDto>>> Create(OrmePurchaseOrderRequest request, CancellationToken cancellationToken)
    {
        var result = await service.SaveAsync(null, request, cancellationToken);
        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, ApiResponse.Success(result.Value, HttpContext))
            : FromResult(result);
    }

    [HttpPut("{id:int}")]
    [HasPermission(OrmePermissions.Purchase.Manage)]
    public async Task<ActionResult<ApiResponse<OrmePurchaseOrderDto>>> Update(int id, OrmePurchaseOrderRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.SaveAsync(id, request, cancellationToken));

    [HttpDelete("{id:int}")]
    [HasPermission(OrmePermissions.Purchase.Manage)]
    public async Task<ActionResult<ApiResponse<object?>>> Delete(int id, CancellationToken cancellationToken) =>
        FromResult(await service.DeleteAsync(id, cancellationToken));

    [HttpPost("{id:int}/approve")]
    [HasPermission(OrmePermissions.Purchase.Manage)]
    public async Task<ActionResult<ApiResponse<OrmePurchaseOrderDto>>> Approve(int id, CancellationToken cancellationToken) =>
        FromResult(await service.ApproveAsync(id, cancellationToken));

    [HttpPost("{id:int}/reopen")]
    [HasPermission(OrmePermissions.Purchase.Manage)]
    public async Task<ActionResult<ApiResponse<OrmePurchaseOrderDto>>> Reopen(int id, CancellationToken cancellationToken) =>
        FromResult(await service.ReopenAsync(id, cancellationToken));

    [HttpPost("{id:int}/cancel")]
    [HasPermission(OrmePermissions.Purchase.Manage)]
    public async Task<ActionResult<ApiResponse<OrmePurchaseOrderDto>>> Cancel(int id, CancellationToken cancellationToken) =>
        FromResult(await service.CancelAsync(id, cancellationToken));

    /// <summary>Kısmi teslim alınmış siparişin kalanını kapatır; kalan artık beklenmez.</summary>
    [HttpPost("{id:int}/close")]
    [HasPermission(OrmePermissions.Purchase.Manage)]
    public async Task<ActionResult<ApiResponse<OrmePurchaseOrderDto>>> Close(int id, CancellationToken cancellationToken) =>
        FromResult(await service.CloseAsync(id, cancellationToken));

    /// <summary>Teslim alır: her satır iplik girişi (lot) olarak girer; lot sipariş satırına bağlanır.</summary>
    [HttpPost("{id:int}/receive")]
    [HasPermission(OrmePermissions.Purchase.Manage)]
    public async Task<ActionResult<ApiResponse<OrmePurchaseOrderDto>>> Receive(int id, OrmePurchaseReceiptRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.ReceiveAsync(id, request, cancellationToken));

    /// <summary>İplik ihtiyaç planındaki eksiklerden her tedarikçi için bir taslak sipariş açar.</summary>
    [HttpPost("from-plan")]
    [HasPermission(OrmePermissions.Purchase.Manage)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<OrmeCreatedPurchaseOrderDto>>>> CreateFromPlan(OrmePurchaseFromPlanRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.CreateFromPlanAsync(request, cancellationToken));
}

/// <summary>İplik ihtiyaç planı: açık siparişlerin iplik ihtiyacı, eldeki stok, yoldaki satın alma ve eksik.</summary>
[Route("api/v1/orme-yarn-plan")]
public sealed class OrmeYarnPlanController(IOrmeYarnPlanService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(OrmePermissions.Purchase.View)]
    public async Task<ActionResult<ApiResponse<OrmeYarnPlanDto>>> Get(CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetAsync(cancellationToken), HttpContext));
}

/// <summary>Makine kapasite planı: makine kuyrukları, tahmini bitişler, doluluk ve termin riski.</summary>
[Route("api/v1/orme-capacity-plan")]
public sealed class OrmeCapacityPlanController(IOrmeCapacityPlanService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(OrmePermissions.Planning.View)]
    public async Task<ActionResult<ApiResponse<OrmeCapacityPlanDto>>> Get([FromQuery] OrmeCapacityPlanQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetAsync(query, cancellationToken), HttpContext));

    /// <summary>Emrin taşınabileceği makineler (aynı plaka) ve kuyruklarının bitişi.</summary>
    [HttpGet("knit-orders/{knitOrderId:int}/options")]
    [HasPermission(OrmePermissions.Planning.View)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<OrmeCapacityMachineOptionDto>>>> GetOptions(int knitOrderId, CancellationToken cancellationToken) =>
        FromResult(await service.GetOptionsAsync(knitOrderId, cancellationToken));

    /// <summary>Emri makine kuyruğuna alır, başka makineye taşır ya da plandan çıkarır.</summary>
    [HttpPut("knit-orders/{knitOrderId:int}")]
    [HasPermission(OrmePermissions.Planning.Manage)]
    public async Task<ActionResult<ApiResponse<object?>>> Plan(int knitOrderId, OrmePlanKnitOrderRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.PlanAsync(knitOrderId, request, cancellationToken));

    /// <summary>Makine kuyruğunun sırasını değiştirir.</summary>
    [HttpPut("machines/{machineId:int}/queue")]
    [HasPermission(OrmePermissions.Planning.Manage)]
    public async Task<ActionResult<ApiResponse<object?>>> Reorder(int machineId, OrmeMachineQueueRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.ReorderAsync(machineId, request, cancellationToken));
}
