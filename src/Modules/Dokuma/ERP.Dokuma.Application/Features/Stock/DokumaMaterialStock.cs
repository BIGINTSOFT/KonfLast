using System.Linq.Expressions;
using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Core.Application.Features.MasterData;
using ERP.Dokuma.Application.Features.Purchasing;
using ERP.Dokuma.Domain.Entities;
using ERP.Dokuma.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using static ERP.Core.Application.Features.MasterData.MasterDataRules;

namespace ERP.Dokuma.Application.Features.Stock;

// ---------------------------------------------------------------------------------------------------------------------
// Malzeme kartı
// ---------------------------------------------------------------------------------------------------------------------

public sealed record DokumaMaterialDto(
    int Id,
    string Code,
    string Name,
    MaterialType Type,
    MaterialUnit Unit,
    decimal? MinimumStock,
    int? PreferredSupplierId,
    string? PreferredSupplierName,
    string? Notes,
    bool IsActive);

/// <param name="MinimumStock">Kritik stok; boşsa malzeme kritik listesine ve tedarik önerisine girmez.</param>
/// <param name="PreferredSupplierId">Tedarik önerisinden açılan siparişin tedarikçisi.</param>
public sealed record DokumaMaterialRequest(
    string Code,
    string Name,
    MaterialType Type = MaterialType.SizingChemical,
    MaterialUnit Unit = MaterialUnit.Kilogram,
    decimal? MinimumStock = null,
    int? PreferredSupplierId = null,
    string? Notes = null,
    bool IsActive = true);

public sealed class DokumaMaterialRequestValidator : AbstractValidator<DokumaMaterialRequest>
{
    public DokumaMaterialRequestValidator()
    {
        RuleFor(x => x.Code).ValidCode();
        RuleFor(x => x.Name).ValidName(200);
        RuleFor(x => x.Type).IsInEnum().WithMessage("Geçerli bir malzeme türü seçin.");
        RuleFor(x => x.Unit).IsInEnum().WithMessage("Geçerli bir birim seçin.");
        RuleFor(x => x.MinimumStock)
            .InclusiveBetween(0, 9_999_999).WithMessage("Kritik stok 0 ile 9.999.999 arasında olmalıdır.")
            .PrecisionScale(14, 3, true).WithMessage("Kritik stok en fazla üç ondalık olabilir.")
            .When(x => x.MinimumStock is not null);
        RuleFor(x => x.Notes).OptionalText(500);
    }
}

/// <summary>Malzeme kartları. Lotu olan malzemenin birimi değiştirilemez.</summary>
public sealed class DokumaMaterialService(IAppDbContext db) : MasterDataService<DokumaMaterial, DokumaMaterialDto, DokumaMaterialRequest>(db)
{
    protected override DbSet<DokumaMaterial> Set => Db.Set<DokumaMaterial>();
    protected override string Label => "Malzeme";
    protected override string CodeField => "code";

    protected override string CodeOf(DokumaMaterialRequest request) => request.Code;
    protected override Expression<Func<DokumaMaterial, bool>> WithCode(string code) => e => e.Code == code;
    protected override Expression<Func<DokumaMaterial, bool>> Matches(string term) => e => e.Code.Contains(term) || e.Name.Contains(term);
    protected override IQueryable<DokumaMaterial> Order(IQueryable<DokumaMaterial> query) => query.OrderBy(e => e.Code);

    protected override Expression<Func<DokumaMaterial, DokumaMaterialDto>> ToDto => e => new DokumaMaterialDto(
        e.Id, e.Code, e.Name, e.Type, e.Unit, e.MinimumStock, e.PreferredSupplierId, e.PreferredSupplier != null ? e.PreferredSupplier.Name : null, e.Notes, e.IsActive);

    // Seçimde birim de görünür: sipariş ve çıkış miktarı bu birimle girilir.
    protected override Expression<Func<DokumaMaterial, LookupDto>> ToLookup => e => new LookupDto(e.Id, e.Code, e.Name)
    {
        Detail = e.Unit == MaterialUnit.Kilogram ? "kg" : e.Unit == MaterialUnit.Piece ? "adet" : e.Unit == MaterialUnit.Liter ? "lt" : "m",
    };

    protected override void Apply(DokumaMaterialRequest request, DokumaMaterial entity, string code)
    {
        entity.Code = code;
        entity.Name = request.Name.Trim();
        entity.Type = request.Type;
        entity.Unit = request.Unit;
        entity.MinimumStock = request.MinimumStock;
        entity.PreferredSupplierId = request.PreferredSupplierId;
        entity.Notes = NullIfBlank(request.Notes);
        entity.IsActive = request.IsActive;
    }

    protected override async Task<Error?> ValidateAsync(DokumaMaterialRequest request, DokumaMaterial? existing, CancellationToken cancellationToken)
    {
        if (await RequireSelectableAsync(Db.Set<DokumaSupplier>(), request.PreferredSupplierId, existing?.PreferredSupplierId, "preferredSupplierId", "Tedarikçi", cancellationToken) is { } error)
        {
            return error;
        }

        if (existing is not null && existing.Unit != request.Unit
            && (await Db.Set<DokumaMaterialLot>().AnyAsync(l => l.MaterialId == existing.Id, cancellationToken)
                || await Db.Set<DokumaPurchaseOrderLine>().AnyAsync(l => l.MaterialId == existing.Id, cancellationToken)))
        {
            return ReferenceValidation.InvalidReference("unit", "Lotu ya da satın alma siparişi olan malzemenin birimi değiştirilemez.");
        }

        return null;
    }

    protected override async Task<string?> FindUsageAsync(int id, CancellationToken cancellationToken)
    {
        if (await Db.Set<DokumaMaterialLot>().AnyAsync(l => l.MaterialId == id, cancellationToken))
        {
            return "malzeme lotları";
        }

        if (await Db.Set<DokumaSizingRecipeLine>().AnyAsync(l => l.MaterialId == id && !l.Recipe.IsDeleted, cancellationToken))
        {
            return "haşıl reçeteleri";
        }

        return await Db.Set<DokumaPurchaseOrderLine>().AnyAsync(l => l.MaterialId == id, cancellationToken) ? "satın alma siparişleri" : null;
    }
}

// ---------------------------------------------------------------------------------------------------------------------
// Malzeme stoğu
// ---------------------------------------------------------------------------------------------------------------------

public sealed class DokumaMaterialStockQuery
{
    /// <summary>Malzeme kodu ya da adında aranır.</summary>
    public string? Search { get; set; }

    /// <summary>Yalnızca kritik stok altındakiler.</summary>
    public bool CriticalOnly { get; set; }

    /// <summary>Pasif malzemeler de listelensin (stoğu olan pasif malzeme her zaman listelenir).</summary>
    public bool IncludeInactive { get; set; }
}

public sealed class DokumaMaterialLotQuery : PageRequest
{
    /// <summary>Lot no, tedarikçi lot no, irsaliye ya da malzemede aranır.</summary>
    public string? Search { get; set; }

    public int? MaterialId { get; set; }

    /// <summary>Yalnızca kalanı olan lotlar.</summary>
    public bool OpenOnly { get; set; }
}

public sealed class DokumaMaterialMovementQuery : PageRequest
{
    /// <summary>Malzeme, lot no, referans, bölüm ya da nedende aranır.</summary>
    public string? Search { get; set; }

    public int? MaterialId { get; set; }
    public int? LoomId { get; set; }
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
}

/// <param name="Quantity">Lotların kalanı toplamı.</param>
/// <param name="OnOrderQuantity">Açık satın alma siparişlerinde (taslak dahil) teslim beklenen.</param>
/// <param name="IsCritical">Kritik stok tanımlı ve stok bunun altında.</param>
public sealed record DokumaMaterialStockRowDto(
    int MaterialId,
    string Code,
    string Name,
    MaterialType Type,
    MaterialUnit Unit,
    decimal? MinimumStock,
    decimal Quantity,
    decimal OnOrderQuantity,
    int LotCount,
    bool IsCritical,
    string? PreferredSupplierName,
    bool IsActive);

/// <param name="IsUsed">Lottan çıkış, tüketim ya da düzeltme yapıldı; malzemesi ve miktarı değiştirilemez, silinemez.</param>
public sealed record DokumaMaterialLotDto(
    int Id,
    string LotNo,
    int MaterialId,
    string MaterialCode,
    string MaterialName,
    MaterialUnit Unit,
    DateOnly ReceiptDate,
    int? SupplierId,
    string? SupplierName,
    string? SupplierLotNo,
    string? DespatchNo,
    decimal Quantity,
    decimal RemainingQuantity,
    decimal? UnitPrice,
    string Currency,
    int? PurchaseOrderId,
    string? PurchaseOrderNo,
    string? Notes,
    bool IsUsed);

public sealed record DokumaMaterialMovementDto(
    int Id,
    DateOnly MovementDate,
    MaterialMovementType Type,
    int MaterialId,
    string MaterialCode,
    string MaterialName,
    MaterialUnit Unit,
    string LotNo,
    decimal Quantity,
    string? LoomCode,
    string? Department,
    string? Reason,
    int? BeamId,
    string? BeamNo,
    string? Reference,
    DateTime CreatedAt,
    string? CreatedBy);

public sealed record DokumaMaterialLotRequest(
    int MaterialId,
    DateOnly ReceiptDate,
    decimal Quantity,
    int? SupplierId = null,
    string? SupplierLotNo = null,
    string? DespatchNo = null,
    decimal? UnitPrice = null,
    string? Currency = null,
    string? Notes = null);

/// <param name="LoomId">Yedek parçanın takıldığı tezgâh (isteğe bağlı).</param>
/// <param name="Department">Çıkışın yapıldığı bölüm (isteğe bağlı).</param>
public sealed record DokumaMaterialIssueRequest(int MaterialId, decimal Quantity, DateOnly Date, string Reason, int? LoomId = null, string? Department = null);

public sealed record DokumaMaterialLotAdjustRequest(decimal CountedQuantity, string Reason, DateOnly? Date = null);

/// <param name="CountedQuantity">Depoda sayılan toplam miktar; fark lotlara yansıtılır.</param>
public sealed record DokumaMaterialCountRequest(int MaterialId, decimal CountedQuantity, DateOnly Date, string? Reason = null);

public sealed class DokumaMaterialLotRequestValidator : AbstractValidator<DokumaMaterialLotRequest>
{
    public DokumaMaterialLotRequestValidator()
    {
        RuleFor(x => x.MaterialId).GreaterThan(0).WithMessage("Malzeme seçilmelidir.");
        RuleFor(x => x.ReceiptDate).NotEmpty().WithMessage("Giriş tarihi zorunludur.");
        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Miktar sıfırdan büyük olmalıdır.")
            .LessThanOrEqualTo(9_999_999).WithMessage("Miktar çok büyük.")
            .PrecisionScale(14, 3, true).WithMessage("Miktar en fazla üç ondalık olabilir.");
        RuleFor(x => x.SupplierLotNo).OptionalText(50);
        RuleFor(x => x.DespatchNo).OptionalText(50);
        RuleFor(x => x.UnitPrice).InclusiveBetween(0, 99_999_999).WithMessage("Birim fiyat geçersiz.").When(x => x.UnitPrice is not null);
        RuleFor(x => x.Currency).Matches(@"^\s*[A-Za-z]{3}\s*$").WithMessage("Para birimi üç harfli ISO kodu olmalıdır.").When(x => !string.IsNullOrWhiteSpace(x.Currency));
        RuleFor(x => x.Notes).OptionalText(500);
    }
}

public sealed class DokumaMaterialIssueRequestValidator : AbstractValidator<DokumaMaterialIssueRequest>
{
    public DokumaMaterialIssueRequestValidator()
    {
        RuleFor(x => x.MaterialId).GreaterThan(0).WithMessage("Malzeme seçilmelidir.");
        RuleFor(x => x.Date).NotEmpty().WithMessage("Çıkış tarihi zorunludur.");
        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Miktar sıfırdan büyük olmalıdır.")
            .LessThanOrEqualTo(9_999_999).WithMessage("Miktar çok büyük.")
            .PrecisionScale(14, 3, true).WithMessage("Miktar en fazla üç ondalık olabilir.");
        RuleFor(x => x.Reason).NotEmpty().WithMessage("Çıkış nedeni zorunludur.").MaximumLength(200).WithMessage("Neden en fazla 200 karakter olabilir.");
        RuleFor(x => x.Department).OptionalText(100);
    }
}

public sealed class DokumaMaterialLotAdjustRequestValidator : AbstractValidator<DokumaMaterialLotAdjustRequest>
{
    public DokumaMaterialLotAdjustRequestValidator()
    {
        RuleFor(x => x.CountedQuantity)
            .InclusiveBetween(0, 9_999_999).WithMessage("Sayılan miktar geçersiz.")
            .PrecisionScale(14, 3, true).WithMessage("Miktar en fazla üç ondalık olabilir.");
        RuleFor(x => x.Reason).NotEmpty().WithMessage("Düzeltme gerekçesi zorunludur.").MaximumLength(200).WithMessage("Gerekçe en fazla 200 karakter olabilir.");
    }
}

public sealed class DokumaMaterialCountRequestValidator : AbstractValidator<DokumaMaterialCountRequest>
{
    public DokumaMaterialCountRequestValidator()
    {
        RuleFor(x => x.MaterialId).GreaterThan(0).WithMessage("Malzeme seçilmelidir.");
        RuleFor(x => x.Date).NotEmpty().WithMessage("Sayım tarihi zorunludur.");
        RuleFor(x => x.CountedQuantity)
            .InclusiveBetween(0, 9_999_999).WithMessage("Sayılan miktar geçersiz.")
            .PrecisionScale(14, 3, true).WithMessage("Miktar en fazla üç ondalık olabilir.");
        RuleFor(x => x.Reason).OptionalText(200);
    }
}

/// <summary>Malzeme lotlarından düşüm: tezgâha çıkış, haşıl tüketimi ve sayım farkı aynı kuralla yapılır.</summary>
public static class DokumaMaterialStockRules
{
    /// <summary>Lot kalanları aynı anda iki istekle düşülmesin diye alınan kilit.</summary>
    public const string MaterialLock = "DokumaMaterialLots";

    public const string LotSeries = "DokumaMaterialLot";

    public static readonly TimeSpan LockTimeout = TimeSpan.FromSeconds(10);

    public static readonly Error Busy = new(ErrorCodes.ResourceBusy, "Malzeme stoğu şu an başka bir işlemle değişiyor; tekrar deneyin.", ErrorType.Conflict);

    public static string LotNo(int year, int sequence) => $"ML-{year}-{sequence:D5}";

    /// <summary>
    /// Miktarı en eski lottan başlayarak düşer; her lot için <paramref name="movement"/> ile bir hareket eklenir (miktarı eksi).
    /// Stok yetmezse hiçbir şey düşülmez, hata döner. Çağıran açık işlem içinde kilidi almış olmalı ve kaydetmelidir.
    /// </summary>
    public static async Task<Error?> AllocateAsync(
        IAppDbContext db, int materialId, decimal quantity, Func<DokumaMaterialMovement> movement, CancellationToken cancellationToken)
    {
        var lots = await db.Set<DokumaMaterialLot>()
            .Where(l => l.MaterialId == materialId && l.RemainingQuantity > 0)
            .OrderBy(l => l.ReceiptDate).ThenBy(l => l.Id)
            .ToListAsync(cancellationToken);
        var available = lots.Sum(l => l.RemainingQuantity);
        if (available < quantity)
        {
            var material = await db.Set<DokumaMaterial>().IgnoreQueryFilters().Where(m => m.Id == materialId).Select(m => m.Code).FirstOrDefaultAsync(cancellationToken);
            return new Error(
                ErrorCodes.InsufficientStock,
                $"{material ?? "Malzeme"} stoğu yetmiyor: {available:0.###} var, {quantity:0.###} isteniyor. Önce giriş yapın.",
                ErrorType.Conflict);
        }

        var open = quantity;
        foreach (var lot in lots)
        {
            if (open <= 0)
            {
                break;
            }

            var taken = Math.Min(open, lot.RemainingQuantity);
            lot.RemainingQuantity -= taken;
            open -= taken;
            var row = movement();
            row.MaterialId = materialId;
            row.LotId = lot.Id;
            row.Quantity = -taken;
            db.Set<DokumaMaterialMovement>().Add(row);
        }

        return null;
    }

    /// <summary>Malzeme başına lot kalanı toplamı.</summary>
    public static async Task<Dictionary<int, decimal>> OnHandAsync(IAppDbContext db, IReadOnlyCollection<int> materialIds, CancellationToken cancellationToken) =>
        await db.Set<DokumaMaterialLot>().AsNoTracking()
            .Where(l => materialIds.Contains(l.MaterialId) && l.RemainingQuantity > 0)
            .GroupBy(l => l.MaterialId)
            .Select(g => new { g.Key, Total = g.Sum(l => l.RemainingQuantity) })
            .ToDictionaryAsync(x => x.Key, x => x.Total, cancellationToken);
}

public interface IDokumaMaterialStockService
{
    Task<IReadOnlyList<DokumaMaterialStockRowDto>> GetStockAsync(DokumaMaterialStockQuery query, CancellationToken cancellationToken);
    Task<PagedResult<DokumaMaterialLotDto>> GetLotsAsync(DokumaMaterialLotQuery query, CancellationToken cancellationToken);
    Task<PagedResult<DokumaMaterialMovementDto>> GetMovementsAsync(DokumaMaterialMovementQuery query, CancellationToken cancellationToken);
    Task<Result<DokumaMaterialLotDto>> CreateLotAsync(DokumaMaterialLotRequest request, CancellationToken cancellationToken);
    Task<Result<DokumaMaterialLotDto>> UpdateLotAsync(int id, DokumaMaterialLotRequest request, CancellationToken cancellationToken);
    Task<Result> DeleteLotAsync(int id, CancellationToken cancellationToken);
    Task<Result<DokumaMaterialLotDto>> AdjustLotAsync(int id, DokumaMaterialLotAdjustRequest request, CancellationToken cancellationToken);
    Task<Result> IssueAsync(DokumaMaterialIssueRequest request, CancellationToken cancellationToken);
    Task<Result> CountAsync(DokumaMaterialCountRequest request, CancellationToken cancellationToken);
}

/// <summary>
/// Malzeme stoğu: lot girişi (elle ya da satın alma teslimi), tezgâha / bölüme çıkış, lot düzeltmesi ve malzeme sayımı.
/// Çıkışlar en eski lottan düşülür; her değişiklik hareket olarak kaydedilir.
/// </summary>
public sealed class DokumaMaterialStockService(IAppDbContext db, IDocumentNumberGenerator numbers, IAppLock appLock) : IDokumaMaterialStockService
{
    private const string InvalidState = "DOKUMA_MATERIAL_LOT_IN_USE";
    private static readonly Error NotFound = new(ErrorCodes.NotFound, "Malzeme lotu bulunamadı.", ErrorType.NotFound);

    public async Task<IReadOnlyList<DokumaMaterialStockRowDto>> GetStockAsync(DokumaMaterialStockQuery query, CancellationToken cancellationToken)
    {
        var materials = db.Set<DokumaMaterial>().AsNoTracking();
        if (NullIfBlank(query.Search) is { } term)
        {
            materials = materials.Where(m => m.Code.Contains(term) || m.Name.Contains(term));
        }

        var rows = await materials
            .OrderBy(m => m.Code)
            .Select(m => new
            {
                m.Id,
                m.Code,
                m.Name,
                m.Type,
                m.Unit,
                m.MinimumStock,
                m.IsActive,
                Supplier = m.PreferredSupplier != null ? m.PreferredSupplier.Name : null,
            })
            .ToListAsync(cancellationToken);

        var lots = await db.Set<DokumaMaterialLot>().AsNoTracking()
            .Where(l => l.RemainingQuantity > 0)
            .GroupBy(l => l.MaterialId)
            .Select(g => new { g.Key, Total = g.Sum(l => l.RemainingQuantity), Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, cancellationToken);
        var onOrder = await DokumaPurchaseReceipts.OpenMaterialQuantitiesAsync(db, cancellationToken);

        return rows
            .Select(m =>
            {
                var stock = lots.GetValueOrDefault(m.Id);
                var quantity = stock?.Total ?? 0;
                return new DokumaMaterialStockRowDto(
                    m.Id, m.Code, m.Name, m.Type, m.Unit, m.MinimumStock, quantity, onOrder.GetValueOrDefault(m.Id), stock?.Count ?? 0,
                    m.MinimumStock is { } min && quantity < min, m.Supplier, m.IsActive);
            })
            .Where(r => (query.IncludeInactive || r.IsActive || r.Quantity > 0) && (!query.CriticalOnly || (r.IsCritical && r.IsActive)))
            .ToList();
    }

    public Task<PagedResult<DokumaMaterialLotDto>> GetLotsAsync(DokumaMaterialLotQuery query, CancellationToken cancellationToken)
    {
        var lots = db.Set<DokumaMaterialLot>().AsNoTracking();
        if (NullIfBlank(query.Search) is { } term)
        {
            lots = lots.Where(l => l.LotNo.Contains(term)
                || l.Material.Code.Contains(term)
                || l.Material.Name.Contains(term)
                || (l.SupplierLotNo != null && l.SupplierLotNo.Contains(term))
                || (l.DespatchNo != null && l.DespatchNo.Contains(term))
                || (l.Supplier != null && l.Supplier.Name.Contains(term)));
        }

        if (query.MaterialId is { } materialId)
        {
            lots = lots.Where(l => l.MaterialId == materialId);
        }

        if (query.OpenOnly)
        {
            lots = lots.Where(l => l.RemainingQuantity > 0);
        }

        return Project(lots.OrderByDescending(l => l.ReceiptDate).ThenByDescending(l => l.Id)).ToPagedResultAsync(query, cancellationToken);
    }

    public Task<PagedResult<DokumaMaterialMovementDto>> GetMovementsAsync(DokumaMaterialMovementQuery query, CancellationToken cancellationToken)
    {
        var movements = db.Set<DokumaMaterialMovement>().AsNoTracking();
        if (NullIfBlank(query.Search) is { } term)
        {
            movements = movements.Where(m => m.Material.Code.Contains(term)
                || m.Material.Name.Contains(term)
                || m.Lot.LotNo.Contains(term)
                || (m.Reference != null && m.Reference.Contains(term))
                || (m.Department != null && m.Department.Contains(term))
                || (m.Reason != null && m.Reason.Contains(term)));
        }

        if (query.MaterialId is { } materialId)
        {
            movements = movements.Where(m => m.MaterialId == materialId);
        }

        if (query.LoomId is { } loomId)
        {
            movements = movements.Where(m => m.LoomId == loomId);
        }

        if (query.From is { } from)
        {
            movements = movements.Where(m => m.MovementDate >= from);
        }

        if (query.To is { } to)
        {
            movements = movements.Where(m => m.MovementDate <= to);
        }

        return movements
            .OrderByDescending(m => m.MovementDate).ThenByDescending(m => m.Id)
            .Select(m => new DokumaMaterialMovementDto(
                m.Id,
                m.MovementDate,
                m.Type,
                m.MaterialId,
                m.Material.Code,
                m.Material.Name,
                m.Material.Unit,
                m.Lot.LotNo,
                m.Quantity,
                m.Loom != null ? m.Loom.Code : null,
                m.Department,
                m.Reason,
                m.BeamId,
                m.Beam != null ? m.Beam.BeamNo : null,
                m.Reference,
                m.CreatedAt,
                m.CreatedBy))
            .ToPagedResultAsync(query, cancellationToken);
    }

    public async Task<Result<DokumaMaterialLotDto>> CreateLotAsync(DokumaMaterialLotRequest request, CancellationToken cancellationToken)
    {
        if (await ValidateReferencesAsync(request, null, cancellationToken) is { } error)
        {
            return error;
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var year = request.ReceiptDate.Year;
        var sequence = await numbers.NextAsync(DokumaMaterialStockRules.LotSeries, year, cancellationToken);
        var lot = new DokumaMaterialLot { LotNo = DokumaMaterialStockRules.LotNo(year, sequence), RemainingQuantity = request.Quantity };
        Apply(lot, request);
        lot.Movements.Add(new DokumaMaterialMovement
        {
            MaterialId = request.MaterialId,
            Type = MaterialMovementType.Receipt,
            MovementDate = request.ReceiptDate,
            Quantity = request.Quantity,
            Reference = lot.DespatchNo,
        });
        db.Set<DokumaMaterialLot>().Add(lot);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetLotAsync(lot.Id, cancellationToken);
    }

    public async Task<Result<DokumaMaterialLotDto>> UpdateLotAsync(int id, DokumaMaterialLotRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (!await appLock.TryAcquireAsync(DokumaMaterialStockRules.MaterialLock, DokumaMaterialStockRules.LockTimeout, cancellationToken))
        {
            return DokumaMaterialStockRules.Busy;
        }

        var lot = await db.Set<DokumaMaterialLot>().SingleOrDefaultAsync(l => l.Id == id, cancellationToken);
        if (lot is null)
        {
            return NotFound;
        }

        var used = await IsUsedAsync(id, cancellationToken);
        if (used && (lot.MaterialId != request.MaterialId || lot.Quantity != request.Quantity))
        {
            return new Error(InvalidState, "Çıkış ya da düzeltme yapılmış lotun malzemesi ve miktarı değiştirilemez; lot düzeltmesi yapın.", ErrorType.Conflict);
        }

        if (lot.PurchaseOrderLineId is not null && lot.MaterialId != request.MaterialId)
        {
            return new Error(InvalidState, "Satın alma siparişinden gelen lotun malzemesi değiştirilemez.", ErrorType.Conflict);
        }

        if (await ValidateReferencesAsync(request, lot, cancellationToken) is { } error)
        {
            return error;
        }

        if (!used)
        {
            lot.RemainingQuantity = request.Quantity;
            var receipt = await db.Set<DokumaMaterialMovement>().SingleOrDefaultAsync(m => m.LotId == id && m.Type == MaterialMovementType.Receipt, cancellationToken);
            if (receipt is not null)
            {
                receipt.MaterialId = request.MaterialId;
                receipt.Quantity = request.Quantity;
                receipt.MovementDate = request.ReceiptDate;
            }
        }

        Apply(lot, request);
        await db.SaveChangesAsync(cancellationToken);
        await DokumaPurchaseReceipts.RefreshLineAsync(db, lot.PurchaseOrderLineId, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetLotAsync(id, cancellationToken);
    }

    public async Task<Result> DeleteLotAsync(int id, CancellationToken cancellationToken)
    {
        var lot = await db.Set<DokumaMaterialLot>().SingleOrDefaultAsync(l => l.Id == id, cancellationToken);
        if (lot is null)
        {
            return NotFound;
        }

        if (await IsUsedAsync(id, cancellationToken))
        {
            return new Error(ErrorCodes.RecordInUse, "Çıkış ya da düzeltme yapılmış lot silinemez.", ErrorType.Conflict);
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var purchaseLine = lot.PurchaseOrderLineId;
        db.Set<DokumaMaterialMovement>().RemoveRange(await db.Set<DokumaMaterialMovement>().Where(m => m.LotId == id).ToListAsync(cancellationToken));
        db.Set<DokumaMaterialLot>().Remove(lot);
        await db.SaveChangesAsync(cancellationToken);
        await DokumaPurchaseReceipts.RefreshLineAsync(db, purchaseLine, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<DokumaMaterialLotDto>> AdjustLotAsync(int id, DokumaMaterialLotAdjustRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (!await appLock.TryAcquireAsync(DokumaMaterialStockRules.MaterialLock, DokumaMaterialStockRules.LockTimeout, cancellationToken))
        {
            return DokumaMaterialStockRules.Busy;
        }

        var lot = await db.Set<DokumaMaterialLot>().SingleOrDefaultAsync(l => l.Id == id, cancellationToken);
        if (lot is null)
        {
            return NotFound;
        }

        var difference = request.CountedQuantity - lot.RemainingQuantity;
        if (difference != 0)
        {
            lot.RemainingQuantity = request.CountedQuantity;
            db.Set<DokumaMaterialMovement>().Add(new DokumaMaterialMovement
            {
                MaterialId = lot.MaterialId,
                LotId = id,
                Type = MaterialMovementType.Adjustment,
                MovementDate = request.Date ?? DateOnly.FromDateTime(DateTime.Today),
                Quantity = difference,
                Reason = request.Reason.Trim(),
            });
            await db.SaveChangesAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return await GetLotAsync(id, cancellationToken);
    }

    public async Task<Result> IssueAsync(DokumaMaterialIssueRequest request, CancellationToken cancellationToken)
    {
        if (!await db.Set<DokumaMaterial>().AnyAsync(m => m.Id == request.MaterialId, cancellationToken))
        {
            return ReferenceValidation.InvalidReference("materialId", "Malzeme bulunamadı.");
        }

        if (await ReferenceValidation.RequireSelectableAsync(db.Set<DokumaLoom>(), request.LoomId, null, "loomId", "Tezgâh", cancellationToken) is { } loomError)
        {
            return loomError;
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (!await appLock.TryAcquireAsync(DokumaMaterialStockRules.MaterialLock, DokumaMaterialStockRules.LockTimeout, cancellationToken))
        {
            return DokumaMaterialStockRules.Busy;
        }

        var reason = request.Reason.Trim();
        var department = NullIfBlank(request.Department);
        var error = await DokumaMaterialStockRules.AllocateAsync(db, request.MaterialId, request.Quantity, () => new DokumaMaterialMovement
        {
            Type = MaterialMovementType.Issue,
            MovementDate = request.Date,
            LoomId = request.LoomId,
            Department = department,
            Reason = reason,
        }, cancellationToken);
        if (error is not null)
        {
            return error;
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Result.Success();
    }

    /// <summary>
    /// Sayım: sayılan toplam ile lotların kalanı karşılaştırılır. Eksik en eski lottan düşülür; fazla en son lota eklenir,
    /// hiç lot yoksa sayım fazlası için yeni lot açılır.
    /// </summary>
    public async Task<Result> CountAsync(DokumaMaterialCountRequest request, CancellationToken cancellationToken)
    {
        if (!await db.Set<DokumaMaterial>().AnyAsync(m => m.Id == request.MaterialId, cancellationToken))
        {
            return ReferenceValidation.InvalidReference("materialId", "Malzeme bulunamadı.");
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (!await appLock.TryAcquireAsync(DokumaMaterialStockRules.MaterialLock, DokumaMaterialStockRules.LockTimeout, cancellationToken))
        {
            return DokumaMaterialStockRules.Busy;
        }

        var current = await db.Set<DokumaMaterialLot>().Where(l => l.MaterialId == request.MaterialId).SumAsync(l => (decimal?)l.RemainingQuantity, cancellationToken) ?? 0;
        var difference = request.CountedQuantity - current;
        if (difference == 0)
        {
            return new Error(ErrorCodes.StockNoChange, "Sayılan miktar stokla aynı; fark yok.", ErrorType.Conflict);
        }

        var reason = NullIfBlank(request.Reason) ?? "Sayım";
        if (difference < 0)
        {
            var error = await DokumaMaterialStockRules.AllocateAsync(db, request.MaterialId, -difference, () => new DokumaMaterialMovement
            {
                Type = MaterialMovementType.Count,
                MovementDate = request.Date,
                Reason = reason,
            }, cancellationToken);
            if (error is not null)
            {
                return error;
            }
        }
        else
        {
            var lot = await db.Set<DokumaMaterialLot>()
                .Where(l => l.MaterialId == request.MaterialId)
                .OrderByDescending(l => l.ReceiptDate).ThenByDescending(l => l.Id)
                .FirstOrDefaultAsync(cancellationToken);
            if (lot is null)
            {
                var year = request.Date.Year;
                var sequence = await numbers.NextAsync(DokumaMaterialStockRules.LotSeries, year, cancellationToken);
                lot = new DokumaMaterialLot
                {
                    LotNo = DokumaMaterialStockRules.LotNo(year, sequence),
                    MaterialId = request.MaterialId,
                    ReceiptDate = request.Date,
                    Quantity = difference,
                    RemainingQuantity = 0,
                    Notes = "Sayım fazlası",
                };
                db.Set<DokumaMaterialLot>().Add(lot);
                await db.SaveChangesAsync(cancellationToken);
            }

            lot.RemainingQuantity += difference;
            db.Set<DokumaMaterialMovement>().Add(new DokumaMaterialMovement
            {
                MaterialId = request.MaterialId,
                LotId = lot.Id,
                Type = MaterialMovementType.Count,
                MovementDate = request.Date,
                Quantity = difference,
                Reason = reason,
            });
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Result.Success();
    }

    private async Task<Error?> ValidateReferencesAsync(DokumaMaterialLotRequest request, DokumaMaterialLot? existing, CancellationToken cancellationToken)
    {
        if (await ReferenceValidation.RequireSelectableAsync(db.Set<DokumaMaterial>(), request.MaterialId, existing?.MaterialId, "materialId", "Malzeme", cancellationToken) is { } materialError)
        {
            return materialError;
        }

        if (await ReferenceValidation.RequireSelectableAsync(db.Set<DokumaSupplier>(), request.SupplierId, existing?.SupplierId, "supplierId", "Tedarikçi", cancellationToken) is { } supplierError)
        {
            return supplierError;
        }

        return await CurrencyRules.RequireActiveAsync(db, request.Currency, existing?.Currency, "currency", cancellationToken);
    }

    private Task<bool> IsUsedAsync(int id, CancellationToken cancellationToken) =>
        db.Set<DokumaMaterialMovement>().AnyAsync(m => m.LotId == id && m.Type != MaterialMovementType.Receipt, cancellationToken);

    private async Task<Result<DokumaMaterialLotDto>> GetLotAsync(int id, CancellationToken cancellationToken) =>
        await Project(db.Set<DokumaMaterialLot>().AsNoTracking().Where(l => l.Id == id)).SingleAsync(cancellationToken);

    private static IQueryable<DokumaMaterialLotDto> Project(IQueryable<DokumaMaterialLot> lots) => lots.Select(l => new DokumaMaterialLotDto(
        l.Id,
        l.LotNo,
        l.MaterialId,
        l.Material.Code,
        l.Material.Name,
        l.Material.Unit,
        l.ReceiptDate,
        l.SupplierId,
        l.Supplier != null ? l.Supplier.Name : null,
        l.SupplierLotNo,
        l.DespatchNo,
        l.Quantity,
        l.RemainingQuantity,
        l.UnitPrice,
        l.Currency,
        l.PurchaseOrderLine != null ? (int?)l.PurchaseOrderLine.OrderId : null,
        l.PurchaseOrderLine != null ? l.PurchaseOrderLine.Order.OrderNo : null,
        l.Notes,
        l.Movements.Any(m => m.Type != MaterialMovementType.Receipt)));

    private static void Apply(DokumaMaterialLot lot, DokumaMaterialLotRequest request)
    {
        lot.MaterialId = request.MaterialId;
        lot.ReceiptDate = request.ReceiptDate;
        lot.Quantity = request.Quantity;
        lot.SupplierId = request.SupplierId;
        lot.SupplierLotNo = NullIfBlank(request.SupplierLotNo);
        lot.DespatchNo = NullIfBlank(request.DespatchNo);
        lot.UnitPrice = request.UnitPrice;
        lot.Currency = NullIfBlank(request.Currency)?.ToUpperInvariant() ?? "TRY";
        lot.Notes = NullIfBlank(request.Notes);
    }
}
