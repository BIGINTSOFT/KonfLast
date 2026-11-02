using ERP.Boyahane.Application.Features.Insights;
using ERP.Boyahane.Application.Features.Operations;
using ERP.Boyahane.Domain.Authorization;
using ERP.Core.Application.Common;
using ERP.Core.Application.Features.MasterData;
using ERP.Core.Infrastructure.Web;
using ERP.Core.Infrastructure.Web.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Boyahane.Infrastructure.Api.Controllers;

[Route("api/v1/boyahane-operations")]
[ResourcePermissions(BoyahanePermissions.Operation.View, BoyahanePermissions.Operation.Manage)]
public sealed class BoyahaneOperationsController(IMasterDataService<BoyahaneOperationDto, BoyahaneOperationRequest> service)
    : MasterDataController<BoyahaneOperationDto, BoyahaneOperationRequest>(service);

/// <summary>Operasyon rotaları: işlem türünün adımları. Parti açılırken adımlar rotadan kopyalanır.</summary>
[Route("api/v1/boyahane-routes")]
public sealed class BoyahaneRoutesController(IBoyahaneRouteService service) : ApiControllerBase
{
    /// <summary>Parti formu da rotaları okur.</summary>
    [HttpGet]
    [HasPermission(BoyahanePermissions.Batch.View)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<BoyahaneRouteDto>>>> GetList(CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetListAsync(cancellationToken), HttpContext));

    [HttpPost]
    [HasPermission(BoyahanePermissions.Operation.Manage)]
    public async Task<ActionResult<ApiResponse<BoyahaneRouteDto>>> Create(BoyahaneRouteRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.CreateAsync(request, cancellationToken));

    [HttpPut("{id:int}")]
    [HasPermission(BoyahanePermissions.Operation.Manage)]
    public async Task<ActionResult<ApiResponse<BoyahaneRouteDto>>> Update(int id, BoyahaneRouteRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.UpdateAsync(id, request, cancellationToken));

    [HttpDelete("{id:int}")]
    [HasPermission(BoyahanePermissions.Operation.Manage)]
    public async Task<ActionResult<ApiResponse<object?>>> Delete(int id, CancellationToken cancellationToken) =>
        FromResult(await service.DeleteAsync(id, cancellationToken));
}

/// <summary>Makinenin yapabildiği operasyonlar.</summary>
[Route("api/v1/boyahane-machines/{id:int}/operations")]
public sealed class BoyahaneMachineOperationsController(IBoyahaneRouteService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(BoyahanePermissions.Batch.View)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<int>>>> Get(int id, CancellationToken cancellationToken) =>
        FromResult(await service.GetMachineOperationsAsync(id, cancellationToken));

    [HttpPut]
    [HasPermission(BoyahanePermissions.Batch.Manage)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<int>>>> Set(int id, SetMachineOperationsRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.SetMachineOperationsAsync(id, request.OperationIds, cancellationToken));
}

/// <summary>Salt okunur ekranlar: pano, kurulum rehberi, takip, mal durumu ve raporlar.</summary>
[Route("api/v1/boyahane-insights")]
public sealed class BoyahaneInsightsController(IBoyahaneInsightService service) : ApiControllerBase
{
    [HttpGet("dashboard")]
    [HasPermission(BoyahanePermissions.Batch.View)]
    public async Task<ActionResult<ApiResponse<BoyahaneDashboardDto>>> Dashboard(CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetDashboardAsync(cancellationToken), HttpContext));

    [HttpGet("setup")]
    [HasPermission(BoyahanePermissions.Module.View)]
    public async Task<ActionResult<ApiResponse<BoyahaneSetupDto>>> Setup(CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetSetupAsync(cancellationToken), HttpContext));

    [HttpGet("tracking")]
    [HasPermission(BoyahanePermissions.Batch.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<BoyahaneTrackingRowDto>>>> Tracking([FromQuery] BoyahaneTrackingQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetTrackingAsync(query, cancellationToken), HttpContext));

    [HttpGet("goods")]
    [HasPermission(BoyahanePermissions.Receipt.View)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<BoyahaneGoodsStatusDto>>>> Goods(CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetGoodsStatusAsync(cancellationToken), HttpContext));

    [HttpGet("orders")]
    [HasPermission(BoyahanePermissions.Order.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<BoyahaneOrderTrackingDto>>>> Orders([FromQuery] BoyahaneOrderTrackingQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetOrderTrackingAsync(query, cancellationToken), HttpContext));

    [HttpGet("report")]
    [HasPermission(BoyahanePermissions.Report.View)]
    public async Task<ActionResult<ApiResponse<BoyahaneReportDto>>> Report([FromQuery] BoyahaneReportQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetReportAsync(query, cancellationToken), HttpContext));

    /// <summary>Günlük sevk: onaylı irsaliyeler gün ve müşteri bazında.</summary>
    [HttpGet("report/shipments")]
    [HasPermission(BoyahanePermissions.Report.View)]
    public async Task<ActionResult<ApiResponse<BoyahaneShipmentReportDto>>> Shipments([FromQuery] BoyahaneReportQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetShipmentReportAsync(query, cancellationToken), HttpContext));

    /// <summary>Hareketsiz stok: verilen gün sayısıdır hareket görmeyen adres stoğu ve boya / kimyasal lotları.</summary>
    [HttpGet("report/idle-stock")]
    [HasPermission(BoyahanePermissions.Report.View)]
    public async Task<ActionResult<ApiResponse<BoyahaneIdleStockDto>>> IdleStock([FromQuery] BoyahaneIdleStockQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetIdleStockAsync(query, cancellationToken), HttpContext));
}
