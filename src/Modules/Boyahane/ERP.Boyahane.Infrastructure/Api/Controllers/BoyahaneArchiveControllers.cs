using ERP.Boyahane.Application.Features.Archive;
using ERP.Boyahane.Domain.Authorization;
using ERP.Core.Application.Common;
using ERP.Core.Application.Features.MasterData;
using ERP.Core.Infrastructure.Web;
using ERP.Core.Infrastructure.Web.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Boyahane.Infrastructure.Api.Controllers;

/// <summary>Renk kartları (renk arşivi). Seçim listesi sipariş formu için oturum açmış her kullanıcıya açıktır.</summary>
[Route("api/v1/boyahane-color-cards")]
[ResourcePermissions(BoyahanePermissions.Lab.View, BoyahanePermissions.Lab.Manage)]
public sealed class BoyahaneColorCardsController(IMasterDataService<BoyahaneColorCardDto, BoyahaneColorCardRequest> service)
    : MasterDataController<BoyahaneColorCardDto, BoyahaneColorCardRequest>(service);

[Route("api/v1/boyahane-color-cards/{id:int}")]
public sealed class BoyahaneColorCardDetailController(IBoyahaneColorCardDetailService service) : ApiControllerBase
{
    /// <summary>Kartın numune görüntüsü (base64). Görüntü yoksa NOT_FOUND döner.</summary>
    [HttpGet("image")]
    [HasPermission(BoyahanePermissions.Lab.View)]
    public async Task<ActionResult<ApiResponse<BoyahaneArchiveImageDto>>> GetImage(int id, CancellationToken cancellationToken) =>
        FromResult(await service.GetImageAsync(id, cancellationToken));

    /// <summary>Kartın görüntüsünü yükler (JPEG / PNG, en fazla 2 MB); varsa eskisinin yerine geçer.</summary>
    [HttpPut("image")]
    [HasPermission(BoyahanePermissions.Lab.Manage)]
    public async Task<ActionResult<ApiResponse<object?>>> SetImage(int id, BoyahaneArchiveImageRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.SetImageAsync(id, request, cancellationToken));

    [HttpDelete("image")]
    [HasPermission(BoyahanePermissions.Lab.Manage)]
    public async Task<ActionResult<ApiResponse<object?>>> RemoveImage(int id, CancellationToken cancellationToken) =>
        FromResult(await service.RemoveImageAsync(id, cancellationToken));

    /// <summary>Kartın onaylı lab reçeteleri: müşteri, kumaş / iplik, reçete no ve tarih.</summary>
    [HttpGet("recipes")]
    [HasPermission(BoyahanePermissions.Lab.View)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<BoyahaneColorCardRecipeDto>>>> GetRecipes(int id, CancellationToken cancellationToken) =>
        FromResult(await service.GetRecipesAsync(id, cancellationToken));
}

/// <summary>Desen kartları (desen arşivi). Seçim listesi sipariş formu için oturum açmış her kullanıcıya açıktır.</summary>
[Route("api/v1/boyahane-patterns")]
[ResourcePermissions(BoyahanePermissions.Lab.View, BoyahanePermissions.Lab.Manage)]
public sealed class BoyahanePatternsController(IMasterDataService<BoyahanePatternDto, BoyahanePatternRequest> service)
    : MasterDataController<BoyahanePatternDto, BoyahanePatternRequest>(service);

/// <summary>Desenin görüntüsü, renk yolları ve baskı reçeteleri.</summary>
[Route("api/v1/boyahane-patterns/{id:int}")]
public sealed class BoyahanePatternDetailController(IBoyahanePatternDetailService service) : ApiControllerBase
{
    [HttpGet("image")]
    [HasPermission(BoyahanePermissions.Lab.View)]
    public async Task<ActionResult<ApiResponse<BoyahaneArchiveImageDto>>> GetImage(int id, CancellationToken cancellationToken) =>
        FromResult(await service.GetImageAsync(id, cancellationToken));

    /// <summary>Desenin görüntüsünü yükler (JPEG / PNG, en fazla 2 MB); varsa eskisinin yerine geçer.</summary>
    [HttpPut("image")]
    [HasPermission(BoyahanePermissions.Lab.Manage)]
    public async Task<ActionResult<ApiResponse<object?>>> SetImage(int id, BoyahaneArchiveImageRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.SetImageAsync(id, request, cancellationToken));

    [HttpDelete("image")]
    [HasPermission(BoyahanePermissions.Lab.Manage)]
    public async Task<ActionResult<ApiResponse<object?>>> RemoveImage(int id, CancellationToken cancellationToken) =>
        FromResult(await service.RemoveImageAsync(id, cancellationToken));

    /// <summary>Renk yolları, renkleri (şablon sırası) ve her rengin baskı reçetesi.</summary>
    [HttpGet("colorways")]
    [HasPermission(BoyahanePermissions.Lab.View)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<BoyahaneColorwayDto>>>> GetColorways(int id, CancellationToken cancellationToken) =>
        FromResult(await service.GetColorwaysAsync(id, cancellationToken));

    /// <summary>Sipariş satırında renk yolu seçimi; başka ekranların formları da kullandığı için oturum açmış her kullanıcıya açıktır.</summary>
    [HttpGet("colorways/lookup")]
    [AnyAuthenticatedUser]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<LookupDto>>>> LookupColorways(int id, [FromQuery] LookupQuery query, CancellationToken cancellationToken) =>
        FromResult(await service.LookupColorwaysAsync(id, query, cancellationToken));

    [HttpPost("colorways")]
    [HasPermission(BoyahanePermissions.Lab.Manage)]
    public async Task<ActionResult<ApiResponse<BoyahaneColorwayDto>>> CreateColorway(int id, BoyahaneColorwayRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.CreateColorwayAsync(id, request, cancellationToken));

    [HttpPut("colorways/{colorwayId:int}")]
    [HasPermission(BoyahanePermissions.Lab.Manage)]
    public async Task<ActionResult<ApiResponse<BoyahaneColorwayDto>>> UpdateColorway(int id, int colorwayId, BoyahaneColorwayRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.UpdateColorwayAsync(id, colorwayId, request, cancellationToken));

    [HttpDelete("colorways/{colorwayId:int}")]
    [HasPermission(BoyahanePermissions.Lab.Manage)]
    public async Task<ActionResult<ApiResponse<object?>>> DeleteColorway(int id, int colorwayId, CancellationToken cancellationToken) =>
        FromResult(await service.DeleteColorwayAsync(id, colorwayId, cancellationToken));
}

/// <summary>Söküm reçeteleri. Seçim listesi renk kontrolü formu için oturum açmış her kullanıcıya açıktır.</summary>
[Route("api/v1/boyahane-stripping-recipes")]
public sealed class BoyahaneStrippingRecipesController(IBoyahaneStrippingRecipeService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(BoyahanePermissions.Lab.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<BoyahaneStrippingRecipeDto>>>> GetList([FromQuery] MasterDataQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetListAsync(query, cancellationToken), HttpContext));

    [HttpGet("lookup")]
    [AnyAuthenticatedUser]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<LookupDto>>>> Lookup([FromQuery] LookupQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.LookupAsync(query, cancellationToken), HttpContext));

    [HttpGet("{id:int}")]
    [HasPermission(BoyahanePermissions.Lab.View)]
    public async Task<ActionResult<ApiResponse<BoyahaneStrippingRecipeDto>>> Get(int id, CancellationToken cancellationToken) =>
        FromResult(await service.GetAsync(id, cancellationToken));

    [HttpPost]
    [HasPermission(BoyahanePermissions.Lab.Manage)]
    public async Task<ActionResult<ApiResponse<BoyahaneStrippingRecipeDto>>> Create(BoyahaneStrippingRecipeRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken);
        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, ApiResponse.Success(result.Value, HttpContext))
            : FromResult(result);
    }

    [HttpPut("{id:int}")]
    [HasPermission(BoyahanePermissions.Lab.Manage)]
    public async Task<ActionResult<ApiResponse<BoyahaneStrippingRecipeDto>>> Update(int id, BoyahaneStrippingRecipeRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.UpdateAsync(id, request, cancellationToken));

    [HttpDelete("{id:int}")]
    [HasPermission(BoyahanePermissions.Lab.Manage)]
    public async Task<ActionResult<ApiResponse<object?>>> Delete(int id, CancellationToken cancellationToken) =>
        FromResult(await service.DeleteAsync(id, cancellationToken));
}
