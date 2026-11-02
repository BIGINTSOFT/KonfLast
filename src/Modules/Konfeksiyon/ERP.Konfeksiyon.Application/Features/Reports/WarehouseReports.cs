using ERP.Konfeksiyon.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Konfeksiyon.Application.Features.Shifts;
using ERP.Core.Domain.Enums;
using ERP.Konfeksiyon.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using static ERP.Konfeksiyon.Application.Features.Reports.ReportColumnType;

namespace ERP.Konfeksiyon.Application.Features.Reports;

/// <summary>Depo raporlarının ortak adları.</summary>
public static class StockLabels
{
    /// <summary>Raporlar ekranındaki grup adı.</summary>
    public const string Group = "Depo";

    public static readonly IReadOnlyDictionary<StockItemType, string> Types = new Dictionary<StockItemType, string>
    {
        [StockItemType.Fabric] = "Kumaş",
        [StockItemType.Accessory] = "Aksesuar",
        [StockItemType.FinishedGood] = "Mamul",
        [StockItemType.Other] = "Diğer",
    };

    public static readonly IReadOnlyDictionary<StockUnit, string> Units = new Dictionary<StockUnit, string>
    {
        [StockUnit.Piece] = "adet",
        [StockUnit.Meter] = "m",
        [StockUnit.Kilogram] = "kg",
        [StockUnit.Roll] = "top",
        [StockUnit.Box] = "koli",
    };

    /// <summary>Miktar × birim maliyet (iki ondalık); maliyet bilinmiyorsa null.</summary>
    public static decimal? Value(decimal quantity, decimal? unitCost) =>
        unitCost is null ? null : decimal.Round(quantity * unitCost.Value, 2);
}

/// <summary>Toplam stoğu asgari stoğunun altına düşmüş kartlar.</summary>
public sealed class BelowMinimumStockReport(IKonfeksiyonDbContext db) : IReport
{
    public ReportDefinition Definition { get; } = new(
        "stock-below-minimum", "Asgari stok altındaki kartlar",
        "Tüm depolardaki toplam stoğu, kartında tanımlı asgari stoğun altına düşmüş malzemeler. Açığı en büyük olan üsttedir.",
        RequiresDateRange: false, SearchLabel: "Kart kodu veya adı", Group: StockLabels.Group);

    public async Task<Result<ReportData>> RunAsync(ReportParameters parameters, CancellationToken cancellationToken)
    {
        var cards = db.StockItems.AsNoTracking().Where(i => i.MinimumStock != null && i.MinimumStock > 0);
        if (parameters.Search is { } search)
        {
            cards = cards.Where(i => i.Code.Contains(search) || i.Name.Contains(search));
        }

        var items = await cards
            .Select(i => new
            {
                i.Id, i.Code, i.Name, i.ItemType, i.Unit, Minimum = i.MinimumStock!.Value,
                Supplier = i.Supplier == null ? null : i.Supplier.Name,
            })
            .ToListAsync(cancellationToken);

        var onHand = await db.Stocks
            .AsNoTracking()
            .Where(s => s.StockItem.MinimumStock != null)
            .GroupBy(s => s.StockItemId)
            .Select(g => new { StockItemId = g.Key, Quantity = g.Sum(s => s.Quantity) })
            .ToDictionaryAsync(g => g.StockItemId, g => g.Quantity, cancellationToken);
        var onOrder = await db.PurchaseOrderItems
            .AsNoTracking()
            .Where(i => !i.PurchaseOrder.IsDeleted && i.StockItem.MinimumStock != null
                && (i.PurchaseOrder.Status == PurchaseOrderStatus.Approved
                    || i.PurchaseOrder.Status == PurchaseOrderStatus.PartiallyReceived))
            .GroupBy(i => i.StockItemId)
            .Select(g => new { StockItemId = g.Key, Quantity = g.Sum(i => i.Quantity - i.ReceivedQuantity) })
            .ToDictionaryAsync(g => g.StockItemId, g => g.Quantity, cancellationToken);

        return new ReportData(
            [
                new("code", "Kart kodu"),
                new("name", "Ad"),
                new("type", "Tür"),
                new("onHand", "Stokta", ReportColumnType.Decimal),
                new("minimum", "Asgari stok", ReportColumnType.Decimal),
                new("shortfall", "Açık", ReportColumnType.Decimal),
                new("onOrder", "Yolda", ReportColumnType.Decimal),
                new("unit", "Birim"),
                new("supplier", "Tedarikçi"),
            ],
            items
                .Select(i => new { Item = i, OnHand = onHand.GetValueOrDefault(i.Id) })
                .Where(r => r.OnHand < r.Item.Minimum)
                .OrderByDescending(r => (r.Item.Minimum - r.OnHand) / r.Item.Minimum)
                .ThenBy(r => r.Item.Code, StringComparer.Ordinal)
                .Select(r => (IReadOnlyList<object?>)
                [
                    r.Item.Code, r.Item.Name, StockLabels.Types[r.Item.ItemType], r.OnHand, r.Item.Minimum,
                    r.Item.Minimum - r.OnHand, onOrder.GetValueOrDefault(r.Item.Id), StockLabels.Units[r.Item.Unit],
                    r.Item.Supplier,
                ])
                .ToList());
    }
}

/// <summary>Stoğu olup son N günde hiç hareket görmemiş kartlar (depo bazında).</summary>
public sealed class IdleStockReport(IKonfeksiyonDbContext db, IFactoryClock clock) : IReport
{
    public const int DefaultDays = 90;

    public ReportDefinition Definition { get; } = new(
        "stock-idle", "Hareketsiz stok",
        "Depoda stoğu olan ama son N günde giriş ya da çıkış görmemiş kartlar (depo içi adres değişimi hareket sayılmaz): "
        + "son hareket, kaç gündür beklediği ve stok değeri. En uzun süredir bekleyen üsttedir. En fazla 5.000 satır.",
        RequiresDateRange: false, SearchLabel: "Kart kodu veya adı", Group: StockLabels.Group,
        DaysLabel: "Hareketsiz gün", DefaultDays: DefaultDays);

    public async Task<Result<ReportData>> RunAsync(ReportParameters parameters, CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var cutoff = now.AddDays(-(parameters.Days ?? DefaultDays));

        var stocks = db.Stocks.AsNoTracking().IgnoreQueryFilters().Where(s => s.Quantity > 0);
        if (parameters.Search is { } search)
        {
            stocks = stocks.Where(s => s.StockItem.Code.Contains(search) || s.StockItem.Name.Contains(search));
        }

        var balances = await stocks
            .GroupBy(s => new
            {
                s.WarehouseId, s.StockItemId, s.StockItem.Code, s.StockItem.Name, s.StockItem.ItemType, s.StockItem.Unit,
                s.StockItem.UnitCost, WarehouseCode = s.Warehouse.Code,
            })
            .Select(g => new { g.Key, Quantity = g.Sum(s => s.Quantity) })
            .ToListAsync(cancellationToken);

        // Kartın o depodaki son giriş / çıkışı; hiç hareketi yoksa (açılış verisi vb.) boş.
        var lastMovements = (await db.StockTransactions
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Where(t => t.Type != StockTransactionType.RelocationOut && t.Type != StockTransactionType.RelocationIn)
                .GroupBy(t => new { t.WarehouseId, t.StockItemId })
                .Select(g => new { g.Key.WarehouseId, g.Key.StockItemId, Last = g.Max(t => t.OccurredAt) })
                .ToListAsync(cancellationToken))
            .ToDictionary(m => (m.WarehouseId, m.StockItemId), m => m.Last);

        var idle = balances
            .Select(b => new
            {
                b.Key,
                b.Quantity,
                Last = lastMovements.TryGetValue((b.Key.WarehouseId, b.Key.StockItemId), out var last) ? last : (System.DateTime?)null,
            })
            .Where(r => r.Last is null || r.Last < cutoff)
            .OrderBy(r => r.Last ?? System.DateTime.MinValue)
            .ThenBy(r => r.Key.Code, StringComparer.Ordinal)
            .ToList();
        var shown = idle.Take(ReportLimits.MaxRows).ToList();
        var values = shown.Select(r => StockLabels.Value(r.Quantity, r.Key.UnitCost)).ToList();

        return new ReportData(
            [
                new("code", "Kart kodu"),
                new("name", "Ad"),
                new("type", "Tür"),
                new("warehouse", "Depo"),
                new("quantity", "Stokta", ReportColumnType.Decimal),
                new("unit", "Birim"),
                new("lastMovement", "Son hareket", ReportColumnType.DateTime),
                new("idleDays", "Hareketsiz (gün)", Integer),
                new("value", "Stok değeri", ReportColumnType.Decimal, Financial: true),
            ],
            shown
                .Select((r, index) => (IReadOnlyList<object?>)
                [
                    r.Key.Code, r.Key.Name, StockLabels.Types[r.Key.ItemType], r.Key.WarehouseCode, r.Quantity,
                    StockLabels.Units[r.Key.Unit], r.Last,
                    r.Last is { } at ? (object?)Math.Max((int)(now - at).TotalDays, 0) : null, values[index],
                ])
                .ToList(),
            shown.Count == 0 ? null : ["Toplam", null, null, null, null, null, null, null, values.Sum(v => v ?? 0)],
            idle.Count > ReportLimits.MaxRows);
    }
}

/// <summary>Stokta duran toplar, giriş tarihine göre (en eski üstte).</summary>
public sealed class LotAgingReport(IKonfeksiyonDbContext db, IFactoryClock clock) : IReport
{
    public ReportDefinition Definition { get; } = new(
        "lot-aging", "Top ve parti yaşlandırma",
        "Stokta duran toplar ve partiler: giriş tarihi, kaç gündür beklediği ve kalan miktarı. En eski top üsttedir. En fazla 5.000 satır.",
        RequiresDateRange: false, SearchLabel: "Top no, parti no ya da kart", Group: StockLabels.Group);

    public async Task<Result<ReportData>> RunAsync(ReportParameters parameters, CancellationToken cancellationToken)
    {
        var stocks = db.Stocks.AsNoTracking().IgnoreQueryFilters().Where(s => s.LotId != null && s.Quantity > 0);
        if (parameters.Search is { } search)
        {
            stocks = stocks.Where(s =>
                s.Lot!.LotNo.Contains(search) || (s.Lot.BatchNo != null && s.Lot.BatchNo.Contains(search))
                || s.StockItem.Code.Contains(search) || s.StockItem.Name.Contains(search));
        }

        var rows = await stocks
            .OrderBy(s => s.Lot!.ReceivedAt).ThenBy(s => s.LotId).ThenBy(s => s.Location.Code)
            .Take(ReportLimits.MaxRows + 1)
            .Select(s => new
            {
                s.Lot!.LotNo, s.StockItem.Code, s.StockItem.Name, s.Lot.BatchNo, s.Lot.Shade, WarehouseCode = s.Warehouse.Code,
                LocationCode = s.Location.Code, s.Lot.ReceivedAt, s.Lot.InitialQuantity, s.Quantity, s.StockItem.Unit,
            })
            .ToListAsync(cancellationToken);

        var now = clock.UtcNow;
        return new ReportData(
            [
                new("lotNo", "Top no"),
                new("code", "Kart kodu"),
                new("name", "Ad"),
                new("batchNo", "Parti"),
                new("shade", "Ton"),
                new("warehouse", "Depo"),
                new("location", "Adres"),
                new("receivedAt", "Giriş", ReportColumnType.DateTime),
                new("ageDays", "Yaş (gün)", Integer),
                new("initial", "Giriş miktarı", ReportColumnType.Decimal),
                new("quantity", "Kalan", ReportColumnType.Decimal),
                new("unit", "Birim"),
            ],
            rows
                .Take(ReportLimits.MaxRows)
                .Select(r => (IReadOnlyList<object?>)
                [
                    r.LotNo, r.Code, r.Name, r.BatchNo, r.Shade, r.WarehouseCode, r.LocationCode, r.ReceivedAt,
                    Math.Max((int)(now - r.ReceivedAt).TotalDays, 0), r.InitialQuantity, r.Quantity, StockLabels.Units[r.Unit],
                ])
                .ToList(),
            Truncated: rows.Count > ReportLimits.MaxRows);
    }
}

/// <summary>Onaylanmış sayımlarda sayılanı beklenenden farklı çıkan satırlar.</summary>
public sealed class StockCountDifferencesReport(IKonfeksiyonDbContext db) : IReport
{
    public ReportDefinition Definition { get; } = new(
        "stock-count-differences", "Sayım farkları",
        "Seçilen aralıkta onaylanan sayımlarda sayılan miktarı beklenenden farklı çıkan satırlar ve farkın güncel birim "
        + "maliyetle tutarı. En fazla 5.000 satır.",
        MaxRangeDays: 366, SearchLabel: "Sayım no ya da kart", Group: StockLabels.Group);

    public async Task<Result<ReportData>> RunAsync(ReportParameters parameters, CancellationToken cancellationToken)
    {
        var lines = db.StockCountLines
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(l => l.StockCount.Status == StockCountStatus.Posted
                && l.StockCount.PostedAt >= parameters.From && l.StockCount.PostedAt < parameters.To
                && l.CountedQuantity != null && l.CountedQuantity != l.ExpectedQuantity);
        if (parameters.Search is { } search)
        {
            lines = lines.Where(l =>
                l.StockCount.CountNo.Contains(search) || l.StockItem.Code.Contains(search) || l.StockItem.Name.Contains(search));
        }

        var rows = await lines
            .OrderBy(l => l.StockCount.PostedAt).ThenBy(l => l.StockCountId).ThenBy(l => l.Location.Code).ThenBy(l => l.StockItem.Code)
            .Take(ReportLimits.MaxRows + 1)
            .Select(l => new
            {
                l.StockCount.PostedAt, l.StockCount.CountNo, WarehouseCode = l.StockCount.Warehouse.Code,
                LocationCode = l.Location.Code, l.StockItem.Code, l.StockItem.Name, LotNo = l.Lot == null ? null : l.Lot.LotNo,
                l.ExpectedQuantity, Counted = l.CountedQuantity!.Value, l.StockItem.Unit, l.StockItem.UnitCost,
                l.StockCount.PostedBy,
            })
            .ToListAsync(cancellationToken);

        var shown = rows.Take(ReportLimits.MaxRows).ToList();
        var values = shown.Select(r => StockLabels.Value(r.Counted - r.ExpectedQuantity, r.UnitCost)).ToList();
        return new ReportData(
            [
                new("postedAt", "Onay zamanı", ReportColumnType.DateTime),
                new("countNo", "Sayım"),
                new("warehouse", "Depo"),
                new("location", "Adres"),
                new("code", "Kart kodu"),
                new("name", "Ad"),
                new("lotNo", "Top no"),
                new("expected", "Beklenen", ReportColumnType.Decimal),
                new("counted", "Sayılan", ReportColumnType.Decimal),
                new("difference", "Fark", ReportColumnType.Decimal),
                new("unit", "Birim"),
                new("value", "Fark tutarı", ReportColumnType.Decimal, Financial: true),
                new("postedBy", "Onaylayan"),
            ],
            shown
                .Select((r, index) => (IReadOnlyList<object?>)
                [
                    r.PostedAt, r.CountNo, r.WarehouseCode, r.LocationCode, r.Code, r.Name, r.LotNo, r.ExpectedQuantity,
                    r.Counted, r.Counted - r.ExpectedQuantity, StockLabels.Units[r.Unit], values[index], r.PostedBy,
                ])
                .ToList(),
            shown.Count == 0
                ? null
                : ["Toplam", null, null, null, null, null, null, null, null, null, null, values.Sum(v => v ?? 0), null],
            rows.Count > ReportLimits.MaxRows);
    }
}

/// <summary>Depo adreslerinin doluluğu.</summary>
public sealed class LocationOccupancyReport(IKonfeksiyonDbContext db) : IReport
{
    public ReportDefinition Definition { get; } = new(
        "location-occupancy", "Adres doluluk",
        "Depo adresleri ve içlerindeki stok: kaç kart, kaç top var; hangi adresler boş. En fazla 5.000 satır.",
        RequiresDateRange: false, SearchLabel: "Depo ya da adres kodu", Group: StockLabels.Group);

    public async Task<Result<ReportData>> RunAsync(ReportParameters parameters, CancellationToken cancellationToken)
    {
        var locations = db.WarehouseLocations.AsNoTracking();
        if (parameters.Search is { } search)
        {
            locations = locations.Where(l => l.Code.Contains(search) || l.Warehouse.Code.Contains(search));
        }

        var rows = await locations
            .OrderBy(l => l.Warehouse.Code).ThenByDescending(l => l.IsDefault).ThenBy(l => l.Code)
            .Take(ReportLimits.MaxRows + 1)
            .Select(l => new
            {
                WarehouseCode = l.Warehouse.Code,
                l.Code,
                l.Description,
                l.IsActive,
                Items = db.Stocks.Where(s => s.LocationId == l.Id && s.Quantity > 0).Select(s => s.StockItemId).Distinct().Count(),
                Lots = db.Stocks.Count(s => s.LocationId == l.Id && s.Quantity > 0 && s.LotId != null),
            })
            .ToListAsync(cancellationToken);

        var shown = rows.Take(ReportLimits.MaxRows).ToList();
        var occupied = shown.Count(r => r.Items > 0);
        return new ReportData(
            [
                new("warehouse", "Depo"),
                new("location", "Adres"),
                new("description", "Açıklama"),
                new("items", "Kart sayısı", Integer),
                new("lots", "Top sayısı", Integer),
                new("state", "Durum"),
                new("active", "Kullanımda"),
            ],
            shown
                .Select(r => (IReadOnlyList<object?>)
                [
                    r.WarehouseCode, r.Code, r.Description, r.Items, r.Lots, r.Items > 0 ? "Dolu" : "Boş",
                    r.IsActive ? "Evet" : "Pasif",
                ])
                .ToList(),
            shown.Count == 0
                ? null
                : ["Toplam", null, null, shown.Sum(r => r.Items), shown.Sum(r => r.Lots), $"{occupied} / {shown.Count} dolu", null],
            rows.Count > ReportLimits.MaxRows);
    }
}

/// <summary>Seçilen aralıkta kart ve depo bazında giriş, çıkış ve net değişim.</summary>
public sealed class StockMovementSummaryReport(IKonfeksiyonDbContext db) : IReport
{
    public ReportDefinition Definition { get; } = new(
        "stock-movement-summary", "Stok hareket özeti",
        "Seçilen aralıkta kart ve depo bazında toplam giriş, çıkış ve net değişim (depo içi adres değişimi hariç). En fazla 5.000 satır.",
        MaxRangeDays: 92, SearchLabel: "Kart kodu veya adı", Group: StockLabels.Group);

    public async Task<Result<ReportData>> RunAsync(ReportParameters parameters, CancellationToken cancellationToken)
    {
        var transactions = db.StockTransactions
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(t => t.OccurredAt >= parameters.From && t.OccurredAt < parameters.To
                && t.Type != StockTransactionType.RelocationOut && t.Type != StockTransactionType.RelocationIn);
        if (parameters.Search is { } search)
        {
            transactions = transactions.Where(t => t.StockItem.Code.Contains(search) || t.StockItem.Name.Contains(search));
        }

        var rows = await transactions
            .GroupBy(t => new { t.StockItem.Code, t.StockItem.Name, t.StockItem.Unit, WarehouseCode = t.Warehouse.Code })
            .Select(g => new
            {
                g.Key.Code,
                g.Key.Name,
                g.Key.Unit,
                g.Key.WarehouseCode,
                In = g.Sum(t => t.Quantity > 0 ? t.Quantity : 0),
                Out = g.Sum(t => t.Quantity < 0 ? -t.Quantity : 0),
                Count = g.Count(),
            })
            .OrderBy(r => r.Code).ThenBy(r => r.WarehouseCode)
            .Take(ReportLimits.MaxRows + 1)
            .ToListAsync(cancellationToken);

        // Birimler farklı olduğundan miktarların toplamı anlamsızdır; yalnızca hareket sayısı toplanır.
        var shown = rows.Take(ReportLimits.MaxRows).ToList();
        return new ReportData(
            [
                new("code", "Kart kodu"),
                new("name", "Ad"),
                new("warehouse", "Depo"),
                new("in", "Giriş", ReportColumnType.Decimal),
                new("out", "Çıkış", ReportColumnType.Decimal),
                new("net", "Net", ReportColumnType.Decimal),
                new("unit", "Birim"),
                new("movements", "Hareket sayısı", Integer),
            ],
            shown
                .Select(r => (IReadOnlyList<object?>)
                    [r.Code, r.Name, r.WarehouseCode, r.In, r.Out, r.In - r.Out, StockLabels.Units[r.Unit], r.Count])
                .ToList(),
            shown.Count == 0 ? null : ["Toplam", null, null, null, null, null, null, shown.Sum(r => r.Count)],
            rows.Count > ReportLimits.MaxRows);
    }
}

/// <summary>Üretim siparişlerine ayrılmış stok.</summary>
public sealed class StockReservationsReport(IKonfeksiyonDbContext db) : IReport
{
    public ReportDefinition Definition { get; } = new(
        "stock-reservations", "Rezerve stok",
        "Üretim siparişlerine ayrılmış malzemeler: hangi sipariş, hangi depoda, ne kadar. Termini en yakın sipariş üsttedir. En fazla 5.000 satır.",
        RequiresDateRange: false, SearchLabel: "Sipariş no ya da kart", Group: StockLabels.Group);

    public async Task<Result<ReportData>> RunAsync(ReportParameters parameters, CancellationToken cancellationToken)
    {
        var reservations = db.StockReservations.AsNoTracking().IgnoreQueryFilters();
        if (parameters.Search is { } search)
        {
            reservations = reservations.Where(r =>
                r.ProductionOrder.OrderNo.Contains(search) || r.StockItem.Code.Contains(search) || r.StockItem.Name.Contains(search));
        }

        var rows = await reservations
            .OrderBy(r => r.ProductionOrder.DueDate).ThenBy(r => r.ProductionOrder.OrderNo).ThenBy(r => r.StockItem.Code)
            .Take(ReportLimits.MaxRows + 1)
            .Select(r => new
            {
                r.ProductionOrder.OrderNo, r.ProductionOrder.DueDate, r.StockItem.Code, r.StockItem.Name,
                WarehouseCode = r.Warehouse.Code, r.Quantity, r.StockItem.Unit, r.UpdatedAt, r.UpdatedBy,
            })
            .ToListAsync(cancellationToken);

        return new ReportData(
            [
                new("orderNo", "Sipariş"),
                new("dueDate", "Termin", Date),
                new("code", "Kart kodu"),
                new("name", "Ad"),
                new("warehouse", "Depo"),
                new("quantity", "Ayrılan", ReportColumnType.Decimal),
                new("unit", "Birim"),
                new("updatedAt", "Ayrılma zamanı", ReportColumnType.DateTime),
                new("updatedBy", "Ayıran"),
            ],
            rows
                .Take(ReportLimits.MaxRows)
                .Select(r => (IReadOnlyList<object?>)
                [
                    r.OrderNo, r.DueDate, r.Code, r.Name, r.WarehouseCode, r.Quantity, StockLabels.Units[r.Unit], r.UpdatedAt,
                    r.UpdatedBy,
                ])
                .ToList(),
            Truncated: rows.Count > ReportLimits.MaxRows);
    }
}
