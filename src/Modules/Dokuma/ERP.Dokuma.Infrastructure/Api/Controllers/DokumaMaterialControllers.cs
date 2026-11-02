using ERP.Core.Application.Features.MasterData;
using ERP.Core.Infrastructure.Web;
using ERP.Core.Infrastructure.Web.Authorization;
using ERP.Dokuma.Application.Features.Materials;
using ERP.Dokuma.Domain.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Dokuma.Infrastructure.Api.Controllers;

[Route("api/v1/dokuma-suppliers")]
[ResourcePermissions(DokumaPermissions.Purchase.View, DokumaPermissions.Purchase.Manage)]
public sealed class DokumaSuppliersController(IMasterDataService<DokumaSupplierDto, DokumaSupplierRequest> service)
    : MasterDataController<DokumaSupplierDto, DokumaSupplierRequest>(service);

[Route("api/v1/dokuma-yarns")]
[ResourcePermissions(DokumaPermissions.Material.View, DokumaPermissions.Material.Manage)]
public sealed class DokumaYarnsController(IMasterDataService<DokumaYarnDto, DokumaYarnRequest> service)
    : MasterDataController<DokumaYarnDto, DokumaYarnRequest>(service);

[Route("api/v1/dokuma-looms")]
[ResourcePermissions(DokumaPermissions.Loom.View, DokumaPermissions.Loom.Manage)]
public sealed class DokumaLoomsController(IMasterDataService<DokumaLoomDto, DokumaLoomRequest> service)
    : MasterDataController<DokumaLoomDto, DokumaLoomRequest>(service);

[Route("api/v1/dokuma-articles")]
[ResourcePermissions(DokumaPermissions.Material.View, DokumaPermissions.Material.Manage)]
public sealed class DokumaArticlesController(IMasterDataService<DokumaArticleDto, DokumaArticleRequest> service)
    : MasterDataController<DokumaArticleDto, DokumaArticleRequest>(service);

/// <summary>Artikelin çözgü ve atkı iplikleri; liste bütün olarak kaydedilir, gönderilmeyenler silinir.</summary>
[Route("api/v1/dokuma-articles/{articleId:int}/yarns")]
public sealed class DokumaArticleYarnsController(IDokumaArticleYarnService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(DokumaPermissions.Material.View)]
    public async Task<ActionResult<ApiResponse<DokumaArticleYarnsDto>>> Get(int articleId, CancellationToken cancellationToken) =>
        FromResult(await service.GetAsync(articleId, cancellationToken));

    [HttpPut]
    [HasPermission(DokumaPermissions.Material.Manage)]
    public async Task<ActionResult<ApiResponse<DokumaArticleYarnsDto>>> Save(
        int articleId, SaveDokumaArticleYarnsRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.SaveAsync(articleId, request, cancellationToken));
}
