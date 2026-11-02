using ERP.Core.Domain.Common;

namespace ERP.Core.Domain.Entities.Identity;

public class Permission : BaseEntity
{
    /// <summary>Örn. ProductionOrder.View</summary>
    public string Code { get; set; } = null!;
    public string Module { get; set; } = null!;
    public string Description { get; set; } = null!;

    public ICollection<RolePermission> RolePermissions { get; } = new List<RolePermission>();
}
