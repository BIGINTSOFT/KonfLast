using ERP.Core.Domain.Common;

namespace ERP.Konfeksiyon.Domain.Entities;

/// <summary>
/// Satış fiyat listesi. Müşteri seçilmişse yalnızca o müşterinin siparişlerinde, seçilmemişse (genel liste) tüm
/// müşterilerde kullanılır. Bir gün için birden fazla liste geçerliyse müşteriye özel liste genel listeden, aynı
/// türde olanlardan başlangıcı en yeni olan öncelidir.
/// </summary>
public class PriceList : SoftDeletableEntity, IActivatable
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;

    /// <summary>Boşsa genel liste.</summary>
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    /// <summary>Kalem fiyatlarının para birimi.</summary>
    public string Currency { get; set; } = ERP.Core.Domain.Entities.Currency.LocalCode;

    public DateOnly ValidFrom { get; set; }

    /// <summary>Boşsa süresiz.</summary>
    public DateOnly? ValidTo { get; set; }

    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<PriceListItem> Items { get; } = new List<PriceListItem>();
}

/// <summary>
/// Fiyat listesinde bir ürünün fiyatı. Renk ve / veya beden boşsa ürünün tüm renk / bedenleri için geçerlidir;
/// dolu olan satır (ör. büyük bedenler için farklı fiyat) genel satırın yerine geçer.
/// </summary>
public class PriceListItem : BaseEntity
{
    public int PriceListId { get; set; }
    public PriceList PriceList { get; set; } = null!;

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public int? ColorId { get; set; }
    public Color? Color { get; set; }

    public int? SizeId { get; set; }
    public Size? Size { get; set; }

    public decimal UnitPrice { get; set; }

    /// <summary>Eşleşmenin özgüllüğü: renk ve beden belirtilen satır en özel olandır.</summary>
    public int Specificity => (SizeId is null ? 0 : 2) + (ColorId is null ? 0 : 1);
}

/// <summary>
/// Müşteri iskontosu (yüzde). Ürün boşsa müşterinin genel iskontosudur; ürüne özel iskonto, o üründe genel
/// iskontonun yerine geçer (üst üste uygulanmaz).
/// </summary>
public class CustomerDiscount : AuditableEntity
{
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public int? ProductId { get; set; }
    public Product? Product { get; set; }

    public decimal DiscountPercent { get; set; }

    public DateOnly ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }

    public string? Notes { get; set; }
}
