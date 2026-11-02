using ERP.Konfeksiyon.Application.Abstractions;
using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Konfeksiyon.Application.Features.MasterData;
using ERP.Core.Domain.Common;
using ERP.Konfeksiyon.Domain.Entities;
using ERP.Konfeksiyon.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ERP.Konfeksiyon.Application.Features.Rfid;

public interface IRfidReadService
{
    /// <summary>Tabletten okutulan personel kartını operatöre çözer.</summary>
    Task<Result<OperatorReadDto>> ReadOperatorAsync(RfidReadRequest request, CancellationToken cancellationToken);

    /// <summary>Tabletten okutulan demet kartını demete çözer.</summary>
    Task<Result<BundleReadDto>> ReadBundleAsync(RfidReadRequest request, CancellationToken cancellationToken);
}

/// <summary>
/// Kart okutmalarını çözer ve kaydeder. Her okutma bir kez işlenir:
/// - aynı okuma kimliği (ReadId) yeniden gelirse önceki sonuç döner (ağ tekrarı / çevrimdışı kuyruk),
/// - aynı tablette aynı kart debounce süresi içinde yeniden okunursa önceki sonuç döner (okuyucunun çift okuması).
/// İleride okutmaya bağlanacak işlemler (oturum açma, operasyon başlatma) da bu kapının arkasında kalır.
/// </summary>
public sealed class RfidReadService(
    IKonfeksiyonDbContext db, ICurrentUser currentUser, IMachineOperationService machineOperations, TimeProvider timeProvider,
    IOptions<RfidOptions> options)
    : IRfidReadService
{
    private readonly RfidOptions _options = options.Value;

    private sealed record ReadingTablet(int Id, int? MachineId);

    /// <summary>Bir okutmanın sonucu: çözülen kayıt ya da reddedilme nedeni.</summary>
    private sealed record Outcome(Error? Error, int? OperatorId = null, int? BundleId = null);

    public async Task<Result<OperatorReadDto>> ReadOperatorAsync(
        RfidReadRequest request, CancellationToken cancellationToken)
    {
        var read = await ProcessAsync(request, RfidReadKind.Operator, ResolveOperatorAsync, cancellationToken);
        if (!read.IsSuccess)
        {
            return read.Error!;
        }

        var (_, outcome, duplicate) = read.Value;
        var machineOperator = await db.Operators
            .AsNoTracking()
            .Where(o => o.Id == outcome.OperatorId)
            .Select(o => new OperatorReadDto(duplicate, o.Id, o.OperatorCode, o.FirstName + " " + o.LastName))
            .SingleAsync(cancellationToken);

        return machineOperator;
    }

    public async Task<Result<BundleReadDto>> ReadBundleAsync(RfidReadRequest request, CancellationToken cancellationToken)
    {
        var read = await ProcessAsync(request, RfidReadKind.Bundle, ResolveBundleAsync, cancellationToken);
        if (!read.IsSuccess)
        {
            return read.Error!;
        }

        var (tablet, outcome, duplicate) = read.Value;
        var bundle = await db.Bundles
            .AsNoTracking()
            .Where(b => b.Id == outcome.BundleId)
            .Select(b => new
            {
                b.Id,
                b.BundleNo,
                b.ProductionOrder.OrderNo,
                b.ProductionOrder.Product.Model.ModelCode,
                Color = b.Color.Name,
                Size = b.Size.Name,
                b.Quantity,
                b.Status,
                Operations = b.Operations
                    .OrderBy(o => o.SequenceNo)
                    .Select(o => new
                    {
                        o.Id, o.SequenceNo, o.Operation.Name, o.OperationId, o.Status, o.PlannedQuantity,
                        o.ProducedQuantity, o.RejectedQuantity, o.ReworkQuantity, o.Sam,
                    })
                    .ToList(),
            })
            .SingleAsync(cancellationToken);

        IReadOnlyList<int> capable = tablet.MachineId is { } machineId
            ? await machineOperations.CapableOperationIdsAsync(machineId, cancellationToken)
            : [];
        // Demet fiziksel olarak tek yerdedir: işlemde bir adımı varken başka adımı başlatılamaz.
        var workable = bundle.Status is BundleStatus.Waiting or BundleStatus.InProduction
            or BundleStatus.PartiallyCompleted or BundleStatus.Rework;
        var busy = bundle.Operations.Any(o => o.Status == BundleOperationStatus.InProgress);
        // Yarım bırakılan operasyon tamamlanmadan başka operasyona geçilmez (tamir adedi sayılmaz; bkz. Bundle.UnfinishedOperations).
        var unfinished = bundle.Operations
            .Where(o => o.Status == BundleOperationStatus.Paused
                && o.PlannedQuantity - o.ProducedQuantity - o.RejectedQuantity - o.ReworkQuantity > 0)
            .Select(o => o.Id)
            .ToList();
        var operations = bundle.Operations
            .Select(o =>
            {
                var canDo = capable.Contains(o.OperationId);
                var canStart = canDo && workable && (o.Status == BundleOperationStatus.InProgress
                    || (!busy && o.Status is BundleOperationStatus.Ready or BundleOperationStatus.Paused
                        && (unfinished.Count == 0 || unfinished.Contains(o.Id))));
                return new BundleReadOperationDto(
                    o.Id, o.SequenceNo, o.Name, o.Status, o.PlannedQuantity, o.ProducedQuantity, o.RejectedQuantity,
                    o.PlannedQuantity - o.ProducedQuantity - o.RejectedQuantity, canDo, canStart, o.Sam);
            })
            .ToList();
        var machineOperation = operations.FirstOrDefault(o => o.CanStart)
            ?? operations.FirstOrDefault(o => o.MachineCanDo && o.Status != BundleOperationStatus.Completed)
            ?? operations.FirstOrDefault(o => o.MachineCanDo);

        return new BundleReadDto(
            duplicate, bundle.Id, bundle.BundleNo, bundle.OrderNo, bundle.ModelCode, bundle.Color, bundle.Size,
            bundle.Quantity, bundle.Status, machineOperation, operations);
    }

    /// <summary>
    /// Okutmayı bir kez işler ve kaydeder; tekrar ise kayıtlı sonucu döner. Başarılıysa tablet, sonuç ve
    /// tekrar olup olmadığı; okutma reddedildiyse (ilk seferde de tekrarında da) aynı hata döner.
    /// </summary>
    private async Task<Result<(ReadingTablet Tablet, Outcome Outcome, bool Duplicate)>> ProcessAsync(
        RfidReadRequest request,
        RfidReadKind kind,
        Func<string, CancellationToken, Task<Outcome>> resolve,
        CancellationToken cancellationToken)
    {
        var uid = RfidUid.Normalize(request.Uid);
        if (uid is null)
        {
            return new Error(ErrorCodes.InvalidRfidUid, RfidValidation.InvalidUidMessage, ErrorType.Validation);
        }

        var tablet = await db.Tablets
            .AsNoTracking()
            .Where(t => t.Id == currentUser.TabletId && t.IsActive)
            .Select(t => new ReadingTablet(t.Id, t.MachineId))
            .SingleOrDefaultAsync(cancellationToken);
        if (tablet is null)
        {
            return new Error(
                ErrorCodes.InvalidDeviceCredentials, "Cihaz kimlik bilgileri geçersiz.", ErrorType.Unauthorized);
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var previous = await FindPreviousAsync(request.ReadId, tablet.Id, uid, kind, now, cancellationToken);
        if (previous is not null)
        {
            return Replay(tablet, previous);
        }

        var outcome = await resolve(uid, cancellationToken);
        db.RfidReadLogs.Add(new RfidReadLog
        {
            ReadId = request.ReadId,
            TabletId = tablet.Id,
            Uid = uid,
            Kind = kind,
            ResultCode = outcome.Error?.Code ?? RfidReadLog.Ok,
            OperatorId = outcome.OperatorId,
            BundleId = outcome.BundleId,
            ReadAt = now,
        });

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Aynı okuma kimliği eşzamanlı olarak başka bir istekle kaydedildi: bu istek tekrardır.
            var winner = await db.RfidReadLogs.AsNoTracking().SingleOrDefaultAsync(l => l.ReadId == request.ReadId, cancellationToken);
            if (winner is null)
            {
                throw;
            }

            return Replay(tablet, winner);
        }

        if (outcome.Error is not null)
        {
            return outcome.Error;
        }

        return (tablet, outcome, false);
    }

    private async Task<RfidReadLog?> FindPreviousAsync(
        Guid readId, int tabletId, string uid, RfidReadKind kind, DateTime now, CancellationToken cancellationToken)
    {
        var sameRead = await db.RfidReadLogs.AsNoTracking().SingleOrDefaultAsync(l => l.ReadId == readId, cancellationToken);
        if (sameRead is not null || _options.DebounceSeconds == 0)
        {
            return sameRead;
        }

        var debounceFrom = now.AddSeconds(-_options.DebounceSeconds);
        return await db.RfidReadLogs
            .AsNoTracking()
            .Where(l => l.TabletId == tabletId && l.Uid == uid && l.Kind == kind && l.ReadAt > debounceFrom)
            .OrderByDescending(l => l.ReadAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static Result<(ReadingTablet, Outcome, bool)> Replay(ReadingTablet tablet, RfidReadLog log)
    {
        if (log.ResultCode != RfidReadLog.Ok)
        {
            // İlk yanıtla aynı kod ve aynı HTTP durumu dönsün.
            return new Error(
                log.ResultCode,
                "Bu kart okutması az önce reddedildi.",
                log.ResultCode == ErrorCodes.RfidNotRegistered ? ErrorType.NotFound : ErrorType.BusinessRule);
        }

        return (tablet, new Outcome(null, log.OperatorId, log.BundleId), true);
    }

    private async Task<Outcome> ResolveOperatorAsync(string uid, CancellationToken cancellationToken)
    {
        var card = await FindCardAsync(uid, RfidCardType.Operator, cancellationToken);
        if (!card.IsSuccess)
        {
            return new Outcome(card.Error);
        }

        var owner = await db.OperatorCards
            .AsNoTracking()
            .Where(c => c.RfidCardId == card.Value && c.RevokedAt == null)
            .Select(c => new { c.OperatorId, c.Operator.IsActive })
            .SingleOrDefaultAsync(cancellationToken);
        if (owner is null)
        {
            return new Outcome(new Error(ErrorCodes.RfidCardNotAssigned, "Bu kart bir operatöre atanmamış."));
        }

        return owner.IsActive
            ? new Outcome(null, OperatorId: owner.OperatorId)
            : new Outcome(new Error(ErrorCodes.OperatorInactive, "Operatör pasif durumda."), OperatorId: owner.OperatorId);
    }

    private async Task<Outcome> ResolveBundleAsync(string uid, CancellationToken cancellationToken)
    {
        var card = await FindCardAsync(uid, RfidCardType.Bundle, cancellationToken);
        if (!card.IsSuccess)
        {
            return new Outcome(card.Error);
        }

        var bundle = await db.BundleCardAssignments
            .AsNoTracking()
            .Where(a => a.RfidCardId == card.Value && a.ReleasedAt == null)
            .Select(a => new { a.BundleId, a.Bundle.Status })
            .SingleOrDefaultAsync(cancellationToken);
        if (bundle is null)
        {
            return new Outcome(new Error(ErrorCodes.RfidCardNotAssigned, "Bu kart bir demete atanmamış."));
        }

        return bundle.Status is BundleStatus.Cancelled or BundleStatus.Closed
            ? new Outcome(new Error(ErrorCodes.BundleNotActive, "Demet iptal edilmiş ya da kapatılmış."), BundleId: bundle.BundleId)
            : new Outcome(null, BundleId: bundle.BundleId);
    }

    /// <summary>Kartın kayıtlı, aktif ve beklenen türde olduğunu doğrular; kart Id'sini döner.</summary>
    private async Task<Result<int>> FindCardAsync(string uid, RfidCardType expectedType, CancellationToken cancellationToken)
    {
        var card = await db.RfidCards
            .AsNoTracking()
            .Where(c => c.Uid == uid)
            .Select(c => new { c.Id, c.IsActive, c.CardType })
            .SingleOrDefaultAsync(cancellationToken);

        if (card is null)
        {
            return new Error(ErrorCodes.RfidNotRegistered, "Kart sistemde tanımlı değil.", ErrorType.NotFound);
        }

        if (!card.IsActive)
        {
            return new Error(ErrorCodes.RfidCardInactive, "Kart pasif durumda.");
        }

        if (card.CardType != expectedType)
        {
            return new Error(
                ErrorCodes.RfidCardTypeMismatch,
                expectedType == RfidCardType.Operator
                    ? "Bu bir demet kartı; personel kartı okutun."
                    : "Bu bir personel kartı; demet kartı okutun.");
        }

        return card.Id;
    }
}
