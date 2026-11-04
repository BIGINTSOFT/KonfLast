using ERP.Konfeksiyon.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Konfeksiyon.Application.Features.Quality;
using ERP.Konfeksiyon.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using static ERP.Konfeksiyon.Application.Features.Reports.ReportColumnType;

namespace ERP.Konfeksiyon.Application.Features.Reports;

/// <summary>Operasyon bazında kalite kontrol sonuçları.</summary>
public sealed class QualityReport(IKonfeksiyonDbContext db) : IReport
{
    public ReportDefinition Definition { get; } = new(
        "quality", "Kalite raporu",
        "Operasyon bazında kontrol edilen, geçen, reddedilen ve tamire gönderilen adet. Hata oranı en yüksek operasyon üsttedir.",
        MaxRangeDays: 366, Group: ReportGroups.Quality);

    public async Task<Result<ReportData>> RunAsync(ReportParameters parameters, CancellationToken cancellationToken)
    {
        var operations = await db.QualityInspections
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(i => i.InspectedAt >= parameters.From && i.InspectedAt < parameters.To)
            .GroupBy(i => new
            {
                i.BundleOperation.OperationId, i.BundleOperation.Operation.OperationCode, i.BundleOperation.Operation.Name,
            })
            .Select(g => new
            {
                g.Key.OperationCode,
                g.Key.Name,
                Inspections = g.Count(),
                Inspected = g.Sum(i => i.InspectedQuantity),
                Rejected = g.Sum(i => i.RejectedQuantity),
                Rework = g.Sum(i => i.ReworkQuantity),
                Holds = g.Count(i => i.PlacedOnHold),
            })
            .ToListAsync(cancellationToken);

        var inspected = operations.Sum(o => o.Inspected);
        var rejected = operations.Sum(o => o.Rejected);
        var rework = operations.Sum(o => o.Rework);

        return new ReportData(
            [
                new("code", "Kod"),
                new("name", "Operasyon"),
                new("inspections", "Kontrol sayısı", Integer),
                new("inspected", "Kontrol edilen", Integer),
                new("passed", "Geçen", Integer),
                new("rejected", "Ret", Integer),
                new("rework", "Tamir", Integer),
                new("holds", "Bekletme", Integer),
                new("defectRate", "Hata oranı", Percent),
            ],
            operations
                .OrderByDescending(o => ReportMath.Rate(o.Rejected + o.Rework, o.Inspected) ?? 0)
                .ThenBy(o => o.OperationCode, StringComparer.Ordinal)
                .Select(o => (IReadOnlyList<object?>)
                [
                    o.OperationCode, o.Name, o.Inspections, o.Inspected, o.Inspected - o.Rejected - o.Rework, o.Rejected,
                    o.Rework, o.Holds, ReportMath.Rate(o.Rejected + o.Rework, o.Inspected),
                ])
                .ToList(),
            operations.Count == 0
                ? null
                :
                [
                    "Toplam", null, operations.Sum(o => o.Inspections), inspected, inspected - rejected - rework, rejected, rework,
                    operations.Sum(o => o.Holds), ReportMath.Rate(rejected + rework, inspected),
                ]);
    }
}

/// <summary>Hata türlerine göre dağılım.</summary>
public sealed class DefectReport(IQualityService quality) : IReport
{
    public ReportDefinition Definition { get; } = new(
        "defects", "Hata raporu",
        "Hata türlerine göre bulunan hata adedi ve payı. En sık görülen hata üsttedir.", MaxRangeDays: 366, Group: ReportGroups.Quality);

    public async Task<Result<ReportData>> RunAsync(ReportParameters parameters, CancellationToken cancellationToken)
    {
        var result = await quality.GetDefectSummaryAsync(
            new DefectSummaryQuery { From = parameters.From, To = parameters.To }, cancellationToken);
        if (!result.IsSuccess)
        {
            return result.Error!;
        }

        var defects = result.Value.Defects;
        var total = defects.Sum(d => d.Quantity);

        return new ReportData(
            [
                new("code", "Kod"),
                new("name", "Hata türü"),
                new("quantity", "Adet", Integer),
                new("share", "Pay", Percent),
                new("inspections", "Görüldüğü kontrol sayısı", Integer),
            ],
            defects
                .Select(d => (IReadOnlyList<object?>)[d.Code, d.Name, d.Quantity, ReportMath.Rate(d.Quantity, total), d.InspectionCount])
                .ToList(),
            defects.Count == 0 ? null : ["Toplam", null, total, 100.0, null]);
    }
}

/// <summary>Anlık stok: depo ve kart bazında bakiyeler.</summary>
public sealed class StockReport(IKonfeksiyonDbContext db) : IReport
{
    public ReportDefinition Definition { get; } = new(
        "stock", "Stok",
        "Şu anki stok: depo, adres ve kart bazında bakiyeler (sıfır bakiyeler hariç). En fazla 5.000 satır.",
        RequiresDateRange: false, SearchLabel: "Kart kodu veya adı", Group: StockLabels.Group);

    public async Task<Result<ReportData>> RunAsync(ReportParameters parameters, CancellationToken cancellationToken)
    {
        var stocks = db.Stocks.AsNoTracking().IgnoreQueryFilters().Where(s => s.Quantity > 0);
        if (parameters.Search is { } search)
        {
            stocks = stocks.Where(s => s.StockItem.Code.Contains(search) || s.StockItem.Name.Contains(search));
        }

        var items = await stocks
            .OrderBy(s => s.StockItem.Code).ThenBy(s => s.Warehouse.Code)
            .ThenByDescending(s => s.Location.IsDefault).ThenBy(s => s.Location.Code)
            .Take(ReportLimits.MaxRows + 1)
            .Select(s => new
            {
                s.StockItem.Code, s.StockItem.Name, s.StockItem.ItemType, WarehouseCode = s.Warehouse.Code,
                WarehouseName = s.Warehouse.Name, LocationCode = s.Location.Code, s.Quantity, s.StockItem.Unit,
            })
            .ToListAsync(cancellationToken);

        // Birimler farklı olduğundan (m, kg, adet) miktarların toplamı anlamsızdır; toplam satırı yoktur.
        return new ReportData(
            [
                new("code", "Kart kodu"),
                new("name", "Ad"),
                new("type", "Tür"),
                new("warehouseCode", "Depo"),
                new("warehouseName", "Depo adı"),
                new("locationCode", "Adres"),
                new("quantity", "Miktar", ReportColumnType.Decimal),
                new("unit", "Birim"),
            ],
            items
                .Take(ReportLimits.MaxRows)
                .Select(s => (IReadOnlyList<object?>)
                [
                    s.Code, s.Name, StockLabels.Types[s.ItemType], s.WarehouseCode, s.WarehouseName, s.LocationCode, s.Quantity,
                    StockLabels.Units[s.Unit],
                ])
                .ToList(),
            Truncated: items.Count > ReportLimits.MaxRows);
    }
}

/// <summary>Sevk edilen mamuller: sevkiyat kalemleri, sevk zamanına göre.</summary>
public sealed class ShipmentReport(IKonfeksiyonDbContext db) : IReport
{
    public ReportDefinition Definition { get; } = new(
        "shipments", "Sevkiyat",
        "Sevk edilmiş sevkiyatların kalemleri, eskiden yeniye. En fazla 5.000 satır; daraltmak için arama yapın.",
        MaxRangeDays: 366, SearchLabel: "Sevkiyat no, müşteri, sipariş no veya müşteri sipariş no", Group: StockLabels.Group);

    public async Task<Result<ReportData>> RunAsync(ReportParameters parameters, CancellationToken cancellationToken)
    {
        // Geçmiş, sonradan silinen müşteri / kart / depo için de eksiksiz görünür.
        var lines = db.ShipmentItems
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(i => !i.Shipment.IsDeleted && i.Shipment.Status == ShipmentStatus.Shipped
                && i.Shipment.ShippedAt >= parameters.From && i.Shipment.ShippedAt < parameters.To);

        if (parameters.Search is { } search)
        {
            lines = lines.Where(i =>
                i.Shipment.ShipmentNo.Contains(search)
                || i.Shipment.Customer.Name.Contains(search)
                || (i.Shipment.ProductionOrder != null && i.Shipment.ProductionOrder.OrderNo.Contains(search))
                || (i.Shipment.SalesOrder != null
                    && (i.Shipment.SalesOrder.OrderNo.Contains(search)
                        || (i.Shipment.SalesOrder.CustomerOrderNo != null && i.Shipment.SalesOrder.CustomerOrderNo.Contains(search)))));
        }

        var items = await lines
            .OrderBy(i => i.Shipment.ShippedAt).ThenBy(i => i.Id)
            .Take(ReportLimits.MaxRows + 1)
            .Select(i => new
            {
                i.Shipment.ShippedAt,
                i.Shipment.ShipmentNo,
                CustomerName = i.Shipment.Customer.Name,
                // Üretim siparişi yoksa müşteri siparişi gösterilir (sevkiyat listesindeki gibi).
                OrderNo = i.Shipment.ProductionOrder != null
                    ? i.Shipment.ProductionOrder.OrderNo
                    : i.Shipment.SalesOrder != null ? i.Shipment.SalesOrder.OrderNo : null,
                i.StockItem.Code,
                i.StockItem.Name,
                i.Quantity,
                WarehouseCode = i.Shipment.Warehouse.Code,
                i.Shipment.DocumentNo,
                i.Shipment.Carrier,
                i.Shipment.ShippedBy,
            })
            .ToListAsync(cancellationToken);

        var shown = items.Take(ReportLimits.MaxRows).ToList();
        return new ReportData(
            [
                new("shippedAt", "Sevk zamanı", ReportColumnType.DateTime),
                new("shipmentNo", "Sevkiyat"),
                new("customer", "Müşteri"),
                new("orderNo", "İş emri"),
                new("code", "Mamul kodu"),
                new("name", "Ad"),
                new("quantity", "Adet", ReportColumnType.Decimal),
                new("warehouse", "Depo"),
                new("documentNo", "İrsaliye"),
                new("carrier", "Taşıyıcı"),
                new("shippedBy", "Sevk eden"),
            ],
            shown
                .Select(i => (IReadOnlyList<object?>)
                [
                    i.ShippedAt, i.ShipmentNo, i.CustomerName, i.OrderNo, i.Code, i.Name, i.Quantity, i.WarehouseCode,
                    i.DocumentNo, i.Carrier, i.ShippedBy,
                ])
                .ToList(),
            shown.Count == 0 ? null : ["Toplam", null, null, null, null, null, shown.Sum(i => i.Quantity), null, null, null, null],
            items.Count > ReportLimits.MaxRows);
    }
}
