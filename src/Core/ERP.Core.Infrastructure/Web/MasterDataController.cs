using Microsoft.AspNetCore.Http;
using ERP.Core.Infrastructure.Web.Authorization;
using ERP.Core.Application.Common;
using ERP.Core.Application.Features.MasterData;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Core.Infrastructure.Web;

/// <summary>
/// Ana veri kaynaklarının ortak uçları. Yetkiler türeyen controller'daki
/// <see cref="ResourcePermissionsAttribute"/> ile belirlenir.
/// </summary>
public abstract class MasterDataController<TDto, TRequest>(IMasterDataService<TDto, TRequest> service)
    : ApiControllerBase
{
    /// <summary>Sayfalı liste; kod/ad araması ve aktiflik filtresi.</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<TDto>>>> GetList(
        [FromQuery] MasterDataQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetListAsync(query, cancellationToken), HttpContext));

    /// <summary>
    /// Seçim kutuları için aktif kayıtların kod ve adı (en fazla 100; arama ile daraltılır).
    /// Başka ekranların formları da kullandığı için oturum açmış her kullanıcıya açıktır.
    /// </summary>
    [HttpGet("lookup")]
    [AnyAuthenticatedUser]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<LookupDto>>>> Lookup(
        [FromQuery] LookupQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.LookupAsync(query, cancellationToken), HttpContext));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<TDto>>> Get(int id, CancellationToken cancellationToken) =>
        FromResult(await service.GetAsync(id, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<ApiResponse<TDto>>> Create(TRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken);
        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, ApiResponse.Success(result.Value, HttpContext))
            : FromResult(result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<TDto>>> Update(
        int id, TRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.UpdateAsync(id, request, cancellationToken));

    /// <summary>Kaydı siler (soft delete). Başka kayıtlarca kullanılıyorsa RECORD_IN_USE döner.</summary>
    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<object?>>> Delete(int id, CancellationToken cancellationToken) =>
        FromResult(await service.DeleteAsync(id, cancellationToken));
}
