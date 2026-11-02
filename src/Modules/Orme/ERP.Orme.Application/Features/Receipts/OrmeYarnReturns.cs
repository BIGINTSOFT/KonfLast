using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Orme.Domain.Entities;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using static ERP.Core.Application.Features.MasterData.MasterDataRules;

namespace ERP.Orme.Application.Features.Receipts;

public sealed class OrmeYarnReturnQuery : PageRequest
{
    /// <summary>İade no, lot no, iplik ya da müşteri.</summary>
    public string? Search { get; set; }
}

public sealed record OrmeYarnReturnDto(
    int Id,
    string ReturnNo,
    DateOnly ReturnDate,
    int YarnLotId,
    string LotNo,
    string? ProducerLotNo,
    string CustomerName,
    string YarnCode,
    string YarnName,
    decimal Kilograms,
    string? Notes);

public sealed record OrmeYarnReturnRequest(int YarnLotId, DateOnly ReturnDate, decimal Kilograms, string? Notes = null);

public sealed class OrmeYarnReturnRequestValidator : AbstractValidator<OrmeYarnReturnRequest>
{
    public OrmeYarnReturnRequestValidator()
    {
        RuleFor(x => x.YarnLotId).GreaterThan(0).WithMessage("İplik lotu seçilmelidir.");
        RuleFor(x => x.ReturnDate).NotEmpty().WithMessage("İade tarihi zorunludur.");
        RuleFor(x => x.Kilograms)
            .GreaterThan(0).WithMessage("Kilo sıfırdan büyük olmalıdır.")
            .PrecisionScale(12, 2, true).WithMessage("Kilo en fazla iki ondalık olabilir.");
        RuleFor(x => x.Notes).OptionalText(500);
    }
}

public interface IOrmeYarnReturnService
{
    Task<PagedResult<OrmeYarnReturnDto>> GetListAsync(OrmeYarnReturnQuery query, CancellationToken cancellationToken);
    Task<Result<OrmeYarnReturnDto>> CreateAsync(OrmeYarnReturnRequest request, CancellationToken cancellationToken);
    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken);
}

/// <summary>Fasonda müşterinin kalan ipliğinin iadesi: lotun kalanından düşer. Yanlış kayıt silinir, iplik lota döner.</summary>
public sealed class OrmeYarnReturnService(IAppDbContext db, IDocumentNumberGenerator numbers, IAppLock appLock) : IOrmeYarnReturnService
{
    private const string Series = "OrmeYarnReturn";
    private const string InvalidState = "ORME_YARN_RETURN_INVALID";
    private static readonly TimeSpan LockTimeout = TimeSpan.FromSeconds(10);

    private static IQueryable<OrmeYarnReturnDto> Project(IQueryable<OrmeYarnReturn> returns) => returns.Select(r => new OrmeYarnReturnDto(
        r.Id,
        r.ReturnNo,
        r.ReturnDate,
        r.YarnLotId,
        r.YarnLot.LotNo,
        r.YarnLot.ProducerLotNo,
        r.YarnLot.Customer != null ? r.YarnLot.Customer.Name : "",
        r.YarnLot.Yarn.YarnCode,
        r.YarnLot.Yarn.Name,
        r.Kilograms,
        r.Notes));

    public Task<PagedResult<OrmeYarnReturnDto>> GetListAsync(OrmeYarnReturnQuery query, CancellationToken cancellationToken)
    {
        var returns = db.Set<OrmeYarnReturn>().AsNoTracking();
        if (NullIfBlank(query.Search) is { } term)
        {
            returns = returns.Where(r => r.ReturnNo.Contains(term)
                || r.YarnLot.LotNo.Contains(term)
                || r.YarnLot.Yarn.YarnCode.Contains(term)
                || (r.YarnLot.Customer != null && r.YarnLot.Customer.Name.Contains(term)));
        }

        return Project(returns.OrderByDescending(r => r.Id)).ToPagedResultAsync(query, cancellationToken);
    }

    public async Task<Result<OrmeYarnReturnDto>> CreateAsync(OrmeYarnReturnRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (!await appLock.TryAcquireAsync(OrmeYarnStock.YarnLotLock, LockTimeout, cancellationToken))
        {
            return new Error("ORME_BUSY", "Aynı anda başka bir iplik işlemi yapılıyor; tekrar deneyin.", ErrorType.Conflict);
        }

        var lot = await db.Set<OrmeYarnLot>().SingleOrDefaultAsync(l => l.Id == request.YarnLotId, cancellationToken);
        if (lot is null)
        {
            return ReferenceValidation.InvalidReference("yarnLotId", "İplik lotu bulunamadı.");
        }

        if (lot.CustomerId is null)
        {
            return new Error(InvalidState, "İşletme malı iplik müşteriye iade edilmez.", ErrorType.Conflict);
        }

        var remaining = lot.Kilograms - await OrmeYarnStock.IssuedFromLotAsync(db, lot.Id, cancellationToken);
        if (request.Kilograms > remaining)
        {
            return new Error(InvalidState, $"{lot.LotNo} lotunda {remaining:0.##} kg kaldı; daha fazlası iade edilemez.", ErrorType.Conflict);
        }

        var year = request.ReturnDate.Year;
        var sequence = await numbers.NextAsync(Series, year, cancellationToken);
        var item = new OrmeYarnReturn
        {
            ReturnNo = $"OIA-{year}-{sequence:D5}",
            YarnLotId = lot.Id,
            ReturnDate = request.ReturnDate,
            Kilograms = request.Kilograms,
            Notes = NullIfBlank(request.Notes),
        };
        db.Set<OrmeYarnReturn>().Add(item);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await Project(db.Set<OrmeYarnReturn>().AsNoTracking().Where(r => r.Id == item.Id)).SingleAsync(cancellationToken);
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var item = await db.Set<OrmeYarnReturn>().SingleOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (item is null)
        {
            return new Error(ErrorCodes.NotFound, "İplik iadesi bulunamadı.", ErrorType.NotFound);
        }

        db.Set<OrmeYarnReturn>().Remove(item);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
