using ERP.Boyahane.Application.Features.Warehousing;
using System.Linq.Expressions;
using ERP.Boyahane.Domain.Entities;
using ERP.Boyahane.Domain.Enums;
using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Core.Application.Features.MasterData;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using static ERP.Core.Application.Features.MasterData.MasterDataRules;

namespace ERP.Boyahane.Application.Features.Purchasing;

public sealed record BoyahaneSupplierDto(int Id, string Code, string Name, string? TaxNumber, string? Phone, string? Email, string Currency, int? LeadTimeDays, bool IsActive);

public sealed record BoyahaneSupplierRequest(
    string Code,
    string Name,
    string? TaxNumber = null,
    string? Phone = null,
    string? Email = null,
    string? Currency = null,
    int? LeadTimeDays = null,
    bool IsActive = true);

public sealed class BoyahaneSupplierRequestValidator : AbstractValidator<BoyahaneSupplierRequest>
{
    public BoyahaneSupplierRequestValidator()
    {
        RuleFor(x => x.Code).ValidCode();
        RuleFor(x => x.Name).ValidName(200);
        RuleFor(x => x.TaxNumber).OptionalText(20);
        RuleFor(x => x.Phone).OptionalText(30);
        RuleFor(x => x.Email).EmailAddress().WithMessage("Geçerli bir e-posta girin.").MaximumLength(150).When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Currency).Matches(@"^\s*[A-Za-z]{3}\s*$").WithMessage("Para birimi üç harfli ISO kodu olmalıdır.").When(x => !string.IsNullOrWhiteSpace(x.Currency));
        RuleFor(x => x.LeadTimeDays).InclusiveBetween(0, 3650).WithMessage("Teslim süresi geçersiz.").When(x => x.LeadTimeDays is not null);
    }
}

public sealed class BoyahaneSupplierService(IAppDbContext db) : MasterDataService<BoyahaneSupplier, BoyahaneSupplierDto, BoyahaneSupplierRequest>(db)
{
    protected override DbSet<BoyahaneSupplier> Set => Db.Set<BoyahaneSupplier>();
    protected override string Label => "Tedarikçi";
    protected override string CodeField => "code";

    protected override string CodeOf(BoyahaneSupplierRequest request) => request.Code;
    protected override Expression<Func<BoyahaneSupplier, bool>> WithCode(string code) => e => e.Code == code;
    protected override Expression<Func<BoyahaneSupplier, bool>> Matches(string term) => e => e.Code.Contains(term) || e.Name.Contains(term);
    protected override IQueryable<BoyahaneSupplier> Order(IQueryable<BoyahaneSupplier> query) => query.OrderBy(e => e.Code);
    protected override Expression<Func<BoyahaneSupplier, BoyahaneSupplierDto>> ToDto => e => new BoyahaneSupplierDto(e.Id, e.Code, e.Name, e.TaxNumber, e.Phone, e.Email, e.Currency, e.LeadTimeDays, e.IsActive);
    protected override Expression<Func<BoyahaneSupplier, LookupDto>> ToLookup => e => new LookupDto(e.Id, e.Code, e.Name);

    protected override void Apply(BoyahaneSupplierRequest request, BoyahaneSupplier entity, string code)
    {
        entity.Code = code;
        entity.Name = request.Name.Trim();
        entity.TaxNumber = NullIfBlank(request.TaxNumber);
        entity.Phone = NullIfBlank(request.Phone);
        entity.Email = NullIfBlank(request.Email);
        entity.Currency = string.IsNullOrWhiteSpace(request.Currency) ? "TRY" : request.Currency.Trim().ToUpperInvariant();
        entity.LeadTimeDays = request.LeadTimeDays;
        entity.IsActive = request.IsActive;
    }

    protected override async Task<string?> FindUsageAsync(int id, CancellationToken cancellationToken) =>
        await Db.Set<BoyahanePurchaseOrder>().AnyAsync(o => o.SupplierId == id, cancellationToken)
        || await Db.Set<BoyahaneChemicalLot>().AnyAsync(l => l.SupplierId == id, cancellationToken)
            ? "satın alma siparişleri ya da lotlar"
            : null;
}

public sealed class BoyahanePurchaseOrderQuery : PageRequest
{
    /// <summary>Sipariş no ya da tedarikçi.</summary>
    public string? Search { get; set; }
}

public sealed record BoyahanePurchaseOrderListItemDto(
    int Id,
    string OrderNo,
    string SupplierName,
    DateOnly OrderDate,
    DateOnly? ExpectedDate,
    int LineCount,
    decimal OrderedKilograms,
    decimal ReceivedKilograms,
    decimal Amount,
    string Currency,
    PurchaseOrderStatus Status);

public sealed record BoyahanePurchaseLotDto(int Id, string LotNo, string? SupplierLotNo, DateOnly ReceiptDate, DateOnly? ExpiryDate, decimal Kilograms, decimal RemainingKilograms);

public sealed record BoyahanePurchaseLineDto(
    int Id,
    int DyeChemicalId,
    string Code,
    string Name,
    decimal Kilograms,
    decimal? UnitPrice,
    decimal? Amount,
    decimal ReceivedKilograms,
    decimal OpenKilograms,
    IReadOnlyList<BoyahanePurchaseLotDto> Lots);

public sealed record BoyahanePurchaseOrderDto(
    int Id,
    string OrderNo,
    int SupplierId,
    string SupplierName,
    DateOnly OrderDate,
    DateOnly? ExpectedDate,
    string Currency,
    string? Notes,
    PurchaseOrderStatus Status,
    decimal Amount,
    IReadOnlyList<BoyahanePurchaseLineDto> Lines);

public sealed record BoyahanePurchaseLineRequest(int DyeChemicalId, decimal Kilograms, decimal? UnitPrice = null);

/// <param name="ExpectedDate">Boşsa tedarikçinin teslim süresinden hesaplanır.</param>
/// <param name="Currency">Boşsa tedarikçinin para birimi.</param>
public sealed record BoyahanePurchaseOrderRequest(
    int SupplierId,
    DateOnly OrderDate,
    IReadOnlyList<BoyahanePurchaseLineRequest> Lines,
    DateOnly? ExpectedDate = null,
    string? Currency = null,
    string? Notes = null);

public sealed class BoyahanePurchaseOrderRequestValidator : AbstractValidator<BoyahanePurchaseOrderRequest>
{
    public BoyahanePurchaseOrderRequestValidator()
    {
        RuleFor(x => x.SupplierId).GreaterThan(0).WithMessage("Tedarikçi seçilmelidir.");
        RuleFor(x => x.OrderDate).NotEmpty().WithMessage("Sipariş tarihi zorunludur.");
        RuleFor(x => x.ExpectedDate).Must((request, date) => date >= request.OrderDate).WithMessage("Beklenen tarih sipariş tarihinden önce olamaz.").When(x => x.ExpectedDate is not null);
        RuleFor(x => x.Currency).Matches(@"^\s*[A-Za-z]{3}\s*$").WithMessage("Para birimi üç harfli ISO kodu olmalıdır.").When(x => !string.IsNullOrWhiteSpace(x.Currency));
        RuleFor(x => x.Notes).OptionalText(1000);
        RuleFor(x => x.Lines).NotEmpty().WithMessage("En az bir satır girilmelidir.");
        RuleFor(x => x.Lines).Must(lines => lines is null || lines.Select(l => l.DyeChemicalId).Distinct().Count() == lines.Count).WithMessage("Aynı boya ya da kimyasal iki satırda olamaz.");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.DyeChemicalId).GreaterThan(0).WithMessage("Boya ya da kimyasal seçilmelidir.");
            line.RuleFor(l => l.Kilograms).InclusiveBetween(0.001m, 9_999_999).WithMessage("Miktar sıfırdan büyük olmalıdır.");
            line.RuleFor(l => l.UnitPrice).InclusiveBetween(0, 99_999_999).WithMessage("Birim fiyat geçersiz.").When(l => l.UnitPrice is not null);
        });
    }
}

public sealed record BoyahanePurchaseReceiptLineRequest(int LineId, decimal Kilograms, string? SupplierLotNo = null, DateOnly? ExpiryDate = null);

/// <param name="LocationId">Gelen lotların adresi; boşsa boya-kimyasal deposunun genel adresi.</param>
public sealed record BoyahanePurchaseReceiptRequest(DateOnly ReceiptDate, IReadOnlyList<BoyahanePurchaseReceiptLineRequest> Lines, int? LocationId = null);

public sealed class BoyahanePurchaseReceiptRequestValidator : AbstractValidator<BoyahanePurchaseReceiptRequest>
{
    public BoyahanePurchaseReceiptRequestValidator()
    {
        RuleFor(x => x.ReceiptDate).NotEmpty().WithMessage("Teslim tarihi zorunludur.");
        RuleFor(x => x.Lines).NotEmpty().WithMessage("Teslim alınan en az bir satır girilmelidir.");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.Kilograms).InclusiveBetween(0.001m, 9_999_999).WithMessage("Miktar sıfırdan büyük olmalıdır.");
            line.RuleFor(l => l.SupplierLotNo).OptionalText(50);
        });
    }
}

/// <param name="SuggestedKilograms">Kritik seviyeye çıkmak için gereken: kritik stok − kullanılabilir − yoldaki.</param>
public sealed record BoyahaneSupplySuggestionDto(
    int DyeChemicalId,
    string Code,
    string Name,
    DyeChemicalKind Kind,
    decimal MinStockKilograms,
    decimal UsableKilograms,
    decimal OnOrderKilograms,
    decimal SuggestedKilograms);

public interface IBoyahanePurchasingService
{
    Task<PagedResult<BoyahanePurchaseOrderListItemDto>> GetListAsync(BoyahanePurchaseOrderQuery query, CancellationToken cancellationToken);
    Task<Result<BoyahanePurchaseOrderDto>> GetAsync(int id, CancellationToken cancellationToken);
    Task<Result<BoyahanePurchaseOrderDto>> SaveAsync(int? id, BoyahanePurchaseOrderRequest request, CancellationToken cancellationToken);
    Task<Result<BoyahanePurchaseOrderDto>> ConfirmAsync(int id, CancellationToken cancellationToken);
    Task<Result<BoyahanePurchaseOrderDto>> ReopenAsync(int id, CancellationToken cancellationToken);
    Task<Result<BoyahanePurchaseOrderDto>> CancelAsync(int id, CancellationToken cancellationToken);
    Task<Result<BoyahanePurchaseOrderDto>> CloseAsync(int id, CancellationToken cancellationToken);
    Task<Result<BoyahanePurchaseOrderDto>> ReceiveAsync(int id, BoyahanePurchaseReceiptRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<BoyahaneSupplySuggestionDto>> GetSuggestionsAsync(CancellationToken cancellationToken);
}

public sealed class BoyahanePurchasingService(IAppDbContext db, IDocumentNumberGenerator numbers, ICurrentUser currentUser, TimeProvider timeProvider) : IBoyahanePurchasingService
{
    private const string OrderSeries = "BoyahanePurchase";
    private const string LotSeries = "BoyahaneChemicalLot";
    private const string InvalidState = "PURCHASE_INVALID_STATE";

    private static readonly Error NotFound = new(ErrorCodes.NotFound, "Satın alma siparişi bulunamadı.", ErrorType.NotFound);

    public Task<PagedResult<BoyahanePurchaseOrderListItemDto>> GetListAsync(BoyahanePurchaseOrderQuery query, CancellationToken cancellationToken)
    {
        var orders = db.Set<BoyahanePurchaseOrder>().AsNoTracking();
        if (NullIfBlank(query.Search) is { } term)
        {
            orders = orders.Where(o => o.OrderNo.Contains(term) || o.Supplier.Name.Contains(term));
        }

        return orders
            .OrderByDescending(o => o.Id)
            .Select(o => new BoyahanePurchaseOrderListItemDto(
                o.Id,
                o.OrderNo,
                o.Supplier.Name,
                o.OrderDate,
                o.ExpectedDate,
                o.Lines.Count,
                o.Lines.Sum(l => l.Kilograms),
                o.Lines.Sum(l => l.ReceivedKilograms),
                o.Lines.Sum(l => l.Kilograms * (l.UnitPrice ?? 0)),
                o.Currency,
                o.Status))
            .ToPagedResultAsync(query, cancellationToken);
    }

    public async Task<Result<BoyahanePurchaseOrderDto>> GetAsync(int id, CancellationToken cancellationToken)
    {
        var order = await db.Set<BoyahanePurchaseOrder>().AsNoTracking()
            .Include(o => o.Supplier)
            .Include(o => o.Lines).ThenInclude(l => l.DyeChemical)
            .SingleOrDefaultAsync(o => o.Id == id, cancellationToken);
        if (order is null)
        {
            return NotFound;
        }

        var lineIds = order.Lines.Select(l => l.Id).ToList();
        var lots = await db.Set<BoyahaneChemicalLot>().AsNoTracking()
            .Where(l => l.PurchaseOrderLineId != null && lineIds.Contains(l.PurchaseOrderLineId.Value))
            .OrderBy(l => l.Id)
            .ToListAsync(cancellationToken);
        var lines = order.Lines.OrderBy(l => l.Id).Select(l => new BoyahanePurchaseLineDto(
            l.Id,
            l.DyeChemicalId,
            l.DyeChemical.Code,
            l.DyeChemical.Name,
            l.Kilograms,
            l.UnitPrice,
            l.UnitPrice is { } price ? Math.Round(l.Kilograms * price, 2) : null,
            l.ReceivedKilograms,
            Math.Max(l.Kilograms - l.ReceivedKilograms, 0),
            lots.Where(x => x.PurchaseOrderLineId == l.Id)
                .Select(x => new BoyahanePurchaseLotDto(x.Id, x.LotNo, x.SupplierLotNo, x.ReceiptDate, x.ExpiryDate, x.ReceivedKilograms, x.RemainingKilograms))
                .ToList())).ToList();
        return new BoyahanePurchaseOrderDto(
            order.Id,
            order.OrderNo,
            order.SupplierId,
            order.Supplier.Name,
            order.OrderDate,
            order.ExpectedDate,
            order.Currency,
            order.Notes,
            order.Status,
            lines.Sum(l => l.Amount ?? 0),
            lines);
    }

    public async Task<Result<BoyahanePurchaseOrderDto>> SaveAsync(int? id, BoyahanePurchaseOrderRequest request, CancellationToken cancellationToken)
    {
        BoyahanePurchaseOrder? order = null;
        if (id is not null)
        {
            order = await db.Set<BoyahanePurchaseOrder>().Include(o => o.Lines).SingleOrDefaultAsync(o => o.Id == id, cancellationToken);
            if (order is null)
            {
                return NotFound;
            }

            if (order.Status != PurchaseOrderStatus.Draft)
            {
                return new Error(InvalidState, "Yalnızca taslak sipariş düzenlenebilir.", ErrorType.Conflict);
            }
        }

        if (await ReferenceValidation.RequireSelectableAsync(db.Set<BoyahaneSupplier>(), request.SupplierId, order?.SupplierId, "supplierId", "Tedarikçi", cancellationToken) is { } supplierError)
        {
            return supplierError;
        }

        foreach (var line in request.Lines)
        {
            var kept = order?.Lines.Any(l => l.DyeChemicalId == line.DyeChemicalId) == true ? line.DyeChemicalId : (int?)null;
            if (await ReferenceValidation.RequireSelectableAsync(db.Set<BoyahaneDyeChemical>(), line.DyeChemicalId, kept, "lines", "Boya / kimyasal", cancellationToken) is { } chemicalError)
            {
                return chemicalError;
            }
        }

        var supplier = await db.Set<BoyahaneSupplier>().AsNoTracking().SingleAsync(s => s.Id == request.SupplierId, cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (order is null)
        {
            var year = request.OrderDate.Year;
            var sequence = await numbers.NextAsync(OrderSeries, year, cancellationToken);
            order = new BoyahanePurchaseOrder { OrderNo = $"SA-{year}-{sequence:D5}" };
            db.Set<BoyahanePurchaseOrder>().Add(order);
        }
        else
        {
            db.Set<BoyahanePurchaseOrderLine>().RemoveRange(order.Lines);
        }

        order.SupplierId = request.SupplierId;
        order.OrderDate = request.OrderDate;
        order.ExpectedDate = request.ExpectedDate ?? (supplier.LeadTimeDays is { } days ? request.OrderDate.AddDays(days) : null);
        order.Currency = string.IsNullOrWhiteSpace(request.Currency) ? supplier.Currency : request.Currency.Trim().ToUpperInvariant();
        order.Notes = NullIfBlank(request.Notes);
        foreach (var line in request.Lines)
        {
            order.Lines.Add(new BoyahanePurchaseOrderLine { DyeChemicalId = line.DyeChemicalId, Kilograms = line.Kilograms, UnitPrice = line.UnitPrice });
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetAsync(order.Id, cancellationToken);
    }

    public Task<Result<BoyahanePurchaseOrderDto>> ConfirmAsync(int id, CancellationToken cancellationToken) =>
        ChangeAsync(id, order => order.Status == PurchaseOrderStatus.Draft ? null : "Yalnızca taslak sipariş onaylanabilir.", PurchaseOrderStatus.Confirmed, cancellationToken);

    public Task<Result<BoyahanePurchaseOrderDto>> ReopenAsync(int id, CancellationToken cancellationToken) =>
        ChangeAsync(id, order => order.Status == PurchaseOrderStatus.Confirmed ? null : "Yalnızca teslimat başlamamış onaylı sipariş taslağa alınabilir.", PurchaseOrderStatus.Draft, cancellationToken);

    public Task<Result<BoyahanePurchaseOrderDto>> CancelAsync(int id, CancellationToken cancellationToken) =>
        ChangeAsync(id, order => order.Status is PurchaseOrderStatus.Draft or PurchaseOrderStatus.Confirmed ? null : "Teslimat başlamış sipariş iptal edilemez; kalanı kapatın.", PurchaseOrderStatus.Cancelled, cancellationToken);

    public Task<Result<BoyahanePurchaseOrderDto>> CloseAsync(int id, CancellationToken cancellationToken) =>
        ChangeAsync(id, order => order.Status == PurchaseOrderStatus.PartiallyReceived ? null : "Yalnızca kısmi teslim alınmış sipariş kapatılabilir.", PurchaseOrderStatus.Closed, cancellationToken);

    public async Task<Result<BoyahanePurchaseOrderDto>> ReceiveAsync(int id, BoyahanePurchaseReceiptRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var order = await db.Set<BoyahanePurchaseOrder>().Include(o => o.Lines).Include(o => o.Supplier).SingleOrDefaultAsync(o => o.Id == id, cancellationToken);
        if (order is null)
        {
            return NotFound;
        }

        if (order.Status is not (PurchaseOrderStatus.Confirmed or PurchaseOrderStatus.PartiallyReceived))
        {
            return new Error(InvalidState, "Yalnızca onaylı ve teslimatı süren siparişe teslim alınır.", ErrorType.Conflict);
        }

        if (request.Lines.Any(r => order.Lines.All(l => l.Id != r.LineId)))
        {
            return new Error(InvalidState, "Teslim satırı bu siparişe ait değil.", ErrorType.Conflict);
        }

        if (request.Lines.Any(r => r.ExpiryDate < request.ReceiptDate))
        {
            return new Error(InvalidState, "Son kullanma tarihi teslim tarihinden önce olamaz.", ErrorType.Conflict);
        }

        if (await BoyahaneWarehouseRules.ValidateLocationAsync(db, request.LocationId, cancellationToken) is { } locationError)
        {
            return locationError;
        }

        var locationId = request.LocationId ?? await BoyahaneWarehouseRules.DefaultLocationIdAsync(db, BoyahaneWarehouseType.Chemical, cancellationToken);
        var year = request.ReceiptDate.Year;
        var first = await numbers.NextRangeAsync(LotSeries, year, request.Lines.Count, cancellationToken);
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var lots = new List<BoyahaneChemicalLot>();
        foreach (var (receipt, index) in request.Lines.Select((r, i) => (r, i)))
        {
            var line = order.Lines.Single(l => l.Id == receipt.LineId);
            line.ReceivedKilograms += receipt.Kilograms;
            var lot = new BoyahaneChemicalLot
            {
                LotNo = $"KL-{year}-{first + index:D5}",
                DyeChemicalId = line.DyeChemicalId,
                SupplierId = order.SupplierId,
                SupplierName = order.Supplier.Name,
                SupplierLotNo = NullIfBlank(receipt.SupplierLotNo),
                PurchaseOrderLineId = line.Id,
                ReceiptDate = request.ReceiptDate,
                ExpiryDate = receipt.ExpiryDate,
                ReceivedKilograms = receipt.Kilograms,
                RemainingKilograms = receipt.Kilograms,
                UnitCost = line.UnitPrice,
                Currency = order.Currency,
                LocationId = locationId,
            };
            lots.Add(lot);
            db.Set<BoyahaneChemicalLot>().Add(lot);
        }

        await db.SaveChangesAsync(cancellationToken);
        foreach (var lot in lots)
        {
            db.Set<BoyahaneChemicalMovement>().Add(new BoyahaneChemicalMovement
            {
                LotId = lot.Id,
                Type = ChemicalMovementType.Receipt,
                Kilograms = lot.ReceivedKilograms,
                Reference = order.OrderNo,
                CreatedAt = now,
                CreatedBy = currentUser.UserName,
            });
        }

        order.RefreshReceiptStatus();
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    public async Task<IReadOnlyList<BoyahaneSupplySuggestionDto>> GetSuggestionsAsync(CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var chemicals = await db.Set<BoyahaneDyeChemical>().AsNoTracking()
            .Where(c => c.IsActive && c.MinStockKilograms != null)
            .OrderBy(c => c.Code)
            .Select(c => new { c.Id, c.Code, c.Name, c.Kind, Min = c.MinStockKilograms!.Value })
            .ToListAsync(cancellationToken);
        var usable = await db.Set<BoyahaneChemicalLot>().AsNoTracking()
            .Where(l => l.RemainingKilograms > 0 && (l.ExpiryDate == null || l.ExpiryDate >= today))
            .GroupBy(l => l.DyeChemicalId)
            .Select(g => new { g.Key, Total = g.Sum(l => l.RemainingKilograms) })
            .ToDictionaryAsync(x => x.Key, x => x.Total, cancellationToken);
        var open = await db.Set<BoyahanePurchaseOrderLine>().AsNoTracking()
            .Where(l => l.Order.Status == PurchaseOrderStatus.Confirmed || l.Order.Status == PurchaseOrderStatus.PartiallyReceived)
            .Where(l => l.Kilograms > l.ReceivedKilograms)
            .GroupBy(l => l.DyeChemicalId)
            .Select(g => new { g.Key, Total = g.Sum(l => l.Kilograms - l.ReceivedKilograms) })
            .ToDictionaryAsync(x => x.Key, x => x.Total, cancellationToken);

        return chemicals
            .Select(c =>
            {
                var have = usable.GetValueOrDefault(c.Id);
                var coming = open.GetValueOrDefault(c.Id);
                return new BoyahaneSupplySuggestionDto(c.Id, c.Code, c.Name, c.Kind, c.Min, have, coming, Math.Max(c.Min - have - coming, 0));
            })
            .Where(s => s.SuggestedKilograms > 0)
            .ToList();
    }

    private async Task<Result<BoyahanePurchaseOrderDto>> ChangeAsync(int id, Func<BoyahanePurchaseOrder, string?> check, PurchaseOrderStatus target, CancellationToken cancellationToken)
    {
        var order = await db.Set<BoyahanePurchaseOrder>().SingleOrDefaultAsync(o => o.Id == id, cancellationToken);
        if (order is null)
        {
            return NotFound;
        }

        if (check(order) is { } message)
        {
            return new Error(InvalidState, message, ErrorType.Conflict);
        }

        order.Status = target;
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }
}
