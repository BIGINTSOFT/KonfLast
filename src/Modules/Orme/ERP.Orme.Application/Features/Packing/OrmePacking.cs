using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Core.Domain.Enums;
using ERP.Orme.Domain.Entities;
using ERP.Orme.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using static ERP.Core.Application.Features.MasterData.MasterDataRules;

namespace ERP.Orme.Application.Features.Packing;

public sealed class OrmePackingListQuery : PageRequest
{
    /// <summary>Çeki no, sipariş no, müşteri sipariş no ya da müşteri.</summary>
    public string? Search { get; set; }
}

public sealed record OrmePackingListItemDto(
    int Id,
    string PackingNo,
    string OrderNo,
    string? CustomerName,
    DateOnly PackingDate,
    int RollCount,
    decimal TotalKilograms,
    PackingListStatus Status,
    string? DespatchNo,
    string? BoyahaneReceiptNo);

/// <param name="Kilograms">Makinede tartılan kilo.</param>
/// <param name="MetersEstimated">Metre ölçülmedi, tahmindir.</param>
/// <param name="CustomerFabricCode">Müşterinin sipariş satırındaki kendi kumaş kodu.</param>
public sealed record OrmePackingItemDto(
    int Id,
    int RollId,
    string RollNo,
    string KnitOrderNo,
    string MachineCode,
    string FabricCode,
    string FabricName,
    string? CustomerFabricCode,
    decimal Kilograms,
    decimal Meters,
    bool MetersEstimated,
    FabricRollStatus Grade);

/// <param name="IsStock">Stoğa üretim siparişi: irsaliye için listede müşteri seçilir.</param>
/// <param name="IsCommission">Fason sipariş: Boyahane'ye gönderirken Boyahane müşterisi zorunlu.</param>
/// <param name="DocumentLanguage">Baskı dili: siparişin ya da satılan müşterinin kartından; müşteri yoksa Türkçe.</param>
public sealed record OrmePackingListDto(
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
    IReadOnlyList<OrmePackingItemDto> Items,
    bool IsStock,
    bool IsCommission,
    int? SaleCustomerId,
    int? DespatchId,
    string? DespatchNo,
    string? BoyahaneReceiptNo,
    DateOnly? BoyahaneSentOn,
    bool CanSendToBoyahane,
    DocumentLanguage DocumentLanguage);

/// <summary>Listeye alınabilecek top: 1. ya da 2. kalite, başka listede olmayan, siparişi onaylı.</summary>
public sealed record OrmePackingCandidateDto(
    int RollId,
    int OrderId,
    string OrderNo,
    string? CustomerName,
    string RollNo,
    string KnitOrderNo,
    string FabricCode,
    decimal Kilograms,
    decimal Meters,
    bool MetersEstimated,
    FabricRollStatus Grade,
    bool Selected);

public sealed record OrmePackingListRequest(DateOnly PackingDate, IReadOnlyList<int> RollIds, string? Notes = null);

public sealed record SetOrmePackingSaleCustomerRequest(int? CustomerId);

/// <param name="FabricId">Boyahane'deki kumaş kartı.</param>
/// <param name="CustomerId">Boyahane'deki malın sahibi; fasonda zorunlu, kendi malında boş.</param>
public sealed record SendOrmePackingToBoyahaneRequest(int FabricId, DateOnly Date, int? CustomerId = null);

public sealed class OrmePackingListRequestValidator : AbstractValidator<OrmePackingListRequest>
{
    public const int MaxRolls = 500;

    public OrmePackingListRequestValidator()
    {
        RuleFor(x => x.PackingDate).NotEmpty().WithMessage("Tarih zorunludur.");
        RuleFor(x => x.Notes).OptionalText(1000);
        RuleFor(x => x.RollIds)
            .NotEmpty().WithMessage("Listeye en az bir top seçilmelidir.")
            .Must(ids => ids.Count <= MaxRolls).WithMessage($"Bir listede en fazla {MaxRolls} top olabilir.");
    }
}

public sealed class SendOrmePackingToBoyahaneRequestValidator : AbstractValidator<SendOrmePackingToBoyahaneRequest>
{
    public SendOrmePackingToBoyahaneRequestValidator()
    {
        RuleFor(x => x.FabricId).GreaterThan(0).WithMessage("Boyahane kumaş kartı seçilmelidir.");
        RuleFor(x => x.Date).NotEmpty().WithMessage("Tarih zorunludur.");
    }
}

public interface IOrmePackingService
{
    Task<PagedResult<OrmePackingListItemDto>> GetListAsync(OrmePackingListQuery query, CancellationToken cancellationToken);
    Task<Result<OrmePackingListDto>> GetAsync(int id, CancellationToken cancellationToken);
    Task<IReadOnlyList<OrmePackingCandidateDto>> GetCandidatesAsync(int? packingListId, CancellationToken cancellationToken);
    Task<Result<OrmePackingListDto>> CreateAsync(OrmePackingListRequest request, CancellationToken cancellationToken);
    Task<Result<OrmePackingListDto>> UpdateAsync(int id, OrmePackingListRequest request, CancellationToken cancellationToken);
    Task<Result<OrmePackingListDto>> SetSaleCustomerAsync(int id, SetOrmePackingSaleCustomerRequest request, CancellationToken cancellationToken);
    Task<Result<OrmePackingListDto>> ConfirmAsync(int id, CancellationToken cancellationToken);
    Task<Result<OrmePackingListDto>> ReopenAsync(int id, CancellationToken cancellationToken);
    Task<Result<OrmePackingListDto>> CancelAsync(int id, CancellationToken cancellationToken);
    Task<Result<OrmePackingListDto>> SendToBoyahaneAsync(int id, SendOrmePackingToBoyahaneRequest request, CancellationToken cancellationToken);
}

/// <summary>
/// Çeki listesi: bir siparişin 1. ve 2. kalite topları. Liste onaylanınca ya irsaliyeyle müşteriye ya da (Boyahane modülü
/// yüklüyse) Boyahane'ye gider. Listedeki top yeniden kontrol edilemez.
/// </summary>
public sealed class OrmePackingService(IAppDbContext db, IDocumentNumberGenerator numbers, IEnumerable<IFabricTransferReceiver> receivers) : IOrmePackingService
{
    private const string Series = "OrmePacking";
    private const string InvalidState = "ORME_PACKING_INVALID_STATE";
    private const string InvalidItem = "ORME_PACKING_INVALID_ITEM";
    private const int MaxCandidates = 1000;

    /// <summary>Boyahane'ye gönderim sürerken aynı listenin ikinci kez gönderilmesini engelleyen geçici işaret.</summary>
    private const string SendingMarker = "GONDERILIYOR";

    private static readonly Error NotFound = new(ErrorCodes.NotFound, "Çeki listesi bulunamadı.", ErrorType.NotFound);
    private static readonly FabricRollStatus[] Shippable = [FabricRollStatus.FirstQuality, FabricRollStatus.SecondQuality];

    private readonly IFabricTransferReceiver? _receiver = receivers.FirstOrDefault();

    public Task<PagedResult<OrmePackingListItemDto>> GetListAsync(OrmePackingListQuery query, CancellationToken cancellationToken)
    {
        var lists = db.Set<OrmePackingList>().AsNoTracking();
        if (NullIfBlank(query.Search) is { } term)
        {
            lists = lists.Where(l => l.PackingNo.Contains(term)
                || l.Order.OrderNo.Contains(term)
                || (l.Order.CustomerOrderNo != null && l.Order.CustomerOrderNo.Contains(term))
                || (l.Order.Customer != null && l.Order.Customer.Name.Contains(term))
                || (l.SaleCustomer != null && l.SaleCustomer.Name.Contains(term)));
        }

        return lists
            .OrderByDescending(l => l.Id)
            .Select(l => new OrmePackingListItemDto(
                l.Id,
                l.PackingNo,
                l.Order.OrderNo,
                l.Order.Customer != null ? l.Order.Customer.Name : l.SaleCustomer != null ? l.SaleCustomer.Name : null,
                l.PackingDate,
                l.Items.Count(i => i.IsActive),
                l.Items.Where(i => i.IsActive).Sum(i => (decimal?)i.Kilograms) ?? 0,
                l.Status,
                l.Despatch != null ? l.Despatch.DespatchNo : null,
                l.BoyahaneReceiptNo))
            .ToPagedResultAsync(query, cancellationToken);
    }

    public async Task<Result<OrmePackingListDto>> GetAsync(int id, CancellationToken cancellationToken)
    {
        var list = await db.Set<OrmePackingList>().AsNoTracking()
            .Include(l => l.Order).ThenInclude(o => o.Customer)
            .Include(l => l.SaleCustomer)
            .Include(l => l.Despatch)
            .Include(l => l.Items).ThenInclude(i => i.Roll).ThenInclude(r => r.Machine)
            .Include(l => l.Items).ThenInclude(i => i.Roll).ThenInclude(r => r.KnitOrder).ThenInclude(k => k.OrderLine).ThenInclude(o => o.Fabric)
            .SingleOrDefaultAsync(l => l.Id == id, cancellationToken);
        if (list is null)
        {
            return NotFound;
        }

        var items = list.Items.Where(i => i.IsActive).OrderBy(i => i.Roll.RollNo).Select(i => new OrmePackingItemDto(
            i.Id,
            i.RollId,
            i.Roll.RollNo,
            i.Roll.KnitOrder.KnitOrderNo,
            i.Roll.Machine.Code,
            i.Roll.KnitOrder.OrderLine.Fabric.FabricCode,
            i.Roll.KnitOrder.OrderLine.Fabric.Name,
            i.Roll.KnitOrder.OrderLine.CustomerFabricCode,
            i.Kilograms,
            i.Meters,
            i.MetersEstimated,
            i.Grade)).ToList();

        return new OrmePackingListDto(
            list.Id,
            list.PackingNo,
            list.OrderId,
            list.Order.OrderNo,
            list.Order.CustomerOrderNo,
            list.Order.Customer?.Name ?? list.SaleCustomer?.Name,
            list.PackingDate,
            list.Notes,
            list.Status,
            items.Sum(i => i.Kilograms),
            items,
            list.Order.CustomerId is null,
            list.Order.Ownership == OrderOwnership.Commission,
            list.SaleCustomerId,
            list.DespatchId,
            list.Despatch?.DespatchNo,
            list.BoyahaneReceiptNo,
            list.BoyahaneSentOn,
            _receiver is not null,
            (list.Order.Customer ?? list.SaleCustomer)?.DocumentLanguage ?? DocumentLanguage.Turkish);
    }

    public async Task<IReadOnlyList<OrmePackingCandidateDto>> GetCandidatesAsync(int? packingListId, CancellationToken cancellationToken) =>
        await db.Set<OrmeFabricRoll>().AsNoTracking()
            .Where(r => Shippable.Contains(r.Status)
                && r.KnitOrder.OrderLine.Order.Status == OrderStatus.Confirmed
                && !db.Set<OrmePackingItem>().Any(i => i.RollId == r.Id && i.IsActive && i.PackingListId != packingListId))
            .OrderBy(r => r.KnitOrder.OrderLine.OrderId).ThenBy(r => r.RollNo)
            .Take(MaxCandidates)
            .Select(r => new OrmePackingCandidateDto(
                r.Id,
                r.KnitOrder.OrderLine.OrderId,
                r.KnitOrder.OrderLine.Order.OrderNo,
                r.KnitOrder.OrderLine.Order.Customer != null ? r.KnitOrder.OrderLine.Order.Customer.Name : null,
                r.RollNo,
                r.KnitOrder.KnitOrderNo,
                r.KnitOrder.OrderLine.Fabric.FabricCode,
                r.Kilograms,
                r.InspectedMeters ?? 0,
                r.InspectedMetersEstimated,
                r.Status,
                db.Set<OrmePackingItem>().Any(i => i.RollId == r.Id && i.IsActive && i.PackingListId == packingListId)))
            .ToListAsync(cancellationToken);

    public async Task<Result<OrmePackingListDto>> CreateAsync(OrmePackingListRequest request, CancellationToken cancellationToken)
    {
        var plan = await PlanAsync(request, null, cancellationToken);
        if (!plan.IsSuccess)
        {
            return plan.Error!;
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var year = request.PackingDate.Year;
        var sequence = await numbers.NextAsync(Series, year, cancellationToken);
        var list = new OrmePackingList { PackingNo = $"OC-{year}-{sequence:D5}" };
        Apply(list, request, plan.Value);
        db.Set<OrmePackingList>().Add(list);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetAsync(list.Id, cancellationToken);
    }

    public async Task<Result<OrmePackingListDto>> UpdateAsync(int id, OrmePackingListRequest request, CancellationToken cancellationToken)
    {
        var list = await db.Set<OrmePackingList>().Include(l => l.Items).SingleOrDefaultAsync(l => l.Id == id, cancellationToken);
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

        // Dizin çakışmasın diye çıkarılan satırlar önce silinir.
        db.Set<OrmePackingItem>().RemoveRange(list.Items.Where(i => !request.RollIds.Contains(i.RollId)).ToList());
        await db.SaveChangesAsync(cancellationToken);
        Apply(list, request, plan.Value);
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    /// <summary>Stoğa üretim siparişinin listesinde malın satıldığı müşteri; irsaliye bu müşteriye yazılır.</summary>
    public async Task<Result<OrmePackingListDto>> SetSaleCustomerAsync(int id, SetOrmePackingSaleCustomerRequest request, CancellationToken cancellationToken)
    {
        var list = await db.Set<OrmePackingList>().Include(l => l.Order).SingleOrDefaultAsync(l => l.Id == id, cancellationToken);
        if (list is null)
        {
            return NotFound;
        }

        if (list.Order.CustomerId is not null)
        {
            return new Error(InvalidState, "Müşterili siparişin listesinde müşteri seçilmez.", ErrorType.Conflict);
        }

        if (list.DespatchId is not null || list.BoyahaneReceiptNo is not null)
        {
            return new Error(InvalidState, "Sevk edilmiş listenin müşterisi değiştirilemez.", ErrorType.Conflict);
        }

        if (await ReferenceValidation.RequireSelectableAsync(db.Set<OrmeCustomer>(), request.CustomerId, list.SaleCustomerId, "customerId", "Müşteri", cancellationToken) is { } error)
        {
            return error;
        }

        list.SaleCustomerId = request.CustomerId;
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    public Task<Result<OrmePackingListDto>> ConfirmAsync(int id, CancellationToken cancellationToken) =>
        ChangeStatusAsync(id, PackingListStatus.Draft, PackingListStatus.Confirmed, "Yalnızca taslak liste onaylanabilir.", cancellationToken);

    public async Task<Result<OrmePackingListDto>> ReopenAsync(int id, CancellationToken cancellationToken)
    {
        var list = await db.Set<OrmePackingList>().AsNoTracking().SingleOrDefaultAsync(l => l.Id == id, cancellationToken);
        if (list is null)
        {
            return NotFound;
        }

        if (list.DespatchId is not null || list.BoyahaneReceiptNo is not null)
        {
            return new Error(InvalidState, "İrsaliyeye girmiş ya da Boyahane'ye gönderilmiş liste taslağa alınamaz.", ErrorType.Conflict);
        }

        return await ChangeStatusAsync(id, PackingListStatus.Confirmed, PackingListStatus.Draft, "Yalnızca onaylı liste taslağa alınabilir.", cancellationToken);
    }

    /// <summary>Taslak listeyi iptal eder; toplar boşa çıkar, satırlar geçmişte kalır.</summary>
    public async Task<Result<OrmePackingListDto>> CancelAsync(int id, CancellationToken cancellationToken)
    {
        var list = await db.Set<OrmePackingList>().Include(l => l.Items).SingleOrDefaultAsync(l => l.Id == id, cancellationToken);
        if (list is null)
        {
            return NotFound;
        }

        if (list.Status != PackingListStatus.Draft)
        {
            return new Error(InvalidState, "Yalnızca taslak liste iptal edilebilir.", ErrorType.Conflict);
        }

        foreach (var item in list.Items)
        {
            item.IsActive = false;
        }

        list.Status = PackingListStatus.Cancelled;
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    public async Task<Result<OrmePackingListDto>> SendToBoyahaneAsync(int id, SendOrmePackingToBoyahaneRequest request, CancellationToken cancellationToken)
    {
        if (_receiver is null)
        {
            return new Error(InvalidState, "Boyahane modülü bu kurulumda yok.", ErrorType.Conflict);
        }

        var list = await db.Set<OrmePackingList>().AsNoTracking()
            .Include(l => l.Order)
            .Include(l => l.Items).ThenInclude(i => i.Roll)
            .SingleOrDefaultAsync(l => l.Id == id, cancellationToken);
        if (list is null)
        {
            return NotFound;
        }

        if (list.Status != PackingListStatus.Confirmed)
        {
            return new Error(InvalidState, "Yalnızca onaylı liste Boyahane'ye gönderilir.", ErrorType.Conflict);
        }

        if (list.DespatchId is not null || list.BoyahaneReceiptNo is not null)
        {
            return new Error(InvalidState, "Liste zaten sevk edilmiş ya da Boyahane'ye gönderilmiş.", ErrorType.Conflict);
        }

        var commission = list.Order.Ownership == OrderOwnership.Commission;
        if (commission && request.CustomerId is null)
        {
            return ReferenceValidation.InvalidReference("customerId", "Fason siparişte Boyahane müşterisi seçilmelidir.");
        }

        // Kendi malı Boyahane'de işletme malı olur.
        var customerId = commission ? request.CustomerId : null;

        // Çift gönderimi engeller: yalnızca bir istek işareti koyabilir.
        var claimed = await db.Set<OrmePackingList>()
            .Where(l => l.Id == id && l.BoyahaneReceiptNo == null && l.DespatchId == null)
            .ExecuteUpdateAsync(s => s.SetProperty(l => l.BoyahaneReceiptNo, SendingMarker), cancellationToken);
        if (claimed == 0)
        {
            return new Error(InvalidState, "Liste zaten sevk edilmiş ya da Boyahane'ye gönderilmiş.", ErrorType.Conflict);
        }

        var rolls = list.Items.Where(i => i.IsActive).OrderBy(i => i.Roll.RollNo)
            .Select(i => new FabricTransferRoll(i.Roll.RollNo, i.MetersEstimated ? null : i.Meters, i.Kilograms))
            .ToList();
        Result<string> result;
        try
        {
            result = await _receiver.ReceiveAsync(new FabricTransferRequest(request.Date, customerId, request.FabricId, list.PackingNo, rolls, "Örme'den gönderildi."), cancellationToken);
        }
        catch
        {
            await db.Set<OrmePackingList>().Where(l => l.Id == id).ExecuteUpdateAsync(s => s.SetProperty(l => l.BoyahaneReceiptNo, (string?)null), CancellationToken.None);
            throw;
        }

        var receiptNo = result.IsSuccess ? result.Value : null;
        DateOnly? sentOn = result.IsSuccess ? request.Date : null;
        await db.Set<OrmePackingList>()
            .Where(l => l.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(l => l.BoyahaneReceiptNo, receiptNo)
                .SetProperty(l => l.BoyahaneSentOn, sentOn), cancellationToken);

        return result.IsSuccess ? await GetAsync(id, cancellationToken) : result.Error!;
    }

    private async Task<Result<OrmePackingListDto>> ChangeStatusAsync(int id, PackingListStatus from, PackingListStatus to, string message, CancellationToken cancellationToken)
    {
        var list = await db.Set<OrmePackingList>().SingleOrDefaultAsync(l => l.Id == id, cancellationToken);
        if (list is null)
        {
            return NotFound;
        }

        if (list.Status != from)
        {
            return new Error(InvalidState, message, ErrorType.Conflict);
        }

        list.Status = to;
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    private sealed record PackingPlan(int OrderId, IReadOnlyList<OrmeFabricRoll> Rolls);

    /// <summary>Seçilen toplar aynı siparişten, 1. ya da 2. kalite, başka listede olmayan olmalıdır.</summary>
    private async Task<Result<PackingPlan>> PlanAsync(OrmePackingListRequest request, OrmePackingList? existing, CancellationToken cancellationToken)
    {
        var ids = request.RollIds.Distinct().ToList();
        var rolls = await db.Set<OrmeFabricRoll>().AsNoTracking()
            .Include(r => r.KnitOrder).ThenInclude(k => k.OrderLine).ThenInclude(l => l.Order)
            .Where(r => ids.Contains(r.Id))
            .ToListAsync(cancellationToken);
        if (rolls.Count != ids.Count)
        {
            return new Error(InvalidItem, "Seçilen toplardan biri bulunamadı.", ErrorType.Conflict);
        }

        var orderIds = rolls.Select(r => r.KnitOrder.OrderLine.OrderId).Distinct().ToList();
        if (orderIds.Count > 1)
        {
            return new Error(InvalidItem, "Bir çeki listesindeki toplar aynı siparişten olmalıdır.", ErrorType.Conflict);
        }

        var busy = await db.Set<OrmePackingItem>()
            .Where(i => ids.Contains(i.RollId) && i.IsActive && (existing == null || i.PackingListId != existing.Id))
            .Select(i => i.RollId)
            .ToListAsync(cancellationToken);
        foreach (var roll in rolls)
        {
            var problem = roll.Status == FabricRollStatus.PendingInspection ? "kontrol edilmedi"
                : roll.Status == FabricRollStatus.Rejected ? "red; önce yetkili kararı gerekir"
                : roll.KnitOrder.OrderLine.Order.Status != OrderStatus.Confirmed ? "siparişi onaylı değil"
                : busy.Contains(roll.Id) ? "başka bir listede"
                : null;
            if (problem is not null)
            {
                return new Error(InvalidItem, $"{roll.RollNo} listeye alınamaz: {problem}.", ErrorType.Conflict);
            }
        }

        return new PackingPlan(orderIds[0], rolls);
    }

    private static void Apply(OrmePackingList list, OrmePackingListRequest request, PackingPlan plan)
    {
        list.OrderId = plan.OrderId;
        list.PackingDate = request.PackingDate;
        list.Notes = NullIfBlank(request.Notes);
        foreach (var roll in plan.Rolls.Where(r => list.Items.All(i => i.RollId != r.Id)))
        {
            list.Items.Add(new OrmePackingItem
            {
                RollId = roll.Id,
                Kilograms = roll.Kilograms,
                Meters = roll.InspectedMeters!.Value,
                MetersEstimated = roll.InspectedMetersEstimated,
                Grade = roll.Status,
            });
        }
    }
}
