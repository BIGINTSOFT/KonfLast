using ERP.Core.Domain.Common;

namespace ERP.Konfeksiyon.Domain.Entities;

public class Color : SoftDeletableEntity, IActivatable
{
    public string ColorCode { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? HexCode { get; set; }
    public bool IsActive { get; set; } = true;
}
