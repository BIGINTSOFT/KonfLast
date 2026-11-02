using ERP.Core.Domain.Common;
using ERP.Konfeksiyon.Domain.Enums;

namespace ERP.Konfeksiyon.Domain.Entities;

/// <summary>
/// Satın alma siparişi: bir tedarikçiden istenen malzemeler. Onaylandıktan sonra içeriği kilitlenir ve
/// mal kabul yapılabilir; teslim alındıkça durum kendiliğinden ilerler.
/// </summary>
public class PurchaseOrder : SoftDeletableEntity
{
    /// <summary>Kullanıcının yapabildiği geçişler. Teslim durumları (kısmen / tamamen) mal kabulle oluşur.</summary>
    private static readonly IReadOnlyDictionary<PurchaseOrderStatus, PurchaseOrderStatus[]> Transitions =
        new Dictionary<PurchaseOrderStatus, PurchaseOrderStatus[]>
        {
            [PurchaseOrderStatus.Draft] = [PurchaseOrderStatus.Approved, PurchaseOrderStatus.Cancelled],
            [PurchaseOrderStatus.Approved] = [PurchaseOrderStatus.Draft, PurchaseOrderStatus.Cancelled],
            [PurchaseOrderStatus.PartiallyReceived] = [PurchaseOrderStatus.Closed],
            [PurchaseOrderStatus.Received] = [],
            [PurchaseOrderStatus.Closed] = [],
            [PurchaseOrderStatus.Cancelled] = [],
        };

    /// <summary>Sistem tarafından üretilir, örn. SA-2026-00012; değiştirilemez ve yeniden kullanılmaz.</summary>
    public string OrderNo { get; set; } = null!;

    public int SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;

    public DateOnly OrderDate { get; set; }

    /// <summary>Tedarikçinin teslim etmesi beklenen tarih.</summary>
    public DateOnly? ExpectedDate { get; set; }

    /// <summary>Birim fiyatların para birimi (ISO 4217, örn. TRY).</summary>
    public string Currency { get; set; } = "TRY";

    /// <summary>
    /// Sipariş para biriminin sistemin maliyet para birimine kuru (1 birim = kaç maliyet birimi). Dövizli alışta
    /// mal kabul, birim fiyatı bu kurla çevirip stok kartının ortalama maliyetine işler; yerel para biriminde boştur.
    /// </summary>
    public decimal? ExchangeRate { get; set; }

    public PurchaseOrderStatus Status { get; private set; } = PurchaseOrderStatus.Draft;
    public string? Notes { get; set; }

    /// <summary>Aynı siparişin iki kullanıcı tarafından eşzamanlı değiştirilmesini engeller.</summary>
    public byte[] RowVersion { get; set; } = null!;

    public ICollection<PurchaseOrderItem> Items { get; } = new List<PurchaseOrderItem>();

    /// <summary>Başlık ve kalemler yalnızca taslakken değiştirilebilir.</summary>
    public bool IsEditable => Status == PurchaseOrderStatus.Draft;

    /// <summary>Onaylı ve henüz tamamı teslim alınmamış siparişe mal kabul yapılabilir.</summary>
    public bool CanReceive => Status is PurchaseOrderStatus.Approved or PurchaseOrderStatus.PartiallyReceived;

    public IReadOnlyList<PurchaseOrderStatus> AllowedTransitions => Transitions[Status];

    public bool CanTransitionTo(PurchaseOrderStatus target) => Transitions[Status].Contains(target);

    public void TransitionTo(PurchaseOrderStatus target)
    {
        if (!CanTransitionTo(target))
        {
            throw new InvalidOperationException($"{Status} durumundaki satın alma siparişi {target} durumuna geçemez.");
        }

        Status = target;
    }

    /// <summary>
    /// Kalemin bir kısmını ya da tamamını teslim alır. Sipariş edilenin en çok <paramref name="tolerancePercent"/>
    /// kadar fazlası alınabilir (top kumaş, tartıyla gelen malzeme). Tüm kalemler tamamlanınca sipariş "teslim
    /// alındı", aksi halde "kısmen teslim alındı" olur.
    /// </summary>
    public void Receive(PurchaseOrderItem item, decimal quantity, decimal tolerancePercent = 0)
    {
        if (!CanReceive)
        {
            throw new InvalidOperationException($"{Status} durumundaki satın alma siparişine mal kabul yapılamaz.");
        }

        if (!Items.Contains(item))
        {
            throw new ArgumentException("Kalem bu siparişe ait değil.", nameof(item));
        }

        item.Receive(quantity, tolerancePercent);
        Status = Items.All(i => i.RemainingQuantity == 0) ? PurchaseOrderStatus.Received : PurchaseOrderStatus.PartiallyReceived;
    }
}

public class PurchaseOrderItem : BaseEntity
{
    public int PurchaseOrderId { get; set; }
    public PurchaseOrder PurchaseOrder { get; set; } = null!;

    public int StockItemId { get; set; }
    public StockItem StockItem { get; set; } = null!;

    /// <summary>Sipariş edilen miktar (stok kartının biriminde).</summary>
    public decimal Quantity { get; set; }

    /// <summary>Birim fiyat (siparişin para biriminde); bilinmiyorsa boş.</summary>
    public decimal? UnitPrice { get; set; }

    /// <summary>Kalem bir üretim siparişinin eksik malzemesi için açıldıysa o sipariş; üretim siparişinde "siparişte" görünür.</summary>
    public int? ProductionOrderId { get; set; }
    public ProductionOrder? ProductionOrder { get; set; }

    public decimal ReceivedQuantity { get; private set; }

    /// <summary>Henüz teslim alınmamış miktar; tolerans içinde fazla teslim alınan kalemde sıfırdır.</summary>
    public decimal RemainingQuantity => Math.Max(Quantity - ReceivedQuantity, 0);

    /// <summary>Tolerans dahil daha en çok ne kadar teslim alınabileceği.</summary>
    public decimal ReceivableQuantity(decimal tolerancePercent) =>
        Math.Max(decimal.Round(Quantity * (1 + tolerancePercent / 100m), 3, MidpointRounding.ToZero) - ReceivedQuantity, 0);

    internal void Receive(decimal quantity, decimal tolerancePercent)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Teslim alınan miktar sıfırdan büyük olmalıdır.");
        }

        if (quantity > ReceivableQuantity(tolerancePercent))
        {
            throw new InvalidOperationException("Sipariş edilenden (tolerans dahil) fazlası teslim alınamaz.");
        }

        ReceivedQuantity += quantity;
    }
}

/// <summary>Mal kabul: bir satın alma siparişinden bir depoya tek seferde teslim alınanlar. Değiştirilmez.</summary>
[NotAudited]
public class GoodsReceipt
{
    public long Id { get; set; }

    /// <summary>İstemcinin ürettiği istek kimliği; aynı istek yeniden gelirse mal kabul ikinci kez yazılmaz.</summary>
    public Guid RequestId { get; set; }

    public int PurchaseOrderId { get; set; }
    public PurchaseOrder PurchaseOrder { get; set; } = null!;

    public int WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;

    /// <summary>Tedarikçinin irsaliye numarası.</summary>
    public string? DocumentNo { get; set; }
    public string? Note { get; set; }

    /// <summary>Sunucu saati.</summary>
    public DateTime ReceivedAt { get; set; }
    public string ReceivedBy { get; set; } = null!;

    public ICollection<GoodsReceiptLine> Lines { get; } = new List<GoodsReceiptLine>();
}

[NotAudited]
public class GoodsReceiptLine
{
    public long Id { get; set; }

    public long GoodsReceiptId { get; set; }
    public GoodsReceipt GoodsReceipt { get; set; } = null!;

    public int PurchaseOrderItemId { get; set; }
    public PurchaseOrderItem PurchaseOrderItem { get; set; } = null!;

    public decimal Quantity { get; set; }
}
