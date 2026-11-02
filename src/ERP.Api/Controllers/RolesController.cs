using ERP.Core.Infrastructure.Web.Authorization;
using ERP.Core.Infrastructure.Web;
using ERP.Core.Application.Features.Roles;
using ERP.Core.Domain.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Api.Controllers;

[Route("api/v1")]
public sealed class RolesController(IRoleService roleService) : ApiControllerBase
{
    /// <summary>Roller ve sahip oldukları yetkiler. Kullanıcı ekranı da rol seçimi için kullanır.</summary>
    [HttpGet("roles")]
    [HasPermission(Permissions.Role.View, Permissions.User.View)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RoleDto>>>> GetRoles(CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await roleService.GetRolesAsync(cancellationToken), HttpContext));

    /// <summary>Rolün yetkilerini verilen listeyle değiştirir; değişiklik hemen geçerli olur.</summary>
    [HttpPut("roles/{id:int}/permissions")]
    [HasPermission(Permissions.Role.Manage)]
    public async Task<ActionResult<ApiResponse<RoleDto>>> UpdatePermissions(
        int id, UpdateRolePermissionsRequest request, CancellationToken cancellationToken) =>
        FromResult(await roleService.UpdatePermissionsAsync(id, request, cancellationToken));

    /// <summary>Sistemde tanımlı tüm yetkiler.</summary>
    [HttpGet("permissions")]
    [HasPermission(Permissions.Role.View)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PermissionDto>>>> GetPermissions(
        CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await roleService.GetPermissionsAsync(cancellationToken), HttpContext));
}
