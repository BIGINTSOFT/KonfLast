using ERP.Konfeksiyon.Application.Abstractions;
using System.Linq.Expressions;
using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Core.Application.Features.Integrations;
using ERP.Konfeksiyon.Application.Features.Integrations;
using ERP.Core.Application.Features.MasterData;
using ERP.Konfeksiyon.Application.Features.MasterData;
using ERP.Konfeksiyon.Application.Features.Payroll;
using ERP.Konfeksiyon.Application.Features.Warehousing;
using ERP.Core.Domain.Entities;
using ERP.Konfeksiyon.Domain.Entities;
using ERP.Konfeksiyon.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using static ERP.Core.Application.Features.MasterData.MasterDataRules;

namespace ERP.Konfeksiyon.Application.Features.Purchasing;

public sealed class SupplierService(IKonfeksiyonDbContext db) : KonfeksiyonMasterDataService<Supplier, SupplierDto, SupplierRequest>(db)
{
    protected override DbSet<Supplier> Set => Db.Suppliers;
    protected override string Label => "Tedarikçi";
    protected override string CodeField => "supplierCode";

    protected override string CodeOf(SupplierRequest request) => request.SupplierCode;
    protected override Expression<Func<Supplier, bool>> WithCode(string code) => e => e.SupplierCode == code;

    protected override Expression<Func<Supplier, bool>> Matches(string term) =>
        e => e.SupplierCode.Contains(term) || e.Name.Contains(term);

    protected override IQueryable<Supplier> Order(IQueryable<Supplier> query) => query.OrderBy(e => e.SupplierCode);

    protected override Expression<Func<Supplier, SupplierDto>> ToDto => e => new SupplierDto(
        e.Id, e.SupplierCode, e.Name, e.TaxNumber, e.Phone, e.Email, e.Address, e.IsActive);

    protected override Expression<Func<Supplier, LookupDto>> ToLookup => e => new LookupDto(e.Id, e.SupplierCode, e.Name);

    protected override void Apply(SupplierRequest request, Supplier entity, string code)
    {
        entity.SupplierCode = code;
        entity.Name = request.Name.Trim();
        entity.TaxNumber = NullIfBlank(request.TaxNumber);
        entity.Phone = NullIfBlank(request.Phone);
        entity.Email = NullIfBlank(request.Email);
        entity.Address = NullIfBlank(request.Address);
        entity.IsActive = request.IsActive;
    }

    protected override async Task<string?> FindUsageAsync(int id, CancellationToken cancellationToken) =>
        await Db.PurchaseOrders.AnyAsync(o => o.SupplierId == id, cancellationToken) ? "satın alma siparişleri"
        : await Db.StockItems.AnyAsync(i => i.SupplierId == id, cancellationToken) ? "stok kartları"
        : null;
}

public interface IPurchaseOrderService
{
    Task<PagedResult<PurchaseOrderListItemDto>> GetListAsync(PurchaseOrderListQuery query, CancellationToken cancellationToken);
    Task<Result<PurchaseOrderDto>> GetAsync(int id, CancellationToken cancellationToken);
    Task<Result<PurchaseOrderDto>> CreateAsync(PurchaseOrderRequest request, CancellationToken cancellationToken);
    Task<Result<PurchaseOrderDto>> UpdateAsync(int id, PurchaseOrderRequest request, CancellationToken cancellationToken);

    /// <summary>Siparişi hedef duruma geçirir (onayla, taslağa al, iptal et, eksik kapat).</summary>
    Task<Result<PurchaseOrderDto>> TransitionAsync(
        int id, PurchaseOrderStatus target, string rowVersion, CancellationToken cancellationToken);

    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken);

    /// <summary>Siparişin kalemlerini (kısmen ya da tamamen) bir depoya teslim alır; stok aynı işlemde artar.</summary>
    Task<Result<GoodsReceiptResultDto>> ReceiveAsync(int id, GoodsReceiptRequest request, CancellationToken cancellationToken);

    /// <summary>Siparişin mal kabulleri, eskiden yeniye.</summary>
    Task<Result<IReadOnlyList<GoodsReceiptDto>>> GetReceiptsAsync(int id, CancellationToken cancellationToken);
}

/// <summary>
/// Satın alma siparişleri ve mal kabul. Durum kuralları <see cref="PurchaseOrder"/> içindedir. Mal kabul;
/// sipariş kilidi altında, teslim alınan miktarı, mal kabul kaydını ve stok girişini tek transaction'da yazar.
/// </summary>
public sealed class PurchaseOrderService(
    IKonfeksiyonDbContext db,
    IAppLock appLock,
    IStockLedger stockLedger,
    IPayrollService payroll,
    IIntegrationOutbox outbox,
    IDocumentNumberGenerator numberGenerator,
    ICurrentUser currentUser,
    ICurrentPermissions permissions,
    TimeProvider timeProvider,
    IOptions<PurchasingOptions> options) : IPurchaseOrderService
{
    private static readonly Error CannotSeePrices = new(
        ErrorCodes.Forbidden,
        "Satın alma siparişini düzenlemek için fiyatları görme yetkisi gerekir (Satın alma fiyatlarını görme).",
        ErrorType.Forbidden);

    private Task<bool> CanSeePricesAsync(CancellationToken cancellationToken) =>
        permissions.HasAsync(ERP.Konfeksiyon.Domain.Authorization.KonfeksiyonPermissions.Purchase.ViewPrices, cancellationToken);

    private const string DefaultCurrency = "TRY";
    private const string SystemUser = "system";
    private static readonly TimeSpan LockTimeout = TimeSpan.FromSeconds(10);

    private static readonly Error NotFound = new(ErrorCodes.NotFound, "Satın alma siparişi bulunamadı.", ErrorType.NotFound);

    private static readonly Error ConcurrencyConflict = new(
        ErrorCodes.ConcurrencyConflict,
        "Sipariş siz açtıktan sonra başka bir kullanıcı tarafından değiştirildi. Sayfayı yenileyip tekrar deneyin.",
        ErrorType.Conflict);

    private static readonly IReadOnlyDictionary<PurchaseOrderStatus, string> StatusLabels =
        new Dictionary<PurchaseOrderStatus, string>
        {
            [PurchaseOrderStatus.Draft] = "Taslak",
            [PurchaseOrderStatus.Approved] = "Onaylı",
            [PurchaseOrderStatus.PartiallyReceived] = "Kısmen teslim alındı",
            [PurchaseOrderStatus.Received] = "Teslim alındı",
            [PurchaseOrderStatus.Closed] = "Kapatıldı",
            [PurchaseOrderStatus.Cancelled] = "İptal",
        };

    private readonly PurchasingOptions _options = options.Value;

    public async Task<PagedResult<PurchaseOrderListItemDto>> GetListAsync(
        PurchaseOrderListQuery query, CancellationToken cancellationToken)
    {
        // Silinmiş tedarikçi de adıyla görünür; sipariş kendisi silinmemiş olmalıdır.
        var orders = db.PurchaseOrders.AsNoTracking().IgnoreQueryFilters().Where(o => !o.IsDeleted);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            orders = orders.Where(o => o.OrderNo.Contains(search) || o.Supplier.Name.Contains(search));
        }

        if (query.Status is { } status)
        {
            orders = orders.Where(o => o.Status == status);
        }

        if (query.SupplierId is { } supplierId)
        {
            orders = orders.Where(o => o.SupplierId == supplierId);
        }

        if (query.Receivable is { } receivable)
        {
            orders = orders.Where(o =>
                (o.Status == PurchaseOrderStatus.Approved || o.Status == PurchaseOrderStatus.PartiallyReceived) == receivable);
        }

        var page = await orders
            .OrderByDescending(o => o.Id)
            .Select(o => new PurchaseOrderListItemDto(
                o.Id,
                o.OrderNo,
                o.SupplierId,
                o.Supplier.Name,
                o.OrderDate,
                o.ExpectedDate,
                o.Status,
                o.Items.Count,
                o.Items.Sum(i => i.Quantity * i.UnitPrice),
                o.Currency))
            .ToPagedResultAsync(query, cancellationToken);

        return await CanSeePricesAsync(cancellationToken)
            ? page
            : page with { Items = page.Items.Select(o => o with { TotalAmount = null }).ToList() };
    }

    public async Task<Result<PurchaseOrderDto>> GetAsync(int id, CancellationToken cancellationToken)
    {
        var order = await db.PurchaseOrders
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Include(o => o.Supplier)
            .Include(o => o.Items).ThenInclude(i => i.StockItem)
            .Include(o => o.Items).ThenInclude(i => i.ProductionOrder)
            .SingleOrDefaultAsync(o => o.Id == id && !o.IsDeleted, cancellationToken);

        if (order is null)
        {
            return NotFound;
        }

        var dto = ToDto(order, (await payroll.LoadSettingAsync(cancellationToken)).Currency);
        // Fiyat görme yetkisi yoksa miktarlar ve teslim durumu görünür, fiyat ve tutarlar boş döner.
        return await CanSeePricesAsync(cancellationToken)
            ? dto
            : dto with
            {
                TotalAmount = null,
                ExchangeRate = null,
                Items = dto.Items.Select(i => i with { UnitPrice = null, LineAmount = null }).ToList(),
            };
    }

    public async Task<Result<PurchaseOrderDto>> CreateAsync(PurchaseOrderRequest request, CancellationToken cancellationToken)
    {
        if (!await CanSeePricesAsync(cancellationToken))
        {
            return CannotSeePrices;
        }

        if (await ValidateAsync(request, null, cancellationToken) is { } error)
        {
            return error;
        }

        var order = new PurchaseOrder();
        Apply(request, order);
        foreach (var item in request.Items)
        {
            order.Items.Add(new PurchaseOrderItem
            {
                StockItemId = item.StockItemId, Quantity = item.Quantity, UnitPrice = item.UnitPrice,
                ProductionOrderId = item.ProductionOrderId,
            });
        }

        // Numara ve sipariş aynı transaction'da: kayıt başarısız olursa numara da geri alınır.
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var year = timeProvider.GetLocalNow().Year;
        var sequence = await numberGenerator.NextAsync(DocumentSeries.PurchaseOrder, year, cancellationToken);
        order.OrderNo = $"{_options.NumberPrefix}-{year}-{sequence.ToString().PadLeft(_options.NumberDigits, '0')}";

        db.PurchaseOrders.Add(order);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return await GetAsync(order.Id, cancellationToken);
    }

    public async Task<Result<PurchaseOrderDto>> UpdateAsync(
        int id, PurchaseOrderRequest request, CancellationToken cancellationToken)
    {
        // Fiyatları göremeyen kullanıcının formu fiyatları boş gönderir; kaydedilirse mevcut fiyatlar silinirdi.
        if (!await CanSeePricesAsync(cancellationToken))
        {
            return CannotSeePrices;
        }

        var order = await db.PurchaseOrders.Include(o => o.Items).SingleOrDefaultAsync(o => o.Id == id, cancellationToken);
        if (order is null)
        {
            return NotFound;
        }

        if (RowVersionToken.Check(order.RowVersion, request.RowVersion, ConcurrencyConflict) is { } versionError)
        {
            return versionError;
        }

        if (!order.IsEditable)
        {
            return new Error(
                ErrorCodes.PurchaseOrderNotEditable,
                $"{StatusLabels[order.Status]} durumundaki sipariş değiştirilemez. Mal kabul yapılmadıysa taslağa alabilirsiniz.");
        }

        if (await ValidateAsync(request, order, cancellationToken) is { } error)
        {
            return error;
        }

        Apply(request, order);
        SyncItems(order, request.Items);

        return await SaveAndReloadAsync(order, cancellationToken);
    }

    public async Task<Result<PurchaseOrderDto>> TransitionAsync(
        int id, PurchaseOrderStatus target, string rowVersion, CancellationToken cancellationToken)
    {
        var order = await db.PurchaseOrders.Include(o => o.Items).SingleOrDefaultAsync(o => o.Id == id, cancellationToken);
        if (order is null)
        {
            return NotFound;
        }

        if (RowVersionToken.Check(order.RowVersion, rowVersion, ConcurrencyConflict) is { } versionError)
        {
            return versionError;
        }

        if (!order.CanTransitionTo(target))
        {
            return new Error(
                ErrorCodes.InvalidStatusTransition,
                $"{StatusLabels[order.Status]} durumundaki sipariş '{StatusLabels[target]}' durumuna geçirilemez.");
        }

        if (target == PurchaseOrderStatus.Approved && order.Items.Count == 0)
        {
            return new Error(ErrorCodes.OrderHasNoItems, "Kalemi olmayan sipariş onaylanamaz.");
        }

        // Dövizli alış kur olmadan stok maliyetine işlenemez; fiyatlı kalemi olan sipariş kursuz onaylanmaz.
        if (target == PurchaseOrderStatus.Approved && order.ExchangeRate is null && order.Items.Any(i => i.UnitPrice is not null))
        {
            var costCurrency = (await payroll.LoadSettingAsync(cancellationToken)).Currency;
            if (order.Currency != costCurrency)
            {
                var message = $"Sipariş {order.Currency} para biriminde; {costCurrency} karşılığı kur girilmeden onaylanamaz.";
                return new Error(
                    ErrorCodes.ExchangeRateRequired, message, ErrorType.BusinessRule,
                    new Dictionary<string, string[]> { ["exchangeRate"] = [message] });
            }
        }

        order.TransitionTo(target);
        return await SaveAndReloadAsync(order, cancellationToken);
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var order = await db.PurchaseOrders.SingleOrDefaultAsync(o => o.Id == id, cancellationToken);
        if (order is null)
        {
            return NotFound;
        }

        if (!order.IsEditable)
        {
            return new Error(
                ErrorCodes.PurchaseOrderNotEditable,
                $"{StatusLabels[order.Status]} durumundaki sipariş silinemez; bunun yerine iptal edin.");
        }

        // Soft delete: kayıt ve numarası korunur, numara yeniden kullanılmaz.
        db.PurchaseOrders.Remove(order);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<GoodsReceiptResultDto>> ReceiveAsync(
        int id, GoodsReceiptRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        // Aynı siparişe eşzamanlı iki mal kabul, kalan miktarı birlikte aşamasın.
        if (!await appLock.TryAcquireAsync($"purchase-order:{id}", LockTimeout, cancellationToken))
        {
            return new Error(
                ErrorCodes.ResourceBusy, "Bu sipariş için şu an başka bir mal kabul işleniyor. Lütfen tekrar deneyin.",
                ErrorType.Conflict);
        }

        var replay = await db.GoodsReceipts
            .Where(r => r.RequestId == request.RequestId)
            .Select(r => new { r.Id, r.PurchaseOrderId })
            .SingleOrDefaultAsync(cancellationToken);
        if (replay is not null)
        {
            return replay.PurchaseOrderId == id
                ? await LoadResultAsync(replay.Id, id, duplicate: true, cancellationToken)
                : new Error(ErrorCodes.RequestIdReused, "Bu istek kimliği başka bir işlem için kullanılmış.", ErrorType.Conflict);
        }

        var order = await db.PurchaseOrders.Include(o => o.Items).SingleOrDefaultAsync(o => o.Id == id, cancellationToken);
        if (order is null)
        {
            return NotFound;
        }

        if (!order.CanReceive)
        {
            return new Error(
                ErrorCodes.PurchaseOrderNotReceivable,
                order.Status == PurchaseOrderStatus.Draft
                    ? "Sipariş henüz onaylanmamış; mal kabul için önce onaylanmalıdır."
                    : $"{StatusLabels[order.Status]} durumundaki siparişe mal kabul yapılamaz.");
        }

        var stockItemIds = order.Items.Select(i => i.StockItemId).ToList();
        var stockItems = await db.StockItems
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(i => stockItemIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, cancellationToken);

        var receipt = new GoodsReceipt
        {
            RequestId = request.RequestId,
            PurchaseOrderId = order.Id,
            WarehouseId = request.WarehouseId,
            DocumentNo = NullIfBlank(request.DocumentNo),
            Note = NullIfBlank(request.Note),
            ReceivedAt = timeProvider.GetUtcNow().UtcDateTime,
            ReceivedBy = currentUser.UserName ?? SystemUser,
        };

        foreach (var line in request.Lines)
        {
            var item = order.Items.SingleOrDefault(i => i.Id == line.PurchaseOrderItemId);
            if (item is null)
            {
                return ReferenceValidation.InvalidReference("lines", "Teslim alınan kalem bu siparişe ait değil.");
            }

            var stockItem = stockItems[item.StockItemId];
            if (!stockItem.Accepts(line.Quantity))
            {
                var message = $"{stockItem.Code}: adetle izlenen stok kartında miktar tam sayı olmalıdır.";
                return new Error(
                    ErrorCodes.StockQuantityInvalid, message, ErrorType.Validation,
                    new Dictionary<string, string[]> { ["lines"] = [message] });
            }

            var receivable = item.ReceivableQuantity(_options.OverReceiptTolerancePercent);
            if (line.Quantity > receivable)
            {
                var message =
                    $"{stockItem.Code}: sipariş edilen {item.Quantity:0.###}, daha önce teslim alınan {item.ReceivedQuantity:0.###}; " +
                    $"en fazla {receivable:0.###} teslim alınabilir.";
                return new Error(
                    ErrorCodes.ReceiptExceedsOrdered, message, ErrorType.BusinessRule,
                    new Dictionary<string, string[]> { ["lines"] = [message] });
            }

            order.Receive(item, line.Quantity, _options.OverReceiptTolerancePercent);
            receipt.Lines.Add(new GoodsReceiptLine { PurchaseOrderItemId = item.Id, Quantity = line.Quantity });
        }

        db.GoodsReceipts.Add(receipt);
        var costCurrency = (await payroll.LoadSettingAsync(cancellationToken)).Currency;

        var stockError = await stockLedger.PostPurchaseReceiptAsync(
            request.WarehouseId,
            order.Id,
            request.Lines
                .Select(l =>
                {
                    var item = order.Items.Single(i => i.Id == l.PurchaseOrderItemId);
                    // Ortalama maliyet sistem para biriminde tutulur: dövizli alış fiyatı siparişin kuruyla çevrilir
                    // (kuru olmayan eski siparişte maliyete işlenmez).
                    var unitCost = order.Currency == costCurrency
                        ? item.UnitPrice
                        : item.UnitPrice is { } price && order.ExchangeRate is { } rate ? decimal.Round(price * rate, 4) : null;
                    return new PurchaseReceiptLine(
                        item.StockItemId, l.Quantity, unitCost,
                        l.Lots?
                            .Select(lot => new ReceivedLot(
                                lot.Quantity, new StockLotRequest(lot.BatchNo, lot.Shade, lot.Width, lot.SupplierRollNo)))
                            .ToList());
                })
                .ToList(),
            // İrsaliye numarası yoksa hareket, sipariş numarasıyla izlenir.
            receipt.DocumentNo ?? order.OrderNo,
            receipt.Note,
            cancellationToken);
        if (stockError is not null)
        {
            return stockError;
        }

        // Yalnızca kalemler değişse de başlık güncellenir: rowversion ilerler.
        db.Entry(order).State = EntityState.Modified;
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ConcurrencyConflict;
        }
        catch (DbUpdateException)
        {
            // Tekil istek kimliği: aynı istek eşzamanlı olarak başka bir bağlantıdan işlendi.
            return new Error(
                ErrorCodes.ResourceBusy, "İşlem aynı anda başka bir istekle yapıldı. Lütfen siparişi yenileyip kontrol edin.",
                ErrorType.Conflict);
        }

        // Mal kabulün kimliği kayıttan sonra bellidir; olay aynı transaction'da ayrıca yazılır.
        await EnqueueReceivedAsync(order, receipt, stockItems, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return await LoadResultAsync(receipt.Id, id, duplicate: false, cancellationToken);
    }

    public async Task<Result<IReadOnlyList<GoodsReceiptDto>>> GetReceiptsAsync(int id, CancellationToken cancellationToken)
    {
        if (!await db.PurchaseOrders.AnyAsync(o => o.Id == id, cancellationToken))
        {
            return NotFound;
        }

        return Result<IReadOnlyList<GoodsReceiptDto>>.Success(
            await ProjectReceipts(db.GoodsReceipts.Where(r => r.PurchaseOrderId == id).OrderBy(r => r.Id))
                .ToListAsync(cancellationToken));
    }

    /// <summary>Mal kabul olayını dış sistemlere duyurulmak üzere yazar.</summary>
    private async Task EnqueueReceivedAsync(
        PurchaseOrder order, GoodsReceipt receipt, IReadOnlyDictionary<int, StockItem> stockItems,
        CancellationToken cancellationToken)
    {
        var supplier = await db.Suppliers
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(s => s.Id == order.SupplierId)
            .Select(s => new PartyPayload(s.SupplierCode, s.Name, s.TaxNumber))
            .SingleAsync(cancellationToken);
        var warehouseCode = await db.Warehouses
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(w => w.Id == receipt.WarehouseId)
            .Select(w => w.Code)
            .SingleAsync(cancellationToken);

        var items = order.Items.ToDictionary(i => i.Id);
        outbox.Enqueue(
            IntegrationEventTypes.GoodsReceiptPosted,
            nameof(GoodsReceipt),
            receipt.Id,
            receipt.DocumentNo ?? order.OrderNo,
            new GoodsReceiptPostedPayload(
                order.OrderNo,
                order.Status,
                receipt.ReceivedAt,
                receipt.ReceivedBy,
                receipt.DocumentNo,
                receipt.Note,
                supplier,
                warehouseCode,
                order.Currency,
                receipt.Lines
                    .Select(line =>
                    {
                        var item = items[line.PurchaseOrderItemId];
                        var stockItem = stockItems[item.StockItemId];
                        return new StockLinePayload(stockItem.Code, stockItem.Name, stockItem.Unit, line.Quantity, item.UnitPrice);
                    })
                    .OrderBy(l => l.ItemCode, StringComparer.Ordinal)
                    .ToList()));
    }

    private async Task<Result<GoodsReceiptResultDto>> LoadResultAsync(
        long receiptId, int orderId, bool duplicate, CancellationToken cancellationToken)
    {
        var receipt = await ProjectReceipts(db.GoodsReceipts.Where(r => r.Id == receiptId)).SingleAsync(cancellationToken);
        var order = await GetAsync(orderId, cancellationToken);
        return new GoodsReceiptResultDto(duplicate, receipt, order.Value);
    }

    private async Task<Error?> ValidateAsync(
        PurchaseOrderRequest request, PurchaseOrder? existing, CancellationToken cancellationToken)
    {
        var stockItemIds = request.Items.Select(i => i.StockItemId).Distinct().ToList();
        var referenceError =
            await ReferenceValidation.RequireSelectableAsync(
                db.Suppliers, request.SupplierId, existing?.SupplierId, "supplierId", "Tedarikçi", cancellationToken)
            ?? await ReferenceValidation.RequireSelectableAsync(
                db.StockItems, stockItemIds, existing?.Items.Select(i => i.StockItemId).ToHashSet() ?? [], "items",
                "Stok kartı", cancellationToken);
        if (referenceError is not null)
        {
            return referenceError;
        }

        // Kalemin bağlandığı üretim siparişi var olmalı ve iptal edilmemiş olmalı.
        var productionIds = request.Items.Where(i => i.ProductionOrderId != null).Select(i => i.ProductionOrderId!.Value).Distinct().ToList();
        if (productionIds.Count > 0
            && await db.ProductionOrders.CountAsync(p => productionIds.Contains(p.Id) && p.Status != ProductionOrderStatus.Cancelled, cancellationToken)
                != productionIds.Count)
        {
            return ReferenceValidation.InvalidReference("items", "Kalemin bağlandığı iş emri bulunamadı ya da iptal edilmiş.");
        }

        var stockItems = await db.StockItems
            .AsNoTracking()
            .Where(i => stockItemIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, cancellationToken);
        var fractional = request.Items.FirstOrDefault(i => !stockItems[i.StockItemId].Accepts(i.Quantity));
        if (fractional is not null)
        {
            var message = $"{stockItems[fractional.StockItemId].Code}: adetle izlenen stok kartında miktar tam sayı olmalıdır.";
            return new Error(
                ErrorCodes.StockQuantityInvalid, message, ErrorType.Validation,
                new Dictionary<string, string[]> { ["items"] = [message] });
        }

        return null;
    }

    private static void Apply(PurchaseOrderRequest request, PurchaseOrder order)
    {
        order.SupplierId = request.SupplierId;
        order.OrderDate = request.OrderDate;
        order.ExpectedDate = request.ExpectedDate;
        order.Currency = string.IsNullOrWhiteSpace(request.Currency) ? DefaultCurrency : request.Currency.Trim().ToUpperInvariant();
        order.ExchangeRate = request.ExchangeRate;
        order.Notes = NullIfBlank(request.Notes);
    }

    /// <summary>Kalemleri istekle eşitler: kalan kalem güncellenir, eksilen silinir, yeni gelen eklenir.</summary>
    private void SyncItems(PurchaseOrder order, IReadOnlyList<PurchaseOrderItemRequest> requested)
    {
        var wanted = requested.ToDictionary(i => i.StockItemId);

        foreach (var item in order.Items.ToList())
        {
            if (wanted.Remove(item.StockItemId, out var request))
            {
                item.Quantity = request.Quantity;
                item.UnitPrice = request.UnitPrice;
                item.ProductionOrderId = request.ProductionOrderId;
            }
            else
            {
                db.PurchaseOrderItems.Remove(item);
            }
        }

        foreach (var request in wanted.Values)
        {
            db.PurchaseOrderItems.Add(new PurchaseOrderItem
            {
                PurchaseOrderId = order.Id, StockItemId = request.StockItemId, Quantity = request.Quantity,
                UnitPrice = request.UnitPrice, ProductionOrderId = request.ProductionOrderId,
            });
        }
    }

    private async Task<Result<PurchaseOrderDto>> SaveAndReloadAsync(PurchaseOrder order, CancellationToken cancellationToken)
    {
        // Yalnızca kalemler değişse bile başlık satırı güncellenir: rowversion ilerler ve yükleme ile kayıt
        // arasındaki eşzamanlı değişiklikleri veritabanı yakalar.
        db.Entry(order).State = EntityState.Modified;

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ConcurrencyConflict;
        }

        return await GetAsync(order.Id, cancellationToken);
    }

    private static PurchaseOrderDto ToDto(PurchaseOrder order, string costCurrency)
    {
        var items = order.Items
            .OrderBy(i => i.StockItem.Code, StringComparer.Ordinal)
            .Select(i => new PurchaseOrderItemDto(
                i.Id, i.StockItemId, i.StockItem.Code, i.StockItem.Name, i.StockItem.Unit, i.Quantity, i.UnitPrice,
                i.ReceivedQuantity, i.RemainingQuantity,
                i.UnitPrice is null ? null : decimal.Round(i.Quantity * i.UnitPrice.Value, 2), i.StockItem.IsLotTracked,
                i.ProductionOrderId, i.ProductionOrder?.OrderNo))
            .ToList();

        return new PurchaseOrderDto(
            order.Id,
            order.OrderNo,
            order.SupplierId,
            order.Supplier.SupplierCode,
            order.Supplier.Name,
            order.OrderDate,
            order.ExpectedDate,
            order.Currency,
            order.Status,
            order.Notes,
            items.Any(i => i.LineAmount is not null) ? items.Sum(i => i.LineAmount ?? 0) : null,
            order.IsEditable,
            order.CanReceive,
            order.AllowedTransitions,
            RowVersionToken.From(order.RowVersion),
            order.CreatedAt,
            order.CreatedBy,
            items,
            order.ExchangeRate,
            costCurrency);
    }

    // Geçmiş mal kabuller, sonradan silinen depo / kart için de eksiksiz görünür.
    private static IQueryable<GoodsReceiptDto> ProjectReceipts(IQueryable<GoodsReceipt> receipts) =>
        receipts.AsNoTracking().IgnoreQueryFilters().Select(r => new GoodsReceiptDto(
            r.Id,
            r.PurchaseOrderId,
            r.Warehouse.Code,
            r.Warehouse.Name,
            r.DocumentNo,
            r.Note,
            r.ReceivedAt,
            r.ReceivedBy,
            r.Lines
                .OrderBy(l => l.Id)
                .Select(l => new GoodsReceiptLineDto(
                    l.PurchaseOrderItemId, l.PurchaseOrderItem.StockItem.Code, l.PurchaseOrderItem.StockItem.Name,
                    l.PurchaseOrderItem.StockItem.Unit, l.Quantity))
                .ToList()));
}
