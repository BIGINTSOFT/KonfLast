using ERP.Konfeksiyon.Domain.Authorization;
using ERP.Core.Infrastructure.Web.Authorization;
using ERP.Core.Infrastructure.Web;
using ERP.Core.Application.Common;
using ERP.Konfeksiyon.Application.Features.ProductionRoutes;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Konfeksiyon.Infrastructure.Api.Controllers;

[Route("api/v1/production-routes")]
public sealed class ProductionRoutesController(IProductionRouteService service) : ApiControllerBase
{
    /// <summary>Rota listesi, modele ve sürüme göre sıralı (sayfalı; arama, model ve durum filtresi).</summary>
    [HttpGet]
    [HasPermission(KonfeksiyonPermissions.ProductionRoute.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<ProductionRouteListItemDto>>>> GetList(
        [FromQuery] ProductionRouteListQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetListAsync(query, cancellationToken), HttpContext));

    /// <summary>Rota ayrıntısı: operasyon adımları, öncülleri ve akıştaki aşamaları.</summary>
    [HttpGet("{id:int}")]
    [HasPermission(KonfeksiyonPermissions.ProductionRoute.View)]
    public async Task<ActionResult<ApiResponse<ProductionRouteDto>>> Get(int id, CancellationToken cancellationToken) =>
        FromResult(await service.GetAsync(id, cancellationToken));

    /// <summary>Model için yeni bir taslak rota (sıradaki sürüm) oluşturur.</summary>
    [HttpPost]
    [HasPermission(KonfeksiyonPermissions.ProductionRoute.Manage)]
    public async Task<ActionResult<ApiResponse<ProductionRouteDto>>> Create(
        ProductionRouteRequest request, CancellationToken cancellationToken) =>
        Created(await service.CreateAsync(request, cancellationToken));

    /// <summary>Taslak rotanın adımlarını ve bağımlılıklarını günceller (rowVersion zorunlu).</summary>
    [HttpPut("{id:int}")]
    [HasPermission(KonfeksiyonPermissions.ProductionRoute.Manage)]
    public async Task<ActionResult<ApiResponse<ProductionRouteDto>>> Update(
        int id, ProductionRouteRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.UpdateAsync(id, request, cancellationToken));

    /// <summary>Rotayı modelin geçerli rotası yapar ve kilitler; önceki etkin rota arşivlenir.</summary>
    [HttpPost("{id:int}/activate")]
    [HasPermission(KonfeksiyonPermissions.ProductionRoute.Activate)]
    public async Task<ActionResult<ApiResponse<ProductionRouteDto>>> Activate(
        int id, RouteVersionRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.ActivateAsync(id, request.RowVersion, cancellationToken));

    /// <summary>Etkin rotayı kullanımdan kaldırır (modelin etkin rotası kalmaz).</summary>
    [HttpPost("{id:int}/archive")]
    [HasPermission(KonfeksiyonPermissions.ProductionRoute.Manage)]
    public async Task<ActionResult<ApiResponse<ProductionRouteDto>>> Archive(
        int id, RouteVersionRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.ArchiveAsync(id, request.RowVersion, cancellationToken));

    /// <summary>Rotayı yeni bir taslak sürüm olarak kopyalar (kilitli rotada değişiklik yapmanın yolu).</summary>
    [HttpPost("{id:int}/copy")]
    [HasPermission(KonfeksiyonPermissions.ProductionRoute.Manage)]
    public async Task<ActionResult<ApiResponse<ProductionRouteDto>>> Copy(int id, CancellationToken cancellationToken) =>
        Created(await service.CopyAsync(id, cancellationToken));

    /// <summary>Taslak rotayı siler (soft delete); sürüm numarası yeniden kullanılmaz.</summary>
    [HttpDelete("{id:int}")]
    [HasPermission(KonfeksiyonPermissions.ProductionRoute.Manage)]
    public async Task<ActionResult<ApiResponse<object?>>> Delete(int id, CancellationToken cancellationToken) =>
        FromResult(await service.DeleteAsync(id, cancellationToken));

    /// <summary>
    /// Adımın fason ayarını (fason mu, varsayılan fasoncu) değiştirir; akışı ve SAM'i değiştirmediği için etkin rotada
    /// da yapılabilir.
    /// </summary>
    [HttpPut("{id:int}/operations/{routeOperationId:int}/subcontract")]
    [HasPermission(KonfeksiyonPermissions.ProductionRoute.Manage)]
    public async Task<ActionResult<ApiResponse<ProductionRouteDto>>> SetSubcontract(
        int id, int routeOperationId, RouteOperationSubcontractRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.SetSubcontractAsync(id, routeOperationId, request, cancellationToken));

    private ActionResult<ApiResponse<ProductionRouteDto>> Created(Result<ProductionRouteDto> result) =>
        result.IsSuccess
            ? CreatedAtAction(nameof(Get), new { id = result.Value.Id }, ApiResponse.Success(result.Value, HttpContext))
            : FromResult(result);
}
