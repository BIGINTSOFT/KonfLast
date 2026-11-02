using ERP.Konfeksiyon.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Core.Application.Features.MasterData;
using ERP.Konfeksiyon.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ERP.Konfeksiyon.Application.Features.WorkSessions;

public interface IOperatorQualificationService
{
    Task<Result<OperatorQualificationsDto>> GetAsync(int operatorId, CancellationToken cancellationToken);

    /// <summary>Operatörün yetkinliklerini verilen listelerle değiştirir.</summary>
    Task<Result<OperatorQualificationsDto>> ReplaceAsync(
        int operatorId, OperatorQualificationsRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Operatör bu makinede (ve verilmişse bu operasyonda) çalışabilir mi? Çalışamıyorsa nedeni döner.
    /// </summary>
    Task<Error?> CheckAsync(int operatorId, int machineId, int? operationId, CancellationToken cancellationToken);
}

public sealed class OperatorQualificationService(IKonfeksiyonDbContext db, IOptions<ProductionOptions> options)
    : IOperatorQualificationService
{
    private static readonly Error NotFound = new(ErrorCodes.NotFound, "Operatör bulunamadı.", ErrorType.NotFound);

    public async Task<Result<OperatorQualificationsDto>> GetAsync(int operatorId, CancellationToken cancellationToken)
    {
        var name = await db.Operators
            .Where(o => o.Id == operatorId)
            .Select(o => o.FirstName + " " + o.LastName)
            .SingleOrDefaultAsync(cancellationToken);
        if (name is null)
        {
            return NotFound;
        }

        // Sonradan silinen operasyon / makine listede görünmez (sorgu filtresi); kayıt geçmişte kalır.
        var operations = await db.OperatorOperationQualifications
            .Where(q => q.OperatorId == operatorId)
            .OrderBy(q => q.Operation.OperationCode)
            .Select(q => new LookupDto(q.OperationId, q.Operation.OperationCode, q.Operation.Name))
            .ToListAsync(cancellationToken);
        var machines = await db.OperatorMachines
            .Where(m => m.OperatorId == operatorId)
            .OrderBy(m => m.Machine.MachineCode)
            .Select(m => new LookupDto(m.MachineId, m.Machine.MachineCode, m.Machine.MachineName))
            .ToListAsync(cancellationToken);

        return new OperatorQualificationsDto(operatorId, name, operations, machines);
    }

    public async Task<Result<OperatorQualificationsDto>> ReplaceAsync(
        int operatorId, OperatorQualificationsRequest request, CancellationToken cancellationToken)
    {
        if (!await db.Operators.AnyAsync(o => o.Id == operatorId, cancellationToken))
        {
            return NotFound;
        }

        var operationIds = request.OperationIds.Distinct().ToList();
        var machineIds = (request.MachineIds ?? []).Distinct().ToList();

        var currentOperations = await db.OperatorOperationQualifications
            .Where(q => q.OperatorId == operatorId)
            .ToListAsync(cancellationToken);
        var currentMachines = await db.OperatorMachines.Where(m => m.OperatorId == operatorId).ToListAsync(cancellationToken);

        var operationError = await ReferenceValidation.RequireSelectableAsync(
            db.Operations, operationIds, currentOperations.Select(q => q.OperationId).ToHashSet(), "operationIds",
            "Operasyon", cancellationToken);
        if (operationError is not null)
        {
            return operationError;
        }

        var machineError = await ReferenceValidation.RequireSelectableAsync(
            db.Machines, machineIds, currentMachines.Select(m => m.MachineId).ToHashSet(), "machineIds", "Makine",
            cancellationToken);
        if (machineError is not null)
        {
            return machineError;
        }

        // Yalnızca farklar yazılır: değişmeyen yetkinlik denetim izinde "silindi / eklendi" görünmez.
        db.OperatorOperationQualifications.RemoveRange(currentOperations.Where(q => !operationIds.Contains(q.OperationId)));
        db.OperatorOperationQualifications.AddRange(operationIds
            .Except(currentOperations.Select(q => q.OperationId))
            .Select(id => new OperatorOperationQualification { OperatorId = operatorId, OperationId = id }));

        db.OperatorMachines.RemoveRange(currentMachines.Where(m => !machineIds.Contains(m.MachineId)));
        db.OperatorMachines.AddRange(machineIds
            .Except(currentMachines.Select(m => m.MachineId))
            .Select(id => new OperatorMachine { OperatorId = operatorId, MachineId = id }));

        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(operatorId, cancellationToken);
    }

    public async Task<Error?> CheckAsync(
        int operatorId, int machineId, int? operationId, CancellationToken cancellationToken)
    {
        if (!options.Value.RequireOperatorQualification)
        {
            return null;
        }

        var machineIds = await db.OperatorMachines
            .Where(m => m.OperatorId == operatorId)
            .Select(m => m.MachineId)
            .ToListAsync(cancellationToken);
        if (machineIds.Count > 0 && !machineIds.Contains(machineId))
        {
            return new Error(ErrorCodes.OperatorNotQualified, "Operatör bu makinede çalışmaya yetkili değil.");
        }

        if (operationId is null
            || await db.OperatorOperationQualifications.AnyAsync(
                q => q.OperatorId == operatorId && q.OperationId == operationId, cancellationToken))
        {
            return null;
        }

        var operationName = await db.Operations
            .IgnoreQueryFilters()
            .Where(o => o.Id == operationId)
            .Select(o => o.Name)
            .SingleOrDefaultAsync(cancellationToken);
        return new Error(
            ErrorCodes.OperatorNotQualified, $"Operatörün '{operationName}' operasyonu için yetkinliği yok.");
    }
}
