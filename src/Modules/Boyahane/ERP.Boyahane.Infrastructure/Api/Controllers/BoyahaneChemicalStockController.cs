using ERP.Boyahane.Application.Features.Stock;
using ERP.Boyahane.Domain.Authorization;
using ERP.Core.Application.Common;
using ERP.Core.Infrastructure.Web;
using ERP.Core.Infrastructure.Web.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Boyahane.Infrastructure.Api.Controllers;

/// <summary>Boya ve kimyasal deposu: lot girişi, stok, hareketler, sayım düzeltmesi. Tüketim boyama adımından düşer.</summary>
[Route("api/v1/boyahane-chemical-stock")]
public sealed class BoyahaneChemicalStockController(IBoyahaneChemicalStockService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(BoyahanePermissions.ChemicalStock.View)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<BoyahaneChemicalStockRowDto>>>> GetStock(CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetStockAsync(cancellationToken), HttpContext));

    [HttpGet("movements")]
    [HasPermission(BoyahanePermissions.ChemicalStock.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<BoyahaneChemicalMovementDto>>>> GetMovements([FromQuery] BoyahaneChemicalStockQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetMovementsAsync(query, cancellationToken), HttpContext));

    [HttpGet("lots")]
    [HasPermission(BoyahanePermissions.ChemicalStock.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<BoyahaneChemicalLotDto>>>> GetLots([FromQuery] BoyahaneChemicalStockQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetLotsAsync(query, cancellationToken), HttpContext));

    [HttpPost("lots")]
    [HasPermission(BoyahanePermissions.ChemicalStock.Manage)]
    public async Task<ActionResult<ApiResponse<BoyahaneChemicalLotDto>>> Create(BoyahaneChemicalLotRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.CreateLotAsync(request, cancellationToken));

    [HttpPut("lots/{id:int}")]
    [HasPermission(BoyahanePermissions.ChemicalStock.Manage)]
    public async Task<ActionResult<ApiResponse<BoyahaneChemicalLotDto>>> Update(int id, BoyahaneChemicalLotRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.UpdateLotAsync(id, request, cancellationToken));

    [HttpDelete("lots/{id:int}")]
    [HasPermission(BoyahanePermissions.ChemicalStock.Manage)]
    public async Task<ActionResult<ApiResponse<object?>>> Delete(int id, CancellationToken cancellationToken) =>
        FromResult(await service.DeleteLotAsync(id, cancellationToken));

    /// <summary>Sayım düzeltmesi: lotun kalanı sayılan miktara çekilir, fark gerekçesiyle hareket olur.</summary>
    [HttpPost("lots/{id:int}/adjust")]
    [HasPermission(BoyahanePermissions.ChemicalStock.Manage)]
    public async Task<ActionResult<ApiResponse<BoyahaneChemicalLotDto>>> Adjust(int id, AdjustChemicalLotRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.AdjustLotAsync(id, request, cancellationToken));
}
