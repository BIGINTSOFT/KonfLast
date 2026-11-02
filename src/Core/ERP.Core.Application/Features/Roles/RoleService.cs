using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Core.Domain.Entities.Identity;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using RoleCodes = ERP.Core.Domain.Authorization.Roles;

namespace ERP.Core.Application.Features.Roles;

/// <param name="ModuleKey">Rolün ait olduğu proje; null ise bütün projelerde geçerlidir.</param>
public sealed record RoleDto(int Id, string Code, string Name, bool IsSystem, string? ModuleKey, IReadOnlyList<string> Permissions);

/// <param name="ModuleKey">Yetkinin ait olduğu proje; null ise çekirdek yetkidir.</param>
public sealed record PermissionDto(int Id, string Code, string Module, string Description, string? ModuleKey);

public sealed record UpdateRolePermissionsRequest(IReadOnlyList<string> Permissions);

public sealed class UpdateRolePermissionsRequestValidator : AbstractValidator<UpdateRolePermissionsRequest>
{
    public UpdateRolePermissionsRequestValidator()
    {
        RuleFor(x => x.Permissions).NotNull().WithMessage("Yetki listesi zorunludur.");
    }
}

public interface IRoleService
{
    Task<IReadOnlyList<RoleDto>> GetRolesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<PermissionDto>> GetPermissionsAsync(CancellationToken cancellationToken);

    /// <summary>Rolün yetkilerini verilen listeyle değiştirir.</summary>
    Task<Result<RoleDto>> UpdatePermissionsAsync(
        int roleId, UpdateRolePermissionsRequest request, CancellationToken cancellationToken);
}

public sealed class RoleService(IAppDbContext db, IPermissionService permissionService, IActivePermissions active)
    : IRoleService
{
    public async Task<IReadOnlyList<RoleDto>> GetRolesAsync(CancellationToken cancellationToken)
    {
        var roles = await db.Roles
            .AsNoTracking()
            .OrderBy(r => r.Id)
            .Select(r => new
            {
                r.Id,
                r.Code,
                r.Name,
                r.IsSystem,
                r.ModuleKey,
                Permissions = r.RolePermissions.Select(rp => rp.Permission.Code).ToList(),
            })
            .ToListAsync(cancellationToken);

        return roles
            .Select(r => new RoleDto(r.Id, r.Code, r.Name, r.IsSystem, r.ModuleKey, r.Permissions.Where(active.Contains).Order().ToList()))
            .ToList();
    }

    public async Task<IReadOnlyList<PermissionDto>> GetPermissionsAsync(CancellationToken cancellationToken) =>
        (await db.Permissions
            .AsNoTracking()
            .OrderBy(p => p.Module).ThenBy(p => p.Code)
            .Select(p => new { p.Id, p.Code, p.Module, p.Description })
            .ToListAsync(cancellationToken))
        .Where(p => active.Contains(p.Code))
        .Select(p => new PermissionDto(p.Id, p.Code, p.Module, p.Description, active.ModuleOf(p.Code)))
        .ToList();

    public async Task<Result<RoleDto>> UpdatePermissionsAsync(
        int roleId, UpdateRolePermissionsRequest request, CancellationToken cancellationToken)
    {
        var role = await db.Roles
            .Include(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .SingleOrDefaultAsync(r => r.Id == roleId, cancellationToken);
        if (role is null)
        {
            return new Error(ErrorCodes.RoleNotFound, "Rol bulunamadı.", ErrorType.NotFound);
        }

        if (role.Code == RoleCodes.Admin)
        {
            // Yönetici rolü her zaman tüm yetkilere sahiptir; aksi halde sistem kilitlenebilir.
            return new Error(ErrorCodes.RoleNotEditable, "ADMIN rolünün yetkileri değiştirilemez.");
        }

        var codes = request.Permissions.Distinct().ToList();
        var permissions = await db.Permissions
            .Where(p => codes.Contains(p.Code))
            .Select(p => new { p.Id, p.Code })
            .ToListAsync(cancellationToken);

        var unknown = codes.Except(permissions.Select(p => p.Code).Where(active.Contains)).ToList();
        if (unknown.Count > 0)
        {
            return new Error(
                ErrorCodes.ValidationError,
                "Gönderilen veriler geçersiz.",
                ErrorType.Validation,
                new Dictionary<string, string[]>
                {
                    ["permissions"] = [$"Bilinmeyen yetki: {string.Join(", ", unknown)}"],
                });
        }

        var targetIds = permissions.Select(p => p.Id).ToHashSet();
        var currentIds = role.RolePermissions.Select(rp => rp.PermissionId).ToHashSet();

        // Devrede olmayan modüllerin atamaları ekranda görünmez; korunur (modül yeniden açılınca geri gelir).
        db.RolePermissions.RemoveRange(role.RolePermissions
            .Where(rp => !targetIds.Contains(rp.PermissionId) && active.Contains(rp.Permission.Code)));
        db.RolePermissions.AddRange(targetIds
            .Where(id => !currentIds.Contains(id))
            .Select(id => new RolePermission { RoleId = role.Id, PermissionId = id }));

        await db.SaveChangesAsync(cancellationToken);
        permissionService.InvalidateCache();

        return new RoleDto(
            role.Id, role.Code, role.Name, role.IsSystem, role.ModuleKey, permissions.Select(p => p.Code).Order().ToList());
    }
}
