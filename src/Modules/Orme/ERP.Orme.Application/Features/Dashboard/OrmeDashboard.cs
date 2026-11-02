using ERP.Core.Application.Abstractions;
using ERP.Orme.Domain.Entities;
using ERP.Orme.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ERP.Orme.Application.Features.Dashboard;

public sealed record OrmeDashboardStopDto(string MachineCode, string MachineName, OrmeStopCategory Category, string ReasonName, DateTime Since, string? KnitOrderNo);

/// <param name="ShippedKilograms">Onaylı irsaliyeyle ya da Boyahane'ye gönderilen.</param>
public sealed record OrmeDashboardLateOrderDto(int OrderId, string OrderNo, string? CustomerName, DateOnly DueDate, decimal OrderedKilograms, decimal ShippedKilograms);

/// <param name="RunningMachines">Üzerinde emir olan ve durmayan makine.</param>
/// <param name="IdleMachines">Emri ve açık duruşu olmayan makine.</param>
/// <param name="PackingListsToShip">Onaylanmış, henüz sevk edilmemiş çeki listesi.</param>
public sealed record OrmeDashboardDto(
    int Machines,
    int RunningMachines,
    int StoppedMachines,
    int BreakdownMachines,
    int IdleMachines,
    int OpenKnitOrders,
    decimal KnittedTodayKilograms,
    int RollsToday,
    int PendingInspectionRolls,
    int PackingListsToShip,
    int LateOrderCount,
    IReadOnlyList<OrmeDashboardStopDto> Stops,
    IReadOnlyList<OrmeDashboardLateOrderDto> LateOrders);

public interface IOrmeDashboardService
{
    Task<OrmeDashboardDto> GetAsync(CancellationToken cancellationToken);
}

/// <summary>Canlı pano: makine durumu, bugünkü üretim, bekleyen işler ve dikkat isteyen kayıtlar.</summary>
public sealed class OrmeDashboardService(IAppDbContext db, TimeProvider timeProvider) : IOrmeDashboardService
{
    private const int MaxLateOrders = 10;

    public async Task<OrmeDashboardDto> GetAsync(CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);

        var machines = await db.Set<OrmeMachine>().AsNoTracking().Where(m => m.IsActive).Select(m => m.Id).ToListAsync(cancellationToken);
        var running = await db.Set<OrmeKnitOrderMachine>().AsNoTracking().Where(m => m.ReleasedOn == null).Select(m => m.MachineId).ToListAsync(cancellationToken);
        // Yalnızca etkin makineler: çalışan + boşta + duran + arızalı = makine sayısı.
        var openStops = db.Set<OrmeMachineStop>().AsNoTracking().Where(s => s.EndedAt == null && s.Machine.IsActive);
        var stops = await openStops
            .OrderBy(s => s.StartedAt)
            .Select(s => new OrmeDashboardStopDto(s.Machine.Code, s.Machine.Name, s.Category, s.Reason.Name, s.StartedAt, s.KnitOrder != null ? s.KnitOrder.KnitOrderNo : null))
            .ToListAsync(cancellationToken);

        // Makine bir kez sayılır, açık duruşunun türüyle; birden çok açık duruş varsa arıza öne geçer.
        var stoppedMachines = (await openStops.Select(s => new { s.MachineId, s.Category }).ToListAsync(cancellationToken))
            .GroupBy(s => s.MachineId)
            .ToDictionary(g => g.Key, g => g.Any(s => s.Category == OrmeStopCategory.Breakdown));
        var stoppedIds = stoppedMachines.Keys.ToHashSet();

        var todayRolls = db.Set<OrmeFabricRoll>().AsNoTracking().Where(r => r.CutOn == today);
        var late = db.Set<OrmeOrder>().AsNoTracking()
            .Where(o => o.Status == OrderStatus.Confirmed && o.DueDate < today)
            .Select(o => new
            {
                o.Id,
                o.OrderNo,
                CustomerName = o.Customer != null ? o.Customer.Name : null,
                DueDate = o.DueDate!.Value,
                Ordered = o.Lines.Sum(l => (decimal?)l.Kilograms) ?? 0,
                Shipped = db.Set<OrmePackingItem>()
                    .Where(i => i.IsActive
                        && i.Roll.KnitOrder.OrderLine.OrderId == o.Id
                        && i.PackingList.Status == PackingListStatus.Confirmed
                        && (i.PackingList.BoyahaneReceiptNo != null || (i.PackingList.Despatch != null && i.PackingList.Despatch.Status == DespatchStatus.Confirmed)))
                    .Sum(i => (decimal?)i.Kilograms) ?? 0,
            })
            .Where(o => o.Shipped < o.Ordered);

        var lateRows = await late.OrderBy(o => o.DueDate).ThenBy(o => o.Id).Take(MaxLateOrders).ToListAsync(cancellationToken);

        return new OrmeDashboardDto(
            machines.Count,
            machines.Count(id => running.Contains(id) && !stoppedIds.Contains(id)),
            stoppedMachines.Count(m => !m.Value),
            stoppedMachines.Count(m => m.Value),
            machines.Count(id => !running.Contains(id) && !stoppedIds.Contains(id)),
            await db.Set<OrmeKnitOrder>().CountAsync(k => k.Status == KnitOrderStatus.Open, cancellationToken),
            await todayRolls.SumAsync(r => (decimal?)r.Kilograms, cancellationToken) ?? 0,
            await todayRolls.CountAsync(cancellationToken),
            await db.Set<OrmeFabricRoll>().CountAsync(r => r.Status == FabricRollStatus.PendingInspection, cancellationToken),
            await db.Set<OrmePackingList>().CountAsync(l => l.Status == PackingListStatus.Confirmed && l.DespatchId == null && l.BoyahaneReceiptNo == null, cancellationToken),
            await late.CountAsync(cancellationToken),
            stops,
            lateRows.Select(o => new OrmeDashboardLateOrderDto(o.Id, o.OrderNo, o.CustomerName, o.DueDate, o.Ordered, o.Shipped)).ToList());
    }
}
