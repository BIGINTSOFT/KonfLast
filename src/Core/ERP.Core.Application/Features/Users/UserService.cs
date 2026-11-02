using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Core.Application.Features.Auth;
using ERP.Core.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using RoleCodes = ERP.Core.Domain.Authorization.Roles;

namespace ERP.Core.Application.Features.Users;

public interface IUserService
{
    Task<PagedResult<UserDto>> GetUsersAsync(UserListQuery query, CancellationToken cancellationToken);
    Task<Result<UserDto>> GetUserAsync(int id, CancellationToken cancellationToken);
    Task<Result<UserDto>> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken);
    Task<Result<UserDto>> UpdateAsync(int id, UpdateUserRequest request, CancellationToken cancellationToken);
    Task<Result> ResetPasswordAsync(int id, ResetPasswordRequest request, CancellationToken cancellationToken);
    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken);
}

public sealed class UserService(
    IAppDbContext db,
    IPasswordHasher passwordHasher,
    ICurrentUser currentUser,
    ICurrentPermissions permissions,
    TimeProvider timeProvider) : IUserService
{
    private static readonly Error CannotAssignRoles = new(
        ErrorCodes.Forbidden, "Kullanıcıya rol verme yetkiniz yok (Kullanıcıya rol verme).", ErrorType.Forbidden);

    private static readonly Error NotFound = new(
        ErrorCodes.UserNotFound, "Kullanıcı bulunamadı.", ErrorType.NotFound);

    private static readonly Error LastAdminRequired = new(
        ErrorCodes.LastAdminRequired, "Sistemde en az bir aktif yönetici (ADMIN) kalmalıdır.");

    private DateTime UtcNow => timeProvider.GetUtcNow().UtcDateTime;

    public Task<PagedResult<UserDto>> GetUsersAsync(UserListQuery query, CancellationToken cancellationToken)
    {
        var users = db.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            users = users.Where(u => u.UserName.Contains(search) || u.FullName.Contains(search));
        }

        if (query.IsActive is { } isActive)
        {
            users = users.Where(u => u.IsActive == isActive);
        }

        if (!string.IsNullOrWhiteSpace(query.Role))
        {
            users = users.Where(u => u.UserRoles.Any(ur => ur.Role.Code == query.Role));
        }

        return Project(users.OrderBy(u => u.UserName)).ToPagedResultAsync(query, cancellationToken);
    }

    public async Task<Result<UserDto>> GetUserAsync(int id, CancellationToken cancellationToken)
    {
        var user = await Project(db.Users.AsNoTracking().Where(u => u.Id == id))
            .SingleOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            return NotFound;
        }

        return user;
    }

    public async Task<Result<UserDto>> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var normalizedUserName = User.NormalizeUserName(request.UserName);
        if (await UserNameExistsAsync(normalizedUserName, cancellationToken))
        {
            return UserNameAlreadyExists();
        }

        // Rol vermek ayrı yetkidir: kullanıcı yönetimi yetkisi olan biri kendine ya da başkasına yönetici rolü veremesin.
        if (request.Roles.Count > 0 && !await permissions.HasAsync(ERP.Core.Domain.Authorization.Permissions.User.AssignRoles, cancellationToken))
        {
            return CannotAssignRoles;
        }

        var roles = await ResolveRolesAsync(request.Roles, cancellationToken);
        if (!roles.IsSuccess)
        {
            return roles.Error!;
        }

        var user = new User
        {
            UserName = request.UserName.Trim(),
            NormalizedUserName = normalizedUserName,
            FullName = request.FullName.Trim(),
            Email = NormalizeEmail(request.Email),
            PasswordHash = passwordHasher.Hash(request.Password),
            IsActive = true,
        };
        foreach (var role in roles.Value)
        {
            user.UserRoles.Add(new UserRole { RoleId = role.Id });
        }

        db.Users.Add(user);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Aynı kullanıcı adı eşzamanlı başka bir istekle alınmış olabilir (unique index).
            if (await UserNameExistsAsync(normalizedUserName, cancellationToken))
            {
                return UserNameAlreadyExists();
            }

            throw;
        }

        return await GetUserAsync(user.Id, cancellationToken);
    }

    public async Task<Result<UserDto>> UpdateAsync(
        int id, UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var user = await db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .SingleOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user is null)
        {
            return NotFound;
        }

        if (user.Id == currentUser.UserId && !request.IsActive)
        {
            return new Error(ErrorCodes.CannotModifySelf, "Kendi hesabınızı pasif yapamazsınız.");
        }

        var roles = await ResolveRolesAsync(request.Roles, cancellationToken);
        if (!roles.IsSuccess)
        {
            return roles.Error!;
        }

        var rolesChanged = !roles.Value.Select(r => r.Id).ToHashSet().SetEquals(user.UserRoles.Select(ur => ur.RoleId));
        if (rolesChanged && !await permissions.HasAsync(ERP.Core.Domain.Authorization.Permissions.User.AssignRoles, cancellationToken))
        {
            return CannotAssignRoles;
        }

        var isActiveAdmin = user.IsActive && user.UserRoles.Any(ur => ur.Role.Code == RoleCodes.Admin);
        var staysActiveAdmin = request.IsActive && roles.Value.Any(r => r.Code == RoleCodes.Admin);
        if (isActiveAdmin && !staysActiveAdmin && !await AnotherActiveAdminExistsAsync(id, cancellationToken))
        {
            return LastAdminRequired;
        }

        var deactivated = user.IsActive && !request.IsActive;
        user.FullName = request.FullName.Trim();
        user.Email = NormalizeEmail(request.Email);
        user.IsActive = request.IsActive;

        var targetRoleIds = roles.Value.Select(r => r.Id).ToHashSet();
        db.UserRoles.RemoveRange(user.UserRoles.Where(ur => !targetRoleIds.Contains(ur.RoleId)));
        var currentRoleIds = user.UserRoles.Select(ur => ur.RoleId).ToHashSet();
        db.UserRoles.AddRange(targetRoleIds
            .Where(roleId => !currentRoleIds.Contains(roleId))
            .Select(roleId => new UserRole { UserId = user.Id, RoleId = roleId }));

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        if (deactivated)
        {
            await db.RevokeUserSessionsAsync(
                user.Id, RefreshTokenRevokeReasons.UserDeactivated, UtcNow, cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);

        return await GetUserAsync(id, cancellationToken);
    }

    public async Task<Result> ResetPasswordAsync(
        int id, ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user is null)
        {
            return NotFound;
        }

        user.PasswordHash = passwordHasher.Hash(request.NewPassword);
        user.ClearLockout();

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await db.RevokeUserSessionsAsync(user.Id, RefreshTokenRevokeReasons.PasswordChanged, UtcNow, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var user = await db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .SingleOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user is null)
        {
            return NotFound;
        }

        if (user.Id == currentUser.UserId)
        {
            return new Error(ErrorCodes.CannotModifySelf, "Kendi hesabınızı silemezsiniz.");
        }

        var isActiveAdmin = user.IsActive && user.UserRoles.Any(ur => ur.Role.Code == RoleCodes.Admin);
        if (isActiveAdmin && !await AnotherActiveAdminExistsAsync(id, cancellationToken))
        {
            return LastAdminRequired;
        }

        // Fiziksel silme değildir: kayıt IsDeleted olarak işaretlenir (audit için korunur).
        db.Users.Remove(user);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await db.RevokeUserSessionsAsync(user.Id, RefreshTokenRevokeReasons.UserDeleted, UtcNow, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success();
    }

    private IQueryable<UserDto> Project(IQueryable<User> users)
    {
        var now = UtcNow;
        return users.Select(u => new UserDto(
            u.Id,
            u.UserName,
            u.FullName,
            u.Email,
            u.IsActive,
            u.LockoutEndAt != null && u.LockoutEndAt > now,
            u.LastLoginAt,
            u.CreatedAt,
            u.UserRoles.Select(ur => ur.Role.Code).OrderBy(code => code).ToList()));
    }

    private async Task<Result<List<Role>>> ResolveRolesAsync(
        IReadOnlyList<string> roleCodes, CancellationToken cancellationToken)
    {
        var codes = roleCodes.Distinct().ToList();
        var roles = await db.Roles.Where(r => codes.Contains(r.Code)).ToListAsync(cancellationToken);

        var unknown = codes.Except(roles.Select(r => r.Code)).ToList();
        if (unknown.Count > 0)
        {
            return new Error(
                ErrorCodes.ValidationError,
                "Gönderilen veriler geçersiz.",
                ErrorType.Validation,
                new Dictionary<string, string[]> { ["roles"] = [$"Bilinmeyen rol: {string.Join(", ", unknown)}"] });
        }

        return roles;
    }

    private Task<bool> AnotherActiveAdminExistsAsync(int exceptUserId, CancellationToken cancellationToken) =>
        db.UserRoles.AnyAsync(
            ur => ur.UserId != exceptUserId && ur.Role.Code == RoleCodes.Admin && ur.User.IsActive, cancellationToken);

    private Task<bool> UserNameExistsAsync(string normalizedUserName, CancellationToken cancellationToken) =>
        db.Users.AnyAsync(u => u.NormalizedUserName == normalizedUserName, cancellationToken);

    private static Error UserNameAlreadyExists() => new(
        ErrorCodes.UserNameAlreadyExists,
        "Bu kullanıcı adı zaten kullanılıyor.",
        ErrorType.Conflict,
        new Dictionary<string, string[]> { ["userName"] = ["Bu kullanıcı adı zaten kullanılıyor."] });

    private static string? NormalizeEmail(string? email) =>
        string.IsNullOrWhiteSpace(email) ? null : email.Trim();
}
