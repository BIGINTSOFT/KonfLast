using ERP.Core.Domain.Common;
using ERP.Konfeksiyon.Domain.Enums;

namespace ERP.Konfeksiyon.Domain.Entities;

/// <summary>
/// Üretim siparişi (iş emri): bir müşteri için bir ürünün renk-beden dağılımıyla üretilecek adetleri.
/// </summary>
public class ProductionOrder : SoftDeletableEntity
{
    private static readonly IReadOnlyDictionary<ProductionOrderStatus, ProductionOrderStatus[]> Transitions =
        new Dictionary<ProductionOrderStatus, ProductionOrderStatus[]>
        {
            [ProductionOrderStatus.Draft] = [ProductionOrderStatus.Released, ProductionOrderStatus.Cancelled],
            [ProductionOrderStatus.Released] =
            [
                ProductionOrderStatus.Draft, ProductionOrderStatus.InProduction, ProductionOrderStatus.Cancelled,
            ],
            [ProductionOrderStatus.InProduction] = [ProductionOrderStatus.Completed, ProductionOrderStatus.Cancelled],
            [ProductionOrderStatus.Completed] = [],
            [ProductionOrderStatus.Cancelled] = [],
        };

    /// <summary>Sistem tarafından üretilir, örn. IE-2026-00125; değiştirilemez ve yeniden kullanılmaz.</summary>
    public string OrderNo { get; set; } = null!;

    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    /// <summary>
    /// Reçete malzemesinin ayrılacağı ve düşüleceği depo. Boşsa tek hammadde deposu kullanılır; birden fazla
    /// hammadde deposu varsa depo her işlemde seçilir.
    /// </summary>
    public int? MaterialWarehouseId { get; set; }

    /// <summary>Bu üretim siparişi bir müşteri siparişinden açıldıysa o sipariş.</summary>
    public int? SalesOrderId { get; set; }
    public SalesOrder? SalesOrder { get; set; }

    /// <summary>Müşterinin kendi sipariş numarası (varsa).</summary>
    public string? CustomerOrderNo { get; set; }

    public DateOnly OrderDate { get; set; }

    /// <summary>Termin tarihi.</summary>
    public DateOnly DueDate { get; set; }

    public ProductionOrderStatus Status { get; private set; } = ProductionOrderStatus.Draft;
    public string? Notes { get; set; }

    /// <summary>
    /// Demet operasyonlarının sırası (sıralı / sırasız). Açılan demetlere kopyalanır; ilk demet açıldıktan
    /// sonra değiştirilemez.
    /// </summary>
    public OperationSequenceMode OperationSequence { get; set; } = OperationSequenceMode.Sequential;

    /// <summary>Aynı siparişin iki kullanıcı tarafından eşzamanlı değiştirilmesini engeller.</summary>
    public byte[] RowVersion { get; set; } = null!;

    public ICollection<ProductionOrderItem> Items { get; } = new List<ProductionOrderItem>();

    /// <summary>Başlık ve kalemler yalnızca taslakken değiştirilebilir.</summary>
    public bool IsEditable => Status == ProductionOrderStatus.Draft;

    public IReadOnlyList<ProductionOrderStatus> AllowedTransitions => Transitions[Status];

    public bool CanTransitionTo(ProductionOrderStatus target) => Transitions[Status].Contains(target);

    public void TransitionTo(ProductionOrderStatus target)
    {
        if (!CanTransitionTo(target))
        {
            throw new InvalidOperationException($"{Status} durumundaki sipariş {target} durumuna geçemez.");
        }

        Status = target;
    }
}

/// <summary>Siparişin bir renk-beden kırılımı için üretilecek adet.</summary>
public class ProductionOrderItem : BaseEntity
{
    public int ProductionOrderId { get; set; }
    public ProductionOrder ProductionOrder { get; set; } = null!;

    public int ColorId { get; set; }
    public Color Color { get; set; } = null!;

    public int SizeId { get; set; }
    public Size Size { get; set; } = null!;

    public int Quantity { get; set; }
}
