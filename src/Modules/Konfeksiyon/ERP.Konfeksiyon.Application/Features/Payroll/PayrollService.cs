using ERP.Konfeksiyon.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Konfeksiyon.Application.Features.ProductionTracking;
using ERP.Konfeksiyon.Application.Features.Reports;
using ERP.Konfeksiyon.Application.Features.Shifts;
using ERP.Konfeksiyon.Domain.Entities;
using ERP.Konfeksiyon.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ERP.Konfeksiyon.Application.Features.Payroll;

public interface IPayrollService
{
    Task<PayrollSettingsDto> GetSettingsAsync(CancellationToken cancellationToken);
    Task<Result<PayrollSettingsDto>> UpdateSettingsAsync(UpdatePayrollSettingsRequest request, CancellationToken cancellationToken);

    /// <summary>Geçerli ücret ayarları (demet açılırken birim ücreti sabitlemek için). Kayıt yoksa boş ayar döner.</summary>
    Task<PayrollSetting> LoadSettingAsync(CancellationToken cancellationToken);

    /// <summary>Aralıkta üretim bildiren operatörlerin parça başı kazancı ve primi.</summary>
    Task<Result<PayrollReportDto>> GetEarningsAsync(EarningsQuery query, CancellationToken cancellationToken);

    /// <summary>Bir operatörün kazancının gün ve operasyon dökümü.</summary>
    Task<Result<OperatorEarningsDetailDto>> GetOperatorEarningsAsync(
        int operatorId, EarningsQuery query, CancellationToken cancellationToken);
}

/// <summary>
/// Parça başı ücret ve prim. Kazanç, değiştirilemeyen üretim olaylarından (sağlam adet bildirimleri) ve demet
/// açılırken sabitlenen birim ücretten hesaplanır; bu yüzden aynı aralık her zaman aynı sonucu verir. Prim,
/// operatörün o günkü verimine (Performans ekranıyla aynı hesap) göre o günün parça başı kazancına eklenir.
/// </summary>
public sealed class PayrollService(IKonfeksiyonDbContext db, IPerformanceService performance, IFactoryClock clock) : IPayrollService
{
    private const string DefaultCurrency = "TRY";

    /// <summary>Bir operatörün bir gündeki bir operasyon için üretimi.</summary>
    private sealed record Output(int OperatorId, DateOnly Day, int OperationId, int Quantity, decimal Sam, decimal? UnitRate);

    private sealed record Computation(
        DateTime From,
        DateTime To,
        PayrollSetting Setting,
        IReadOnlyList<Output> Outputs,
        IReadOnlyDictionary<(int OperatorId, DateOnly Day), double?> DailyEfficiency,
        IReadOnlyDictionary<int, double?> PeriodEfficiency,
        double? TotalEfficiency);

    public async Task<PayrollSettingsDto> GetSettingsAsync(CancellationToken cancellationToken) =>
        ToDto(await LoadSettingAsync(cancellationToken));

    public async Task<PayrollSetting> LoadSettingAsync(CancellationToken cancellationToken) =>
        await db.PayrollSettings
            .AsNoTracking()
            .Include(s => s.BonusTiers)
            .SingleOrDefaultAsync(s => s.Id == PayrollSetting.SingletonId, cancellationToken)
        ?? new PayrollSetting { Currency = DefaultCurrency };

    public async Task<Result<PayrollSettingsDto>> UpdateSettingsAsync(
        UpdatePayrollSettingsRequest request, CancellationToken cancellationToken)
    {
        var setting = await db.PayrollSettings
            .Include(s => s.BonusTiers)
            .SingleOrDefaultAsync(s => s.Id == PayrollSetting.SingletonId, cancellationToken);
        if (setting is null)
        {
            return new Error(ErrorCodes.NotFound, "Ücret ayarları bulunamadı.", ErrorType.NotFound);
        }

        setting.MinuteRate = request.MinuteRate;
        setting.Currency = string.IsNullOrWhiteSpace(request.Currency) ? DefaultCurrency : request.Currency.Trim().ToUpperInvariant();

        db.PayrollBonusTiers.RemoveRange(setting.BonusTiers);
        setting.BonusTiers.Clear();
        foreach (var tier in (request.BonusTiers ?? []).OrderBy(t => t.MinEfficiencyPercent))
        {
            setting.BonusTiers.Add(new PayrollBonusTier
            {
                MinEfficiencyPercent = tier.MinEfficiencyPercent, BonusPercent = tier.BonusPercent,
            });
        }

        // Yalnızca kademeler değişse de ayar satırı güncellenir: "son değiştiren" bilgisi doğru kalır.
        db.Entry(setting).State = EntityState.Modified;
        await db.SaveChangesAsync(cancellationToken);
        return await GetSettingsAsync(cancellationToken);
    }

    public async Task<Result<PayrollReportDto>> GetEarningsAsync(EarningsQuery query, CancellationToken cancellationToken)
    {
        var computed = await ComputeAsync(query, null, cancellationToken);
        if (!computed.IsSuccess)
        {
            return computed.Error!;
        }

        var c = computed.Value;
        var operatorIds = c.Outputs.Select(o => o.OperatorId).Distinct().ToList();
        var names = await LoadOperatorsAsync(operatorIds, cancellationToken);

        var rows = operatorIds
            .Select(id =>
            {
                var (code, name) = names.GetValueOrDefault(id, ("?", "?"));
                return Summarize(c, id, code, name, c.Outputs.Where(o => o.OperatorId == id).ToList());
            })
            .OrderBy(r => r.OperatorCode, StringComparer.Ordinal)
            .ToList();

        var total = new OperatorEarningsDto(
            0, "", "Toplam",
            rows.Count == 0 ? 0 : rows.Max(r => r.DaysWorked),
            rows.Sum(r => r.Produced),
            Math.Round(rows.Sum(r => r.EarnedMinutes), 1),
            c.TotalEfficiency,
            rows.Sum(r => r.PieceEarnings),
            rows.Sum(r => r.BonusAmount),
            rows.Sum(r => r.TotalEarnings),
            rows.Sum(r => r.UnpricedQuantity));

        return new PayrollReportDto(c.From, c.To, c.Setting.Currency, c.Setting.MinuteRate, total, rows);
    }

    public async Task<Result<OperatorEarningsDetailDto>> GetOperatorEarningsAsync(
        int operatorId, EarningsQuery query, CancellationToken cancellationToken)
    {
        var names = await LoadOperatorsAsync([operatorId], cancellationToken);
        if (!names.TryGetValue(operatorId, out var operatorName))
        {
            return new Error(ErrorCodes.NotFound, "Operatör bulunamadı.", ErrorType.NotFound);
        }

        var computed = await ComputeAsync(query, operatorId, cancellationToken);
        if (!computed.IsSuccess)
        {
            return computed.Error!;
        }

        var c = computed.Value;
        var days = c.Outputs
            .GroupBy(o => o.Day)
            .OrderBy(g => g.Key)
            .Select(g => DayOf(c, operatorId, g.Key, g.ToList()))
            .ToList();

        var operationIds = c.Outputs.Select(o => o.OperationId).Distinct().ToList();
        var operationNames = await db.Operations
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(o => operationIds.Contains(o.Id))
            .ToDictionaryAsync(o => o.Id, o => new { o.OperationCode, o.Name }, cancellationToken);

        var operations = c.Outputs
            .GroupBy(o => o.OperationId)
            .Select(g =>
            {
                var priced = g.Where(o => o.UnitRate is not null).ToList();
                var amount = decimal.Round(priced.Sum(o => o.Quantity * o.UnitRate!.Value), 2);
                var pricedQuantity = priced.Sum(o => o.Quantity);
                var operation = operationNames.GetValueOrDefault(g.Key);
                return new EarningsOperationDto(
                    g.Key,
                    operation?.OperationCode ?? "?",
                    operation?.Name ?? "?",
                    g.Sum(o => o.Quantity),
                    pricedQuantity == 0 ? null : decimal.Round(priced.Sum(o => o.Quantity * o.UnitRate!.Value) / pricedQuantity, 4),
                    amount);
            })
            .OrderBy(o => o.OperationCode, StringComparer.Ordinal)
            .ToList();

        return new OperatorEarningsDetailDto(
            c.From, c.To, c.Setting.Currency,
            Summarize(c, operatorId, operatorName.Code, operatorName.Name, c.Outputs),
            days,
            operations);
    }

    private async Task<Result<Computation>> ComputeAsync(EarningsQuery query, int? operatorId, CancellationToken cancellationToken)
    {
        if (query.From == default || query.To == default || query.To <= query.From)
        {
            return RangeError("Geçerli bir tarih aralığı verin: bitiş, başlangıçtan sonra olmalıdır.");
        }

        if ((query.To - query.From).TotalDays > PerformanceService.MaxRangeDays)
        {
            return RangeError($"Kazanç hesabı için tarih aralığı en fazla {PerformanceService.MaxRangeDays} gün olabilir.");
        }

        var from = DateTime.SpecifyKind(query.From.ToUniversalTime(), DateTimeKind.Utc);
        var to = DateTime.SpecifyKind(query.To.ToUniversalTime(), DateTimeKind.Utc);
        var setting = await LoadSettingAsync(cancellationToken);

        var events = db.BundleOperationEvents
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(e => e.EventType == BundleOperationEventType.QuantityReported && e.ProducedQuantity > 0
                && e.OccurredAt >= from && e.OccurredAt < to
                // Operatörsüz ya da "performansa sayılmasın" diye elle bildirilen adet prime girmez.
                && e.CountsForPerformance && e.OperatorId != null);
        if (operatorId is { } id)
        {
            events = events.Where(e => e.OperatorId == id);
        }

        var facts = await events
            .Select(e => new
            {
                e.OperatorId,
                e.OccurredAt,
                e.ProducedQuantity,
                e.BundleOperation.OperationId,
                e.BundleOperation.Sam,
                e.BundleOperation.UnitRate,
            })
            .ToListAsync(cancellationToken);

        // Ücret tanımlanmadan önce açılmış demetlerde sabitlenmiş ücret yoktur: güncel tanımdan hesaplanır.
        var unpricedOperationIds = facts.Where(f => f.UnitRate is null).Select(f => f.OperationId).Distinct().ToList();
        var currentPieceRates = unpricedOperationIds.Count == 0
            ? []
            : await db.Operations
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Where(o => unpricedOperationIds.Contains(o.Id))
                .ToDictionaryAsync(o => o.Id, o => o.PieceRate, cancellationToken);

        var outputs = facts
            .Select(f => new Output(
                f.OperatorId!.Value,
                DateOnly.FromDateTime(clock.ToLocal(f.OccurredAt)),
                f.OperationId,
                f.ProducedQuantity,
                f.Sam,
                f.UnitRate ?? setting.UnitRateFor(currentPieceRates.GetValueOrDefault(f.OperationId), f.Sam)))
            .ToList();

        outputs.AddRange(await QualityDeductionsAsync(from, to, operatorId, setting, cancellationToken));

        // Prim günlük verime bağlıdır: her gün için Performans ekranıyla aynı hesap çalıştırılır.
        var dailyEfficiency = new Dictionary<(int, DateOnly), double?>();
        if (setting.BonusTiers.Count > 0)
        {
            var workedDays = outputs.Select(o => o.Day).ToHashSet();
            foreach (var (day, start, end) in ReportDays.Split(clock, from, to))
            {
                if (!workedDays.Contains(day))
                {
                    continue;
                }

                var daily = await performance.GetAsync(
                    new PerformanceQuery { From = start, To = end, GroupBy = PerformanceGroupBy.Operator }, cancellationToken);
                if (!daily.IsSuccess)
                {
                    return daily.Error!;
                }

                foreach (var row in daily.Value.Rows)
                {
                    dailyEfficiency[(row.Id, day)] = row.EfficiencyPercent;
                }
            }
        }

        var period = await performance.GetAsync(
            new PerformanceQuery { From = from, To = to, GroupBy = PerformanceGroupBy.Operator }, cancellationToken);
        if (!period.IsSuccess)
        {
            return period.Error!;
        }

        return new Computation(
            from, to, setting, outputs, dailyEfficiency,
            period.Value.Rows.ToDictionary(r => r.Id, r => r.EfficiencyPercent),
            period.Value.Total.EfficiencyPercent);
    }

    /// <summary>
    /// Kalitenin dönem içinde geri aldığı (ret ya da tamire gönderdiği) parçaların ücret kesintileri. Kesinti,
    /// kontrolün yapıldığı güne ve o parçaları bildiren operatöre yazılır: bir operasyonu birden fazla kişi
    /// bildirdiyse geri alınan adet en son bildirenden başlanarak düşülür. Tamir edilen parçalar yeniden
    /// bildirildiğinde tamiri yapan operatöre ödenir; böylece toplam ödeme sağlam çıkan adede eşit olur.
    /// </summary>
    private async Task<List<Output>> QualityDeductionsAsync(
        DateTime from, DateTime to, int? operatorId, PayrollSetting setting, CancellationToken cancellationToken)
    {
        var operationIds = await db.QualityInspections
            .AsNoTracking()
            .Where(i => i.InspectedAt >= from && i.InspectedAt < to && i.RejectedQuantity + i.ReworkQuantity > 0)
            .Select(i => i.BundleOperationId)
            .Distinct()
            .ToListAsync(cancellationToken);
        if (operationIds.Count == 0)
        {
            return [];
        }

        // Önceki dönemlerin geri alımları da okunur: hangi bildirimin ne kadarının zaten düşüldüğü onlardan çıkar.
        var takeBacks = await db.QualityInspections
            .AsNoTracking()
            .Where(i => operationIds.Contains(i.BundleOperationId) && i.InspectedAt < to && i.RejectedQuantity + i.ReworkQuantity > 0)
            .OrderBy(i => i.InspectedAt).ThenBy(i => i.Id)
            .Select(i => new { i.BundleOperationId, i.InspectedAt, Quantity = i.RejectedQuantity + i.ReworkQuantity })
            .ToListAsync(cancellationToken);
        var reports = await db.BundleOperationEvents
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(e => operationIds.Contains(e.BundleOperationId) && e.EventType == BundleOperationEventType.QuantityReported
                && e.ProducedQuantity > 0 && e.OccurredAt < to)
            .OrderBy(e => e.OccurredAt).ThenBy(e => e.Id)
            .Select(e => new
            {
                e.BundleOperationId,
                e.OperatorId,
                e.CountsForPerformance,
                e.OccurredAt,
                e.ProducedQuantity,
                e.BundleOperation.OperationId,
                e.BundleOperation.Sam,
                e.BundleOperation.UnitRate,
            })
            .ToListAsync(cancellationToken);

        var unpriced = reports.Where(r => r.UnitRate is null).Select(r => r.OperationId).Distinct().ToList();
        var pieceRates = unpriced.Count == 0
            ? []
            : await db.Operations
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Where(o => unpriced.Contains(o.Id))
                .ToDictionaryAsync(o => o.Id, o => o.PieceRate, cancellationToken);

        var deductions = new List<Output>();
        foreach (var operation in takeBacks.GroupBy(t => t.BundleOperationId))
        {
            var credits = reports.Where(r => r.BundleOperationId == operation.Key).ToList();
            var left = credits.Select(c => c.ProducedQuantity).ToArray();
            foreach (var takeBack in operation)
            {
                var needed = takeBack.Quantity;
                for (var i = credits.Count - 1; i >= 0 && needed > 0; i--)
                {
                    if (credits[i].OccurredAt > takeBack.InspectedAt || left[i] == 0)
                    {
                        continue;
                    }

                    var taken = Math.Min(left[i], needed);
                    left[i] -= taken;
                    needed -= taken;
                    // Geri alınan adet, prime sayılmamış bir bildirimdense kimseden düşülmez.
                    if (takeBack.InspectedAt >= from && credits[i].CountsForPerformance
                        && credits[i].OperatorId is { } creditOperator
                        && (operatorId is null || operatorId == creditOperator))
                    {
                        deductions.Add(new Output(
                            creditOperator,
                            DateOnly.FromDateTime(clock.ToLocal(takeBack.InspectedAt)),
                            credits[i].OperationId,
                            -taken,
                            credits[i].Sam,
                            credits[i].UnitRate ?? setting.UnitRateFor(pieceRates.GetValueOrDefault(credits[i].OperationId), credits[i].Sam)));
                    }
                }
            }
        }

        return deductions;
    }

    private static OperatorEarningsDto Summarize(
        Computation c, int operatorId, string code, string name, IReadOnlyList<Output> outputs)
    {
        var days = outputs.GroupBy(o => o.Day).Select(g => DayOf(c, operatorId, g.Key, g.ToList())).ToList();
        return new OperatorEarningsDto(
            operatorId,
            code,
            name,
            days.Count,
            outputs.Sum(o => o.Quantity),
            Math.Round((double)outputs.Sum(o => o.Quantity * o.Sam), 1),
            c.PeriodEfficiency.GetValueOrDefault(operatorId),
            days.Sum(d => d.PieceEarnings),
            days.Sum(d => d.BonusAmount),
            days.Sum(d => d.TotalEarnings),
            outputs.Where(o => o.UnitRate is null).Sum(o => o.Quantity));
    }

    private static EarningsDayDto DayOf(Computation c, int operatorId, DateOnly day, IReadOnlyList<Output> outputs)
    {
        var piece = decimal.Round(outputs.Sum(o => o.Quantity * (o.UnitRate ?? 0)), 2);
        var efficiency = c.DailyEfficiency.GetValueOrDefault((operatorId, day));
        var bonusPercent = c.Setting.BonusPercentFor(efficiency);
        var bonus = decimal.Round(piece * bonusPercent / 100, 2);
        return new EarningsDayDto(day, outputs.Sum(o => o.Quantity), efficiency, piece, bonusPercent, bonus, piece + bonus);
    }

    private async Task<Dictionary<int, (string Code, string Name)>> LoadOperatorsAsync(
        IReadOnlyCollection<int> ids, CancellationToken cancellationToken) =>
        (await db.Operators
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(o => ids.Contains(o.Id))
            .Select(o => new { o.Id, o.OperatorCode, o.FirstName, o.LastName })
            .ToListAsync(cancellationToken))
        .ToDictionary(o => o.Id, o => (o.OperatorCode, $"{o.FirstName} {o.LastName}"));

    private static PayrollSettingsDto ToDto(PayrollSetting setting) => new(
        setting.MinuteRate,
        setting.Currency,
        setting.BonusTiers
            .OrderBy(t => t.MinEfficiencyPercent)
            .Select(t => new BonusTierDto(t.MinEfficiencyPercent, t.BonusPercent))
            .ToList(),
        setting.UpdatedAt,
        setting.UpdatedBy);

    private static Error RangeError(string message) => new(
        ErrorCodes.ValidationError, message, ErrorType.Validation,
        new Dictionary<string, string[]> { ["from"] = [message] });
}

/// <summary>Operatör kazançları (parça başı ücret + prim); Ücret ve Prim ekranıyla aynı hesap.</summary>
public sealed class OperatorEarningsReport(IPayrollService payroll) : IReport
{
    public ReportDefinition Definition { get; } = new(
        "operator-earnings", "Operatör kazançları",
        "Operatör bazında parça başı kazanç ve verim primi. Kazanç, bildirilen sağlam adet ile demet açılırken " +
        "sabitlenen birim ücretin çarpımıdır; prim günlük verim kademelerinden gelir.",
        Group: ReportGroups.Personnel,
        Permission: ERP.Konfeksiyon.Domain.Authorization.KonfeksiyonPermissions.Report.Personnel);

    public async Task<Result<ReportData>> RunAsync(ReportParameters parameters, CancellationToken cancellationToken)
    {
        var result = await payroll.GetEarningsAsync(
            new EarningsQuery { From = parameters.From, To = parameters.To }, cancellationToken);
        if (!result.IsSuccess)
        {
            return result.Error!;
        }

        var report = result.Value;
        static IReadOnlyList<object?> Cells(OperatorEarningsDto r, string? code) =>
        [
            code, r.OperatorName, r.DaysWorked, r.Produced, r.EarnedMinutes, r.EfficiencyPercent, r.PieceEarnings,
            r.BonusAmount, r.TotalEarnings, r.UnpricedQuantity,
        ];

        var totals = Cells(report.Total, "Toplam");
        return new ReportData(
            [
                new("code", "Sicil"),
                new("name", "Operatör"),
                new("days", "Çalışılan gün", ReportColumnType.Integer),
                new("produced", "Üretilen", ReportColumnType.Integer),
                new("earned", "Kazanılan", ReportColumnType.Minutes),
                new("efficiency", "Verim", ReportColumnType.Percent),
                new("piece", $"Parça başı ({report.Currency})", ReportColumnType.Decimal),
                new("bonus", $"Prim ({report.Currency})", ReportColumnType.Decimal),
                new("total", $"Toplam ({report.Currency})", ReportColumnType.Decimal),
                new("unpriced", "Ücretsiz adet", ReportColumnType.Integer),
            ],
            report.Rows.Select(r => Cells(r, r.OperatorCode)).ToList(),
            report.Rows.Count == 0 ? null : [totals[0], null, null, .. totals.Skip(3)]);
    }
}
