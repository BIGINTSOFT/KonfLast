using ERP.Konfeksiyon.Application.Abstractions;
using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Konfeksiyon.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERP.Konfeksiyon.Application.Features.ProductionLines;

public interface ILineMachineService
{
    Task<Result<LineOverviewDto>> GetOverviewAsync(int lineId, CancellationToken cancellationToken);

    /// <summary>Makineyi bantın sonuna ekler; istenirse başka banttan taşır.</summary>
    Task<Result<LineOverviewDto>> AssignAsync(
        int lineId, AssignMachineRequest request, CancellationToken cancellationToken);

    Task<Result<LineOverviewDto>> UnassignAsync(int lineId, int machineId, CancellationToken cancellationToken);

    Task<Result<LineOverviewDto>> ReorderAsync(
        int lineId, ReorderMachinesRequest request, CancellationToken cancellationToken);
}

public sealed class LineMachineService(
    IKonfeksiyonDbContext db, IAppLock appLock, ILinePlanService planService, TimeProvider timeProvider) : ILineMachineService
{
    private const int UpcomingPlanCount = 10;
    private static readonly TimeSpan LockTimeout = TimeSpan.FromSeconds(10);

    private static readonly Error LineNotFound = new(ErrorCodes.NotFound, "Bant bulunamadı.", ErrorType.NotFound);

    private static readonly Error Busy = new(
        ErrorCodes.ResourceBusy, "İşlem şu an başka bir kullanıcı tarafından yapılıyor. Lütfen tekrar deneyin.",
        ErrorType.Conflict);

    private DateTime UtcNow => timeProvider.GetUtcNow().UtcDateTime;

    public async Task<Result<LineOverviewDto>> GetOverviewAsync(int lineId, CancellationToken cancellationToken)
    {
        var line = await db.ProductionLines
            .AsNoTracking()
            .Where(l => l.Id == lineId)
            .Select(l => new { l.Id, l.LineCode, l.Name, Area = l.ProductionArea.Name, Factory = l.ProductionArea.Factory.Name, l.IsActive })
            .SingleOrDefaultAsync(cancellationToken);
        if (line is null)
        {
            return LineNotFound;
        }

        var machines = await db.LineMachineAssignments
            .AsNoTracking()
            .Where(a => a.ProductionLineId == lineId && a.UnassignedAt == null)
            .OrderBy(a => a.Position).ThenBy(a => a.Id)
            .Select(a => new LineMachineDto(
                a.MachineId,
                a.Position,
                a.Machine.MachineCode,
                a.Machine.MachineName,
                a.Machine.MachineType.Name,
                a.Machine.Operation!.Name,
                a.Machine.Status,
                a.Machine.IsActive,
                a.AssignedAt))
            .ToListAsync(cancellationToken);

        var (current, upcoming) = await planService.GetCurrentAndUpcomingAsync(lineId, UpcomingPlanCount, cancellationToken);

        return new LineOverviewDto(
            line.Id, line.LineCode, line.Name, line.Area, line.Factory, line.IsActive, machines, current, upcoming);
    }

    public async Task<Result<LineOverviewDto>> AssignAsync(
        int lineId, AssignMachineRequest request, CancellationToken cancellationToken)
    {
        var line = await db.ProductionLines
            .Where(l => l.Id == lineId)
            .Select(l => new { l.LineCode, l.IsActive })
            .SingleOrDefaultAsync(cancellationToken);
        if (line is null)
        {
            return LineNotFound;
        }

        if (!line.IsActive)
        {
            return ReferenceValidation.InvalidReference("lineId", "Pasif banta makine atanamaz.");
        }

        var machineError = await ReferenceValidation.RequireSelectableAsync(
            db.Machines, request.MachineId, null, "machineId", "Makine", cancellationToken);
        if (machineError is not null)
        {
            return machineError;
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        // Aynı makine için eşzamanlı atamalar sıraya girer; kural ayrıca unique index ile de korunur.
        if (!await appLock.TryAcquireAsync($"machine-assignment:{request.MachineId}", LockTimeout, cancellationToken))
        {
            return Busy;
        }

        var now = UtcNow;
        var current = await db.LineMachineAssignments
            .Include(a => a.ProductionLine)
            .SingleOrDefaultAsync(a => a.MachineId == request.MachineId && a.UnassignedAt == null, cancellationToken);

        if (current is not null)
        {
            if (current.ProductionLineId == lineId)
            {
                return new Error(ErrorCodes.MachineAlreadyOnLine, "Makine zaten bu bantta.", ErrorType.Conflict);
            }

            if (!request.Transfer)
            {
                return new Error(
                    ErrorCodes.MachineAlreadyAssigned,
                    $"Makine şu an {current.ProductionLine.LineCode} bantında. Bu banta taşımak için onaylayın.",
                    ErrorType.Conflict);
            }

            current.End(now);
            // Unique index'e takılmamak için eski atama yenisinden önce kapatılır.
            await db.SaveChangesAsync(cancellationToken);
        }

        var lastPosition = await db.LineMachineAssignments
            .Where(a => a.ProductionLineId == lineId && a.UnassignedAt == null)
            .MaxAsync(a => (int?)a.Position, cancellationToken) ?? 0;

        db.LineMachineAssignments.Add(new LineMachineAssignment
        {
            ProductionLineId = lineId, MachineId = request.MachineId, Position = lastPosition + 1, AssignedAt = now,
        });

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return await GetOverviewAsync(lineId, cancellationToken);
    }

    public async Task<Result<LineOverviewDto>> UnassignAsync(
        int lineId, int machineId, CancellationToken cancellationToken)
    {
        var assignment = await db.LineMachineAssignments.SingleOrDefaultAsync(
            a => a.ProductionLineId == lineId && a.MachineId == machineId && a.UnassignedAt == null, cancellationToken);
        if (assignment is null)
        {
            return new Error(ErrorCodes.NotFound, "Makine bu bantta değil.", ErrorType.NotFound);
        }

        // Kayıt silinmez; makinenin bu bantta hangi tarihler arasında durduğu geçmişte kalır.
        assignment.End(UtcNow);
        await db.SaveChangesAsync(cancellationToken);

        return await GetOverviewAsync(lineId, cancellationToken);
    }

    public async Task<Result<LineOverviewDto>> ReorderAsync(
        int lineId, ReorderMachinesRequest request, CancellationToken cancellationToken)
    {
        if (!await db.ProductionLines.AnyAsync(l => l.Id == lineId, cancellationToken))
        {
            return LineNotFound;
        }

        var assignments = await db.LineMachineAssignments
            .Where(a => a.ProductionLineId == lineId && a.UnassignedAt == null)
            .ToListAsync(cancellationToken);

        if (!assignments.Select(a => a.MachineId).ToHashSet().SetEquals(request.MachineIds))
        {
            const string message = "Liste, bantta şu an bulunan makinelerin tamamını içermelidir.";
            return new Error(
                ErrorCodes.ValidationError,
                message,
                ErrorType.Validation,
                new Dictionary<string, string[]> { ["machineIds"] = [message] });
        }

        var byMachine = assignments.ToDictionary(a => a.MachineId);
        for (var index = 0; index < request.MachineIds.Count; index++)
        {
            byMachine[request.MachineIds[index]].Position = index + 1;
        }

        await db.SaveChangesAsync(cancellationToken);
        return await GetOverviewAsync(lineId, cancellationToken);
    }
}
