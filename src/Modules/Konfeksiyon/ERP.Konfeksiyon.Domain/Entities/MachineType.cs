using ERP.Core.Domain.Common;

namespace ERP.Konfeksiyon.Domain.Entities;

public class MachineType : SoftDeletableEntity, IActivatable
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public bool IsActive { get; set; } = true;
}
