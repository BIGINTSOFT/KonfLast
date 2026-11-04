using ERP.Konfeksiyon.Application.Abstractions;
using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Core.Domain.Enums;
using ERP.Konfeksiyon.Application.Features.Warehousing;
using ERP.Konfeksiyon.Domain.Entities;
using ERP.Konfeksiyon.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ERP.Konfeksiyon.Application.Features.Shipping;

/// <summary>Toplama kuyruğundaki (toplamada ya da toplanmış) sevkiyat.</summary>
/// <param name="PickingBy">Toplamayı üstlenmiş kullanıcı (henüz kimse başlamadıysa boş).</param>
public sealed record PickingQueueItemDto(
    int Id,
    string ShipmentNo,
    ShipmentStatus Status,
    string CustomerName,
    string WarehouseCode,
    string? OrderNo,
    string? CustomerOrderNo,
    DateOnly ShipDate,
    int ItemCount,
    decimal Quantity,
    decimal PickedQuantity,
    string? PickingBy,
    DateTime? PickingStartedAt);

public sealed class PickingQueueQuery
{
    /// <summary>Sevkiyat no, müşteri, sipariş no ya da müşteri sipariş no içinde aranır.</summary>
    public string? Search { get; set; }

    /// <summary>Verilmezse toplamadaki sevkiyatlar listelenir.</summary>
    public ShipmentStatus? Status { get; set; }
}

/// <summary>Sevkiyatın bir kalemi ve toplanma durumu.</summary>
public sealed record PickingItemDto(
    int ShipmentItemId,
    int StockItemId,
    string ItemCode,
    string ItemName,
    string? ProductCode,
    string? ColorName,
    string? SizeName,
    int SizeOrder,
    StockUnit Unit,
    decimal Quantity,
    decimal PickedQuantity,
    decimal? RequestedQuantity,
    decimal Shortage);

/// <summary>
/// Toplama listesinin bir satırı: bir adresten bir kalemden alınacak adet. Satırlar adres sırasıyla (önce genel adres,
/// sonra adres kodu) numaralanır; toplayıcı bu sırayla yürür.
/// </summary>
/// <param name="Quantity">Bu adresten alınacak toplam (alınmış + kalan öneri).</param>
/// <param name="PickedQuantity">Bu adresten şimdiye kadar alınan.</param>
public sealed record PickingStepDto(
    int Sequence,
    int ShipmentItemId,
    int StockItemId,
    string ItemCode,
    string ItemName,
    string? ColorName,
    string? SizeName,
    int LocationId,
    string LocationCode,
    decimal Quantity,
    decimal PickedQuantity,
    bool Done);

public sealed record PickRecordDto(long Id, string ItemCode, string LocationCode, decimal Quantity, DateTime PickedAt, string PickedBy);

/// <param name="IsMine">Toplamayı isteği yapan kullanıcı üstlenmiş.</param>
public sealed record ShipmentPickingDto(
    int Id,
    string ShipmentNo,
    ShipmentStatus Status,
    string CustomerName,
    int WarehouseId,
    string WarehouseCode,
    string? OrderNo,
    string? CustomerOrderNo,
    DateOnly ShipDate,
    string? Notes,
    string? PickingBy,
    DateTime? PickingStartedAt,
    bool IsMine,
    string RowVersion,
    IReadOnlyList<PickingItemDto> Items,
    IReadOnlyList<PickingStepDto> Steps,
    IReadOnlyList<PickRecordDto> Picks);

/// <param name="TakeOver">Başka bir kullanıcının üstlendiği toplamayı devral.</param>
public sealed record StartPickingRequest(bool TakeOver = false);

/// <summary>Toplayıcının bir adresten aldığı adet; istek kimliği tekrar gönderimde çift kaydı önler.</summary>
public sealed record PickRequest(Guid RequestId, int ShipmentItemId, int LocationId, decimal Quantity);

public sealed class PickRequestValidator : AbstractValidator<PickRequest>
{
    public PickRequestValidator()
    {
        RuleFor(x => x.RequestId).NotEmpty().WithMessage("İstek kimliği zorunludur.");
        RuleFor(x => x.ShipmentItemId).GreaterThan(0).WithMessage("Kalem seçilmelidir.");
        RuleFor(x => x.LocationId).GreaterThan(0).WithMessage("Adres okutulmalıdır.");
        RuleFor(x => x.Quantity)
            .Must(q => q > 0 && q <= StockRules.MaxQuantity && decimal.Round(q, 3) == q)
            .WithMessage("Adet sıfırdan büyük olmalı ve en fazla 3 ondalık basamak içermelidir.");
    }
}

/// <summary>Sevke hazır müşteri siparişi: depoda ayrılmış ve henüz bir sevkiyata aktarılmamış mal.</summary>
/// <param name="Reserved">Depoda siparişe ayrılmış toplam (stoktan ve üretimden).</param>
/// <param name="InShipments">Açık (taslak, toplamada, toplanmış) sevkiyatlarda bulunan adet.</param>
/// <param name="Transferable">Sevkiyata aktarılabilecek adet: ayrılan − açık sevkiyatlardaki.</param>
public sealed record ReadyOrderDto(
    int SalesOrderId,
    string OrderNo,
    string? CustomerOrderNo,
    int CustomerId,
    string CustomerName,
    DateOnly DueDate,
    int Ordered,
    decimal Shipped,
    decimal Reserved,
    decimal InShipments,
    decimal Transferable,
    int OpenShipmentCount,
    IReadOnlyList<string> WarehouseCodes);

/// <summary>Sevke hazır siparişin bir depo + mamul satırı.</summary>
public sealed record ReadyOrderLineDto(
    int WarehouseId,
    string WarehouseCode,
    int StockItemId,
    string ItemCode,
    string? ProductCode,
    string? ProductName,
    string? ColorName,
    string? SizeName,
    int SizeOrder,
    int Ordered,
    decimal Shipped,
    decimal Reserved,
    decimal InShipments,
    decimal Transferable);

public sealed record ReadyOrderShipmentDto(int Id, string ShipmentNo, ShipmentStatus Status, string WarehouseCode, decimal Quantity, decimal PickedQuantity);

public sealed record ReadyOrderDetailDto(
    ReadyOrderDto Order, IReadOnlyList<ReadyOrderLineDto> Lines, IReadOnlyList<ReadyOrderShipmentDto> Shipments);

public sealed record ReadyOrderTransferLine(int StockItemId, decimal Quantity);

/// <summary>Siparişin bir depodaki ayrılmış malını yeni bir sevkiyata aktarır ve depoya toplamaya verir.</summary>
public sealed record ReadyOrderTransferRequest(
    int WarehouseId, DateOnly ShipDate, IReadOnlyList<ReadyOrderTransferLine> Lines, string? Carrier = null, string? Notes = null);

public sealed class ReadyOrderTransferRequestValidator : AbstractValidator<ReadyOrderTransferRequest>
{
    public ReadyOrderTransferRequestValidator()
    {
        RuleFor(x => x.WarehouseId).GreaterThan(0).WithMessage("Depo seçilmelidir.");
        RuleFor(x => x.ShipDate).NotEqual(default(DateOnly)).WithMessage("Sevk tarihi zorunludur.");
        RuleFor(x => x.Lines).NotEmpty().WithMessage("Aktarılacak en az bir kalem seçilmelidir.");
        RuleForEach(x => x.Lines)
            .Must(l => l.StockItemId > 0 && l.Quantity > 0 && decimal.Round(l.Quantity) == l.Quantity)
            .WithMessage("Aktarılacak adet pozitif tam sayı olmalıdır.");
        RuleFor(x => x.Carrier).MaximumLength(150);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

public interface IShipmentPickingService
{
    Task<IReadOnlyList<PickingQueueItemDto>> GetQueueAsync(PickingQueueQuery query, CancellationToken cancellationToken);
    Task<Result<ShipmentPickingDto>> GetAsync(int shipmentId, CancellationToken cancellationToken);

    /// <summary>Taslak sevkiyatı depoya toplamaya verir; kalemler kilitlenir.</summary>
    Task<Result<ShipmentPickingDto>> SendToPickingAsync(int shipmentId, string rowVersion, CancellationToken cancellationToken);

    /// <summary>Hiçbir şey toplanmamışsa toplamayı geri alır; sevkiyat yeniden düzenlenebilir.</summary>
    Task<Result<ShipmentPickingDto>> ReturnToDraftAsync(int shipmentId, string rowVersion, CancellationToken cancellationToken);

    /// <summary>Toplamayı üstlenir; başka kullanıcının işi ancak devralma onayıyla alınır.</summary>
    Task<Result<ShipmentPickingDto>> StartAsync(int shipmentId, StartPickingRequest request, CancellationToken cancellationToken);

    Task<Result<ShipmentPickingDto>> PickAsync(int shipmentId, PickRequest request, CancellationToken cancellationToken);
    Task<Result<ShipmentPickingDto>> UndoPickAsync(int shipmentId, long pickId, CancellationToken cancellationToken);

    /// <summary>Toplamayı bitirir: eksik toplanan kalemler toplanan adede iner, hiç toplanmayan kalem düşer.</summary>
    Task<Result<ShipmentPickingDto>> CompleteAsync(int shipmentId, CancellationToken cancellationToken);

    /// <summary>Toplanmış sevkiyatı yeniden toplamaya açar; eksik biten kalemler ilk istenen adede döner.</summary>
    Task<Result<ShipmentPickingDto>> ReopenAsync(int shipmentId, string rowVersion, CancellationToken cancellationToken);

    Task<IReadOnlyList<ReadyOrderDto>> GetReadyOrdersAsync(string? search, CancellationToken cancellationToken);
    Task<Result<ReadyOrderDetailDto>> GetReadyOrderAsync(int salesOrderId, CancellationToken cancellationToken);

    /// <summary>Siparişin ayrılmış malını yeni sevkiyata aktarır ve toplamaya verir; oluşan sevkiyatın kimliğini döndürür.</summary>
    Task<Result<int>> TransferAsync(int salesOrderId, ReadyOrderTransferRequest request, CancellationToken cancellationToken);
}

/// <summary>
/// Sevkiyat hazırlığı: siparişten sevkiyata aktarma, depoda adres sırasıyla toplama ve toplamanın bitirilmesi.
/// Toplama stok hareketi yazmaz; stok, irsaliye kesilince toplanan adreslerden düşer. Toplama işlemleri sevkiyat
/// kilidi altında yürür: aynı sevkiyat iki cihazda aynı anda toplanamaz (üstlenen kullanıcı dışındakiler yazamaz).
/// </summary>
public sealed class ShipmentPickingService(
    IKonfeksiyonDbContext db,
    IAppLock appLock,
    ICurrentUser currentUser,
    IShipmentService shipments,
    TimeProvider timeProvider) : IShipmentPickingService
{
    private const string SystemUser = "system";
    private static readonly TimeSpan LockTimeout = TimeSpan.FromSeconds(10);

    private static readonly ShipmentStatus[] OpenStatuses = [ShipmentStatus.Draft, ShipmentStatus.Picking, ShipmentStatus.Picked];

    private static readonly Error NotFound = new(ErrorCodes.NotFound, "Sevkiyat bulunamadı.", ErrorType.NotFound);

    private static readonly Error Busy = new(
        ErrorCodes.ResourceBusy, "Bu sevkiyat şu an başka bir işlemle değiştiriliyor. Lütfen tekrar deneyin.", ErrorType.Conflict);

    private static readonly Error ConcurrencyConflict = new(
        ErrorCodes.ConcurrencyConflict,
        "Sevkiyat siz açtıktan sonra başka bir kullanıcı tarafından değiştirildi. Sayfayı yenileyip tekrar deneyin.",
        ErrorType.Conflict);

    private static readonly Error NotPicking = new(
        ErrorCodes.InvalidStatusTransition, "Sevkiyat toplamada değil; toplama yalnızca depoya verilmiş sevkiyatta yapılır.");

    private string Actor => currentUser.UserName ?? SystemUser;
    private DateTime UtcNow => timeProvider.GetUtcNow().UtcDateTime;

    public async Task<IReadOnlyList<PickingQueueItemDto>> GetQueueAsync(PickingQueueQuery query, CancellationToken cancellationToken)
    {
        var status = query.Status ?? ShipmentStatus.Picking;
        var list = db.Shipments.AsNoTracking().Where(s => s.Status == status);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            list = list.Where(s => s.ShipmentNo.Contains(term) || s.Customer.Name.Contains(term)
                || (s.SalesOrder != null && (s.SalesOrder.OrderNo.Contains(term)
                    || (s.SalesOrder.CustomerOrderNo != null && s.SalesOrder.CustomerOrderNo.Contains(term)))));
        }

        // En eski sevk günü önce: depo, önce gidecek malı toplar.
        return await list
            .OrderBy(s => s.ShipDate).ThenBy(s => s.Id)
            .Take(100)
            .Select(s => new PickingQueueItemDto(
                s.Id, s.ShipmentNo, s.Status, s.Customer.Name, s.Warehouse.Code,
                s.SalesOrder != null ? s.SalesOrder.OrderNo : s.ProductionOrder != null ? s.ProductionOrder.OrderNo : null,
                s.SalesOrder != null ? s.SalesOrder.CustomerOrderNo : null,
                s.ShipDate, s.Items.Count, s.Items.Sum(i => (decimal?)i.Quantity) ?? 0, s.Items.Sum(i => (decimal?)i.PickedQuantity) ?? 0,
                s.PickingBy, s.PickingStartedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<Result<ShipmentPickingDto>> GetAsync(int shipmentId, CancellationToken cancellationToken)
    {
        var header = await db.Shipments.AsNoTracking()
            .Where(s => s.Id == shipmentId)
            .Select(s => new
            {
                s.Id, s.ShipmentNo, s.Status, CustomerName = s.Customer.Name, s.WarehouseId, WarehouseCode = s.Warehouse.Code,
                OrderNo = s.SalesOrder != null ? s.SalesOrder.OrderNo : s.ProductionOrder != null ? s.ProductionOrder.OrderNo : null,
                CustomerOrderNo = s.SalesOrder != null ? s.SalesOrder.CustomerOrderNo : null,
                s.ShipDate, s.Notes, s.PickingBy, s.PickingStartedAt, s.RowVersion,
            })
            .SingleOrDefaultAsync(cancellationToken);
        if (header is null)
        {
            return NotFound;
        }

        var items = await db.ShipmentItems.AsNoTracking()
            .Where(i => i.ShipmentId == shipmentId)
            .Select(i => new
            {
                i.Id, i.StockItemId, i.StockItem.Code, i.StockItem.Name, i.StockItem.Unit, i.Quantity, i.PickedQuantity, i.RequestedQuantity,
                ProductCode = i.StockItem.Product != null ? i.StockItem.Product.ProductCode : null,
                ColorName = i.StockItem.Color != null ? i.StockItem.Color.Name : null,
                SizeName = i.StockItem.Size != null ? i.StockItem.Size.Name : null,
                SizeOrder = i.StockItem.Size != null ? i.StockItem.Size.SortOrder : 0,
            })
            .ToListAsync(cancellationToken);
        items = items
            .OrderBy(i => i.ProductCode ?? i.Code, StringComparer.Ordinal)
            .ThenBy(i => i.ColorName ?? string.Empty, StringComparer.Ordinal)
            .ThenBy(i => i.SizeOrder)
            .ThenBy(i => i.Code, StringComparer.Ordinal)
            .ToList();

        var picks = await db.ShipmentPicks.AsNoTracking()
            .Where(p => p.ShipmentId == shipmentId)
            .OrderBy(p => p.Id)
            .Select(p => new { p.Id, p.ShipmentItemId, p.LocationId, LocationCode = p.Location.Code, p.Quantity, p.PickedAt, p.PickedBy })
            .ToListAsync(cancellationToken);

        // Kalemlerin depodaki adres bakiyeleri, toplayıcının yürüyeceği sırayla.
        var itemIds = items.Select(i => i.StockItemId).ToList();
        var balances = await db.Stocks.AsNoTracking()
            .Where(s => s.WarehouseId == header.WarehouseId && itemIds.Contains(s.StockItemId) && s.Quantity > 0)
            .GroupBy(s => new { s.StockItemId, s.LocationId, s.Location.Code, s.Location.IsDefault })
            .Select(g => new { g.Key.StockItemId, g.Key.LocationId, g.Key.Code, g.Key.IsDefault, Quantity = g.Sum(s => s.Quantity) })
            .ToListAsync(cancellationToken);
        var locationCodes = balances.Select(b => (b.LocationId, b.Code, b.IsDefault))
            .Concat(picks.Select(p => (p.LocationId, Code: p.LocationCode, IsDefault: false)))
            .GroupBy(l => l.LocationId)
            .ToDictionary(g => g.Key, g => (g.First().Code, IsDefault: g.Any(l => l.IsDefault)));

        var rows = new List<(int Item, int LocationId, decimal Quantity, decimal Picked)>();
        var shortages = new Dictionary<int, decimal>();
        foreach (var item in items)
        {
            var pickedHere = picks.Where(p => p.ShipmentItemId == item.Id)
                .GroupBy(p => p.LocationId).ToDictionary(g => g.Key, g => g.Sum(p => p.Quantity));
            var remaining = header.Status == ShipmentStatus.Picking ? Math.Max(item.Quantity - item.PickedQuantity, 0) : 0;
            var suggested = new Dictionary<int, decimal>();
            foreach (var balance in balances.Where(b => b.StockItemId == item.StockItemId)
                         .OrderByDescending(b => b.IsDefault).ThenBy(b => b.Code, StringComparer.Ordinal))
            {
                if (remaining <= 0)
                {
                    break;
                }

                // Bu sevkiyat için oradan alınmış olan, adreste hâlâ duruyor (stok irsaliyede düşer).
                var take = Math.Min(Math.Max(balance.Quantity - pickedHere.GetValueOrDefault(balance.LocationId), 0), remaining);
                if (take > 0)
                {
                    suggested[balance.LocationId] = take;
                    remaining -= take;
                }
            }

            shortages[item.Id] = remaining;
            foreach (var locationId in pickedHere.Keys.Union(suggested.Keys))
            {
                var picked = pickedHere.GetValueOrDefault(locationId);
                rows.Add((item.Id, locationId, picked + suggested.GetValueOrDefault(locationId), picked));
            }
        }

        var itemOrder = items.Select((item, index) => (item.Id, index)).ToDictionary(x => x.Id, x => x.index);
        var steps = rows
            .OrderByDescending(r => locationCodes[r.LocationId].IsDefault)
            .ThenBy(r => locationCodes[r.LocationId].Code, StringComparer.Ordinal)
            .ThenBy(r => itemOrder[r.Item])
            .Select((r, index) =>
            {
                var item = items[itemOrder[r.Item]];
                return new PickingStepDto(
                    index + 1, item.Id, item.StockItemId, item.Code, item.Name, item.ColorName, item.SizeName, r.LocationId,
                    locationCodes[r.LocationId].Code, r.Quantity, r.Picked, r.Picked >= r.Quantity);
            })
            .ToList();

        var codes = items.ToDictionary(i => i.Id, i => i.Code);
        return new ShipmentPickingDto(
            header.Id, header.ShipmentNo, header.Status, header.CustomerName, header.WarehouseId, header.WarehouseCode, header.OrderNo,
            header.CustomerOrderNo, header.ShipDate, header.Notes, header.PickingBy, header.PickingStartedAt,
            header.PickingBy != null && string.Equals(header.PickingBy, Actor, StringComparison.OrdinalIgnoreCase),
            RowVersionToken.From(header.RowVersion),
            items.Select(i => new PickingItemDto(
                    i.Id, i.StockItemId, i.Code, i.Name, i.ProductCode, i.ColorName, i.SizeName, i.SizeOrder, i.Unit, i.Quantity,
                    i.PickedQuantity, i.RequestedQuantity, shortages.GetValueOrDefault(i.Id)))
                .ToList(),
            steps,
            picks.Select(p => new PickRecordDto(p.Id, codes.GetValueOrDefault(p.ShipmentItemId) ?? "?", p.LocationCode, p.Quantity, p.PickedAt, p.PickedBy))
                .ToList());
    }

    public Task<Result<ShipmentPickingDto>> SendToPickingAsync(int shipmentId, string rowVersion, CancellationToken cancellationToken) =>
        TransitionAsync(shipmentId, rowVersion, shipment =>
        {
            if (!shipment.CanSendToPicking)
            {
                return new Error(
                    shipment.Status == ShipmentStatus.Draft ? ErrorCodes.ShipmentHasNoItems : ErrorCodes.InvalidStatusTransition,
                    shipment.Status == ShipmentStatus.Draft
                        ? "Kalemi olmayan sevkiyat toplamaya verilemez."
                        : "Yalnızca taslak sevkiyat toplamaya verilebilir.");
            }

            shipment.SendToPicking();
            return null;
        }, cancellationToken);

    public Task<Result<ShipmentPickingDto>> ReturnToDraftAsync(int shipmentId, string rowVersion, CancellationToken cancellationToken) =>
        TransitionAsync(shipmentId, rowVersion, shipment =>
        {
            if (!shipment.CanReturnToDraft)
            {
                return new Error(
                    ErrorCodes.InvalidStatusTransition,
                    shipment.Status == ShipmentStatus.Picking
                        ? "Toplanmaya başlanmış sevkiyat taslağa döndürülemez; önce toplananları geri alın."
                        : "Yalnızca toplamadaki sevkiyat taslağa döndürülebilir.");
            }

            shipment.ReturnToDraft();
            return null;
        }, cancellationToken);

    public Task<Result<ShipmentPickingDto>> ReopenAsync(int shipmentId, string rowVersion, CancellationToken cancellationToken) =>
        TransitionAsync(shipmentId, rowVersion, shipment =>
        {
            if (!shipment.CanReopenPicking)
            {
                return new Error(ErrorCodes.InvalidStatusTransition, "Yalnızca toplanmış (irsaliye bekleyen) sevkiyat yeniden toplamaya açılır.");
            }

            foreach (var item in shipment.Items)
            {
                if (item.RequestedQuantity is { } requested)
                {
                    item.Quantity = requested;
                    item.RequestedQuantity = null;
                }

                item.CheckedQuantity = null;
            }

            shipment.ReopenPicking();
            return null;
        }, cancellationToken);

    public async Task<Result<ShipmentPickingDto>> StartAsync(int shipmentId, StartPickingRequest request, CancellationToken cancellationToken)
    {
        return await LockedAsync(shipmentId, async shipment =>
        {
            if (shipment.Status != ShipmentStatus.Picking)
            {
                return NotPicking;
            }

            if (shipment.PickingBy is { } other && !IsActor(other) && !request.TakeOver)
            {
                return ClaimedBy(shipment);
            }

            shipment.ClaimPicking(UtcNow, Actor);
            await Task.CompletedTask;
            return null;
        }, cancellationToken);
    }

    public async Task<Result<ShipmentPickingDto>> PickAsync(int shipmentId, PickRequest request, CancellationToken cancellationToken)
    {
        // Aynı okutma (bağlantı koptu, yeniden gönderildi) ikinci kez yazılmaz.
        if (await db.ShipmentPicks.AnyAsync(p => p.RequestId == request.RequestId, cancellationToken))
        {
            return await db.ShipmentPicks.AnyAsync(p => p.RequestId == request.RequestId && p.ShipmentId == shipmentId, cancellationToken)
                ? await GetAsync(shipmentId, cancellationToken)
                : new Error(ErrorCodes.RequestIdReused, "Bu istek kimliği başka bir işlem için kullanılmış.", ErrorType.Conflict);
        }

        return await LockedAsync(shipmentId, async shipment =>
        {
            if (shipment.Status != ShipmentStatus.Picking)
            {
                return NotPicking;
            }

            if (shipment.PickingBy is null || !IsActor(shipment.PickingBy))
            {
                return shipment.PickingBy is null
                    ? new Error(ErrorCodes.PickingClaimed, "Önce toplamaya başlayın.", ErrorType.Conflict)
                    : ClaimedBy(shipment);
            }

            var item = shipment.Items.SingleOrDefault(i => i.Id == request.ShipmentItemId);
            if (item is null)
            {
                return ReferenceValidation.InvalidReference("shipmentItemId", "Kalem bu sevkiyata ait değil.");
            }

            var card = await db.StockItems.AsNoTracking().IgnoreQueryFilters()
                .Where(i => i.Id == item.StockItemId).Select(i => new { i.Code, i.Unit }).SingleAsync(cancellationToken);
            if (card.Unit == StockUnit.Piece && decimal.Truncate(request.Quantity) != request.Quantity)
            {
                return PickError(ErrorCodes.StockQuantityInvalid, "quantity", "Adetle izlenen kartta miktar tam sayı olmalıdır.", ErrorType.Validation);
            }

            if (item.PickedQuantity + request.Quantity > item.Quantity)
            {
                return PickError(
                    ErrorCodes.PickExceedsItem, "quantity",
                    $"{card.Code}: sevkiyatta {item.Quantity:0.###}, toplanan {item.PickedQuantity:0.###}; en fazla {item.Quantity - item.PickedQuantity:0.###} alınabilir.");
            }

            var location = await db.WarehouseLocations.AsNoTracking()
                .Where(l => l.Id == request.LocationId)
                .Select(l => new { l.WarehouseId, l.Code, l.IsActive })
                .SingleOrDefaultAsync(cancellationToken);
            if (location is null || location.WarehouseId != shipment.WarehouseId)
            {
                return PickError(ErrorCodes.InvalidReference, "locationId", "Okutulan adres sevkiyatın deposunda değil.");
            }

            // Adreste duran stok, bu sevkiyat için oradan zaten alınmış olanı da kapsar (stok irsaliyede düşer).
            var atLocation = await db.Stocks
                .Where(s => s.WarehouseId == shipment.WarehouseId && s.LocationId == request.LocationId && s.StockItemId == item.StockItemId)
                .SumAsync(s => (decimal?)s.Quantity, cancellationToken) ?? 0;
            var alreadyTaken = shipment.Picks.Where(p => p.LocationId == request.LocationId && p.ShipmentItemId == item.Id).Sum(p => p.Quantity);
            if (alreadyTaken + request.Quantity > atLocation)
            {
                return PickError(
                    ErrorCodes.PickLocationShort, "quantity",
                    $"{location.Code} adresinde {card.Code} için {atLocation:0.###} var, bu sevkiyata {alreadyTaken:0.###} alınmış; "
                    + $"{Math.Max(atLocation - alreadyTaken, 0):0.###} adetten fazlası buradan alınamaz.");
            }

            db.ShipmentPicks.Add(new ShipmentPick
            {
                RequestId = request.RequestId,
                ShipmentId = shipment.Id,
                ShipmentItemId = item.Id,
                LocationId = request.LocationId,
                Quantity = request.Quantity,
                PickedAt = UtcNow,
                PickedBy = Actor,
            });
            item.PickedQuantity += request.Quantity;
            return null;
        }, cancellationToken);
    }

    public async Task<Result<ShipmentPickingDto>> UndoPickAsync(int shipmentId, long pickId, CancellationToken cancellationToken) =>
        await LockedAsync(shipmentId, async shipment =>
        {
            if (shipment.Status != ShipmentStatus.Picking)
            {
                return NotPicking;
            }

            if (shipment.PickingBy is { } other && !IsActor(other))
            {
                return ClaimedBy(shipment);
            }

            var pick = shipment.Picks.SingleOrDefault(p => p.Id == pickId);
            if (pick is null)
            {
                return new Error(ErrorCodes.NotFound, "Toplama kaydı bulunamadı.", ErrorType.NotFound);
            }

            shipment.Items.Single(i => i.Id == pick.ShipmentItemId).PickedQuantity -= pick.Quantity;
            db.ShipmentPicks.Remove(pick);
            await Task.CompletedTask;
            return null;
        }, cancellationToken);

    public async Task<Result<ShipmentPickingDto>> CompleteAsync(int shipmentId, CancellationToken cancellationToken) =>
        await LockedAsync(shipmentId, async shipment =>
        {
            if (shipment.Status != ShipmentStatus.Picking)
            {
                return NotPicking;
            }

            if (shipment.PickingBy is { } other && !IsActor(other))
            {
                return ClaimedBy(shipment);
            }

            if (!shipment.CanCompletePicking)
            {
                return new Error(ErrorCodes.NothingToReceive, "Hiç mal toplanmamış; toplama bitirilemez. Gerekirse sevkiyatı taslağa döndürün.");
            }

            // Eksik toplanan kalem toplanan adede iner; hiç toplanmayan kalem sevkiyattan düşer.
            foreach (var item in shipment.Items.ToList())
            {
                if (item.PickedQuantity <= 0)
                {
                    shipment.Items.Remove(item);
                    db.ShipmentItems.Remove(item);
                }
                else if (item.PickedQuantity < item.Quantity)
                {
                    item.RequestedQuantity ??= item.Quantity;
                    item.Quantity = item.PickedQuantity;
                }
            }

            shipment.CompletePicking(UtcNow, Actor);
            await Task.CompletedTask;
            return null;
        }, cancellationToken);

    public async Task<IReadOnlyList<ReadyOrderDto>> GetReadyOrdersAsync(string? search, CancellationToken cancellationToken)
    {
        var orders = db.SalesOrders.AsNoTracking()
            .Where(o => o.Status == SalesOrderStatus.Approved && db.SalesOrderReservations.Any(r => r.SalesOrderId == o.Id));
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            orders = orders.Where(o => o.OrderNo.Contains(term) || o.Customer.Name.Contains(term)
                || (o.CustomerOrderNo != null && o.CustomerOrderNo.Contains(term)));
        }

        var ids = await orders.Select(o => o.Id).Take(500).ToListAsync(cancellationToken);
        var result = new List<ReadyOrderDto>();
        foreach (var detail in await LoadReadyAsync(ids, cancellationToken))
        {
            if (detail.Order.Transferable > 0)
            {
                result.Add(detail.Order);
            }
        }

        return result.OrderBy(o => o.DueDate).ThenBy(o => o.OrderNo, StringComparer.Ordinal).ToList();
    }

    public async Task<Result<ReadyOrderDetailDto>> GetReadyOrderAsync(int salesOrderId, CancellationToken cancellationToken)
    {
        if (!await db.SalesOrders.AnyAsync(o => o.Id == salesOrderId, cancellationToken))
        {
            return new Error(ErrorCodes.NotFound, "Müşteri siparişi bulunamadı.", ErrorType.NotFound);
        }

        return (await LoadReadyAsync([salesOrderId], cancellationToken)).Single();
    }

    public async Task<Result<int>> TransferAsync(int salesOrderId, ReadyOrderTransferRequest request, CancellationToken cancellationToken)
    {
        // Aynı siparişin iki aktarması sırayla yürür: ikincisi ilkinin sevkiyatını "açık sevkiyatta" görür.
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (!await appLock.TryAcquireAsync($"sales-order-transfer:{salesOrderId}", LockTimeout, cancellationToken))
        {
            return Busy;
        }

        var order = await db.SalesOrders.AsNoTracking()
            .Where(o => o.Id == salesOrderId)
            .Select(o => new { o.Id, o.OrderNo, o.CustomerId, o.Status })
            .SingleOrDefaultAsync(cancellationToken);
        if (order is null)
        {
            return new Error(ErrorCodes.NotFound, "Müşteri siparişi bulunamadı.", ErrorType.NotFound);
        }

        if (order.Status != SalesOrderStatus.Approved)
        {
            return new Error(ErrorCodes.InvalidStatusTransition, "Yalnızca onaylı siparişin malı sevkiyata aktarılabilir.");
        }

        var ready = (await LoadReadyAsync([salesOrderId], cancellationToken)).Single();
        var problems = request.Lines
            .Select(line => (line, available: ready.Lines
                .Where(l => l.WarehouseId == request.WarehouseId && l.StockItemId == line.StockItemId).Sum(l => l.Transferable)))
            .Where(x => x.line.Quantity > x.available)
            .Select(x => $"{ready.Lines.FirstOrDefault(l => l.StockItemId == x.line.StockItemId)?.ItemCode ?? $"#{x.line.StockItemId}"}: "
                + $"aktarılabilecek {x.available:0.###}, istenen {x.line.Quantity:0.###}.")
            .ToList();
        if (problems.Count > 0)
        {
            return new Error(
                ErrorCodes.TransferExceedsReserved, "Aktarılacak adet, depoda siparişe ayrılmış ve henüz sevkiyata aktarılmamış adedi aşıyor.",
                ErrorType.BusinessRule, new Dictionary<string, string[]> { ["lines"] = [.. problems] });
        }

        var created = await shipments.CreateAsync(
            new ShipmentRequest(
                order.CustomerId, request.WarehouseId, request.ShipDate,
                request.Lines.GroupBy(l => l.StockItemId).Select(g => new ShipmentItemRequest(g.Key, g.Sum(l => l.Quantity))).ToList(),
                Carrier: request.Carrier, Notes: request.Notes, SalesOrderId: salesOrderId),
            cancellationToken);
        if (!created.IsSuccess)
        {
            return created.Error!;
        }

        var shipment = await db.Shipments.Include(s => s.Items).SingleAsync(s => s.Id == created.Value.Id, cancellationToken);
        shipment.SendToPicking();
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return shipment.Id;
    }

    /// <summary>Siparişlerin depo + kart bazında ayırma, açık sevkiyat ve sevk edilen adetleri.</summary>
    private async Task<List<ReadyOrderDetailDto>> LoadReadyAsync(IReadOnlyCollection<int> salesOrderIds, CancellationToken cancellationToken)
    {
        if (salesOrderIds.Count == 0)
        {
            return [];
        }

        var orders = await db.SalesOrders.AsNoTracking().IgnoreQueryFilters()
            .Where(o => salesOrderIds.Contains(o.Id))
            .Select(o => new
            {
                o.Id, o.OrderNo, o.CustomerOrderNo, o.CustomerId, CustomerName = o.Customer.Name, o.DueDate,
                Lines = o.Lines.Select(l => new { l.ProductId, l.ColorId, l.SizeId, l.Quantity }).ToList(),
            })
            .ToListAsync(cancellationToken);
        var reservations = await db.SalesOrderReservations.AsNoTracking()
            .Where(r => salesOrderIds.Contains(r.SalesOrderId))
            .GroupBy(r => new { r.SalesOrderId, r.WarehouseId, r.StockItemId })
            .Select(g => new { g.Key.SalesOrderId, g.Key.WarehouseId, g.Key.StockItemId, Quantity = g.Sum(r => r.Quantity) })
            .ToListAsync(cancellationToken);
        var shipmentItems = await db.ShipmentItems.AsNoTracking()
            .Where(i => i.Shipment.SalesOrderId != null && salesOrderIds.Contains(i.Shipment.SalesOrderId.Value)
                && i.Shipment.Status != ShipmentStatus.Cancelled)
            .Select(i => new
            {
                SalesOrderId = i.Shipment.SalesOrderId!.Value, i.Shipment.WarehouseId, i.StockItemId, i.Quantity, i.PickedQuantity, i.Shipment.Status,
                i.ShipmentId, i.Shipment.ShipmentNo, WarehouseCode = i.Shipment.Warehouse.Code,
            })
            .ToListAsync(cancellationToken);

        var itemIds = reservations.Select(r => r.StockItemId).Concat(shipmentItems.Select(i => i.StockItemId)).Distinct().ToList();
        var cards = await db.StockItems.AsNoTracking().IgnoreQueryFilters()
            .Where(i => itemIds.Contains(i.Id))
            .Select(i => new
            {
                i.Id, i.Code, i.ProductId, i.ColorId, i.SizeId,
                ProductCode = i.Product != null ? i.Product.ProductCode : null,
                ProductName = i.Product != null ? i.Product.Name : null,
                ColorName = i.Color != null ? i.Color.Name : null,
                SizeName = i.Size != null ? i.Size.Name : null,
                SizeOrder = i.Size != null ? i.Size.SortOrder : 0,
            })
            .ToDictionaryAsync(i => i.Id, cancellationToken);
        var warehouseIds = reservations.Select(r => r.WarehouseId).Concat(shipmentItems.Select(i => i.WarehouseId)).Distinct().ToList();
        var warehouses = await db.Warehouses.AsNoTracking().IgnoreQueryFilters()
            .Where(w => warehouseIds.Contains(w.Id)).ToDictionaryAsync(w => w.Id, w => w.Code, cancellationToken);

        var result = new List<ReadyOrderDetailDto>();
        foreach (var order in orders)
        {
            var orderShipments = shipmentItems.Where(i => i.SalesOrderId == order.Id).ToList();
            var keys = reservations.Where(r => r.SalesOrderId == order.Id).Select(r => (r.WarehouseId, r.StockItemId))
                .Concat(orderShipments.Where(i => OpenStatuses.Contains(i.Status)).Select(i => (i.WarehouseId, i.StockItemId)))
                .Distinct();
            var lines = keys
                .Select(key =>
                {
                    var card = cards[key.StockItemId];
                    var ordered = order.Lines
                        .Where(l => l.ProductId == card.ProductId && l.ColorId == card.ColorId && l.SizeId == card.SizeId).Sum(l => l.Quantity);
                    var reserved = reservations
                        .Where(r => r.SalesOrderId == order.Id && r.WarehouseId == key.WarehouseId && r.StockItemId == key.StockItemId)
                        .Sum(r => r.Quantity);
                    var inShipments = orderShipments
                        .Where(i => OpenStatuses.Contains(i.Status) && i.WarehouseId == key.WarehouseId && i.StockItemId == key.StockItemId)
                        .Sum(i => i.Quantity);
                    var shipped = orderShipments
                        .Where(i => i.Status == ShipmentStatus.Shipped && i.StockItemId == key.StockItemId).Sum(i => i.Quantity);
                    return new ReadyOrderLineDto(
                        key.WarehouseId, warehouses.GetValueOrDefault(key.WarehouseId) ?? "?", key.StockItemId, card.Code, card.ProductCode,
                        card.ProductName, card.ColorName, card.SizeName, card.SizeOrder, ordered, shipped, reserved, inShipments,
                        Math.Max(reserved - inShipments, 0));
                })
                .OrderBy(l => l.WarehouseCode, StringComparer.Ordinal)
                .ThenBy(l => l.ProductCode ?? l.ItemCode, StringComparer.Ordinal)
                .ThenBy(l => l.ColorName ?? string.Empty, StringComparer.Ordinal)
                .ThenBy(l => l.SizeOrder)
                .ToList();

            var openShipments = orderShipments
                .Where(i => OpenStatuses.Contains(i.Status))
                .GroupBy(i => new { i.ShipmentId, i.ShipmentNo, i.Status, i.WarehouseCode })
                .Select(g => new ReadyOrderShipmentDto(
                    g.Key.ShipmentId, g.Key.ShipmentNo, g.Key.Status, g.Key.WarehouseCode, g.Sum(i => i.Quantity), g.Sum(i => i.PickedQuantity)))
                .OrderBy(s => s.ShipmentNo, StringComparer.Ordinal)
                .ToList();

            result.Add(new ReadyOrderDetailDto(
                new ReadyOrderDto(
                    order.Id, order.OrderNo, order.CustomerOrderNo, order.CustomerId, order.CustomerName, order.DueDate,
                    order.Lines.Sum(l => l.Quantity), orderShipments.Where(i => i.Status == ShipmentStatus.Shipped).Sum(i => i.Quantity),
                    lines.Sum(l => l.Reserved), lines.Sum(l => l.InShipments), lines.Sum(l => l.Transferable), openShipments.Count,
                    lines.Where(l => l.Transferable > 0).Select(l => l.WarehouseCode).Distinct().ToList()),
                lines,
                openShipments));
        }

        return result;
    }

    /// <summary>Durum değişikliği: rowVersion denetlenir, işlem uygulanır ve kaydedilir.</summary>
    private async Task<Result<ShipmentPickingDto>> TransitionAsync(
        int shipmentId, string rowVersion, Func<Shipment, Error?> apply, CancellationToken cancellationToken)
    {
        var shipment = await db.Shipments.Include(s => s.Items).SingleOrDefaultAsync(s => s.Id == shipmentId, cancellationToken);
        if (shipment is null)
        {
            return NotFound;
        }

        if (RowVersionToken.Check(shipment.RowVersion, rowVersion, ConcurrencyConflict) is { } versionError)
        {
            return versionError;
        }

        if (apply(shipment) is { } error)
        {
            return error;
        }

        return await SaveAsync(shipment, cancellationToken);
    }

    /// <summary>
    /// Toplama işlemi: sevkiyat kilidi altında, kalemleri ve toplama kayıtlarıyla yüklenir. El terminali sürüm taşımaz;
    /// eşzamanlılığı kilit ve üstlenen kullanıcı denetimi sağlar.
    /// </summary>
    private async Task<Result<ShipmentPickingDto>> LockedAsync(
        int shipmentId, Func<Shipment, Task<Error?>> apply, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (!await appLock.TryAcquireAsync($"shipment:{shipmentId}", LockTimeout, cancellationToken))
        {
            return Busy;
        }

        var shipment = await db.Shipments.Include(s => s.Items).Include(s => s.Picks)
            .SingleOrDefaultAsync(s => s.Id == shipmentId, cancellationToken);
        if (shipment is null)
        {
            return NotFound;
        }

        if (await apply(shipment) is { } error)
        {
            return error;
        }

        var saved = await SaveAsync(shipment, cancellationToken);
        if (saved.IsSuccess)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        return saved;
    }

    private async Task<Result<ShipmentPickingDto>> SaveAsync(Shipment shipment, CancellationToken cancellationToken)
    {
        // Kalem ya da toplama değişikliği de sevkiyatın sürümünü ilerletir: açık ekrandaki eski kontrol çalışmaz.
        db.Entry(shipment).State = EntityState.Modified;
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ConcurrencyConflict;
        }

        return await GetAsync(shipment.Id, cancellationToken);
    }

    private bool IsActor(string user) => string.Equals(user, Actor, StringComparison.OrdinalIgnoreCase);

    private static Error ClaimedBy(Shipment shipment) => new(
        ErrorCodes.PickingClaimed,
        $"Bu sevkiyatı {shipment.PickingBy} topluyor. Devralmak için toplamayı devralarak başlatın.",
        ErrorType.Conflict);

    private static Error PickError(string code, string field, string message, ErrorType type = ErrorType.BusinessRule) => new(
        code, message, type, new Dictionary<string, string[]> { [field] = [message] });
}
