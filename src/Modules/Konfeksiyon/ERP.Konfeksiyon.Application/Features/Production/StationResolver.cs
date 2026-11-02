using ERP.Konfeksiyon.Application.Abstractions;
using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Konfeksiyon.Application.Features.MasterData;
using ERP.Konfeksiyon.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ERP.Konfeksiyon.Application.Features.Production;

/// <summary>İsteği yapan tablet ve bağlı olduğu makine.</summary>
/// <param name="OperationId">Makinenin ana operasyonu.</param>
/// <param name="OperationIds">Makinenin yapabildiği tüm operasyonlar (ana operasyon ve ek operasyonlar).</param>
public sealed record Station(
    int TabletId, string TabletCode, int MachineId, string MachineCode, bool MachineActive, MachineStatus MachineStatus,
    int? OperationId, string? OperationName, IReadOnlyList<int> OperationIds);

public interface IStationResolver
{
    /// <summary>
    /// Tablet token'ından istasyonu çözer. Tablet pasifse / silinmişse INVALID_DEVICE_CREDENTIALS,
    /// makineye bağlı değilse TABLET_NOT_ASSIGNED döner.
    /// </summary>
    Task<Result<Station>> ResolveAsync(CancellationToken cancellationToken);
}

public sealed class StationResolver(IKonfeksiyonDbContext db, ICurrentUser currentUser, IMachineOperationService machineOperations)
    : IStationResolver
{
    public async Task<Result<Station>> ResolveAsync(CancellationToken cancellationToken)
    {
        var tablet = await db.Tablets
            .AsNoTracking()
            .Where(t => t.Id == currentUser.TabletId && t.IsActive)
            .Select(t => new
            {
                t.Id,
                t.TabletCode,
                t.MachineId,
                t.Machine!.MachineCode,
                IsActive = (bool?)t.Machine.IsActive,
                Status = (MachineStatus?)t.Machine.Status,
                t.Machine.OperationId,
                OperationName = t.Machine.Operation!.Name,
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (tablet is null)
        {
            return new Error(
                ErrorCodes.InvalidDeviceCredentials, "Cihaz kimlik bilgileri geçersiz.", ErrorType.Unauthorized);
        }

        if (tablet.MachineId is not { } machineId || tablet.IsActive is null)
        {
            return new Error(ErrorCodes.TabletNotAssigned, "Tablet henüz bir makineye bağlanmamış.");
        }

        return new Station(
            tablet.Id, tablet.TabletCode, machineId, tablet.MachineCode, tablet.IsActive.Value, tablet.Status!.Value,
            tablet.OperationId, tablet.OperationName,
            await machineOperations.CapableOperationIdsAsync(machineId, cancellationToken));
    }
}
