using ERP.Core.Application.Common;

namespace ERP.Konfeksiyon.Application.Features.Reports;

/// <summary>Sütundaki değerlerin türü; gösterim ve dışa aktarma biçimini belirler.</summary>
public enum ReportColumnType
{
    Text,
    Integer,
    Decimal,

    /// <summary>0-100 ölçeğinde yüzde (ör. 107,8).</summary>
    Percent,

    /// <summary>Dakika cinsinden süre.</summary>
    Minutes,

    /// <summary>UTC an; gösterilirken yerel saate çevrilir.</summary>
    DateTime,

    /// <summary>Fabrikanın yerel günü.</summary>
    Date,
}

/// <param name="Financial">Parasal sütun (stok değeri, fark tutarı): yalnızca parasal rapor yetkisiyle görünür.</param>
public sealed record ReportColumn(
    string Key, string Title, ReportColumnType Type = ReportColumnType.Text, bool Financial = false);

/// <summary>Raporun ürettiği veri: sütunlar ve sütun sırasıyla hücre değerleri.</summary>
/// <param name="Totals">Toplam satırı (sütun sırasıyla; toplamı olmayan hücre null).</param>
/// <param name="Truncated">Satır sınırı aşıldı; yalnızca ilk <see cref="ReportLimits.MaxRows"/> satır döndü.</param>
public sealed record ReportData(
    IReadOnlyList<ReportColumn> Columns,
    IReadOnlyList<IReadOnlyList<object?>> Rows,
    IReadOnlyList<object?>? Totals = null,
    bool Truncated = false);

/// <summary>
/// Çalıştırılmış rapor. Çizimden bağımsızdır: web tabloyu gösterir, dışa aktarıcılar dosyaya çevirir;
/// ileride bir rapor tasarım aracı (ör. FastReport) da aynı tabloyu veri kaynağı olarak kullanabilir.
/// </summary>
public sealed record ReportTable(
    string Key,
    string Title,
    DateTime GeneratedAt,
    DateTime? From,
    DateTime? To,
    IReadOnlyList<ReportColumn> Columns,
    IReadOnlyList<IReadOnlyList<object?>> Rows,
    IReadOnlyList<object?>? Totals,
    bool Truncated);

/// <param name="RequiresDateRange">Rapor bir tarih aralığı için mi çalışır (false: anlık durum).</param>
/// <param name="MaxRangeDays">Tarih aralığının en fazla uzunluğu.</param>
/// <param name="SearchLabel">Rapor metin araması destekliyorsa arama kutusunun açıklaması; yoksa null.</param>
/// <param name="Group">Raporlar ekranında raporun listelendiği grup (ör. "Depo"); verilmezse genel raporlar arasındadır.</param>
/// <param name="Permission">Rapor yetkisine ek olarak gereken yetki (ör. personel raporları); yoksa null.</param>
/// <param name="DaysLabel">Rapor bir gün sayısı istiyorsa (ör. "son N günde hareket görmemiş") kutunun açıklaması; yoksa null.</param>
/// <param name="DefaultDays">Gün sayısı verilmezse kullanılan değer.</param>
public sealed record ReportDefinition(
    string Key,
    string Title,
    string Description,
    bool RequiresDateRange = true,
    int MaxRangeDays = 31,
    string? SearchLabel = null,
    string? Group = null,
    string? Permission = null,
    string? DaysLabel = null,
    int? DefaultDays = null);

/// <summary>Raporlar ekranındaki grup adları (depo ve fason gruplarının adı kendi özelliklerindedir).</summary>
public static class ReportGroups
{
    public const string Production = "Üretim";
    public const string Personnel = "Personel";
    public const string Machine = "Makine";
    public const string Quality = "Kalite";
}

public sealed class ReportQuery
{
    /// <summary>Aralığın başı (UTC, dahil).</summary>
    public DateTime? From { get; set; }

    /// <summary>Aralığın sonu (UTC, hariç).</summary>
    public DateTime? To { get; set; }

    public string? Search { get; set; }

    /// <summary>Gün sayısı isteyen raporlarda gün sayısı; boşsa raporun varsayılanı.</summary>
    public int? Days { get; set; }
}

public static class ReportDayLimits
{
    public const int Min = 1;
    public const int Max = 3650;
}

/// <summary>Doğrulanmış rapor parametreleri. Tarih aralığı istemeyen raporda From / To varsayılan değerdedir.</summary>
/// <param name="Days">Gün sayısı isteyen raporda doğrulanmış gün sayısı; diğerlerinde null.</param>
public sealed record ReportParameters(DateTime From, DateTime To, string? Search, int? Days = null);

public static class ReportLimits
{
    /// <summary>Liste türü raporların döndürdüğü en fazla satır.</summary>
    public const int MaxRows = 5000;
}

public interface IReport
{
    ReportDefinition Definition { get; }

    Task<Result<ReportData>> RunAsync(ReportParameters parameters, CancellationToken cancellationToken);
}

public interface IReportService
{
    /// <summary>
    /// Kullanıcının çalıştırabileceği raporlar: rapor yetkisi olan hepsini, yalnızca depo raporu yetkisi olan
    /// depo grubundakileri görür.
    /// </summary>
    Task<IReadOnlyList<ReportDefinition>> GetCatalogAsync(CancellationToken cancellationToken);

    Task<Result<ReportTable>> RunAsync(string key, ReportQuery query, CancellationToken cancellationToken);
}

/// <summary>Rapor tablosunu bir dosya biçimine çevirir. Yeni biçimler (xlsx, pdf) bu arayüzle eklenir.</summary>
public interface IReportExporter
{
    /// <summary>İstekteki biçim adı, örn. "csv".</summary>
    string Format { get; }
    string ContentType { get; }
    string FileExtension { get; }

    byte[] Export(ReportTable table);
}
