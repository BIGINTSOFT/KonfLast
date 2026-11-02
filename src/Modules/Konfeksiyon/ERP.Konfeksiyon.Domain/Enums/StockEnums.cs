namespace ERP.Konfeksiyon.Domain.Enums;

/// <summary>Deponun kullanım amacı (bilgi amaçlıdır; hangi kartın hangi depoda duracağını kısıtlamaz).</summary>
public enum WarehouseType
{
    RawMaterial,
    FinishedGoods,
    General,
}

public enum StockItemType
{
    Fabric,
    Accessory,

    /// <summary>Mamul: bir ürünün tek renk ve bedeni. Sistem tarafından açılır.</summary>
    FinishedGood,
    Other,
}

public enum StockTransactionType
{
    /// <summary>Depoya giriş (satın alma, iade vb.).</summary>
    Receipt,

    /// <summary>Depodan çıkış (üretime malzeme, fire vb.).</summary>
    Issue,
    TransferOut,
    TransferIn,

    /// <summary>Sayım düzeltmesi; miktar artı ya da eksi olabilir.</summary>
    Adjustment,

    /// <summary>Satın alma siparişinden mal kabul.</summary>
    PurchaseReceipt,

    /// <summary>Müşteriye sevkiyat (depodan çıkış).</summary>
    Shipment,

    /// <summary>Tamamlanan demetin mamul olarak depoya girişi.</summary>
    ProductionReceipt,

    /// <summary>Demet açılırken reçeteye göre üretime malzeme çıkışı.</summary>
    ProductionIssue,

    /// <summary>İptal edilen demetin malzemesinin depoya geri girişi.</summary>
    ProductionReturn,

    /// <summary>Aynı depo içinde adres değişimi: eski adresten çıkış.</summary>
    RelocationOut,

    /// <summary>Aynı depo içinde adres değişimi: yeni adrese giriş.</summary>
    RelocationIn,
}

public enum PurchaseOrderStatus
{
    Draft,

    /// <summary>Onaylandı; tedarikçiye verildi, mal kabul bekleniyor.</summary>
    Approved,
    PartiallyReceived,

    /// <summary>Tüm kalemler eksiksiz teslim alındı.</summary>
    Received,

    /// <summary>Eksik kalan miktar beklenmeden kapatıldı.</summary>
    Closed,
    Cancelled,
}

public enum StockCountStatus
{
    /// <summary>Sayım sürüyor; kapsamdaki adreslerde stok hareketi yapılamaz.</summary>
    Open,

    /// <summary>Onaylandı; farklar stoğa işlendi.</summary>
    Posted,
    Cancelled,
}

public enum SalesOrderStatus
{
    /// <summary>Hazırlanıyor; düzenlenebilir ve silinebilir.</summary>
    Draft,

    /// <summary>Onaya gönderildi; içeriği kilitlidir, onay ya da ret bekler.</summary>
    Submitted,

    /// <summary>Onaylandı; üretim siparişlerine aktarılabilir.</summary>
    Approved,

    /// <summary>İş bitti; sipariş kapatıldı.</summary>
    Closed,
    Cancelled,
}

public enum ShipmentStatus
{
    Draft,

    /// <summary>Sevk edildi; stok düştü.</summary>
    Shipped,
    Cancelled,
}
