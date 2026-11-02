using ERP.Core.Domain.Common;

namespace ERP.Core.Domain.Entities.Identity;

public class Role : BaseEntity
{
    /// <summary>Sabit rol kodu, örn. ADMIN, LINE_MANAGER.</summary>
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;

    /// <summary>Sistem rolleri silinemez / kodu değiştirilemez.</summary>
    public bool IsSystem { get; set; }

    /// <summary>Rolün ait olduğu proje (modül anahtarı); null ise rol bütün projelerde geçerlidir.</summary>
    public string? ModuleKey { get; set; }

    public ICollection<UserRole> UserRoles { get; } = new List<UserRole>();
    public ICollection<RolePermission> RolePermissions { get; } = new List<RolePermission>();
}
