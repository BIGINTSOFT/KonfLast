using ERP.Konfeksiyon.Application.Abstractions;
using System.ComponentModel.DataAnnotations;
using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Core.Domain.Entities;
using ERP.Konfeksiyon.Domain.Entities;
using ERP.Core.Domain.Enums;
using ERP.Konfeksiyon.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ERP.Konfeksiyon.Application.Features.Warehousing;

/// <param name="LineCount">Fişteki satır sayısı.</param>
/// <param name="CountedLineCount">Miktarı girilmiş satır sayısı.</param>
/// <param name="DifferenceLineCount">Sayılan miktarı beklenenden farklı satır sayısı.</param>
public sealed record StockCountListItemDto(
    int Id,
    string CountNo,
    int WarehouseId,
    string WarehouseCode,
    string WarehouseName,
    string? LocationCode,
    StockCountStatus Status,
    int LineCount,
    int CountedLineCount,
    int DifferenceLineCount,
    DateTime CreatedAt,
    string? CreatedBy,
    DateTime? PostedAt);

/// <param name="CountedQuantity">Sayılan miktar; henüz sayılmadıysa boş.</param>
/// <param name="Difference">Sayılan − beklenen; sayılmadıysa boş.</param>
public sealed record StockCountLineDto(
    int Id,
    int LocationId,
    string LocationCode,
    int StockItemId,
    string ItemCode,
    string ItemName,
    StockUnit Unit,
    int? LotId,
    string? LotNo,
    string? BatchNo,
    decimal ExpectedQuantity,
    decimal? CountedQuantity,
    decimal? Difference);

/// <param name="LocationId">Yalnızca bu adres sayılıyorsa; boşsa deponun tamamı.</param>
/// <param name="RowVersion">Kayıt, onay ve iptalde geri gönderilir (eşzamanlılık kontrolü).</param>
public sealed record StockCountDto(
    int Id,
    string CountNo,
    int WarehouseId,
    string WarehouseCode,
    string WarehouseName,
    int? LocationId,
    string? LocationCode,
    StockCountStatus Status,
    string? Note,
    bool IsOpen,
    string RowVersion,
    DateTime CreatedAt,
    string? CreatedBy,
    DateTime? PostedAt,
    string? PostedBy,
    IReadOnlyList<StockCountLineDto> Lines);

public sealed class StockCountListQuery : PageRequest
{
    public int? WarehouseId { get; set; }
    public StockCountStatus? Status { get; set; }

    /// <summary>Sayım no içinde aranır.</summary>
    public string? Search { get; set; }
}

/// <param name="LocationId">Yalnızca bu adres sayılacaksa; verilmezse deponun tamamı.</param>
public sealed record OpenStockCountRequest(int WarehouseId, int? LocationId = null, string? Note = null);

/// <param name="CountedQuantity">Sayılan miktar; boş gönderilirse satır "sayılmadı" olur.</param>
public sealed record StockCountLineEntry(int LineId, decimal? CountedQuantity);

public sealed record SaveStockCountLinesRequest(string RowVersion, IReadOnlyList<StockCountLineEntry> Lines);

/// <summary>Sayımda bulunan ama fişte olmayan stok için satır ekler.</summary>
/// <param name="LocationId">Bulunduğu adres; verilmezse fişin adresi, o da yoksa deponun genel adresi.</param>
/// <param name="LotId">Takipli kartta bulunan top (zorunlu).</param>
public sealed record AddStockCountLineRequest(
    string RowVersion, int StockItemId, decimal CountedQuantity, int? LocationId = null, int? LotId = null);

public sealed record StockCountActionRequest(string RowVersion);

public sealed class StockCountOptions
{
    public const string SectionName = "StockCount";

    /// <summary>Sayım numarası öneki: {önek}-{yıl}-{sıra}, örn. SY-2026-00003.</summary>
    [Required, RegularExpression("^[A-Z0-9]{1,10}$")]
    public string NumberPrefix { get; set; } = "SY";

    [Range(3, 9)]
    public int NumberDigits { get; set; } = 5;
}

public sealed class OpenStockCountRequestValidator : AbstractValidator<OpenStockCountRequest>
{
    public OpenStockCountRequestValidator()
    {
        RuleFor(x => x.WarehouseId).GreaterThan(0).WithMessage("Depo seçilmelidir.");
        RuleFor(x => x.Note).MaximumLength(300).WithMessage("Not en fazla 300 karakter olabilir.");
    }
}

public sealed class SaveStockCountLinesRequestValidator : AbstractValidator<SaveStockCountLinesRequest>
{
    public SaveStockCountLinesRequestValidator()
    {
        RuleFor(x => x.RowVersion).NotEmpty().WithMessage("Fiş sürümü (rowVersion) zorunludur.");
        RuleFor(x => x.Lines).NotNull().WithMessage("Satır listesi zorunludur.");
        When(x => x.Lines is not null, () =>
        {
            RuleFor(x => x.Lines)
                .Must(lines => lines.All(l => l.CountedQuantity is null || StockCountRules.IsValidQuantity(l.CountedQuantity.Value)))
                .WithMessage(StockCountRules.QuantityMessage)
                .Must(lines => lines.Select(l => l.LineId).Distinct().Count() == lines.Count)
                .WithMessage("Aynı satır birden fazla kez gönderilemez.");
        });
    }
}

public sealed class AddStockCountLineRequestValidator : AbstractValidator<AddStockCountLineRequest>
{
    public AddStockCountLineRequestValidator()
    {
        RuleFor(x => x.RowVersion).NotEmpty().WithMessage("Fiş sürümü (rowVersion) zorunludur.");
        RuleFor(x => x.StockItemId).GreaterThan(0).WithMessage("Stok kartı seçilmelidir.");
        RuleFor(x => x.CountedQuantity).Must(StockCountRules.IsValidQuantity).WithMessage(StockCountRules.QuantityMessage);
    }
}

public sealed class StockCountActionRequestValidator : AbstractValidator<StockCountActionRequest>
{
    public StockCountActionRequestValidator()
    {
        RuleFor(x => x.RowVersion).NotEmpty().WithMessage("Fiş sürümü (rowVersion) zorunludur.");
    }
}

public static class StockCountRules
{
    public const string QuantityMessage = "Sayılan miktar negatif olamaz ve en fazla 3 ondalık basamak içerebilir.";

    public static bool IsValidQuantity(decimal quantity) =>
        quantity >= 0 && quantity <= StockRules.MaxQuantity && decimal.Round(quantity, 3) == quantity;
}

public interface IStockCountService
{
    /// <summary>Sayım fişleri, en yeniden eskiye.</summary>
    Task<PagedResult<StockCountListItemDto>> GetListAsync(StockCountListQuery query, CancellationToken cancellationToken);

    Task<Result<StockCountDto>> GetAsync(int id, CancellationToken cancellationToken);

    /// <summary>Sayım açar: kapsamdaki stok fişe beklenen miktar olarak yazılır ve adresler kilitlenir.</summary>
    Task<Result<StockCountDto>> OpenAsync(OpenStockCountRequest request, CancellationToken cancellationToken);

    /// <summary>Sayılan miktarları kaydeder; stok değişmez.</summary>
    Task<Result<StockCountDto>> SaveLinesAsync(int id, SaveStockCountLinesRequest request, CancellationToken cancellationToken);

    /// <summary>Fişte olmayan, sayımda bulunan stok için satır ekler.</summary>
    Task<Result<StockCountDto>> AddLineAsync(int id, AddStockCountLineRequest request, CancellationToken cancellationToken);

    /// <summary>Fişi onaylar: sayılan satırların farkı stok düzeltmesi olarak yazılır, kilit kalkar.</summary>
    Task<Result<StockCountDto>> PostAsync(int id, StockCountActionRequest request, CancellationToken cancellationToken);

    /// <summary>Fişi iptal eder: stok değişmez, kilit kalkar.</summary>
    Task<Result<StockCountDto>> CancelAsync(int id, StockCountActionRequest request, CancellationToken cancellationToken);
}

/// <summary>
/// Sayım fişleri. Açık bir fiş, kapsamındaki adreslerde stok hareketini engeller (kontrol stok servisindedir);
/// böylece sayılan miktar ile kayıtlar aynı ana aittir. Onayda fark, kilit altında okunan güncel bakiyeye göre
/// hesaplanır.
/// </summary>
public sealed class StockCountService(
    IKonfeksiyonDbContext db,
    IAppLock appLock,
    IStockLedger stockLedger,
    IDocumentNumberGenerator numberGenerator,
    ICurrentUser currentUser,
    TimeProvider timeProvider,
    Microsoft.Extensions.Options.IOptions<StockCountOptions> options) : IStockCountService
{
    private const string SystemUser = "system";
    private static readonly TimeSpan LockTimeout = TimeSpan.FromSeconds(10);

    private static readonly Error NotFound = new(ErrorCodes.NotFound, "Sayım fişi bulunamadı.", ErrorType.NotFound);

    private static readonly Error ConcurrencyConflict = new(
        ErrorCodes.ConcurrencyConflict,
        "Fiş siz açtıktan sonra başka bir kullanıcı tarafından değiştirildi. Sayfayı yenileyip tekrar deneyin.",
        ErrorType.Conflict);

    private static readonly Error NotOpen = new(
        ErrorCodes.StockCountNotOpen, "Yalnızca açık sayım fişi değiştirilebilir.");

    private readonly StockCountOptions _options = options.Value;

    public Task<PagedResult<StockCountListItemDto>> GetListAsync(StockCountListQuery query, CancellationToken cancellationToken)
    {
        var counts = db.StockCounts.AsNoTracking().IgnoreQueryFilters();

        if (query.WarehouseId is { } warehouseId)
        {
            counts = counts.Where(c => c.WarehouseId == warehouseId);
        }

        if (query.Status is { } status)
        {
            counts = counts.Where(c => c.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            counts = counts.Where(c => c.CountNo.Contains(search));
        }

        return counts
            .OrderByDescending(c => c.Id)
            .Select(c => new StockCountListItemDto(
                c.Id,
                c.CountNo,
                c.WarehouseId,
                c.Warehouse.Code,
                c.Warehouse.Name,
                c.Location == null ? null : c.Location.Code,
                c.Status,
                c.Lines.Count,
                c.Lines.Count(l => l.CountedQuantity != null),
                c.Lines.Count(l => l.CountedQuantity != null && l.CountedQuantity != l.ExpectedQuantity),
                c.CreatedAt,
                c.CreatedBy,
                c.PostedAt))
            .ToPagedResultAsync(query, cancellationToken);
    }

    public async Task<Result<StockCountDto>> GetAsync(int id, CancellationToken cancellationToken)
    {
        var header = await db.StockCounts
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(c => c.Id == id)
            .Select(c => new
            {
                c.Id, c.CountNo, c.WarehouseId, WarehouseCode = c.Warehouse.Code, WarehouseName = c.Warehouse.Name, c.LocationId,
                LocationCode = c.Location == null ? null : c.Location.Code, c.Status, c.Note, c.RowVersion, c.CreatedAt,
                c.CreatedBy, c.PostedAt, c.PostedBy,
            })
            .SingleOrDefaultAsync(cancellationToken);
        if (header is null)
        {
            return NotFound;
        }

        var lines = await db.StockCountLines
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(l => l.StockCountId == id)
            .OrderByDescending(l => l.Location.IsDefault).ThenBy(l => l.Location.Code)
            .ThenBy(l => l.StockItem.Code).ThenBy(l => l.LotId)
            .Select(l => new StockCountLineDto(
                l.Id, l.LocationId, l.Location.Code, l.StockItemId, l.StockItem.Code, l.StockItem.Name, l.StockItem.Unit,
                l.LotId, l.Lot == null ? null : l.Lot.LotNo, l.Lot == null ? null : l.Lot.BatchNo, l.ExpectedQuantity,
                l.CountedQuantity, l.CountedQuantity - l.ExpectedQuantity))
            .ToListAsync(cancellationToken);

        return new StockCountDto(
            header.Id, header.CountNo, header.WarehouseId, header.WarehouseCode, header.WarehouseName, header.LocationId,
            header.LocationCode, header.Status, header.Note, header.Status == StockCountStatus.Open,
            RowVersionToken.From(header.RowVersion), header.CreatedAt, header.CreatedBy, header.PostedAt, header.PostedBy, lines);
    }

    public async Task<Result<StockCountDto>> OpenAsync(OpenStockCountRequest request, CancellationToken cancellationToken)
    {
        // Pasif depo da sayılabilir; depo var olmalıdır.
        if (!await db.Warehouses.AnyAsync(w => w.Id == request.WarehouseId, cancellationToken))
        {
            return ReferenceValidation.InvalidReference("warehouseId", "Depo bulunamadı.");
        }

        if (request.LocationId is { } locationId
            && !await db.WarehouseLocations.AnyAsync(
                l => l.Id == locationId && l.WarehouseId == request.WarehouseId, cancellationToken))
        {
            return ReferenceValidation.InvalidReference("locationId", "Adres bulunamadı ya da seçilen depoya ait değil.");
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        // Aynı depoda eşzamanlı iki sayım açılışı kapsam çakışmasını birlikte kaçırmasın.
        if (!await appLock.TryAcquireAsync($"stock-count:{request.WarehouseId}", LockTimeout, cancellationToken))
        {
            return new Error(
                ErrorCodes.ResourceBusy, "Bu depo için şu an başka bir sayım işlemi yapılıyor. Lütfen tekrar deneyin.",
                ErrorType.Conflict);
        }

        var overlapping = await db.StockCounts
            .Where(c => c.WarehouseId == request.WarehouseId && c.Status == StockCountStatus.Open
                && (c.LocationId == null || request.LocationId == null || c.LocationId == request.LocationId))
            .Select(c => c.CountNo)
            .FirstOrDefaultAsync(cancellationToken);
        if (overlapping is not null)
        {
            return new Error(
                ErrorCodes.StockCountInProgress,
                $"Bu depoda aynı kapsamı sayan açık bir fiş var ({overlapping}). Önce o fiş onaylanmalı ya da iptal edilmeli.",
                ErrorType.Conflict);
        }

        var year = timeProvider.GetUtcNow().Year;
        var sequence = await numberGenerator.NextAsync(DocumentSeries.StockCount, year, cancellationToken);
        var count = new StockCount
        {
            CountNo = $"{_options.NumberPrefix}-{year}-{sequence.ToString().PadLeft(_options.NumberDigits, '0')}",
            WarehouseId = request.WarehouseId,
            LocationId = request.LocationId,
            Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
        };

        // Kayıtlarda stoğu görünen her satır fişe girer; sıfır bakiyeli satırlar sayımda bulunursa eklenir.
        var stocks = await db.Stocks
            .AsNoTracking()
            .Where(s => s.WarehouseId == request.WarehouseId && s.Quantity > 0
                && (request.LocationId == null || s.LocationId == request.LocationId))
            .Select(s => new { s.LocationId, s.StockItemId, s.LotId, s.Quantity })
            .ToListAsync(cancellationToken);
        foreach (var stock in stocks)
        {
            count.Lines.Add(new StockCountLine
            {
                LocationId = stock.LocationId, StockItemId = stock.StockItemId, LotId = stock.LotId,
                ExpectedQuantity = stock.Quantity,
            });
        }

        db.StockCounts.Add(count);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetAsync(count.Id, cancellationToken);
    }

    public async Task<Result<StockCountDto>> SaveLinesAsync(
        int id, SaveStockCountLinesRequest request, CancellationToken cancellationToken)
    {
        var loaded = await LoadOpenAsync(id, request.RowVersion, cancellationToken);
        if (!loaded.IsSuccess)
        {
            return loaded.Error!;
        }

        var count = loaded.Value;
        var units = await LoadUnitsAsync(count.Lines.Select(l => l.StockItemId), cancellationToken);
        foreach (var entry in request.Lines)
        {
            var line = count.Lines.SingleOrDefault(l => l.Id == entry.LineId);
            if (line is null)
            {
                return ReferenceValidation.InvalidReference("lines", "Satır bu sayım fişine ait değil.");
            }

            if (entry.CountedQuantity is { } quantity && !units[line.StockItemId].Accepts(quantity))
            {
                var message = $"{units[line.StockItemId].Code}: adetle izlenen stok kartında miktar tam sayı olmalıdır.";
                return new Error(
                    ErrorCodes.StockQuantityInvalid, message, ErrorType.Validation,
                    new Dictionary<string, string[]> { ["lines"] = [message] });
            }

            line.CountedQuantity = entry.CountedQuantity;
        }

        return await SaveAsync(count, cancellationToken);
    }

    public async Task<Result<StockCountDto>> AddLineAsync(
        int id, AddStockCountLineRequest request, CancellationToken cancellationToken)
    {
        var loaded = await LoadOpenAsync(id, request.RowVersion, cancellationToken);
        if (!loaded.IsSuccess)
        {
            return loaded.Error!;
        }

        var count = loaded.Value;
        var item = await db.StockItems.AsNoTracking().SingleOrDefaultAsync(i => i.Id == request.StockItemId, cancellationToken);
        if (item is null)
        {
            return ReferenceValidation.InvalidReference("stockItemId", "Stok kartı bulunamadı.");
        }

        if (!item.Accepts(request.CountedQuantity))
        {
            const string message = "Adetle izlenen stok kartında miktar tam sayı olmalıdır.";
            return new Error(
                ErrorCodes.StockQuantityInvalid, message, ErrorType.Validation,
                new Dictionary<string, string[]> { ["countedQuantity"] = [message] });
        }

        // Adres verilmezse fişin adresi, o da yoksa deponun genel adresi.
        var locationId = request.LocationId ?? count.LocationId ?? await db.WarehouseLocations
            .Where(l => l.WarehouseId == count.WarehouseId && l.IsDefault)
            .Select(l => l.Id)
            .SingleAsync(cancellationToken);
        if (!count.Covers(locationId)
            || !await db.WarehouseLocations.AnyAsync(l => l.Id == locationId && l.WarehouseId == count.WarehouseId, cancellationToken))
        {
            return ReferenceValidation.InvalidReference("locationId", "Adres bu sayımın kapsamında değil.");
        }

        if (item.IsLotTracked != (request.LotId is not null))
        {
            var message = item.IsLotTracked
                ? "Top / parti takipli stok kartında bulunan top seçilmelidir."
                : "Stok kartı top / parti takipli değil; top seçilemez.";
            return new Error(
                item.IsLotTracked ? ErrorCodes.StockLotRequired : ErrorCodes.StockLotNotAllowed, message, ErrorType.Validation,
                new Dictionary<string, string[]> { ["lotId"] = [message] });
        }

        if (request.LotId is { } lotId
            && !await db.StockLots.AnyAsync(l => l.Id == lotId && l.StockItemId == item.Id, cancellationToken))
        {
            return ReferenceValidation.InvalidReference("lotId", "Top bulunamadı ya da bu stok kartına ait değil.");
        }

        if (count.Lines.Any(l => l.LocationId == locationId && l.StockItemId == item.Id && l.LotId == request.LotId))
        {
            return new Error(
                ErrorCodes.StockCountLineExists, "Bu kart (ve top) bu adres için fişte zaten var; miktarını o satıra girin.",
                ErrorType.Conflict);
        }

        var expected = await db.Stocks
            .Where(s => s.LocationId == locationId && s.StockItemId == item.Id && s.LotId == request.LotId)
            .Select(s => (decimal?)s.Quantity)
            .SingleOrDefaultAsync(cancellationToken) ?? 0;
        count.Lines.Add(new StockCountLine
        {
            LocationId = locationId, StockItemId = item.Id, LotId = request.LotId, ExpectedQuantity = expected,
            CountedQuantity = request.CountedQuantity,
        });

        return await SaveAsync(count, cancellationToken);
    }

    public async Task<Result<StockCountDto>> PostAsync(int id, StockCountActionRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var loaded = await LoadOpenAsync(id, request.RowVersion, cancellationToken);
        if (!loaded.IsSuccess)
        {
            return loaded.Error!;
        }

        var count = loaded.Value;
        var counted = count.Lines.Where(l => l.CountedQuantity is not null).ToList();
        if (counted.Count == 0)
        {
            return new Error(ErrorCodes.StockCountEmpty, "Hiçbir satır sayılmamış; onaylanacak bir şey yok.");
        }

        // Farklar stoğa işlenir; sayılmayan satırlara dokunulmaz.
        var stockError = await stockLedger.PostCountAsync(
            count.WarehouseId, count.CountNo,
            counted.Select(l => (l.LocationId, l.StockItemId, l.LotId, l.CountedQuantity!.Value)).ToList(), cancellationToken);
        if (stockError is not null)
        {
            return stockError;
        }

        count.Post(timeProvider.GetUtcNow().UtcDateTime, currentUser.UserName ?? SystemUser);
        var result = await SaveAsync(count, cancellationToken);
        if (result.IsSuccess)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        return result;
    }

    public async Task<Result<StockCountDto>> CancelAsync(
        int id, StockCountActionRequest request, CancellationToken cancellationToken)
    {
        var loaded = await LoadOpenAsync(id, request.RowVersion, cancellationToken);
        if (!loaded.IsSuccess)
        {
            return loaded.Error!;
        }

        loaded.Value.Cancel();
        return await SaveAsync(loaded.Value, cancellationToken);
    }

    private async Task<Result<StockCount>> LoadOpenAsync(int id, string rowVersion, CancellationToken cancellationToken)
    {
        var count = await db.StockCounts.Include(c => c.Lines).SingleOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (count is null)
        {
            return NotFound;
        }

        if (RowVersionToken.Check(count.RowVersion, rowVersion, ConcurrencyConflict) is { } versionError)
        {
            return versionError;
        }

        return count.IsOpen ? count : NotOpen;
    }

    private async Task<Dictionary<int, StockItem>> LoadUnitsAsync(IEnumerable<int> itemIds, CancellationToken cancellationToken)
    {
        var ids = itemIds.Distinct().ToList();
        return await db.StockItems
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(i => ids.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, cancellationToken);
    }

    private async Task<Result<StockCountDto>> SaveAsync(StockCount count, CancellationToken cancellationToken)
    {
        // Yalnızca satırlar değişse de başlık güncellenir: rowversion ilerler.
        db.Entry(count).State = db.Entry(count).State == EntityState.Unchanged ? EntityState.Modified : db.Entry(count).State;
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ConcurrencyConflict;
        }

        return await GetAsync(count.Id, cancellationToken);
    }
}
