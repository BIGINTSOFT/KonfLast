using ERP.Core.Application.Common;
using ERP.Core.Infrastructure.Web;
using ERP.Core.Infrastructure.Web.Authorization;
using ERP.Konfeksiyon.Application.Features.Quotations;
using ERP.Konfeksiyon.Domain.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Konfeksiyon.Infrastructure.Api.Controllers;

/// <summary>Ön maliyet parametreleri: dakika işçilik / enerji maliyeti, genel gider, varsayılan fire ve kâr marjı.</summary>
[Route("api/v1/cost-settings")]
public sealed class CostSettingsController(ICostSettingService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(KonfeksiyonPermissions.Quotation.View)]
    public async Task<ActionResult<ApiResponse<CostSettingDto>>> Get(CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetAsync(cancellationToken), HttpContext));

    [HttpPut]
    [HasPermission(KonfeksiyonPermissions.Quotation.Manage)]
    public async Task<ActionResult<ApiResponse<CostSettingDto>>> Update(
        CostSettingRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.UpdateAsync(request, cancellationToken));
}

/// <summary>
/// Teklifler: satır başına reçete, rota ve fason fiyatından parça maliyeti; kâr marjıyla birim fiyat. Kabul edilen
/// tekliften taslak müşteri siparişi açılır.
/// </summary>
[Route("api/v1/quotations")]
public sealed class QuotationsController(IQuotationService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(KonfeksiyonPermissions.Quotation.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<QuotationListItemDto>>>> GetList(
        [FromQuery] QuotationListQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetListAsync(query, cancellationToken), HttpContext));

    [HttpGet("{id:int}")]
    [HasPermission(KonfeksiyonPermissions.Quotation.View)]
    public async Task<ActionResult<ApiResponse<QuotationDto>>> Get(int id, CancellationToken cancellationToken) =>
        FromResult(await service.GetAsync(id, cancellationToken));

    /// <summary>Basılı teklif: teklif, satıcı ve müşteri bilgileri (müşterinin belge dilinde basılır).</summary>
    [HttpGet("{id:int}/document")]
    [HasPermission(KonfeksiyonPermissions.Quotation.View)]
    public async Task<ActionResult<ApiResponse<QuotationDocumentDto>>> GetDocument(int id, CancellationToken cancellationToken) =>
        FromResult(await service.GetDocumentAsync(id, cancellationToken));

    /// <summary>Bir satırın maliyetini kaydetmeden hesaplar (yeni satır ve "ya olursa" denemesi).</summary>
    [HttpPost("line-preview")]
    [HasPermission(KonfeksiyonPermissions.Quotation.Manage)]
    public async Task<ActionResult<ApiResponse<QuotationLineDto>>> PreviewLine(
        QuotationLinePreviewRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.PreviewLineAsync(request, cancellationToken));

    [HttpPost]
    [HasPermission(KonfeksiyonPermissions.Quotation.Manage)]
    public async Task<ActionResult<ApiResponse<QuotationDto>>> Create(QuotationRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(Get), new { id = result.Value.Id }, ApiResponse.Success(result.Value, HttpContext))
            : FromResult(result);
    }

    /// <summary>Taslak teklifi günceller; maliyetler yeniden hesaplanır (rowVersion zorunlu).</summary>
    [HttpPut("{id:int}")]
    [HasPermission(KonfeksiyonPermissions.Quotation.Manage)]
    public async Task<ActionResult<ApiResponse<QuotationDto>>> Update(
        int id, QuotationRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.UpdateAsync(id, request, cancellationToken));

    [HttpDelete("{id:int}")]
    [HasPermission(KonfeksiyonPermissions.Quotation.Manage)]
    public async Task<ActionResult<ApiResponse<object?>>> Delete(int id, CancellationToken cancellationToken) =>
        FromResult(await service.DeleteAsync(id, cancellationToken));

    [HttpPost("{id:int}/send")]
    [HasPermission(KonfeksiyonPermissions.Quotation.Manage)]
    public async Task<ActionResult<ApiResponse<QuotationDto>>> Send(
        int id, QuotationActionRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.SendAsync(id, request, cancellationToken));

    [HttpPost("{id:int}/reject")]
    [HasPermission(KonfeksiyonPermissions.Quotation.Manage)]
    public async Task<ActionResult<ApiResponse<QuotationDto>>> Reject(
        int id, QuotationActionRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.RejectAsync(id, request, cancellationToken));

    [HttpPost("{id:int}/expire")]
    [HasPermission(KonfeksiyonPermissions.Quotation.Manage)]
    public async Task<ActionResult<ApiResponse<QuotationDto>>> Expire(
        int id, QuotationActionRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.ExpireAsync(id, request, cancellationToken));

    [HttpPost("{id:int}/reopen")]
    [HasPermission(KonfeksiyonPermissions.Quotation.Manage)]
    public async Task<ActionResult<ApiResponse<QuotationDto>>> Reopen(
        int id, QuotationActionRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.ReopenAsync(id, request, cancellationToken));

    /// <summary>
    /// Kabul: renk / beden adetleri ve teklif fiyatlarıyla taslak müşteri siparişi açar. Sipariş oluşturma kuralları
    /// (fiyat görme yetkisi, fiyat listesinin altı) geçerlidir.
    /// </summary>
    [HttpPost("{id:int}/accept")]
    [HasPermission(KonfeksiyonPermissions.Quotation.Manage)]
    public async Task<ActionResult<ApiResponse<QuotationDto>>> Accept(
        int id, QuotationAcceptRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.AcceptAsync(id, request, cancellationToken));
}
