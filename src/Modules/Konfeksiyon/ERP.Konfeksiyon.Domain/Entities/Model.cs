using ERP.Core.Domain.Common;

namespace ERP.Konfeksiyon.Domain.Entities;

/// <summary>Model (stil): operasyon rotasının bağlandığı tasarım, örn. TSH-001.</summary>
public class Model : SoftDeletableEntity, IActivatable
{
    public string ModelCode { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Product> Products { get; } = new List<Product>();
}
