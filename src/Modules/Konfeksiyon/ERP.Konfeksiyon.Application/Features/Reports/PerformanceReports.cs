using ERP.Core.Application.Common;
using ERP.Konfeksiyon.Application.Features.ProductionTracking;
using ERP.Konfeksiyon.Application.Features.Shifts;
using static ERP.Konfeksiyon.Application.Features.Reports.ReportColumnType;

namespace ERP.Konfeksiyon.Application.Features.Reports;

/// <summary>
/// Performans raporları. Rakamlar Performans ekranıyla aynı hesaptan (<see cref="IPerformanceService"/>)
/// gelir; rapor yalnızca sütun seçer. Böylece ekran ile rapor hiçbir zaman farklı sayı göstermez.
/// </summary>
public abstract class PerformanceReportBase(IPerformanceService performance) : IReport
{
    public abstract ReportDefinition Definition { get; }

    protected abstract PerformanceGroupBy GroupBy { get; }
    protected abstract string RowTitle { get; }

    /// <summary>Kod ve ad sütunlarından sonraki sütunlar ve bir satırdan değerleri.</summary>
    protected abstract IReadOnlyList<(ReportColumn Column, Func<PerformanceRowDto, object?> Value)> Fields { get; }

    protected virtual IEnumerable<PerformanceRowDto> Order(IEnumerable<PerformanceRowDto> rows) => rows;

    public async Task<Result<ReportData>> RunAsync(ReportParameters parameters, CancellationToken cancellationToken)
    {
        var result = await performance.GetAsync(
            new PerformanceQuery { From = parameters.From, To = parameters.To, GroupBy = GroupBy }, cancellationToken);
        if (!result.IsSuccess)
        {
            return result.Error!;
        }

        var report = result.Value;
        return new ReportData(
            [new("code", "Kod"), new("name", RowTitle), .. Fields.Select(f => f.Column)],
            Order(report.Rows)
                .Select(row => (IReadOnlyList<object?>)[row.Code, row.Name, .. Fields.Select(f => f.Value(row))])
                .ToList(),
            report.Rows.Count == 0 ? null : ["Toplam", null, .. Fields.Select(f => f.Value(report.Total))]);
    }

    protected static (ReportColumn, Func<PerformanceRowDto, object?>) Field(
        string key, string title, ReportColumnType type, Func<PerformanceRowDto, object?> value) =>
        (new ReportColumn(key, title, type), value);

    /// <summary>Operatör ve makine raporlarının ortak sütunları: süre dökümü, üretim ve verim.</summary>
    protected static readonly IReadOnlyList<(ReportColumn, Func<PerformanceRowDto, object?>)> TimeAndOutput =
    [
        Field("presence", "Mevcudiyet", Minutes, r => r.PresenceMinutes),
        Field("break", "Planlı mola", Minutes, r => r.BreakMinutes),
        Field("pause", "Ara", Minutes, r => r.PauseMinutes),
        Field("breakdown", "Makine arızası", Minutes, r => r.BreakdownMinutes),
        Field("waiting", "Demet bekleme", Minutes, r => r.WaitingMinutes),
        Field("net", "Net çalışma", Minutes, r => r.NetWorkingMinutes),
        Field("active", "Aktif üretim", Minutes, r => r.ActiveMinutes),
        Field("produced", "Üretilen", Integer, r => r.Produced),
        Field("target", "Hedef", Integer, r => r.TargetQuantity),
        Field("rejected", "Hatalı", Integer, r => r.Rejected),
        Field("rejectRate", "Hata oranı", Percent, r => r.RejectRatePercent),
        Field("efficiency", "Verim", Percent, r => r.EfficiencyPercent),
        Field("utilization", "Kullanım", Percent, r => r.UtilizationPercent),
    ];
}

public sealed class OperatorPerformanceReport(IPerformanceService performance) : PerformanceReportBase(performance)
{
    public override ReportDefinition Definition { get; } = new(
        "operator-performance", "Operatör performansı",
        "Operatör bazında süre dökümü, üretim, hedef ve verim. Verimi en düşük olan üsttedir.",
        Permission: ERP.Konfeksiyon.Domain.Authorization.KonfeksiyonPermissions.Report.Personnel);

    protected override PerformanceGroupBy GroupBy => PerformanceGroupBy.Operator;
    protected override string RowTitle => "Operatör";
    protected override IReadOnlyList<(ReportColumn, Func<PerformanceRowDto, object?>)> Fields => TimeAndOutput;
}

public sealed class MachinePerformanceReport(IPerformanceService performance) : PerformanceReportBase(performance)
{
    public override ReportDefinition Definition { get; } = new(
        "machine-performance", "Makine performansı",
        "Makine bazında süre dökümü, üretim, hedef ve verim. Verimi en düşük olan üsttedir.");

    protected override PerformanceGroupBy GroupBy => PerformanceGroupBy.Machine;
    protected override string RowTitle => "Makine";
    protected override IReadOnlyList<(ReportColumn, Func<PerformanceRowDto, object?>)> Fields => TimeAndOutput;
}

public sealed class LineProductionReport(IPerformanceService performance) : PerformanceReportBase(performance)
{
    public override ReportDefinition Definition { get; } = new(
        "line-production", "Bant üretimi",
        "Bant bazında üretim, hedef, saatlik hız, verim ve kayıp süreler (arıza, demet bekleme, mola).");

    protected override PerformanceGroupBy GroupBy => PerformanceGroupBy.Line;
    protected override string RowTitle => "Bant";

    protected override IEnumerable<PerformanceRowDto> Order(IEnumerable<PerformanceRowDto> rows) =>
        rows.OrderBy(r => r.Code, StringComparer.Ordinal);

    protected override IReadOnlyList<(ReportColumn, Func<PerformanceRowDto, object?>)> Fields { get; } =
    [
        Field("produced", "Üretilen", Integer, r => r.Produced),
        Field("target", "Hedef", Integer, r => r.TargetQuantity),
        Field("rejected", "Hatalı", Integer, r => r.Rejected),
        Field("producedPerHour", "Gerçek / saat", ReportColumnType.Decimal, r => r.ProducedPerHour),
        Field("targetPerHour", "Hedef / saat", ReportColumnType.Decimal, r => r.TargetPerHour),
        Field("efficiency", "Verim", Percent, r => r.EfficiencyPercent),
        Field("breakdown", "Makine arızası", Minutes, r => r.BreakdownMinutes),
        Field("waiting", "Demet bekleme", Minutes, r => r.WaitingMinutes),
        Field("break", "Planlı mola", Minutes, r => r.BreakMinutes),
        Field("net", "Net çalışma", Minutes, r => r.NetWorkingMinutes),
    ];
}

public sealed class SamPerformanceReport(IPerformanceService performance) : PerformanceReportBase(performance)
{
    public override ReportDefinition Definition { get; } = new(
        "sam-performance", "SAM performansı",
        "Operasyon bazında standart süre (SAM) ile gerçekleşen birim sürenin karşılaştırması. Gerçekleşen süre, " +
        "operasyonun fiilen işlemde olduğu sürenin üretilen adede bölümüdür.");

    protected override PerformanceGroupBy GroupBy => PerformanceGroupBy.Operation;
    protected override string RowTitle => "Operasyon";

    protected override IReadOnlyList<(ReportColumn, Func<PerformanceRowDto, object?>)> Fields { get; } =
    [
        Field("produced", "Üretilen", Integer, r => r.Produced),
        Field("active", "Aktif üretim", Minutes, r => r.ActiveMinutes),
        Field("earned", "Kazanılan (adet × SAM)", Minutes, r => r.EarnedMinutes),
        Field("sam", "SAM (dk / adet)", ReportColumnType.Decimal, r => PerPiece(r.EarnedMinutes, r.Produced)),
        Field("actual", "Gerçekleşen (dk / adet)", ReportColumnType.Decimal, r => PerPiece(r.ActiveMinutes, r.Produced)),
        Field("efficiency", "Verim", Percent, r => r.EfficiencyPercent),
    ];

    private static double? PerPiece(double minutes, int produced) => produced > 0 ? Math.Round(minutes / produced, 3) : null;
}

/// <summary>Darboğaz: verimi düşük ve önünde iş biriken operasyonlar.</summary>
public sealed class BottleneckReport(IPerformanceService performance, IProductionTrackingService tracking) : IReport
{
    public ReportDefinition Definition { get; } = new(
        "bottlenecks", "Bant darboğazları",
        "Operasyonların verimi ve saatlik hızı ile şu an önlerinde bekleyen iş. Verimi en düşük operasyon üsttedir; " +
        "önünde iş biriken düşük verimli operasyon hattı yavaşlatan operasyondur.");

    public async Task<Result<ReportData>> RunAsync(ReportParameters parameters, CancellationToken cancellationToken)
    {
        var result = await performance.GetAsync(
            new PerformanceQuery { From = parameters.From, To = parameters.To, GroupBy = PerformanceGroupBy.Operation },
            cancellationToken);
        if (!result.IsSuccess)
        {
            return result.Error!;
        }

        // Bekleyen iş anlık durumdur (aralıktan bağımsız): operasyonun önündeki demetler.
        var wip = (await tracking.GetWipAsync(new WipQuery(), cancellationToken)).ToDictionary(w => w.OperationId);
        var rows = result.Value.Rows;

        // Aralıkta hiç çalışılmamış ama önünde iş bekleyen operasyon da darboğaz adayıdır.
        var idle = wip.Values.Where(w => rows.All(r => r.Id != w.OperationId));

        return new ReportData(
            [
                new("code", "Kod"),
                new("name", "Operasyon"),
                new("efficiency", "Verim", Percent),
                new("producedPerHour", "Gerçek / saat", ReportColumnType.Decimal),
                new("targetPerHour", "Hedef / saat", ReportColumnType.Decimal),
                new("produced", "Üretilen", Integer),
                new("waitingBundles", "Bekleyen demet (şu an)", Integer),
                new("waitingQuantity", "Bekleyen adet (şu an)", Integer),
            ],
            rows
                .Select(r =>
                {
                    var waiting = wip.GetValueOrDefault(r.Id);
                    return (IReadOnlyList<object?>)
                    [
                        r.Code, r.Name, r.EfficiencyPercent, r.ProducedPerHour, r.TargetPerHour, r.Produced,
                        (waiting?.ReadyBundles ?? 0) + (waiting?.PausedBundles ?? 0),
                        (waiting?.ReadyQuantity ?? 0) + (waiting?.PausedQuantity ?? 0),
                    ];
                })
                .Concat(idle
                    .OrderByDescending(w => w.ReadyQuantity + w.PausedQuantity)
                    .Select(w => (IReadOnlyList<object?>)
                    [
                        w.OperationCode, w.OperationName, null, null, null, 0, w.ReadyBundles + w.PausedBundles,
                        w.ReadyQuantity + w.PausedQuantity,
                    ]))
                .ToList());
    }
}

/// <summary>Gün gün fabrika verimi.</summary>
public sealed class EfficiencyReport(IPerformanceService performance, IFactoryClock clock) : IReport
{
    public ReportDefinition Definition { get; } = new(
        "efficiency", "Verim (günlük)",
        "Gün gün fabrikanın toplam verimi: kazanılan dakika (üretilen adet × SAM) ÷ kullanılabilir süre.");

    public async Task<Result<ReportData>> RunAsync(ReportParameters parameters, CancellationToken cancellationToken)
    {
        var rows = new List<IReadOnlyList<object?>>();
        foreach (var (day, start, end) in ReportDays.Split(clock, parameters.From, parameters.To))
        {
            var result = await performance.GetAsync(
                new PerformanceQuery { From = start, To = end, GroupBy = PerformanceGroupBy.Operator }, cancellationToken);
            if (!result.IsSuccess)
            {
                return result.Error!;
            }

            var total = result.Value.Total;
            rows.Add(
            [
                day, result.Value.Rows.Count, total.NetWorkingMinutes, total.WaitingMinutes, total.EarnedMinutes, total.Produced,
                total.TargetQuantity, total.Rejected, total.EfficiencyPercent, total.UtilizationPercent,
            ]);
        }

        return new ReportData(
            [
                new("day", "Tarih", Date),
                new("operators", "Operatör", Integer),
                new("net", "Net çalışma", Minutes),
                new("waiting", "Demet bekleme", Minutes),
                new("earned", "Kazanılan", Minutes),
                new("produced", "Üretilen", Integer),
                new("target", "Hedef", Integer),
                new("rejected", "Hatalı", Integer),
                new("efficiency", "Verim", Percent),
                new("utilization", "Kullanım", Percent),
            ],
            rows);
    }
}
