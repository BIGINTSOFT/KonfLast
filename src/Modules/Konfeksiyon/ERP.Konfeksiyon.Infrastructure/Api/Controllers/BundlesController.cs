using ERP.Konfeksiyon.Domain.Authorization;
using ERP.Core.Infrastructure.Web.Authorization;
using ERP.Core.Infrastructure.Web;
using ERP.Core.Application.Common;
using ERP.Konfeksiyon.Application.Features.Bundles;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Konfeksiyon.Infrastructure.Api.Controllers;

[Route("api/v1")]
public sealed class BundlesController(IBundleService service) : ApiControllerBase
{
    /// <summary>Demet listesi, en yeniden eskiye (sayfalı; demet/sipariş no, sipariş, durum, renk, beden filtresi).</summary>
    [HttpGet("bundles")]
    [HasPermission(KonfeksiyonPermissions.Bundle.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<BundleListItemDto>>>> GetList(
        [FromQuery] BundleListQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetListAsync(query, cancellationToken), HttpContext));

    /// <summary>Demet ayrıntısı: rotadaki her adımın durumu, adetleri ve demetin şu an bulunduğu yer.</summary>
    [HttpGet("bundles/{id:int}")]
    [HasPermission(KonfeksiyonPermissions.Bundle.View)]
    public async Task<ActionResult<ApiResponse<BundleDto>>> Get(int id, CancellationToken cancellationToken) =>
        FromResult(await service.GetAsync(id, cancellationToken));

    /// <summary>Tek demet oluşturur; operasyonları modelin etkin rotasından üretilir.</summary>
    [HttpPost("bundles")]
    [HasPermission(KonfeksiyonPermissions.Bundle.Create)]
    public async Task<ActionResult<ApiResponse<BundleDto>>> Create(
        CreateBundleRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(Get), new { id = result.Value.Id }, ApiResponse.Success(result.Value, HttpContext))
            : FromResult(result);
    }

    /// <summary>Üretime girmemiş demeti iptal eder; adedi siparişte yeniden demetlenebilir hale gelir.</summary>
    [HttpPost("bundles/{id:int}/cancel")]
    [HasPermission(KonfeksiyonPermissions.Bundle.Cancel)]
    public async Task<ActionResult<ApiResponse<BundleDto>>> Cancel(
        int id, BundleVersionRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.CancelAsync(id, request.RowVersion, cancellationToken));

    /// <summary>Siparişin kalem bazında sipariş / demetlenen / kalan adetleri ve demetlenebilir olup olmadığı.</summary>
    [HttpGet("production-orders/{orderId:int}/bundle-summary")]
    [HasPermission(KonfeksiyonPermissions.Bundle.View)]
    public async Task<ActionResult<ApiResponse<OrderBundleSummaryDto>>> GetOrderSummary(
        int orderId, CancellationToken cancellationToken) =>
        FromResult(await service.GetOrderSummaryAsync(orderId, cancellationToken));

    /// <summary>
    /// Siparişin kalan adetlerini verilen demet boyuna bölerek toplu demet oluşturur (artan adet son demete kalır).
    /// </summary>
    [HttpPost("production-orders/{orderId:int}/bundles/generate")]
    [HasPermission(KonfeksiyonPermissions.Bundle.Create)]
    public async Task<ActionResult<ApiResponse<GenerateBundlesResultDto>>> Generate(
        int orderId, GenerateBundlesRequest request, CancellationToken cancellationToken)
    {
        var result = await service.GenerateAsync(orderId, request, cancellationToken);
        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, ApiResponse.Success(result.Value, HttpContext))
            : FromResult(result);
    }
}
