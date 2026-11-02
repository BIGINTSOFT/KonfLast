using ERP.Boyahane.Application.Features.Warehousing;
using ERP.Boyahane.Domain.Entities;
using ERP.Boyahane.Domain.Enums;
using ERP.Boyahane.Domain.Rules;
using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using static ERP.Core.Application.Features.MasterData.MasterDataRules;

namespace ERP.Boyahane.Application.Features.Receipts;

public sealed class BoyahaneFabricReceiptQuery : PageRequest
{
    /// <summary>Belge no, irsaliye no ya da müşteri adında aranır.</summary>
    public string? Search { get; set; }
}

/// <param name="CustomerName">Malın sahibi; boşsa mal işletmenindir.</param>
public sealed record BoyahaneFabricReceiptListItemDto(
    int Id,
    string ReceiptNo,
    DateOnly ReceiptDate,
    string? CustomerName,
    string? DespatchNo,
    int RollCount,
    decimal TotalMeters,
    decimal TotalKilograms,
    int PendingRollCount,
    DateTime CreatedAt,
    string? CreatedBy);

public sealed record BoyahaneFabricRollDto(
    int Id,
    string RollNo,
    string? CustomerRollNo,
    decimal Meters,
    decimal Kilograms,
    bool MetersMeasured,
    bool KilogramsMeasured,
    bool IsConditionallyApproved,
    FabricRollStatus Status,
    bool IsLocked,
    string? LocationCode);

public sealed record BoyahaneFabricLotDto(
    int Id,
    string LotNo,
    int FabricId,
    string FabricCode,
    string FabricName,
    string? CustomerFabricCode,
    int? DeclaredRollCount,
    decimal? DeclaredMeters,
    decimal? DeclaredKilograms,
    bool InspectionRequired,
    IReadOnlyList<BoyahaneFabricRollDto> Rolls);

public sealed record BoyahaneFabricReceiptDto(
    int Id,
    string ReceiptNo,
    DateOnly ReceiptDate,
    int? CustomerId,
    string? CustomerName,
    string? DespatchNo,
    string? Notes,
    bool CanDelete,
    DateTime CreatedAt,
    string? CreatedBy,
    IReadOnlyList<BoyahaneFabricLotDto> Lots,
    bool IsOpening);

/// <param name="Meters">Ölçülen metre; boşsa tartılan kilodan hesaplanır.</param>
/// <param name="Kilograms">Tartılan kilo; boşsa ölçülen metreden hesaplanır.</param>
/// <param name="Id">Var olan top güncelleniyorsa Id'si; numarası değişmez, basılmış etiketi geçerli kalır.</param>
public sealed record BoyahaneFabricRollRequest(string? CustomerRollNo = null, decimal? Meters = null, decimal? Kilograms = null, int? Id = null);

/// <param name="Id">Var olan lot güncelleniyorsa Id'si.</param>
public sealed record BoyahaneFabricLotRequest(
    int FabricId,
    IReadOnlyList<BoyahaneFabricRollRequest> Rolls,
    string? CustomerFabricCode = null,
    int? DeclaredRollCount = null,
    decimal? DeclaredMeters = null,
    decimal? DeclaredKilograms = null,
    int? Id = null);

/// <param name="CustomerId">Malın sahibi olan müşteri; boşsa mal işletmenindir.</param>
/// <param name="LocationId">Yeni topların adresi; boşsa ham deponun genel adresi.</param>
/// <param name="IsOpening">Sisteme geçişte başlangıç stoğu: toplar ham kontrole girmeden uygun olur.</param>
public sealed record CreateBoyahaneFabricReceiptRequest(
    DateOnly ReceiptDate,
    IReadOnlyList<BoyahaneFabricLotRequest> Lots,
    int? CustomerId = null,
    string? DespatchNo = null,
    string? Notes = null,
    int? LocationId = null,
    bool IsOpening = false);

public sealed class CreateBoyahaneFabricReceiptRequestValidator : AbstractValidator<CreateBoyahaneFabricReceiptRequest>
{
    public const int MaxLots = 50;
    public const int MaxRollsPerLot = 500;

    public CreateBoyahaneFabricReceiptRequestValidator()
    {
        RuleFor(x => x.ReceiptDate).NotEmpty().WithMessage("Kabul tarihi zorunludur.");
        RuleFor(x => x.DespatchNo).OptionalText(50);
        RuleFor(x => x.Notes).OptionalText(1000);
        RuleFor(x => x.Lots)
            .NotEmpty().WithMessage("En az bir kumaş girilmelidir.")
            .Must(lots => lots.Count <= MaxLots).WithMessage($"Bir belgede en fazla {MaxLots} kumaş olabilir.")
            .Custom((lots, context) =>
            {
                if (lots is null)
                {
                    return;
                }

                for (var i = 0; i < lots.Count; i++)
                {
                    foreach (var problem in Problems(lots[i]))
                    {
                        context.AddFailure($"{i + 1}. kumaş: {problem}");
                    }
                }
            });
    }

    private static IEnumerable<string> Problems(BoyahaneFabricLotRequest lot)
    {
        if (lot.FabricId <= 0)
        {
            yield return "Kumaş kartı seçilmelidir.";
        }

        if (lot.CustomerFabricCode?.Trim().Length > 100)
        {
            yield return "Müşteri kumaş kodu en fazla 100 karakter olabilir.";
        }

        if (lot.DeclaredRollCount < 0 || lot.DeclaredMeters < 0 || lot.DeclaredKilograms < 0)
        {
            yield return "İrsaliye değerleri negatif olamaz.";
        }

        if (lot.Rolls is null || lot.Rolls.Count == 0)
        {
            yield return "En az bir top girilmelidir.";
            yield break;
        }

        if (lot.Rolls.Count > MaxRollsPerLot)
        {
            yield return $"En fazla {MaxRollsPerLot} top girilebilir.";
        }

        for (var i = 0; i < lot.Rolls.Count; i++)
        {
            var roll = lot.Rolls[i];
            if (roll.Meters is null && roll.Kilograms is null)
            {
                yield return $"{i + 1}. top için metre ya da kilo girilmelidir.";
            }

            if (roll.Meters <= 0 || roll.Kilograms <= 0)
            {
                yield return $"{i + 1}. topun miktarı sıfırdan büyük olmalıdır.";
            }

            if (roll.Meters > 999_999 || roll.Kilograms > 999_999)
            {
                yield return $"{i + 1}. topun miktarı çok büyük.";
            }

            if (roll.CustomerRollNo?.Trim().Length > 50)
            {
                yield return $"{i + 1}. topun müşteri numarası en fazla 50 karakter olabilir.";
            }
        }
    }
}

public interface IBoyahaneFabricReceiptService
{
    Task<PagedResult<BoyahaneFabricReceiptListItemDto>> GetListAsync(BoyahaneFabricReceiptQuery query, CancellationToken cancellationToken);
    Task<Result<BoyahaneFabricReceiptDto>> GetAsync(int id, CancellationToken cancellationToken);
    Task<Result<BoyahaneFabricReceiptDto>> CreateAsync(CreateBoyahaneFabricReceiptRequest request, CancellationToken cancellationToken);
    Task<Result<BoyahaneFabricReceiptDto>> UpdateAsync(int id, CreateBoyahaneFabricReceiptRequest request, CancellationToken cancellationToken);
    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken);
}

public sealed class BoyahaneFabricReceiptService(IAppDbContext db, IDocumentNumberGenerator numbers) : IBoyahaneFabricReceiptService
{
    private const string ReceiptSeries = "BoyahaneFabricReceipt";
    private const string RollSeries = "BoyahaneFabricRoll";
    private const string LockedRoll = "FABRIC_ROLL_LOCKED";

    private static readonly Error NotFound = new(ErrorCodes.NotFound, "Kumaş kabul belgesi bulunamadı.", ErrorType.NotFound);

    public Task<PagedResult<BoyahaneFabricReceiptListItemDto>> GetListAsync(
        BoyahaneFabricReceiptQuery query, CancellationToken cancellationToken)
    {
        var receipts = db.Set<BoyahaneFabricReceipt>().AsNoTracking();
        if (NullIfBlank(query.Search) is { } term)
        {
            receipts = receipts.Where(r => r.ReceiptNo.Contains(term)
                || (r.DespatchNo != null && r.DespatchNo.Contains(term))
                || (r.Customer != null && r.Customer.Name.Contains(term)));
        }

        return receipts
            .OrderByDescending(r => r.ReceiptDate).ThenByDescending(r => r.Id)
            .Select(r => new BoyahaneFabricReceiptListItemDto(
                r.Id,
                r.ReceiptNo,
                r.ReceiptDate,
                r.Customer != null ? r.Customer.Name : null,
                r.DespatchNo,
                r.Lots.SelectMany(l => l.Rolls).Count(),
                r.Lots.SelectMany(l => l.Rolls).Sum(t => t.InspectedMeters ?? t.Meters),
                r.Lots.SelectMany(l => l.Rolls).Sum(t => t.InspectedKilograms ?? t.Kilograms),
                r.Lots.SelectMany(l => l.Rolls).Count(t => t.Status == FabricRollStatus.PendingInspection),
                r.CreatedAt,
                r.CreatedBy))
            .ToPagedResultAsync(query, cancellationToken);
    }

    public async Task<Result<BoyahaneFabricReceiptDto>> GetAsync(int id, CancellationToken cancellationToken)
    {
        var receipt = await db.Set<BoyahaneFabricReceipt>().AsNoTracking()
            .Include(r => r.Customer)
            .Include(r => r.Lots).ThenInclude(l => l.Fabric)
            .Include(r => r.Lots).ThenInclude(l => l.Rolls).ThenInclude(t => t.Location)
            .SingleOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (receipt is null)
        {
            return NotFound;
        }

        return new BoyahaneFabricReceiptDto(
            receipt.Id,
            receipt.ReceiptNo,
            receipt.ReceiptDate,
            receipt.CustomerId,
            receipt.Customer?.Name,
            receipt.DespatchNo,
            receipt.Notes,
            !HasInspectedRoll(receipt),
            receipt.CreatedAt,
            receipt.CreatedBy,
            receipt.Lots.OrderBy(l => l.LotNo, StringComparer.Ordinal).Select(l => new BoyahaneFabricLotDto(
                l.Id,
                l.LotNo,
                l.FabricId,
                l.Fabric.FabricCode,
                l.Fabric.Name,
                l.CustomerFabricCode,
                l.DeclaredRollCount,
                l.DeclaredMeters,
                l.DeclaredKilograms,
                l.InspectionRequired,
                l.Rolls.OrderBy(t => t.RollNo, StringComparer.Ordinal).Select(t => new BoyahaneFabricRollDto(
                    t.Id, t.RollNo, t.CustomerRollNo, t.InspectedMeters ?? t.Meters, t.InspectedKilograms ?? t.Kilograms,
                    t.MetersMeasured, t.KilogramsMeasured, t.IsConditionallyApproved, t.Status, IsLocked(t), t.Location?.Code)).ToList()))
                .ToList(),
            receipt.IsOpening);
    }

    public async Task<Result<BoyahaneFabricReceiptDto>> CreateAsync(
        CreateBoyahaneFabricReceiptRequest request, CancellationToken cancellationToken)
    {
        if (await ReferenceValidation.RequireSelectableAsync(
                db.Set<BoyahaneCustomer>(), request.CustomerId, null, "customerId", "Müşteri", cancellationToken) is { } customerError)
        {
            return customerError;
        }

        var fabricIds = request.Lots.Select(l => l.FabricId).Distinct().ToList();
        foreach (var fabricId in fabricIds)
        {
            if (await ReferenceValidation.RequireSelectableAsync(
                    db.Set<BoyahaneFabric>(), fabricId, null, "lots", "Kumaş kartı", cancellationToken) is { } fabricError)
            {
                return fabricError;
            }
        }

        var fabrics = await db.Set<BoyahaneFabric>().AsNoTracking()
            .Where(f => fabricIds.Contains(f.Id))
            .ToDictionaryAsync(f => f.Id, cancellationToken);

        if (await BoyahaneWarehouseRules.ValidateLocationAsync(db, request.LocationId, cancellationToken) is { } locationError)
        {
            return locationError;
        }

        var locationId = request.LocationId ?? await BoyahaneWarehouseRules.DefaultLocationIdAsync(db, BoyahaneWarehouseType.RawFabric, cancellationToken);

        // Müşteri kontrolü zorunlu saymıyorsa toplar doğrudan uygun girer; işletme malı her zaman kontrole girer. Açılış stoğu kontrole girmez.
        var inspectionRequired = !request.IsOpening && (request.CustomerId is not { } customerId
            || await db.Set<BoyahaneCustomer>().Where(c => c.Id == customerId).Select(c => c.RequiresRawInspection).SingleAsync(cancellationToken));

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var year = request.ReceiptDate.Year;
        var receiptSequence = await numbers.NextAsync(ReceiptSeries, year, cancellationToken);
        var rollSequence = await numbers.NextRangeAsync(RollSeries, year, request.Lots.Sum(l => l.Rolls.Count), cancellationToken);

        var receipt = new BoyahaneFabricReceipt
        {
            ReceiptNo = $"BK-{year}-{receiptSequence:D5}",
            ReceiptDate = request.ReceiptDate,
            CustomerId = request.CustomerId,
            DespatchNo = NullIfBlank(request.DespatchNo),
            Notes = NullIfBlank(request.Notes),
            IsOpening = request.IsOpening,
        };

        var lotIndex = 0;
        foreach (var item in request.Lots)
        {
            var fabric = fabrics[item.FabricId];
            var lot = new BoyahaneFabricLot
            {
                LotNo = $"{receipt.ReceiptNo}/{++lotIndex}",
                FabricId = item.FabricId,
                CustomerFabricCode = NullIfBlank(item.CustomerFabricCode),
                DeclaredRollCount = item.DeclaredRollCount,
                DeclaredMeters = item.DeclaredMeters,
                DeclaredKilograms = item.DeclaredKilograms,
                InspectionRequired = inspectionRequired,
            };

            foreach (var roll in item.Rolls)
            {
                lot.Rolls.Add(new BoyahaneFabricRoll
                {
                    RollNo = $"BT-{year}-{rollSequence++:D6}",
                    CustomerRollNo = NullIfBlank(roll.CustomerRollNo),
                    Meters = roll.Meters ?? FabricConversion.MetersFromKilograms(roll.Kilograms!.Value, fabric.RawWidthCm, fabric.RawWeightGsm),
                    Kilograms = roll.Kilograms ?? FabricConversion.KilogramsFromMeters(roll.Meters!.Value, fabric.RawWidthCm, fabric.RawWeightGsm),
                    MetersMeasured = roll.Meters is not null,
                    KilogramsMeasured = roll.Kilograms is not null,
                    Status = inspectionRequired ? FabricRollStatus.PendingInspection : FabricRollStatus.Approved,
                    LocationId = locationId,
                });
            }

            receipt.Lots.Add(lot);
        }

        db.Set<BoyahaneFabricReceipt>().Add(receipt);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetAsync(receipt.Id, cancellationToken);
    }

    public async Task<Result<BoyahaneFabricReceiptDto>> UpdateAsync(
        int id, CreateBoyahaneFabricReceiptRequest request, CancellationToken cancellationToken)
    {
        var receipt = await db.Set<BoyahaneFabricReceipt>()
            .Include(r => r.Lots).ThenInclude(l => l.Rolls)
            .SingleOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (receipt is null)
        {
            return NotFound;
        }

        var existingRolls = receipt.Lots.SelectMany(l => l.Rolls).ToList();
        if (receipt.CustomerId != request.CustomerId && existingRolls.Any(IsLocked))
        {
            return new Error(LockedRoll, "Kontrol edilmiş ya da siparişe bağlanmış top varken malın sahibi değiştirilemez.", ErrorType.Conflict);
        }

        if (request.Lots.Any(l => l.Id is { } lotId && receipt.Lots.All(e => e.Id != lotId))
            || request.Lots.SelectMany(l => l.Rolls).Any(t => t.Id is { } rollId && existingRolls.All(e => e.Id != rollId)))
        {
            return ReferenceValidation.InvalidReference("lots", "Kumaş ya da top bu belgeye ait değil.");
        }

        foreach (var lot in receipt.Lots)
        {
            var item = request.Lots.FirstOrDefault(l => l.Id == lot.Id);
            foreach (var roll in lot.Rolls.Where(IsLocked))
            {
                // Toplar lotlar arasında taşınmaz: kilitli top kendi lotunda aynı kart ile kalmalıdır.
                if (item is null || item.FabricId != lot.FabricId || item.Rolls.All(t => t.Id != roll.Id))
                {
                    return new Error(LockedRoll, $"{roll.RollNo} kontrol edilmiş ya da siparişe bağlanmış; silinemez, kumaşı değiştirilemez.", ErrorType.Conflict);
                }
            }

            if (item is not null && item.Rolls.Any(t => t.Id is { } rollId && lot.Rolls.All(e => e.Id != rollId)))
            {
                return ReferenceValidation.InvalidReference("lots", "Top başka bir kumaşa taşınamaz.");
            }
        }

        if (await ReferenceValidation.RequireSelectableAsync(
                db.Set<BoyahaneCustomer>(), request.CustomerId, receipt.CustomerId, "customerId", "Müşteri", cancellationToken) is { } customerError)
        {
            return customerError;
        }

        var fabricIds = request.Lots.Select(l => l.FabricId).Distinct().ToList();
        foreach (var fabricId in fabricIds)
        {
            var current = receipt.Lots.Any(l => l.FabricId == fabricId) ? fabricId : (int?)null;
            if (await ReferenceValidation.RequireSelectableAsync(
                    db.Set<BoyahaneFabric>(), fabricId, current, "lots", "Kumaş kartı", cancellationToken) is { } fabricError)
            {
                return fabricError;
            }
        }

        var fabrics = await db.Set<BoyahaneFabric>().AsNoTracking()
            .Where(f => fabricIds.Contains(f.Id))
            .ToDictionaryAsync(f => f.Id, cancellationToken);
        var inspectionRequired = !request.IsOpening && (request.CustomerId is not { } customerId
            || await db.Set<BoyahaneCustomer>().Where(c => c.Id == customerId).Select(c => c.RequiresRawInspection).SingleAsync(cancellationToken));
        if (await BoyahaneWarehouseRules.ValidateLocationAsync(db, request.LocationId, cancellationToken) is { } locationError)
        {
            return locationError;
        }

        var locationId = request.LocationId ?? await BoyahaneWarehouseRules.DefaultLocationIdAsync(db, BoyahaneWarehouseType.RawFabric, cancellationToken);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var newRollCount = request.Lots.SelectMany(l => l.Rolls).Count(t => t.Id is null);
        var year = request.ReceiptDate.Year;
        var rollSequence = newRollCount > 0 ? await numbers.NextRangeAsync(RollSeries, year, newRollCount, cancellationToken) : 0;

        receipt.ReceiptDate = request.ReceiptDate;
        receipt.CustomerId = request.CustomerId;
        receipt.DespatchNo = NullIfBlank(request.DespatchNo);
        receipt.Notes = NullIfBlank(request.Notes);
        receipt.IsOpening = request.IsOpening;

        var removedLots = receipt.Lots.Where(l => request.Lots.All(i => i.Id != l.Id)).ToList();
        db.Set<BoyahaneFabricRoll>().RemoveRange(removedLots.SelectMany(l => l.Rolls));
        db.Set<BoyahaneFabricLot>().RemoveRange(removedLots);

        var lotIndex = receipt.Lots.Select(l => int.Parse(l.LotNo[(l.LotNo.LastIndexOf('/') + 1)..])).DefaultIfEmpty(0).Max();
        foreach (var item in request.Lots)
        {
            var fabric = fabrics[item.FabricId];
            var lot = item.Id is { } lotId ? receipt.Lots.Single(l => l.Id == lotId) : new BoyahaneFabricLot { LotNo = $"{receipt.ReceiptNo}/{++lotIndex}" };
            lot.FabricId = item.FabricId;
            lot.CustomerFabricCode = NullIfBlank(item.CustomerFabricCode);
            lot.DeclaredRollCount = item.DeclaredRollCount;
            lot.DeclaredMeters = item.DeclaredMeters;
            lot.DeclaredKilograms = item.DeclaredKilograms;
            lot.InspectionRequired = inspectionRequired;
            if (item.Id is null)
            {
                receipt.Lots.Add(lot);
            }

            db.Set<BoyahaneFabricRoll>().RemoveRange(lot.Rolls.Where(t => item.Rolls.All(i => i.Id != t.Id)));

            foreach (var rollItem in item.Rolls)
            {
                var roll = rollItem.Id is { } rollId ? lot.Rolls.Single(t => t.Id == rollId) : null;
                if (roll is not null && IsLocked(roll))
                {
                    continue;
                }

                if (roll is null)
                {
                    roll = new BoyahaneFabricRoll { RollNo = $"BT-{year}-{rollSequence++:D6}", LocationId = locationId };
                    lot.Rolls.Add(roll);
                }

                roll.CustomerRollNo = NullIfBlank(rollItem.CustomerRollNo);
                roll.Meters = rollItem.Meters ?? FabricConversion.MetersFromKilograms(rollItem.Kilograms!.Value, fabric.RawWidthCm, fabric.RawWeightGsm);
                roll.Kilograms = rollItem.Kilograms ?? FabricConversion.KilogramsFromMeters(rollItem.Meters!.Value, fabric.RawWidthCm, fabric.RawWeightGsm);
                roll.MetersMeasured = rollItem.Meters is not null;
                roll.KilogramsMeasured = rollItem.Kilograms is not null;
                roll.Status = inspectionRequired ? FabricRollStatus.PendingInspection : FabricRollStatus.Approved;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var receipt = await db.Set<BoyahaneFabricReceipt>()
            .Include(r => r.Lots).ThenInclude(l => l.Rolls)
            .SingleOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (receipt is null)
        {
            return NotFound;
        }

        if (HasInspectedRoll(receipt))
        {
            return new Error(ErrorCodes.RecordInUse, "Ham kontrolü yapılmış ya da siparişe bağlanmış top bulunan belge silinemez.", ErrorType.Conflict);
        }

        db.Set<BoyahaneFabricRoll>().RemoveRange(receipt.Lots.SelectMany(l => l.Rolls));
        db.Set<BoyahaneFabricLot>().RemoveRange(receipt.Lots);
        db.Set<BoyahaneFabricReceipt>().Remove(receipt);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private static bool HasInspectedRoll(BoyahaneFabricReceipt receipt) => receipt.Lots.Any(l => l.Rolls.Any(IsLocked));

    /// <summary>Kontrol edilmiş ya da siparişe bağlanmış top düzenlenemez, silinemez.</summary>
    private static bool IsLocked(BoyahaneFabricRoll roll) => roll.InspectedAt != null || roll.OrderLineId != null;
}
