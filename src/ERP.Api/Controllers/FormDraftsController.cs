using ERP.Core.Infrastructure.Web.Authorization;
using ERP.Core.Infrastructure.Web;
using ERP.Core.Application.Features.Workspace;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Api.Controllers;

/// <summary>
/// Kullanıcıya özel form taslakları: kaydedilmemiş girişler sayfa yenilense ya da bağlantı kopsa da kaybolmaz.
/// Her kullanıcı yalnızca kendi taslaklarını görür.
/// </summary>
[Route("api/v1/workspace/drafts/{formKey}")]
public sealed class FormDraftsController(IFormDraftService service) : ApiControllerBase
{
    /// <summary>Taslak; yoksa veri boş döner.</summary>
    [HttpGet]
    [AnyAuthenticatedUser]
    public async Task<ActionResult<ApiResponse<FormDraftDto?>>> Get(string formKey, CancellationToken cancellationToken) =>
        FromResult(await service.GetAsync(formKey, cancellationToken));

    [HttpPut]
    [AnyAuthenticatedUser]
    public async Task<ActionResult<ApiResponse<FormDraftDto>>> Save(
        string formKey, SaveFormDraftRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.SaveAsync(formKey, request, cancellationToken));

    [HttpDelete]
    [AnyAuthenticatedUser]
    public async Task<ActionResult<ApiResponse<object?>>> Delete(string formKey, CancellationToken cancellationToken) =>
        FromResult(await service.DeleteAsync(formKey, cancellationToken));
}
