using ERP.Core.Application.Abstractions;
using ERP.Orme.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERP.Orme.Application.Features.Receipts;

/// <summary>İplik lotlarından yapılan çıkışlar: lotun kalanı kilo − örme emrine verilen + geri alınan − müşteriye iade.</summary>
public static class OrmeYarnStock
{
    /// <summary>Lot hareketlerinin kilidi; kalan hesaplanıp yazılana kadar tutulur.</summary>
    public const string YarnLotLock = "OrmeYarnLots";

    /// <summary>Lotta çıkış kaydı var mı; varsa lot silinmez, ipliği ve sahibi değişmez.</summary>
    public static async Task<bool> HasMovementsAsync(IAppDbContext db, int lotId, CancellationToken cancellationToken) =>
        await db.Set<OrmeYarnIssue>().AnyAsync(i => i.YarnLotId == lotId, cancellationToken)
        || await db.Set<OrmeYarnReturn>().AnyAsync(r => r.YarnLotId == lotId, cancellationToken);

    /// <summary>Lottan çıkan net toplam: verilen ve müşteriye iade, geri alınan düşülmüş.</summary>
    public static async Task<decimal> IssuedFromLotAsync(IAppDbContext db, int lotId, CancellationToken cancellationToken) =>
        (await db.Set<OrmeYarnIssue>().Where(i => i.YarnLotId == lotId).SumAsync(i => (decimal?)i.Kilograms, cancellationToken) ?? 0)
        + (await db.Set<OrmeYarnReturn>().Where(r => r.YarnLotId == lotId).SumAsync(r => (decimal?)r.Kilograms, cancellationToken) ?? 0)
        - (await db.Set<OrmeYarnTakeBack>().Where(t => t.YarnLotId == lotId).SumAsync(t => (decimal?)t.Kilograms, cancellationToken) ?? 0);

    /// <summary>Malın sahibinin (boşsa işletme) lotlarından iplik başına çıkan net toplam.</summary>
    public static async Task<Dictionary<int, decimal>> ConsumedByYarnAsync(
        IAppDbContext db, IReadOnlyCollection<int> yarnIds, int? owner, CancellationToken cancellationToken)
    {
        var issued = await db.Set<OrmeYarnIssue>().AsNoTracking()
            .Where(i => yarnIds.Contains(i.YarnLot.YarnId) && i.YarnLot.CustomerId == owner)
            .GroupBy(i => i.YarnLot.YarnId)
            .Select(g => new { YarnId = g.Key, Kilograms = g.Sum(i => i.Kilograms) })
            .ToListAsync(cancellationToken);
        var taken = await db.Set<OrmeYarnTakeBack>().AsNoTracking()
            .Where(t => yarnIds.Contains(t.YarnLot.YarnId) && t.YarnLot.CustomerId == owner)
            .GroupBy(t => t.YarnLot.YarnId)
            .Select(g => new { YarnId = g.Key, Kilograms = -g.Sum(t => t.Kilograms) })
            .ToListAsync(cancellationToken);
        var returned = await db.Set<OrmeYarnReturn>().AsNoTracking()
            .Where(r => yarnIds.Contains(r.YarnLot.YarnId) && r.YarnLot.CustomerId == owner)
            .GroupBy(r => r.YarnLot.YarnId)
            .Select(g => new { YarnId = g.Key, Kilograms = g.Sum(r => r.Kilograms) })
            .ToListAsync(cancellationToken);

        return issued.Concat(taken).Concat(returned).GroupBy(x => x.YarnId).ToDictionary(g => g.Key, g => g.Sum(x => x.Kilograms));
    }

    /// <summary>Siparişlere iplik başına verilen net toplam; <paramref name="yarnIds"/> boşsa (null) bütün iplikler.</summary>
    public static async Task<Dictionary<(int OrderId, int YarnId), decimal>> IssuedByOrderAsync(
        IAppDbContext db, IReadOnlyCollection<int> orderIds, IReadOnlyCollection<int>? yarnIds, CancellationToken cancellationToken)
    {
        var issued = await db.Set<OrmeYarnIssue>().AsNoTracking()
            .Where(i => orderIds.Contains(i.KnitOrder.OrderLine.OrderId) && (yarnIds == null || yarnIds.Contains(i.YarnLot.YarnId)))
            .GroupBy(i => new { i.KnitOrder.OrderLine.OrderId, i.YarnLot.YarnId })
            .Select(g => new { g.Key.OrderId, g.Key.YarnId, Kilograms = g.Sum(i => i.Kilograms) })
            .ToListAsync(cancellationToken);
        var taken = await db.Set<OrmeYarnTakeBack>().AsNoTracking()
            .Where(t => orderIds.Contains(t.KnitOrder.OrderLine.OrderId) && (yarnIds == null || yarnIds.Contains(t.YarnLot.YarnId)))
            .GroupBy(t => new { t.KnitOrder.OrderLine.OrderId, t.YarnLot.YarnId })
            .Select(g => new { g.Key.OrderId, g.Key.YarnId, Kilograms = -g.Sum(t => t.Kilograms) })
            .ToListAsync(cancellationToken);

        return issued.Concat(taken).GroupBy(x => (x.OrderId, x.YarnId)).ToDictionary(g => g.Key, g => g.Sum(x => x.Kilograms));
    }
}
