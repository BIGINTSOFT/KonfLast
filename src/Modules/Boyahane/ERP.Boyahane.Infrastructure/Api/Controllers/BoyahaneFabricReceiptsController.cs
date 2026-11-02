using ERP.Boyahane.Application.Features.Receipts;
using ERP.Boyahane.Domain.Authorization;
using ERP.Core.Application.Common;
using ERP.Core.Infrastructure.Web;
using ERP.Core.Infrastructure.Web.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Boyahane.Infrastructure.Api.Controllers;

/// <summary>Kumaş kabul belgeleri. Toplar barkodlu numara alır ve müşteri kartına göre kontrol bekler ya da doğrudan uygun girer.</summary>
[Route("api/v1/boyahane-fabric-receipts")]
public sealed class BoyahaneFabricReceiptsController(IBoyahaneFabricReceiptService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(BoyahanePermissions.Receipt.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<BoyahaneFabricReceiptListItemDto>>>> GetList(
        [FromQuery] BoyahaneFabricReceiptQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetListAsync(query, cancellationToken), HttpContext));

    [HttpGet("{id:int}")]
    [HasPermission(BoyahanePermissions.Receipt.View)]
    public async Task<ActionResult<ApiResponse<BoyahaneFabricReceiptDto>>> Get(int id, CancellationToken cancellationToken) =>
        FromResult(await service.GetAsync(id, cancellationToken));

    /// <summary>Metre ya da kilodan girilmeyeni kumaş kartının ham en ve gramajından hesaplanır.</summary>
    [HttpPost]
    [HasPermission(BoyahanePermissions.Receipt.Manage)]
    public async Task<ActionResult<ApiResponse<BoyahaneFabricReceiptDto>>> Create(
        CreateBoyahaneFabricReceiptRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken);
        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, ApiResponse.Success(result.Value, HttpContext))
            : FromResult(result);
    }

    /// <summary>Belgeyi düzeltir; var olan toplar numaralarını korur. Kontrol edilmiş ya da siparişe bağlanmış top değiştirilemez.</summary>
    [HttpPut("{id:int}")]
    [HasPermission(BoyahanePermissions.Receipt.Manage)]
    public async Task<ActionResult<ApiResponse<BoyahaneFabricReceiptDto>>> Update(
        int id, CreateBoyahaneFabricReceiptRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.UpdateAsync(id, request, cancellationToken));

    /// <summary>Kontrol edilmiş ya da siparişe bağlanmış top varsa RECORD_IN_USE döner.</summary>
    [HttpDelete("{id:int}")]
    [HasPermission(BoyahanePermissions.Receipt.Manage)]
    public async Task<ActionResult<ApiResponse<object?>>> Delete(int id, CancellationToken cancellationToken) =>
        FromResult(await service.DeleteAsync(id, cancellationToken));
}
