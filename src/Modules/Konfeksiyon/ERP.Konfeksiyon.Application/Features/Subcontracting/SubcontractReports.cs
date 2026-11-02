using ERP.Core.Application.Common;
using ERP.Konfeksiyon.Application.Abstractions;
using ERP.Konfeksiyon.Application.Features.Reports;
using ERP.Konfeksiyon.Application.Features.Shifts;
using ERP.Konfeksiyon.Domain.Authorization;
using ERP.Konfeksiyon.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using static ERP.Konfeksiyon.Application.Features.Reports.ReportColumnType;

namespace ERP.Konfeksiyon.Application.Features.Subcontracting;

internal static class SubcontractReportLabels
{
    /// <summary>Raporlar ekranındaki grup.</summary>
    public const string Group = "Fason";
}

/// <summary>Fasoncudaki işler: açık gönderimler, dışarıdaki adet ve dışarıda geçen gün.</summary>
public sealed class SubcontractOpenReport(IKonfeksiyonDbContext db, TimeProvider timeProvider) : IReport
{
    public ReportDefinition Definition { get; } = new(
        "subcontract-open", "Fasondaki işler",
        "Açık (tamamı dönmemiş) fason gönderimleri: fasoncu, operasyon, gönderilen, dönen ve hâlâ dışarıdaki adet, " +
        "gönderimden bu yana geçen gün. En uzun süredir dışarıda olan üsttedir.",
        RequiresDateRange: false, SearchLabel: "Fasoncu, fiş no veya operasyon", Group: SubcontractReportLabels.Group,
        Permission: KonfeksiyonPermissions.Subcontract.View);

    public async Task<Result<ReportData>> RunAsync(ReportParameters parameters, CancellationToken cancellationToken)
    {
        var sends = db.SubcontractSends
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(s => s.Status == SubcontractSendStatus.Open || s.Status == SubcontractSendStatus.PartiallyReturned);

        if (parameters.Search is { } search)
        {
            sends = sends.Where(s => s.SendNo.Contains(search) || s.Supplier.Name.Contains(search)
                || s.Operation.Name.Contains(search) || s.Operation.OperationCode.Contains(search));
        }

        var rows = await sends
            .OrderBy(s => s.SendDate).ThenBy(s => s.Id)
            .Take(ReportLimits.MaxRows + 1)
            .Select(s => new
            {
                SupplierName = s.Supplier.Name,
                s.SendNo,
                s.SendDate,
                OperationName = s.Operation.Name,
                Sent = s.Lines.Sum(l => (int?)l.Quantity) ?? 0,
                Good = s.Lines.Sum(l => (int?)l.ReturnedGood) ?? 0,
                Lost = s.Lines.Sum(l => (int?)(l.ReturnedDefective + l.ReturnedMissing)) ?? 0,
            })
            .ToListAsync(cancellationToken);

        var today = DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);
        var shown = rows.Take(ReportLimits.MaxRows).ToList();
        return new ReportData(
            [
                new("supplier", "Fasoncu"),
                new("sendNo", "Gönderim"),
                new("sendDate", "Gönderim tarihi", Date),
                new("operation", "Operasyon"),
                new("sent", "Gönderilen", Integer),
                new("good", "Sağlam dönen", Integer),
                new("lost", "Hatalı / eksik", Integer),
                new("outside", "Dışarıda", Integer),
                new("days", "Gün", Integer),
            ],
            shown
                .Select(r => (IReadOnlyList<object?>)
                [
                    r.SupplierName, r.SendNo, r.SendDate, r.OperationName, r.Sent, r.Good, r.Lost, r.Sent - r.Good - r.Lost,
                    today.DayNumber - r.SendDate.DayNumber,
                ])
                .ToList(),
            shown.Count == 0
                ? null
                :
                [
                    "Toplam", null, null, null, shown.Sum(r => r.Sent), shown.Sum(r => r.Good), shown.Sum(r => r.Lost),
                    shown.Sum(r => r.Sent - r.Good - r.Lost), null,
                ],
            rows.Count > ReportLimits.MaxRows);
    }
}

/// <summary>Fason kaybı: fasoncu ve operasyon bazında dönen sağlam, hatalı ve eksik adet.</summary>
public sealed class SubcontractLossReport(IKonfeksiyonDbContext db, IFactoryClock clock) : IReport
{
    public ReportDefinition Definition { get; } = new(
        "subcontract-loss", "Fason fire ve kayıp",
        "Dönüş tarihine göre fasoncu ve operasyon bazında dönen sağlam, hatalı ve eksik adet; kayıp oranı en yüksek üsttedir.",
        MaxRangeDays: 366, Group: SubcontractReportLabels.Group, Permission: KonfeksiyonPermissions.Subcontract.View);

    public async Task<Result<ReportData>> RunAsync(ReportParameters parameters, CancellationToken cancellationToken)
    {
        // Dönüş tarihi fabrikanın yerel günüdür; aralığın sonu hariçtir.
        var from = DateOnly.FromDateTime(clock.ToLocal(parameters.From));
        var to = DateOnly.FromDateTime(clock.ToLocal(parameters.To));

        var rows = await db.SubcontractReturnLines
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(l => l.SubcontractReturn.ReturnDate >= from && l.SubcontractReturn.ReturnDate < to)
            .GroupBy(l => new
            {
                l.SubcontractReturn.SupplierId,
                SupplierName = l.SubcontractReturn.Supplier.Name,
                l.SubcontractReturn.SubcontractSend.OperationId,
                OperationName = l.SubcontractReturn.SubcontractSend.Operation.Name,
            })
            .Select(g => new
            {
                g.Key.SupplierName,
                g.Key.OperationName,
                Returns = g.Select(l => l.SubcontractReturnId).Distinct().Count(),
                Good = g.Sum(l => l.GoodQuantity),
                Defective = g.Sum(l => l.DefectiveQuantity),
                Missing = g.Sum(l => l.MissingQuantity),
            })
            .ToListAsync(cancellationToken);

        static double? LossRate(int good, int defective, int missing) =>
            ReportMath.Rate(defective + missing, good + defective + missing);

        var good = rows.Sum(r => r.Good);
        var defective = rows.Sum(r => r.Defective);
        var missing = rows.Sum(r => r.Missing);

        return new ReportData(
            [
                new("supplier", "Fasoncu"),
                new("operation", "Operasyon"),
                new("returns", "Dönüş fişi", Integer),
                new("returned", "Dönen", Integer),
                new("good", "Sağlam", Integer),
                new("defective", "Hatalı", Integer),
                new("missing", "Eksik", Integer),
                new("lossRate", "Kayıp oranı", Percent),
            ],
            rows
                .OrderByDescending(r => LossRate(r.Good, r.Defective, r.Missing) ?? 0)
                .ThenBy(r => r.SupplierName, StringComparer.CurrentCulture)
                .Select(r => (IReadOnlyList<object?>)
                [
                    r.SupplierName, r.OperationName, r.Returns, r.Good + r.Defective + r.Missing, r.Good, r.Defective,
                    r.Missing, LossRate(r.Good, r.Defective, r.Missing),
                ])
                .ToList(),
            rows.Count == 0
                ? null
                :
                [
                    "Toplam", null, rows.Sum(r => r.Returns), good + defective + missing, good, defective, missing,
                    LossRate(good, defective, missing),
                ]);
    }
}
