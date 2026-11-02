using System.Globalization;
using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Konfeksiyon.Application.Features.Shifts;
using KP = ERP.Konfeksiyon.Domain.Authorization.KonfeksiyonPermissions;

namespace ERP.Konfeksiyon.Application.Features.Reports;

/// <summary>Raporları anahtarıyla bulur, parametrelerini doğrular ve çalıştırır.</summary>
public sealed class ReportService(
    IEnumerable<IReport> reports, IFactoryClock clock, ICurrentUser currentUser, IPermissionService permissions) : IReportService
{
    private readonly IReadOnlyList<IReport> _reports = reports.ToList();

    private static readonly StringComparer TitleOrder = StringComparer.Create(CultureInfo.GetCultureInfo("tr-TR"), ignoreCase: true);

    public async Task<IReadOnlyList<ReportDefinition>> GetCatalogAsync(CancellationToken cancellationToken)
    {
        var granted = await GrantedAsync(cancellationToken);
        var all = granted.Contains(KP.Report.View);
        return _reports.Select(r => r.Definition)
            .Where(d => (all || IsWarehouseReport(d)) && (d.Permission is null || granted.Contains(d.Permission)))
            .OrderBy(d => d.Title, TitleOrder)
            .ToList();
    }

    private Task<IReadOnlySet<string>> GrantedAsync(CancellationToken cancellationToken) =>
        permissions.GetPermissionsAsync(currentUser.Roles, cancellationToken);

    private async Task<bool> CanSeeAllAsync(CancellationToken cancellationToken) =>
        (await GrantedAsync(cancellationToken)).Contains(KP.Report.View);

    private static bool IsWarehouseReport(ReportDefinition definition) => definition.Group == StockLabels.Group;

    public async Task<Result<ReportTable>> RunAsync(string key, ReportQuery query, CancellationToken cancellationToken)
    {
        var report = _reports.FirstOrDefault(r => string.Equals(r.Definition.Key, key, StringComparison.OrdinalIgnoreCase));
        if (report is null)
        {
            return new Error(ErrorCodes.NotFound, "Rapor bulunamadı.", ErrorType.NotFound);
        }

        var definition = report.Definition;
        if (!IsWarehouseReport(definition) && !await CanSeeAllAsync(cancellationToken))
        {
            return new Error(ErrorCodes.Forbidden, "Bu raporu görüntüleme yetkiniz yok.", ErrorType.Forbidden);
        }

        var granted = await GrantedAsync(cancellationToken);
        if (definition.Permission is not null && !granted.Contains(definition.Permission))
        {
            return new Error(ErrorCodes.Forbidden, "Bu raporu görüntüleme yetkiniz yok.", ErrorType.Forbidden);
        }

        DateTime from = default, to = default;
        if (definition.RequiresDateRange)
        {
            if (query.From is null || query.To is null || query.To <= query.From)
            {
                return RangeError("Geçerli bir tarih aralığı verin: bitiş, başlangıçtan sonra olmalıdır.");
            }

            from = DateTime.SpecifyKind(query.From.Value.ToUniversalTime(), DateTimeKind.Utc);
            to = DateTime.SpecifyKind(query.To.Value.ToUniversalTime(), DateTimeKind.Utc);
            if ((to - from).TotalDays > definition.MaxRangeDays)
            {
                return RangeError($"Bu rapor için tarih aralığı en fazla {definition.MaxRangeDays} gün olabilir.");
            }
        }

        int? days = null;
        if (definition.DaysLabel is not null)
        {
            days = query.Days ?? definition.DefaultDays ?? ReportDayLimits.Min;
            if (days is < ReportDayLimits.Min or > ReportDayLimits.Max)
            {
                var message = $"Gün sayısı {ReportDayLimits.Min} ile {ReportDayLimits.Max} arasında olmalıdır.";
                return new Error(
                    ErrorCodes.ValidationError, message, ErrorType.Validation,
                    new Dictionary<string, string[]> { ["days"] = [message] });
            }
        }

        var search = definition.SearchLabel is null || string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();
        var result = await report.RunAsync(new ReportParameters(from, to, search, days), cancellationToken);
        if (!result.IsSuccess)
        {
            return result.Error!;
        }

        // Parasal sütunlar (stok değeri, fark tutarı) yalnızca parasal rapor yetkisiyle görünür; yoksa sütun çıkarılır.
        var data = granted.Contains(KP.Report.Financial) ? result.Value : WithoutFinancialColumns(result.Value);
        return new ReportTable(
            definition.Key,
            definition.Title,
            clock.UtcNow,
            definition.RequiresDateRange ? from : null,
            definition.RequiresDateRange ? to : null,
            data.Columns,
            data.Rows,
            data.Totals,
            data.Truncated);
    }

    private static ReportData WithoutFinancialColumns(ReportData data)
    {
        var keep = data.Columns.Select((column, index) => (column, index)).Where(c => !c.column.Financial).Select(c => c.index).ToList();
        if (keep.Count == data.Columns.Count)
        {
            return data;
        }

        IReadOnlyList<object?> Pick(IReadOnlyList<object?> row) => keep.Select(i => row[i]).ToList();
        return data with
        {
            Columns = keep.Select(i => data.Columns[i]).ToList(),
            Rows = data.Rows.Select(Pick).ToList(),
            Totals = data.Totals is null ? null : Pick(data.Totals),
        };
    }

    private static Error RangeError(string message) => new(
        ErrorCodes.ValidationError, message, ErrorType.Validation,
        new Dictionary<string, string[]> { ["from"] = [message] });
}

public static class ReportDays
{
    /// <summary>
    /// Aralığı fabrikanın yerel günlerine böler. İlk ve son gün, aralığın sınırlarıyla kırpılır.
    /// </summary>
    public static IEnumerable<(DateOnly Day, DateTime Start, DateTime End)> Split(IFactoryClock clock, DateTime from, DateTime to)
    {
        var day = clock.ToLocal(from).Date;
        while (true)
        {
            var start = clock.ToUtc(day);
            if (start >= to)
            {
                yield break;
            }

            var end = clock.ToUtc(day.AddDays(1));
            yield return (DateOnly.FromDateTime(day), start < from ? from : start, end > to ? to : end);
            day = day.AddDays(1);
        }
    }
}
