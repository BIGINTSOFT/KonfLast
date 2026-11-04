using ERP.Core.Application.Abstractions;
using ERP.Konfeksiyon.Application.Abstractions;
using ERP.Konfeksiyon.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERP.Konfeksiyon.Application.Features.Production;

/// <summary>
/// Konfeksiyon kayıtlarının ait olduğu makine / tablet: makinedeki tablet bu değişiklik duyurulunca ekranını
/// yeniler (ör. web'den elle adet bildirimi, arıza kapatma, oturum kapatma, tabletin başka makineye bağlanması).
/// </summary>
public sealed class KonfeksiyonLiveTargets : ILiveTargetSource
{
    /// <summary>Tablet token'ındaki modül adı.</summary>
    public const string Module = "Konfeksiyon";

    public IEnumerable<LiveTarget> TargetsOf(object entity) => entity switch
    {
        BundleOperationEvent e => [Machine(e.MachineId)],
        BundleOperation { MachineId: { } machineId } => [Machine(machineId)],
        WorkSession s => [Machine(s.MachineId)],
        MachineStatusLog l => [Machine(l.MachineId)],
        Domain.Entities.Machine m => [Machine(m.Id)],
        LineMachineAssignment a => [Machine(a.MachineId)],
        Tablet t => [new LiveTarget(Module, LiveTargetKind.Tablet, t.Id)],
        _ => [],
    };

    private static LiveTarget Machine(int machineId) => new(Module, LiveTargetKind.Machine, machineId);
}

/// <summary>Makinelere bağlı Konfeksiyon tabletleri.</summary>
public sealed class KonfeksiyonLiveTablets(IKonfeksiyonDbContext db) : ILiveTabletDirectory
{
    public string Module => KonfeksiyonLiveTargets.Module;

    public async Task<IReadOnlyList<int>> TabletsOfMachinesAsync(
        IReadOnlyCollection<int> machineIds, CancellationToken cancellationToken) =>
        await db.Tablets.AsNoTracking()
            .Where(t => t.MachineId != null && machineIds.Contains(t.MachineId.Value))
            .Select(t => t.Id)
            .ToListAsync(cancellationToken);
}
