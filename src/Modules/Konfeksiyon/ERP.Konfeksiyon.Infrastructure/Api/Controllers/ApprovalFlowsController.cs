using ERP.Konfeksiyon.Domain.Authorization;
using ERP.Core.Infrastructure.Web.Authorization;
using ERP.Core.Infrastructure.Web;
using ERP.Konfeksiyon.Application.Features.Sales;
using ERP.Core.Domain.Authorization;
using ERP.Konfeksiyon.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Konfeksiyon.Infrastructure.Api.Controllers;

/// <summary>
/// Satış siparişi onay akışı: sıralı adımlar, her adım bir role ya da seçili kişilere atanır; isteğe bağlı tutar
/// eşiği (TRY). Yönetim yetkisiyle düzenlenir.
/// </summary>
[Route("api/v1/approval-flows/sales-order")]
public sealed class ApprovalFlowsController(IApprovalFlowService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.Role.View)]
    public async Task<ActionResult<ApiResponse<ApprovalFlowDto>>> Get(CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetAsync(ApprovalDocumentType.SalesOrder, cancellationToken), HttpContext));

    /// <summary>Akış düzenleme ekranında seçilebilecek roller ve aktif kullanıcılar.</summary>
    [HttpGet("options")]
    [HasPermission(Permissions.Role.View)]
    public async Task<ActionResult<ApiResponse<ApprovalFlowOptionsDto>>> GetOptions(CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetOptionsAsync(cancellationToken), HttpContext));

    /// <summary>Akışı verilen adımlarla değiştirir; süren onaylar gönderildikleri andaki akışla devam eder.</summary>
    [HttpPut]
    [HasPermission(KonfeksiyonPermissions.ApprovalFlow.Manage)]
    public async Task<ActionResult<ApiResponse<ApprovalFlowDto>>> Save(
        SaveApprovalFlowRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.SaveAsync(ApprovalDocumentType.SalesOrder, request, cancellationToken));
}
