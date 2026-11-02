using ERP.Core.Domain.Common;

namespace ERP.Konfeksiyon.Domain.Entities;

/// <summary>
/// Asorti şablonu: bir kolinin beden dağılımı (ör. S:1, M:2, L:2, XL:1 → 6 adetlik koli). Müşteri seçilmişse o
/// müşteriye özeldir, seçilmemişse genel şablondur. Koliler tek renklidir: her koliye bir ürünün tek bir rengi girer.
/// </summary>
public class AssortmentTemplate : SoftDeletableEntity, IActivatable
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;

    /// <summary>Boşsa genel şablon (tüm müşteriler).</summary>
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<AssortmentTemplateLine> Lines { get; } = new List<AssortmentTemplateLine>();

    /// <summary>Bir koliye giren toplam adet: oranların toplamı (saklanmaz).</summary>
    public int PiecesPerCarton => Lines.Sum(l => l.Ratio);
}

/// <summary>Şablonda bir bedenin kolideki adedi (oran).</summary>
public class AssortmentTemplateLine : BaseEntity
{
    public int AssortmentTemplateId { get; set; }
    public AssortmentTemplate AssortmentTemplate { get; set; } = null!;

    public int SizeId { get; set; }
    public Size Size { get; set; } = null!;

    /// <summary>Bir kolideki adet (sıfırdan büyük tam sayı).</summary>
    public int Ratio { get; set; }
}

/// <summary>
/// Sevkiyatın kolisi (çeki listesi satırı). Koli numarası sevkiyat içinde 1'den başlayarak sıralıdır; etikette
/// <see cref="CodeOf"/> ile üretilen koli kodu barkod olarak basılır. Yükleme okutulduğunda yükleme anı yazılır.
/// </summary>
public class ShipmentCarton : BaseEntity
{
    public int ShipmentId { get; set; }
    public Shipment Shipment { get; set; } = null!;

    /// <summary>Sevkiyat içindeki sıra numarası (1, 2, 3 …).</summary>
    public int CartonNo { get; private set; }

    /// <summary>Koli bir şablonla dolduruldu ise o şablon; boşsa karışık (artan ya da elle girilmiş) kolidir.</summary>
    public int? AssortmentTemplateId { get; set; }
    public AssortmentTemplate? AssortmentTemplate { get; set; }

    public decimal? NetKg { get; set; }
    public decimal? GrossKg { get; set; }

    /// <summary>Kolinin araca yüklendiği okutma anı (sunucu saati); okutulmadıysa boş.</summary>
    public DateTime? LoadedAt { get; private set; }
    public string? LoadedBy { get; private set; }

    public ICollection<ShipmentCartonLine> Lines { get; } = new List<ShipmentCartonLine>();

    public bool IsLoaded => LoadedAt is not null;

    /// <summary>Koli numarasını verir. Numara değişirse etiket de değişir: önceki yükleme okutması geçersiz olur.</summary>
    public void Renumber(int cartonNo)
    {
        if (cartonNo < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(cartonNo), "Koli numarası 1'den küçük olamaz.");
        }

        if (CartonNo != 0 && CartonNo != cartonNo)
        {
            LoadedAt = null;
            LoadedBy = null;
        }

        CartonNo = cartonNo;
    }

    /// <summary>Yüklemeyi kaydeder; zaten yüklenmişse ilk okutma korunur. Yeni yüklendiyse true döner.</summary>
    public bool MarkLoaded(DateTime utcNow, string user)
    {
        if (IsLoaded)
        {
            return false;
        }

        LoadedAt = utcNow;
        LoadedBy = user;
        return true;
    }

    /// <summary>Etikete basılan koli kodu: {sevkiyat no}-{koli no, üç hane}, örn. SV-2026-00031-007.</summary>
    public static string CodeOf(string shipmentNo, int cartonNo) => $"{shipmentNo}-{cartonNo:000}";

    /// <summary>Okutulan metin bir koli koduysa sevkiyat numarasını ve koli numarasını ayırır.</summary>
    public static bool TryParseCode(string text, out string shipmentNo, out int cartonNo)
    {
        shipmentNo = string.Empty;
        cartonNo = 0;
        var trimmed = text.Trim();
        var separator = trimmed.LastIndexOf('-');
        if (separator <= 0 || separator == trimmed.Length - 1)
        {
            return false;
        }

        var number = trimmed.AsSpan(separator + 1);
        if (number.IndexOfAnyExceptInRange('0', '9') >= 0 || !int.TryParse(number, out cartonNo) || cartonNo < 1)
        {
            cartonNo = 0;
            return false;
        }

        shipmentNo = trimmed[..separator].ToUpperInvariant();
        return true;
    }
}

/// <summary>Kolideki bir mamul kartının (ürün + renk + beden) adedi.</summary>
public class ShipmentCartonLine : BaseEntity
{
    public int ShipmentCartonId { get; set; }
    public ShipmentCarton Carton { get; set; } = null!;

    public int StockItemId { get; set; }
    public StockItem StockItem { get; set; } = null!;

    public decimal Quantity { get; set; }
}
