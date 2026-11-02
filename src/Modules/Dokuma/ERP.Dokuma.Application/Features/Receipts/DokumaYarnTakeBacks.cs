using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Dokuma.Application.Features.Warping;
using ERP.Dokuma.Domain.Entities;
using ERP.Dokuma.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using static ERP.Core.Application.Features.MasterData.MasterDataRules;

namespace ERP.Dokuma.Application.Features.Receipts;

public sealed record DokumaYarnTakeBackDto(int Id, int YarnLotId, string LotNo, string YarnCode, string YarnName, DateOnly TakenOn, decimal Kilograms, string? Notes);

public sealed record DokumaYarnTakeBackRequest(int YarnLotId, decimal Kilograms, DateOnly TakenOn, string? Notes = null);

public sealed class DokumaYarnTakeBackRequestValidator : AbstractValidator<DokumaYarnTakeBackRequest>
{
    public DokumaYarnTakeBackRequestValidator()
    {
        RuleFor(x => x.YarnLotId).GreaterThan(0).WithMessage("İplik lotu seçilmelidir.");
        RuleFor(x => x.TakenOn).NotEmpty().WithMessage("Tarih zorunludur.");
        RuleFor(x => x.Kilograms)
            .GreaterThan(0).WithMessage("Kilo sıfırdan büyük olmalıdır.")
            .PrecisionScale(12, 2, true).WithMessage("Kilo en fazla iki ondalık olabilir.");
        RuleFor(x => x.Notes).OptionalText(500);
    }
}

public interface IDokumaYarnTakeBackService
{
    Task<Result> AddToWarpAsync(int warpOrderId, DokumaYarnTakeBackRequest request, CancellationToken cancellationToken);
    Task<Result> AddToBeamAsync(int beamId, DokumaYarnTakeBackRequest request, CancellationToken cancellationToken);
    Task<Result> RemoveFromWarpAsync(int warpOrderId, int id, CancellationToken cancellationToken);
    Task<Result> RemoveFromBeamAsync(int beamId, int id, CancellationToken cancellationToken);
}

/// <summary>
/// Üretimden artan ipliğin geri alımı: verilenden düşer, lota döner; böylece fire doğru çıkar ve artan iplik müşteriye iade
/// edilebilir. Süren çözgüde çıkış doğrudan geri alındığı için çözgüde yalnız biten emirde, atkıda levent tezgâhta ya da
/// bitmişken yapılır.
/// </summary>
public sealed class DokumaYarnTakeBackService(IAppDbContext db, IAppLock appLock) : IDokumaYarnTakeBackService
{
    private const string InvalidState = "DOKUMA_TAKE_BACK_INVALID";
    private static readonly TimeSpan LockTimeout = TimeSpan.FromSeconds(10);
    private static readonly Error Busy = new("DOKUMA_BUSY", "Aynı anda başka bir iplik işlemi yapılıyor; tekrar deneyin.", ErrorType.Conflict);
    private static readonly Error NotFound = new(ErrorCodes.NotFound, "İplik geri alımı bulunamadı.", ErrorType.NotFound);

    public async Task<Result> AddToWarpAsync(int warpOrderId, DokumaYarnTakeBackRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (!await appLock.TryAcquireAsync(DokumaWarpService.YarnLotLock, LockTimeout, cancellationToken))
        {
            return Busy;
        }

        var status = await db.Set<DokumaWarpOrder>().Where(w => w.Id == warpOrderId).Select(w => (WarpOrderStatus?)w.Status).SingleOrDefaultAsync(cancellationToken);
        if (status is null)
        {
            return new Error(ErrorCodes.NotFound, "Çözgü emri bulunamadı.", ErrorType.NotFound);
        }

        if (status != WarpOrderStatus.Completed)
        {
            return new Error(InvalidState, "Çözgüden artan iplik çözgü bitince geri alınır; süren çözgüde çıkışı geri alın.", ErrorType.Conflict);
        }

        var issued = await db.Set<DokumaWarpIssue>().Where(i => i.WarpOrderId == warpOrderId && i.YarnLotId == request.YarnLotId).SumAsync(i => (decimal?)i.Kilograms, cancellationToken) ?? 0;
        var taken = await db.Set<DokumaYarnTakeBack>().Where(t => t.WarpOrderId == warpOrderId && t.YarnLotId == request.YarnLotId).SumAsync(t => (decimal?)t.Kilograms, cancellationToken) ?? 0;
        if (request.Kilograms > issued - taken)
        {
            return new Error(InvalidState, $"Bu lottan çözgüye verilip geri alınmamış {issued - taken:0.##} kg var; daha fazlası geri alınamaz.", ErrorType.Conflict);
        }

        db.Set<DokumaYarnTakeBack>().Add(new DokumaYarnTakeBack
        {
            WarpOrderId = warpOrderId,
            YarnLotId = request.YarnLotId,
            TakenOn = request.TakenOn,
            Kilograms = request.Kilograms,
            Notes = NullIfBlank(request.Notes),
        });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> AddToBeamAsync(int beamId, DokumaYarnTakeBackRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (!await appLock.TryAcquireAsync(DokumaWarpService.YarnLotLock, LockTimeout, cancellationToken))
        {
            return Busy;
        }

        var status = await db.Set<DokumaBeam>().Where(b => b.Id == beamId).Select(b => (BeamStatus?)b.Status).SingleOrDefaultAsync(cancellationToken);
        if (status is null)
        {
            return new Error(ErrorCodes.NotFound, "Levent bulunamadı.", ErrorType.NotFound);
        }

        if (status is not (BeamStatus.OnLoom or BeamStatus.Finished))
        {
            return new Error(InvalidState, "Atkıdan artan iplik levent tezgâhtayken ya da bittikten sonra geri alınır.", ErrorType.Conflict);
        }

        var issued = await db.Set<DokumaWeftIssue>().Where(i => i.BeamId == beamId && i.YarnLotId == request.YarnLotId).SumAsync(i => (decimal?)i.Kilograms, cancellationToken) ?? 0;
        var taken = await db.Set<DokumaYarnTakeBack>().Where(t => t.BeamId == beamId && t.YarnLotId == request.YarnLotId).SumAsync(t => (decimal?)t.Kilograms, cancellationToken) ?? 0;
        if (request.Kilograms > issued - taken)
        {
            return new Error(InvalidState, $"Bu lottan atkıya verilip geri alınmamış {issued - taken:0.##} kg var; daha fazlası geri alınamaz.", ErrorType.Conflict);
        }

        db.Set<DokumaYarnTakeBack>().Add(new DokumaYarnTakeBack
        {
            BeamId = beamId,
            YarnLotId = request.YarnLotId,
            TakenOn = request.TakenOn,
            Kilograms = request.Kilograms,
            Notes = NullIfBlank(request.Notes),
        });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Result.Success();
    }

    public Task<Result> RemoveFromWarpAsync(int warpOrderId, int id, CancellationToken cancellationToken) =>
        RemoveAsync(t => t.Id == id && t.WarpOrderId == warpOrderId, id, cancellationToken);

    public Task<Result> RemoveFromBeamAsync(int beamId, int id, CancellationToken cancellationToken) =>
        RemoveAsync(t => t.Id == id && t.BeamId == beamId, id, cancellationToken);

    /// <summary>Yanlış geri alımı siler: kilo yeniden üretimde sayılır, bu yüzden lotta o kadar kalan olmalıdır.</summary>
    private async Task<Result> RemoveAsync(System.Linq.Expressions.Expression<Func<DokumaYarnTakeBack, bool>> match, int id, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (!await appLock.TryAcquireAsync(DokumaWarpService.YarnLotLock, LockTimeout, cancellationToken))
        {
            return Busy;
        }

        var item = await db.Set<DokumaYarnTakeBack>().Include(t => t.YarnLot).SingleOrDefaultAsync(match, cancellationToken);
        if (item is null)
        {
            return NotFound;
        }

        var remaining = item.YarnLot.Kilograms - await DokumaYarnStock.IssuedFromLotAsync(db, item.YarnLotId, cancellationToken);
        if (item.Kilograms > remaining)
        {
            return new Error(InvalidState, $"{item.YarnLot.LotNo} lotunda {remaining:0.##} kg kaldı; geri alım silinemez, iplik başka yere verilmiş.", ErrorType.Conflict);
        }

        db.Set<DokumaYarnTakeBack>().Remove(item);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Result.Success();
    }
}
