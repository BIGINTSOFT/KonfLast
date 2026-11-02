using ERP.Boyahane.Application.Features.Warehousing;
using ERP.Boyahane.Domain.Entities;
using ERP.Boyahane.Domain.Enums;
using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Core.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using static ERP.Core.Application.Features.MasterData.MasterDataRules;

namespace ERP.Boyahane.Application.Features.Packing;

public sealed class BoyahanePackingListQuery : PageRequest
{
    /// <summary>Çeki no, sipariş no, müşteri sipariş no ya da müşteri.</summary>
    public string? Search { get; set; }
}

public sealed record BoyahanePackingListItemDto(
    int Id,
    string PackingNo,
    string OrderNo,
    string? CustomerName,
    DateOnly PackingDate,
    int PackageCount,
    decimal TotalKilograms,
    decimal TotalMeters,
    PackingListStatus Status);

/// <param name="CustomerItemNo">Müşterinin top ya da lot numarası.</param>
/// <param name="CustomerFabricCode">Müşterinin kabul irsaliyesindeki kendi kumaş kodu; iplikte boş.</param>
public sealed record BoyahanePackageDto(
    int Id,
    int BatchItemId,
    string PackageNo,
    string ItemNo,
    int BatchId,
    string BatchNo,
    string CardCode,
    string CardName,
    string? ColorName,
    string? ColorCode,
    decimal Kilograms,
    decimal? Meters,
    int BoxCount,
    bool IsYarn,
    string? CustomerItemNo,
    string? CustomerFabricCode);

/// <param name="DocumentLanguage">Baskının dili: siparişin ya da satılan müşterinin belge dili.</param>
public sealed record BoyahanePackingListDto(
    int Id,
    string PackingNo,
    int OrderId,
    string OrderNo,
    string? CustomerOrderNo,
    string? CustomerName,
    DateOnly PackingDate,
    string? Notes,
    PackingListStatus Status,
    decimal TotalKilograms,
    decimal TotalMeters,
    int TotalBoxes,
    IReadOnlyList<BoyahanePackageDto> Packages,
    bool IsStock,
    int? SaleCustomerId,
    bool IsDespatched,
    DocumentLanguage DocumentLanguage);

public sealed record SetPackingSaleCustomerRequest(int? CustomerId);

/// <summary>Listeye alınabilecek mal: final kalitede uygun, başka listede olmayan. Sipariş sipariş gelir.</summary>
public sealed record BoyahanePackingCandidateDto(
    int BatchItemId,
    int OrderId,
    string OrderNo,
    string? CustomerName,
    string ItemNo,
    string BatchNo,
    string CardCode,
    string? ColorName,
    decimal Kilograms,
    decimal? Meters,
    bool IsYarn,
    int BoxCount,
    bool Selected);

public sealed record BoyahanePackingItemRequest(int BatchItemId, int BoxCount = 1);

public sealed record BoyahanePackingListRequest(DateOnly PackingDate, IReadOnlyList<BoyahanePackingItemRequest> Items, string? Notes = null);

public sealed class BoyahanePackingListRequestValidator : AbstractValidator<BoyahanePackingListRequest>
{
    public BoyahanePackingListRequestValidator()
    {
        RuleFor(x => x.PackingDate).NotEmpty().WithMessage("Tarih zorunludur.");
        RuleFor(x => x.Notes).OptionalText(1000);
        RuleFor(x => x.Items).NotEmpty().WithMessage("Listeye en az bir top ya da lot seçilmelidir.");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.BatchItemId).GreaterThan(0);
            item.RuleFor(i => i.BoxCount).InclusiveBetween(1, 100_000).WithMessage("Koli adedi en az 1 olmalıdır.");
        });
    }
}

public interface IBoyahanePackingService
{
    Task<PagedResult<BoyahanePackingListItemDto>> GetListAsync(BoyahanePackingListQuery query, CancellationToken cancellationToken);
    Task<Result<BoyahanePackingListDto>> GetAsync(int id, CancellationToken cancellationToken);
    Task<IReadOnlyList<BoyahanePackingCandidateDto>> GetCandidatesAsync(int? packingListId, CancellationToken cancellationToken);
    Task<Result<BoyahanePackingListDto>> CreateAsync(BoyahanePackingListRequest request, CancellationToken cancellationToken);
    Task<Result<BoyahanePackingListDto>> UpdateAsync(int id, BoyahanePackingListRequest request, CancellationToken cancellationToken);
    Task<Result<BoyahanePackingListDto>> SetSaleCustomerAsync(int id, SetPackingSaleCustomerRequest request, CancellationToken cancellationToken);
    Task<Result<BoyahanePackingListDto>> ConfirmAsync(int id, CancellationToken cancellationToken);
    Task<Result<BoyahanePackingListDto>> ReopenAsync(int id, CancellationToken cancellationToken);
    Task<Result<BoyahanePackingListDto>> CancelAsync(int id, CancellationToken cancellationToken);
}

public sealed class BoyahanePackingService(IAppDbContext db, IDocumentNumberGenerator numbers, TimeProvider timeProvider) : IBoyahanePackingService
{
    private const string Series = "BoyahanePacking";
    private const string InvalidState = "PACKING_INVALID_STATE";
    private const string InvalidItem = "PACKING_INVALID_ITEM";
    private const int MaxCandidates = 1000;

    private static readonly Error NotFound = new(ErrorCodes.NotFound, "Çeki listesi bulunamadı.", ErrorType.NotFound);

    public Task<PagedResult<BoyahanePackingListItemDto>> GetListAsync(BoyahanePackingListQuery query, CancellationToken cancellationToken)
    {
        var lists = db.Set<BoyahanePackingList>().AsNoTracking();
        if (NullIfBlank(query.Search) is { } term)
        {
            lists = lists.Where(l => l.PackingNo.Contains(term) || l.Order.OrderNo.Contains(term)
                || (l.Order.CustomerOrderNo != null && l.Order.CustomerOrderNo.Contains(term))
                || (l.Order.Customer != null && l.Order.Customer.Name.Contains(term)) || (l.SaleCustomer != null && l.SaleCustomer.Name.Contains(term)));
        }

        return lists
            .OrderByDescending(l => l.Id)
            .Select(l => new BoyahanePackingListItemDto(
                l.Id,
                l.PackingNo,
                l.Order.OrderNo,
                l.Order.Customer != null ? l.Order.Customer.Name : l.SaleCustomer != null ? l.SaleCustomer.Name : null,
                l.PackingDate,
                l.Items.Count,
                l.Items.Sum(i => i.Kilograms),
                l.Items.Sum(i => i.Meters ?? 0),
                l.Status))
            .ToPagedResultAsync(query, cancellationToken);
    }

    public async Task<Result<BoyahanePackingListDto>> GetAsync(int id, CancellationToken cancellationToken)
    {
        var list = await db.Set<BoyahanePackingList>().AsNoTracking()
            .Include(l => l.Order).ThenInclude(o => o.Customer)
            .Include(l => l.SaleCustomer)
            .Include(l => l.Items).ThenInclude(i => i.BatchItem).ThenInclude(b => b.Batch)
            .Include(l => l.Items).ThenInclude(i => i.BatchItem).ThenInclude(b => b.Roll!).ThenInclude(r => r.Lot)
            .Include(l => l.Items).ThenInclude(i => i.BatchItem).ThenInclude(b => b.YarnLot)
            .Include(l => l.Items).ThenInclude(i => i.BatchItem).ThenInclude(b => b.OrderLine).ThenInclude(o => o.Fabric)
            .Include(l => l.Items).ThenInclude(i => i.BatchItem).ThenInclude(b => b.OrderLine).ThenInclude(o => o.Yarn)
            .SingleOrDefaultAsync(l => l.Id == id, cancellationToken);
        if (list is null)
        {
            return NotFound;
        }

        return new BoyahanePackingListDto(
            list.Id,
            list.PackingNo,
            list.OrderId,
            list.Order.OrderNo,
            list.Order.CustomerOrderNo,
            list.Order.Customer?.Name ?? list.SaleCustomer?.Name,
            list.PackingDate,
            list.Notes,
            list.Status,
            list.Items.Sum(i => i.Kilograms),
            list.Items.Sum(i => i.Meters ?? 0),
            list.Items.Sum(i => i.BoxCount),
            list.Items.OrderBy(i => i.PackageNo).Select(i =>
            {
                var line = i.BatchItem.OrderLine;
                return new BoyahanePackageDto(
                    i.Id,
                    i.BatchItemId,
                    i.PackageNo,
                    i.BatchItem.Roll?.RollNo ?? i.BatchItem.YarnLot?.LotNo ?? "",
                    i.BatchItem.BatchId,
                    i.BatchItem.Batch.BatchNo,
                    line.Fabric?.FabricCode ?? line.Yarn?.YarnCode ?? "",
                    line.Fabric?.Name ?? line.Yarn?.Name ?? "",
                    line.ColorName,
                    line.ColorCode,
                    i.Kilograms,
                    i.Meters,
                    i.BoxCount,
                    i.BatchItem.YarnLotId != null,
                    i.BatchItem.Roll?.CustomerRollNo ?? i.BatchItem.YarnLot?.CustomerLotNo,
                    i.BatchItem.Roll?.Lot.CustomerFabricCode);
            }).ToList(),
            list.Order.CustomerId is null,
            list.SaleCustomerId,
            list.DespatchId is not null,
            (list.Order.Customer ?? list.SaleCustomer)?.DocumentLanguage ?? DocumentLanguage.Turkish);
    }

    public async Task<IReadOnlyList<BoyahanePackingCandidateDto>> GetCandidatesAsync(int? packingListId, CancellationToken cancellationToken)
    {
        var mine = packingListId is { } id
            ? await db.Set<BoyahanePackingItem>().AsNoTracking().Where(i => i.PackingListId == id && i.IsActive).ToDictionaryAsync(i => i.BatchItemId, i => i.BoxCount, cancellationToken)
            : [];
        var taken = db.Set<BoyahanePackingItem>().Where(i => i.IsActive && i.PackingListId != packingListId).Select(i => i.BatchItemId);

        var rows = await db.Set<BoyahaneBatchItem>().AsNoTracking()
            .Where(i => i.FinalResult == FinalQualityResult.Approved && !taken.Contains(i.Id) && i.OrderLine.Order.Status != OrderStatus.Closed)
            .OrderBy(i => i.OrderLine.OrderId).ThenBy(i => i.Id)
            .Take(MaxCandidates)
            .Select(i => new
            {
                i.Id,
                i.OrderLine.OrderId,
                i.OrderLine.Order.OrderNo,
                CustomerName = i.OrderLine.Order.Customer != null ? i.OrderLine.Order.Customer.Name : null,
                ItemNo = i.Roll != null ? i.Roll.RollNo : i.YarnLot != null ? i.YarnLot.LotNo : "",
                i.Batch.BatchNo,
                CardCode = i.OrderLine.Fabric != null ? i.OrderLine.Fabric.FabricCode : i.OrderLine.Yarn != null ? i.OrderLine.Yarn.YarnCode : "",
                i.OrderLine.ColorName,
                Kilograms = i.FinalKilograms ?? i.Kilograms,
                Meters = i.FinalMeters ?? i.Meters,
                IsYarn = i.YarnLotId != null,
            })
            .ToListAsync(cancellationToken);

        return rows.Select(r => new BoyahanePackingCandidateDto(
            r.Id, r.OrderId, r.OrderNo, r.CustomerName, r.ItemNo, r.BatchNo, r.CardCode, r.ColorName, r.Kilograms, r.Meters, r.IsYarn,
            mine.GetValueOrDefault(r.Id, 1), mine.ContainsKey(r.Id))).ToList();
    }

    public async Task<Result<BoyahanePackingListDto>> CreateAsync(BoyahanePackingListRequest request, CancellationToken cancellationToken)
    {
        var plan = await PlanAsync(request, null, cancellationToken);
        if (!plan.IsSuccess)
        {
            return plan.Error!;
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var year = timeProvider.GetUtcNow().Year;
        var sequence = await numbers.NextAsync(Series, year, cancellationToken);
        var list = new BoyahanePackingList { PackingNo = $"BC-{year}-{sequence:D5}", OrderId = plan.Value.OrderId };
        Apply(list, request, plan.Value);
        db.Set<BoyahanePackingList>().Add(list);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetAsync(list.Id, cancellationToken);
    }

    public async Task<Result<BoyahanePackingListDto>> UpdateAsync(int id, BoyahanePackingListRequest request, CancellationToken cancellationToken)
    {
        var list = await db.Set<BoyahanePackingList>().Include(l => l.Items).SingleOrDefaultAsync(l => l.Id == id, cancellationToken);
        if (list is null)
        {
            return NotFound;
        }

        if (list.Status != PackingListStatus.Draft)
        {
            return new Error(InvalidState, "Yalnızca taslak liste düzenlenebilir.", ErrorType.Conflict);
        }

        var plan = await PlanAsync(request, list, cancellationToken);
        if (!plan.IsSuccess)
        {
            return plan.Error!;
        }

        Apply(list, request, plan.Value);
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    public Task<Result<BoyahanePackingListDto>> ConfirmAsync(int id, CancellationToken cancellationToken) =>
        ChangeStatusAsync(id, PackingListStatus.Draft, PackingListStatus.Confirmed, "Yalnızca taslak liste onaylanabilir.", cancellationToken);

    public async Task<Result<BoyahanePackingListDto>> ReopenAsync(int id, CancellationToken cancellationToken)
    {
        if (await db.Set<BoyahanePackingList>().AnyAsync(l => l.Id == id && l.DespatchId != null, cancellationToken))
        {
            return new Error(InvalidState, "İrsaliyeye girmiş liste taslağa alınamaz; önce irsaliyeden çıkarın.", ErrorType.Conflict);
        }

        return await ChangeStatusAsync(id, PackingListStatus.Confirmed, PackingListStatus.Draft, "Yalnızca onaylı liste taslağa alınabilir.", cancellationToken);
    }

    public async Task<Result<BoyahanePackingListDto>> CancelAsync(int id, CancellationToken cancellationToken)
    {
        var list = await db.Set<BoyahanePackingList>().Include(l => l.Items).SingleOrDefaultAsync(l => l.Id == id, cancellationToken);
        if (list is null)
        {
            return NotFound;
        }

        if (list.Status != PackingListStatus.Draft)
        {
            return new Error(InvalidState, "Yalnızca taslak liste iptal edilebilir.", ErrorType.Conflict);
        }

        list.Status = PackingListStatus.Cancelled;
        foreach (var item in list.Items)
        {
            item.IsActive = false;
        }

        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    /// <summary>Stoğa üretilen malı satışa bağlar; irsaliyeye bu müşteriyle girer. İrsaliyeye girmiş listede değişmez.</summary>
    public async Task<Result<BoyahanePackingListDto>> SetSaleCustomerAsync(int id, SetPackingSaleCustomerRequest request, CancellationToken cancellationToken)
    {
        var list = await db.Set<BoyahanePackingList>().Include(l => l.Order).SingleOrDefaultAsync(l => l.Id == id, cancellationToken);
        if (list is null)
        {
            return NotFound;
        }

        if (list.Order.CustomerId is not null)
        {
            return new Error(InvalidState, "Müşterili siparişin malı başka müşteriye satılamaz.", ErrorType.Conflict);
        }

        if (list.Status == PackingListStatus.Cancelled || list.DespatchId is not null)
        {
            return new Error(InvalidState, "İptal edilmiş ya da irsaliyeye girmiş listede müşteri değiştirilemez.", ErrorType.Conflict);
        }

        if (await ReferenceValidation.RequireSelectableAsync(db.Set<BoyahaneCustomer>(), request.CustomerId, list.SaleCustomerId, "customerId", "Müşteri", cancellationToken) is { } customerError)
        {
            return customerError;
        }

        list.SaleCustomerId = request.CustomerId;
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    private async Task<Result<BoyahanePackingListDto>> ChangeStatusAsync(int id, PackingListStatus from, PackingListStatus to, string message, CancellationToken cancellationToken)
    {
        var list = await db.Set<BoyahanePackingList>().Include(l => l.Items).SingleOrDefaultAsync(l => l.Id == id, cancellationToken);
        if (list is null)
        {
            return NotFound;
        }

        if (list.Status != from)
        {
            return new Error(InvalidState, message, ErrorType.Conflict);
        }

        list.Status = to;
        if (to == PackingListStatus.Confirmed)
        {
            // Yeniden onaylanan listede daha önce taşınmış paketin adresi korunur.
            var locationId = await BoyahaneWarehouseRules.DefaultLocationIdAsync(db, BoyahaneWarehouseType.FinishedGoods, cancellationToken);
            foreach (var item in list.Items.Where(i => i.IsActive && i.LocationId is null))
            {
                item.LocationId = locationId;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    private sealed record PackingPlan(int OrderId, IReadOnlyList<BoyahaneBatchItem> Items);

    /// <summary>Listedeki mal final kalitede uygun, aynı siparişten ve başka bir listede olmayan maldır.</summary>
    private async Task<Result<PackingPlan>> PlanAsync(BoyahanePackingListRequest request, BoyahanePackingList? existing, CancellationToken cancellationToken)
    {
        var ids = request.Items.Select(i => i.BatchItemId).Distinct().ToList();
        var items = await db.Set<BoyahaneBatchItem>().AsNoTracking()
            .Include(i => i.OrderLine).ThenInclude(l => l.Order)
            .Include(i => i.Roll)
            .Include(i => i.YarnLot)
            .Where(i => ids.Contains(i.Id))
            .ToListAsync(cancellationToken);
        if (items.Count != ids.Count)
        {
            return new Error(InvalidItem, "Seçilen mallardan biri bulunamadı.", ErrorType.Conflict);
        }

        var unapproved = items.FirstOrDefault(i => i.FinalResult != FinalQualityResult.Approved);
        if (unapproved is not null)
        {
            return new Error(InvalidItem, $"{unapproved.Roll?.RollNo ?? unapproved.YarnLot?.LotNo} final kalitede uygun değil.", ErrorType.Conflict);
        }

        var orders = items.Select(i => i.OrderLine.OrderId).Distinct().ToList();
        if (orders.Count != 1)
        {
            return new Error(InvalidItem, "Çeki listesi tek siparişin malından oluşur.", ErrorType.Conflict);
        }

        if (existing is not null && existing.OrderId != orders[0])
        {
            return new Error(InvalidItem, "Listenin siparişi değiştirilemez.", ErrorType.Conflict);
        }

        if (items[0].OrderLine.Order.Status == OrderStatus.Closed)
        {
            return new Error(InvalidItem, "Kapatılmış siparişin malı çeki listesine alınamaz.", ErrorType.Conflict);
        }

        var existingId = existing?.Id;
        var taken = await db.Set<BoyahanePackingItem>().AsNoTracking()
            .Where(i => i.IsActive && i.PackingListId != existingId && ids.Contains(i.BatchItemId))
            .Select(i => i.PackingList.PackingNo)
            .FirstOrDefaultAsync(cancellationToken);
        if (taken is not null)
        {
            return new Error(InvalidItem, $"Seçilen mal {taken} listesinde.", ErrorType.Conflict);
        }

        return new PackingPlan(orders[0], items);
    }

    /// <summary>
    /// Listeyi istekle eşler. Listede kalan malın paketi (numarası, etiketi, depo yeri) korunur, yalnızca koli adedi
    /// güncellenir; çıkarılan malın paketi silinir, eklenen mal sıradaki numarayı alır.
    /// </summary>
    private void Apply(BoyahanePackingList list, BoyahanePackingListRequest request, PackingPlan plan)
    {
        list.PackingDate = request.PackingDate;
        list.Notes = NullIfBlank(request.Notes);
        var boxes = request.Items.GroupBy(i => i.BatchItemId).ToDictionary(g => g.Key, g => g.Last().BoxCount);
        var selected = plan.Items.Select(i => i.Id).ToHashSet();

        // Silinen paketin numarası yeniden verilmez; basılmış etiket başka mala karışmaz.
        var sequence = list.Items.Select(i => PackageSequence(list.PackingNo, i.PackageNo)).DefaultIfEmpty(0).Max();
        foreach (var removed in list.Items.Where(i => !selected.Contains(i.BatchItemId)).ToList())
        {
            list.Items.Remove(removed);
            db.Set<BoyahanePackingItem>().Remove(removed);
        }

        var kept = list.Items.ToDictionary(i => i.BatchItemId);
        foreach (var item in plan.Items.OrderBy(i => i.Id))
        {
            var boxCount = item.YarnLotId is null ? 1 : boxes[item.Id];
            if (kept.TryGetValue(item.Id, out var package))
            {
                package.BoxCount = boxCount;
                continue;
            }

            list.Items.Add(new BoyahanePackingItem
            {
                // Numara liste numarasından türer; etiket barkodu olarak okutulur.
                PackageNo = $"{list.PackingNo}-{++sequence:D3}",
                BatchItemId = item.Id,
                Kilograms = item.FinalKilograms ?? item.Kilograms,
                Meters = item.FinalMeters ?? item.Meters,
                BoxCount = boxCount,
            });
        }
    }

    /// <summary>Paket numarasının liste numarasından sonraki sıra kısmı; okunamazsa sıfır.</summary>
    private static int PackageSequence(string packingNo, string packageNo) =>
        packageNo.StartsWith(packingNo + "-", StringComparison.Ordinal) && int.TryParse(packageNo.AsSpan(packingNo.Length + 1), out var sequence)
            ? sequence
            : 0;
}
