using ERP.Konfeksiyon.Domain.Authorization;
using ERP.Core.Infrastructure.Web.Authorization;
using ERP.Core.Infrastructure.Web;
using ERP.Konfeksiyon.Application.Features.Planning;
using ERP.Konfeksiyon.Application.Features.ProductionOrders;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Konfeksiyon.Infrastructure.Api.Controllers;

/// <summary>
/// Planlama yardımcısı: sipariş riski, bant yükü ve plan önerisi. Hiçbir şey kaydetmez; plan
/// POST /api/v1/line-plans ile oluşturulur.
/// </summary>
[Route("api/v1/planning")]
[HasPermission(KonfeksiyonPermissions.ProductionLine.View)]
public sealed class PlanningController(IPlanningService service, IPlanningDemandService demand) : ApiControllerBase
{
    /// <summary>
    /// Açık (onaylı / üretimdeki) siparişler: biten, kalan, planlanan ve planlanmamış adet ile termin riski
    /// (LATE, PLAN_AFTER_DUE_DATE, UNPLANNED, ON_TRACK). En riskli ve termini en yakın olan önce; en fazla 500.
    /// </summary>
    [HttpGet("orders")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PlanningOrderDto>>>> GetOrders(CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetOrdersAsync(cancellationToken), HttpContext));

    /// <summary>
    /// Aralıkta (UTC; en fazla 92 gün) bantların çalışma süresi, planlara ayrılan kısmı ve her planın gereken
    /// süresinin penceresine sığıp sığmadığı (OK, TIGHT, OVERLOADED, NO_CAPACITY, NO_ROUTE).
    /// </summary>
    [HttpGet("line-load")]
    public async Task<ActionResult<ApiResponse<LineLoadReportDto>>> GetLineLoad(
        [FromQuery] LineLoadQuery query, CancellationToken cancellationToken) =>
        FromResult(await service.GetLineLoadAsync(query, cancellationToken));

    /// <summary>
    /// Sipariş için her banta yerleştirme önerisi: bant hızı, gereken süre, mevcut planlarla çakışmayan en erken
    /// başlangıç, takvime göre bitiş ve terme yetişip yetişmediği. Uygun bantlar önce, en erken biten üstte.
    /// </summary>
    [HttpGet("suggestions")]
    public async Task<ActionResult<ApiResponse<PlanSuggestionDto>>> Suggest(
        [FromQuery] PlanSuggestionQuery query, CancellationToken cancellationToken) =>
        FromResult(await service.SuggestAsync(query, cancellationToken));

    /// <summary>
    /// Planlanacak siparişler: onaylı müşteri siparişlerinin stoktan ya da üretim siparişinden karşılanmamış adedi
    /// (sipariş ve ürün başına, renk / beden dağılımıyla), termini en yakın önce.
    /// </summary>
    [HttpGet("demand")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PlanningDemandDto>>>> GetDemand(CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await demand.GetDemandAsync(cancellationToken), HttpContext));

    /// <summary>
    /// Planlanacak adetten üretim siparişi açar (taslak). Sipariş bundan sonra Üretim Siparişleri ekranında onaylanır.
    /// </summary>
    [HttpPost("demand/production-orders")]
    [HasPermission(KonfeksiyonPermissions.ProductionOrder.Create)]
    public async Task<ActionResult<ApiResponse<ProductionOrderDto>>> CreateProductionOrder(
        CreateProductionFromDemandRequest request, CancellationToken cancellationToken) =>
        FromResult(await demand.CreateProductionOrderAsync(request, cancellationToken));
}
