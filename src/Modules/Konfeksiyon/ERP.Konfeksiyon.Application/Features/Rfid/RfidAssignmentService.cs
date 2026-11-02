using ERP.Konfeksiyon.Application.Abstractions;
using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Core.Domain.Common;
using ERP.Konfeksiyon.Domain.Entities;
using ERP.Konfeksiyon.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ERP.Konfeksiyon.Application.Features.Rfid;

public interface IRfidAssignmentService
{
    /// <summary>Demet kartını demete atar; demet üretime hazır (Bekliyor) duruma geçer.</summary>
    Task<Result> AssignToBundleAsync(int bundleId, string uid, CancellationToken cancellationToken);

    /// <summary>Demetin kartını bırakır; kart başka bir demete atanabilir.</summary>
    Task<Result> ReleaseFromBundleAsync(int bundleId, CancellationToken cancellationToken);

    /// <summary>Okutulan demet kartını ve bağlı olduğu demeti bulur (kart boşaltma ekranı).</summary>
    Task<Result<BundleCardDto>> GetBundleCardAsync(string uid, CancellationToken cancellationToken);

    /// <summary>
    /// Kart boşaltma: okutulan demet kartının demetle bağını kaldırır. Demet yarım kalmış olsa da kart
    /// serbest kalır; demet açık kalır ve sonradan yeni bir kartla sürdürülebilir.
    /// </summary>
    Task<Result<BundleCardReleaseDto>> ReleaseBundleCardAsync(string uid, CancellationToken cancellationToken);

    /// <summary>Personel kartını operatöre atar.</summary>
    Task<Result> AssignToOperatorAsync(int operatorId, string uid, CancellationToken cancellationToken);

    Task<Result> ReleaseFromOperatorAsync(int operatorId, CancellationToken cancellationToken);
}

public sealed class RfidAssignmentService(IKonfeksiyonDbContext db, IAppLock appLock, TimeProvider timeProvider)
    : IRfidAssignmentService
{
    private static readonly TimeSpan LockTimeout = TimeSpan.FromSeconds(10);

    private static readonly Error Busy = new(
        ErrorCodes.ResourceBusy, "Bu kart için şu an başka bir işlem sürüyor. Lütfen tekrar deneyin.",
        ErrorType.Conflict);

    private DateTime UtcNow => timeProvider.GetUtcNow().UtcDateTime;

    public async Task<Result> AssignToBundleAsync(int bundleId, string uid, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var card = await LockCardAsync(uid, RfidCardType.Bundle, cancellationToken);
        if (!card.IsSuccess)
        {
            return card.Error!;
        }

        var bundle = await db.Bundles.SingleOrDefaultAsync(b => b.Id == bundleId, cancellationToken);
        if (bundle is null)
        {
            return new Error(ErrorCodes.NotFound, "Demet bulunamadı.", ErrorType.NotFound);
        }

        if (!bundle.IsOpen)
        {
            return new Error(ErrorCodes.BundleNotActive, "Tamamlanmış ya da iptal edilmiş demete kart atanamaz.");
        }

        var cardInUse = await db.BundleCardAssignments
            .Where(a => a.RfidCardId == card.Value.Id && a.ReleasedAt == null)
            .Select(a => new { a.BundleId, a.Bundle.BundleNo })
            .SingleOrDefaultAsync(cancellationToken);
        if (cardInUse is not null)
        {
            return new Error(
                ErrorCodes.RfidCardAlreadyAssigned,
                cardInUse.BundleId == bundleId
                    ? "Bu kart zaten bu demete atanmış."
                    : $"Bu kart şu an {cardInUse.BundleNo} numaralı demete atanmış.",
                ErrorType.Conflict);
        }

        if (await db.BundleCardAssignments.AnyAsync(a => a.BundleId == bundleId && a.ReleasedAt == null, cancellationToken))
        {
            return new Error(
                ErrorCodes.BundleAlreadyHasCard, "Demetin zaten bir kartı var; değiştirmek için önce mevcut kartı bırakın.",
                ErrorType.Conflict);
        }

        db.BundleCardAssignments.Add(new BundleCardAssignment
        {
            RfidCardId = card.Value.Id, BundleId = bundleId, AssignedAt = UtcNow,
        });
        bundle.OnCardAssigned();

        return await SaveAsync(transaction, cancellationToken);
    }

    public async Task<Result> ReleaseFromBundleAsync(int bundleId, CancellationToken cancellationToken)
    {
        var assignment = await db.BundleCardAssignments
            .Include(a => a.Bundle)
            .SingleOrDefaultAsync(a => a.BundleId == bundleId && a.ReleasedAt == null, cancellationToken);
        if (assignment is null)
        {
            return new Error(ErrorCodes.RfidCardNotAssigned, "Demetin atanmış bir kartı yok.", ErrorType.NotFound);
        }

        assignment.Release(UtcNow);
        assignment.Bundle.OnCardReleased();
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<BundleCardDto>> GetBundleCardAsync(string uid, CancellationToken cancellationToken)
    {
        var card = await FindBundleCardAsync(uid, cancellationToken);
        if (!card.IsSuccess)
        {
            return card.Error!;
        }

        var bundle = await db.BundleCardAssignments
            .Where(a => a.RfidCardId == card.Value.Id && a.ReleasedAt == null)
            .Select(a => new { a.BundleId, a.Bundle.BundleNo })
            .SingleOrDefaultAsync(cancellationToken);

        return new BundleCardDto(card.Value.Uid, card.Value.Label, card.Value.IsActive, bundle?.BundleId, bundle?.BundleNo);
    }

    public async Task<Result<BundleCardReleaseDto>> ReleaseBundleCardAsync(string uid, CancellationToken cancellationToken)
    {
        // Pasif kart da boşaltılabilir: kart toplanıp kenara ayrılırken bağı kaldırılır.
        var card = await FindBundleCardAsync(uid, cancellationToken);
        if (!card.IsSuccess)
        {
            return card.Error!;
        }

        var assignment = await db.BundleCardAssignments
            .Include(a => a.Bundle)
            .SingleOrDefaultAsync(a => a.RfidCardId == card.Value.Id && a.ReleasedAt == null, cancellationToken);
        if (assignment is null)
        {
            return new Error(ErrorCodes.RfidCardNotAssigned, "Kart zaten boş; bir demete bağlı değil.", ErrorType.NotFound);
        }

        assignment.Release(UtcNow);
        assignment.Bundle.OnCardReleased();
        await db.SaveChangesAsync(cancellationToken);
        return new BundleCardReleaseDto(card.Value.Uid, assignment.BundleId, assignment.Bundle.BundleNo);
    }

    private async Task<Result<RfidCard>> FindBundleCardAsync(string uid, CancellationToken cancellationToken)
    {
        var normalized = RfidUid.Normalize(uid);
        if (normalized is null)
        {
            return ReferenceValidation.InvalidReference("uid", RfidValidation.InvalidUidMessage);
        }

        var card = await db.RfidCards.AsNoTracking().SingleOrDefaultAsync(c => c.Uid == normalized, cancellationToken);
        if (card is null)
        {
            return new Error(
                ErrorCodes.RfidNotRegistered, $"{normalized} UID'li kart sistemde tanımlı değil.", ErrorType.NotFound);
        }

        if (card.CardType != RfidCardType.Bundle)
        {
            return new Error(ErrorCodes.RfidCardTypeMismatch, "Bu bir personel kartı; demet kartı okutun.");
        }

        return card;
    }

    public async Task<Result> AssignToOperatorAsync(int operatorId, string uid, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var card = await LockCardAsync(uid, RfidCardType.Operator, cancellationToken);
        if (!card.IsSuccess)
        {
            return card.Error!;
        }

        var machineOperator = await db.Operators
            .Where(o => o.Id == operatorId)
            .Select(o => new { o.IsActive })
            .SingleOrDefaultAsync(cancellationToken);
        if (machineOperator is null)
        {
            return new Error(ErrorCodes.NotFound, "Operatör bulunamadı.", ErrorType.NotFound);
        }

        if (!machineOperator.IsActive)
        {
            return new Error(ErrorCodes.OperatorInactive, "Pasif operatöre kart atanamaz.");
        }

        var cardInUse = await db.OperatorCards
            .Where(c => c.RfidCardId == card.Value.Id && c.RevokedAt == null)
            .Select(c => new { c.OperatorId, Name = c.Operator.FirstName + " " + c.Operator.LastName })
            .SingleOrDefaultAsync(cancellationToken);
        if (cardInUse is not null)
        {
            return new Error(
                ErrorCodes.RfidCardAlreadyAssigned,
                cardInUse.OperatorId == operatorId
                    ? "Bu kart zaten bu operatöre atanmış."
                    : $"Bu kart şu an {cardInUse.Name} adlı operatöre atanmış.",
                ErrorType.Conflict);
        }

        if (await db.OperatorCards.AnyAsync(c => c.OperatorId == operatorId && c.RevokedAt == null, cancellationToken))
        {
            return new Error(
                ErrorCodes.OperatorAlreadyHasCard,
                "Operatörün zaten bir kartı var; değiştirmek için önce mevcut kartı bırakın.",
                ErrorType.Conflict);
        }

        db.OperatorCards.Add(new OperatorCard { OperatorId = operatorId, RfidCardId = card.Value.Id, AssignedAt = UtcNow });

        return await SaveAsync(transaction, cancellationToken);
    }

    public async Task<Result> ReleaseFromOperatorAsync(int operatorId, CancellationToken cancellationToken)
    {
        var card = await db.OperatorCards
            .SingleOrDefaultAsync(c => c.OperatorId == operatorId && c.RevokedAt == null, cancellationToken);
        if (card is null)
        {
            return new Error(ErrorCodes.RfidCardNotAssigned, "Operatörün atanmış bir kartı yok.", ErrorType.NotFound);
        }

        card.Revoke(UtcNow);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    /// <summary>
    /// Kartı UID ile bulur, kullanılabilir olduğunu doğrular ve atama süresince kilitler (aynı kartın
    /// eşzamanlı iki atamasından yalnızca biri ilerler). Açık bir transaction içinde çağrılmalıdır.
    /// </summary>
    private async Task<Result<RfidCard>> LockCardAsync(
        string uid, RfidCardType expectedType, CancellationToken cancellationToken)
    {
        var normalized = RfidUid.Normalize(uid);
        if (normalized is null)
        {
            return ReferenceValidation.InvalidReference("uid", RfidValidation.InvalidUidMessage);
        }

        var card = await db.RfidCards.AsNoTracking().SingleOrDefaultAsync(c => c.Uid == normalized, cancellationToken);
        if (card is null)
        {
            return new Error(
                ErrorCodes.RfidNotRegistered, $"{normalized} UID'li kart sistemde tanımlı değil.", ErrorType.NotFound);
        }

        if (!card.IsActive)
        {
            return new Error(ErrorCodes.RfidCardInactive, "Kart pasif durumda.");
        }

        if (card.CardType != expectedType)
        {
            return new Error(
                ErrorCodes.RfidCardTypeMismatch,
                expectedType == RfidCardType.Bundle
                    ? "Bu bir personel kartı; demete atanamaz."
                    : "Bu bir demet kartı; operatöre atanamaz.");
        }

        if (!await appLock.TryAcquireAsync($"rfid-card:{card.Id}", LockTimeout, cancellationToken))
        {
            return Busy;
        }

        return card;
    }

    private async Task<Result> SaveAsync(
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction, CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Kilit farklı kaynaklarda (kart / demet / operatör) olduğundan, aynı demete ya da operatöre
            // eşzamanlı iki farklı kart atanırsa unique index devreye girer.
            return new Error(
                ErrorCodes.ConcurrencyConflict, "Atama aynı anda başka bir işlemle değişti. Lütfen tekrar deneyin.",
                ErrorType.Conflict);
        }

        await transaction.CommitAsync(cancellationToken);
        return Result.Success();
    }
}
