using ERP.Core.Application.Common;
using ERP.Core.Infrastructure.Web;
using ERP.Core.Infrastructure.Web.Authorization;
using ERP.Konfeksiyon.Application.Features.Subcontracting;
using ERP.Konfeksiyon.Domain.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Konfeksiyon.Infrastructure.Api.Controllers;

/// <summary>
/// Fason fiyatları: fasoncu + operasyon (+ ürün) için adet başı fiyat, başlangıç tarihinden itibaren geçerli. Ürüne
/// özel fiyat genel fiyatın önündedir.
/// </summary>
[Route("api/v1/subcontract-prices")]
public sealed class SubcontractPricesController(ISubcontractPriceService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(KonfeksiyonPermissions.Subcontract.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<SubcontractPriceDto>>>> GetList(
        [FromQuery] SubcontractPriceQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetListAsync(query, cancellationToken), HttpContext));

    /// <summary>Verilen gün için geçerli fiyat (gönderim fişi formu öneri olarak gösterir).</summary>
    [HttpGet("resolve")]
    [HasPermission(KonfeksiyonPermissions.Subcontract.View)]
    public async Task<ActionResult<ApiResponse<ResolvedSubcontractPriceDto>>> Resolve(
        [FromQuery] int supplierId, [FromQuery] int operationId, [FromQuery] int? productId, [FromQuery] DateOnly date,
        CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.ResolveAsync(supplierId, operationId, productId, date, cancellationToken), HttpContext));

    [HttpGet("{id:int}")]
    [HasPermission(KonfeksiyonPermissions.Subcontract.View)]
    public async Task<ActionResult<ApiResponse<SubcontractPriceDto>>> Get(int id, CancellationToken cancellationToken) =>
        FromResult(await service.GetAsync(id, cancellationToken));

    [HttpPost]
    [HasPermission(KonfeksiyonPermissions.Subcontract.Manage)]
    public async Task<ActionResult<ApiResponse<SubcontractPriceDto>>> Create(
        SubcontractPriceRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(Get), new { id = result.Value.Id }, ApiResponse.Success(result.Value, HttpContext))
            : FromResult(result);
    }

    [HttpPut("{id:int}")]
    [HasPermission(KonfeksiyonPermissions.Subcontract.Manage)]
    public async Task<ActionResult<ApiResponse<SubcontractPriceDto>>> Update(
        int id, SubcontractPriceRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.UpdateAsync(id, request, cancellationToken));

    [HttpDelete("{id:int}")]
    [HasPermission(KonfeksiyonPermissions.Subcontract.Manage)]
    public async Task<ActionResult<ApiResponse<object?>>> Delete(int id, CancellationToken cancellationToken) =>
        FromResult(await service.DeleteAsync(id, cancellationToken));
}

/// <summary>
/// Fason gönderim fişleri. Gönderilen demetler dönüş işlenene kadar fasoncuda sayılır ve sahada (tablet / elle
/// bildirim) işlenemez.
/// </summary>
[Route("api/v1/subcontract-sends")]
public sealed class SubcontractSendsController(ISubcontractService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(KonfeksiyonPermissions.Subcontract.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<SubcontractSendListItemDto>>>> GetList(
        [FromQuery] SubcontractSendListQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetSendsAsync(query, cancellationToken), HttpContext));

    /// <summary>Etkin rotalarda fason işaretli operasyonlar ve varsayılan fasoncuları.</summary>
    [HttpGet("operation-options")]
    [HasPermission(KonfeksiyonPermissions.Subcontract.View)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SubcontractOperationOptionDto>>>> GetOperationOptions(
        CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetOperationOptionsAsync(cancellationToken), HttpContext));

    /// <summary>Seçilen operasyon sıradaki işi olan, fasona gönderilebilecek açık demetler.</summary>
    [HttpGet("candidates")]
    [HasPermission(KonfeksiyonPermissions.Subcontract.Manage)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SubcontractCandidateDto>>>> GetCandidates(
        [FromQuery] SubcontractCandidateQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetCandidatesAsync(query, cancellationToken), HttpContext));

    [HttpGet("{id:int}")]
    [HasPermission(KonfeksiyonPermissions.Subcontract.View)]
    public async Task<ActionResult<ApiResponse<SubcontractSendDto>>> Get(int id, CancellationToken cancellationToken) =>
        FromResult(await service.GetSendAsync(id, cancellationToken));

    /// <summary>Basılı gönderim irsaliyesi: fiş, gönderen (fabrika) ve alıcı (fasoncu).</summary>
    [HttpGet("{id:int}/document")]
    [HasPermission(KonfeksiyonPermissions.Subcontract.View)]
    public async Task<ActionResult<ApiResponse<SubcontractSendDocumentDto>>> GetDocument(
        int id, CancellationToken cancellationToken) =>
        FromResult(await service.GetSendDocumentAsync(id, cancellationToken));

    /// <summary>Gönderim fişi oluşturur; demetler fasoncuda sayılır, fiyat gönderim günündeki fason fiyatıdır.</summary>
    [HttpPost]
    [HasPermission(KonfeksiyonPermissions.Subcontract.Manage)]
    public async Task<ActionResult<ApiResponse<SubcontractSendDto>>> Create(
        SubcontractSendRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateSendAsync(request, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(Get), new { id = result.Value.Id }, ApiResponse.Success(result.Value, HttpContext))
            : FromResult(result);
    }

    /// <summary>Hiç dönüşü işlenmemiş fişi iptal eder; demetler sahaya döner.</summary>
    [HttpPost("{id:int}/cancel")]
    [HasPermission(KonfeksiyonPermissions.Subcontract.Manage)]
    public async Task<ActionResult<ApiResponse<SubcontractSendDto>>> Cancel(
        int id, SubcontractVersionRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.CancelSendAsync(id, request.RowVersion, cancellationToken));
}

/// <summary>Fason dönüş fişleri ve hakediş (sağlam × fiyat; ödendi işareti).</summary>
[Route("api/v1/subcontract-returns")]
public sealed class SubcontractReturnsController(ISubcontractService service) : ApiControllerBase
{
    /// <summary>Dönüş fişleri / hakedişler: fasoncu, dönem ve ödeme durumuna göre süzülür.</summary>
    [HttpGet]
    [HasPermission(KonfeksiyonPermissions.Subcontract.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<SubcontractReturnListItemDto>>>> GetList(
        [FromQuery] SubcontractReturnListQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetReturnsAsync(query, cancellationToken), HttpContext));

    /// <summary>Hakediş özeti: fasoncu başına toplam, ödenen ve ödenmeyen tutarlar.</summary>
    [HttpGet("statement")]
    [HasPermission(KonfeksiyonPermissions.Subcontract.View)]
    public async Task<ActionResult<ApiResponse<SubcontractStatementDto>>> GetStatement(
        [FromQuery] SubcontractStatementQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetStatementAsync(query, cancellationToken), HttpContext));

    [HttpGet("{id:int}")]
    [HasPermission(KonfeksiyonPermissions.Subcontract.View)]
    public async Task<ActionResult<ApiResponse<SubcontractReturnDto>>> Get(int id, CancellationToken cancellationToken) =>
        FromResult(await service.GetReturnAsync(id, cancellationToken));

    /// <summary>
    /// Dönüş fişi işler: sağlam adet demet operasyonuna üretilmiş, hatalı / eksik adet hatalı olarak yazılır. Satırın
    /// tamamı döndüyse demet sahaya döner.
    /// </summary>
    [HttpPost]
    [HasPermission(KonfeksiyonPermissions.Subcontract.Manage)]
    public async Task<ActionResult<ApiResponse<SubcontractReturnDto>>> Create(
        SubcontractReturnRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateReturnAsync(request, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(Get), new { id = result.Value.Id }, ApiResponse.Success(result.Value, HttpContext))
            : FromResult(result);
    }

    /// <summary>Ödenmemiş hakedişin satır fiyatlarını düzeltir.</summary>
    [HttpPut("{id:int}/prices")]
    [HasPermission(KonfeksiyonPermissions.Subcontract.Manage)]
    public async Task<ActionResult<ApiResponse<SubcontractReturnDto>>> UpdatePrices(
        int id, SubcontractReturnPricesRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.UpdateReturnPricesAsync(id, request, cancellationToken));

    /// <summary>Hakedişi ödendi olarak işaretler ya da işareti kaldırır.</summary>
    [HttpPost("{id:int}/payment")]
    [HasPermission(KonfeksiyonPermissions.Subcontract.Manage)]
    public async Task<ActionResult<ApiResponse<SubcontractReturnDto>>> SetPayment(
        int id, SubcontractPaymentRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.SetPaymentAsync(id, request, cancellationToken));
}
