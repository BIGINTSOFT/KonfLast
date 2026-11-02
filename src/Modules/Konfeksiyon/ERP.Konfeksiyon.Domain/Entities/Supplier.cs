using ERP.Core.Domain.Common;

namespace ERP.Konfeksiyon.Domain.Entities;

public class Supplier : SoftDeletableEntity, IActivatable
{
    public string SupplierCode { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? TaxNumber { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public bool IsActive { get; set; } = true;
}
