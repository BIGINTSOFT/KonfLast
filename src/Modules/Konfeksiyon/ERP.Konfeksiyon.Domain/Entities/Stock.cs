using ERP.Core.Domain.Common;
using ERP.Core.Domain.Enums;
using ERP.Konfeksiyon.Domain.Enums;

namespace ERP.Konfeksiyon.Domain.Entities;

public class Warehouse : SoftDeletableEntity, IActivatable
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public WarehouseType Type { get; set; } = WarehouseType.General;
    public bool IsActive { get; set; } = true;

    public ICollection<WarehouseLocation> Locations { get; } = new List<WarehouseLocation>();
}

/// <summary>
/// Depo adresi: stoğun depo içinde durduğu yer (bölge / raf / göz). Her deponun bir genel adresi vardır;
/// adres belirtilmeyen hareketler oraya yazılır.
/// </summary>
public class WarehouseLocation : SoftDeletableEntity, IActivatable
{
    /// <summary>Genel adresin, depo açılırken verilen kodu.</summary>
    public const string DefaultCode = "GENEL";

    public int WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;

    /// <summary>Bölge, raf ve gözden oluşur (örn. A-01-03); depo içinde benzersizdir.</summary>
    public string Code { get; private set; } = null!;

    public string Zone { get; private set; } = null!;
    public string? Rack { get; private set; }
    public string? Bin { get; private set; }

    public string? Description { get; set; }

    /// <summary>Deponun genel adresi: silinemez ve pasif yapılamaz.</summary>
    public bool IsDefault { get; private set; }

    public bool IsActive { get; set; } = true;

    /// <summary>Okutulan metin bir adres barkoduysa adresin kimliğini verir.</summary>
    public static bool TryParseBarcode(string text, out int id)
    {
        id = 0;
        return text.Length == BarcodePrefix.Length + 8
            && text.StartsWith(BarcodePrefix, StringComparison.OrdinalIgnoreCase)
            && text.AsSpan(BarcodePrefix.Length).IndexOfAnyExceptInRange('0', '9') < 0
            && int.TryParse(text.AsSpan(BarcodePrefix.Length), out id)
            && id > 0;
    }

    private const string BarcodePrefix = "ADR";

    public static WarehouseLocation CreateDefault()
    {
        var location = new WarehouseLocation { IsDefault = true, Description = "Adres belirtilmeyen stok" };
        location.SetAddress(DefaultCode, null, null);
        return location;
    }

    /// <summary>Adres kodu: dolu seviyelerin tire ile birleşimi.</summary>
    public static string ComposeCode(string zone, string? rack, string? bin) =>
        string.Join('-', new[] { zone, rack, bin }.Where(part => !string.IsNullOrEmpty(part)));

    /// <summary>Seviyeleri ve onlardan oluşan kodu yazar. Göz, rafı olmayan adreste verilemez.</summary>
    public void SetAddress(string zone, string? rack, string? bin)
    {
        if (string.IsNullOrEmpty(zone))
        {
            throw new ArgumentException("Bölge zorunludur.", nameof(zone));
        }

        if (string.IsNullOrEmpty(rack) && !string.IsNullOrEmpty(bin))
        {
            throw new ArgumentException("Raf belirtilmeden göz verilemez.", nameof(bin));
        }

        Zone = zone;
        Rack = string.IsNullOrEmpty(rack) ? null : rack;
        Bin = string.IsNullOrEmpty(bin) ? null : bin;
        Code = ComposeCode(zone, Rack, Bin);
    }
}

/// <summary>
/// Stok kartı: stokta izlenen her şey. Malzemeler (kumaş, aksesuar) kullanıcı tarafından tanımlanır;
/// mamul kartları (ürün + renk + beden) üretimden ilk girişte sistem tarafından açılır.
/// </summary>
public class StockItem : SoftDeletableEntity, IActivatable
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public StockItemType ItemType { get; set; }
    public StockUnit Unit { get; set; }

    // Yalnızca mamul kartlarında doludur.
    public int? ProductId { get; set; }
    public Product? Product { get; set; }
    public int? ColorId { get; set; }
    public Color? Color { get; set; }
    public int? SizeId { get; set; }
    public Size? Size { get; set; }

    /// <summary>
    /// Ortalama birim maliyet (sistem para biriminde). Fiyatlı mal kabulde ağırlıklı ortalama ile güncellenir;
    /// elle de girilebilir. Bilinmiyorsa boştur.
    /// </summary>
    public decimal? UnitCost { get; set; }

    /// <summary>
    /// Top / parti takipli kart: her giriş bir <see cref="StockLot"/> açar ve stok top bazında izlenir.
    /// Kumaşta zorunludur, mamulde yoktur; hareket görmüş kartta değiştirilemez.
    /// </summary>
    public bool IsLotTracked { get; set; }

    /// <summary>Asgari stok: tüm depolardaki toplam bu miktarın altına düşmemelidir; boşsa izlenmez.</summary>
    public decimal? MinimumStock { get; set; }

    /// <summary>Sipariş katı: tedarik önerisinde bu miktardan azı önerilmez (asgari sipariş miktarı).</summary>
    public decimal? ReorderQuantity { get; set; }

    /// <summary>Kartın genellikle alındığı tedarikçi; tedarik önerisinden açılan sipariş ona yazılır.</summary>
    public int? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsFinishedGood => ItemType == StockItemType.FinishedGood;

    /// <summary>
    /// Eksik: açık ihtiyaç ve asgari stok, eldeki stok ile beklenen girişlerden (yoldaki ve taslaktaki satın alma)
    /// fazlaysa aradaki fark.
    /// </summary>
    public decimal ShortageFor(decimal onHand, decimal incoming, decimal demand) =>
        Math.Max(demand + (MinimumStock ?? 0) - onHand - incoming, 0);

    /// <summary>Eksik için önerilen sipariş miktarı: sipariş katından az olmaz, adetle izlenen kartta tam sayıdır.</summary>
    public decimal SuggestedOrderQuantity(decimal shortage)
    {
        if (shortage <= 0)
        {
            return 0;
        }

        var quantity = Math.Max(shortage, ReorderQuantity ?? 0);
        return Unit == StockUnit.Piece ? decimal.Ceiling(quantity) : quantity;
    }

    /// <summary>Kart türü için geçerli takip biçimi: kumaş her zaman takiplidir, mamul hiçbir zaman.</summary>
    public static bool LotTrackingFor(StockItemType itemType, bool requested) => itemType switch
    {
        StockItemType.Fabric => true,
        StockItemType.FinishedGood => false,
        _ => requested,
    };

    /// <summary>Miktar bu kartın birimi için geçerli mi (adetle izlenen kart kesirli miktar almaz).</summary>
    public bool Accepts(decimal quantity) => Unit != StockUnit.Piece || quantity == decimal.Truncate(quantity);
}

/// <summary>
/// Top / parti: takipli bir kartın tek girişte gelen, ayrı izlenen miktarı (kumaş topu, aksesuar partisi).
/// Miktarı ve yeri <see cref="Stock"/> satırlarındadır; burada topun kimliği ve özellikleri durur.
/// </summary>
public class StockLot : AuditableEntity
{
    public int StockItemId { get; set; }
    public StockItem StockItem { get; set; } = null!;

    /// <summary>Sistem tarafından verilen, etikete basılan numara (örn. T00000123); veritabanında hesaplanır.</summary>
    public string LotNo { get; private set; } = null!;

    /// <summary>Tedarikçinin parti / lot numarası.</summary>
    public string? BatchNo { get; set; }

    /// <summary>Renk tonu (aynı partide toplar arasında fark olabilir).</summary>
    public string? Shade { get; set; }

    /// <summary>Kumaş eni (cm).</summary>
    public decimal? Width { get; set; }

    /// <summary>Tedarikçinin top numarası.</summary>
    public string? SupplierRollNo { get; set; }

    /// <summary>Topun giriş miktarı (kartın biriminde).</summary>
    public decimal InitialQuantity { get; set; }

    /// <summary>Sunucu saati.</summary>
    public DateTime ReceivedAt { get; set; }

    /// <summary>Top bir satın alma siparişinden geldiyse o sipariş.</summary>
    public int? PurchaseOrderId { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }
}

/// <summary>
/// Bir kartın bir depo adresindeki (takipli kartta: bir topun o adresteki) bakiyesi; adresin bakiyesi
/// toplarının, deponun bakiyesi adreslerinin toplamıdır. Bakiye yalnızca stok hareketiyle değişir; her
/// değişikliğin karşılığında bir <see cref="StockTransaction"/> vardır.
/// </summary>
[NotAudited]
public class Stock
{
    public int Id { get; set; }

    public int WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;

    /// <summary>Stoğun durduğu adres; <see cref="WarehouseId"/> deposuna aittir.</summary>
    public int LocationId { get; set; }
    public WarehouseLocation Location { get; set; } = null!;

    public int StockItemId { get; set; }
    public StockItem StockItem { get; set; } = null!;

    /// <summary>Takipli kartta bakiyenin ait olduğu top; takipsiz kartta boş.</summary>
    public int? LotId { get; set; }
    public StockLot? Lot { get; set; }

    public decimal Quantity { get; private set; }

    /// <summary>Aynı bakiyenin iki işlem tarafından eşzamanlı değiştirilmesini engeller.</summary>
    public byte[] RowVersion { get; set; } = null!;

    /// <summary>Bu değişiklik bakiyeyi eksiye düşürür mü.</summary>
    public bool CanApply(decimal delta) => Quantity + delta >= 0;

    /// <summary>Bakiyeyi değiştirir ve yeni bakiyeyi döndürür. Stok eksiye düşemez.</summary>
    public decimal Apply(decimal delta)
    {
        if (!CanApply(delta))
        {
            throw new InvalidOperationException("Stok eksiye düşemez.");
        }

        Quantity += delta;
        return Quantity;
    }
}

/// <summary>
/// Rezervasyon: bir üretim siparişinin reçete malzemesi için bir depoda ayrılmış miktar. Stok depoda durur;
/// ayrılan miktar başka siparişlerce ve elle çıkışlarla kullanılamaz. Sipariş için malzeme çıkıldıkça azalır.
/// </summary>
[NotAudited]
public class StockReservation
{
    public int Id { get; set; }

    public int ProductionOrderId { get; set; }
    public ProductionOrder ProductionOrder { get; set; } = null!;

    public int WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;

    public int StockItemId { get; set; }
    public StockItem StockItem { get; set; } = null!;

    /// <summary>Hâlâ ayrılmış duran miktar (kartın biriminde); sıfıra inen rezervasyon silinir.</summary>
    public decimal Quantity { get; private set; }

    /// <summary>Sunucu saati.</summary>
    public DateTime UpdatedAt { get; set; }
    public string UpdatedBy { get; set; } = null!;

    public void SetQuantity(decimal quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Ayrılan miktar sıfırdan büyük olmalıdır.");
        }

        Quantity = quantity;
    }

    /// <summary>Sipariş için çıkılan miktarı rezervden düşer (rezervden fazlası çıkılmış olabilir); kalanı döner.</summary>
    public decimal Consume(decimal issued)
    {
        if (issued <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(issued), "Çıkılan miktar sıfırdan büyük olmalıdır.");
        }

        Quantity = Math.Max(Quantity - issued, 0);
        return Quantity;
    }
}

/// <summary>
/// Stok hareketi. Yalnızca eklenir; düzeltme ters yönlü yeni bir hareketle yapılır. Transfer ve adres
/// değişimi, aynı istek kimliğini taşıyan bir çıkış ve bir giriş satırıdır.
/// </summary>
[NotAudited]
public class StockTransaction
{
    public long Id { get; set; }

    /// <summary>İstemcinin ürettiği istek kimliği; aynı istek yeniden gelirse hareket ikinci kez yazılmaz.</summary>
    public Guid RequestId { get; set; }

    public StockTransactionType Type { get; set; }

    public int WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;

    /// <summary>Bakiyesi değişen adres.</summary>
    public int LocationId { get; set; }
    public WarehouseLocation Location { get; set; } = null!;

    public int StockItemId { get; set; }
    public StockItem StockItem { get; set; } = null!;

    /// <summary>Bakiyedeki değişim: girişte artı, çıkışta eksi.</summary>
    public decimal Quantity { get; set; }

    /// <summary>Hareketten sonra kartın bu depodaki bakiyesi (adres değişiminde depo bakiyesi değişmez).</summary>
    public decimal BalanceAfter { get; set; }

    /// <summary>Hareketten sonra kartın bu adresteki bakiyesi.</summary>
    public decimal LocationBalanceAfter { get; set; }

    /// <summary>Takipli kartta hareketin ait olduğu top.</summary>
    public int? LotId { get; set; }
    public StockLot? Lot { get; set; }

    /// <summary>Hareketten sonra topun bu adresteki bakiyesi; takipsiz kartta boş.</summary>
    public decimal? LotBalanceAfter { get; set; }

    /// <summary>Hareket anındaki birim maliyet (mal kabulde alış fiyatı, üretime çıkışta ortalama maliyet); bilinmiyorsa boş.</summary>
    public decimal? UnitCost { get; set; }

    /// <summary>Transferde karşı depo.</summary>
    public int? CounterWarehouseId { get; set; }
    public Warehouse? CounterWarehouse { get; set; }

    /// <summary>Transferde ve adres değişiminde karşı adres.</summary>
    public int? CounterLocationId { get; set; }
    public WarehouseLocation? CounterLocation { get; set; }

    /// <summary>Üretimden girişte teslim alınan demet.</summary>
    public int? BundleId { get; set; }
    public Bundle? Bundle { get; set; }

    /// <summary>Hareketin ilgili olduğu üretim siparişi (üretimden giriş, üretime malzeme çıkışı).</summary>
    public int? ProductionOrderId { get; set; }
    public ProductionOrder? ProductionOrder { get; set; }

    /// <summary>Mal kabulde ilgili satın alma siparişi.</summary>
    public int? PurchaseOrderId { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }

    /// <summary>Sevkiyat çıkışında ilgili sevkiyat.</summary>
    public int? ShipmentId { get; set; }
    public Shipment? Shipment { get; set; }

    /// <summary>İrsaliye, fiş vb. belge numarası.</summary>
    public string? DocumentNo { get; set; }
    public string? Note { get; set; }

    /// <summary>Sunucu saati.</summary>
    public DateTime OccurredAt { get; set; }
    public string CreatedBy { get; set; } = null!;
}
