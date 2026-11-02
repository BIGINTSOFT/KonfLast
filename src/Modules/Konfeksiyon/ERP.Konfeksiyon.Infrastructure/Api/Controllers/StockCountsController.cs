using ERP.Konfeksiyon.Domain.Authorization;
using ERP.Core.Infrastructure.Web.Authorization;
using ERP.Core.Infrastructure.Web;
using ERP.Core.Application.Common;
using ERP.Konfeksiyon.Application.Features.Warehousing;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Konfeksiyon.Infrastructure.Api.Controllers;

/// <summary>
/// Sayım fişleri. Açık bir fiş, kapsamındaki adreslerde stok hareketini engeller (STOCK_COUNT_IN_PROGRESS);
/// onaylanınca sayılan satırların farkı stok düzeltmesi olarak yazılır.
/// </summary>
[Route("api/v1/stock-counts")]
public sealed class StockCountsController(IStockCountService service) : ApiControllerBase
{
    /// <summary>Sayım fişleri, en yeniden eskiye (sayfalı; depo, durum, sayım no araması).</summary>
    [HttpGet]
    [HasPermission(KonfeksiyonPermissions.Stock.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<StockCountListItemDto>>>> GetList(
        [FromQuery] StockCountListQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetListAsync(query, cancellationToken), HttpContext));

    [HttpGet("{id:int}")]
    [HasPermission(KonfeksiyonPermissions.Stock.View)]
    public async Task<ActionResult<ApiResponse<StockCountDto>>> Get(int id, CancellationToken cancellationToken) =>
        FromResult(await service.GetAsync(id, cancellationToken));

    /// <summary>
    /// Sayım açar: depodaki (ya da verilen adresteki) stok fişe beklenen miktar olarak yazılır ve kapsam kilitlenir.
    /// Aynı kapsamda açık fiş varsa STOCK_COUNT_IN_PROGRESS döner.
    /// </summary>
    [HttpPost]
    [HasPermission(KonfeksiyonPermissions.Stock.Manage)]
    public async Task<ActionResult<ApiResponse<StockCountDto>>> Open(
        OpenStockCountRequest request, CancellationToken cancellationToken)
    {
        var result = await service.OpenAsync(request, cancellationToken);
        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, ApiResponse.Success(result.Value, HttpContext))
            : FromResult(result);
    }

    /// <summary>Sayılan miktarları kaydeder (boş miktar: sayılmadı); stok değişmez.</summary>
    [HttpPut("{id:int}/lines")]
    [HasPermission(KonfeksiyonPermissions.Stock.Manage)]
    public async Task<ActionResult<ApiResponse<StockCountDto>>> SaveLines(
        int id, SaveStockCountLinesRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.SaveLinesAsync(id, request, cancellationToken));

    /// <summary>Fişte olmayan, sayımda bulunan stok için satır ekler.</summary>
    [HttpPost("{id:int}/lines")]
    [HasPermission(KonfeksiyonPermissions.Stock.Manage)]
    public async Task<ActionResult<ApiResponse<StockCountDto>>> AddLine(
        int id, AddStockCountLineRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.AddLineAsync(id, request, cancellationToken));

    /// <summary>
    /// Fişi onaylar: sayılan satırların farkı sayım düzeltmesi olarak stoğa işlenir, kilit kalkar. Sayılmayan
    /// satırlar stoğu değiştirmez; hiç sayılan satır yoksa STOCK_COUNT_EMPTY döner.
    /// </summary>
    [HttpPost("{id:int}/post")]
    [HasPermission(KonfeksiyonPermissions.Stock.Adjust)]
    public async Task<ActionResult<ApiResponse<StockCountDto>>> Post(
        int id, StockCountActionRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.PostAsync(id, request, cancellationToken));

    /// <summary>Fişi iptal eder: stok değişmez, kilit kalkar.</summary>
    [HttpPost("{id:int}/cancel")]
    [HasPermission(KonfeksiyonPermissions.Stock.Manage)]
    public async Task<ActionResult<ApiResponse<StockCountDto>>> Cancel(
        int id, StockCountActionRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.CancelAsync(id, request, cancellationToken));
}
