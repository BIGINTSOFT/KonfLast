using System.Linq.Expressions;
using ERP.Boyahane.Domain.Entities;
using ERP.Boyahane.Domain.Enums;
using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Core.Application.Features.MasterData;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using static ERP.Core.Application.Features.MasterData.MasterDataRules;

namespace ERP.Boyahane.Application.Features.Warehousing;

public sealed record BoyahaneWarehouseDto(int Id, string Code, string Name, BoyahaneWarehouseType Type, bool IsActive);

public sealed record BoyahaneWarehouseRequest(string Code, string Name, BoyahaneWarehouseType Type = BoyahaneWarehouseType.RawFabric, bool IsActive = true);

public sealed class BoyahaneWarehouseRequestValidator : AbstractValidator<BoyahaneWarehouseRequest>
{
    public BoyahaneWarehouseRequestValidator()
    {
        RuleFor(x => x.Code).ValidCode();
        RuleFor(x => x.Name).ValidName(100);
        RuleFor(x => x.Type).IsInEnum().WithMessage("Geçerli bir depo türü seçin.");
    }
}

public sealed class BoyahaneWarehouseService(IAppDbContext db) : MasterDataService<BoyahaneWarehouse, BoyahaneWarehouseDto, BoyahaneWarehouseRequest>(db)
{
    protected override DbSet<BoyahaneWarehouse> Set => Db.Set<BoyahaneWarehouse>();
    protected override string Label => "Depo";
    protected override string CodeField => "code";

    protected override string CodeOf(BoyahaneWarehouseRequest request) => request.Code;
    protected override Expression<Func<BoyahaneWarehouse, bool>> WithCode(string code) => e => e.Code == code;
    protected override Expression<Func<BoyahaneWarehouse, bool>> Matches(string term) => e => e.Code.Contains(term) || e.Name.Contains(term);
    protected override IQueryable<BoyahaneWarehouse> Order(IQueryable<BoyahaneWarehouse> query) => query.OrderBy(e => e.Code);
    protected override Expression<Func<BoyahaneWarehouse, BoyahaneWarehouseDto>> ToDto => e => new BoyahaneWarehouseDto(e.Id, e.Code, e.Name, e.Type, e.IsActive);
    protected override Expression<Func<BoyahaneWarehouse, LookupDto>> ToLookup => e => new LookupDto(e.Id, e.Code, e.Name);

    protected override void Apply(BoyahaneWarehouseRequest request, BoyahaneWarehouse entity, string code)
    {
        entity.Code = code;
        entity.Name = request.Name.Trim();
        entity.Type = request.Type;
        entity.IsActive = request.IsActive;
        if (entity.Id == 0 && entity.Locations.Count == 0)
        {
            entity.Locations.Add(BoyahaneWarehouseLocation.CreateDefault());
        }
    }

    protected override async Task<string?> FindUsageAsync(int id, CancellationToken cancellationToken) =>
        await BoyahaneWarehouseRules.LocationInUseAsync(Db, l => l.WarehouseId == id, cancellationToken) ? "depodaki mal ya da adres hareketleri" : null;
}

public sealed record BoyahaneWarehouseLocationDto(int Id, int WarehouseId, string WarehouseCode, string WarehouseName, string Code, string Zone, string? Rack, string? Bin, string? Description, bool IsDefault, bool IsActive);

public sealed record BoyahaneWarehouseLocationRequest(int WarehouseId, string Zone, string? Rack = null, string? Bin = null, string? Description = null, bool IsActive = true);

public sealed class BoyahaneWarehouseLocationRequestValidator : AbstractValidator<BoyahaneWarehouseLocationRequest>
{
    private const string Segment = @"^\s*[A-Za-z0-9ÇĞİÖŞÜçğıöşü]{1,10}\s*$";

    public BoyahaneWarehouseLocationRequestValidator()
    {
        RuleFor(x => x.WarehouseId).GreaterThan(0).WithMessage("Depo seçilmelidir.");
        RuleFor(x => x.Zone).NotEmpty().WithMessage("Bölge zorunludur.").Matches(Segment).WithMessage("Bölge en fazla 10 harf ya da rakam olabilir.");
        RuleFor(x => x.Rack).Matches(Segment).WithMessage("Raf en fazla 10 harf ya da rakam olabilir.").When(x => !string.IsNullOrWhiteSpace(x.Rack));
        RuleFor(x => x.Bin).Matches(Segment).WithMessage("Göz en fazla 10 harf ya da rakam olabilir.").When(x => !string.IsNullOrWhiteSpace(x.Bin));
        RuleFor(x => x.Bin).Empty().WithMessage("Raf girilmeden göz verilemez.").When(x => string.IsNullOrWhiteSpace(x.Rack));
        RuleFor(x => x.Description).OptionalText(100);
    }
}

public sealed class BoyahaneWarehouseLocationService(IAppDbContext db) : MasterDataService<BoyahaneWarehouseLocation, BoyahaneWarehouseLocationDto, BoyahaneWarehouseLocationRequest>(db)
{
    protected override DbSet<BoyahaneWarehouseLocation> Set => Db.Set<BoyahaneWarehouseLocation>();
    protected override string Label => "Depo adresi";
    protected override string CodeField => "zone";

    protected override string CodeOf(BoyahaneWarehouseLocationRequest request) =>
        BoyahaneWarehouseLocation.ComposeCode(Upper(request.Zone)!, Upper(request.Rack), Upper(request.Bin));

    protected override Expression<Func<BoyahaneWarehouseLocation, bool>> WithCode(string code) => e => e.Code == code;
    protected override Expression<Func<BoyahaneWarehouseLocation, bool>> WithCode(string code, BoyahaneWarehouseLocationRequest request) =>
        e => e.Code == code && e.WarehouseId == request.WarehouseId;

    protected override Expression<Func<BoyahaneWarehouseLocation, bool>> Matches(string term) =>
        e => e.Code.Contains(term) || (e.Description != null && e.Description.Contains(term)) || e.Warehouse.Code.Contains(term) || e.Warehouse.Name.Contains(term);

    protected override IQueryable<BoyahaneWarehouseLocation> Order(IQueryable<BoyahaneWarehouseLocation> query) =>
        query.OrderBy(e => e.Warehouse.Code).ThenByDescending(e => e.IsDefault).ThenBy(e => e.Code);

    protected override Expression<Func<BoyahaneWarehouseLocation, BoyahaneWarehouseLocationDto>> ToDto => e =>
        new BoyahaneWarehouseLocationDto(e.Id, e.WarehouseId, e.Warehouse.Code, e.Warehouse.Name, e.Code, e.Zone, e.Rack, e.Bin, e.Description, e.IsDefault, e.IsActive);

    protected override Expression<Func<BoyahaneWarehouseLocation, LookupDto>> ToLookup => e => new LookupDto(e.Id, e.Code, e.Warehouse.Name);

    protected override void Apply(BoyahaneWarehouseLocationRequest request, BoyahaneWarehouseLocation entity, string code)
    {
        entity.WarehouseId = request.WarehouseId;
        entity.SetAddress(Upper(request.Zone)!, Upper(request.Rack), Upper(request.Bin));
        entity.Description = NullIfBlank(request.Description);
        entity.IsActive = request.IsActive;
    }

    protected override async Task<Error?> ValidateAsync(BoyahaneWarehouseLocationRequest request, BoyahaneWarehouseLocation? existing, CancellationToken cancellationToken)
    {
        if (existing is null)
        {
            return await ReferenceValidation.RequireSelectableAsync(Db.Set<BoyahaneWarehouse>(), request.WarehouseId, null, "warehouseId", "Depo", cancellationToken);
        }

        if (existing.WarehouseId != request.WarehouseId)
        {
            return ReferenceValidation.InvalidReference("warehouseId", "Adresin deposu değiştirilemez; diğer depoda yeni adres açın.");
        }

        if (existing.IsDefault && (!request.IsActive || CodeOf(request) != existing.Code))
        {
            return new Error(ErrorCodes.RecordInUse, "Deponun genel adresi değiştirilemez, pasif yapılamaz.", ErrorType.Conflict);
        }

        return null;
    }

    protected override async Task<string?> FindUsageAsync(int id, CancellationToken cancellationToken) =>
        await Db.Set<BoyahaneWarehouseLocation>().AnyAsync(l => l.Id == id && l.IsDefault, cancellationToken) ? "deponun genel adresi"
        : await BoyahaneWarehouseRules.LocationInUseAsync(Db, l => l.Id == id, cancellationToken) ? "depodaki mal ya da adres hareketleri"
        : null;

    private static string? Upper(string? value) => NullIfBlank(value)?.ToUpper(System.Globalization.CultureInfo.GetCultureInfo("tr-TR"));
}

/// <summary>Adres seçilmeyen malın düşeceği yer ve adres doğrulaması; kabul, satın alma ve çeki akışları kullanır.</summary>
public static class BoyahaneWarehouseRules
{
    /// <summary>Türündeki ilk etkin deponun genel adresi; böyle depo yoksa boş.</summary>
    public static Task<int?> DefaultLocationIdAsync(IAppDbContext db, BoyahaneWarehouseType type, CancellationToken cancellationToken) =>
        db.Set<BoyahaneWarehouseLocation>()
            .Where(l => l.IsDefault && l.IsActive && l.Warehouse.IsActive && l.Warehouse.Type == type)
            .OrderBy(l => l.WarehouseId)
            .Select(l => (int?)l.Id)
            .FirstOrDefaultAsync(cancellationToken);

    /// <summary>Depodaki top: makineye girmemiş ve iade edilmemiş.</summary>
    public static IQueryable<BoyahaneFabricRoll> RollsInWarehouse(IAppDbContext db) => db.Set<BoyahaneFabricRoll>()
        .Where(r => !db.Set<BoyahaneBatchItem>().Any(i => i.RollId == r.Id && i.IsActive && (i.Batch.Status == BatchStatus.OnMachine || i.Batch.Status == BatchStatus.Completed)))
        .Where(r => !db.Set<BoyahaneGoodsReturnItem>().Any(i => i.RollId == r.Id && i.Return.Status == GoodsReturnStatus.Confirmed));

    /// <summary>Depodaki iplik lotu: makineye girmemiş ve iade edilmemiş.</summary>
    public static IQueryable<BoyahaneYarnLot> YarnLotsInWarehouse(IAppDbContext db) => db.Set<BoyahaneYarnLot>()
        .Where(y => !db.Set<BoyahaneBatchItem>().Any(i => i.YarnLotId == y.Id && i.IsActive && (i.Batch.Status == BatchStatus.OnMachine || i.Batch.Status == BatchStatus.Completed)))
        .Where(y => !db.Set<BoyahaneGoodsReturnItem>().Any(i => i.YarnLotId == y.Id && i.Return.Status == GoodsReturnStatus.Confirmed));

    /// <summary>Mamul depodaki paket: onaylı listede, onaylı irsaliyeyle sevk edilmemiş.</summary>
    public static IQueryable<BoyahanePackingItem> PackagesInWarehouse(IAppDbContext db) => db.Set<BoyahanePackingItem>()
        .Where(p => p.IsActive && p.PackingList.Status == PackingListStatus.Confirmed
            && (p.PackingList.Despatch == null || p.PackingList.Despatch.Status != DespatchStatus.Confirmed));

    public static Task<Error?> ValidateLocationAsync(IAppDbContext db, int? locationId, CancellationToken cancellationToken) =>
        ReferenceValidation.RequireSelectableAsync(db.Set<BoyahaneWarehouseLocation>(), locationId, null, "locationId", "Depo adresi", cancellationToken);

    public static async Task<bool> LocationInUseAsync(IAppDbContext db, Expression<Func<BoyahaneWarehouseLocation, bool>> selector, CancellationToken cancellationToken)
    {
        var ids = db.Set<BoyahaneWarehouseLocation>().IgnoreQueryFilters().Where(selector).Select(l => l.Id);
        return await db.Set<BoyahaneFabricRoll>().AnyAsync(x => x.LocationId != null && ids.Contains(x.LocationId.Value), cancellationToken)
            || await db.Set<BoyahaneYarnLot>().AnyAsync(x => x.LocationId != null && ids.Contains(x.LocationId.Value), cancellationToken)
            || await db.Set<BoyahaneChemicalLot>().AnyAsync(x => x.LocationId != null && ids.Contains(x.LocationId.Value), cancellationToken)
            || await db.Set<BoyahanePackingItem>().AnyAsync(x => x.LocationId != null && ids.Contains(x.LocationId.Value), cancellationToken)
            || await db.Set<BoyahaneLocationMovement>().AnyAsync(x => ids.Contains(x.ToLocationId) || (x.FromLocationId != null && ids.Contains(x.FromLocationId.Value)), cancellationToken);
    }
}

public sealed class BoyahaneWarehouseGoodsQuery : PageRequest
{
    public WarehouseGoodsType Type { get; set; } = WarehouseGoodsType.FabricRoll;
    public int? WarehouseId { get; set; }
    public int? LocationId { get; set; }

    /// <summary>Numara, kart ya da malın sahibi.</summary>
    public string? Search { get; set; }
}

public sealed record BoyahaneWarehouseGoodsDto(
    int Id,
    string GoodsNo,
    string CardCode,
    string CardName,
    string? ColorName,
    string? OwnerName,
    string? OrderNo,
    decimal Kilograms,
    decimal? Meters,
    string? Condition,
    DateOnly? ExpiryDate,
    string? WarehouseName,
    string? LocationCode);

public sealed record BoyahaneLocationOptionDto(int Id, string Code, string? Description, bool IsActive);

public sealed record BoyahaneLocationMovementDto(
    long Id,
    WarehouseGoodsType GoodsType,
    string GoodsNo,
    LocationMovementType Type,
    string? FromWarehouse,
    string? FromLocation,
    string ToWarehouse,
    string ToLocation,
    string? Note,
    DateTime CreatedAt,
    string? CreatedBy);

public sealed class BoyahaneLocationMovementQuery : PageRequest
{
    public string? Search { get; set; }
}

public sealed record BoyahaneMoveGoodsRequest(WarehouseGoodsType Type, IReadOnlyList<int> Ids, int ToLocationId, string? Note = null);

public sealed class BoyahaneMoveGoodsRequestValidator : AbstractValidator<BoyahaneMoveGoodsRequest>
{
    public BoyahaneMoveGoodsRequestValidator()
    {
        RuleFor(x => x.Type).IsInEnum().WithMessage("Geçerli bir mal türü seçin.");
        RuleFor(x => x.Ids).NotEmpty().WithMessage("En az bir mal seçilmelidir.").Must(ids => ids is null || ids.Count <= 500).WithMessage("Bir seferde en fazla 500 mal taşınabilir.");
        RuleFor(x => x.ToLocationId).GreaterThan(0).WithMessage("Hedef adres seçilmelidir.");
        RuleFor(x => x.Note).OptionalText(200);
    }
}

public interface IBoyahaneWarehousingService
{
    Task<PagedResult<BoyahaneWarehouseGoodsDto>> GetGoodsAsync(BoyahaneWarehouseGoodsQuery query, CancellationToken cancellationToken);
    Task<IReadOnlyList<BoyahaneLocationOptionDto>> GetLocationsAsync(int warehouseId, CancellationToken cancellationToken);
    Task<PagedResult<BoyahaneLocationMovementDto>> GetMovementsAsync(BoyahaneLocationMovementQuery query, CancellationToken cancellationToken);
    Task<Result<int>> MoveAsync(BoyahaneMoveGoodsRequest request, CancellationToken cancellationToken);
}

/// <summary>
/// Depodaki mal: henüz üretime girmemiş ham mal, kalan miktarı olan boya lotu, onaylı listede sevk edilmemiş paket.
/// Makineye giren parti ve sevk edilen paket listeden düşer; son adresi geçmiş olarak kalır.
/// </summary>
public sealed class BoyahaneWarehousingService(IAppDbContext db, ICurrentUser currentUser, TimeProvider timeProvider) : IBoyahaneWarehousingService
{
    private const string NotInWarehouse = "GOODS_NOT_IN_WAREHOUSE";

    private IQueryable<BoyahaneFabricRoll> Rolls => BoyahaneWarehouseRules.RollsInWarehouse(db);

    private IQueryable<BoyahaneYarnLot> Yarns => BoyahaneWarehouseRules.YarnLotsInWarehouse(db);

    private IQueryable<BoyahaneChemicalLot> Chemicals => db.Set<BoyahaneChemicalLot>().Where(l => l.RemainingKilograms > 0);

    private IQueryable<BoyahanePackingItem> Packages => BoyahaneWarehouseRules.PackagesInWarehouse(db);

    public Task<PagedResult<BoyahaneWarehouseGoodsDto>> GetGoodsAsync(BoyahaneWarehouseGoodsQuery query, CancellationToken cancellationToken)
    {
        var term = NullIfBlank(query.Search);
        return (query.Type switch
        {
            WarehouseGoodsType.YarnLot => Yarns.AsNoTracking()
                .Where(y => (query.WarehouseId == null || y.Location!.WarehouseId == query.WarehouseId) && (query.LocationId == null || y.LocationId == query.LocationId))
                .Where(y => term == null || y.LotNo.Contains(term) || y.Yarn.YarnCode.Contains(term) || y.Yarn.Name.Contains(term) || (y.Customer != null && y.Customer.Name.Contains(term)))
                .OrderBy(y => y.LotNo)
                .Select(y => new BoyahaneWarehouseGoodsDto(
                    y.Id, y.LotNo, y.Yarn.YarnCode, y.Yarn.Name,
                    y.OrderLine != null ? y.OrderLine.ColorName : null,
                    y.Customer != null ? y.Customer.Name : null,
                    y.OrderLine != null ? y.OrderLine.Order.OrderNo : null,
                    y.Kilograms, null,
                    y.Status == YarnLotStatus.Rejected ? "Red" : "Uygun",
                    null,
                    y.Location != null ? y.Location.Warehouse.Name : null,
                    y.Location != null ? y.Location.Code : null)),
            WarehouseGoodsType.ChemicalLot => Chemicals.AsNoTracking()
                .Where(l => (query.WarehouseId == null || l.Location!.WarehouseId == query.WarehouseId) && (query.LocationId == null || l.LocationId == query.LocationId))
                .Where(l => term == null || l.LotNo.Contains(term) || l.DyeChemical.Code.Contains(term) || l.DyeChemical.Name.Contains(term) || (l.SupplierName != null && l.SupplierName.Contains(term)))
                .OrderBy(l => l.LotNo)
                .Select(l => new BoyahaneWarehouseGoodsDto(
                    l.Id, l.LotNo, l.DyeChemical.Code, l.DyeChemical.Name, null, l.SupplierName, null,
                    l.RemainingKilograms, null, null, l.ExpiryDate,
                    l.Location != null ? l.Location.Warehouse.Name : null,
                    l.Location != null ? l.Location.Code : null)),
            WarehouseGoodsType.Package => Packages.AsNoTracking()
                .Where(p => (query.WarehouseId == null || p.Location!.WarehouseId == query.WarehouseId) && (query.LocationId == null || p.LocationId == query.LocationId))
                .Where(p => term == null || p.PackageNo.Contains(term) || p.PackingList.Order.OrderNo.Contains(term) || (p.PackingList.Order.Customer != null && p.PackingList.Order.Customer.Name.Contains(term)))
                .OrderBy(p => p.PackageNo)
                .Select(p => new BoyahaneWarehouseGoodsDto(
                    p.Id, p.PackageNo,
                    p.BatchItem.OrderLine.Fabric != null ? p.BatchItem.OrderLine.Fabric.FabricCode : p.BatchItem.OrderLine.Yarn != null ? p.BatchItem.OrderLine.Yarn.YarnCode : "",
                    p.BatchItem.OrderLine.Fabric != null ? p.BatchItem.OrderLine.Fabric.Name : p.BatchItem.OrderLine.Yarn != null ? p.BatchItem.OrderLine.Yarn.Name : "",
                    p.BatchItem.OrderLine.ColorName,
                    p.PackingList.Order.Customer != null ? p.PackingList.Order.Customer.Name : null,
                    p.PackingList.Order.OrderNo,
                    p.Kilograms, p.Meters, null, null,
                    p.Location != null ? p.Location.Warehouse.Name : null,
                    p.Location != null ? p.Location.Code : null)),
            _ => Rolls.AsNoTracking()
                .Where(r => (query.WarehouseId == null || r.Location!.WarehouseId == query.WarehouseId) && (query.LocationId == null || r.LocationId == query.LocationId))
                .Where(r => term == null || r.RollNo.Contains(term) || r.Lot.Fabric.FabricCode.Contains(term) || r.Lot.Fabric.Name.Contains(term) || (r.Lot.Receipt.Customer != null && r.Lot.Receipt.Customer.Name.Contains(term)))
                .OrderBy(r => r.RollNo)
                .Select(r => new BoyahaneWarehouseGoodsDto(
                    r.Id, r.RollNo, r.Lot.Fabric.FabricCode, r.Lot.Fabric.Name,
                    r.OrderLine != null ? r.OrderLine.ColorName : null,
                    r.Lot.Receipt.Customer != null ? r.Lot.Receipt.Customer.Name : null,
                    r.OrderLine != null ? r.OrderLine.Order.OrderNo : null,
                    r.InspectedKilograms ?? r.Kilograms,
                    r.InspectedMeters ?? r.Meters,
                    r.Status == FabricRollStatus.Rejected ? "Red"
                        : r.Status == FabricRollStatus.Conditional ? "Şartlı"
                        : r.Status == FabricRollStatus.PendingInspection ? "Kontrol bekliyor"
                        : "Uygun",
                    null,
                    r.Location != null ? r.Location.Warehouse.Name : null,
                    r.Location != null ? r.Location.Code : null)),
        }).ToPagedResultAsync(query, cancellationToken);
    }

    public async Task<IReadOnlyList<BoyahaneLocationOptionDto>> GetLocationsAsync(int warehouseId, CancellationToken cancellationToken) =>
        await db.Set<BoyahaneWarehouseLocation>().AsNoTracking()
            .Where(l => l.WarehouseId == warehouseId)
            .OrderByDescending(l => l.IsDefault).ThenBy(l => l.Code)
            .Select(l => new BoyahaneLocationOptionDto(l.Id, l.Code, l.Description, l.IsActive))
            .ToListAsync(cancellationToken);

    public Task<PagedResult<BoyahaneLocationMovementDto>> GetMovementsAsync(BoyahaneLocationMovementQuery query, CancellationToken cancellationToken)
    {
        var movements = db.Set<BoyahaneLocationMovement>().AsNoTracking();
        if (NullIfBlank(query.Search) is { } term)
        {
            movements = movements.Where(m => m.GoodsNo.Contains(term));
        }

        return movements
            .OrderByDescending(m => m.Id)
            .Select(m => new BoyahaneLocationMovementDto(
                m.Id,
                m.GoodsType,
                m.GoodsNo,
                m.Type,
                m.FromLocation != null ? m.FromLocation.Warehouse.Name : null,
                m.FromLocation != null ? m.FromLocation.Code : null,
                m.ToLocation.Warehouse.Name,
                m.ToLocation.Code,
                m.Note,
                m.CreatedAt,
                m.CreatedBy))
            .ToPagedResultAsync(query, cancellationToken);
    }

    /// <summary>Malı hedef adrese taşır; depo değişiyorsa transfer, değişmiyorsa adres değişimi yazılır. Sonuç: taşınan mal sayısı.</summary>
    public async Task<Result<int>> MoveAsync(BoyahaneMoveGoodsRequest request, CancellationToken cancellationToken)
    {
        if (await BoyahaneWarehouseRules.ValidateLocationAsync(db, request.ToLocationId, cancellationToken) is { } error)
        {
            return error;
        }

        var ids = request.Ids.Distinct().ToList();
        var goods = await LoadAsync(request.Type, ids, cancellationToken);
        if (goods.Count != ids.Count)
        {
            return new Error(NotInWarehouse, "Seçilen malın bir kısmı artık depoda değil (üretimde, sevk edilmiş ya da tükenmiş); listeyi yenileyin.", ErrorType.Conflict);
        }

        var locationIds = goods.Where(g => g.Current != null).Select(g => g.Current!.Value).Append(request.ToLocationId).Distinct().ToList();
        var warehouses = await db.Set<BoyahaneWarehouseLocation>().IgnoreQueryFilters()
            .Where(l => locationIds.Contains(l.Id))
            .ToDictionaryAsync(l => l.Id, l => l.WarehouseId, cancellationToken);
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var moved = 0;
        foreach (var item in goods.Where(g => g.Current != request.ToLocationId))
        {
            item.Assign(request.ToLocationId);
            db.Set<BoyahaneLocationMovement>().Add(new BoyahaneLocationMovement
            {
                GoodsType = request.Type,
                GoodsId = item.Id,
                GoodsNo = item.No,
                Type = item.Current is { } from && warehouses[from] == warehouses[request.ToLocationId] ? LocationMovementType.AddressChange : LocationMovementType.Transfer,
                FromLocationId = item.Current,
                ToLocationId = request.ToLocationId,
                Note = NullIfBlank(request.Note),
                CreatedAt = now,
                CreatedBy = currentUser.UserName,
            });
            moved++;
        }

        await db.SaveChangesAsync(cancellationToken);
        return moved;
    }

    private sealed record Goods(int Id, string No, int? Current, Action<int> Assign);

    private async Task<List<Goods>> LoadAsync(WarehouseGoodsType type, List<int> ids, CancellationToken cancellationToken)
    {
        switch (type)
        {
            case WarehouseGoodsType.YarnLot:
                return (await Yarns.Where(x => ids.Contains(x.Id)).ToListAsync(cancellationToken))
                    .Select(x => new Goods(x.Id, x.LotNo, x.LocationId, to => x.LocationId = to)).ToList();
            case WarehouseGoodsType.ChemicalLot:
                return (await Chemicals.Where(x => ids.Contains(x.Id)).ToListAsync(cancellationToken))
                    .Select(x => new Goods(x.Id, x.LotNo, x.LocationId, to => x.LocationId = to)).ToList();
            case WarehouseGoodsType.Package:
                return (await Packages.Where(x => ids.Contains(x.Id)).ToListAsync(cancellationToken))
                    .Select(x => new Goods(x.Id, x.PackageNo, x.LocationId, to => x.LocationId = to)).ToList();
            default:
                return (await Rolls.Where(x => ids.Contains(x.Id)).ToListAsync(cancellationToken))
                    .Select(x => new Goods(x.Id, x.RollNo, x.LocationId, to => x.LocationId = to)).ToList();
        }
    }
}
