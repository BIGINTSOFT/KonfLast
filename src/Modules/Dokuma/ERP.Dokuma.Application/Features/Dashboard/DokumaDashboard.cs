using ERP.Core.Application.Abstractions;
using ERP.Dokuma.Domain.Entities;
using ERP.Dokuma.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ERP.Dokuma.Application.Features.Dashboard;

public sealed record DokumaDashboardStopDto(string LoomCode, string LoomName, DokumaStopCategory Category, string ReasonName, DateTime Since, string? BeamNo);

/// <param name="ShippedMeters">Onaylı irsaliyeyle ya da Boyahane'ye gönderilen.</param>
public sealed record DokumaDashboardLateOrderDto(int OrderId, string OrderNo, string? CustomerName, DateOnly DueDate, decimal OrderedMeters, decimal ShippedMeters);

/// <param name="RunningLooms">Üzerinde levent olan ve durmayan tezgâh.</param>
/// <param name="IdleLooms">Leventi ve açık duruşu olmayan tezgâh.</param>
/// <param name="ReadyBeams">Tezgâha bağlanmayı bekleyen levent.</param>
/// <param name="PackingListsToShip">Onaylanmış, henüz sevk edilmemiş çeki listesi.</param>
public sealed record DokumaDashboardDto(
    int Looms,
    int RunningLooms,
    int StoppedLooms,
    int BreakdownLooms,
    int IdleLooms,
    int ReadyBeams,
    decimal WovenTodayMeters,
    int RollsToday,
    int PendingInspectionRolls,
    int PackingListsToShip,
    int LateOrderCount,
    IReadOnlyList<DokumaDashboardStopDto> Stops,
    IReadOnlyList<DokumaDashboardLateOrderDto> LateOrders);

public interface IDokumaDashboardService
{
    Task<DokumaDashboardDto> GetAsync(CancellationToken cancellationToken);
}

/// <summary>Canlı pano: tezgâh durumu, bugünkü üretim, bekleyen işler ve dikkat isteyen kayıtlar.</summary>
public sealed class DokumaDashboardService(IAppDbContext db, TimeProvider timeProvider) : IDokumaDashboardService
{
    private const int MaxLateOrders = 10;

    public async Task<DokumaDashboardDto> GetAsync(CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);

        var looms = await db.Set<DokumaLoom>().AsNoTracking().Where(l => l.IsActive).Select(l => l.Id).ToListAsync(cancellationToken);
        var loaded = await db.Set<DokumaBeam>().AsNoTracking().Where(b => b.Status == BeamStatus.OnLoom && b.LoomId != null).Select(b => b.LoomId!.Value).ToListAsync(cancellationToken);
        // Yalnızca etkin tezgâhların açık duruşları sayılır: çalışan + boşta + duran + arızalı = tezgâh sayısı.
        var openStops = db.Set<DokumaLoomStop>().AsNoTracking().Where(s => s.EndedAt == null && s.Loom.IsActive);
        var stops = await openStops
            .OrderBy(s => s.StartedAt)
            .Select(s => new DokumaDashboardStopDto(s.Loom.Code, s.Loom.Name, s.Category, s.Reason.Name, s.StartedAt, s.Beam != null ? s.Beam.BeamNo : null))
            .ToListAsync(cancellationToken);
        // Tezgâh başına bir kez: birden çok açık duruşu varsa arıza önceliklidir.
        var stoppedLooms = (await openStops.Select(s => new { s.LoomId, s.Category }).ToListAsync(cancellationToken))
            .GroupBy(s => s.LoomId)
            .ToDictionary(g => g.Key, g => g.Any(s => s.Category == DokumaStopCategory.Breakdown));
        var stoppedIds = stoppedLooms.Keys.ToHashSet();

        var todayRolls = db.Set<DokumaFabricRoll>().AsNoTracking().Where(r => r.CutOn == today);
        var late = db.Set<DokumaOrder>().AsNoTracking()
            .Where(o => o.Status == OrderStatus.Confirmed && o.DueDate < today)
            .Select(o => new
            {
                o.Id,
                o.OrderNo,
                CustomerName = o.Customer != null ? o.Customer.Name : null,
                DueDate = o.DueDate!.Value,
                Ordered = o.Lines.Sum(l => (decimal?)l.Meters) ?? 0,
                Shipped = db.Set<DokumaPackingItem>()
                    .Where(i => i.IsActive
                        && i.Roll.Beam.WarpOrder.OrderLine.OrderId == o.Id
                        && i.PackingList.Status == PackingListStatus.Confirmed
                        && (i.PackingList.BoyahaneReceiptNo != null || (i.PackingList.Despatch != null && i.PackingList.Despatch.Status == DespatchStatus.Confirmed)))
                    .Sum(i => (decimal?)i.Meters) ?? 0,
            })
            .Where(o => o.Shipped < o.Ordered);

        var lateRows = await late.OrderBy(o => o.DueDate).ThenBy(o => o.Id).Take(MaxLateOrders).ToListAsync(cancellationToken);

        return new DokumaDashboardDto(
            looms.Count,
            looms.Count(id => loaded.Contains(id) && !stoppedIds.Contains(id)),
            stoppedLooms.Count(s => !s.Value),
            stoppedLooms.Count(s => s.Value),
            looms.Count(id => !loaded.Contains(id) && !stoppedIds.Contains(id)),
            await db.Set<DokumaBeam>().CountAsync(b => b.Status == BeamStatus.Ready, cancellationToken),
            await todayRolls.SumAsync(r => (decimal?)r.Meters, cancellationToken) ?? 0,
            await todayRolls.CountAsync(cancellationToken),
            await db.Set<DokumaFabricRoll>().CountAsync(r => r.Status == FabricRollStatus.PendingInspection, cancellationToken),
            await db.Set<DokumaPackingList>().CountAsync(l => l.Status == PackingListStatus.Confirmed && l.DespatchId == null && l.BoyahaneReceiptNo == null, cancellationToken),
            await late.CountAsync(cancellationToken),
            stops,
            lateRows.Select(o => new DokumaDashboardLateOrderDto(o.Id, o.OrderNo, o.CustomerName, o.DueDate, o.Ordered, o.Shipped)).ToList());
    }
}
