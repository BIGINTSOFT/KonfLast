using ERP.Core.Domain.Common;
using ERP.Konfeksiyon.Domain.Enums;

namespace ERP.Konfeksiyon.Domain.Entities;

/// <summary>
/// Sayım fişi: bir deponun (ya da tek bir adresinin) sayımı. Açılırken o andaki stok fişe beklenen miktar olarak
/// yazılır; sayım sürerken kapsamdaki adreslerde stok hareketi yapılamaz. Onaylanınca farklar stok düzeltmesi
/// olarak işlenir.
/// </summary>
public class StockCount : AuditableEntity
{
    /// <summary>Sistem tarafından üretilir, örn. SY-2026-00003; değiştirilemez.</summary>
    public string CountNo { get; set; } = null!;

    public int WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;

    /// <summary>Yalnızca bu adres sayılıyorsa; boşsa deponun tamamı.</summary>
    public int? LocationId { get; set; }
    public WarehouseLocation? Location { get; set; }

    public StockCountStatus Status { get; private set; } = StockCountStatus.Open;
    public string? Note { get; set; }

    /// <summary>Sunucu saati.</summary>
    public DateTime? PostedAt { get; private set; }
    public string? PostedBy { get; private set; }

    /// <summary>Aynı fişin iki kullanıcı tarafından eşzamanlı değiştirilmesini engeller.</summary>
    public byte[] RowVersion { get; set; } = null!;

    public ICollection<StockCountLine> Lines { get; } = new List<StockCountLine>();

    /// <summary>Miktarlar yalnızca açık fişte girilebilir; açık fiş kapsamındaki adresleri kilitler.</summary>
    public bool IsOpen => Status == StockCountStatus.Open;

    /// <summary>Adres bu sayımın kapsamında mı.</summary>
    public bool Covers(int locationId) => LocationId is null || LocationId == locationId;

    /// <summary>Fişi onaylar: farklar çağıran tarafından stoğa işlenmiş olmalıdır.</summary>
    public void Post(DateTime utcNow, string postedBy)
    {
        EnsureOpen();
        Status = StockCountStatus.Posted;
        PostedAt = utcNow;
        PostedBy = postedBy;
    }

    public void Cancel()
    {
        EnsureOpen();
        Status = StockCountStatus.Cancelled;
    }

    private void EnsureOpen()
    {
        if (!IsOpen)
        {
            throw new InvalidOperationException($"{Status} durumundaki sayım fişi değiştirilemez.");
        }
    }
}

/// <summary>Sayım satırı: bir kartın (takipli kartta: bir topun) bir adresteki beklenen ve sayılan miktarı.</summary>
public class StockCountLine : BaseEntity
{
    public int StockCountId { get; set; }
    public StockCount StockCount { get; set; } = null!;

    public int LocationId { get; set; }
    public WarehouseLocation Location { get; set; } = null!;

    public int StockItemId { get; set; }
    public StockItem StockItem { get; set; } = null!;

    public int? LotId { get; set; }
    public StockLot? Lot { get; set; }

    /// <summary>Fiş açılırken (ya da satır eklenirken) kayıtlardaki miktar.</summary>
    public decimal ExpectedQuantity { get; set; }

    /// <summary>Sayılan miktar; henüz sayılmadıysa boş. Sayılmayan satır onayda stoğu değiştirmez.</summary>
    public decimal? CountedQuantity { get; set; }

    public decimal? Difference => CountedQuantity - ExpectedQuantity;
}
