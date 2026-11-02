using ERP.Boyahane.Domain.Entities;
using ERP.Boyahane.Domain.Enums;
using ERP.Boyahane.Domain.Rules;
using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Core.Application.Features.MasterData;
using ERP.Core.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using static ERP.Core.Application.Features.MasterData.MasterDataRules;

namespace ERP.Boyahane.Application.Features.Orders;

public sealed class BoyahaneOrderQuery : PageRequest
{
    /// <summary>Sipariş no, müşteri sipariş no ya da müşteri adında aranır.</summary>
    public string? Search { get; set; }
}

public sealed record BoyahaneOrderListItemDto(
    int Id,
    string OrderNo,
    DateOnly OrderDate,
    string? CustomerName,
    string? CustomerOrderNo,
    OrderOwnership Ownership,
    DateOnly? DueDate,
    int LineCount,
    int AssignedRollCount,
    decimal TotalAmount,
    string Currency,
    OrderStatus Status);

/// <param name="Amount">Miktar × birim fiyat; fiyat girilmemişse boş.</param>
/// <param name="AssignedDifferencePercent">Bağlanan miktar siparişten toleransı aşan oranda farklıysa fark (%); aksi halde boş.</param>
public sealed record BoyahaneOrderLineDto(
    int Id,
    OrderProcessType ProcessType,
    int? FabricId,
    int? YarnId,
    string ItemCode,
    string ItemName,
    string? ColorName,
    string? ColorCode,
    decimal Quantity,
    StockUnit Unit,
    BillingUnit? BillingUnit,
    StockUnit EffectiveBillingUnit,
    decimal? UnitPrice,
    decimal? Amount,
    string? Notes,
    int AssignedRollCount,
    int AssignedYarnLotCount,
    decimal AssignedMeters,
    decimal AssignedKilograms,
    decimal? AssignedDifferencePercent,
    int? LabRequestId,
    string? LabRequestNo,
    LabRequestStatus? LabRequestStatus,
    string? RecipeTrialCode,
    int? ColorCardId = null,
    string? ColorCardCode = null,
    int? PatternId = null,
    string? PatternCode = null,
    int? ColorwayId = null,
    string? ColorwayCode = null);

public sealed record BoyahaneOrderDto(
    int Id,
    string OrderNo,
    DateOnly OrderDate,
    int? CustomerId,
    string? CustomerName,
    string? CustomerOrderNo,
    OrderOwnership Ownership,
    DateOnly? DueDate,
    string Currency,
    string? Notes,
    OrderStatus Status,
    decimal TotalAmount,
    DateTime CreatedAt,
    string? CreatedBy,
    IReadOnlyList<BoyahaneOrderLineDto> Lines,
    DateTime? ClosedAt = null,
    string? ClosedBy = null,
    string? CloseNote = null);

/// <param name="Note">Kapatma gerekçesi; isteğe bağlı.</param>
public sealed record CloseBoyahaneOrderRequest(string? Note = null);

public sealed class CloseBoyahaneOrderRequestValidator : AbstractValidator<CloseBoyahaneOrderRequest>
{
    public CloseBoyahaneOrderRequestValidator()
    {
        RuleFor(x => x.Note).OptionalText(500);
    }
}

/// <param name="Id">Var olan satır güncelleniyorsa Id'si; yeni satırda boş.</param>
/// <param name="ColorCardId">Boyama satırında renk kartı; seçilirse renk adı ve kodu karttan gelir.</param>
/// <param name="PatternId">Baskı satırında desen kartı; seçilirse desen adı ve kodu karttan gelir.</param>
/// <param name="ColorwayId">Baskı satırında desenin renk yolu; desen seçilmeden verilemez.</param>
public sealed record BoyahaneOrderLineRequest(
    int? Id,
    OrderProcessType ProcessType,
    decimal Quantity,
    StockUnit Unit,
    int? FabricId = null,
    int? YarnId = null,
    string? ColorName = null,
    string? ColorCode = null,
    decimal? UnitPrice = null,
    string? Notes = null,
    BillingUnit? BillingUnit = null,
    int? ColorCardId = null,
    int? PatternId = null,
    int? ColorwayId = null);

/// <param name="CustomerId">Fason işte zorunlu; kendi malında boşsa stoğa üretimdir.</param>
/// <param name="Currency">Boşsa müşterinin para birimi, müşteri yoksa TRY kullanılır.</param>
public sealed record BoyahaneOrderRequest(
    int? CustomerId,
    DateOnly OrderDate,
    IReadOnlyList<BoyahaneOrderLineRequest> Lines,
    OrderOwnership Ownership = OrderOwnership.Commission,
    string? CustomerOrderNo = null,
    DateOnly? DueDate = null,
    string? Currency = null,
    string? Notes = null);

public sealed class BoyahaneOrderRequestValidator : AbstractValidator<BoyahaneOrderRequest>
{
    public const int MaxLines = 100;

    public BoyahaneOrderRequestValidator()
    {
        RuleFor(x => x.CustomerId).NotNull().WithMessage("Fason siparişte müşteri seçilmelidir.").When(x => x.Ownership == OrderOwnership.Commission);
        RuleFor(x => x.OrderDate).NotEmpty().WithMessage("Sipariş tarihi zorunludur.");
        RuleFor(x => x.Ownership).IsInEnum().WithMessage("Geçerli bir iş türü seçin.");
        RuleFor(x => x.CustomerOrderNo).OptionalText(50);
        RuleFor(x => x.Notes).OptionalText(1000);
        RuleFor(x => x.DueDate)
            .Must((request, due) => due >= request.OrderDate).WithMessage("Termin sipariş tarihinden önce olamaz.")
            .When(x => x.DueDate is not null);
        RuleFor(x => x.Currency)
            .Matches(@"^\s*[A-Za-z]{3}\s*$").WithMessage("Para birimi üç harfli ISO kodu olmalıdır (örn. TRY, EUR).")
            .When(x => !string.IsNullOrWhiteSpace(x.Currency));
        RuleFor(x => x.Lines)
            .NotEmpty().WithMessage("En az bir satır girilmelidir.")
            .Must(lines => lines.Count <= MaxLines).WithMessage($"Bir siparişte en fazla {MaxLines} satır olabilir.")
            .Custom((lines, context) =>
            {
                if (lines is null)
                {
                    return;
                }

                for (var i = 0; i < lines.Count; i++)
                {
                    foreach (var problem in Problems(lines[i]))
                    {
                        context.AddFailure($"{i + 1}. satır: {problem}");
                    }
                }
            });
    }

    private static IEnumerable<string> Problems(BoyahaneOrderLineRequest line)
    {
        if (!Enum.IsDefined(line.ProcessType))
        {
            yield return "Geçerli bir işlem türü seçin.";
        }

        if (line.ProcessType == OrderProcessType.YarnDyeing)
        {
            if (line.YarnId is null or <= 0)
            {
                yield return "İplik boyamada iplik kartı seçilmelidir.";
            }

            if (line.Unit != StockUnit.Kilogram)
            {
                yield return "İplik boyamada birim kilogram olmalıdır.";
            }

            if (line.BillingUnit == BillingUnit.Meter)
            {
                yield return "İplikte fatura ölçüsü metre olamaz.";
            }
        }
        else
        {
            if (line.FabricId is null or <= 0)
            {
                yield return "Kumaş kartı seçilmelidir.";
            }

            if (line.Unit is not (StockUnit.Meter or StockUnit.Kilogram))
            {
                yield return "Birim metre ya da kilogram olmalıdır.";
            }
        }

        // Renk kartı ya da desen seçildiyse ad karttan gelir.
        var fromCard = line.ProcessType == OrderProcessType.Printing ? line.PatternId is not null : line.ColorCardId is not null;
        if (line.ProcessType != OrderProcessType.Finishing && !fromCard && string.IsNullOrWhiteSpace(line.ColorName))
        {
            yield return line.ProcessType == OrderProcessType.Printing ? "Desen adı zorunludur." : "Renk adı zorunludur.";
        }

        if (line.ColorCardId is not null && line.ProcessType is not (OrderProcessType.FabricDyeing or OrderProcessType.YarnDyeing))
        {
            yield return "Renk kartı yalnızca kumaş ve iplik boyama satırında seçilir.";
        }

        if ((line.PatternId is not null || line.ColorwayId is not null) && line.ProcessType != OrderProcessType.Printing)
        {
            yield return "Desen ve renk yolu yalnızca baskı satırında seçilir.";
        }

        if (line.ColorwayId is not null && line.PatternId is null)
        {
            yield return "Renk yolu için önce desen seçilmelidir.";
        }

        if (line.ColorName?.Trim().Length > 100 || line.ColorCode?.Trim().Length > 50)
        {
            yield return "Renk adı en fazla 100, kodu en fazla 50 karakter olabilir.";
        }

        if (line.Quantity is <= 0 or > 99_999_999)
        {
            yield return "Miktar sıfırdan büyük olmalıdır.";
        }

        if (line.UnitPrice is < 0 or > 99_999_999)
        {
            yield return "Birim fiyat negatif olamaz.";
        }

        if (line.Notes?.Trim().Length > 500)
        {
            yield return "Satır notu en fazla 500 karakter olabilir.";
        }
    }
}

/// <param name="RollIds">Satıra bağlanacak topların tamamı; listede olmayanların bağı kaldırılır.</param>
public sealed record AssignOrderLineRollsRequest(IReadOnlyList<int> RollIds);

/// <param name="LotIds">Satıra bağlanacak iplik lotlarının tamamı; listede olmayanların bağı kaldırılır.</param>
public sealed record AssignOrderLineYarnLotsRequest(IReadOnlyList<int> LotIds);

public sealed record BoyahaneAssignableYarnLotDto(
    int Id,
    string LotNo,
    string? CustomerLotNo,
    YarnPackageType PackageType,
    int PackageCount,
    decimal Kilograms,
    bool Assigned);

public sealed class AssignOrderLineRollsRequestValidator : AbstractValidator<AssignOrderLineRollsRequest>
{
    public AssignOrderLineRollsRequestValidator()
    {
        RuleFor(x => x.RollIds).NotNull().WithMessage("Top listesi zorunludur.");
    }
}

/// <param name="Assigned">Top bu satıra bağlı mı.</param>
public sealed record BoyahaneAssignableRollDto(
    int Id,
    string RollNo,
    string? CustomerRollNo,
    string LotNo,
    decimal Meters,
    decimal Kilograms,
    FabricRollStatus Status,
    bool Assigned);

public interface IBoyahaneOrderService
{
    Task<PagedResult<BoyahaneOrderListItemDto>> GetListAsync(BoyahaneOrderQuery query, CancellationToken cancellationToken);
    Task<Result<BoyahaneOrderDto>> GetAsync(int id, CancellationToken cancellationToken);
    Task<Result<BoyahaneOrderDto>> CreateAsync(BoyahaneOrderRequest request, CancellationToken cancellationToken);
    Task<Result<BoyahaneOrderDto>> UpdateAsync(int id, BoyahaneOrderRequest request, CancellationToken cancellationToken);
    Task<Result<BoyahaneOrderDto>> ConfirmAsync(int id, CancellationToken cancellationToken);
    Task<Result<BoyahaneOrderDto>> ReopenAsync(int id, CancellationToken cancellationToken);
    Task<Result<BoyahaneOrderDto>> CancelAsync(int id, CancellationToken cancellationToken);
    Task<Result<BoyahaneOrderDto>> CloseAsync(int id, CloseBoyahaneOrderRequest request, CancellationToken cancellationToken);
    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken);
    Task<Result<IReadOnlyList<BoyahaneAssignableRollDto>>> GetAssignableRollsAsync(int id, int lineId, CancellationToken cancellationToken);
    Task<Result<BoyahaneOrderDto>> AssignRollsAsync(int id, int lineId, AssignOrderLineRollsRequest request, CancellationToken cancellationToken);
    Task<Result<IReadOnlyList<BoyahaneAssignableYarnLotDto>>> GetAssignableYarnLotsAsync(int id, int lineId, CancellationToken cancellationToken);
    Task<Result<BoyahaneOrderDto>> AssignYarnLotsAsync(int id, int lineId, AssignOrderLineYarnLotsRequest request, CancellationToken cancellationToken);
}

public sealed class BoyahaneOrderService(IAppDbContext db, IDocumentNumberGenerator numbers, ICurrentUser currentUser, TimeProvider timeProvider) : IBoyahaneOrderService
{
    private const string OrderSeries = "BoyahaneOrder";
    private const string InvalidState = "BOYAHANE_ORDER_INVALID_STATE";
    private const string InvalidRoll = "BOYAHANE_ORDER_INVALID_ROLL";
    private const int MaxAssignableRolls = 1000;
    private const string InBatchMessage = "Partiye girmiş mal bulunan sipariş değiştirilemez; planlı partiyi iptal edin. Reddedilen mal İade ve Hurda belgesiyle çıkar.";

    /// <summary>Top bölünmediği için tam eşleşme nadirdir; bu oranı aşan fark uyarı olarak gösterilir.</summary>
    private const decimal QuantityTolerancePercent = 10;

    private static readonly Error NotFound = new(ErrorCodes.NotFound, "Sipariş bulunamadı.", ErrorType.NotFound);
    private static readonly Error LineNotFound = new(ErrorCodes.NotFound, "Sipariş satırı bulunamadı.", ErrorType.NotFound);

    public Task<PagedResult<BoyahaneOrderListItemDto>> GetListAsync(BoyahaneOrderQuery query, CancellationToken cancellationToken)
    {
        var orders = db.Set<BoyahaneOrder>().AsNoTracking();
        if (NullIfBlank(query.Search) is { } term)
        {
            orders = orders.Where(o => o.OrderNo.Contains(term)
                || (o.CustomerOrderNo != null && o.CustomerOrderNo.Contains(term))
                || (o.Customer != null && o.Customer.Name.Contains(term)));
        }

        return orders
            .OrderByDescending(o => o.OrderDate).ThenByDescending(o => o.Id)
            .Select(o => new BoyahaneOrderListItemDto(
                o.Id,
                o.OrderNo,
                o.OrderDate,
                o.Customer != null ? o.Customer.Name : null,
                o.CustomerOrderNo,
                o.Ownership,
                o.DueDate,
                o.Lines.Count,
                o.Lines.SelectMany(l => l.Rolls).Count() + o.Lines.SelectMany(l => l.YarnLots).Count(),
                o.Lines
                    .Where(l => l.ProcessType == OrderProcessType.YarnDyeing
                        || (l.BillingUnit ?? (o.Customer != null ? o.Customer.BillingUnit : BillingUnit.OrderUnit)) == BillingUnit.OrderUnit
                        || ((l.BillingUnit ?? (o.Customer != null ? o.Customer.BillingUnit : BillingUnit.OrderUnit)) == BillingUnit.Meter && l.Unit == StockUnit.Meter)
                        || ((l.BillingUnit ?? (o.Customer != null ? o.Customer.BillingUnit : BillingUnit.OrderUnit)) == BillingUnit.Kilogram && l.Unit == StockUnit.Kilogram))
                    .Sum(l => l.Quantity * (l.UnitPrice ?? 0)),
                o.Currency,
                o.Status))
            .ToPagedResultAsync(query, cancellationToken);
    }

    public async Task<Result<BoyahaneOrderDto>> GetAsync(int id, CancellationToken cancellationToken)
    {
        var order = await db.Set<BoyahaneOrder>().AsNoTracking()
            .Include(o => o.Customer)
            .Include(o => o.Lines).ThenInclude(l => l.Fabric)
            .Include(o => o.Lines).ThenInclude(l => l.Yarn)
            .Include(o => o.Lines).ThenInclude(l => l.Rolls)
            .Include(o => o.Lines).ThenInclude(l => l.YarnLots)
            .Include(o => o.Lines).ThenInclude(l => l.ColorCard)
            .Include(o => o.Lines).ThenInclude(l => l.Pattern)
            .Include(o => o.Lines).ThenInclude(l => l.Colorway)
            .SingleOrDefaultAsync(o => o.Id == id, cancellationToken);
        if (order is null)
        {
            return NotFound;
        }

        var lineIds = order.Lines.Select(l => l.Id).ToList();
        var labRequests = await db.Set<BoyahaneLabRequest>().AsNoTracking()
            .Where(r => lineIds.Contains(r.OrderLineId) && r.Status != LabRequestStatus.Cancelled)
            .Select(r => new
            {
                r.OrderLineId,
                r.Id,
                r.RequestNo,
                r.Status,
                Recipe = r.Trials.Where(t => t.Decision == LabTrialDecision.Accepted).Select(t => t.TrialCode).FirstOrDefault(),
            })
            .ToDictionaryAsync(r => r.OrderLineId, cancellationToken);

        var lines = order.Lines.OrderBy(l => l.Id).Select(l => new BoyahaneOrderLineDto(
            l.Id,
            l.ProcessType,
            l.FabricId,
            l.YarnId,
            l.Fabric?.FabricCode ?? l.Yarn?.YarnCode ?? "",
            l.Fabric?.Name ?? l.Yarn?.Name ?? "",
            l.ColorName,
            l.ColorCode,
            l.Quantity,
            l.Unit,
            l.BillingUnit,
            Billing.Resolve(l.BillingUnit, order.Customer?.BillingUnit, l.Unit, l.ProcessType == OrderProcessType.YarnDyeing),
            l.UnitPrice,
            l.UnitPrice is { } price && Billing.Resolve(l.BillingUnit, order.Customer?.BillingUnit, l.Unit, l.ProcessType == OrderProcessType.YarnDyeing) == l.Unit ? Math.Round(l.Quantity * price, 2) : null,
            l.Notes,
            l.Rolls.Count,
            l.YarnLots.Count,
            AssignedMeters(l),
            AssignedKilograms(l),
            DifferencePercent(l),
            labRequests.GetValueOrDefault(l.Id)?.Id,
            labRequests.GetValueOrDefault(l.Id)?.RequestNo,
            labRequests.GetValueOrDefault(l.Id)?.Status,
            labRequests.GetValueOrDefault(l.Id)?.Recipe,
            l.ColorCardId,
            l.ColorCard?.Code,
            l.PatternId,
            l.Pattern?.Code,
            l.ColorwayId,
            l.Colorway?.Code)).ToList();

        return new BoyahaneOrderDto(
            order.Id,
            order.OrderNo,
            order.OrderDate,
            order.CustomerId,
            order.Customer?.Name,
            order.CustomerOrderNo,
            order.Ownership,
            order.DueDate,
            order.Currency,
            order.Notes,
            order.Status,
            lines.Sum(l => l.Amount ?? 0),
            order.CreatedAt,
            order.CreatedBy,
            lines,
            order.ClosedAt,
            order.ClosedBy,
            order.CloseNote);
    }

    public async Task<Result<BoyahaneOrderDto>> CreateAsync(BoyahaneOrderRequest request, CancellationToken cancellationToken)
    {
        if (await ValidateReferencesAsync(request, null, cancellationToken) is { } error)
        {
            return error;
        }

        var archive = await LoadArchiveAsync(request.Lines, cancellationToken);
        if (ValidateArchive(request, null, archive) is { } archiveError)
        {
            return archiveError;
        }

        var customerCurrency = await CustomerCurrencyAsync(request.CustomerId, cancellationToken);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var year = request.OrderDate.Year;
        var sequence = await numbers.NextAsync(OrderSeries, year, cancellationToken);

        var order = new BoyahaneOrder { OrderNo = $"BS-{year}-{sequence:D5}" };
        Apply(order, request, customerCurrency);
        foreach (var item in request.Lines)
        {
            var line = new BoyahaneOrderLine();
            ApplyLine(line, item, archive);
            order.Lines.Add(line);
        }

        db.Set<BoyahaneOrder>().Add(order);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetAsync(order.Id, cancellationToken);
    }

    public async Task<Result<BoyahaneOrderDto>> UpdateAsync(int id, BoyahaneOrderRequest request, CancellationToken cancellationToken)
    {
        var order = await db.Set<BoyahaneOrder>()
            .Include(o => o.Lines).ThenInclude(l => l.Rolls)
            .Include(o => o.Lines).ThenInclude(l => l.YarnLots)
            .SingleOrDefaultAsync(o => o.Id == id, cancellationToken);
        if (order is null)
        {
            return NotFound;
        }

        if (order.Status != OrderStatus.Draft)
        {
            return new Error(InvalidState, "Yalnızca taslak sipariş düzenlenebilir.", ErrorType.Conflict);
        }

        var foreign = request.Lines.FirstOrDefault(l => l.Id is { } lineId && order.Lines.All(e => e.Id != lineId));
        if (foreign is not null)
        {
            return ReferenceValidation.InvalidReference("lines", "Satır bu siparişe ait değil.");
        }

        if (order.CustomerId != request.CustomerId && order.Lines.Any(HasAssignments))
        {
            return new Error(InvalidState, "Top bağlı siparişin müşterisi değiştirilemez.", ErrorType.Conflict);
        }

        if (order.Ownership != request.Ownership && order.Lines.Any(HasAssignments))
        {
            return new Error(InvalidState, "Top bağlı siparişin iş türü değiştirilemez.", ErrorType.Conflict);
        }

        foreach (var line in order.Lines.Where(HasAssignments))
        {
            var item = request.Lines.FirstOrDefault(l => l.Id == line.Id);
            if (item is null || item.FabricId != line.FabricId || item.YarnId != line.YarnId)
            {
                return new Error(InvalidState, "Mal bağlı satır silinemez, kartı değiştirilemez; önce malı çıkarın.", ErrorType.Conflict);
            }
        }

        // Reçete bu satırın kartı ve rengi için hazırlanır; talep açıldıktan sonra bunlar değiştirilemez.
        var lineIds = order.Lines.Select(l => l.Id).ToList();
        var labLineIds = await db.Set<BoyahaneLabRequest>()
            .Where(r => lineIds.Contains(r.OrderLineId) && r.Status != LabRequestStatus.Cancelled)
            .Select(r => r.OrderLineId)
            .ToListAsync(cancellationToken);
        foreach (var line in order.Lines.Where(l => labLineIds.Contains(l.Id)))
        {
            // Renk kartı aynı kaldıkça ad ve kod karttan alındığı haliyle korunur; serbest metinli renkte ad ve kod karşılaştırılır.
            var item = request.Lines.FirstOrDefault(l => l.Id == line.Id);
            if (item is null || item.FabricId != line.FabricId || item.YarnId != line.YarnId || item.ColorCardId != line.ColorCardId
                || (line.ColorCardId is null && (NullIfBlank(item.ColorName) != line.ColorName || NullIfBlank(item.ColorCode) != line.ColorCode)))
            {
                return new Error(InvalidState, "Laboratuvar talebi olan satır silinemez, kartı ve rengi değiştirilemez; önce talebi iptal edin.", ErrorType.Conflict);
            }
        }

        if (await ValidateReferencesAsync(request, order, cancellationToken) is { } error)
        {
            return error;
        }

        var archive = await LoadArchiveAsync(request.Lines, cancellationToken);
        if (ValidateArchive(request, order, archive) is { } archiveError)
        {
            return archiveError;
        }

        var customerCurrency = await CustomerCurrencyAsync(request.CustomerId, cancellationToken);
        Apply(order, request, customerCurrency);

        db.Set<BoyahaneOrderLine>().RemoveRange(order.Lines.Where(l => request.Lines.All(r => r.Id != l.Id)));
        foreach (var item in request.Lines)
        {
            if (item.Id is { } lineId)
            {
                ApplyLine(order.Lines.Single(l => l.Id == lineId), item, archive);
            }
            else
            {
                var line = new BoyahaneOrderLine();
                ApplyLine(line, item, archive);
                order.Lines.Add(line);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    public async Task<Result<BoyahaneOrderDto>> ConfirmAsync(int id, CancellationToken cancellationToken)
    {
        var order = await db.Set<BoyahaneOrder>().SingleOrDefaultAsync(o => o.Id == id, cancellationToken);
        if (order is null)
        {
            return NotFound;
        }

        if (order.Status != OrderStatus.Draft)
        {
            return new Error(InvalidState, "Yalnızca taslak sipariş onaylanabilir.", ErrorType.Conflict);
        }

        order.Status = OrderStatus.Confirmed;
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    /// <summary>Onaylı siparişi düzeltmek için taslağa döndürür; numara ve bağlı mal korunur.</summary>
    public async Task<Result<BoyahaneOrderDto>> ReopenAsync(int id, CancellationToken cancellationToken)
    {
        var order = await db.Set<BoyahaneOrder>().SingleOrDefaultAsync(o => o.Id == id, cancellationToken);
        if (order is null)
        {
            return NotFound;
        }

        if (order.Status != OrderStatus.Confirmed)
        {
            return new Error(InvalidState, "Yalnızca onaylı sipariş taslağa alınabilir.", ErrorType.Conflict);
        }

        if (await db.Set<BoyahaneBatchItem>().AnyAsync(i => i.IsActive && i.OrderLine.OrderId == id, cancellationToken))
        {
            return new Error(InvalidState, InBatchMessage, ErrorType.Conflict);
        }

        order.Status = OrderStatus.Draft;
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    public async Task<Result<BoyahaneOrderDto>> CancelAsync(int id, CancellationToken cancellationToken)
    {
        var order = await db.Set<BoyahaneOrder>()
            .Include(o => o.Lines).ThenInclude(l => l.Rolls)
            .Include(o => o.Lines).ThenInclude(l => l.YarnLots)
            .SingleOrDefaultAsync(o => o.Id == id, cancellationToken);
        if (order is null)
        {
            return NotFound;
        }

        if (order.Status == OrderStatus.Cancelled)
        {
            return new Error(InvalidState, "Sipariş zaten iptal edilmiş.", ErrorType.Conflict);
        }

        if (order.Status == OrderStatus.Closed)
        {
            return new Error(InvalidState, "Kapatılmış sipariş iptal edilemez.", ErrorType.Conflict);
        }

        if (await db.Set<BoyahaneBatchItem>().AnyAsync(i => i.IsActive && i.OrderLine.OrderId == id, cancellationToken))
        {
            return new Error(InvalidState, InBatchMessage, ErrorType.Conflict);
        }

        // Açık laboratuvar talepleri de kapanır; onaylanmış reçeteler geçmişte kalır, sonraki siparişlere önerilir.
        var orderLineIds = order.Lines.Select(l => l.Id).ToList();
        foreach (var lab in await db.Set<BoyahaneLabRequest>()
                     .Where(r => orderLineIds.Contains(r.OrderLineId) && r.Status == LabRequestStatus.Open)
                     .ToListAsync(cancellationToken))
        {
            lab.Status = LabRequestStatus.Cancelled;
        }

        // İptal edilen siparişin topları boşa çıkar, başka siparişe bağlanabilir.
        ReleaseAssignments(order);

        order.Status = OrderStatus.Cancelled;
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    /// <summary>
    /// İşi biten onaylı siparişi kapatır: planlı ya da işlemdeki partisi olmamalıdır; biten ve iptal edilen parti engel değildir.
    /// Partiye hiç girmemiş (ya da partisi iptal edilip boşa çıkmış) top ve lotlar siparişten çözülür, açık laboratuvar talepleri
    /// iptal edilir. Kapatılan sipariş salt okunurdur.
    /// </summary>
    public async Task<Result<BoyahaneOrderDto>> CloseAsync(int id, CloseBoyahaneOrderRequest request, CancellationToken cancellationToken)
    {
        var order = await db.Set<BoyahaneOrder>()
            .Include(o => o.Lines).ThenInclude(l => l.Rolls)
            .Include(o => o.Lines).ThenInclude(l => l.YarnLots)
            .SingleOrDefaultAsync(o => o.Id == id, cancellationToken);
        if (order is null)
        {
            return NotFound;
        }

        if (order.Status != OrderStatus.Confirmed)
        {
            return new Error(InvalidState, "Yalnızca onaylı sipariş kapatılabilir.", ErrorType.Conflict);
        }

        var open = await db.Set<BoyahaneBatchItem>()
            .Where(i => i.OrderLine.OrderId == id && (i.Batch.Status == BatchStatus.Planned || i.Batch.Status == BatchStatus.OnMachine))
            .Select(i => i.Batch.BatchNo)
            .FirstOrDefaultAsync(cancellationToken);
        if (open is not null)
        {
            return new Error(InvalidState, $"Planlı ya da işlemdeki parti ({open}) bitmeden sipariş kapatılamaz.", ErrorType.Conflict);
        }

        // Kapalı siparişin malı çeki listesine giremez: son kaliteden geçip paketlenmemiş mal varken kapatılmaz.
        var unpacked = await db.Set<BoyahaneBatchItem>()
            .Where(i => i.OrderLine.OrderId == id && i.FinalResult == FinalQualityResult.Approved
                && !db.Set<BoyahanePackingItem>().Any(p => p.IsActive && p.BatchItemId == i.Id))
            .Select(i => i.Batch.BatchNo)
            .FirstOrDefaultAsync(cancellationToken);
        if (unpacked is not null)
        {
            return new Error(InvalidState, $"Son kaliteden geçmiş, çeki listesine alınmamış mal var (parti {unpacked}); önce paketleyin.", ErrorType.Conflict);
        }

        var orderLineIds = order.Lines.Select(l => l.Id).ToList();
        foreach (var lab in await db.Set<BoyahaneLabRequest>()
                     .Where(r => orderLineIds.Contains(r.OrderLineId) && r.Status == LabRequestStatus.Open)
                     .ToListAsync(cancellationToken))
        {
            lab.Status = LabRequestStatus.Cancelled;
        }

        // İşlenmiş mal (etkin parti kaydı olan) siparişte kalır; takip, sevk ve fatura buna bağlıdır. Kalan mal boşa çıkar.
        var processedRolls = await db.Set<BoyahaneBatchItem>()
            .Where(i => i.IsActive && i.RollId != null && orderLineIds.Contains(i.OrderLineId))
            .Select(i => i.RollId!.Value)
            .ToListAsync(cancellationToken);
        var processedLots = await db.Set<BoyahaneBatchItem>()
            .Where(i => i.IsActive && i.YarnLotId != null && orderLineIds.Contains(i.OrderLineId))
            .Select(i => i.YarnLotId!.Value)
            .ToListAsync(cancellationToken);
        foreach (var roll in order.Lines.SelectMany(l => l.Rolls).Where(t => !processedRolls.Contains(t.Id)))
        {
            roll.OrderLineId = null;
        }

        foreach (var lot in order.Lines.SelectMany(l => l.YarnLots).Where(l => !processedLots.Contains(l.Id)))
        {
            lot.OrderLineId = null;
        }

        order.Status = OrderStatus.Closed;
        order.ClosedAt = timeProvider.GetUtcNow().UtcDateTime;
        order.ClosedBy = currentUser.UserName;
        order.CloseNote = NullIfBlank(request.Note);
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var order = await db.Set<BoyahaneOrder>()
            .Include(o => o.Lines).ThenInclude(l => l.Rolls)
            .Include(o => o.Lines).ThenInclude(l => l.YarnLots)
            .SingleOrDefaultAsync(o => o.Id == id, cancellationToken);
        if (order is null)
        {
            return NotFound;
        }

        if (order.Status != OrderStatus.Draft)
        {
            return new Error(InvalidState, "Yalnızca taslak sipariş silinebilir; onaylanmış sipariş iptal edilir.", ErrorType.Conflict);
        }

        var deletedLineIds = order.Lines.Select(l => l.Id).ToList();
        if (await db.Set<BoyahaneLabRequest>().AnyAsync(r => deletedLineIds.Contains(r.OrderLineId), cancellationToken))
        {
            return new Error(InvalidState, "Laboratuvar talebi olan sipariş silinemez; iptal edin.", ErrorType.Conflict);
        }

        ReleaseAssignments(order);

        db.Set<BoyahaneOrderLine>().RemoveRange(order.Lines);
        db.Set<BoyahaneOrder>().Remove(order);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<BoyahaneAssignableRollDto>>> GetAssignableRollsAsync(int id, int lineId, CancellationToken cancellationToken)
    {
        var line = await db.Set<BoyahaneOrderLine>().AsNoTracking()
            .Include(l => l.Order)
            .SingleOrDefaultAsync(l => l.Id == lineId && l.OrderId == id, cancellationToken);
        if (line is null)
        {
            return LineNotFound;
        }

        if (line.FabricId is null)
        {
            return new List<BoyahaneAssignableRollDto>();
        }

        var ownerId = OwnerOf(line.Order);
        return await db.Set<BoyahaneFabricRoll>().AsNoTracking()
            .Where(t => t.Lot.FabricId == line.FabricId
                && t.Lot.Receipt.CustomerId == ownerId
                && t.Status != FabricRollStatus.Rejected
                && (t.OrderLineId == null || t.OrderLineId == lineId))
            .OrderBy(t => t.RollNo)
            .Take(MaxAssignableRolls)
            .Select(t => new BoyahaneAssignableRollDto(
                t.Id,
                t.RollNo,
                t.CustomerRollNo,
                t.Lot.LotNo,
                t.InspectedMeters ?? t.Meters,
                t.InspectedKilograms ?? t.Kilograms,
                t.Status,
                t.OrderLineId == lineId))
            .ToListAsync(cancellationToken);
    }

    public async Task<Result<BoyahaneOrderDto>> AssignRollsAsync(
        int id, int lineId, AssignOrderLineRollsRequest request, CancellationToken cancellationToken)
    {
        var line = await db.Set<BoyahaneOrderLine>()
            .Include(l => l.Order)
            .Include(l => l.Rolls)
            .SingleOrDefaultAsync(l => l.Id == lineId && l.OrderId == id, cancellationToken);
        if (line is null)
        {
            return LineNotFound;
        }

        if (line.Order.Status is OrderStatus.Cancelled or OrderStatus.Closed)
        {
            return new Error(InvalidState, "İptal edilmiş ya da kapatılmış siparişe top bağlanamaz.", ErrorType.Conflict);
        }

        if (line.FabricId is null)
        {
            return new Error(InvalidRoll, "İplik satırına kumaş topu bağlanamaz.", ErrorType.Conflict);
        }

        var rollIds = request.RollIds.Distinct().ToList();
        var rolls = await db.Set<BoyahaneFabricRoll>()
            .Include(t => t.Lot).ThenInclude(l => l.Receipt)
            .Where(t => rollIds.Contains(t.Id))
            .ToListAsync(cancellationToken);
        if (rolls.Count != rollIds.Count)
        {
            return new Error(InvalidRoll, "Seçilen toplardan biri bulunamadı.", ErrorType.Conflict);
        }

        var ownerId = OwnerOf(line.Order);
        foreach (var roll in rolls)
        {
            var problem = roll.Lot.FabricId != line.FabricId ? "kumaş kartı satırla aynı değil"
                : roll.Lot.Receipt.CustomerId != ownerId ? "malın sahibi siparişle uyuşmuyor"
                : roll.Status == FabricRollStatus.Rejected ? "reddedilmiş"
                : roll.OrderLineId is not null && roll.OrderLineId != lineId ? "başka bir sipariş satırına bağlı"
                : null;
            if (problem is not null)
            {
                return new Error(InvalidRoll, $"{roll.RollNo} bağlanamaz: {problem}.", ErrorType.Conflict);
            }
        }

        var released = line.Rolls.Where(t => !rollIds.Contains(t.Id)).ToList();
        var releasedIds = released.Select(t => t.Id).ToList();
        if (await db.Set<BoyahaneBatchItem>().AnyAsync(i => i.IsActive && i.RollId != null && releasedIds.Contains(i.RollId.Value), cancellationToken))
        {
            return new Error(InvalidRoll, "Partiye girmiş top siparişten çıkarılamaz.", ErrorType.Conflict);
        }

        foreach (var roll in released)
        {
            roll.OrderLineId = null;
        }

        foreach (var roll in rolls)
        {
            roll.OrderLineId = lineId;
        }

        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    public async Task<Result<IReadOnlyList<BoyahaneAssignableYarnLotDto>>> GetAssignableYarnLotsAsync(int id, int lineId, CancellationToken cancellationToken)
    {
        var line = await db.Set<BoyahaneOrderLine>().AsNoTracking()
            .Include(l => l.Order)
            .SingleOrDefaultAsync(l => l.Id == lineId && l.OrderId == id, cancellationToken);
        if (line is null)
        {
            return LineNotFound;
        }

        if (line.YarnId is null)
        {
            return new List<BoyahaneAssignableYarnLotDto>();
        }

        var ownerId = OwnerOf(line.Order);
        return await db.Set<BoyahaneYarnLot>().AsNoTracking()
            .Where(l => l.YarnId == line.YarnId
                && l.CustomerId == ownerId
                && l.Status == YarnLotStatus.Approved
                && (l.OrderLineId == null || l.OrderLineId == lineId))
            .OrderBy(l => l.LotNo)
            .Take(MaxAssignableRolls)
            .Select(l => new BoyahaneAssignableYarnLotDto(
                l.Id, l.LotNo, l.CustomerLotNo, l.PackageType, l.PackageCount, l.Kilograms, l.OrderLineId == lineId))
            .ToListAsync(cancellationToken);
    }

    public async Task<Result<BoyahaneOrderDto>> AssignYarnLotsAsync(
        int id, int lineId, AssignOrderLineYarnLotsRequest request, CancellationToken cancellationToken)
    {
        var line = await db.Set<BoyahaneOrderLine>()
            .Include(l => l.Order)
            .Include(l => l.YarnLots)
            .SingleOrDefaultAsync(l => l.Id == lineId && l.OrderId == id, cancellationToken);
        if (line is null)
        {
            return LineNotFound;
        }

        if (line.Order.Status is OrderStatus.Cancelled or OrderStatus.Closed)
        {
            return new Error(InvalidState, "İptal edilmiş ya da kapatılmış siparişe lot bağlanamaz.", ErrorType.Conflict);
        }

        if (line.YarnId is null)
        {
            return new Error(InvalidRoll, "Kumaş satırına iplik lotu bağlanamaz.", ErrorType.Conflict);
        }

        var lotIds = request.LotIds.Distinct().ToList();
        var lots = await db.Set<BoyahaneYarnLot>().Where(l => lotIds.Contains(l.Id)).ToListAsync(cancellationToken);
        if (lots.Count != lotIds.Count)
        {
            return new Error(InvalidRoll, "Seçilen lotlardan biri bulunamadı.", ErrorType.Conflict);
        }

        var ownerId = OwnerOf(line.Order);
        foreach (var lot in lots)
        {
            var problem = lot.Status != YarnLotStatus.Approved ? "lot reddedilmiş"
                : lot.YarnId != line.YarnId ? "iplik kartı satırla aynı değil"
                : lot.CustomerId != ownerId ? "malın sahibi siparişle uyuşmuyor"
                : lot.OrderLineId is not null && lot.OrderLineId != lineId ? "başka bir sipariş satırına bağlı"
                : null;
            if (problem is not null)
            {
                return new Error(InvalidRoll, $"{lot.LotNo} bağlanamaz: {problem}.", ErrorType.Conflict);
            }
        }

        var releasedLots = line.YarnLots.Where(l => !lotIds.Contains(l.Id)).ToList();
        var releasedLotIds = releasedLots.Select(l => l.Id).ToList();
        if (await db.Set<BoyahaneBatchItem>().AnyAsync(i => i.IsActive && i.YarnLotId != null && releasedLotIds.Contains(i.YarnLotId.Value), cancellationToken))
        {
            return new Error(InvalidRoll, "Partiye girmiş lot siparişten çıkarılamaz.", ErrorType.Conflict);
        }

        foreach (var lot in releasedLots)
        {
            lot.OrderLineId = null;
        }

        foreach (var lot in lots)
        {
            lot.OrderLineId = lineId;
        }

        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    private static bool HasAssignments(BoyahaneOrderLine line) => line.Rolls.Count > 0 || line.YarnLots.Count > 0;

    private static void ReleaseAssignments(BoyahaneOrder order)
    {
        foreach (var roll in order.Lines.SelectMany(l => l.Rolls))
        {
            roll.OrderLineId = null;
        }

        foreach (var lot in order.Lines.SelectMany(l => l.YarnLots))
        {
            lot.OrderLineId = null;
        }
    }

    private static decimal AssignedMeters(BoyahaneOrderLine line) => line.Rolls.Sum(t => t.InspectedMeters ?? t.Meters);

    private static decimal AssignedKilograms(BoyahaneOrderLine line) =>
        line.Rolls.Sum(t => t.InspectedKilograms ?? t.Kilograms) + line.YarnLots.Sum(l => l.Kilograms);

    private static decimal? DifferencePercent(BoyahaneOrderLine line)
    {
        if (!HasAssignments(line))
        {
            return null;
        }

        var assigned = line.Unit == StockUnit.Meter ? AssignedMeters(line) : AssignedKilograms(line);
        var difference = (assigned - line.Quantity) * 100 / line.Quantity;
        return Math.Abs(difference) > QuantityTolerancePercent ? Math.Round(difference, 1) : null;
    }

    /// <summary>Müşteri varsa onun para birimi; stoğa üretimde yerel para.</summary>
    private async Task<string> CustomerCurrencyAsync(int? customerId, CancellationToken cancellationToken) =>
        customerId is { } id
            ? await db.Set<BoyahaneCustomer>().Where(c => c.Id == id).Select(c => c.Currency).SingleAsync(cancellationToken)
            : "TRY";

    /// <summary>Malın sahibi: fason işte siparişin müşterisi, kendi malında işletme (boş).</summary>
    private static int? OwnerOf(BoyahaneOrder order) =>
        order.Ownership == OrderOwnership.Commission ? order.CustomerId : null;

    private async Task<Error?> ValidateReferencesAsync(BoyahaneOrderRequest request, BoyahaneOrder? existing, CancellationToken cancellationToken)
    {
        if (await ReferenceValidation.RequireSelectableAsync(
                db.Set<BoyahaneCustomer>(), request.CustomerId, existing?.CustomerId, "customerId", "Müşteri", cancellationToken) is { } customerError)
        {
            return customerError;
        }

        if (await CurrencyRules.RequireActiveAsync(db, request.Currency, existing?.Currency, "currency", cancellationToken) is { } currencyError)
        {
            return currencyError;
        }

        foreach (var fabricId in request.Lines.Where(l => l.ProcessType != OrderProcessType.YarnDyeing).Select(l => l.FabricId).Distinct())
        {
            var current = existing?.Lines.Any(l => l.FabricId == fabricId) == true ? fabricId : null;
            if (await ReferenceValidation.RequireSelectableAsync(
                    db.Set<BoyahaneFabric>(), fabricId, current, "lines", "Kumaş kartı", cancellationToken) is { } fabricError)
            {
                return fabricError;
            }
        }

        foreach (var yarnId in request.Lines.Where(l => l.ProcessType == OrderProcessType.YarnDyeing).Select(l => l.YarnId).Distinct())
        {
            var current = existing?.Lines.Any(l => l.YarnId == yarnId) == true ? yarnId : null;
            if (await ReferenceValidation.RequireSelectableAsync(
                    db.Set<BoyahaneYarn>(), yarnId, current, "lines", "İplik kartı", cancellationToken) is { } yarnError)
            {
                return yarnError;
            }
        }

        return null;
    }

    private static void Apply(BoyahaneOrder order, BoyahaneOrderRequest request, string customerCurrency)
    {
        order.OrderDate = request.OrderDate;
        order.CustomerId = request.CustomerId;
        order.CustomerOrderNo = NullIfBlank(request.CustomerOrderNo);
        order.Ownership = request.Ownership;
        order.DueDate = request.DueDate;
        order.Currency = NullIfBlank(request.Currency)?.ToUpperInvariant() ?? customerCurrency;
        order.Notes = NullIfBlank(request.Notes);
    }

    private sealed record CardInfo(string Code, string Name, string? CustomerColorCode, int? CustomerId, bool IsActive);

    private sealed record ColorwayInfo(int PatternId, string Code, string Name, bool IsActive);

    /// <summary>Satırlarda seçilen renk kartları, desenler ve renk yolları (silinmiş kayıtlar gelmez).</summary>
    private sealed record ArchiveInfo(
        IReadOnlyDictionary<int, CardInfo> Cards,
        IReadOnlyDictionary<int, CardInfo> Patterns,
        IReadOnlyDictionary<int, ColorwayInfo> Colorways);

    private async Task<ArchiveInfo> LoadArchiveAsync(IReadOnlyList<BoyahaneOrderLineRequest> lines, CancellationToken cancellationToken)
    {
        var cardIds = lines.Where(l => l.ColorCardId is not null).Select(l => l.ColorCardId!.Value).Distinct().ToList();
        var patternIds = lines.Where(l => l.PatternId is not null).Select(l => l.PatternId!.Value).Distinct().ToList();
        var colorwayIds = lines.Where(l => l.ColorwayId is not null).Select(l => l.ColorwayId!.Value).Distinct().ToList();

        var cards = await db.Set<BoyahaneColorCard>().AsNoTracking()
            .Where(c => cardIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => new CardInfo(c.Code, c.Name, c.CustomerColorCode, c.CustomerId, c.IsActive), cancellationToken);
        var patterns = await db.Set<BoyahanePattern>().AsNoTracking()
            .Where(p => patternIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => new CardInfo(p.Code, p.Name, null, p.CustomerId, p.IsActive), cancellationToken);
        var colorways = await db.Set<BoyahanePatternColorway>().AsNoTracking()
            .Where(c => colorwayIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => new ColorwayInfo(c.PatternId, c.Code, c.Name, c.IsActive), cancellationToken);
        return new ArchiveInfo(cards, patterns, colorways);
    }

    /// <summary>
    /// Renk kartı ve desen var olmalı; yeni seçimde etkin ve genel ya da siparişin müşterisine ait olmalıdır. Renk yolu
    /// seçilen desene ait olmalıdır. Satırda zaten seçili olan kayıt pasif olsa da kabul edilir.
    /// </summary>
    private static Error? ValidateArchive(BoyahaneOrderRequest request, BoyahaneOrder? existing, ArchiveInfo archive)
    {
        foreach (var item in request.Lines)
        {
            var current = item.Id is { } lineId ? existing?.Lines.FirstOrDefault(l => l.Id == lineId) : null;

            if (item.ColorCardId is { } cardId)
            {
                if (!archive.Cards.TryGetValue(cardId, out var card))
                {
                    return ReferenceValidation.InvalidReference("lines", "Renk kartı bulunamadı.");
                }

                if (current?.ColorCardId != cardId && !card.IsActive)
                {
                    return ReferenceValidation.InvalidReference("lines", $"{card.Code} renk kartı pasif durumda.");
                }

                if (current?.ColorCardId != cardId && card.CustomerId is not null && card.CustomerId != request.CustomerId)
                {
                    return ReferenceValidation.InvalidReference("lines", $"{card.Code} renk kartı başka bir müşteriye ait.");
                }
            }

            if (item.PatternId is { } patternId)
            {
                if (!archive.Patterns.TryGetValue(patternId, out var pattern))
                {
                    return ReferenceValidation.InvalidReference("lines", "Desen kartı bulunamadı.");
                }

                if (current?.PatternId != patternId && !pattern.IsActive)
                {
                    return ReferenceValidation.InvalidReference("lines", $"{pattern.Code} desen kartı pasif durumda.");
                }

                if (current?.PatternId != patternId && pattern.CustomerId is not null && pattern.CustomerId != request.CustomerId)
                {
                    return ReferenceValidation.InvalidReference("lines", $"{pattern.Code} desen kartı başka bir müşteriye ait.");
                }
            }

            if (item.ColorwayId is { } colorwayId)
            {
                if (!archive.Colorways.TryGetValue(colorwayId, out var colorway) || colorway.PatternId != item.PatternId)
                {
                    return ReferenceValidation.InvalidReference("lines", "Renk yolu seçilen desene ait değil.");
                }

                if (current?.ColorwayId != colorwayId && !colorway.IsActive)
                {
                    return ReferenceValidation.InvalidReference("lines", $"{colorway.Code} renk yolu pasif durumda.");
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Renk kartı ya da desen seçildiğinde renk adı ve kodu karttan yazılır. Kart aynı kaldıkça satırdaki ad ve kod korunur:
    /// kart sonradan değişse de sipariş ve lab reçetesi seçildiği anki adı taşır.
    /// </summary>
    private static void ApplyLine(BoyahaneOrderLine line, BoyahaneOrderLineRequest item, ArchiveInfo archive)
    {
        var yarn = item.ProcessType == OrderProcessType.YarnDyeing;
        var printing = item.ProcessType == OrderProcessType.Printing;
        var cardId = item.ProcessType is OrderProcessType.FabricDyeing or OrderProcessType.YarnDyeing ? item.ColorCardId : null;
        var patternId = printing ? item.PatternId : null;
        var colorwayId = printing && patternId is not null ? item.ColorwayId : null;

        if (cardId is { } selectedCard)
        {
            if (line.ColorCardId != selectedCard || line.Id == 0)
            {
                var card = archive.Cards[selectedCard];
                line.ColorName = Truncate(card.Name, 100);
                line.ColorCode = Truncate(card.CustomerColorCode ?? card.Code, 50);
            }
        }
        else if (patternId is { } selectedPattern)
        {
            if (line.PatternId != selectedPattern || line.ColorwayId != colorwayId || line.Id == 0)
            {
                var pattern = archive.Patterns[selectedPattern];
                var colorway = colorwayId is { } selectedColorway ? archive.Colorways[selectedColorway] : null;
                line.ColorName = Truncate(colorway is null ? pattern.Name : $"{pattern.Name} / {colorway.Name}", 100);
                line.ColorCode = Truncate(colorway is null ? pattern.Code : $"{pattern.Code}/{colorway.Code}", 50);
            }
        }
        else
        {
            line.ColorName = NullIfBlank(item.ColorName);
            line.ColorCode = NullIfBlank(item.ColorCode);
        }

        line.ColorCardId = cardId;
        line.PatternId = patternId;
        line.ColorwayId = colorwayId;
        line.ProcessType = item.ProcessType;
        line.FabricId = yarn ? null : item.FabricId;
        line.YarnId = yarn ? item.YarnId : null;
        line.Quantity = item.Quantity;
        line.Unit = item.Unit;
        line.BillingUnit = item.BillingUnit;
        line.UnitPrice = item.UnitPrice;
        line.Notes = NullIfBlank(item.Notes);
    }

    private static string Truncate(string value, int maxLength) => value.Length <= maxLength ? value : value[..maxLength];
}
