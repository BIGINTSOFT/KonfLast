using ERP.Boyahane.Domain.Entities;
using ERP.Boyahane.Domain.Enums;
using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using static ERP.Core.Application.Features.MasterData.MasterDataRules;

namespace ERP.Boyahane.Application.Features.Returns;

public sealed class BoyahaneGoodsReturnQuery : PageRequest
{
    /// <summary>Belge no ya da müşteri.</summary>
    public string? Search { get; set; }
}

public sealed record BoyahaneGoodsReturnListItemDto(
    int Id,
    string ReturnNo,
    GoodsReturnKind Kind,
    string? CustomerName,
    DateOnly ReturnDate,
    int ItemCount,
    decimal Kilograms,
    GoodsReturnStatus Status);

public sealed record BoyahaneGoodsReturnItemDto(int Id, int? RollId, int? YarnLotId, string ItemNo, string CardName, decimal Kilograms, decimal? Meters, string? Reason);

public sealed record BoyahaneGoodsReturnDto(
    int Id,
    string ReturnNo,
    GoodsReturnKind Kind,
    int? CustomerId,
    string? CustomerName,
    string? CustomerTaxNumber,
    DateOnly ReturnDate,
    string? VehiclePlate,
    string? DriverName,
    string? DeliveryAddress,
    string? Notes,
    GoodsReturnStatus Status,
    decimal Kilograms,
    decimal Meters,
    IReadOnlyList<BoyahaneGoodsReturnItemDto> Items);

/// <param name="Stage">Reddin geldiği yer: ham kontrol, final kalite ya da iplik kabul.</param>
public sealed record BoyahaneGoodsReturnCandidateDto(
    int? RollId,
    int? YarnLotId,
    string ItemNo,
    string CardName,
    int? OwnerCustomerId,
    string? OwnerName,
    string Stage,
    decimal Kilograms,
    decimal? Meters,
    string? Reason,
    bool Selected);

public sealed record BoyahaneGoodsReturnItemRequest(int? RollId = null, int? YarnLotId = null);

/// <param name="CustomerId">İadede zorunlu; hurdada boş.</param>
public sealed record BoyahaneGoodsReturnRequest(
    GoodsReturnKind Kind,
    DateOnly ReturnDate,
    IReadOnlyList<BoyahaneGoodsReturnItemRequest> Items,
    int? CustomerId = null,
    string? VehiclePlate = null,
    string? DriverName = null,
    string? DeliveryAddress = null,
    string? Notes = null);

public sealed class BoyahaneGoodsReturnRequestValidator : AbstractValidator<BoyahaneGoodsReturnRequest>
{
    public BoyahaneGoodsReturnRequestValidator()
    {
        RuleFor(x => x.Kind).IsInEnum().WithMessage("Geçerli bir belge türü seçin.");
        RuleFor(x => x.ReturnDate).NotEmpty().WithMessage("Tarih zorunludur.");
        RuleFor(x => x.CustomerId).NotNull().WithMessage("İade edilecek müşteri seçilmelidir.").When(x => x.Kind == GoodsReturnKind.CustomerReturn);
        RuleFor(x => x.Notes).NotEmpty().WithMessage("Hurda için gerekçe zorunludur.").When(x => x.Kind == GoodsReturnKind.Scrap);
        RuleFor(x => x.Notes).OptionalText(1000);
        RuleFor(x => x.VehiclePlate).OptionalText(20);
        RuleFor(x => x.DriverName).OptionalText(100);
        RuleFor(x => x.DeliveryAddress).OptionalText(500);
        RuleFor(x => x.Items).NotEmpty().WithMessage("En az bir top ya da lot seçilmelidir.");
        RuleForEach(x => x.Items).Must(i => (i.RollId is null) != (i.YarnLotId is null)).WithMessage("Her satır bir top ya da bir lot olmalıdır.");
    }
}

public interface IBoyahaneGoodsReturnService
{
    Task<PagedResult<BoyahaneGoodsReturnListItemDto>> GetListAsync(BoyahaneGoodsReturnQuery query, CancellationToken cancellationToken);
    Task<Result<BoyahaneGoodsReturnDto>> GetAsync(int id, CancellationToken cancellationToken);
    Task<IReadOnlyList<BoyahaneGoodsReturnCandidateDto>> GetCandidatesAsync(int? returnId, CancellationToken cancellationToken);
    Task<Result<BoyahaneGoodsReturnDto>> SaveAsync(int? id, BoyahaneGoodsReturnRequest request, CancellationToken cancellationToken);
    Task<Result<BoyahaneGoodsReturnDto>> ConfirmAsync(int id, CancellationToken cancellationToken);
    Task<Result<BoyahaneGoodsReturnDto>> ReopenAsync(int id, CancellationToken cancellationToken);
    Task<Result<BoyahaneGoodsReturnDto>> CancelAsync(int id, CancellationToken cancellationToken);
}

public sealed class BoyahaneGoodsReturnService(IAppDbContext db, IDocumentNumberGenerator numbers) : IBoyahaneGoodsReturnService
{
    private const string Series = "BoyahaneGoodsReturn";
    private const string InvalidState = "GOODS_RETURN_INVALID_STATE";
    private const string InvalidItem = "GOODS_RETURN_INVALID_ITEM";
    private const int MaxCandidates = 1000;

    private static readonly Error NotFound = new(ErrorCodes.NotFound, "İade belgesi bulunamadı.", ErrorType.NotFound);

    public Task<PagedResult<BoyahaneGoodsReturnListItemDto>> GetListAsync(BoyahaneGoodsReturnQuery query, CancellationToken cancellationToken)
    {
        var returns = db.Set<BoyahaneGoodsReturn>().AsNoTracking();
        if (NullIfBlank(query.Search) is { } term)
        {
            returns = returns.Where(r => r.ReturnNo.Contains(term) || (r.Customer != null && r.Customer.Name.Contains(term)));
        }

        return returns
            .OrderByDescending(r => r.Id)
            .Select(r => new BoyahaneGoodsReturnListItemDto(
                r.Id,
                r.ReturnNo,
                r.Kind,
                r.Customer != null ? r.Customer.Name : null,
                r.ReturnDate,
                r.Items.Count,
                r.Items.Sum(i => i.Kilograms),
                r.Status))
            .ToPagedResultAsync(query, cancellationToken);
    }

    public async Task<Result<BoyahaneGoodsReturnDto>> GetAsync(int id, CancellationToken cancellationToken)
    {
        var r = await db.Set<BoyahaneGoodsReturn>().AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Items)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (r is null)
        {
            return NotFound;
        }

        return new BoyahaneGoodsReturnDto(
            r.Id,
            r.ReturnNo,
            r.Kind,
            r.CustomerId,
            r.Customer?.Name,
            r.Customer?.TaxNumber,
            r.ReturnDate,
            r.VehiclePlate,
            r.DriverName,
            r.DeliveryAddress,
            r.Notes,
            r.Status,
            r.Items.Sum(i => i.Kilograms),
            r.Items.Sum(i => i.Meters ?? 0),
            r.Items.OrderBy(i => i.ItemNo).Select(i => new BoyahaneGoodsReturnItemDto(i.Id, i.RollId, i.YarnLotId, i.ItemNo, i.CardName, i.Kilograms, i.Meters, i.Reason)).ToList());
    }

    public async Task<IReadOnlyList<BoyahaneGoodsReturnCandidateDto>> GetCandidatesAsync(int? returnId, CancellationToken cancellationToken)
    {
        var mine = returnId is { } id
            ? await db.Set<BoyahaneGoodsReturnItem>().AsNoTracking().Where(i => i.ReturnId == id).Select(i => new { i.RollId, i.YarnLotId }).ToListAsync(cancellationToken)
            : [];
        var rows = await LoadEligibleAsync(returnId, null, null, cancellationToken);
        return rows.Select(r => r with { Selected = mine.Any(m => (r.RollId != null && m.RollId == r.RollId) || (r.YarnLotId != null && m.YarnLotId == r.YarnLotId)) }).ToList();
    }

    public async Task<Result<BoyahaneGoodsReturnDto>> SaveAsync(int? id, BoyahaneGoodsReturnRequest request, CancellationToken cancellationToken)
    {
        BoyahaneGoodsReturn? entity = null;
        if (id is not null)
        {
            entity = await db.Set<BoyahaneGoodsReturn>().Include(r => r.Items).SingleOrDefaultAsync(r => r.Id == id, cancellationToken);
            if (entity is null)
            {
                return NotFound;
            }

            if (entity.Status != GoodsReturnStatus.Draft)
            {
                return new Error(InvalidState, "Yalnızca taslak belge düzenlenebilir.", ErrorType.Conflict);
            }
        }

        var customerId = request.Kind == GoodsReturnKind.CustomerReturn ? request.CustomerId : null;
        if (await ReferenceValidation.RequireSelectableAsync(db.Set<BoyahaneCustomer>(), customerId, entity?.CustomerId, "customerId", "Müşteri", cancellationToken) is { } customerError)
        {
            return customerError;
        }

        var rollIds = request.Items.Where(i => i.RollId != null).Select(i => i.RollId!.Value).Distinct().ToList();
        var yarnIds = request.Items.Where(i => i.YarnLotId != null).Select(i => i.YarnLotId!.Value).Distinct().ToList();
        var eligible = await LoadEligibleAsync(id, rollIds, yarnIds, cancellationToken);
        if (eligible.Count != rollIds.Count + yarnIds.Count)
        {
            return new Error(InvalidItem, "Seçilen malın bir kısmı reddedilmiş değil ya da başka bir belgede.", ErrorType.Conflict);
        }

        if (customerId is not null && eligible.FirstOrDefault(e => e.OwnerCustomerId != customerId) is { } foreign)
        {
            return new Error(InvalidItem, $"{foreign.ItemNo} bu müşterinin malı değil.", ErrorType.Conflict);
        }

        var address = customerId is null ? null : NullIfBlank(request.DeliveryAddress) ?? await db.Set<BoyahaneCustomerAddress>().AsNoTracking()
            .Where(a => a.CustomerId == customerId && a.IsDefaultDelivery)
            .Select(a => a.AddressLine + " " + a.District + "/" + a.City)
            .FirstOrDefaultAsync(cancellationToken);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (entity is null)
        {
            var year = request.ReturnDate.Year;
            var sequence = await numbers.NextAsync(Series, year, cancellationToken);
            entity = new BoyahaneGoodsReturn { ReturnNo = $"IA-{year}-{sequence:D5}" };
            db.Set<BoyahaneGoodsReturn>().Add(entity);
        }
        else
        {
            db.Set<BoyahaneGoodsReturnItem>().RemoveRange(entity.Items);
        }

        entity.Kind = request.Kind;
        entity.CustomerId = customerId;
        entity.ReturnDate = request.ReturnDate;
        entity.VehiclePlate = NullIfBlank(request.VehiclePlate);
        entity.DriverName = NullIfBlank(request.DriverName);
        entity.DeliveryAddress = address;
        entity.Notes = NullIfBlank(request.Notes);
        foreach (var item in eligible)
        {
            entity.Items.Add(new BoyahaneGoodsReturnItem
            {
                RollId = item.RollId,
                YarnLotId = item.YarnLotId,
                ItemNo = item.ItemNo,
                CardName = item.CardName,
                Kilograms = item.Kilograms,
                Meters = item.Meters,
                Reason = item.Reason,
            });
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetAsync(entity.Id, cancellationToken);
    }

    public async Task<Result<BoyahaneGoodsReturnDto>> ConfirmAsync(int id, CancellationToken cancellationToken)
    {
        var entity = await db.Set<BoyahaneGoodsReturn>().Include(r => r.Items).SingleOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (entity is null)
        {
            return NotFound;
        }

        if (entity.Status != GoodsReturnStatus.Draft)
        {
            return new Error(InvalidState, "Yalnızca taslak belge onaylanabilir.", ErrorType.Conflict);
        }

        // Taslak beklerken kalite kararı değişmiş olabilir.
        var rollIds = entity.Items.Where(i => i.RollId != null).Select(i => i.RollId!.Value).ToList();
        var yarnIds = entity.Items.Where(i => i.YarnLotId != null).Select(i => i.YarnLotId!.Value).ToList();
        var eligible = await LoadEligibleAsync(id, rollIds, yarnIds, cancellationToken);
        if (eligible.Count != entity.Items.Count)
        {
            return new Error(InvalidItem, "Belgedeki malın bir kısmı artık reddedilmiş değil; belgeyi düzenleyin.", ErrorType.Conflict);
        }

        entity.Status = GoodsReturnStatus.Confirmed;
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    public Task<Result<BoyahaneGoodsReturnDto>> ReopenAsync(int id, CancellationToken cancellationToken) =>
        ChangeAsync(id, GoodsReturnStatus.Confirmed, GoodsReturnStatus.Draft, "Yalnızca onaylı belge taslağa alınabilir.", cancellationToken);

    public Task<Result<BoyahaneGoodsReturnDto>> CancelAsync(int id, CancellationToken cancellationToken) =>
        ChangeAsync(id, GoodsReturnStatus.Draft, GoodsReturnStatus.Cancelled, "Yalnızca taslak belge iptal edilebilir.", cancellationToken);

    private async Task<Result<BoyahaneGoodsReturnDto>> ChangeAsync(int id, GoodsReturnStatus from, GoodsReturnStatus to, string message, CancellationToken cancellationToken)
    {
        var entity = await db.Set<BoyahaneGoodsReturn>().SingleOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (entity is null)
        {
            return NotFound;
        }

        if (entity.Status != from)
        {
            return new Error(InvalidState, message, ErrorType.Conflict);
        }

        entity.Status = to;
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    /// <summary>
    /// Belgeye girebilecek mal: ham kontrolde ya da iplik kabulde reddedilen, ya da final kalitede reddedilen; başka bir
    /// etkin belgede olmayan. Kimlik listesi verilirse yalnızca onlar aranır.
    /// </summary>
    private async Task<List<BoyahaneGoodsReturnCandidateDto>> LoadEligibleAsync(int? returnId, IReadOnlyList<int>? rollIds, IReadOnlyList<int>? yarnIds, CancellationToken cancellationToken)
    {
        var taken = db.Set<BoyahaneGoodsReturnItem>().Where(i => i.Return.Status != GoodsReturnStatus.Cancelled && i.ReturnId != returnId);
        var finalRejected = db.Set<BoyahaneBatchItem>().Where(i => i.IsActive && i.FinalResult == FinalQualityResult.Rejected);

        var rolls = await db.Set<BoyahaneFabricRoll>().AsNoTracking()
            .Where(r => rollIds == null || rollIds.Contains(r.Id))
            .Where(r => r.Status == FabricRollStatus.Rejected || finalRejected.Any(i => i.RollId == r.Id))
            .Where(r => !taken.Any(i => i.RollId == r.Id))
            .OrderBy(r => r.RollNo)
            .Take(MaxCandidates)
            .Select(r => new
            {
                r.Id,
                r.RollNo,
                CardName = r.Lot.Fabric.Name,
                r.Lot.Receipt.CustomerId,
                OwnerName = r.Lot.Receipt.Customer != null ? r.Lot.Receipt.Customer.Name : null,
                IsRaw = r.Status == FabricRollStatus.Rejected,
                Kilograms = r.InspectedKilograms ?? r.Kilograms,
                Meters = r.InspectedMeters ?? r.Meters,
                r.InspectionNote,
                r.CustomerDecisionNote,
                FinalNote = finalRejected.Where(i => i.RollId == r.Id).Select(i => i.FinalNote).FirstOrDefault(),
            })
            .ToListAsync(cancellationToken);

        var yarns = await db.Set<BoyahaneYarnLot>().AsNoTracking()
            .Where(y => yarnIds == null || yarnIds.Contains(y.Id))
            .Where(y => y.Status == YarnLotStatus.Rejected || finalRejected.Any(i => i.YarnLotId == y.Id))
            .Where(y => !taken.Any(i => i.YarnLotId == y.Id))
            .OrderBy(y => y.LotNo)
            .Take(MaxCandidates)
            .Select(y => new
            {
                y.Id,
                y.LotNo,
                CardName = y.Yarn.Name,
                y.CustomerId,
                OwnerName = y.Customer != null ? y.Customer.Name : null,
                IsRaw = y.Status == YarnLotStatus.Rejected,
                y.Kilograms,
                y.RejectionNote,
                FinalNote = finalRejected.Where(i => i.YarnLotId == y.Id).Select(i => i.FinalNote).FirstOrDefault(),
            })
            .ToListAsync(cancellationToken);

        return rolls
            .Select(r => new BoyahaneGoodsReturnCandidateDto(r.Id, null, r.RollNo, r.CardName, r.CustomerId, r.OwnerName, r.IsRaw ? "Ham kontrol" : "Final kalite", r.Kilograms, r.Meters, r.IsRaw ? r.CustomerDecisionNote ?? r.InspectionNote : r.FinalNote, false))
            .Concat(yarns.Select(y => new BoyahaneGoodsReturnCandidateDto(null, y.Id, y.LotNo, y.CardName, y.CustomerId, y.OwnerName, y.IsRaw ? "İplik kabul" : "Final kalite", y.Kilograms, null, y.IsRaw ? y.RejectionNote : y.FinalNote, false)))
            .ToList();
    }
}
