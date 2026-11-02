using ERP.Core.Application.Common;
using ERP.Core.Application.Features.MasterData;
using ERP.Core.Infrastructure.Web;
using ERP.Core.Infrastructure.Web.Authorization;
using ERP.Dokuma.Application.Features.Sizing;
using ERP.Dokuma.Application.Features.Stock;
using ERP.Dokuma.Domain.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Dokuma.Infrastructure.Api.Controllers;

/// <summary>Malzeme kartları: haşıl kimyasalı, yedek parça ve sarf malzemesi.</summary>
[Route("api/v1/dokuma-materials")]
[ResourcePermissions(DokumaPermissions.Material.View, DokumaPermissions.Material.Manage)]
public sealed class DokumaMaterialsController(IMasterDataService<DokumaMaterialDto, DokumaMaterialRequest> service)
    : MasterDataController<DokumaMaterialDto, DokumaMaterialRequest>(service);

/// <summary>Haşıl reçeteleri: artikel başına, kimyasallar banyo yüzdesiyle.</summary>
[Route("api/v1/dokuma-sizing-recipes")]
public sealed class DokumaSizingRecipesController(IDokumaSizingRecipeService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(DokumaPermissions.Material.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<DokumaSizingRecipeListItemDto>>>> GetList(
        [FromQuery] DokumaSizingRecipeQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetListAsync(query, cancellationToken), HttpContext));

    [HttpGet("{id:int}")]
    [HasPermission(DokumaPermissions.Material.View)]
    public async Task<ActionResult<ApiResponse<DokumaSizingRecipeDto>>> Get(int id, CancellationToken cancellationToken) =>
        FromResult(await service.GetAsync(id, cancellationToken));

    [HttpPost]
    [HasPermission(DokumaPermissions.Material.Manage)]
    public async Task<ActionResult<ApiResponse<DokumaSizingRecipeDto>>> Create(DokumaSizingRecipeRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken);
        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, ApiResponse.Success(result.Value, HttpContext))
            : FromResult(result);
    }

    [HttpPut("{id:int}")]
    [HasPermission(DokumaPermissions.Material.Manage)]
    public async Task<ActionResult<ApiResponse<DokumaSizingRecipeDto>>> Update(int id, DokumaSizingRecipeRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.UpdateAsync(id, request, cancellationToken));

    [HttpDelete("{id:int}")]
    [HasPermission(DokumaPermissions.Material.Manage)]
    public async Task<ActionResult<ApiResponse<object?>>> Delete(int id, CancellationToken cancellationToken) =>
        FromResult(await service.DeleteAsync(id, cancellationToken));
}

/// <summary>Malzeme stoğu: lot girişi, tezgâha / bölüme çıkış, lot düzeltmesi, sayım ve hareketler.</summary>
[Route("api/v1/dokuma-material-stock")]
public sealed class DokumaMaterialStockController(IDokumaMaterialStockService service) : ApiControllerBase
{
    /// <summary>Malzeme başına stok, siparişte bekleyen ve kritik durum.</summary>
    [HttpGet]
    [HasPermission(DokumaPermissions.Stock.View)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DokumaMaterialStockRowDto>>>> GetStock(
        [FromQuery] DokumaMaterialStockQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetStockAsync(query, cancellationToken), HttpContext));

    [HttpGet("lots")]
    [HasPermission(DokumaPermissions.Stock.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<DokumaMaterialLotDto>>>> GetLots(
        [FromQuery] DokumaMaterialLotQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetLotsAsync(query, cancellationToken), HttpContext));

    [HttpGet("movements")]
    [HasPermission(DokumaPermissions.Stock.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<DokumaMaterialMovementDto>>>> GetMovements(
        [FromQuery] DokumaMaterialMovementQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetMovementsAsync(query, cancellationToken), HttpContext));

    [HttpPost("lots")]
    [HasPermission(DokumaPermissions.Stock.Manage)]
    public async Task<ActionResult<ApiResponse<DokumaMaterialLotDto>>> CreateLot(DokumaMaterialLotRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateLotAsync(request, cancellationToken);
        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, ApiResponse.Success(result.Value, HttpContext))
            : FromResult(result);
    }

    [HttpPut("lots/{id:int}")]
    [HasPermission(DokumaPermissions.Stock.Manage)]
    public async Task<ActionResult<ApiResponse<DokumaMaterialLotDto>>> UpdateLot(int id, DokumaMaterialLotRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.UpdateLotAsync(id, request, cancellationToken));

    [HttpDelete("lots/{id:int}")]
    [HasPermission(DokumaPermissions.Stock.Manage)]
    public async Task<ActionResult<ApiResponse<object?>>> DeleteLot(int id, CancellationToken cancellationToken) =>
        FromResult(await service.DeleteLotAsync(id, cancellationToken));

    /// <summary>Lotun kalanını sayılan miktara düzeltir; fark gerekçesiyle hareket olarak yazılır.</summary>
    [HttpPost("lots/{id:int}/adjust")]
    [HasPermission(DokumaPermissions.Stock.Manage)]
    public async Task<ActionResult<ApiResponse<DokumaMaterialLotDto>>> AdjustLot(int id, DokumaMaterialLotAdjustRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.AdjustLotAsync(id, request, cancellationToken));

    /// <summary>Tezgâha ya da bölüme çıkış; en eski lottan başlayarak düşülür.</summary>
    [HttpPost("issues")]
    [HasPermission(DokumaPermissions.Stock.Manage)]
    public async Task<ActionResult<ApiResponse<object?>>> Issue(DokumaMaterialIssueRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.IssueAsync(request, cancellationToken));

    /// <summary>Malzeme sayımı: sayılan toplam ile stok arasındaki fark lotlara yansıtılır.</summary>
    [HttpPost("counts")]
    [HasPermission(DokumaPermissions.Stock.Manage)]
    public async Task<ActionResult<ApiResponse<object?>>> Count(DokumaMaterialCountRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.CountAsync(request, cancellationToken));
}
