using ERP.Core.Infrastructure.Web.Authorization;
using ERP.Core.Infrastructure.Web;
using ERP.Core.Application.Common;
using ERP.Core.Application.Features.Users;
using ERP.Core.Domain.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Api.Controllers;

[Route("api/v1/users")]
public sealed class UsersController(IUserService userService) : ApiControllerBase
{
    /// <summary>Kullanıcı listesi (sayfalı; arama, durum ve rol filtresi).</summary>
    [HttpGet]
    [HasPermission(Permissions.User.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<UserDto>>>> GetUsers(
        [FromQuery] UserListQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await userService.GetUsersAsync(query, cancellationToken), HttpContext));

    [HttpGet("{id:int}")]
    [HasPermission(Permissions.User.View)]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetUser(int id, CancellationToken cancellationToken) =>
        FromResult(await userService.GetUserAsync(id, cancellationToken));

    /// <summary>Yeni kullanıcı oluşturur ve rollerini atar.</summary>
    [HttpPost]
    [HasPermission(Permissions.User.Manage)]
    public async Task<ActionResult<ApiResponse<UserDto>>> Create(
        CreateUserRequest request, CancellationToken cancellationToken)
    {
        var result = await userService.CreateAsync(request, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetUser), new { id = result.Value.Id }, ApiResponse.Success(result.Value, HttpContext))
            : FromResult(result);
    }

    /// <summary>Ad, e-posta, aktiflik ve rolleri günceller. Pasif yapılan kullanıcının oturumları kapanır.</summary>
    [HttpPut("{id:int}")]
    [HasPermission(Permissions.User.Manage)]
    public async Task<ActionResult<ApiResponse<UserDto>>> Update(
        int id, UpdateUserRequest request, CancellationToken cancellationToken) =>
        FromResult(await userService.UpdateAsync(id, request, cancellationToken));

    /// <summary>Kullanıcının parolasını sıfırlar, kilidini kaldırır ve tüm oturumlarını kapatır.</summary>
    [HttpPost("{id:int}/reset-password")]
    [HasPermission(Permissions.User.ResetPassword)]
    public async Task<ActionResult<ApiResponse<object?>>> ResetPassword(
        int id, ResetPasswordRequest request, CancellationToken cancellationToken) =>
        FromResult(await userService.ResetPasswordAsync(id, request, cancellationToken));

    /// <summary>Kullanıcıyı siler (soft delete) ve oturumlarını kapatır.</summary>
    [HttpDelete("{id:int}")]
    [HasPermission(Permissions.User.Manage)]
    public async Task<ActionResult<ApiResponse<object?>>> Delete(int id, CancellationToken cancellationToken) =>
        FromResult(await userService.DeleteAsync(id, cancellationToken));
}
