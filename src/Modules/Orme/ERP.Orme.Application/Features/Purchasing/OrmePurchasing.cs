using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Core.Application.Features.MasterData;
using ERP.Orme.Application.Features.Receipts;
using ERP.Orme.Domain.Entities;
using ERP.Orme.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using static ERP.Core.Application.Features.MasterData.MasterDataRules;

namespace ERP.Orme.Application.Features.Purchasing;

public sealed class OrmePurchaseOrderQuery : PageRequest
{
    /// <summary>Sipariş no ya da tedarikçi adında aranır.</summary>
    public string? Search { get; set; }
}

/// <param name="DueDate">Satırların en erken termini.</param>
/// <param name="ReceivedKilograms">Siparişe bağlı iplik girişlerinin toplamı.</param>
public sealed record OrmePurchaseOrderListItemDto(
    int Id,
    string OrderNo,
    string SupplierName,
    DateOnly OrderDate,
    DateOnly? DueDate,
    int LineCount,
    decimal OrderedKilograms,
    decimal ReceivedKilograms,
    decimal Amount,
    string Currency,
    PurchaseOrderStatus Status);

/// <summary>Satırdan teslim alınan iplik girişi (lot).</summary>
public sealed record OrmePurchaseLotDto(int Id, string LotNo, DateOnly ReceiptDate, string? DespatchNo, string? ProducerLotNo, int PackageCount, decimal Kilograms);

/// <param name="Amount">Kilo × birim fiyat; fiyat girilmemişse boş.</param>
/// <param name="OpenKilograms">Sipariş − teslim; fazla teslimde sıfır.</param>
public sealed record OrmePurchaseLineDto(
    int Id,
    int YarnId,
    string YarnCode,
    string YarnName,
    string? YarnColor,
    decimal Kilograms,
    decimal? UnitPrice,
    decimal? Amount,
    DateOnly? DueDate,
    string? Notes,
    decimal ReceivedKilograms,
    decimal OpenKilograms,
    IReadOnlyList<OrmePurchaseLotDto> Lots);

public sealed record OrmePurchaseOrderDto(
    int Id,
    string OrderNo,
    int SupplierId,
    string SupplierCode,
    string SupplierName,
    string? SupplierTaxNumber,
    string? SupplierPhone,
    string? SupplierEmail,
    DateOnly OrderDate,
    string Currency,
    string? Notes,
    PurchaseOrderStatus Status,
    decimal TotalKilograms,
    decimal ReceivedKilograms,
    decimal Amount,
    bool HasUnpricedLines,
    DateTime CreatedAt,
    string? CreatedBy,
    IReadOnlyList<OrmePurchaseLineDto> Lines);

/// <param name="DueDate">Boşsa sipariş tarihi + tedarikçinin teslim süresi; süre tanımlı değilse boş kalır.</param>
public sealed record OrmePurchaseLineRequest(int YarnId, decimal Kilograms, decimal? UnitPrice = null, DateOnly? DueDate = null, string? Notes = null);

/// <param name="Currency">Boşsa tedarikçinin para birimi.</param>
public sealed record OrmePurchaseOrderRequest(
    int SupplierId,
    DateOnly OrderDate,
    IReadOnlyList<OrmePurchaseLineRequest> Lines,
    string? Currency = null,
    string? Notes = null);

public sealed class OrmePurchaseOrderRequestValidator : AbstractValidator<OrmePurchaseOrderRequest>
{
    public const int MaxLines = 100;

    public OrmePurchaseOrderRequestValidator()
    {
        RuleFor(x => x.SupplierId).GreaterThan(0).WithMessage("Tedarikçi seçilmelidir.");
        RuleFor(x => x.OrderDate).NotEmpty().WithMessage("Sipariş tarihi zorunludur.");
        RuleFor(x => x.Currency)
            .Matches(@"^\s*[A-Za-z]{3}\s*$").WithMessage("Para birimi üç harfli ISO kodu olmalıdır (örn. TRY, USD).")
            .When(x => !string.IsNullOrWhiteSpace(x.Currency));
        RuleFor(x => x.Notes).OptionalText(1000);
        RuleFor(x => x.Lines)
            .NotEmpty().WithMessage("En az bir satır girilmelidir.")
            .Must(lines => lines is null || lines.Count <= MaxLines).WithMessage($"Bir siparişte en fazla {MaxLines} satır olabilir.")
            .Custom((lines, context) =>
            {
                if (lines is null)
                {
                    return;
                }

                var orderDate = context.InstanceToValidate.OrderDate;
                for (var i = 0; i < lines.Count; i++)
                {
                    var line = lines[i];
                    if (line.YarnId <= 0)
                    {
                        context.AddFailure($"{i + 1}. satır: iplik seçilmelidir.");
                    }

                    if (line.Kilograms is <= 0 or > 9_999_999 || decimal.Round(line.Kilograms, 2) != line.Kilograms)
                    {
                        context.AddFailure($"{i + 1}. satır: kilo sıfırdan büyük ve en fazla iki ondalık olmalıdır.");
                    }

                    if (line.UnitPrice is < 0 or > 99_999_999)
                    {
                        context.AddFailure($"{i + 1}. satır: birim fiyat geçersiz.");
                    }

                    if (line.DueDate is { } due && due < orderDate)
                    {
                        context.AddFailure($"{i + 1}. satır: termin sipariş tarihinden önce olamaz.");
                    }

                    if (line.Notes?.Trim().Length > 500)
                    {
                        context.AddFailure($"{i + 1}. satır: not en fazla 500 karakter olabilir.");
                    }
                }
            });
    }
}

/// <param name="Kilograms">Tartılan kilo; satırın kalanından fazla da olabilir.</param>
/// <param name="PackageCount">Bobin sayısı.</param>
/// <param name="ProducerLotNo">İplik üreticisinin lot numarası; aynı satırın farklı üretici lotları ayrı satır olarak girilir.</param>
public sealed record OrmePurchaseReceiptLineRequest(int LineId, decimal Kilograms, int PackageCount, string? ProducerLotNo = null);

/// <param name="DespatchNo">Tedarikçinin irsaliye numarası; bütün lotlara yazılır.</param>
public sealed record OrmePurchaseReceiptRequest(DateOnly ReceiptDate, IReadOnlyList<OrmePurchaseReceiptLineRequest> Lines, string? DespatchNo = null, string? Notes = null);

public sealed class OrmePurchaseReceiptRequestValidator : AbstractValidator<OrmePurchaseReceiptRequest>
{
    public OrmePurchaseReceiptRequestValidator()
    {
        RuleFor(x => x.ReceiptDate).NotEmpty().WithMessage("Teslim tarihi zorunludur.");
        RuleFor(x => x.DespatchNo).OptionalText(50);
        RuleFor(x => x.Notes).OptionalText(1000);
        RuleFor(x => x.Lines)
            .NotEmpty().WithMessage("Teslim alınan en az bir satır girilmelidir.")
            .Must(lines => lines is null || lines.Count <= 200).WithMessage("Bir teslimde en fazla 200 satır olabilir.");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.LineId).GreaterThan(0).WithMessage("Sipariş satırı seçilmelidir.");
            line.RuleFor(l => l.Kilograms)
                .GreaterThan(0).WithMessage("Kilo sıfırdan büyük olmalıdır.")
                .LessThanOrEqualTo(9_999_999).WithMessage("Kilo çok büyük.")
                .PrecisionScale(12, 2, true).WithMessage("Kilo en fazla iki ondalık olabilir.");
            line.RuleFor(l => l.PackageCount).InclusiveBetween(1, 100_000).WithMessage("Bobin sayısı en az 1 olmalıdır.");
            line.RuleFor(l => l.ProducerLotNo).OptionalText(50);
        });
    }
}

/// <param name="DueDate">İhtiyaç planındaki ihtiyaç tarihi; boşsa tedarikçinin teslim süresinden hesaplanır.</param>
public sealed record OrmePlanPurchaseLineRequest(int YarnId, int SupplierId, decimal Kilograms, DateOnly? DueDate = null);

/// <summary>İplik ihtiyaç planındaki eksiklerden taslak satın alma siparişleri: her tedarikçi için bir sipariş.</summary>
public sealed record OrmePurchaseFromPlanRequest(DateOnly OrderDate, IReadOnlyList<OrmePlanPurchaseLineRequest> Lines);

public sealed class OrmePurchaseFromPlanRequestValidator : AbstractValidator<OrmePurchaseFromPlanRequest>
{
    public OrmePurchaseFromPlanRequestValidator()
    {
        RuleFor(x => x.OrderDate).NotEmpty().WithMessage("Sipariş tarihi zorunludur.");
        RuleFor(x => x.Lines)
            .NotEmpty().WithMessage("En az bir iplik seçilmelidir.")
            .Must(lines => lines is null || lines.Count <= OrmePurchaseOrderRequestValidator.MaxLines * 5).WithMessage("Bir seferde çok fazla satır gönderildi.");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.YarnId).GreaterThan(0).WithMessage("İplik seçilmelidir.");
            line.RuleFor(l => l.SupplierId).GreaterThan(0).WithMessage("Her satır için tedarikçi seçilmelidir.");
            line.RuleFor(l => l.Kilograms)
                .GreaterThan(0).WithMessage("Kilo sıfırdan büyük olmalıdır.")
                .LessThanOrEqualTo(9_999_999).WithMessage("Kilo çok büyük.")
                .PrecisionScale(12, 2, true).WithMessage("Kilo en fazla iki ondalık olabilir.");
        });
    }
}

public sealed record OrmeCreatedPurchaseOrderDto(int Id, string OrderNo, string SupplierName, int LineCount, decimal Kilograms);

public interface IOrmePurchasingService
{
    Task<PagedResult<OrmePurchaseOrderListItemDto>> GetListAsync(OrmePurchaseOrderQuery query, CancellationToken cancellationToken);
    Task<Result<OrmePurchaseOrderDto>> GetAsync(int id, CancellationToken cancellationToken);
    Task<Result<OrmePurchaseOrderDto>> SaveAsync(int? id, OrmePurchaseOrderRequest request, CancellationToken cancellationToken);
    Task<Result<OrmePurchaseOrderDto>> ApproveAsync(int id, CancellationToken cancellationToken);
    Task<Result<OrmePurchaseOrderDto>> ReopenAsync(int id, CancellationToken cancellationToken);
    Task<Result<OrmePurchaseOrderDto>> CancelAsync(int id, CancellationToken cancellationToken);
    Task<Result<OrmePurchaseOrderDto>> CloseAsync(int id, CancellationToken cancellationToken);
    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken);
    Task<Result<OrmePurchaseOrderDto>> ReceiveAsync(int id, OrmePurchaseReceiptRequest request, CancellationToken cancellationToken);
    Task<Result<IReadOnlyList<OrmeCreatedPurchaseOrderDto>>> CreateFromPlanAsync(OrmePurchaseFromPlanRequest request, CancellationToken cancellationToken);
}

/// <summary>Satın alma siparişlerinin teslim durumu: teslim alınan miktar satıra bağlı iplik girişlerinden hesaplanır.</summary>
public static class OrmePurchaseReceipts
{
    /// <summary>Satır başına teslim alınan kilo (bağlı lotların toplamı).</summary>
    public static Task<Dictionary<int, decimal>> ReceivedByLineAsync(IAppDbContext db, IReadOnlyCollection<int> lineIds, CancellationToken cancellationToken) =>
        db.Set<OrmeYarnLot>().AsNoTracking()
            .Where(l => l.PurchaseOrderLineId != null && lineIds.Contains(l.PurchaseOrderLineId.Value))
            .GroupBy(l => l.PurchaseOrderLineId!.Value)
            .Select(g => new { LineId = g.Key, Kilograms = g.Sum(l => l.Kilograms) })
            .ToDictionaryAsync(g => g.LineId, g => g.Kilograms, cancellationToken);

    /// <summary>Siparişin teslim durumunu lotlardan yeniden hesaplar ve kaydeder.</summary>
    public static async Task RefreshStatusAsync(IAppDbContext db, int orderId, CancellationToken cancellationToken)
    {
        var order = await db.Set<OrmePurchaseOrder>().Include(o => o.Lines).SingleOrDefaultAsync(o => o.Id == orderId, cancellationToken);
        if (order is null)
        {
            return;
        }

        var received = await ReceivedByLineAsync(db, order.Lines.Select(l => l.Id).ToList(), cancellationToken);
        order.RefreshReceiptStatus(lineId => received.GetValueOrDefault(lineId));
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Lot düzeltildiğinde ya da silindiğinde bağlı olduğu siparişin durumunu yeniler.</summary>
    public static async Task RefreshStatusByLineAsync(IAppDbContext db, int purchaseOrderLineId, CancellationToken cancellationToken)
    {
        var orderId = await db.Set<OrmePurchaseOrderLine>().Where(l => l.Id == purchaseOrderLineId).Select(l => (int?)l.OrderId).SingleOrDefaultAsync(cancellationToken);
        if (orderId is { } id)
        {
            await RefreshStatusAsync(db, id, cancellationToken);
        }
    }
}

/// <summary>
/// İplik satın alma: taslak → onaylı → (kısmi) teslim → kapatıldı / iptal. Teslim alma, iplik girişleriyle aynı seriden lot açar;
/// lot satıra bağlanır, tedarikçi siparişten gelir, mal işletmenindir. Müşterinin (fason) ipliği satın alınmaz.
/// </summary>
public sealed class OrmePurchasingService(IAppDbContext db, IDocumentNumberGenerator numbers) : IOrmePurchasingService
{
    private const string OrderSeries = "OrmePurchase";
    private const string InvalidState = "ORME_PURCHASE_INVALID_STATE";

    private static readonly Error NotFound = new(ErrorCodes.NotFound, "Satın alma siparişi bulunamadı.", ErrorType.NotFound);

    public Task<PagedResult<OrmePurchaseOrderListItemDto>> GetListAsync(OrmePurchaseOrderQuery query, CancellationToken cancellationToken)
    {
        var orders = db.Set<OrmePurchaseOrder>().AsNoTracking();
        if (NullIfBlank(query.Search) is { } term)
        {
            orders = orders.Where(o => o.OrderNo.Contains(term) || o.Supplier.Name.Contains(term));
        }

        return orders
            .OrderByDescending(o => o.OrderDate).ThenByDescending(o => o.Id)
            .Select(o => new OrmePurchaseOrderListItemDto(
                o.Id,
                o.OrderNo,
                o.Supplier.Name,
                o.OrderDate,
                o.Lines.Min(l => l.DueDate),
                o.Lines.Count,
                o.Lines.Sum(l => (decimal?)l.Kilograms) ?? 0,
                o.Lines.SelectMany(l => l.Lots).Sum(x => (decimal?)x.Kilograms) ?? 0,
                o.Lines.Sum(l => (decimal?)(l.Kilograms * (l.UnitPrice ?? 0))) ?? 0,
                o.Currency,
                o.Status))
            .ToPagedResultAsync(query, cancellationToken);
    }

    public async Task<Result<OrmePurchaseOrderDto>> GetAsync(int id, CancellationToken cancellationToken)
    {
        // Belge olarak sonradan pasife alınan tedarikçi ya da iplik de görünür.
        var order = await db.Set<OrmePurchaseOrder>().AsNoTracking().IgnoreQueryFilters()
            .Include(o => o.Supplier)
            .Include(o => o.Lines).ThenInclude(l => l.Yarn)
            .SingleOrDefaultAsync(o => o.Id == id, cancellationToken);
        if (order is null)
        {
            return NotFound;
        }

        var lineIds = order.Lines.Select(l => l.Id).ToList();
        var lots = await db.Set<OrmeYarnLot>().AsNoTracking()
            .Where(l => l.PurchaseOrderLineId != null && lineIds.Contains(l.PurchaseOrderLineId.Value))
            .OrderBy(l => l.ReceiptDate).ThenBy(l => l.Id)
            .Select(l => new { LineId = l.PurchaseOrderLineId!.Value, Lot = new OrmePurchaseLotDto(l.Id, l.LotNo, l.ReceiptDate, l.DespatchNo, l.ProducerLotNo, l.PackageCount, l.Kilograms) })
            .ToListAsync(cancellationToken);

        var lines = order.Lines.OrderBy(l => l.Id).Select(l =>
        {
            var own = lots.Where(x => x.LineId == l.Id).Select(x => x.Lot).ToList();
            var received = own.Sum(x => x.Kilograms);
            return new OrmePurchaseLineDto(
                l.Id,
                l.YarnId,
                l.Yarn.YarnCode,
                l.Yarn.Name,
                l.Yarn.Color,
                l.Kilograms,
                l.UnitPrice,
                l.UnitPrice is { } price ? Math.Round(l.Kilograms * price, 2, MidpointRounding.AwayFromZero) : null,
                l.DueDate,
                l.Notes,
                received,
                Math.Max(0, l.Kilograms - received),
                own);
        }).ToList();

        var supplier = order.Supplier;
        return new OrmePurchaseOrderDto(
            order.Id,
            order.OrderNo,
            order.SupplierId,
            supplier.Code,
            supplier.Name,
            supplier.TaxNumber,
            supplier.Phone,
            supplier.Email,
            order.OrderDate,
            order.Currency,
            order.Notes,
            order.Status,
            lines.Sum(l => l.Kilograms),
            lines.Sum(l => l.ReceivedKilograms),
            lines.Sum(l => l.Amount ?? 0),
            lines.Any(l => l.UnitPrice is null),
            order.CreatedAt,
            order.CreatedBy,
            lines);
    }

    public async Task<Result<OrmePurchaseOrderDto>> SaveAsync(int? id, OrmePurchaseOrderRequest request, CancellationToken cancellationToken)
    {
        OrmePurchaseOrder? order = null;
        if (id is not null)
        {
            order = await db.Set<OrmePurchaseOrder>().Include(o => o.Lines).SingleOrDefaultAsync(o => o.Id == id, cancellationToken);
            if (order is null)
            {
                return NotFound;
            }

            if (order.Status != PurchaseOrderStatus.Draft)
            {
                return new Error(InvalidState, "Yalnızca taslak sipariş düzenlenebilir.", ErrorType.Conflict);
            }
        }

        if (await ReferenceValidation.RequireSelectableAsync(db.Set<OrmeSupplier>(), request.SupplierId, order?.SupplierId, "supplierId", "Tedarikçi", cancellationToken) is { } supplierError)
        {
            return supplierError;
        }

        // Siparişte zaten bulunan pasif iplik kalabilir; yeni seçilen iplik etkin olmalıdır.
        if (await ReferenceValidation.RequireSelectableAsync(
                db.Set<OrmeYarn>(),
                request.Lines.Select(l => l.YarnId).Distinct().ToList(),
                order?.Lines.Select(l => l.YarnId).ToHashSet() ?? [],
                "lines",
                "İplik kartı",
                cancellationToken) is { } yarnError)
        {
            return yarnError;
        }

        if (await CurrencyRules.RequireActiveAsync(db, request.Currency, order?.Currency, "currency", cancellationToken) is { } currencyError)
        {
            return currencyError;
        }

        var supplier = await db.Set<OrmeSupplier>().AsNoTracking().SingleAsync(s => s.Id == request.SupplierId, cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (order is null)
        {
            var year = request.OrderDate.Year;
            var sequence = await numbers.NextAsync(OrderSeries, year, cancellationToken);
            order = new OrmePurchaseOrder { OrderNo = OrderNo(year, sequence) };
            db.Set<OrmePurchaseOrder>().Add(order);
        }
        else
        {
            db.Set<OrmePurchaseOrderLine>().RemoveRange(order.Lines);
            order.Lines.Clear();
        }

        order.SupplierId = request.SupplierId;
        order.OrderDate = request.OrderDate;
        order.Currency = NullIfBlank(request.Currency)?.ToUpperInvariant() ?? supplier.Currency;
        order.Notes = NullIfBlank(request.Notes);
        foreach (var line in request.Lines)
        {
            order.Lines.Add(new OrmePurchaseOrderLine
            {
                YarnId = line.YarnId,
                Kilograms = line.Kilograms,
                UnitPrice = line.UnitPrice,
                DueDate = line.DueDate ?? DefaultDueDate(request.OrderDate, supplier.LeadTimeDays),
                Notes = NullIfBlank(line.Notes),
            });
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetAsync(order.Id, cancellationToken);
    }

    public Task<Result<OrmePurchaseOrderDto>> ApproveAsync(int id, CancellationToken cancellationToken) =>
        ChangeAsync(id, order => order.Status == PurchaseOrderStatus.Draft ? null : "Yalnızca taslak sipariş onaylanabilir.", PurchaseOrderStatus.Approved, cancellationToken);

    public Task<Result<OrmePurchaseOrderDto>> ReopenAsync(int id, CancellationToken cancellationToken) =>
        ChangeAsync(id, order => order.Status == PurchaseOrderStatus.Approved ? null : "Yalnızca teslimatı başlamamış onaylı sipariş taslağa alınabilir.", PurchaseOrderStatus.Draft, cancellationToken);

    public Task<Result<OrmePurchaseOrderDto>> CancelAsync(int id, CancellationToken cancellationToken) =>
        ChangeAsync(id, order => order.Status is PurchaseOrderStatus.Draft or PurchaseOrderStatus.Approved ? null : "Teslimat başlamış sipariş iptal edilemez; kalanı kapatın.", PurchaseOrderStatus.Cancelled, cancellationToken);

    public Task<Result<OrmePurchaseOrderDto>> CloseAsync(int id, CancellationToken cancellationToken) =>
        ChangeAsync(id, order => order.Status == PurchaseOrderStatus.PartiallyReceived ? null : "Yalnızca kısmi teslim alınmış sipariş kapatılabilir.", PurchaseOrderStatus.Closed, cancellationToken);

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var order = await db.Set<OrmePurchaseOrder>().Include(o => o.Lines).SingleOrDefaultAsync(o => o.Id == id, cancellationToken);
        if (order is null)
        {
            return NotFound;
        }

        if (order.Status != PurchaseOrderStatus.Draft)
        {
            return new Error(InvalidState, "Yalnızca taslak sipariş silinebilir; onaylanmış sipariş iptal edilir.", ErrorType.Conflict);
        }

        db.Set<OrmePurchaseOrderLine>().RemoveRange(order.Lines);
        db.Set<OrmePurchaseOrder>().Remove(order);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    /// <summary>
    /// Teslim alır: her satır iplik girişi (lot) olarak, iplik girişleriyle aynı seriden numara alarak girer. Lotun sahibi işletme,
    /// tedarikçisi siparişin tedarikçisidir. Siparişin durumu teslim alınan miktara göre yenilenir.
    /// </summary>
    public async Task<Result<OrmePurchaseOrderDto>> ReceiveAsync(int id, OrmePurchaseReceiptRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var order = await db.Set<OrmePurchaseOrder>().Include(o => o.Lines).SingleOrDefaultAsync(o => o.Id == id, cancellationToken);
        if (order is null)
        {
            return NotFound;
        }

        if (order.Status is not (PurchaseOrderStatus.Approved or PurchaseOrderStatus.PartiallyReceived))
        {
            return new Error(InvalidState, "Yalnızca onaylı ve teslimatı süren siparişe teslim alınır.", ErrorType.Conflict);
        }

        if (request.Lines.Any(r => order.Lines.All(l => l.Id != r.LineId)))
        {
            return ReferenceValidation.InvalidReference("lines", "Teslim satırlarından biri bu siparişe ait değil.");
        }

        if (request.ReceiptDate < order.OrderDate)
        {
            return ReferenceValidation.InvalidReference("receiptDate", "Teslim tarihi sipariş tarihinden önce olamaz.");
        }

        var year = request.ReceiptDate.Year;
        var first = await numbers.NextRangeAsync(OrmeYarnLotService.Series, year, request.Lines.Count, cancellationToken);
        var despatchNo = NullIfBlank(request.DespatchNo);
        var notes = NullIfBlank(request.Notes) ?? $"{order.OrderNo} satın alma teslimi";
        foreach (var (receipt, index) in request.Lines.Select((r, i) => (r, i)))
        {
            var line = order.Lines.Single(l => l.Id == receipt.LineId);
            db.Set<OrmeYarnLot>().Add(new OrmeYarnLot
            {
                LotNo = OrmeYarnLotService.LotNo(year, first + index),
                ReceiptDate = request.ReceiptDate,
                CustomerId = null,
                SupplierId = order.SupplierId,
                YarnId = line.YarnId,
                DespatchNo = despatchNo,
                ProducerLotNo = NullIfBlank(receipt.ProducerLotNo),
                PackageCount = receipt.PackageCount,
                Kilograms = receipt.Kilograms,
                Notes = notes,
                PurchaseOrderLineId = line.Id,
            });
        }

        await db.SaveChangesAsync(cancellationToken);
        var received = await OrmePurchaseReceipts.ReceivedByLineAsync(db, order.Lines.Select(l => l.Id).ToList(), cancellationToken);
        order.RefreshReceiptStatus(lineId => received.GetValueOrDefault(lineId));
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    /// <summary>
    /// İhtiyaç planındaki eksiklerden taslak siparişler: satırlar tedarikçiye göre gruplanır, her tedarikçiye bir taslak açılır.
    /// Aynı iplik bir tedarikçide tek satırda toplanır. Birim fiyat, aynı tedarikçiden aynı para biriminde verilen son siparişten gelir.
    /// </summary>
    public async Task<Result<IReadOnlyList<OrmeCreatedPurchaseOrderDto>>> CreateFromPlanAsync(OrmePurchaseFromPlanRequest request, CancellationToken cancellationToken)
    {
        var supplierIds = request.Lines.Select(l => l.SupplierId).Distinct().ToList();
        var yarnIds = request.Lines.Select(l => l.YarnId).Distinct().ToList();
        if (await ReferenceValidation.RequireSelectableAsync(db.Set<OrmeSupplier>(), supplierIds, new HashSet<int>(), "lines", "Tedarikçi", cancellationToken) is { } supplierError)
        {
            return supplierError;
        }

        if (await ReferenceValidation.RequireSelectableAsync(db.Set<OrmeYarn>(), yarnIds, new HashSet<int>(), "lines", "İplik kartı", cancellationToken) is { } yarnError)
        {
            return yarnError;
        }

        var suppliers = await db.Set<OrmeSupplier>().AsNoTracking()
            .Where(s => supplierIds.Contains(s.Id))
            .Select(s => new { s.Id, s.Name, s.Currency, s.LeadTimeDays })
            .ToDictionaryAsync(s => s.Id, cancellationToken);
        var prices = (await db.Set<OrmePurchaseOrderLine>().AsNoTracking()
                .Where(l => yarnIds.Contains(l.YarnId) && supplierIds.Contains(l.Order.SupplierId)
                    && l.UnitPrice != null && l.Order.Status != PurchaseOrderStatus.Cancelled)
                .Select(l => new { l.YarnId, l.Order.SupplierId, l.Order.Currency, l.Order.OrderDate, l.Id, Price = l.UnitPrice!.Value })
                .ToListAsync(cancellationToken))
            .Where(p => p.Currency == suppliers[p.SupplierId].Currency)
            .GroupBy(p => (p.SupplierId, p.YarnId))
            .ToDictionary(g => g.Key, g => g.OrderByDescending(p => p.OrderDate).ThenByDescending(p => p.Id).First().Price);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var year = request.OrderDate.Year;
        var created = new List<OrmePurchaseOrder>();
        foreach (var group in request.Lines.GroupBy(l => l.SupplierId).OrderBy(g => suppliers[g.Key].Name))
        {
            var supplier = suppliers[group.Key];
            var sequence = await numbers.NextAsync(OrderSeries, year, cancellationToken);
            var order = new OrmePurchaseOrder
            {
                OrderNo = OrderNo(year, sequence),
                SupplierId = supplier.Id,
                OrderDate = request.OrderDate,
                Currency = supplier.Currency,
                Notes = "İplik ihtiyaç planından oluşturuldu.",
            };

            foreach (var yarn in group.GroupBy(l => l.YarnId))
            {
                // İhtiyaç tarihi geçmişse ya da sipariş tarihinden önceyse termin tedarikçinin teslim süresine göre verilir.
                var due = yarn.Where(l => l.DueDate >= request.OrderDate).Min(l => l.DueDate) ?? DefaultDueDate(request.OrderDate, supplier.LeadTimeDays);
                order.Lines.Add(new OrmePurchaseOrderLine
                {
                    YarnId = yarn.Key,
                    Kilograms = yarn.Sum(l => l.Kilograms),
                    UnitPrice = prices.TryGetValue((supplier.Id, yarn.Key), out var price) ? price : null,
                    DueDate = due,
                });
            }

            db.Set<OrmePurchaseOrder>().Add(order);
            created.Add(order);
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return created
            .Select(o => new OrmeCreatedPurchaseOrderDto(o.Id, o.OrderNo, suppliers[o.SupplierId].Name, o.Lines.Count, o.Lines.Sum(l => l.Kilograms)))
            .ToList();
    }

    private static string OrderNo(int year, int sequence) => $"OSA-{year}-{sequence:D5}";

    private static DateOnly? DefaultDueDate(DateOnly orderDate, int? leadTimeDays) => leadTimeDays is { } days ? orderDate.AddDays(days) : null;

    private async Task<Result<OrmePurchaseOrderDto>> ChangeAsync(int id, Func<OrmePurchaseOrder, string?> check, PurchaseOrderStatus target, CancellationToken cancellationToken)
    {
        var order = await db.Set<OrmePurchaseOrder>().SingleOrDefaultAsync(o => o.Id == id, cancellationToken);
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
