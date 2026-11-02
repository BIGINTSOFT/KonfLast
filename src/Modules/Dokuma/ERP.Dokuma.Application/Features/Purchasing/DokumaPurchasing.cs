using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Core.Application.Features.MasterData;
using ERP.Dokuma.Application.Features.Orders;
using ERP.Dokuma.Application.Features.Receipts;
using ERP.Dokuma.Application.Features.Stock;
using ERP.Dokuma.Domain.Entities;
using ERP.Dokuma.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using static ERP.Core.Application.Features.MasterData.MasterDataRules;

namespace ERP.Dokuma.Application.Features.Purchasing;

/// <summary>Satın alma satırının kalemi.</summary>
public enum PurchaseItemKind
{
    Yarn,
    Material,
}

public sealed class DokumaPurchaseOrderQuery : PageRequest
{
    /// <summary>Sipariş no ya da tedarikçi adında aranır.</summary>
    public string? Search { get; set; }

    public int? SupplierId { get; set; }
}

/// <param name="OpenLineCount">Teslimi tamamlanmamış satır sayısı.</param>
public sealed record DokumaPurchaseOrderListItemDto(
    int Id,
    string OrderNo,
    int SupplierId,
    string SupplierName,
    DateOnly OrderDate,
    DateOnly? DueDate,
    int LineCount,
    int OpenLineCount,
    decimal Amount,
    string Currency,
    PurchaseOrderStatus Status);

/// <summary>Satırdan teslim alınan lot (iplikte iplik lotu, malzemede malzeme lotu).</summary>
public sealed record DokumaPurchaseLotDto(int Id, string LotNo, DateOnly ReceiptDate, string? SupplierLotNo, decimal Quantity);

public sealed record DokumaPurchaseOrderLineDto(
    int Id,
    int Sequence,
    PurchaseItemKind Kind,
    int? YarnId,
    int? MaterialId,
    string ItemCode,
    string ItemName,
    string? ItemDetail,
    decimal Quantity,
    MaterialUnit Unit,
    decimal? UnitPrice,
    decimal? Amount,
    DateOnly? DueDate,
    decimal ReceivedQuantity,
    decimal OpenQuantity,
    string? Notes,
    IReadOnlyList<DokumaPurchaseLotDto> Lots);

/// <summary>Satın alma siparişi; yazdırılan sipariş formu da bu bilgilerle basılır.</summary>
public sealed record DokumaPurchaseOrderDto(
    int Id,
    string OrderNo,
    int SupplierId,
    string SupplierCode,
    string SupplierName,
    string? SupplierTaxNumber,
    string? SupplierPhone,
    string? SupplierEmail,
    DateOnly OrderDate,
    DateOnly? DueDate,
    string Currency,
    string? Notes,
    PurchaseOrderStatus Status,
    DateTime? ApprovedAt,
    string? ApprovedBy,
    DateTime CreatedAt,
    string? CreatedBy,
    decimal Amount,
    bool HasMissingPrices,
    IReadOnlyList<DokumaPurchaseOrderLineDto> Lines);

/// <param name="YarnId">İplik satırında iplik; malzeme satırında boş.</param>
/// <param name="MaterialId">Malzeme satırında malzeme; iplik satırında boş.</param>
/// <param name="Quantity">İplikte kilogram, malzemede malzeme kartının birimi.</param>
public sealed record DokumaPurchaseOrderLineRequest(
    int? YarnId,
    int? MaterialId,
    decimal Quantity,
    decimal? UnitPrice = null,
    DateOnly? DueDate = null,
    string? Notes = null);

/// <param name="DueDate">Boşsa tedarikçinin teslim süresinden hesaplanır.</param>
/// <param name="Currency">Boşsa tedarikçinin para birimi.</param>
public sealed record DokumaPurchaseOrderRequest(
    int SupplierId,
    DateOnly OrderDate,
    IReadOnlyList<DokumaPurchaseOrderLineRequest> Lines,
    DateOnly? DueDate = null,
    string? Currency = null,
    string? Notes = null);

public sealed class DokumaPurchaseOrderRequestValidator : AbstractValidator<DokumaPurchaseOrderRequest>
{
    public const int MaxLines = 100;

    public DokumaPurchaseOrderRequestValidator()
    {
        RuleFor(x => x.SupplierId).GreaterThan(0).WithMessage("Tedarikçi seçilmelidir.");
        RuleFor(x => x.OrderDate).NotEmpty().WithMessage("Sipariş tarihi zorunludur.");
        RuleFor(x => x.DueDate)
            .Must((request, due) => due >= request.OrderDate).WithMessage("Termin sipariş tarihinden önce olamaz.")
            .When(x => x.DueDate is not null);
        RuleFor(x => x.Currency).Matches(@"^\s*[A-Za-z]{3}\s*$").WithMessage("Para birimi üç harfli ISO kodu olmalıdır.").When(x => !string.IsNullOrWhiteSpace(x.Currency));
        RuleFor(x => x.Notes).OptionalText(1000);
        RuleFor(x => x.Lines)
            .NotEmpty().WithMessage("En az bir satır girilmelidir.")
            .Must(lines => lines.Count <= MaxLines).WithMessage($"Bir siparişte en fazla {MaxLines} satır olabilir.")
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
                    if ((line.YarnId is null) == (line.MaterialId is null))
                    {
                        context.AddFailure($"{i + 1}. satır: iplik ya da malzemeden biri seçilmelidir.");
                    }

                    if (line.Quantity is <= 0 or > 9_999_999)
                    {
                        context.AddFailure($"{i + 1}. satır: miktar sıfırdan büyük olmalıdır.");
                    }
                    else if (decimal.Round(line.Quantity, line.YarnId is null ? 3 : 2) != line.Quantity)
                    {
                        context.AddFailure($"{i + 1}. satır: miktar iplikte en fazla iki, malzemede üç ondalık olabilir.");
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

/// <param name="LotNo">İplikte üretici lot no, malzemede tedarikçinin lot / parti no.</param>
/// <param name="PackageCount">İplik satırında bobin sayısı (zorunlu); malzemede kullanılmaz.</param>
public sealed record DokumaPurchaseReceiptLineRequest(int LineId, decimal Quantity, string? LotNo = null, int? PackageCount = null, string? Notes = null);

public sealed record DokumaPurchaseReceiptRequest(DateOnly ReceiptDate, IReadOnlyList<DokumaPurchaseReceiptLineRequest> Lines, string? DespatchNo = null);

public sealed class DokumaPurchaseReceiptRequestValidator : AbstractValidator<DokumaPurchaseReceiptRequest>
{
    public DokumaPurchaseReceiptRequestValidator()
    {
        RuleFor(x => x.ReceiptDate).NotEmpty().WithMessage("Teslim tarihi zorunludur.");
        RuleFor(x => x.DespatchNo).OptionalText(50);
        RuleFor(x => x.Lines).NotEmpty().WithMessage("Teslim alınan en az bir satır girilmelidir.");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.Quantity)
                .GreaterThan(0).WithMessage("Miktar sıfırdan büyük olmalıdır.")
                .LessThanOrEqualTo(9_999_999).WithMessage("Miktar çok büyük.")
                .PrecisionScale(14, 3, true).WithMessage("Miktar en fazla üç ondalık olabilir.");
            line.RuleFor(l => l.LotNo).OptionalText(50);
            line.RuleFor(l => l.Notes).OptionalText(500);
            line.RuleFor(l => l.PackageCount).InclusiveBetween(1, 100_000).WithMessage("Bobin sayısı en az 1 olmalıdır.").When(l => l.PackageCount is not null);
        });
    }
}

/// <param name="RequiredQuantity">
/// Malzemede kritik stok; iplikte onaylı kendi malı siparişlerin henüz verilmemiş fireli ihtiyacı.
/// </param>
/// <param name="AvailableQuantity">Eldeki: malzemede lot kalanı, iplikte işletme malı lotların kalanı.</param>
/// <param name="OnOrderQuantity">Açık satın alma siparişlerinde (taslak dahil) teslim beklenen.</param>
/// <param name="SuggestedQuantity">İhtiyaç − eldeki − siparişte; sıfırın altına inmez.</param>
/// <param name="SupplierId">Önerilen tedarikçi: malzemede kartındaki, iplikte son satın alındığı tedarikçi.</param>
/// <param name="OrderNos">İplikte ihtiyacı doğuran siparişler.</param>
public sealed record DokumaSupplySuggestionDto(
    PurchaseItemKind Kind,
    int ItemId,
    string Code,
    string Name,
    MaterialUnit Unit,
    decimal RequiredQuantity,
    decimal AvailableQuantity,
    decimal OnOrderQuantity,
    decimal SuggestedQuantity,
    int? SupplierId,
    string? SupplierName,
    IReadOnlyList<string> OrderNos);

public sealed record DokumaSuggestionDraftLineRequest(PurchaseItemKind Kind, int ItemId, decimal Quantity, int SupplierId);

/// <param name="OrderDate">Boşsa bugün.</param>
public sealed record CreateDokumaPurchaseDraftsRequest(IReadOnlyList<DokumaSuggestionDraftLineRequest> Lines, DateOnly? OrderDate = null);

public sealed class CreateDokumaPurchaseDraftsRequestValidator : AbstractValidator<CreateDokumaPurchaseDraftsRequest>
{
    public CreateDokumaPurchaseDraftsRequestValidator()
    {
        RuleFor(x => x.Lines)
            .NotEmpty().WithMessage("En az bir öneri seçilmelidir.")
            .Must(lines => lines.Count <= 300).WithMessage("En fazla 300 öneri seçilebilir.");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.Kind).IsInEnum().WithMessage("Geçersiz kalem türü.");
            line.RuleFor(l => l.ItemId).GreaterThan(0).WithMessage("Kalem seçilmelidir.");
            line.RuleFor(l => l.SupplierId).GreaterThan(0).WithMessage("Her öneri için tedarikçi seçilmelidir.");
            line.RuleFor(l => l.Quantity)
                .GreaterThan(0).WithMessage("Miktar sıfırdan büyük olmalıdır.")
                .LessThanOrEqualTo(9_999_999).WithMessage("Miktar çok büyük.");
        });
    }
}

public sealed record DokumaPurchaseDraftDto(int Id, string OrderNo, string SupplierName, int LineCount);

/// <summary>Teslim alınan lotlar ile sipariş satırının teslim miktarı arasındaki bağ.</summary>
public static class DokumaPurchaseReceipts
{
    private static readonly PurchaseOrderStatus[] OpenStatuses = [PurchaseOrderStatus.Draft, PurchaseOrderStatus.Approved, PurchaseOrderStatus.PartiallyReceived];

    /// <summary>
    /// Siparişten gelen lot düzeltilince ya da silinince satırın teslim miktarı lotlardan yeniden hesaplanır, sipariş
    /// durumu güncellenir.
    /// </summary>
    public static async Task RefreshLineAsync(IAppDbContext db, int? purchaseLineId, CancellationToken cancellationToken)
    {
        if (purchaseLineId is not { } lineId)
        {
            return;
        }

        var order = await db.Set<DokumaPurchaseOrder>().Include(o => o.Lines).SingleAsync(o => o.Lines.Any(l => l.Id == lineId), cancellationToken);
        var line = order.Lines.Single(l => l.Id == lineId);
        line.ReceivedQuantity = line.YarnId is not null
            ? await db.Set<DokumaYarnLot>().Where(l => l.PurchaseOrderLineId == lineId).SumAsync(l => (decimal?)l.Kilograms, cancellationToken) ?? 0
            : await db.Set<DokumaMaterialLot>().Where(l => l.PurchaseOrderLineId == lineId).SumAsync(l => (decimal?)l.Quantity, cancellationToken) ?? 0;
        order.RefreshReceiptStatus();
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Açık siparişlerde (taslak dahil) malzeme başına teslim beklenen miktar.</summary>
    public static async Task<Dictionary<int, decimal>> OpenMaterialQuantitiesAsync(IAppDbContext db, CancellationToken cancellationToken) =>
        await db.Set<DokumaPurchaseOrderLine>().AsNoTracking()
            .Where(l => l.MaterialId != null && OpenStatuses.Contains(l.Order.Status) && l.Quantity > l.ReceivedQuantity)
            .GroupBy(l => l.MaterialId!.Value)
            .Select(g => new { g.Key, Total = g.Sum(l => l.Quantity - l.ReceivedQuantity) })
            .ToDictionaryAsync(x => x.Key, x => x.Total, cancellationToken);

    /// <summary>Açık siparişlerde (taslak dahil) iplik başına teslim beklenen kilogram.</summary>
    public static async Task<Dictionary<int, decimal>> OpenYarnQuantitiesAsync(IAppDbContext db, CancellationToken cancellationToken) =>
        await db.Set<DokumaPurchaseOrderLine>().AsNoTracking()
            .Where(l => l.YarnId != null && OpenStatuses.Contains(l.Order.Status) && l.Quantity > l.ReceivedQuantity)
            .GroupBy(l => l.YarnId!.Value)
            .Select(g => new { g.Key, Total = g.Sum(l => l.Quantity - l.ReceivedQuantity) })
            .ToDictionaryAsync(x => x.Key, x => x.Total, cancellationToken);
}

public interface IDokumaPurchasingService
{
    Task<PagedResult<DokumaPurchaseOrderListItemDto>> GetListAsync(DokumaPurchaseOrderQuery query, CancellationToken cancellationToken);
    Task<Result<DokumaPurchaseOrderDto>> GetAsync(int id, CancellationToken cancellationToken);
    Task<Result<DokumaPurchaseOrderDto>> SaveAsync(int? id, DokumaPurchaseOrderRequest request, CancellationToken cancellationToken);
    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken);
    Task<Result<DokumaPurchaseOrderDto>> ApproveAsync(int id, CancellationToken cancellationToken);
    Task<Result<DokumaPurchaseOrderDto>> ReopenAsync(int id, CancellationToken cancellationToken);
    Task<Result<DokumaPurchaseOrderDto>> CancelAsync(int id, CancellationToken cancellationToken);
    Task<Result<DokumaPurchaseOrderDto>> CloseAsync(int id, CancellationToken cancellationToken);
    Task<Result<DokumaPurchaseOrderDto>> ReceiveAsync(int id, DokumaPurchaseReceiptRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<DokumaSupplySuggestionDto>> GetSuggestionsAsync(CancellationToken cancellationToken);
    Task<Result<IReadOnlyList<DokumaPurchaseDraftDto>>> CreateDraftsAsync(CreateDokumaPurchaseDraftsRequest request, CancellationToken cancellationToken);
}

/// <summary>
/// İplik ve malzeme satın alma: taslak → onaylı → (kısmi) teslim → kapalı / iptal. Teslimde iplik satırı işletme malı
/// iplik lotu, malzeme satırı malzeme lotu olarak girer ve satıra bağlanır.
/// </summary>
public sealed class DokumaPurchasingService(
    IAppDbContext db,
    IDocumentNumberGenerator numbers,
    ICurrentUser currentUser,
    TimeProvider timeProvider,
    IDokumaOrderService orders) : IDokumaPurchasingService
{
    private const string OrderSeries = "DokumaPurchaseOrder";
    private const string InvalidState = "DOKUMA_PURCHASE_INVALID_STATE";

    private static readonly Error NotFound = new(ErrorCodes.NotFound, "Satın alma siparişi bulunamadı.", ErrorType.NotFound);

    public Task<PagedResult<DokumaPurchaseOrderListItemDto>> GetListAsync(DokumaPurchaseOrderQuery query, CancellationToken cancellationToken)
    {
        var purchases = db.Set<DokumaPurchaseOrder>().AsNoTracking();
        if (NullIfBlank(query.Search) is { } term)
        {
            purchases = purchases.Where(o => o.OrderNo.Contains(term) || o.Supplier.Name.Contains(term) || o.Supplier.Code.Contains(term));
        }

        if (query.SupplierId is { } supplierId)
        {
            purchases = purchases.Where(o => o.SupplierId == supplierId);
        }

        return purchases
            .OrderByDescending(o => o.OrderDate).ThenByDescending(o => o.Id)
            .Select(o => new DokumaPurchaseOrderListItemDto(
                o.Id,
                o.OrderNo,
                o.SupplierId,
                o.Supplier.Name,
                o.OrderDate,
                o.DueDate,
                o.Lines.Count,
                o.Lines.Count(l => l.ReceivedQuantity < l.Quantity),
                o.Lines.Sum(l => (decimal?)(l.Quantity * (l.UnitPrice ?? 0))) ?? 0,
                o.Currency,
                o.Status))
            .ToPagedResultAsync(query, cancellationToken);
    }

    public async Task<Result<DokumaPurchaseOrderDto>> GetAsync(int id, CancellationToken cancellationToken)
    {
        var order = await db.Set<DokumaPurchaseOrder>().AsNoTracking()
            .Include(o => o.Supplier)
            .Include(o => o.Lines).ThenInclude(l => l.Yarn)
            .Include(o => o.Lines).ThenInclude(l => l.Material)
            .SingleOrDefaultAsync(o => o.Id == id, cancellationToken);
        if (order is null)
        {
            return NotFound;
        }

        var lineIds = order.Lines.Select(l => l.Id).ToList();
        var yarnLots = await db.Set<DokumaYarnLot>().AsNoTracking()
            .Where(l => l.PurchaseOrderLineId != null && lineIds.Contains(l.PurchaseOrderLineId.Value))
            .OrderBy(l => l.Id)
            .Select(l => new { LineId = l.PurchaseOrderLineId!.Value, Lot = new DokumaPurchaseLotDto(l.Id, l.LotNo, l.ReceiptDate, l.ProducerLotNo, l.Kilograms) })
            .ToListAsync(cancellationToken);
        var materialLots = await db.Set<DokumaMaterialLot>().AsNoTracking()
            .Where(l => l.PurchaseOrderLineId != null && lineIds.Contains(l.PurchaseOrderLineId.Value))
            .OrderBy(l => l.Id)
            .Select(l => new { LineId = l.PurchaseOrderLineId!.Value, Lot = new DokumaPurchaseLotDto(l.Id, l.LotNo, l.ReceiptDate, l.SupplierLotNo, l.Quantity) })
            .ToListAsync(cancellationToken);
        var lots = yarnLots.Concat(materialLots).ToLookup(x => x.LineId, x => x.Lot);

        var lines = order.Lines.OrderBy(l => l.Sequence).Select(l => new DokumaPurchaseOrderLineDto(
            l.Id,
            l.Sequence,
            l.YarnId is not null ? PurchaseItemKind.Yarn : PurchaseItemKind.Material,
            l.YarnId,
            l.MaterialId,
            l.Yarn?.YarnCode ?? l.Material?.Code ?? "",
            l.Yarn?.Name ?? l.Material?.Name ?? "",
            l.Yarn?.Color,
            l.Quantity,
            l.Unit,
            l.UnitPrice,
            l.UnitPrice is { } price ? Math.Round(l.Quantity * price, 2) : null,
            l.DueDate ?? order.DueDate,
            l.ReceivedQuantity,
            Math.Max(l.Quantity - l.ReceivedQuantity, 0),
            l.Notes,
            lots[l.Id].ToList())).ToList();

        return new DokumaPurchaseOrderDto(
            order.Id,
            order.OrderNo,
            order.SupplierId,
            order.Supplier.Code,
            order.Supplier.Name,
            order.Supplier.TaxNumber,
            order.Supplier.Phone,
            order.Supplier.Email,
            order.OrderDate,
            order.DueDate,
            order.Currency,
            order.Notes,
            order.Status,
            order.ApprovedAt,
            order.ApprovedBy,
            order.CreatedAt,
            order.CreatedBy,
            lines.Sum(l => l.Amount ?? 0),
            lines.Any(l => l.UnitPrice is null),
            lines);
    }

    public async Task<Result<DokumaPurchaseOrderDto>> SaveAsync(int? id, DokumaPurchaseOrderRequest request, CancellationToken cancellationToken)
    {
        DokumaPurchaseOrder? order = null;
        if (id is not null)
        {
            order = await db.Set<DokumaPurchaseOrder>().Include(o => o.Lines).SingleOrDefaultAsync(o => o.Id == id, cancellationToken);
            if (order is null)
            {
                return NotFound;
            }

            if (order.Status != PurchaseOrderStatus.Draft)
            {
                return new Error(InvalidState, "Yalnızca taslak sipariş düzenlenebilir; onaylı siparişi önce taslağa alın.", ErrorType.Conflict);
            }
        }

        if (await ReferenceValidation.RequireSelectableAsync(db.Set<DokumaSupplier>(), request.SupplierId, order?.SupplierId, "supplierId", "Tedarikçi", cancellationToken) is { } supplierError)
        {
            return supplierError;
        }

        if (await CurrencyRules.RequireActiveAsync(db, request.Currency, order?.Currency, "currency", cancellationToken) is { } currencyError)
        {
            return currencyError;
        }

        // Siparişte zaten bulunan pasif kalem kalabilir; yeni eklenen kalem etkin olmalıdır.
        var yarnIds = request.Lines.Where(l => l.YarnId is not null).Select(l => l.YarnId!.Value).Distinct().ToList();
        var materialIds = request.Lines.Where(l => l.MaterialId is not null).Select(l => l.MaterialId!.Value).Distinct().ToList();
        if (await ReferenceValidation.RequireSelectableAsync(
                db.Set<DokumaYarn>(), yarnIds, order?.Lines.Where(l => l.YarnId is not null).Select(l => l.YarnId!.Value).ToHashSet() ?? [], "lines", "İplik", cancellationToken) is { } yarnError)
        {
            return yarnError;
        }

        if (await ReferenceValidation.RequireSelectableAsync(
                db.Set<DokumaMaterial>(), materialIds, order?.Lines.Where(l => l.MaterialId is not null).Select(l => l.MaterialId!.Value).ToHashSet() ?? [], "lines", "Malzeme", cancellationToken) is { } materialError)
        {
            return materialError;
        }

        var units = await db.Set<DokumaMaterial>().Where(m => materialIds.Contains(m.Id)).ToDictionaryAsync(m => m.Id, m => m.Unit, cancellationToken);
        var supplier = await db.Set<DokumaSupplier>().AsNoTracking().SingleAsync(s => s.Id == request.SupplierId, cancellationToken);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (order is null)
        {
            var year = request.OrderDate.Year;
            var sequence = await numbers.NextAsync(OrderSeries, year, cancellationToken);
            order = new DokumaPurchaseOrder { OrderNo = $"DSA-{year}-{sequence:D5}" };
            db.Set<DokumaPurchaseOrder>().Add(order);
        }
        else
        {
            // Sıra dizini benzersiz olduğu için eski satırlar önce silinir.
            db.Set<DokumaPurchaseOrderLine>().RemoveRange(order.Lines);
            await db.SaveChangesAsync(cancellationToken);
        }

        order.SupplierId = request.SupplierId;
        order.OrderDate = request.OrderDate;
        order.DueDate = request.DueDate ?? (supplier.LeadTimeDays is { } days ? request.OrderDate.AddDays(days) : null);
        order.Currency = NullIfBlank(request.Currency)?.ToUpperInvariant() ?? supplier.Currency;
        order.Notes = NullIfBlank(request.Notes);
        var sequenceNo = 0;
        foreach (var line in request.Lines)
        {
            order.Lines.Add(new DokumaPurchaseOrderLine
            {
                Sequence = ++sequenceNo,
                YarnId = line.YarnId,
                MaterialId = line.YarnId is null ? line.MaterialId : null,
                Quantity = line.Quantity,
                Unit = line.YarnId is null && line.MaterialId is { } materialId ? units[materialId] : MaterialUnit.Kilogram,
                UnitPrice = line.UnitPrice,
                DueDate = line.DueDate,
                Notes = NullIfBlank(line.Notes),
            });
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetAsync(order.Id, cancellationToken);
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var order = await db.Set<DokumaPurchaseOrder>().Include(o => o.Lines).SingleOrDefaultAsync(o => o.Id == id, cancellationToken);
        if (order is null)
        {
            return NotFound;
        }

        if (order.Status != PurchaseOrderStatus.Draft)
        {
            return new Error(InvalidState, "Yalnızca taslak sipariş silinebilir; onaylanmış sipariş iptal edilir.", ErrorType.Conflict);
        }

        db.Set<DokumaPurchaseOrderLine>().RemoveRange(order.Lines);
        db.Set<DokumaPurchaseOrder>().Remove(order);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<DokumaPurchaseOrderDto>> ApproveAsync(int id, CancellationToken cancellationToken)
    {
        var order = await db.Set<DokumaPurchaseOrder>().Include(o => o.Lines).SingleOrDefaultAsync(o => o.Id == id, cancellationToken);
        if (order is null)
        {
            return NotFound;
        }

        if (order.Status != PurchaseOrderStatus.Draft)
        {
            return new Error(InvalidState, "Yalnızca taslak sipariş onaylanabilir.", ErrorType.Conflict);
        }

        if (order.Lines.Count == 0)
        {
            return new Error(InvalidState, "Satırı olmayan sipariş onaylanamaz.", ErrorType.Conflict);
        }

        order.Status = PurchaseOrderStatus.Approved;
        order.ApprovedAt = timeProvider.GetUtcNow().UtcDateTime;
        order.ApprovedBy = currentUser.UserName;
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    public async Task<Result<DokumaPurchaseOrderDto>> ReopenAsync(int id, CancellationToken cancellationToken)
    {
        var order = await db.Set<DokumaPurchaseOrder>().Include(o => o.Lines).SingleOrDefaultAsync(o => o.Id == id, cancellationToken);
        if (order is null)
        {
            return NotFound;
        }

        if (order.Status != PurchaseOrderStatus.Approved || order.Lines.Any(l => l.ReceivedQuantity > 0))
        {
            return new Error(InvalidState, "Yalnızca teslimatı başlamamış onaylı sipariş taslağa alınabilir.", ErrorType.Conflict);
        }

        order.Status = PurchaseOrderStatus.Draft;
        order.ApprovedAt = null;
        order.ApprovedBy = null;
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    public async Task<Result<DokumaPurchaseOrderDto>> CancelAsync(int id, CancellationToken cancellationToken)
    {
        var order = await db.Set<DokumaPurchaseOrder>().Include(o => o.Lines).SingleOrDefaultAsync(o => o.Id == id, cancellationToken);
        if (order is null)
        {
            return NotFound;
        }

        if (order.Status is not (PurchaseOrderStatus.Draft or PurchaseOrderStatus.Approved) || order.Lines.Any(l => l.ReceivedQuantity > 0))
        {
            return new Error(InvalidState, "Teslimat başlamış sipariş iptal edilemez; kalanı kapatın.", ErrorType.Conflict);
        }

        order.Status = PurchaseOrderStatus.Cancelled;
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    public async Task<Result<DokumaPurchaseOrderDto>> CloseAsync(int id, CancellationToken cancellationToken)
    {
        var order = await db.Set<DokumaPurchaseOrder>().SingleOrDefaultAsync(o => o.Id == id, cancellationToken);
        if (order is null)
        {
            return NotFound;
        }

        if (order.Status != PurchaseOrderStatus.PartiallyReceived)
        {
            return new Error(InvalidState, "Yalnızca kısmi teslim alınmış sipariş kapatılabilir.", ErrorType.Conflict);
        }

        order.Status = PurchaseOrderStatus.Closed;
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    /// <summary>
    /// Teslim alma: her satır ayrı lot olarak girer. İplik işletme malı lot olur (tedarikçi siparişten); malzeme lotunun
    /// birim fiyatı ve para birimi siparişten gelir. Kalandan fazlası da kabul edilir.
    /// </summary>
    public async Task<Result<DokumaPurchaseOrderDto>> ReceiveAsync(int id, DokumaPurchaseReceiptRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var order = await db.Set<DokumaPurchaseOrder>().Include(o => o.Lines).SingleOrDefaultAsync(o => o.Id == id, cancellationToken);
        if (order is null)
        {
            return NotFound;
        }

        if (order.Status is not (PurchaseOrderStatus.Approved or PurchaseOrderStatus.PartiallyReceived))
        {
            return new Error(InvalidState, "Yalnızca onaylı ve teslimatı süren siparişten teslim alınır.", ErrorType.Conflict);
        }

        if (request.ReceiptDate < order.OrderDate)
        {
            return ReferenceValidation.InvalidReference("receiptDate", "Teslim tarihi sipariş tarihinden önce olamaz.");
        }

        if (request.Lines.Any(r => order.Lines.All(l => l.Id != r.LineId)))
        {
            return ReferenceValidation.InvalidReference("lines", "Teslim satırlarından biri bu siparişe ait değil.");
        }

        for (var i = 0; i < request.Lines.Count; i++)
        {
            var receipt = request.Lines[i];
            var line = order.Lines.Single(l => l.Id == receipt.LineId);
            if (line.YarnId is not null && receipt.PackageCount is null)
            {
                return ReferenceValidation.InvalidReference("lines", $"{i + 1}. satır: iplik teslimde bobin sayısı girilmelidir.");
            }

            if (line.YarnId is not null && decimal.Round(receipt.Quantity, 2) != receipt.Quantity)
            {
                return ReferenceValidation.InvalidReference("lines", $"{i + 1}. satır: iplik kilosu en fazla iki ondalık olabilir.");
            }
        }

        var year = request.ReceiptDate.Year;
        var yarnCount = request.Lines.Count(r => order.Lines.Single(l => l.Id == r.LineId).YarnId is not null);
        var materialCount = request.Lines.Count - yarnCount;
        var nextYarn = yarnCount > 0 ? await numbers.NextRangeAsync(DokumaYarnLotService.Series, year, yarnCount, cancellationToken) : 0;
        var nextMaterial = materialCount > 0 ? await numbers.NextRangeAsync(DokumaMaterialStockRules.LotSeries, year, materialCount, cancellationToken) : 0;
        var despatchNo = NullIfBlank(request.DespatchNo);

        foreach (var receipt in request.Lines)
        {
            var line = order.Lines.Single(l => l.Id == receipt.LineId);
            line.ReceivedQuantity += receipt.Quantity;
            if (line.YarnId is { } yarnId)
            {
                db.Set<DokumaYarnLot>().Add(new DokumaYarnLot
                {
                    LotNo = DokumaYarnLotService.LotNo(year, nextYarn++),
                    ReceiptDate = request.ReceiptDate,
                    YarnId = yarnId,
                    CustomerId = null,
                    SupplierId = order.SupplierId,
                    DespatchNo = despatchNo,
                    ProducerLotNo = NullIfBlank(receipt.LotNo),
                    PackageCount = receipt.PackageCount ?? 1,
                    Kilograms = receipt.Quantity,
                    Notes = NullIfBlank(receipt.Notes),
                    PurchaseOrderLineId = line.Id,
                });
            }
            else
            {
                var lot = new DokumaMaterialLot
                {
                    LotNo = DokumaMaterialStockRules.LotNo(year, nextMaterial++),
                    MaterialId = line.MaterialId!.Value,
                    ReceiptDate = request.ReceiptDate,
                    SupplierId = order.SupplierId,
                    SupplierLotNo = NullIfBlank(receipt.LotNo),
                    DespatchNo = despatchNo,
                    Quantity = receipt.Quantity,
                    RemainingQuantity = receipt.Quantity,
                    UnitPrice = line.UnitPrice,
                    Currency = order.Currency,
                    PurchaseOrderLineId = line.Id,
                    Notes = NullIfBlank(receipt.Notes),
                };
                lot.Movements.Add(new DokumaMaterialMovement
                {
                    MaterialId = lot.MaterialId,
                    Type = MaterialMovementType.Receipt,
                    MovementDate = request.ReceiptDate,
                    Quantity = receipt.Quantity,
                    Reference = order.OrderNo,
                });
                db.Set<DokumaMaterialLot>().Add(lot);
            }
        }

        order.RefreshReceiptStatus();
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    /// <summary>
    /// Tedarik önerileri. Malzeme: kritik stok − eldeki − siparişte. İplik: onaylı kendi malı siparişlerin henüz verilmemiş
    /// fireli ihtiyacı (sipariş iplik ihtiyacı hesabıyla) − işletme malı lotların kalanı − siparişte. Fason siparişin ipliği
    /// müşteriden geldiği için öneriye girmez.
    /// </summary>
    public async Task<IReadOnlyList<DokumaSupplySuggestionDto>> GetSuggestionsAsync(CancellationToken cancellationToken)
    {
        var suggestions = new List<DokumaSupplySuggestionDto>();

        // Malzemeler
        var materials = await db.Set<DokumaMaterial>().AsNoTracking()
            .Where(m => m.IsActive && m.MinimumStock != null)
            .OrderBy(m => m.Code)
            .Select(m => new { m.Id, m.Code, m.Name, m.Unit, Min = m.MinimumStock!.Value, m.PreferredSupplierId, Supplier = m.PreferredSupplier != null ? m.PreferredSupplier.Name : null })
            .ToListAsync(cancellationToken);
        var onHand = await DokumaMaterialStockRules.OnHandAsync(db, materials.Select(m => m.Id).ToList(), cancellationToken);
        var materialOnOrder = await DokumaPurchaseReceipts.OpenMaterialQuantitiesAsync(db, cancellationToken);
        foreach (var material in materials)
        {
            var have = onHand.GetValueOrDefault(material.Id);
            var coming = materialOnOrder.GetValueOrDefault(material.Id);
            var suggested = material.Min - have - coming;
            if (suggested > 0)
            {
                suggestions.Add(new DokumaSupplySuggestionDto(
                    PurchaseItemKind.Material, material.Id, material.Code, material.Name, material.Unit, material.Min, have, coming, suggested,
                    material.PreferredSupplierId, material.Supplier, []));
            }
        }

        // İplikler: her siparişin ihtiyaç hesabı aynı sahibin diğer onaylı siparişlerini de içerir; toplam kalan ihtiyaç
        // = siparişin kalanı + diğer siparişlerin kalanı.
        var ownOrders = await db.Set<DokumaOrder>().AsNoTracking()
            .Where(o => o.Status == OrderStatus.Confirmed && o.Ownership == OrderOwnership.OwnGoods)
            .OrderBy(o => o.OrderDate).ThenBy(o => o.Id)
            .Select(o => new { o.Id, o.OrderNo })
            .ToListAsync(cancellationToken);
        var yarnNeeds = new Dictionary<int, (DokumaYarnRequirementDto Row, decimal Total, List<string> Orders)>();
        foreach (var own in ownOrders)
        {
            var result = await orders.GetRequirementsAsync(own.Id, cancellationToken);
            if (!result.IsSuccess)
            {
                continue;
            }

            foreach (var row in result.Value.Yarns)
            {
                var remaining = Math.Max(0, row.RequiredKilograms - row.IssuedKilograms);
                var total = remaining + row.OtherOrdersKilograms;
                if (!yarnNeeds.TryGetValue(row.YarnId, out var need))
                {
                    need = (row, total, new List<string>());
                }

                if (remaining > 0)
                {
                    need.Orders.Add(own.OrderNo);
                }

                yarnNeeds[row.YarnId] = (need.Row, Math.Max(need.Total, total), need.Orders);
            }
        }

        if (yarnNeeds.Count > 0)
        {
            var yarnOnOrder = await DokumaPurchaseReceipts.OpenYarnQuantitiesAsync(db, cancellationToken);
            var yarnIds = yarnNeeds.Keys.ToList();
            var lastPurchase = await db.Set<DokumaPurchaseOrderLine>().AsNoTracking()
                .Where(l => l.YarnId != null && yarnIds.Contains(l.YarnId.Value) && l.Order.Status != PurchaseOrderStatus.Cancelled)
                .OrderByDescending(l => l.Order.OrderDate).ThenByDescending(l => l.Id)
                .Select(l => new { YarnId = l.YarnId!.Value, l.Order.SupplierId, l.Order.Supplier.Name })
                .ToListAsync(cancellationToken);
            var lastLot = await db.Set<DokumaYarnLot>().AsNoTracking()
                .Where(l => yarnIds.Contains(l.YarnId) && l.SupplierId != null && l.CustomerId == null)
                .OrderByDescending(l => l.ReceiptDate).ThenByDescending(l => l.Id)
                .Select(l => new { l.YarnId, SupplierId = l.SupplierId!.Value, l.Supplier!.Name })
                .ToListAsync(cancellationToken);

            foreach (var (yarnId, need) in yarnNeeds.OrderBy(n => n.Value.Row.YarnCode))
            {
                var have = need.Row.AvailableKilograms;
                var coming = yarnOnOrder.GetValueOrDefault(yarnId);
                var suggested = Math.Round(need.Total - Math.Max(have, 0) - coming, 2);
                if (suggested <= 0)
                {
                    continue;
                }

                var purchase = lastPurchase.FirstOrDefault(p => p.YarnId == yarnId);
                var lot = lastLot.FirstOrDefault(p => p.YarnId == yarnId);
                suggestions.Add(new DokumaSupplySuggestionDto(
                    PurchaseItemKind.Yarn, yarnId, need.Row.YarnCode, need.Row.YarnName, MaterialUnit.Kilogram, Math.Round(need.Total, 2), have, coming, suggested,
                    purchase?.SupplierId ?? lot?.SupplierId, purchase?.Name ?? lot?.Name, need.Orders.Distinct().ToList()));
            }
        }

        return suggestions;
    }

    /// <summary>Seçilen önerilerden tedarikçi başına bir taslak sipariş açar; fiyat o tedarikçiden son alınan fiyattır.</summary>
    public async Task<Result<IReadOnlyList<DokumaPurchaseDraftDto>>> CreateDraftsAsync(CreateDokumaPurchaseDraftsRequest request, CancellationToken cancellationToken)
    {
        var supplierIds = request.Lines.Select(l => l.SupplierId).Distinct().ToList();
        if (await ReferenceValidation.RequireSelectableAsync(db.Set<DokumaSupplier>(), supplierIds, new HashSet<int>(), "lines", "Tedarikçi", cancellationToken) is { } supplierError)
        {
            return supplierError;
        }

        var yarnIds = request.Lines.Where(l => l.Kind == PurchaseItemKind.Yarn).Select(l => l.ItemId).Distinct().ToList();
        var materialIds = request.Lines.Where(l => l.Kind == PurchaseItemKind.Material).Select(l => l.ItemId).Distinct().ToList();
        if (await ReferenceValidation.RequireSelectableAsync(db.Set<DokumaYarn>(), yarnIds, new HashSet<int>(), "lines", "İplik", cancellationToken) is { } yarnError)
        {
            return yarnError;
        }

        if (await ReferenceValidation.RequireSelectableAsync(db.Set<DokumaMaterial>(), materialIds, new HashSet<int>(), "lines", "Malzeme", cancellationToken) is { } materialError)
        {
            return materialError;
        }

        var suppliers = await db.Set<DokumaSupplier>().AsNoTracking().Where(s => supplierIds.Contains(s.Id)).ToDictionaryAsync(s => s.Id, cancellationToken);
        var units = await db.Set<DokumaMaterial>().AsNoTracking().Where(m => materialIds.Contains(m.Id)).ToDictionaryAsync(m => m.Id, m => m.Unit, cancellationToken);
        var lastPrices = await db.Set<DokumaPurchaseOrderLine>().AsNoTracking()
            .Where(l => supplierIds.Contains(l.Order.SupplierId) && l.UnitPrice != null && l.Order.Status != PurchaseOrderStatus.Cancelled
                && ((l.YarnId != null && yarnIds.Contains(l.YarnId.Value)) || (l.MaterialId != null && materialIds.Contains(l.MaterialId.Value))))
            .OrderByDescending(l => l.Order.OrderDate).ThenByDescending(l => l.Id)
            .Select(l => new { l.Order.SupplierId, l.YarnId, l.MaterialId, l.UnitPrice })
            .ToListAsync(cancellationToken);

        var orderDate = request.OrderDate ?? DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var created = new List<DokumaPurchaseOrder>();
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        foreach (var group in request.Lines.GroupBy(l => l.SupplierId))
        {
            var supplier = suppliers[group.Key];
            var sequence = await numbers.NextAsync(OrderSeries, orderDate.Year, cancellationToken);
            var order = new DokumaPurchaseOrder
            {
                OrderNo = $"DSA-{orderDate.Year}-{sequence:D5}",
                SupplierId = supplier.Id,
                OrderDate = orderDate,
                DueDate = supplier.LeadTimeDays is { } days ? orderDate.AddDays(days) : null,
                Currency = supplier.Currency,
                Notes = "Tedarik önerisinden oluşturuldu.",
            };

            var lineNo = 0;
            foreach (var item in group)
            {
                var isYarn = item.Kind == PurchaseItemKind.Yarn;
                var price = lastPrices.FirstOrDefault(p => p.SupplierId == supplier.Id && (isYarn ? p.YarnId == item.ItemId : p.MaterialId == item.ItemId))?.UnitPrice;
                order.Lines.Add(new DokumaPurchaseOrderLine
                {
                    Sequence = ++lineNo,
                    YarnId = isYarn ? item.ItemId : null,
                    MaterialId = isYarn ? null : item.ItemId,
                    Quantity = Math.Round(item.Quantity, isYarn ? 2 : 3),
                    Unit = isYarn ? MaterialUnit.Kilogram : units[item.ItemId],
                    UnitPrice = price,
                });
            }

            db.Set<DokumaPurchaseOrder>().Add(order);
            created.Add(order);
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return created.Select(o => new DokumaPurchaseDraftDto(o.Id, o.OrderNo, suppliers[o.SupplierId].Name, o.Lines.Count)).ToList();
    }
}
