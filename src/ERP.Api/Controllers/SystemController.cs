using ERP.Core.Infrastructure.Modules;
using ERP.Core.Infrastructure.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Api.Controllers;

public sealed record ModuleDto(string Key, string DisplayName);

public sealed record LicenseStatusDto(bool Valid, string? Customer, DateOnly? ExpiresAt, string? Error);

/// <param name="Modules">Bu kurulumda devrede olan modüller (kurulu, lisanslı ve açık).</param>
/// <param name="License">Lisansın durumu; geçersizse hiçbir modül devrede değildir.</param>
public sealed record SystemModulesDto(IReadOnlyList<ModuleDto> Modules, LicenseStatusDto License);

[Route("api/v1/system")]
public sealed class SystemController(ErpModuleCatalog catalog) : ApiControllerBase
{
    /// <summary>
    /// Devredeki modüller ve lisans durumu. Web ve tablet hangi proje arayüzlerini sunacağını buradan öğrenir;
    /// oturum açılmadan (giriş ekranı, tablet kurulumu) da okunabilir.
    /// </summary>
    [HttpGet("modules")]
    [AllowAnonymous]
    public ActionResult<ApiResponse<SystemModulesDto>> GetModules()
    {
        var license = catalog.License;
        return Ok(ApiResponse.Success(
            new SystemModulesDto(
                [.. catalog.Modules.Select(m => new ModuleDto(m.Key, m.DisplayName))],
                new LicenseStatusDto(
                    license?.IsValid ?? true,
                    license?.License?.Customer,
                    license?.License?.ExpiresAt,
                    license?.Error)),
            HttpContext));
    }
}
