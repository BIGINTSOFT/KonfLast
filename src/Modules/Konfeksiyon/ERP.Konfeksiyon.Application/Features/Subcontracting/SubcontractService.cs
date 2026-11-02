using System.Globalization;
using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Konfeksiyon.Application.Abstractions;
using ERP.Konfeksiyon.Application.Features.Bundles;
using ERP.Konfeksiyon.Application.Features.Sales;
using ERP.Konfeksiyon.Domain.Entities;
using ERP.Konfeksiyon.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using static ERP.Core.Application.Features.MasterData.MasterDataRules;

namespace ERP.Konfeksiyon.Application.Features.Subcontracting;

public interface ISubcontractService
{
    /// <summary>Etkin rotalarda fason olarak işaretlenmiş operasyonlar ve varsayılan fasoncuları.</summary>
    Task<IReadOnlyList<SubcontractOperationOptionDto>> GetOperationOptionsAsync(CancellationToken cancellationToken);

    /// <summary>Seçilen operasyon için fasona gönderilebilecek açık demetler.</summary>
    Task<IReadOnlyList<SubcontractCandidateDto>> GetCandidatesAsync(SubcontractCandidateQuery query, CancellationToken cancellationToken);

    Task<PagedResult<SubcontractSendListItemDto>> GetSendsAsync(SubcontractSendListQuery query, CancellationToken cancellationToken);
    Task<Result<SubcontractSendDto>> GetSendAsync(int id, CancellationToken cancellationToken);

    /// <summary>Basılı gönderim irsaliyesi için fiş, gönderen ve alıcı.</summary>
    Task<Result<SubcontractSendDocumentDto>> GetSendDocumentAsync(int id, CancellationToken cancellationToken);

    /// <summary>
    /// Gönderim fişi oluşturur: demet satırlarındaki demetler fasoncuda sayılır (sahada işlenemez); fiyatlar gönderim
    /// tarihindeki fason fiyatından kopyalanır.
    /// </summary>
    Task<Result<SubcontractSendDto>> CreateSendAsync(SubcontractSendRequest request, CancellationToken cancellationToken);

    /// <summary>Hiç dönüşü işlenmemiş fişi iptal eder; demetler sahaya döner.</summary>
    Task<Result<SubcontractSendDto>> CancelSendAsync(int id, string rowVersion, CancellationToken cancellationToken);

    Task<PagedResult<SubcontractReturnListItemDto>> GetReturnsAsync(SubcontractReturnListQuery query, CancellationToken cancellationToken);
    Task<Result<SubcontractReturnDto>> GetReturnAsync(int id, CancellationToken cancellationToken);

    /// <summary>
    /// Dönüş fişi işler: sağlam adet demet operasyonuna üretilmiş, hatalı ve eksik adet hatalı olarak yazılır (sahadaki
    /// bildirimle aynı akış). Satırın tamamı döndüyse demet sahaya döner. Kısmi dönüş yapılabilir.
    /// </summary>
    Task<Result<SubcontractReturnDto>> CreateReturnAsync(SubcontractReturnRequest request, CancellationToken cancellationToken);

    /// <summary>Ödenmemiş hakedişin satır fiyatlarını düzeltir.</summary>
    Task<Result<SubcontractReturnDto>> UpdateReturnPricesAsync(
        int id, SubcontractReturnPricesRequest request, CancellationToken cancellationToken);

    /// <summary>Hakedişi ödendi olarak işaretler ya da işareti kaldırır.</summary>
    Task<Result<SubcontractReturnDto>> SetPaymentAsync(int id, SubcontractPaymentRequest request, CancellationToken cancellationToken);

    /// <summary>Fasoncu ve dönem bazında hakediş toplamları (ödenen / ödenmeyen).</summary>
    Task<SubcontractStatementDto> GetStatementAsync(SubcontractStatementQuery query, CancellationToken cancellationToken);
}

/// <summary>
/// Fason (dışarıda yaptırılan) operasyonlar: gönderim ve dönüş fişleri, hakediş.
///
/// Üretim akışı: gönderilen demet <see cref="Bundle.AtSubcontractor"/> olur ve sahada (tablet / elle bildirim)
/// başlatılamaz. Dönüşte sağlam adet, sahadaki bildirimle aynı alan kuralıyla (<see cref="Bundle.ReportSubcontracted"/>)
/// operasyona işlenir: plan, ardıl operasyonlar ve demet durumu aynı şekilde güncellenir. Makine ve operatör olmadığı
/// için üretim olayı (<see cref="BundleOperationEvent"/>) yazılmaz: fason adetleri operatör çıktısına, verime ve parça
/// başı ücrete girmez; izi gönderim / dönüş satırlarıdır.
/// </summary>
public sealed class SubcontractService(
    IKonfeksiyonDbContext db,
    IAppLock appLock,
    IDocumentNumberGenerator numbers,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : ISubcontractService
{
    private const string SendSeries = "SubcontractSend";
    private const string SendPrefix = "FG";
    private const string ReturnSeries = "SubcontractReturn";
    private const string ReturnPrefix = "FD";
    private const string SystemUser = "system";
    private static readonly TimeSpan LockTimeout = TimeSpan.FromSeconds(15);

    private static readonly Error SendNotFound = new(ErrorCodes.NotFound, "Fason gönderim fişi bulunamadı.", ErrorType.NotFound);
    private static readonly Error ReturnNotFound = new(ErrorCodes.NotFound, "Fason dönüş fişi bulunamadı.", ErrorType.NotFound);

    private static readonly Error ConcurrencyConflict = new(
        ErrorCodes.ConcurrencyConflict,
        "Fiş siz açtıktan sonra değişti. Sayfayı yenileyip tekrar deneyin.",
        ErrorType.Conflict);

    private static readonly Error Busy = new(
        ErrorCodes.ResourceBusy, "Demet ya da fiş şu an başka bir işlemle kaydediliyor. Lütfen tekrar deneyin.", ErrorType.Conflict);

    /// <summary>Fasona gönderilebilecek (sahada açık) demet durumları.</summary>
    private static readonly BundleStatus[] SendableBundleStatuses =
    [
        BundleStatus.Created, BundleStatus.Waiting, BundleStatus.InProduction, BundleStatus.PartiallyCompleted,
        BundleStatus.Rework,
    ];

    private DateTime UtcNow => timeProvider.GetUtcNow().UtcDateTime;

    private DateOnly Today() => DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);

    private string UserName => currentUser.UserName ?? SystemUser;

    // ------------------------------------------------------------------ Gönderim

    public async Task<IReadOnlyList<SubcontractOperationOptionDto>> GetOperationOptionsAsync(CancellationToken cancellationToken)
    {
        var steps = await db.ProductionRouteOperations
            .AsNoTracking()
            .Where(o => o.IsSubcontracted && o.ProductionRoute.Status == RouteStatus.Active)
            .Select(o => new
            {
                o.OperationId,
                o.Operation.OperationCode,
                o.Operation.Name,
                o.SubcontractorId,
                SupplierName = o.Subcontractor != null ? o.Subcontractor.Name : null,
            })
            .ToListAsync(cancellationToken);

        return steps
            .GroupBy(s => s.OperationId)
            .Select(g =>
            {
                var first = g.First();
                // Birden fazla rotada farklı fasoncu seçilmişse en çok kullanılan önerilir.
                var supplier = g
                    .Where(s => s.SubcontractorId is not null)
                    .GroupBy(s => (s.SubcontractorId, s.SupplierName))
                    .OrderByDescending(x => x.Count())
                    .Select(x => x.Key)
                    .FirstOrDefault();
                return new SubcontractOperationOptionDto(
                    g.Key, first.OperationCode, first.Name, supplier.SubcontractorId, supplier.SupplierName);
            })
            .OrderBy(o => o.OperationCode, StringComparer.Ordinal)
            .ToList();
    }

    public async Task<IReadOnlyList<SubcontractCandidateDto>> GetCandidatesAsync(
        SubcontractCandidateQuery query, CancellationToken cancellationToken)
    {
        var take = Math.Clamp(query.Take, 1, 500);

        // Bundle.CheckStart (kartsız) ile aynı koşullar: operasyon hazır / bekletilmiş ve kalanı var, demette süren ya da
        // yarım bırakılmış başka operasyon yok, demet açık ve fasoncuda değil, sipariş üretime açık.
        var operations = db.BundleOperations
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(o => o.OperationId == query.OperationId
                && (o.Status == BundleOperationStatus.Ready || o.Status == BundleOperationStatus.Paused)
                && o.PlannedQuantity - o.ProducedQuantity - o.RejectedQuantity > 0
                && SendableBundleStatuses.Contains(o.Bundle.Status)
                && !o.Bundle.AtSubcontractor
                && !o.Bundle.ProductionOrder.IsDeleted
                && (o.Bundle.ProductionOrder.Status == ProductionOrderStatus.Released
                    || o.Bundle.ProductionOrder.Status == ProductionOrderStatus.InProduction)
                && !o.Bundle.Operations.Any(x => x.Id != o.Id
                    && (x.Status == BundleOperationStatus.InProgress
                        || (x.Status == BundleOperationStatus.Paused
                            && x.PlannedQuantity - x.ProducedQuantity - x.RejectedQuantity - x.ReworkQuantity > 0))));

        if (query.ProductionOrderId is { } orderId)
        {
            operations = operations.Where(o => o.Bundle.ProductionOrderId == orderId);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            operations = operations.Where(o =>
                o.Bundle.BundleNo.Contains(search)
                || o.Bundle.ProductionOrder.OrderNo.Contains(search)
                || o.Bundle.ProductionOrder.Product.ProductCode.Contains(search)
                || o.Bundle.ProductionOrder.Product.Model.ModelCode.Contains(search));
        }

        return await operations
            .OrderBy(o => o.Bundle.ProductionOrderId).ThenBy(o => o.BundleId)
            .Take(take)
            .Select(o => new SubcontractCandidateDto(
                o.Id,
                o.BundleId,
                o.Bundle.BundleNo,
                o.Bundle.ProductionOrderId,
                o.Bundle.ProductionOrder.OrderNo,
                o.Bundle.ProductionOrder.ProductId,
                o.Bundle.ProductionOrder.Product.ProductCode,
                o.Bundle.ProductionOrder.Product.Model.ModelCode,
                o.Bundle.ColorId,
                o.Bundle.Color.Name,
                o.Bundle.SizeId,
                o.Bundle.Size.Name,
                o.SequenceNo,
                o.PlannedQuantity - o.ProducedQuantity - o.RejectedQuantity,
                o.RouteOperation.IsSubcontracted,
                o.RouteOperation.SubcontractorId))
            .ToListAsync(cancellationToken);
    }

    public Task<PagedResult<SubcontractSendListItemDto>> GetSendsAsync(
        SubcontractSendListQuery query, CancellationToken cancellationToken)
    {
        var sends = db.SubcontractSends.AsNoTracking().IgnoreQueryFilters();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            sends = sends.Where(s => s.SendNo.Contains(search) || s.Supplier.Name.Contains(search)
                || s.Supplier.SupplierCode.Contains(search) || s.Operation.Name.Contains(search)
                || s.Operation.OperationCode.Contains(search));
        }

        if (query.SupplierId is { } supplierId)
        {
            sends = sends.Where(s => s.SupplierId == supplierId);
        }

        if (query.Status is { } status)
        {
            sends = sends.Where(s => s.Status == status);
        }

        if (query.OpenOnly == true)
        {
            sends = sends.Where(s => s.Status == SubcontractSendStatus.Open || s.Status == SubcontractSendStatus.PartiallyReturned);
        }

        return sends
            .OrderByDescending(s => s.SendDate).ThenByDescending(s => s.Id)
            .Select(s => new SubcontractSendListItemDto(
                s.Id,
                s.SendNo,
                s.SupplierId,
                s.Supplier.Name,
                s.SendDate,
                s.OperationId,
                s.Operation.OperationCode,
                s.Operation.Name,
                s.Status,
                s.Lines.Count,
                s.Lines.Sum(l => (int?)l.Quantity) ?? 0,
                s.Lines.Sum(l => (int?)l.ReturnedGood) ?? 0,
                s.Lines.Sum(l => (int?)l.ReturnedDefective) ?? 0,
                s.Lines.Sum(l => (int?)l.ReturnedMissing) ?? 0,
                s.Status == SubcontractSendStatus.Cancelled
                    ? 0
                    : (s.Lines.Sum(l => (int?)(l.Quantity - l.ReturnedGood - l.ReturnedDefective - l.ReturnedMissing)) ?? 0),
                s.CreatedBy))
            .ToPagedResultAsync(query, cancellationToken);
    }

    public async Task<Result<SubcontractSendDto>> GetSendAsync(int id, CancellationToken cancellationToken)
    {
        // Sonradan silinen fasoncu / ürün / renk / beden de adıyla görünür.
        var send = await db.SubcontractSends
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Include(s => s.Supplier)
            .Include(s => s.Operation)
            .Include(s => s.Lines).ThenInclude(l => l.Bundle)
            .Include(s => s.Lines).ThenInclude(l => l.ProductionOrder)
            .Include(s => s.Lines).ThenInclude(l => l.Product).ThenInclude(p => p.Model)
            .Include(s => s.Lines).ThenInclude(l => l.Color)
            .Include(s => s.Lines).ThenInclude(l => l.Size)
            .SingleOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (send is null)
        {
            return SendNotFound;
        }

        var returns = await db.SubcontractReturns
            .AsNoTracking()
            .Include(r => r.Lines)
            .Where(r => r.SubcontractSendId == id)
            .OrderBy(r => r.ReturnDate).ThenBy(r => r.Id)
            .ToListAsync(cancellationToken);

        var lines = send.Lines
            .OrderBy(l => l.Bundle?.BundleNo ?? "", StringComparer.Ordinal)
            .ThenBy(l => l.Product.ProductCode, StringComparer.Ordinal)
            .ThenBy(l => l.Color.ColorCode, StringComparer.Ordinal)
            .ThenBy(l => l.Size.SortOrder)
            .Select(l => new SubcontractSendLineDto(
                l.Id,
                l.BundleId,
                l.Bundle?.BundleNo,
                l.BundleOperationId,
                l.ProductionOrderId,
                l.ProductionOrder?.OrderNo,
                l.ProductId,
                l.Product.ProductCode,
                l.Product.Name,
                l.Product.Model?.ModelCode,
                l.ColorId,
                l.Color.Name,
                l.SizeId,
                l.Size.Name,
                l.Size.SortOrder,
                l.Quantity,
                l.ReturnedGood,
                l.ReturnedDefective,
                l.ReturnedMissing,
                send.Status == SubcontractSendStatus.Cancelled ? 0 : l.Outstanding,
                l.UnitPrice,
                l.Currency))
            .ToList();

        var outstanding = send.Status == SubcontractSendStatus.Cancelled ? 0 : send.OutstandingQuantity;
        return new SubcontractSendDto(
            send.Id,
            send.SendNo,
            send.SupplierId,
            send.Supplier.SupplierCode,
            send.Supplier.Name,
            send.SendDate,
            send.OperationId,
            send.Operation.OperationCode,
            send.Operation.Name,
            send.Status,
            send.Carrier,
            send.Notes,
            send.CanCancel,
            send.IsOpen,
            send.TotalQuantity,
            outstanding,
            send.IsOpen ? Today().DayNumber - send.SendDate.DayNumber : null,
            RowVersionToken.From(send.RowVersion),
            send.CreatedAt,
            send.CreatedBy,
            lines,
            returns
                .Select(r => new SubcontractReturnSummaryDto(
                    r.Id,
                    r.ReturnNo,
                    r.ReturnDate,
                    r.SupplierDocumentNo,
                    r.Lines.Sum(l => l.GoodQuantity),
                    r.Lines.Sum(l => l.DefectiveQuantity),
                    r.Lines.Sum(l => l.MissingQuantity),
                    AmountsOf(r.Lines.Select(l => (l.Currency, l.Amount))),
                    r.IsPaid,
                    r.PaidDate,
                    r.CreatedBy))
                .ToList());
    }

    public async Task<Result<SubcontractSendDocumentDto>> GetSendDocumentAsync(int id, CancellationToken cancellationToken)
    {
        var send = await GetSendAsync(id, cancellationToken);
        if (!send.IsSuccess)
        {
            return send.Error!;
        }

        var factory = await db.Factories.AsNoTracking()
            .Where(f => f.IsActive)
            .OrderBy(f => f.Id)
            .Select(f => new { f.Name, f.AddressLine, f.District, f.City, f.Phone, f.Email })
            .FirstOrDefaultAsync(cancellationToken);
        var sender = factory is null
            ? null
            : new ProformaPartyDto(
                factory.Name, null, null, AddressText(factory.AddressLine, factory.District, factory.City), factory.Phone,
                factory.Email);
        var receiver = await db.Suppliers.AsNoTracking().IgnoreQueryFilters()
            .Where(s => s.Id == send.Value.SupplierId)
            .Select(s => new ProformaPartyDto(s.Name, null, s.TaxNumber, s.Address, s.Phone, s.Email))
            .SingleAsync(cancellationToken);

        return new SubcontractSendDocumentDto(send.Value, sender, receiver, UtcNow);
    }

    public async Task<Result<SubcontractSendDto>> CreateSendAsync(SubcontractSendRequest request, CancellationToken cancellationToken)
    {
        var referenceError =
            await ReferenceValidation.RequireSelectableAsync(db.Suppliers, request.SupplierId, null, "supplierId", "Fasoncu", cancellationToken)
            ?? await ReferenceValidation.RequireSelectableAsync(db.Operations, request.OperationId, null, "operationId", "Operasyon", cancellationToken);
        if (referenceError is not null)
        {
            return referenceError;
        }

        var items = request.Items ?? [];
        if (items.Count > 0)
        {
            var itemError =
                await ReferenceValidation.RequireSelectableAsync(
                    db.Products, items.Select(i => i.ProductId).Distinct().ToList(), new HashSet<int>(), "items", "Ürün", cancellationToken)
                ?? await ReferenceValidation.RequireSelectableAsync(
                    db.Colors, items.Select(i => i.ColorId).Distinct().ToList(), new HashSet<int>(), "items", "Renk", cancellationToken)
                ?? await ReferenceValidation.RequireSelectableAsync(
                    db.Sizes, items.Select(i => i.SizeId).Distinct().ToList(), new HashSet<int>(), "items", "Beden", cancellationToken);
            if (itemError is not null)
            {
                return itemError;
            }
        }

        var operationIds = request.BundleOperationIds ?? [];
        var targets = await db.BundleOperations
            .AsNoTracking()
            .Where(o => operationIds.Contains(o.Id))
            .Select(o => new { o.Id, o.BundleId })
            .ToListAsync(cancellationToken);
        if (targets.Count != operationIds.Count)
        {
            return ReferenceValidation.InvalidReference("bundleOperationIds", "Seçilen demetlerden biri bulunamadı.");
        }

        if (targets.Select(t => t.BundleId).Distinct().Count() != targets.Count)
        {
            return ReferenceValidation.InvalidReference("bundleOperationIds", "Bir demet bir fişte yalnızca bir kez yer alabilir.");
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        // Demet kilitleri sahadaki işlemlerle (ProductionService) aynı adı kullanır: başlatma ile gönderim yarışmaz.
        foreach (var bundleId in targets.Select(t => t.BundleId).Order())
        {
            if (!await appLock.TryAcquireAsync(BundleLock(bundleId), LockTimeout, cancellationToken))
            {
                return Busy;
            }
        }

        var bundleIds = targets.Select(t => t.BundleId).ToList();
        var bundles = await db.Bundles
            .Include(b => b.Operations)
            .Include(b => b.ProductionOrder)
            .Where(b => bundleIds.Contains(b.Id))
            .ToDictionaryAsync(b => b.Id, cancellationToken);

        var send = new SubcontractSend
        {
            SupplierId = request.SupplierId,
            SendDate = request.SendDate,
            OperationId = request.OperationId,
            Carrier = NullIfBlank(request.Carrier),
            Notes = NullIfBlank(request.Notes),
        };

        foreach (var target in targets)
        {
            var bundle = bundles[target.BundleId];
            var operation = bundle.Operations.Single(o => o.Id == target.Id);
            if (operation.OperationId != request.OperationId)
            {
                return NotSendable($"{bundle.BundleNo}: seçilen adım fiş operasyonu değil.");
            }

            var block = bundle.CheckSubcontractSend(operation);
            if (block != OperationStartBlock.None)
            {
                return NotSendable($"{bundle.BundleNo}: {BlockText(block)}");
            }

            if (operation.RemainingQuantity <= 0)
            {
                return NotSendable($"{bundle.BundleNo}: bu operasyonda gönderilecek parça kalmamış.");
            }

            var order = bundle.ProductionOrder;
            if (order.Status == ProductionOrderStatus.Released)
            {
                // Sahadaki ilk başlatmada olduğu gibi: siparişin üretimi başlamış sayılır.
                order.TransitionTo(ProductionOrderStatus.InProduction);
            }
            else if (order.Status != ProductionOrderStatus.InProduction)
            {
                return NotSendable($"{bundle.BundleNo}: demetin siparişi ({order.OrderNo}) üretime açık değil.");
            }

            bundle.SendToSubcontractor(operation);
            send.Lines.Add(new SubcontractSendLine
            {
                BundleId = bundle.Id,
                BundleOperationId = operation.Id,
                ProductionOrderId = bundle.ProductionOrderId,
                ProductId = order.ProductId,
                ColorId = bundle.ColorId,
                SizeId = bundle.SizeId,
                Quantity = operation.RemainingQuantity,
            });
        }

        foreach (var item in items)
        {
            send.Lines.Add(new SubcontractSendLine
            {
                ProductId = item.ProductId,
                ColorId = item.ColorId,
                SizeId = item.SizeId,
                Quantity = item.Quantity,
            });
        }

        // Fiyat gönderim gününde sabitlenir; sonradan değişen fiyat bu fişin hakedişini etkilemez.
        var prices = await SubcontractPriceService.ResolveManyAsync(
            db,
            send.Lines.Select(l => new SubcontractPriceKey(request.SupplierId, request.OperationId, l.ProductId)).Distinct().ToList(),
            request.SendDate,
            cancellationToken);
        foreach (var line in send.Lines)
        {
            if (prices.TryGetValue(new SubcontractPriceKey(request.SupplierId, request.OperationId, line.ProductId), out var price))
            {
                line.UnitPrice = price.UnitPrice;
                line.Currency = price.Currency;
            }
        }

        var year = request.SendDate.Year;
        var sequence = await numbers.NextAsync(SendSeries, year, cancellationToken);
        send.SendNo = $"{SendPrefix}-{year}-{sequence.ToString(CultureInfo.InvariantCulture).PadLeft(5, '0')}";
        db.SubcontractSends.Add(send);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ConcurrencyConflict;
        }

        await transaction.CommitAsync(cancellationToken);
        return await GetSendAsync(send.Id, cancellationToken);
    }

    public async Task<Result<SubcontractSendDto>> CancelSendAsync(int id, string rowVersion, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (!await appLock.TryAcquireAsync(SendLock(id), LockTimeout, cancellationToken))
        {
            return Busy;
        }

        var send = await db.SubcontractSends.Include(s => s.Lines).SingleOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (send is null)
        {
            return SendNotFound;
        }

        if (RowVersionToken.Check(send.RowVersion, rowVersion, ConcurrencyConflict) is { } versionError)
        {
            return versionError;
        }

        if (!send.CanCancel)
        {
            return new Error(
                SubcontractErrors.InvalidState,
                "Yalnızca hiç dönüşü işlenmemiş açık fiş iptal edilebilir. Dönmeyen parçaları dönüş fişinde eksik olarak girin.",
                ErrorType.Conflict);
        }

        var bundleIds = send.Lines.Where(l => l.BundleId is not null).Select(l => l.BundleId!.Value).Distinct().Order().ToList();
        foreach (var bundleId in bundleIds)
        {
            if (!await appLock.TryAcquireAsync(BundleLock(bundleId), LockTimeout, cancellationToken))
            {
                return Busy;
            }
        }

        var bundles = await db.Bundles.Where(b => bundleIds.Contains(b.Id)).ToListAsync(cancellationToken);
        foreach (var bundle in bundles)
        {
            bundle.ReturnFromSubcontractor();
        }

        send.Cancel();
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ConcurrencyConflict;
        }

        await transaction.CommitAsync(cancellationToken);
        return await GetSendAsync(id, cancellationToken);
    }

    // ------------------------------------------------------------------ Dönüş ve hakediş

    public async Task<PagedResult<SubcontractReturnListItemDto>> GetReturnsAsync(
        SubcontractReturnListQuery query, CancellationToken cancellationToken)
    {
        var returns = FilterReturns(db.SubcontractReturns.AsNoTracking().IgnoreQueryFilters(), query.SupplierId, query.From, query.To);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            returns = returns.Where(r => r.ReturnNo.Contains(search) || r.SubcontractSend.SendNo.Contains(search)
                || r.Supplier.Name.Contains(search) || r.SupplierDocumentNo!.Contains(search));
        }

        if (query.SubcontractSendId is { } sendId)
        {
            returns = returns.Where(r => r.SubcontractSendId == sendId);
        }

        if (query.Paid is { } paid)
        {
            returns = paid ? returns.Where(r => r.PaidDate != null) : returns.Where(r => r.PaidDate == null);
        }

        var page = await returns
            .OrderByDescending(r => r.ReturnDate).ThenByDescending(r => r.Id)
            .Select(r => new SubcontractReturnListItemDto(
                r.Id,
                r.ReturnNo,
                r.ReturnDate,
                r.SubcontractSendId,
                r.SubcontractSend.SendNo,
                r.SupplierId,
                r.Supplier.Name,
                r.SubcontractSend.Operation.Name,
                r.SupplierDocumentNo,
                r.Lines.Sum(l => (int?)l.GoodQuantity) ?? 0,
                r.Lines.Sum(l => (int?)l.DefectiveQuantity) ?? 0,
                r.Lines.Sum(l => (int?)l.MissingQuantity) ?? 0,
                r.Lines.Any(l => l.GoodQuantity > 0 && l.UnitPrice == null),
                r.PaidDate != null,
                r.PaidDate,
                new List<CurrencyAmountDto>()))
            .ToPagedResultAsync(query, cancellationToken);

        // Tutarlar para birimi başına toplanır; sayfadaki fişler için tek sorguda okunur.
        var ids = page.Items.Select(i => i.Id).ToList();
        var amounts = (await db.SubcontractReturnLines
                .AsNoTracking()
                .Where(l => ids.Contains(l.SubcontractReturnId))
                .Select(l => new { l.SubcontractReturnId, l.Currency, l.GoodQuantity, l.UnitPrice })
                .ToListAsync(cancellationToken))
            .ToLookup(l => l.SubcontractReturnId);

        return page with
        {
            Items = page.Items
                .Select(i => i with
                {
                    Amounts = AmountsOf(amounts[i.Id].Select(l => (l.Currency, decimal.Round(l.GoodQuantity * (l.UnitPrice ?? 0), 2)))),
                })
                .ToList(),
        };
    }

    public async Task<Result<SubcontractReturnDto>> GetReturnAsync(int id, CancellationToken cancellationToken)
    {
        var ret = await db.SubcontractReturns
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Include(r => r.Supplier)
            .Include(r => r.SubcontractSend).ThenInclude(s => s.Operation)
            .Include(r => r.Lines).ThenInclude(l => l.DefectType)
            .Include(r => r.Lines).ThenInclude(l => l.SubcontractSendLine).ThenInclude(l => l.Bundle)
            .Include(r => r.Lines).ThenInclude(l => l.SubcontractSendLine).ThenInclude(l => l.ProductionOrder)
            .Include(r => r.Lines).ThenInclude(l => l.SubcontractSendLine).ThenInclude(l => l.Product)
            .Include(r => r.Lines).ThenInclude(l => l.SubcontractSendLine).ThenInclude(l => l.Color)
            .Include(r => r.Lines).ThenInclude(l => l.SubcontractSendLine).ThenInclude(l => l.Size)
            .SingleOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (ret is null)
        {
            return ReturnNotFound;
        }

        return new SubcontractReturnDto(
            ret.Id,
            ret.ReturnNo,
            ret.ReturnDate,
            ret.SubcontractSendId,
            ret.SubcontractSend.SendNo,
            ret.SubcontractSend.SendDate,
            ret.SupplierId,
            ret.Supplier.Name,
            ret.SubcontractSend.OperationId,
            ret.SubcontractSend.Operation.Name,
            ret.SupplierDocumentNo,
            ret.Notes,
            ret.IsPaid,
            ret.PaidDate,
            ret.PaidBy,
            ret.PaymentNote,
            RowVersionToken.From(ret.RowVersion),
            ret.CreatedAt,
            ret.CreatedBy,
            AmountsOf(ret.Lines.Select(l => (l.Currency, l.Amount))),
            ret.Lines
                .OrderBy(l => l.SubcontractSendLine.Bundle?.BundleNo ?? "", StringComparer.Ordinal)
                .ThenBy(l => l.SubcontractSendLine.Product.ProductCode, StringComparer.Ordinal)
                .ThenBy(l => l.SubcontractSendLine.Size.SortOrder)
                .Select(l => new SubcontractReturnLineDto(
                    l.Id,
                    l.SubcontractSendLineId,
                    l.SubcontractSendLine.Bundle?.BundleNo,
                    l.SubcontractSendLine.ProductionOrder?.OrderNo,
                    l.SubcontractSendLine.Product.ProductCode,
                    l.SubcontractSendLine.Product.Name,
                    l.SubcontractSendLine.Color.Name,
                    l.SubcontractSendLine.Size.Name,
                    l.SubcontractSendLine.Quantity,
                    l.GoodQuantity,
                    l.DefectiveQuantity,
                    l.MissingQuantity,
                    l.DefectTypeId,
                    l.DefectType?.Name,
                    l.UnitPrice,
                    l.Currency,
                    l.Amount))
                .ToList());
    }

    public async Task<Result<SubcontractReturnDto>> CreateReturnAsync(
        SubcontractReturnRequest request, CancellationToken cancellationToken)
    {
        var requested = request.Lines.Where(l => l.GoodQuantity + l.DefectiveQuantity + l.MissingQuantity > 0).ToList();
        if (requested.Count == 0)
        {
            return QuantityError("lines", "En az bir satırda dönen (sağlam, hatalı ya da eksik) adet girilmelidir.");
        }

        var defectTypeIds = requested.Where(l => l.DefectTypeId is not null).Select(l => l.DefectTypeId!.Value).Distinct().ToList();
        if (defectTypeIds.Count > 0
            && await ReferenceValidation.RequireSelectableAsync(
                db.DefectTypes, defectTypeIds, new HashSet<int>(), "lines", "Hata türü", cancellationToken) is { } defectError)
        {
            return defectError;
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (!await appLock.TryAcquireAsync(SendLock(request.SubcontractSendId), LockTimeout, cancellationToken))
        {
            return Busy;
        }

        var send = await db.SubcontractSends
            .Include(s => s.Lines)
            .SingleOrDefaultAsync(s => s.Id == request.SubcontractSendId, cancellationToken);
        if (send is null)
        {
            return SendNotFound;
        }

        if (RowVersionToken.Check(send.RowVersion, request.RowVersion, ConcurrencyConflict) is { } versionError)
        {
            return versionError;
        }

        if (!send.IsOpen)
        {
            return new Error(
                SubcontractErrors.InvalidState, "Fiş kapanmış ya da iptal edilmiş; dönüş işlenemez.", ErrorType.Conflict);
        }

        if (request.ReturnDate < send.SendDate)
        {
            return QuantityError("returnDate", "Dönüş tarihi gönderim tarihinden önce olamaz.");
        }

        var pairs = new List<(SubcontractSendLine SendLine, SubcontractReturnLineRequest Request)>();
        foreach (var line in requested)
        {
            var sendLine = send.Lines.FirstOrDefault(l => l.Id == line.SendLineId);
            if (sendLine is null)
            {
                return ReferenceValidation.InvalidReference("lines", "Satırlardan biri bu fişe ait değil.");
            }

            var total = line.GoodQuantity + line.DefectiveQuantity + line.MissingQuantity;
            if (total > sendLine.Outstanding)
            {
                return QuantityError(
                    "lines",
                    $"Dönen adet ({total}) fasoncuda kalan adedi ({sendLine.Outstanding}) aşıyor. Gönderilen = sağlam + hatalı + eksik olmalıdır.");
            }

            pairs.Add((sendLine, line));
        }

        var bundleIds = pairs.Where(p => p.SendLine.BundleId is not null).Select(p => p.SendLine.BundleId!.Value).Distinct().Order().ToList();
        foreach (var bundleId in bundleIds)
        {
            if (!await appLock.TryAcquireAsync(BundleLock(bundleId), LockTimeout, cancellationToken))
            {
                return Busy;
            }
        }

        var bundles = await db.Bundles
            .Include(b => b.Operations)
            .Where(b => bundleIds.Contains(b.Id))
            .ToDictionaryAsync(b => b.Id, cancellationToken);

        var now = UtcNow;
        var ret = new SubcontractReturn
        {
            SubcontractSendId = send.Id,
            SupplierId = send.SupplierId,
            ReturnDate = request.ReturnDate,
            SupplierDocumentNo = NullIfBlank(request.SupplierDocumentNo),
            Notes = NullIfBlank(request.Notes),
        };

        foreach (var (sendLine, line) in pairs)
        {
            sendLine.ReturnedGood += line.GoodQuantity;
            sendLine.ReturnedDefective += line.DefectiveQuantity;
            sendLine.ReturnedMissing += line.MissingQuantity;

            if (sendLine.BundleId is { } bundleId && sendLine.BundleOperationId is { } operationId)
            {
                var bundle = bundles[bundleId];
                if (await ApplyToBundleAsync(bundle, operationId, sendLine, line, now, cancellationToken) is { } bundleError)
                {
                    return bundleError;
                }
            }

            ret.Lines.Add(new SubcontractReturnLine
            {
                SubcontractSendLineId = sendLine.Id,
                GoodQuantity = line.GoodQuantity,
                DefectiveQuantity = line.DefectiveQuantity,
                MissingQuantity = line.MissingQuantity,
                DefectTypeId = line.DefectiveQuantity > 0 ? line.DefectTypeId : null,
                UnitPrice = line.UnitPrice ?? sendLine.UnitPrice,
                Currency = sendLine.Currency,
            });
        }

        send.RefreshStatus();
        // Durum değişmese de fiş satırı güncellenir: sürüm ilerler, açık ekranlar yenilenmesi gerektiğini anlar.
        db.Entry(send).State = EntityState.Modified;

        var year = request.ReturnDate.Year;
        var sequence = await numbers.NextAsync(ReturnSeries, year, cancellationToken);
        ret.ReturnNo = $"{ReturnPrefix}-{year}-{sequence.ToString(CultureInfo.InvariantCulture).PadLeft(5, '0')}";
        db.SubcontractReturns.Add(ret);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ConcurrencyConflict;
        }

        await transaction.CommitAsync(cancellationToken);
        return await GetReturnAsync(ret.Id, cancellationToken);
    }

    /// <summary>
    /// Dönen adetleri demet operasyonuna işler (sahadaki bildirimle aynı alan kuralı) ve satırın tamamı döndüyse demeti
    /// sahaya bırakır. Tamamlanan demetin kartı serbest kalır; sağlam parçası kalmayan demet kapanır (sahadaki gibi).
    /// </summary>
    private async Task<Error?> ApplyToBundleAsync(
        Bundle bundle, long operationId, SubcontractSendLine sendLine, SubcontractReturnLineRequest line, DateTime now,
        CancellationToken cancellationToken)
    {
        var operation = bundle.Operations.Single(o => o.Id == operationId);
        var rejected = line.DefectiveQuantity + line.MissingQuantity;
        var reported = line.GoodQuantity + rejected;

        // Gönderimden sonra operasyonun işi kalmamışsa (ör. kalite reddiyle önceki adımdan parça gelmeyecek) dönüş
        // yalnızca fişe yazılır; akışta işlenecek parça yoktur.
        if (reported > 0 && !operation.IsFinished)
        {
            if (operation.Status is not (BundleOperationStatus.Ready or BundleOperationStatus.Paused))
            {
                return new Error(
                    SubcontractErrors.InvalidState,
                    $"{bundle.BundleNo}: demetin bu operasyonu şu an {operation.Status} durumunda; dönüş işlenemez.",
                    ErrorType.Conflict);
            }

            if (reported > operation.RemainingQuantity)
            {
                return QuantityError(
                    "lines",
                    $"{bundle.BundleNo}: dönen adet ({reported}) demetin bu operasyonda kalan adedini ({operation.RemainingQuantity}) aşıyor.");
            }

            var flow = await db.LoadFlowAsync(bundle, cancellationToken);
            bundle.ReportSubcontracted(operation, line.GoodQuantity, rejected, flow, now);

            if (operation.ReworkQuantity == 0)
            {
                var reworked = await db.ReworkTasks
                    .Where(t => t.BundleOperationId == operation.Id && t.Status == ReworkStatus.Open)
                    .ToListAsync(cancellationToken);
                foreach (var task in reworked)
                {
                    task.MarkDone(now);
                }
            }
        }

        if (sendLine.Outstanding == 0)
        {
            bundle.ReturnFromSubcontractor();
        }

        if (bundle.Status == BundleStatus.Completed)
        {
            var assignment = await db.BundleCardAssignments
                .SingleOrDefaultAsync(a => a.BundleId == bundle.Id && a.ReleasedAt == null, cancellationToken);
            assignment?.Release(now);

            if (!bundle.AtSubcontractor && bundle.FinishedQuantity(await db.LoadFlowAsync(bundle, cancellationToken)) <= 0)
            {
                bundle.Close();
            }
        }

        return null;
    }

    public async Task<Result<SubcontractReturnDto>> UpdateReturnPricesAsync(
        int id, SubcontractReturnPricesRequest request, CancellationToken cancellationToken)
    {
        var ret = await db.SubcontractReturns.Include(r => r.Lines).SingleOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (ret is null)
        {
            return ReturnNotFound;
        }

        if (RowVersionToken.Check(ret.RowVersion, request.RowVersion, ConcurrencyConflict) is { } versionError)
        {
            return versionError;
        }

        if (ret.IsPaid)
        {
            return new Error(
                SubcontractErrors.InvalidState,
                "Ödenmiş hakedişin fiyatı değiştirilemez; önce ödeme işaretini kaldırın.",
                ErrorType.Conflict);
        }

        foreach (var change in request.Lines)
        {
            var line = ret.Lines.FirstOrDefault(l => l.Id == change.ReturnLineId);
            if (line is null)
            {
                return ReferenceValidation.InvalidReference("lines", "Satırlardan biri bu fişe ait değil.");
            }

            line.UnitPrice = change.UnitPrice;
        }

        db.Entry(ret).State = EntityState.Modified;
        return await SaveReturnAsync(ret.Id, cancellationToken);
    }

    public async Task<Result<SubcontractReturnDto>> SetPaymentAsync(
        int id, SubcontractPaymentRequest request, CancellationToken cancellationToken)
    {
        var ret = await db.SubcontractReturns.SingleOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (ret is null)
        {
            return ReturnNotFound;
        }

        if (RowVersionToken.Check(ret.RowVersion, request.RowVersion, ConcurrencyConflict) is { } versionError)
        {
            return versionError;
        }

        if (request.Paid)
        {
            ret.MarkPaid(request.PaidDate ?? Today(), UserName, request.Note);
        }
        else
        {
            ret.MarkUnpaid();
        }

        return await SaveReturnAsync(ret.Id, cancellationToken);
    }

    public async Task<SubcontractStatementDto> GetStatementAsync(
        SubcontractStatementQuery query, CancellationToken cancellationToken)
    {
        var returns = FilterReturns(db.SubcontractReturns.AsNoTracking().IgnoreQueryFilters(), query.SupplierId, query.From, query.To);
        var rows = await db.SubcontractReturnLines
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(l => returns.Select(r => r.Id).Contains(l.SubcontractReturnId))
            .Select(l => new
            {
                l.SubcontractReturnId,
                l.SubcontractReturn.SupplierId,
                SupplierName = l.SubcontractReturn.Supplier.Name,
                Paid = l.SubcontractReturn.PaidDate != null,
                l.Currency,
                l.GoodQuantity,
                l.UnitPrice,
            })
            .ToListAsync(cancellationToken);

        var suppliers = rows
            .GroupBy(r => new { r.SupplierId, r.SupplierName })
            .Select(g => new SubcontractStatementSupplierDto(
                g.Key.SupplierId,
                g.Key.SupplierName,
                g.Select(r => r.SubcontractReturnId).Distinct().Count(),
                g.Sum(r => r.GoodQuantity),
                AmountsOf(g.Select(r => (r.Currency, Amount(r.GoodQuantity, r.UnitPrice)))),
                AmountsOf(g.Where(r => r.Paid).Select(r => (r.Currency, Amount(r.GoodQuantity, r.UnitPrice)))),
                AmountsOf(g.Where(r => !r.Paid).Select(r => (r.Currency, Amount(r.GoodQuantity, r.UnitPrice))))))
            .OrderBy(s => s.SupplierName, StringComparer.CurrentCulture)
            .ToList();

        return new SubcontractStatementDto(
            suppliers,
            AmountsOf(rows.Select(r => (r.Currency, Amount(r.GoodQuantity, r.UnitPrice)))),
            AmountsOf(rows.Where(r => r.Paid).Select(r => (r.Currency, Amount(r.GoodQuantity, r.UnitPrice)))),
            AmountsOf(rows.Where(r => !r.Paid).Select(r => (r.Currency, Amount(r.GoodQuantity, r.UnitPrice)))));
    }

    // ------------------------------------------------------------------ Yardımcılar

    private static IQueryable<SubcontractReturn> FilterReturns(
        IQueryable<SubcontractReturn> returns, int? supplierId, DateOnly? from, DateOnly? to)
    {
        if (supplierId is { } supplier)
        {
            returns = returns.Where(r => r.SupplierId == supplier);
        }

        if (from is { } start)
        {
            returns = returns.Where(r => r.ReturnDate >= start);
        }

        if (to is { } end)
        {
            returns = returns.Where(r => r.ReturnDate <= end);
        }

        return returns;
    }

    private async Task<Result<SubcontractReturnDto>> SaveReturnAsync(int id, CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ConcurrencyConflict;
        }

        return await GetReturnAsync(id, cancellationToken);
    }

    private static decimal Amount(int quantity, decimal? unitPrice) => decimal.Round(quantity * (unitPrice ?? 0), 2);

    /// <summary>Tutarları para birimine göre toplar; sıfır toplamlar da (fiyatsız satırlar) listede kalır.</summary>
    public static IReadOnlyList<CurrencyAmountDto> AmountsOf(IEnumerable<(string Currency, decimal Amount)> amounts) =>
        amounts
            .GroupBy(a => a.Currency)
            .OrderBy(g => g.Key, StringComparer.Ordinal)
            .Select(g => new CurrencyAmountDto(g.Key, g.Sum(a => a.Amount)))
            .ToList();

    private static string BundleLock(int bundleId) => $"bundle:{bundleId}";

    private static string SendLock(int sendId) => $"subcontract-send:{sendId}";

    private static Error NotSendable(string message) => new(
        SubcontractErrors.BundleNotSendable, message, ErrorType.BusinessRule,
        new Dictionary<string, string[]> { ["bundleOperationIds"] = [message] });

    private static Error QuantityError(string field, string message) => new(
        SubcontractErrors.QuantityInvalid, message, ErrorType.Validation,
        new Dictionary<string, string[]> { [field] = [message] });

    private static string BlockText(OperationStartBlock block) => block switch
    {
        OperationStartBlock.BundleClosed => "demet tamamlanmış, kapatılmış ya da iptal edilmiş.",
        OperationStartBlock.BundleOnQualityHold or OperationStartBlock.OperationOnQualityHold => "demet kalite bekletmesinde.",
        OperationStartBlock.OperationCompleted => "bu operasyon zaten tamamlanmış.",
        OperationStartBlock.OperationCancelled => "bu operasyon iptal edilmiş.",
        OperationStartBlock.PredecessorsIncomplete => "önceki operasyonlar tamamlanmamış.",
        OperationStartBlock.AlreadyInProgress or OperationStartBlock.AnotherOperationInProgress => "demet şu an sahada işlemde.",
        OperationStartBlock.AnotherOperationUnfinished => "demetin yarım bırakılmış başka bir operasyonu var.",
        OperationStartBlock.BundleAtSubcontractor => "demet zaten fasonda.",
        _ => "demet şu an gönderilemez.",
    };

    private static string? AddressText(string? line, string? district, string? city)
    {
        var parts = new[] { line, string.Join(" / ", new[] { district, city }.Where(p => !string.IsNullOrWhiteSpace(p))) }
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToList();
        return parts.Count == 0 ? null : string.Join(", ", parts);
    }
}
