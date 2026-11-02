using ERP.Konfeksiyon.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Konfeksiyon.Domain.Entities;
using Microsoft.EntityFrameworkCore;

using ERP.Core.Application.Features.MasterData;

namespace ERP.Konfeksiyon.Application.Features.MasterData;

/// <param name="Operations">Makinenin ana operasyonuna ek olarak yapabildiği operasyonlar.</param>
public sealed record MachineOperationsDto(int MachineId, IReadOnlyList<LookupDto> Operations);

public sealed record UpdateMachineOperationsRequest(IReadOnlyList<int> OperationIds);

public interface IMachineOperationService
{
    Task<Result<MachineOperationsDto>> GetAsync(int machineId, CancellationToken cancellationToken);

    /// <summary>Makinenin ek operasyonlarını verilen listeyle değiştirir.</summary>
    Task<Result<MachineOperationsDto>> SaveAsync(
        int machineId, UpdateMachineOperationsRequest request, CancellationToken cancellationToken);

    /// <summary>Makinenin yapabildiği tüm operasyonlar: ana operasyonu ve ek operasyonları.</summary>
    Task<IReadOnlyList<int>> CapableOperationIdsAsync(int machineId, CancellationToken cancellationToken);
}

public sealed class MachineOperationService(IKonfeksiyonDbContext db) : IMachineOperationService
{
    private static readonly Error NotFound = new(ErrorCodes.NotFound, "Makine bulunamadı.", ErrorType.NotFound);

    public async Task<Result<MachineOperationsDto>> GetAsync(int machineId, CancellationToken cancellationToken)
    {
        if (!await db.Machines.AnyAsync(m => m.Id == machineId, cancellationToken))
        {
            return NotFound;
        }

        var operations = await db.MachineOperations
            .AsNoTracking()
            .Where(o => o.MachineId == machineId)
            .OrderBy(o => o.Operation.OperationCode)
            .Select(o => new LookupDto(o.Operation.Id, o.Operation.OperationCode, o.Operation.Name))
            .ToListAsync(cancellationToken);

        return new MachineOperationsDto(machineId, operations);
    }

    public async Task<Result<MachineOperationsDto>> SaveAsync(
        int machineId, UpdateMachineOperationsRequest request, CancellationToken cancellationToken)
    {
        var machine = await db.Machines
            .Where(m => m.Id == machineId)
            .Select(m => new { m.MachineTypeId, m.OperationId })
            .SingleOrDefaultAsync(cancellationToken);
        if (machine is null)
        {
            return NotFound;
        }

        // Ana operasyon zaten yapılabilir; listede ayrıca tutulmaz.
        var wanted = (request.OperationIds ?? []).Distinct().Where(id => id != machine.OperationId).ToList();
        var current = await db.MachineOperations.Where(o => o.MachineId == machineId).ToListAsync(cancellationToken);
        var added = wanted.Except(current.Select(o => o.OperationId)).ToList();

        // Yeni eklenen operasyon aktif olmalı ve makinenin tipine uymalı; önceden tanımlı olanlar korunur.
        var selectable = await db.Operations
            .Where(o => added.Contains(o.Id) && o.IsActive)
            .Select(o => new { o.Id, o.Name, o.MachineTypeId })
            .ToListAsync(cancellationToken);
        if (selectable.Count != added.Count)
        {
            return Invalid("Seçilen operasyonlardan biri bulunamadı ya da pasif.");
        }

        var mismatch = selectable.FirstOrDefault(o => o.MachineTypeId is not null && o.MachineTypeId != machine.MachineTypeId);
        if (mismatch is not null)
        {
            return new Error(
                ErrorCodes.MachineTypeMismatch, $"'{mismatch.Name}' operasyonu farklı bir makine tipi gerektiriyor.",
                ErrorType.BusinessRule,
                new Dictionary<string, string[]>
                {
                    ["operationIds"] = [$"'{mismatch.Name}' operasyonu farklı bir makine tipi gerektiriyor."],
                });
        }

        db.MachineOperations.RemoveRange(current.Where(o => !wanted.Contains(o.OperationId)));
        db.MachineOperations.AddRange(added.Select(id => new MachineOperation { MachineId = machineId, OperationId = id }));
        await db.SaveChangesAsync(cancellationToken);

        return await GetAsync(machineId, cancellationToken);
    }

    public async Task<IReadOnlyList<int>> CapableOperationIdsAsync(int machineId, CancellationToken cancellationToken)
    {
        var primary = await db.Machines
            .Where(m => m.Id == machineId)
            .Select(m => m.OperationId)
            .SingleOrDefaultAsync(cancellationToken);
        var extra = await db.MachineOperations
            .Where(o => o.MachineId == machineId)
            .Select(o => o.OperationId)
            .ToListAsync(cancellationToken);

        return primary is { } id ? extra.Prepend(id).Distinct().ToList() : extra;
    }

    private static Error Invalid(string message) => new(
        ErrorCodes.InvalidReference, message, ErrorType.Validation,
        new Dictionary<string, string[]> { ["operationIds"] = [message] });
}
