using ERP.Core.Domain.Common;
using ERP.Konfeksiyon.Domain.Enums;

namespace ERP.Konfeksiyon.Domain.Entities;

/// <summary>
/// Sevkiyat: bir depodan bir müşteriye gönderilen mamuller. Taslakken düzenlenir; sevk edildiğinde stok
/// düşer ve kayıt kesinleşir (değiştirilemez, geri alınamaz).
/// </summary>
public class Shipment : SoftDeletableEntity
{
    /// <summary>Sistem tarafından üretilir, örn. SV-2026-00031; değiştirilemez ve yeniden kullanılmaz.</summary>
    public string ShipmentNo { get; set; } = null!;

    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    /// <summary>Mamullerin çıkacağı depo.</summary>
    public int WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;

    /// <summary>Sevkiyat bir üretim siparişine aitse o sipariş; sevk edilen adet siparişe işlenir.</summary>
    public int? ProductionOrderId { get; set; }
    public ProductionOrder? ProductionOrder { get; set; }

    /// <summary>
    /// Sevkiyatın karşıladığı müşteri siparişi. Bir sevkiyat siparişin birden fazla ürününü taşıyabilir; üretim
    /// siparişine bağlı sevkiyatta o siparişin müşteri siparişidir. Sevk edilen adet müşteri siparişine işlenir.
    /// </summary>
    public int? SalesOrderId { get; set; }
    public SalesOrder? SalesOrder { get; set; }

    /// <summary>Planlanan / gerçekleşen sevk günü.</summary>
    public DateOnly ShipDate { get; set; }

    /// <summary>İrsaliye numarası.</summary>
    public string? DocumentNo { get; set; }

    /// <summary>Taşıyıcı firma, araç plakası vb.</summary>
    public string? Carrier { get; set; }
    public string? Notes { get; set; }

    /// <summary>Faturalandıysa fatura; bir sevkiyat tek faturaya girer.</summary>
    public int? InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }

    public ShipmentStatus Status { get; private set; } = ShipmentStatus.Draft;

    /// <summary>Stoğun düştüğü an (sunucu saati); irsaliyenin düzenlenme zamanıdır.</summary>
    public DateTime? ShippedAt { get; private set; }
    public string? ShippedBy { get; private set; }

    /// <summary>Toplamayı üstlenen (el terminalinde toplamaya başlayan) kullanıcı; aynı işi iki kişi toplayamaz.</summary>
    public string? PickingBy { get; private set; }
    public DateTime? PickingStartedAt { get; private set; }

    /// <summary>Toplamanın bittiği an ve bitiren kullanıcı.</summary>
    public DateTime? PickedAt { get; private set; }
    public string? PickedBy { get; private set; }

    /// <summary>Araç plakası, şoför adı ve T.C. kimlik numarası (e-İrsaliye taşıyıcı bilgileri).</summary>
    public string? VehiclePlate { get; set; }
    public string? DriverName { get; set; }
    public string? DriverIdentityNo { get; set; }

    /// <summary>İrsaliyenin evrensel tekil kimliği (ETTN); irsaliye kesilirken verilir.</summary>
    public Guid? DespatchUuid { get; private set; }

    /// <summary>Aynı sevkiyatın iki kullanıcı tarafından eşzamanlı değiştirilmesini engeller.</summary>
    public byte[] RowVersion { get; set; } = null!;

    public ICollection<ShipmentItem> Items { get; } = new List<ShipmentItem>();

    /// <summary>
    /// Koliler (çeki listesi). Boş olabilir; koli varsa sevk anında kolilerdeki adetler kalemlerle birebir tutmalıdır.
    /// </summary>
    public ICollection<ShipmentCarton> Cartons { get; } = new List<ShipmentCarton>();

    /// <summary>Toplama kayıtları: hangi kalemden hangi adresten ne kadar alındığı.</summary>
    public ICollection<ShipmentPick> Picks { get; } = new List<ShipmentPick>();

    /// <summary>Başlık ve kalemler yalnızca taslakken değiştirilebilir.</summary>
    public bool IsEditable => Status == ShipmentStatus.Draft;

    /// <summary>Koliler toplamadan önce (plan) ya da toplama bittikten sonra (gerçek adetlerle) düzenlenir.</summary>
    public bool CanPack => Status is ShipmentStatus.Draft or ShipmentStatus.Picked;

    /// <summary>Kalemi olan taslak depoya toplamaya verilebilir.</summary>
    public bool CanSendToPicking => Status == ShipmentStatus.Draft && Items.Count > 0;

    /// <summary>Henüz hiçbir şey toplanmamışsa toplama geri alınıp sevkiyat yeniden düzenlenebilir.</summary>
    public bool CanReturnToDraft => Status == ShipmentStatus.Picking && Items.All(i => i.PickedQuantity == 0);

    /// <summary>En az bir kalemden mal toplanmışsa toplama bitirilebilir (eksikler sevkiyattan düşer).</summary>
    public bool CanCompletePicking => Status == ShipmentStatus.Picking && Items.Any(i => i.PickedQuantity > 0);

    /// <summary>Kontrolde fark çıkarsa toplanmış sevkiyat yeniden toplamaya açılır.</summary>
    public bool CanReopenPicking => Status == ShipmentStatus.Picked;

    /// <summary>Toplanmış sevkiyatın adetleri kontrol edilip irsaliyesi kesilebilir (stok bu anda düşer).</summary>
    public bool CanShip => Status == ShipmentStatus.Picked && Items.Count > 0;

    /// <summary>Stok düşmeden önce (taslak, toplamada, toplandı) iptal edilebilir.</summary>
    public bool CanCancel => Status is ShipmentStatus.Draft or ShipmentStatus.Picking or ShipmentStatus.Picked;

    public void SendToPicking()
    {
        if (!CanSendToPicking)
        {
            throw new InvalidOperationException(
                Status == ShipmentStatus.Draft ? "Kalemi olmayan sevkiyat toplamaya verilemez." : $"{Status} durumundaki sevkiyat toplamaya verilemez.");
        }

        Status = ShipmentStatus.Picking;
    }

    public void ReturnToDraft()
    {
        if (!CanReturnToDraft)
        {
            throw new InvalidOperationException("Toplanmaya başlanmış sevkiyat taslağa döndürülemez.");
        }

        Status = ShipmentStatus.Draft;
        PickingBy = null;
        PickingStartedAt = null;
    }

    /// <summary>Toplamayı kullanıcı üstlenir (başkasından devralma çağıran tarafça onaylanmış olmalıdır).</summary>
    public void ClaimPicking(DateTime utcNow, string user)
    {
        if (Status != ShipmentStatus.Picking)
        {
            throw new InvalidOperationException($"{Status} durumundaki sevkiyat toplanamaz.");
        }

        PickingBy = user;
        PickingStartedAt ??= utcNow;
    }

    public void CompletePicking(DateTime utcNow, string user)
    {
        if (!CanCompletePicking)
        {
            throw new InvalidOperationException("Hiç mal toplanmamış sevkiyatın toplaması bitirilemez.");
        }

        Status = ShipmentStatus.Picked;
        PickedAt = utcNow;
        PickedBy = user;
    }

    public void ReopenPicking()
    {
        if (!CanReopenPicking)
        {
            throw new InvalidOperationException($"{Status} durumundaki sevkiyat yeniden toplamaya açılamaz.");
        }

        Status = ShipmentStatus.Picking;
        PickedAt = null;
        PickedBy = null;
    }

    /// <summary>İrsaliyeyi keser: adetler kontrol edilmiş olmalıdır; ETTN verilir, stok düşer, sevkiyat kesinleşir.</summary>
    public void Ship(DateTime utcNow, string shippedBy, Guid despatchUuid)
    {
        if (!CanShip)
        {
            throw new InvalidOperationException(
                Status == ShipmentStatus.Picked ? "Kalemi olmayan sevkiyat sevk edilemez." : $"{Status} durumundaki sevkiyat sevk edilemez.");
        }

        Status = ShipmentStatus.Shipped;
        ShippedAt = utcNow;
        ShippedBy = shippedBy;
        DespatchUuid = despatchUuid;
    }

    public void Cancel()
    {
        if (!CanCancel)
        {
            throw new InvalidOperationException($"{Status} durumundaki sevkiyat iptal edilemez.");
        }

        Status = ShipmentStatus.Cancelled;
    }
}

public class ShipmentItem : BaseEntity
{
    public int ShipmentId { get; set; }
    public Shipment Shipment { get; set; } = null!;

    /// <summary>Sevk edilen mamul kartı.</summary>
    public int StockItemId { get; set; }
    public StockItem StockItem { get; set; } = null!;

    /// <summary>Sevk edilecek adet. Toplama eksik biterse toplanan adede iner; ilk istenen adet <see cref="RequestedQuantity"/>'da kalır.</summary>
    public decimal Quantity { get; set; }

    /// <summary>Toplama eksik bittiğinde ilk istenen adet; toplama yeniden açılınca kalem bu adede döner.</summary>
    public decimal? RequestedQuantity { get; set; }

    /// <summary>Depodan toplanan adet (toplama kayıtlarının toplamı).</summary>
    public decimal PickedQuantity { get; set; }

    /// <summary>İrsaliye öncesi son kontrolde sayılan adet.</summary>
    public decimal? CheckedQuantity { get; set; }
}

/// <summary>
/// Toplama kaydı: sevkiyatın bir kaleminden bir adresten alınan adet. Stok hareketi değildir; stok, irsaliye kesilince
/// önce bu adreslerden düşer. Toplama sürerken geri alınabilir (kayıt silinir).
/// </summary>
[NotAudited]
public class ShipmentPick
{
    public long Id { get; set; }

    /// <summary>İstemcinin ürettiği istek kimliği; aynı okutma yeniden gelirse ikinci kez yazılmaz.</summary>
    public Guid RequestId { get; set; }

    public int ShipmentId { get; set; }
    public Shipment Shipment { get; set; } = null!;

    public int ShipmentItemId { get; set; }
    public ShipmentItem ShipmentItem { get; set; } = null!;

    /// <summary>Malın alındığı adres (sevkiyat deposunun adresi).</summary>
    public int LocationId { get; set; }
    public WarehouseLocation Location { get; set; } = null!;

    public decimal Quantity { get; set; }

    /// <summary>Sunucu saati.</summary>
    public DateTime PickedAt { get; set; }
    public string PickedBy { get; set; } = null!;
}
