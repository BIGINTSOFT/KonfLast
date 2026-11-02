using ERP.Core.Application.Abstractions;
using ERP.Dokuma.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERP.Dokuma.Application.Features.Receipts;

/// <summary>
/// İplik lotlarından yapılan çıkışlar: çözgüye (çözgü emri) ve atkıya (tezgâhtaki levent). Lotun kalanı kilo − iki çıkışın
/// toplamıdır.
/// </summary>
public static class DokumaYarnStock
{
    /// <summary>Lotta çıkış ya da geri alım kaydı var mı; varsa lot silinmez, ipliği ve sahibi değişmez.</summary>
    public static async Task<bool> HasMovementsAsync(IAppDbContext db, int lotId, CancellationToken cancellationToken) =>
        await db.Set<DokumaWarpIssue>().AnyAsync(i => i.YarnLotId == lotId, cancellationToken)
        || await db.Set<DokumaWeftIssue>().AnyAsync(i => i.YarnLotId == lotId, cancellationToken)
        || await db.Set<DokumaYarnReturn>().AnyAsync(r => r.YarnLotId == lotId, cancellationToken);

    /// <summary>Lottan çıkan net toplam: çözgü, atkı ve müşteriye iade, üretimden geri alınan düşülmüş.</summary>
    public static async Task<decimal> IssuedFromLotAsync(IAppDbContext db, int lotId, CancellationToken cancellationToken) =>
        (await db.Set<DokumaWarpIssue>().Where(i => i.YarnLotId == lotId).SumAsync(i => (decimal?)i.Kilograms, cancellationToken) ?? 0)
        + (await db.Set<DokumaWeftIssue>().Where(i => i.YarnLotId == lotId).SumAsync(i => (decimal?)i.Kilograms, cancellationToken) ?? 0)
        + (await db.Set<DokumaYarnReturn>().Where(r => r.YarnLotId == lotId).SumAsync(r => (decimal?)r.Kilograms, cancellationToken) ?? 0)
        - (await db.Set<DokumaYarnTakeBack>().Where(t => t.YarnLotId == lotId).SumAsync(t => (decimal?)t.Kilograms, cancellationToken) ?? 0);

    /// <summary>Malın sahibinin (boşsa işletme) lotlarından iplik başına çıkan toplam.</summary>
    public static async Task<Dictionary<int, decimal>> ConsumedByYarnAsync(
        IAppDbContext db, IReadOnlyCollection<int> yarnIds, int? owner, CancellationToken cancellationToken)
    {
        var warp = await db.Set<DokumaWarpIssue>().AsNoTracking()
            .Where(i => yarnIds.Contains(i.YarnLot.YarnId) && i.YarnLot.CustomerId == owner)
            .GroupBy(i => i.YarnLot.YarnId)
            .Select(g => new { YarnId = g.Key, Kilograms = g.Sum(i => i.Kilograms) })
            .ToListAsync(cancellationToken);
        var weft = await db.Set<DokumaWeftIssue>().AsNoTracking()
            .Where(i => yarnIds.Contains(i.YarnLot.YarnId) && i.YarnLot.CustomerId == owner)
            .GroupBy(i => i.YarnLot.YarnId)
            .Select(g => new { YarnId = g.Key, Kilograms = g.Sum(i => i.Kilograms) })
            .ToListAsync(cancellationToken);

        var returned = await db.Set<DokumaYarnReturn>().AsNoTracking()
            .Where(r => yarnIds.Contains(r.YarnLot.YarnId) && r.YarnLot.CustomerId == owner)
            .GroupBy(r => r.YarnLot.YarnId)
            .Select(g => new { YarnId = g.Key, Kilograms = g.Sum(r => r.Kilograms) })
            .ToListAsync(cancellationToken);

        var taken = await db.Set<DokumaYarnTakeBack>().AsNoTracking()
            .Where(t => yarnIds.Contains(t.YarnLot.YarnId) && t.YarnLot.CustomerId == owner)
            .GroupBy(t => t.YarnLot.YarnId)
            .Select(g => new { YarnId = g.Key, Kilograms = -g.Sum(t => t.Kilograms) })
            .ToListAsync(cancellationToken);

        return warp.Concat(weft).Concat(returned).Concat(taken).GroupBy(x => x.YarnId).ToDictionary(g => g.Key, g => g.Sum(x => x.Kilograms));
    }

    /// <summary>Siparişlere iplik başına verilen net toplam (çözgü + atkı − üretimden geri alınan).</summary>
    public static async Task<Dictionary<(int OrderId, int YarnId), decimal>> IssuedByOrderAsync(
        IAppDbContext db, IReadOnlyCollection<int> orderIds, IReadOnlyCollection<int> yarnIds, CancellationToken cancellationToken)
    {
        var warp = await db.Set<DokumaWarpIssue>().AsNoTracking()
            .Where(i => orderIds.Contains(i.WarpOrder.OrderLine.OrderId) && yarnIds.Contains(i.YarnLot.YarnId))
            .GroupBy(i => new { i.WarpOrder.OrderLine.OrderId, i.YarnLot.YarnId })
            .Select(g => new { g.Key.OrderId, g.Key.YarnId, Kilograms = g.Sum(i => i.Kilograms) })
            .ToListAsync(cancellationToken);
        var weft = await db.Set<DokumaWeftIssue>().AsNoTracking()
            .Where(i => orderIds.Contains(i.Beam.WarpOrder.OrderLine.OrderId) && yarnIds.Contains(i.YarnLot.YarnId))
            .GroupBy(i => new { i.Beam.WarpOrder.OrderLine.OrderId, i.YarnLot.YarnId })
            .Select(g => new { g.Key.OrderId, g.Key.YarnId, Kilograms = g.Sum(i => i.Kilograms) })
            .ToListAsync(cancellationToken);

        var takenWarp = await db.Set<DokumaYarnTakeBack>().AsNoTracking()
            .Where(t => t.WarpOrderId != null && orderIds.Contains(t.WarpOrder!.OrderLine.OrderId) && yarnIds.Contains(t.YarnLot.YarnId))
            .GroupBy(t => new { t.WarpOrder!.OrderLine.OrderId, t.YarnLot.YarnId })
            .Select(g => new { g.Key.OrderId, g.Key.YarnId, Kilograms = -g.Sum(t => t.Kilograms) })
            .ToListAsync(cancellationToken);
        var takenWeft = await db.Set<DokumaYarnTakeBack>().AsNoTracking()
            .Where(t => t.BeamId != null && orderIds.Contains(t.Beam!.WarpOrder.OrderLine.OrderId) && yarnIds.Contains(t.YarnLot.YarnId))
            .GroupBy(t => new { t.Beam!.WarpOrder.OrderLine.OrderId, t.YarnLot.YarnId })
            .Select(g => new { g.Key.OrderId, g.Key.YarnId, Kilograms = -g.Sum(t => t.Kilograms) })
            .ToListAsync(cancellationToken);

        return warp.Concat(weft).Concat(takenWarp).Concat(takenWeft).GroupBy(x => (x.OrderId, x.YarnId)).ToDictionary(g => g.Key, g => g.Sum(x => x.Kilograms));
    }
}
