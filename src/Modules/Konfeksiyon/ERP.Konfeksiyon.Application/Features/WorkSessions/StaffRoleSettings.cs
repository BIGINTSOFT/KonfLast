using ERP.Konfeksiyon.Application.Abstractions;
using ERP.Konfeksiyon.Domain.Entities;
using ERP.Konfeksiyon.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ERP.Konfeksiyon.Application.Features.WorkSessions;

/// <summary>Bir görevin tabletteki ortak yetkileri.</summary>
public sealed record StaffRoleSettingDto(
    StaffRole Role, bool CanPause, bool CanReportBreakdown, bool CanResolveBreakdown, bool CanReportReject = false);

/// <summary>
/// Görevlerin tabletteki ortak yetkileri. Ayrı bir tanım yoktur: her görev bir sistem rolüne karşılık gelir
/// ve yetkileri "Roller ve Yetkiler" ekranında o role verilen saha yetkilerinden (Floor.*) okunur.
/// </summary>
public interface IStaffRoleSettingService
{
    /// <summary>Tüm görevlerin yetkileri, görev sırasıyla.</summary>
    Task<IReadOnlyList<StaffRoleSetting>> GetAllAsync(CancellationToken cancellationToken);

    Task<StaffRoleSetting> GetAsync(StaffRole role, CancellationToken cancellationToken);
}

public sealed class StaffRoleSettingService(IKonfeksiyonDbContext db) : IStaffRoleSettingService
{
    public static StaffRoleSettingDto ToDto(StaffRoleSetting setting) =>
        new(setting.Role, setting.CanPause, setting.CanReportBreakdown, setting.CanResolveBreakdown, setting.CanReportReject);

    public async Task<IReadOnlyList<StaffRoleSetting>> GetAllAsync(CancellationToken cancellationToken)
    {
        var roles = Enum.GetValues<StaffRole>();
        var roleCodes = roles.Select(StaffRoleSetting.RoleCodeOf).ToList();
        var granted = await db.RolePermissions
            .AsNoTracking()
            .Where(rp => roleCodes.Contains(rp.Role.Code) && StaffRoleSetting.PermissionCodes.Contains(rp.Permission.Code))
            .Select(rp => new { Role = rp.Role.Code, Permission = rp.Permission.Code })
            .ToListAsync(cancellationToken);

        return roles
            .Select(role => StaffRoleSetting.FromPermissions(
                role, granted.Where(g => g.Role == StaffRoleSetting.RoleCodeOf(role)).Select(g => g.Permission)))
            .ToList();
    }

    public async Task<StaffRoleSetting> GetAsync(StaffRole role, CancellationToken cancellationToken)
    {
        var roleCode = StaffRoleSetting.RoleCodeOf(role);
        var granted = await db.RolePermissions
            .AsNoTracking()
            .Where(rp => rp.Role.Code == roleCode && StaffRoleSetting.PermissionCodes.Contains(rp.Permission.Code))
            .Select(rp => rp.Permission.Code)
            .ToListAsync(cancellationToken);

        return StaffRoleSetting.FromPermissions(role, granted);
    }
}
