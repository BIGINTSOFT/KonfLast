using ERP.Core.Domain.Common;
using ERP.Konfeksiyon.Domain.Enums;

namespace ERP.Konfeksiyon.Domain.Entities;

/// <summary>
/// Müşteri siparişi: müşterinin istediği ürünler, renk-beden adetleri ve fiyatları. Onaylandıktan sonra ürün
/// başına bir üretim siparişine aktarılır; üretim, tedarik ve sevkiyat o üretim siparişleri üzerinden yürür.
/// </summary>
public class SalesOrder : SoftDeletableEntity
{
    private static readonly IReadOnlyDictionary<SalesOrderStatus, SalesOrderStatus[]> Transitions =
        new Dictionary<SalesOrderStatus, SalesOrderStatus[]>
        {
            [SalesOrderStatus.Draft] = [SalesOrderStatus.Submitted, SalesOrderStatus.Cancelled],
            [SalesOrderStatus.Submitted] = [SalesOrderStatus.Approved, SalesOrderStatus.Draft, SalesOrderStatus.Cancelled],
            [SalesOrderStatus.Approved] = [SalesOrderStatus.Closed, SalesOrderStatus.Draft, SalesOrderStatus.Cancelled],
            [SalesOrderStatus.Closed] = [],
            [SalesOrderStatus.Cancelled] = [],
        };

    /// <summary>Sistem tarafından üretilir, örn. MS-2026-00042; değiştirilemez ve yeniden kullanılmaz.</summary>
    public string OrderNo { get; set; } = null!;

    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    /// <summary>Müşteri adına siparişi veren buyer (alt müşteri), varsa.</summary>
    public int? CustomerBuyerId { get; set; }
    public CustomerBuyer? CustomerBuyer { get; set; }

    /// <summary>Malın teslim edileceği müşteri adresi; yeni siparişte müşterinin varsayılan teslimat adresi gelir.</summary>
    public int? DeliveryAddressId { get; set; }
    public CustomerAddress? DeliveryAddress { get; set; }

    /// <summary>Müşterinin kendi sipariş numarası (varsa).</summary>
    public string? CustomerOrderNo { get; set; }

    public DateOnly OrderDate { get; set; }

    /// <summary>Müşteriye söz verilen teslim tarihi.</summary>
    public DateOnly DueDate { get; set; }

    /// <summary>Birim fiyatların para birimi (ISO 4217, örn. TRY).</summary>
    public string Currency { get; set; } = "TRY";

    public SalesOrderStatus Status { get; private set; } = SalesOrderStatus.Draft;
    public string? Notes { get; set; }

    /// <summary>Onaya gönderen ve zamanı (sunucu saati).</summary>
    public DateTime? SubmittedAt { get; private set; }
    public string? SubmittedBy { get; private set; }

    /// <summary>Son onay ya da ret kararı: kim, ne zaman, hangi notla.</summary>
    public DateTime? DecidedAt { get; private set; }
    public string? DecidedBy { get; private set; }
    public string? DecisionNote { get; private set; }

    /// <summary>Kaçıncı onaya gönderim; her gönderimde artar ve onay adımları bu turla kaydedilir.</summary>
    public int ApprovalRound { get; private set; }

    /// <summary>Kapanış: ne zaman, kim (otomatik kapanışta sevk eden), hangi notla.</summary>
    public DateTime? ClosedAt { get; private set; }
    public string? ClosedBy { get; private set; }
    public string? CloseNote { get; private set; }

    /// <summary>Tamamı sevk edildiği için kendiliğinden kapandı (elle kapatılmadı).</summary>
    public bool ClosedAutomatically { get; private set; }

    /// <summary>Aynı siparişin iki kullanıcı tarafından eşzamanlı değiştirilmesini engeller.</summary>
    public byte[] RowVersion { get; set; } = null!;

    public ICollection<SalesOrderLine> Lines { get; } = new List<SalesOrderLine>();

    /// <summary>Başlık ve kalemler yalnızca taslakken değiştirilebilir.</summary>
    public bool IsEditable => Status == SalesOrderStatus.Draft;

    /// <summary>Yalnızca onaylı sipariş üretime aktarılabilir.</summary>
    public bool CanGenerateProduction => Status == SalesOrderStatus.Approved;

    public IReadOnlyList<SalesOrderStatus> AllowedTransitions => Transitions[Status];

    public bool CanTransitionTo(SalesOrderStatus target) => Transitions[Status].Contains(target);

    /// <summary>Taslağı onaya gönderir; içerik kilitlenir, önceki karar notu silinir.</summary>
    public void Submit(DateTime utcNow, string user)
    {
        Move(SalesOrderStatus.Submitted);
        SubmittedAt = utcNow;
        ApprovalRound++;
        SubmittedBy = user;
        DecidedAt = null;
        DecidedBy = null;
        DecisionNote = null;
    }

    public void Approve(DateTime utcNow, string user, string? note)
    {
        Move(SalesOrderStatus.Approved);
        Decide(utcNow, user, note);
    }

    /// <summary>Onaya gönderilmiş siparişi düzeltilmek üzere taslağa geri çevirir.</summary>
    public void Reject(DateTime utcNow, string user, string? note)
    {
        if (Status != SalesOrderStatus.Submitted)
        {
            throw new InvalidOperationException($"{Status} durumundaki müşteri siparişi reddedilemez.");
        }

        Move(SalesOrderStatus.Draft);
        Decide(utcNow, user, note);
    }

    /// <summary>
    /// Onaylı siparişi revizyon için taslağa alır (müşteri adet / ürün değiştirdiğinde). Üretimi ya da sevkiyatı
    /// başlamamış olmalıdır; bu, uygulama katmanında denetlenir. Düzeltilen sipariş yeniden onaya gönderilir.
    /// </summary>
    public void Revise(DateTime utcNow, string user, string? note)
    {
        if (Status != SalesOrderStatus.Approved)
        {
            throw new InvalidOperationException($"{Status} durumundaki müşteri siparişi revizyona alınamaz.");
        }

        Move(SalesOrderStatus.Draft);
        Decide(utcNow, user, note);
    }

    /// <summary>
    /// Siparişi kapatır: tamamı sevk edilince kendiliğinden (<paramref name="automatic"/>) ya da kalan adet sevk
    /// edilmeyecekse elle.
    /// </summary>
    public void Close(DateTime utcNow, string user, string? note, bool automatic)
    {
        Move(SalesOrderStatus.Closed);
        ClosedAt = utcNow;
        ClosedBy = user;
        CloseNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        ClosedAutomatically = automatic;
    }

    public void Cancel() => Move(SalesOrderStatus.Cancelled);

    private void Decide(DateTime utcNow, string user, string? note)
    {
        DecidedAt = utcNow;
        DecidedBy = user;
        DecisionNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
    }

    private void Move(SalesOrderStatus target)
    {
        if (!CanTransitionTo(target))
        {
            throw new InvalidOperationException($"{Status} durumundaki müşteri siparişi {target} durumuna geçemez.");
        }

        Status = target;
    }
}

/// <summary>Müşteri siparişinin bir ürün, renk ve beden için istenen adedi.</summary>
public class SalesOrderLine : BaseEntity
{
    public int SalesOrderId { get; set; }
    public SalesOrder SalesOrder { get; set; } = null!;

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public int ColorId { get; set; }
    public Color Color { get; set; } = null!;

    public int SizeId { get; set; }
    public Size Size { get; set; } = null!;

    public int Quantity { get; set; }

    /// <summary>Birim fiyat (siparişin para biriminde); bilinmiyorsa boş.</summary>
    public decimal? UnitPrice { get; set; }

    /// <summary>
    /// Sipariş için ayrılmış stoktan sevk edilen adet (ayırma sevkiyatla tükendikçe artar). Ayrılan + bu adet, kalemin
    /// stoktan karşılanan kısmıdır; geri kalanı planlamada üretime açılır.
    /// </summary>
    public decimal ShippedFromReservation { get; set; }
}

/// <summary>
/// Müşteri siparişi için depoda ayrılmış hazır mamul (ürün + renk + beden). Ayrılan miktar başka siparişlerin
/// sevkiyatında ve elle çıkışlarda kullanılamaz; siparişin kendi sevkiyatı önce bu ayırmadan düşer.
/// </summary>
public class SalesOrderReservation
{
    public int Id { get; set; }

    public int SalesOrderId { get; set; }
    public SalesOrder SalesOrder { get; set; } = null!;

    public int SalesOrderLineId { get; set; }
    public SalesOrderLine SalesOrderLine { get; set; } = null!;

    public int WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;

    /// <summary>Kalemin hazır mamul stok kartı.</summary>
    public int StockItemId { get; set; }
    public StockItem StockItem { get; set; } = null!;

    /// <summary>Hâlâ ayrılmış duran adet; sıfıra inen ayırma silinir.</summary>
    public decimal Quantity { get; set; }

    /// <summary>Sunucu saati.</summary>
    public DateTime UpdatedAt { get; set; }
    public string UpdatedBy { get; set; } = null!;
}
