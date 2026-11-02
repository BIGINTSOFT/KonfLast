using ERP.Core.Application.Abstractions;
using ERP.Orme.Domain.Entities;
using ERP.Orme.Domain.Enums;
using ERP.Orme.Domain.Rules;
using Microsoft.EntityFrameworkCore;

namespace ERP.Orme.Application.Features.Purchasing;

/// <summary>Bir ipliğin ihtiyacındaki sipariş; ihtiyaç tarihine göre stok ve yoldaki siparişle sırayla karşılanır.</summary>
/// <param name="FabricCodes">Siparişin bu ipliği kullanan kumaşları.</param>
/// <param name="RequiredKilograms">Açık satırların fireli ihtiyacı.</param>
/// <param name="IssuedKilograms">Bu satırlara verilen net iplik (geri alınan düşülmüş).</param>
/// <param name="RemainingKilograms">Satır başına ihtiyaç − verilen, sıfırın altına inmeden.</param>
/// <param name="CoveredKilograms">Termin sırasıyla eldeki stok ve yoldaki satın almadan karşılanan kısım.</param>
public sealed record OrmeYarnPlanOrderDto(
    int OrderId,
    string OrderNo,
    string? CustomerName,
    DateOnly? DueDate,
    string FabricCodes,
    decimal RequiredKilograms,
    decimal IssuedKilograms,
    decimal RemainingKilograms,
    decimal CoveredKilograms);

/// <summary>İplik ihtiyaç planının bir satırı: bir iplik ve malın sahibi (fasonda müşteri, kendi malında işletme).</summary>
/// <param name="OwnerCustomerId">Fasonda ipliğin sahibi müşteri; işletme malında boş.</param>
/// <param name="CustomerSupplied">İpliği müşteri getirir; satın alınmaz, eksik müşteriden beklenir.</param>
/// <param name="NetKilograms">Açık satırların fire hariç ihtiyacı.</param>
/// <param name="RequiredKilograms">Kumaş kartındaki fire eklenmiş ihtiyaç.</param>
/// <param name="IssuedKilograms">Açık satırlara verilmiş net iplik.</param>
/// <param name="RemainingKilograms">Henüz verilmemiş ihtiyaç.</param>
/// <param name="OnHandKilograms">Malın sahibine ait lotların kalanı.</param>
/// <param name="FreeKilograms">Eldeki − kalan ihtiyaç; eksi ise stok yetmiyor.</param>
/// <param name="OpenPurchaseKilograms">Onaylı ve teslimatı süren satın alma siparişlerinin kalanı (yalnız işletme malı).</param>
/// <param name="DraftPurchaseKilograms">Taslak satın alma siparişlerindeki miktar; öneriden düşülür.</param>
/// <param name="ShortageKilograms">Kalan ihtiyaç − eldeki − yoldaki; sıfırdan küçükse sıfır.</param>
/// <param name="SuggestedKilograms">Satın alınması önerilen: eksik − taslaklar. Müşteri ipliğinde her zaman sıfır.</param>
/// <param name="NeededBy">Eksik varsa karşılanamayan ilk siparişin termini; yoksa ihtiyacı olan siparişlerin en erken termini.</param>
/// <param name="SupplierId">Önerilen tedarikçi: ipliğin son satın alındığı, yoksa son giriş yapılan etkin tedarikçi.</param>
/// <param name="OrderBy">Sipariş verilmesi gereken son gün: ihtiyaç tarihi − tedarikçinin teslim süresi.</param>
/// <param name="OrderOverdue">Sipariş verme tarihi geçmiş.</param>
public sealed record OrmeYarnPlanRowDto(
    int YarnId,
    string YarnCode,
    string YarnName,
    string? YarnColor,
    int? OwnerCustomerId,
    string? OwnerName,
    bool CustomerSupplied,
    decimal NetKilograms,
    decimal RequiredKilograms,
    decimal IssuedKilograms,
    decimal RemainingKilograms,
    decimal OnHandKilograms,
    decimal FreeKilograms,
    decimal OpenPurchaseKilograms,
    decimal DraftPurchaseKilograms,
    decimal ShortageKilograms,
    decimal SuggestedKilograms,
    DateOnly? NeededBy,
    int? SupplierId,
    string? SupplierName,
    int? LeadTimeDays,
    DateOnly? OrderBy,
    bool OrderOverdue,
    IReadOnlyList<OrmeYarnPlanOrderDto> Orders);

/// <param name="MissingFabrics">İplik tanımı olmadığı için hesaba katılamayan kumaşlar.</param>
public sealed record OrmeYarnPlanDto(DateOnly Today, IReadOnlyList<OrmeYarnPlanRowDto> Rows, IReadOnlyList<string> MissingFabrics);

public interface IOrmeYarnPlanService
{
    Task<OrmeYarnPlanDto> GetAsync(CancellationToken cancellationToken);
}

/// <summary>
/// İplik ihtiyaç planı (bütün siparişler). Onaylı siparişlerin açık satırları sayılır: örülen kilo sipariş kilosuna ulaşan ya da bütün
/// emirleri bitmiş ve planı sipariş kilosunu karşılayan satır kapanmış kabul edilir. İhtiyaç sipariş satırı kilosunun fireli iplik
/// karşılığıdır; satıra verilen iplik düşülür. İplik ve malın sahibi başına toplanır, eldeki stok ve yoldaki satın alma ile karşılaştırılır.
/// </summary>
public sealed class OrmeYarnPlanService(IAppDbContext db, TimeProvider timeProvider) : IOrmeYarnPlanService
{
    public async Task<OrmeYarnPlanDto> GetAsync(CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(timeProvider.GetUtcNow().UtcDateTime, timeProvider.LocalTimeZone));
        var knitOrders = db.Set<OrmeKnitOrder>();
        var rolls = db.Set<OrmeFabricRoll>();

        var lines = await db.Set<OrmeOrderLine>().AsNoTracking()
            .Where(l => l.Order.Status == OrderStatus.Confirmed)
            .Select(l => new
            {
                l.Id,
                l.OrderId,
                l.Order.OrderNo,
                l.Order.DueDate,
                l.Order.Ownership,
                l.Order.CustomerId,
                CustomerName = l.Order.Customer != null ? l.Order.Customer.Name : null,
                l.Kilograms,
                l.Fabric.FabricCode,
                l.Fabric.WastePercent,
                Yarns = l.Fabric.Yarns.Select(y => new { y.YarnId, y.Percent }).ToList(),
                Planned = knitOrders.Where(k => k.OrderLineId == l.Id && k.Status != KnitOrderStatus.Cancelled).Sum(k => (decimal?)k.PlannedKilograms) ?? 0,
                HasKnitOrders = knitOrders.Any(k => k.OrderLineId == l.Id && k.Status != KnitOrderStatus.Cancelled),
                HasOpenKnitOrders = knitOrders.Any(k => k.OrderLineId == l.Id && k.Status == KnitOrderStatus.Open),
                Knitted = rolls.Where(r => r.KnitOrder.OrderLineId == l.Id).Sum(r => (decimal?)r.Kilograms) ?? 0,
            })
            .ToListAsync(cancellationToken);

        // Kapanmış satır: örülen kilo siparişe ulaştı ya da emirlerin tamamı bitti ve plan siparişi karşılıyor.
        var open = lines
            .Where(l => l.Knitted < l.Kilograms && !(l.HasKnitOrders && !l.HasOpenKnitOrders && l.Planned >= l.Kilograms))
            .ToList();
        var missing = open.Where(l => l.Yarns.Count == 0).Select(l => l.FabricCode).Distinct().OrderBy(code => code).ToList();

        var openIds = open.Select(l => l.Id).ToList();
        var issuedRows = await db.Set<OrmeYarnIssue>().AsNoTracking()
            .Where(i => openIds.Contains(i.KnitOrder.OrderLineId))
            .GroupBy(i => new { i.KnitOrder.OrderLineId, i.YarnLot.YarnId })
            .Select(g => new { g.Key.OrderLineId, g.Key.YarnId, Kilograms = g.Sum(i => i.Kilograms) })
            .ToListAsync(cancellationToken);
        var takenRows = await db.Set<OrmeYarnTakeBack>().AsNoTracking()
            .Where(t => openIds.Contains(t.KnitOrder.OrderLineId))
            .GroupBy(t => new { t.KnitOrder.OrderLineId, t.YarnLot.YarnId })
            .Select(g => new { g.Key.OrderLineId, g.Key.YarnId, Kilograms = -g.Sum(t => t.Kilograms) })
            .ToListAsync(cancellationToken);
        var issued = issuedRows.Concat(takenRows)
            .GroupBy(x => (x.OrderLineId, x.YarnId))
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Kilograms));

        // Satır × iplik ihtiyacı; aynı iplik kumaşta iki kez geçiyorsa oranları toplanır.
        var needs = open
            .SelectMany(l => l.Yarns
                .GroupBy(y => y.YarnId)
                .Select(g =>
                {
                    var net = YarnRequirement.NetKilograms(l.Kilograms, g.Sum(y => y.Percent));
                    var required = YarnRequirement.WithWaste(net, l.WastePercent);
                    var given = issued.GetValueOrDefault((l.Id, g.Key));
                    return new Need(
                        g.Key,
                        l.Ownership == OrderOwnership.Commission ? l.CustomerId : null,
                        l.CustomerName,
                        l.OrderId,
                        l.OrderNo,
                        l.DueDate,
                        l.FabricCode,
                        net,
                        required,
                        given,
                        Math.Max(0, required - given));
                }))
            .ToList();

        var yarnIds = needs.Select(n => n.YarnId).Distinct().ToList();
        if (yarnIds.Count == 0)
        {
            return new OrmeYarnPlanDto(today, [], missing);
        }

        // Kartı sonradan silinen iplik de ihtiyaçta görünür.
        var yarns = await db.Set<OrmeYarn>().AsNoTracking().IgnoreQueryFilters()
            .Where(y => yarnIds.Contains(y.Id))
            .Select(y => new { y.Id, y.YarnCode, y.Name, y.Color })
            .ToDictionaryAsync(y => y.Id, cancellationToken);

        var lots = await db.Set<OrmeYarnLot>().AsNoTracking()
            .Where(l => yarnIds.Contains(l.YarnId))
            .Select(l => new
            {
                l.YarnId,
                l.CustomerId,
                Remaining = l.Kilograms - (l.Issues.Sum(i => (decimal?)i.Kilograms) ?? 0) + (l.TakeBacks.Sum(t => (decimal?)t.Kilograms) ?? 0) - (l.Returns.Sum(r => (decimal?)r.Kilograms) ?? 0),
            })
            .ToListAsync(cancellationToken);
        var onHand = lots.GroupBy(l => (l.YarnId, l.CustomerId)).ToDictionary(g => g.Key, g => g.Sum(l => l.Remaining));

        var purchases = await db.Set<OrmePurchaseOrderLine>().AsNoTracking()
            .Where(l => yarnIds.Contains(l.YarnId)
                && (l.Order.Status == PurchaseOrderStatus.Draft || l.Order.Status == PurchaseOrderStatus.Approved || l.Order.Status == PurchaseOrderStatus.PartiallyReceived))
            .Select(l => new { l.YarnId, l.Order.Status, l.Kilograms, Received = l.Lots.Sum(x => (decimal?)x.Kilograms) ?? 0 })
            .ToListAsync(cancellationToken);
        var onOrder = purchases.Where(p => p.Status != PurchaseOrderStatus.Draft)
            .GroupBy(p => p.YarnId).ToDictionary(g => g.Key, g => g.Sum(p => Math.Max(0, p.Kilograms - p.Received)));
        var drafts = purchases.Where(p => p.Status == PurchaseOrderStatus.Draft)
            .GroupBy(p => p.YarnId).ToDictionary(g => g.Key, g => g.Sum(p => p.Kilograms));

        var preferred = await PreferredSuppliersAsync(yarnIds, cancellationToken);

        var rows = new List<OrmeYarnPlanRowDto>();
        foreach (var group in needs.GroupBy(n => (n.YarnId, n.Owner)))
        {
            var remaining = Round(group.Sum(n => n.Remaining));
            if (remaining <= 0)
            {
                continue;
            }

            var ownGoods = group.Key.Owner is null;
            var have = Round(onHand.GetValueOrDefault((group.Key.YarnId, group.Key.Owner)));
            var coming = ownGoods ? Round(onOrder.GetValueOrDefault(group.Key.YarnId)) : 0;
            var draft = ownGoods ? Round(drafts.GetValueOrDefault(group.Key.YarnId)) : 0;

            // Termin sırasıyla (termini olmayan sonda) önce eldeki, sonra yoldaki iplikle karşılanır.
            var left = Math.Max(0, have) + coming;
            var orders = group
                .GroupBy(n => n.OrderId)
                .Select(g => new
                {
                    g.Key,
                    g.First().OrderNo,
                    g.First().CustomerName,
                    g.First().DueDate,
                    Fabrics = string.Join(", ", g.Select(n => n.FabricCode).Distinct().Order()),
                    Required = Round(g.Sum(n => n.Required)),
                    Issued = Round(g.Sum(n => n.Issued)),
                    Remaining = Round(g.Sum(n => n.Remaining)),
                })
                .OrderBy(o => o.DueDate is null).ThenBy(o => o.DueDate).ThenBy(o => o.OrderNo)
                .Select(o =>
                {
                    var covered = Math.Min(o.Remaining, left);
                    left -= covered;
                    return new OrmeYarnPlanOrderDto(o.Key, o.OrderNo, o.CustomerName, o.DueDate, o.Fabrics, o.Required, o.Issued, o.Remaining, covered);
                })
                .ToList();

            var shortage = Math.Max(0, remaining - Math.Max(0, have) - coming);
            var earliest = orders.Where(o => o.RemainingKilograms > 0).Min(o => o.DueDate);
            var neededBy = shortage > 0 ? orders.FirstOrDefault(o => o.CoveredKilograms < o.RemainingKilograms)?.DueDate ?? earliest : earliest;

            var supplier = ownGoods ? preferred.GetValueOrDefault(group.Key.YarnId) : null;
            DateOnly? orderBy = ownGoods && shortage > 0 && neededBy is { } needed ? needed.AddDays(-(supplier?.LeadTimeDays ?? 0)) : null;
            var yarn = yarns.GetValueOrDefault(group.Key.YarnId);

            rows.Add(new OrmeYarnPlanRowDto(
                group.Key.YarnId,
                yarn?.YarnCode ?? string.Empty,
                yarn?.Name ?? string.Empty,
                yarn?.Color,
                group.Key.Owner,
                ownGoods ? null : group.First().CustomerName,
                !ownGoods,
                Round(group.Sum(n => n.Net)),
                Round(group.Sum(n => n.Required)),
                Round(group.Sum(n => n.Issued)),
                remaining,
                have,
                have - remaining,
                coming,
                draft,
                shortage,
                ownGoods ? Math.Max(0, shortage - draft) : 0,
                neededBy,
                supplier?.Id,
                supplier?.Name,
                supplier?.LeadTimeDays,
                orderBy,
                orderBy is { } last && last < today,
                orders));
        }

        var sorted = rows
            .OrderByDescending(r => r.ShortageKilograms > 0)
            .ThenBy(r => r.OrderBy ?? r.NeededBy ?? DateOnly.MaxValue)
            .ThenBy(r => r.YarnCode)
            .ThenBy(r => r.OwnerName)
            .ToList();
        return new OrmeYarnPlanDto(today, sorted, missing);
    }

    /// <summary>İpliğin önerilen tedarikçisi: son satın alma siparişinin, yoksa son işletme malı girişinin etkin tedarikçisi.</summary>
    private async Task<Dictionary<int, PreferredSupplier>> PreferredSuppliersAsync(IReadOnlyCollection<int> yarnIds, CancellationToken cancellationToken)
    {
        var fromPurchases = await db.Set<OrmePurchaseOrderLine>().AsNoTracking()
            .Where(l => yarnIds.Contains(l.YarnId) && l.Order.Status != PurchaseOrderStatus.Cancelled)
            .Select(l => new { l.YarnId, l.Order.SupplierId, Date = l.Order.OrderDate, l.Id })
            .ToListAsync(cancellationToken);
        var fromLots = await db.Set<OrmeYarnLot>().AsNoTracking()
            .Where(l => yarnIds.Contains(l.YarnId) && l.CustomerId == null && l.SupplierId != null)
            .Select(l => new { l.YarnId, SupplierId = l.SupplierId!.Value, Date = l.ReceiptDate, l.Id })
            .ToListAsync(cancellationToken);

        var supplierIds = fromPurchases.Select(p => p.SupplierId).Concat(fromLots.Select(l => l.SupplierId)).Distinct().ToList();
        var suppliers = await db.Set<OrmeSupplier>().AsNoTracking()
            .Where(s => supplierIds.Contains(s.Id) && s.IsActive)
            .Select(s => new PreferredSupplier(s.Id, s.Name, s.LeadTimeDays))
            .ToDictionaryAsync(s => s.Id, cancellationToken);

        var result = new Dictionary<int, PreferredSupplier>();
        foreach (var yarnId in yarnIds)
        {
            var last = fromPurchases.Where(p => p.YarnId == yarnId && suppliers.ContainsKey(p.SupplierId))
                    .OrderByDescending(p => p.Date).ThenByDescending(p => p.Id).Select(p => (int?)p.SupplierId).FirstOrDefault()
                ?? fromLots.Where(l => l.YarnId == yarnId && suppliers.ContainsKey(l.SupplierId))
                    .OrderByDescending(l => l.Date).ThenByDescending(l => l.Id).Select(l => (int?)l.SupplierId).FirstOrDefault();
            if (last is { } supplierId)
            {
                result[yarnId] = suppliers[supplierId];
            }
        }

        return result;
    }

    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private sealed record Need(
        int YarnId, int? Owner, string? CustomerName, int OrderId, string OrderNo, DateOnly? DueDate, string FabricCode,
        decimal Net, decimal Required, decimal Issued, decimal Remaining);

    private sealed record PreferredSupplier(int Id, string Name, int? LeadTimeDays);
}
