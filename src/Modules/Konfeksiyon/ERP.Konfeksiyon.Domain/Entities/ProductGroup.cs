using ERP.Core.Domain.Common;

namespace ERP.Konfeksiyon.Domain.Entities;

/// <summary>Ürün grubu (Tişört, Polo, Sweatshirt, Pantolon …); ürünleri sınıflandırmak ve raporlamak için.</summary>
public class ProductGroup : SoftDeletableEntity, IActivatable
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public bool IsActive { get; set; } = true;
}
