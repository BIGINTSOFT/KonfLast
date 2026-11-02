using System.Globalization;
using System.Text;
using ERP.Konfeksiyon.Application.Features.Shifts;

namespace ERP.Konfeksiyon.Application.Features.Reports;

/// <summary>
/// Raporu CSV'ye çevirir. Excel'in Türkçe bölge ayarlarıyla doğrudan açılsın diye ayırıcı noktalı virgül,
/// ondalık ayırıcı virgüldür; dosya UTF-8 BOM ile başlar. Zamanlar fabrikanın yerel saatiyle yazılır.
/// </summary>
public sealed class CsvReportExporter(IFactoryClock clock) : IReportExporter
{
    private const char Separator = ';';
    private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");

    public string Format => "csv";
    public string ContentType => "text/csv; charset=utf-8";
    public string FileExtension => "csv";

    public byte[] Export(ReportTable table)
    {
        var text = new StringBuilder();
        AppendRow(text, table.Columns.Select(c => c.Title));

        foreach (var row in table.Rows)
        {
            AppendRow(text, row.Select((value, index) => Cell(value, table.Columns[index].Type)));
        }

        if (table.Totals is not null)
        {
            AppendRow(text, table.Totals.Select((value, index) => Cell(value, table.Columns[index].Type)));
        }

        return [.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes(text.ToString())];
    }

    private string Cell(object? value, ReportColumnType type) => value switch
    {
        null => "",
        DateTime at when type == ReportColumnType.DateTime => clock.ToLocal(at).ToString("dd.MM.yyyy HH:mm:ss", Turkish),
        DateTime at => at.ToString("dd.MM.yyyy", Turkish),
        DateOnly day => day.ToString("dd.MM.yyyy", Turkish),
        // Binlik ayırıcı yazılmaz: Excel sayıyı metin sanmasın.
        IFormattable number => number.ToString(type == ReportColumnType.Integer ? "0" : "0.###", Turkish),
        _ => value.ToString() ?? "",
    };

    private static void AppendRow(StringBuilder text, IEnumerable<string> cells) =>
        text.AppendJoin(Separator, cells.Select(Escape)).Append("\r\n");

    private static string Escape(string cell)
    {
        // Formül enjeksiyonu: = + - @ ile başlayan metin Excel'de formül olarak çalışmasın.
        if (cell.Length > 0 && cell[0] is '=' or '+' or '@' or '\t' or '\r'
            || (cell.Length > 1 && cell[0] == '-' && !char.IsDigit(cell[1])))
        {
            cell = "'" + cell;
        }

        return cell.Contains(Separator) || cell.Contains('"') || cell.Contains('\n') || cell.Contains('\r')
            ? $"\"{cell.Replace("\"", "\"\"")}\""
            : cell;
    }
}
