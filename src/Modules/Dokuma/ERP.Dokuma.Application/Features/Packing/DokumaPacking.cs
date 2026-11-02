using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Core.Domain.Enums;
using ERP.Dokuma.Domain.Entities;
using ERP.Dokuma.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using static ERP.Core.Application.Features.MasterData.MasterDataRules;

namespace ERP.Dokuma.Application.Features.Packing;

public sealed class DokumaPackingListQuery : PageRequest
{
    /// <summary>Çeki no, sipariş no, müşteri sipariş no ya da müşteri.</summary>
    public string? Search { get; set; }
}

public sealed record DokumaPackingListItemDto(
    int Id,
    string PackingNo,
    string OrderNo,
    string? CustomerOrderNo,
    string? CustomerName,
    DateOnly PackingDate,
    int RollCount,
    decimal TotalMeters,
    PackingListStatus Status,
    string? DespatchNo,
    string? BoyahaneReceiptNo);

/// <param name="CustomerArticleCode">Müşterinin artikel için kullandığı kendi kodu (sipariş satırından).</param>
public sealed record DokumaPackingItemDto(
    int Id,
    int RollId,
    string RollNo,
    string BeamNo,
    string LoomCode,
    string ArticleCode,
    string ArticleName,
    decimal Meters,
    decimal? Kilograms,
    FabricRollStatus Grade,
    string? CustomerArticleCode);

/// <param name="IsStock">Stoğa üretim siparişi: irsaliye için listede müşteri seçilir.</param>
/// <param name="IsCommission">Fason sipariş: Boyahane'ye gönderirken Boyahane müşterisi zorunlu.</param>
public sealed record DokumaPackingListDto(
    int Id,
    string PackingNo,
    int OrderId,
    string OrderNo,
    string? CustomerOrderNo,
    string? CustomerName,
    DateOnly PackingDate,
    string? Notes,
    PackingListStatus Status,
    decimal TotalMeters,
    IReadOnlyList<DokumaPackingItemDto> Items,
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
public sealed record DokumaPackingCandidateDto(
    int RollId,
    int OrderId,
    string OrderNo,
    string? CustomerName,
    string RollNo,
    string BeamNo,
    string ArticleCode,
    decimal Meters,
    decimal? Kilograms,
    FabricRollStatus Grade,
    bool Selected);

public sealed record DokumaPackingListRequest(DateOnly PackingDate, IReadOnlyList<int> RollIds, string? Notes = null);

public sealed record SetDokumaPackingSaleCustomerRequest(int? CustomerId);

/// <param name="FabricId">Boyahane'deki kumaş kartı.</param>
/// <param name="CustomerId">Boyahane'deki malın sahibi; fasonda zorunlu, kendi malında boş.</param>
public sealed record SendDokumaPackingToBoyahaneRequest(int FabricId, DateOnly Date, int? CustomerId = null);

public sealed class DokumaPackingListRequestValidator : AbstractValidator<DokumaPackingListRequest>
{
    public const int MaxRolls = 500;

    public DokumaPackingListRequestValidator()
    {
        RuleFor(x => x.PackingDate).NotEmpty().WithMessage("Tarih zorunludur.");
        RuleFor(x => x.Notes).OptionalText(1000);
        RuleFor(x => x.RollIds)
            .NotEmpty().WithMessage("Listeye en az bir top seçilmelidir.")
            .Must(ids => ids.Count <= MaxRolls).WithMessage($"Bir listede en fazla {MaxRolls} top olabilir.");
    }
}

public sealed class SendDokumaPackingToBoyahaneRequestValidator : AbstractValidator<SendDokumaPackingToBoyahaneRequest>
{
    public SendDokumaPackingToBoyahaneRequestValidator()
    {
        RuleFor(x => x.FabricId).GreaterThan(0).WithMessage("Boyahane kumaş kartı seçilmelidir.");
        RuleFor(x => x.Date).NotEmpty().WithMessage("Tarih zorunludur.");
    }
}

public interface IDokumaPackingService
{
    Task<PagedResult<DokumaPackingListItemDto>> GetListAsync(DokumaPackingListQuery query, CancellationToken cancellationToken);
    Task<Result<DokumaPackingListDto>> GetAsync(int id, CancellationToken cancellationToken);
    Task<IReadOnlyList<DokumaPackingCandidateDto>> GetCandidatesAsync(int? packingListId, CancellationToken cancellationToken);
    Task<Result<DokumaPackingListDto>> CreateAsync(DokumaPackingListRequest request, CancellationToken cancellationToken);
    Task<Result<DokumaPackingListDto>> UpdateAsync(int id, DokumaPackingListRequest request, CancellationToken cancellationToken);
    Task<Result<DokumaPackingListDto>> SetSaleCustomerAsync(int id, SetDokumaPackingSaleCustomerRequest request, CancellationToken cancellationToken);
    Task<Result<DokumaPackingListDto>> ConfirmAsync(int id, CancellationToken cancellationToken);
    Task<Result<DokumaPackingListDto>> ReopenAsync(int id, CancellationToken cancellationToken);
    Task<Result<DokumaPackingListDto>> CancelAsync(int id, CancellationToken cancellationToken);
    Task<Result<DokumaPackingListDto>> SendToBoyahaneAsync(int id, SendDokumaPackingToBoyahaneRequest request, CancellationToken cancellationToken);
}

/// <summary>
/// Çeki listesi: bir siparişin 1. ve 2. kalite topları. Liste onaylanınca ya irsaliyeyle müşteriye ya da (Boyahane modülü
/// yüklüyse) Boyahane'ye gider. Listedeki top yeniden kontrol edilemez.
/// </summary>
public sealed class DokumaPackingService(IAppDbContext db, IDocumentNumberGenerator numbers, IEnumerable<IFabricTransferReceiver> receivers) : IDokumaPackingService
{
    private const string Series = "DokumaPacking";
    private const string InvalidState = "DOKUMA_PACKING_INVALID_STATE";
    private const string InvalidItem = "DOKUMA_PACKING_INVALID_ITEM";
    private const int MaxCandidates = 1000;

    /// <summary>Boyahane'ye gönderim sürerken aynı listenin ikinci kez gönderilmesini engelleyen geçici işaret.</summary>
    private const string SendingMarker = "GONDERILIYOR";

    private static readonly Error NotFound = new(ErrorCodes.NotFound, "Çeki listesi bulunamadı.", ErrorType.NotFound);
    private static readonly FabricRollStatus[] Shippable = [FabricRollStatus.FirstQuality, FabricRollStatus.SecondQuality];

    private readonly IFabricTransferReceiver? _receiver = receivers.FirstOrDefault();

    public Task<PagedResult<DokumaPackingListItemDto>> GetListAsync(DokumaPackingListQuery query, CancellationToken cancellationToken)
    {
        var lists = db.Set<DokumaPackingList>().AsNoTracking();
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
            .Select(l => new DokumaPackingListItemDto(
                l.Id,
                l.PackingNo,
                l.Order.OrderNo,
                l.Order.CustomerOrderNo,
                l.Order.Customer != null ? l.Order.Customer.Name : l.SaleCustomer != null ? l.SaleCustomer.Name : null,
                l.PackingDate,
                l.Items.Count(i => i.IsActive),
                l.Items.Where(i => i.IsActive).Sum(i => (decimal?)i.Meters) ?? 0,
                l.Status,
                l.Despatch != null ? l.Despatch.DespatchNo : null,
                l.BoyahaneReceiptNo))
            .ToPagedResultAsync(query, cancellationToken);
    }

    public async Task<Result<DokumaPackingListDto>> GetAsync(int id, CancellationToken cancellationToken)
    {
        var list = await db.Set<DokumaPackingList>().AsNoTracking()
            .Include(l => l.Order).ThenInclude(o => o.Customer)
            .Include(l => l.SaleCustomer)
            .Include(l => l.Despatch)
            .Include(l => l.Items).ThenInclude(i => i.Roll).ThenInclude(r => r.Loom)
            .Include(l => l.Items).ThenInclude(i => i.Roll).ThenInclude(r => r.Beam).ThenInclude(b => b.WarpOrder).ThenInclude(w => w.OrderLine).ThenInclude(o => o.Article)
            .SingleOrDefaultAsync(l => l.Id == id, cancellationToken);
        if (list is null)
        {
            return NotFound;
        }

        var items = list.Items.Where(i => i.IsActive).OrderBy(i => i.Roll.RollNo).Select(i => new DokumaPackingItemDto(
            i.Id,
            i.RollId,
            i.Roll.RollNo,
            i.Roll.Beam.BeamNo,
            i.Roll.Loom.Code,
            i.Roll.Beam.WarpOrder.OrderLine.Article.ArticleCode,
            i.Roll.Beam.WarpOrder.OrderLine.Article.Name,
            i.Meters,
            i.Kilograms,
            i.Grade,
            i.Roll.Beam.WarpOrder.OrderLine.CustomerArticleCode)).ToList();

        return new DokumaPackingListDto(
            list.Id,
            list.PackingNo,
            list.OrderId,
            list.Order.OrderNo,
            list.Order.CustomerOrderNo,
            list.Order.Customer?.Name ?? list.SaleCustomer?.Name,
            list.PackingDate,
            list.Notes,
            list.Status,
            items.Sum(i => i.Meters),
            items,
            list.Order.CustomerId is null,
            list.Order.Ownership == OrderOwnership.Commission,
            list.SaleCustomerId,
            list.DespatchId,
            list.Despatch?.DespatchNo,
            list.BoyahaneReceiptNo,
            list.BoyahaneSentOn,
            _receiver is not null,
            list.Order.Customer?.DocumentLanguage ?? list.SaleCustomer?.DocumentLanguage ?? DocumentLanguage.Turkish);
    }

    public async Task<IReadOnlyList<DokumaPackingCandidateDto>> GetCandidatesAsync(int? packingListId, CancellationToken cancellationToken) =>
        await db.Set<DokumaFabricRoll>().AsNoTracking()
            .Where(r => Shippable.Contains(r.Status)
                && r.Beam.WarpOrder.OrderLine.Order.Status == OrderStatus.Confirmed
                && !db.Set<DokumaPackingItem>().Any(i => i.RollId == r.Id && i.IsActive && i.PackingListId != packingListId))
            .OrderBy(r => r.Beam.WarpOrder.OrderLine.OrderId).ThenBy(r => r.RollNo)
            .Take(MaxCandidates)
            .Select(r => new DokumaPackingCandidateDto(
                r.Id,
                r.Beam.WarpOrder.OrderLine.OrderId,
                r.Beam.WarpOrder.OrderLine.Order.OrderNo,
                r.Beam.WarpOrder.OrderLine.Order.Customer != null ? r.Beam.WarpOrder.OrderLine.Order.Customer.Name : null,
                r.RollNo,
                r.Beam.BeamNo,
                r.Beam.WarpOrder.OrderLine.Article.ArticleCode,
                r.InspectedMeters ?? r.Meters,
                r.Kilograms,
                r.Status,
                db.Set<DokumaPackingItem>().Any(i => i.RollId == r.Id && i.IsActive && i.PackingListId == packingListId)))
            .ToListAsync(cancellationToken);

    public async Task<Result<DokumaPackingListDto>> CreateAsync(DokumaPackingListRequest request, CancellationToken cancellationToken)
    {
        var plan = await PlanAsync(request, null, cancellationToken);
        if (!plan.IsSuccess)
        {
            return plan.Error!;
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var year = request.PackingDate.Year;
        var sequence = await numbers.NextAsync(Series, year, cancellationToken);
        var list = new DokumaPackingList { PackingNo = $"DC-{year}-{sequence:D5}" };
        Apply(list, request, plan.Value);
        db.Set<DokumaPackingList>().Add(list);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetAsync(list.Id, cancellationToken);
    }

    public async Task<Result<DokumaPackingListDto>> UpdateAsync(int id, DokumaPackingListRequest request, CancellationToken cancellationToken)
    {
        var list = await db.Set<DokumaPackingList>().Include(l => l.Items).SingleOrDefaultAsync(l => l.Id == id, cancellationToken);
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
        db.Set<DokumaPackingItem>().RemoveRange(list.Items.Where(i => !request.RollIds.Contains(i.RollId)).ToList());
        await db.SaveChangesAsync(cancellationToken);
        Apply(list, request, plan.Value);
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    /// <summary>Stoğa üretim siparişinin listesinde malın satıldığı müşteri; irsaliye bu müşteriye yazılır.</summary>
    public async Task<Result<DokumaPackingListDto>> SetSaleCustomerAsync(int id, SetDokumaPackingSaleCustomerRequest request, CancellationToken cancellationToken)
    {
        var list = await db.Set<DokumaPackingList>().Include(l => l.Order).SingleOrDefaultAsync(l => l.Id == id, cancellationToken);
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

        if (await ReferenceValidation.RequireSelectableAsync(db.Set<DokumaCustomer>(), request.CustomerId, list.SaleCustomerId, "customerId", "Müşteri", cancellationToken) is { } error)
        {
            return error;
        }

        list.SaleCustomerId = request.CustomerId;
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    public Task<Result<DokumaPackingListDto>> ConfirmAsync(int id, CancellationToken cancellationToken) =>
        ChangeStatusAsync(id, PackingListStatus.Draft, PackingListStatus.Confirmed, "Yalnızca taslak liste onaylanabilir.", cancellationToken);

    public async Task<Result<DokumaPackingListDto>> ReopenAsync(int id, CancellationToken cancellationToken)
    {
        var list = await db.Set<DokumaPackingList>().AsNoTracking().SingleOrDefaultAsync(l => l.Id == id, cancellationToken);
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
    public async Task<Result<DokumaPackingListDto>> CancelAsync(int id, CancellationToken cancellationToken)
    {
        var list = await db.Set<DokumaPackingList>().Include(l => l.Items).SingleOrDefaultAsync(l => l.Id == id, cancellationToken);
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

    public async Task<Result<DokumaPackingListDto>> SendToBoyahaneAsync(int id, SendDokumaPackingToBoyahaneRequest request, CancellationToken cancellationToken)
    {
        if (_receiver is null)
        {
            return new Error(InvalidState, "Boyahane modülü bu kurulumda yok.", ErrorType.Conflict);
        }

        var list = await db.Set<DokumaPackingList>().AsNoTracking()
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
        var claimed = await db.Set<DokumaPackingList>()
            .Where(l => l.Id == id && l.BoyahaneReceiptNo == null && l.DespatchId == null)
            .ExecuteUpdateAsync(s => s.SetProperty(l => l.BoyahaneReceiptNo, SendingMarker), cancellationToken);
        if (claimed == 0)
        {
            return new Error(InvalidState, "Liste zaten sevk edilmiş ya da Boyahane'ye gönderilmiş.", ErrorType.Conflict);
        }

        var rolls = list.Items.Where(i => i.IsActive).OrderBy(i => i.Roll.RollNo)
            .Select(i => new FabricTransferRoll(i.Roll.RollNo, i.Meters, i.Kilograms))
            .ToList();
        Result<string> result;
        try
        {
            result = await _receiver.ReceiveAsync(new FabricTransferRequest(request.Date, customerId, request.FabricId, list.PackingNo, rolls, "Dokuma'dan gönderildi."), cancellationToken);
        }
        catch
        {
            await db.Set<DokumaPackingList>().Where(l => l.Id == id).ExecuteUpdateAsync(s => s.SetProperty(l => l.BoyahaneReceiptNo, (string?)null), CancellationToken.None);
            throw;
        }

        var receiptNo = result.IsSuccess ? result.Value : null;
        DateOnly? sentOn = result.IsSuccess ? request.Date : null;
        await db.Set<DokumaPackingList>()
            .Where(l => l.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(l => l.BoyahaneReceiptNo, receiptNo)
                .SetProperty(l => l.BoyahaneSentOn, sentOn), cancellationToken);

        return result.IsSuccess ? await GetAsync(id, cancellationToken) : result.Error!;
    }

    private async Task<Result<DokumaPackingListDto>> ChangeStatusAsync(int id, PackingListStatus from, PackingListStatus to, string message, CancellationToken cancellationToken)
    {
        var list = await db.Set<DokumaPackingList>().SingleOrDefaultAsync(l => l.Id == id, cancellationToken);
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

    private sealed record PackingPlan(int OrderId, IReadOnlyList<DokumaFabricRoll> Rolls);

    /// <summary>Seçilen toplar aynı siparişten, 1. ya da 2. kalite, başka listede olmayan olmalıdır.</summary>
    private async Task<Result<PackingPlan>> PlanAsync(DokumaPackingListRequest request, DokumaPackingList? existing, CancellationToken cancellationToken)
    {
        var ids = request.RollIds.Distinct().ToList();
        var rolls = await db.Set<DokumaFabricRoll>().AsNoTracking()
            .Include(r => r.Beam).ThenInclude(b => b.WarpOrder).ThenInclude(w => w.OrderLine).ThenInclude(l => l.Order)
            .Where(r => ids.Contains(r.Id))
            .ToListAsync(cancellationToken);
        if (rolls.Count != ids.Count)
        {
            return new Error(InvalidItem, "Seçilen toplardan biri bulunamadı.", ErrorType.Conflict);
        }

        var orderIds = rolls.Select(r => r.Beam.WarpOrder.OrderLine.OrderId).Distinct().ToList();
        if (orderIds.Count > 1)
        {
            return new Error(InvalidItem, "Bir çeki listesindeki toplar aynı siparişten olmalıdır.", ErrorType.Conflict);
        }

        var busy = await db.Set<DokumaPackingItem>()
            .Where(i => ids.Contains(i.RollId) && i.IsActive && (existing == null || i.PackingListId != existing.Id))
            .Select(i => i.RollId)
            .ToListAsync(cancellationToken);
        foreach (var roll in rolls)
        {
            var problem = roll.Status == FabricRollStatus.PendingInspection ? "kontrol edilmedi"
                : roll.Status == FabricRollStatus.Rejected ? "red; önce yetkili kararı gerekir"
                : roll.Beam.WarpOrder.OrderLine.Order.Status != OrderStatus.Confirmed ? "siparişi onaylı değil"
                : busy.Contains(roll.Id) ? "başka bir listede"
                : null;
            if (problem is not null)
            {
                return new Error(InvalidItem, $"{roll.RollNo} listeye alınamaz: {problem}.", ErrorType.Conflict);
            }
        }

        return new PackingPlan(orderIds[0], rolls);
    }

    private static void Apply(DokumaPackingList list, DokumaPackingListRequest request, PackingPlan plan)
    {
        list.OrderId = plan.OrderId;
        list.PackingDate = request.PackingDate;
        list.Notes = NullIfBlank(request.Notes);
        foreach (var roll in plan.Rolls.Where(r => list.Items.All(i => i.RollId != r.Id)))
        {
            list.Items.Add(new DokumaPackingItem
            {
                RollId = roll.Id,
                Meters = roll.InspectedMeters ?? roll.Meters,
                Kilograms = roll.Kilograms,
                Grade = roll.Status,
            });
        }
    }
}
