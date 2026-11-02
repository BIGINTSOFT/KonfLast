namespace ERP.Dokuma.Domain.Enums;

/// <summary>Malzeme kartının türü: haşıl kimyasalı, yedek parça, sarf malzemesi ya da diğer.</summary>
public enum MaterialType
{
    /// <summary>Haşıl reçetesinde kullanılan kimyasal (nişasta, PVA, mum ...).</summary>
    SizingChemical,

    /// <summary>Tezgâh ve makine yedek parçası.</summary>
    SparePart,

    /// <summary>Sarf malzemesi (yağ, bıçak, eldiven ...).</summary>
    Consumable,

    Other,
}

/// <summary>Malzemenin stok birimi.</summary>
public enum MaterialUnit
{
    Kilogram,
    Piece,
    Liter,
    Meter,
}

/// <summary>Malzeme stok hareketinin türü. Girişte miktar artı, çıkışta eksidir.</summary>
public enum MaterialMovementType
{
    /// <summary>Lot girişi (elle ya da satın alma teslimi).</summary>
    Receipt,

    /// <summary>Tezgâha ya da bölüme çıkış.</summary>
    Issue,

    /// <summary>İçeride haşılda leventin tükettiği kimyasal.</summary>
    SizingConsumption,

    /// <summary>Tek lotun miktar düzeltmesi.</summary>
    Adjustment,

    /// <summary>Malzeme sayımı: sayılan toplam ile stok arasındaki fark.</summary>
    Count,
}

/// <summary>Satın alma siparişinin durumu.</summary>
public enum PurchaseOrderStatus
{
    Draft,
    Approved,
    PartiallyReceived,
    Received,

    /// <summary>Kısmi teslimden sonra kalan artık beklenmiyor.</summary>
    Closed,

    Cancelled,
}
