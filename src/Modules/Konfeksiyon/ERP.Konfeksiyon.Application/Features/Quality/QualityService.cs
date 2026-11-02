using ERP.Konfeksiyon.Application.Abstractions;
using System.Linq.Expressions;
using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Konfeksiyon.Application.Features.Bundles;
using ERP.Core.Application.Features.MasterData;
using ERP.Konfeksiyon.Application.Features.MasterData;
using ERP.Konfeksiyon.Domain.Entities;
using ERP.Konfeksiyon.Domain.Enums;
using ERP.Konfeksiyon.Domain.Routing;
using Microsoft.EntityFrameworkCore;

namespace ERP.Konfeksiyon.Application.Features.Quality;

public sealed class DefectTypeService(IKonfeksiyonDbContext db) : KonfeksiyonMasterDataService<DefectType, DefectTypeDto, DefectTypeRequest>(db)
{
    protected override DbSet<DefectType> Set => Db.DefectTypes;
    protected override string Label => "Hata türü";
    protected override string CodeField => "code";

    protected override string CodeOf(DefectTypeRequest request) => request.Code;
    protected override Expression<Func<DefectType, bool>> WithCode(string code) => e => e.Code == code;

    protected override Expression<Func<DefectType, bool>> Matches(string term) =>
        e => e.Code.Contains(term) || e.Name.Contains(term);

    protected override IQueryable<DefectType> Order(IQueryable<DefectType> query) => query.OrderBy(e => e.Name);

    protected override Expression<Func<DefectType, DefectTypeDto>> ToDto =>
        e => new DefectTypeDto(e.Id, e.Code, e.Name, e.IsActive);

    protected override Expression<Func<DefectType, LookupDto>> ToLookup => e => new LookupDto(e.Id, e.Code, e.Name);

    protected override void Apply(DefectTypeRequest request, DefectType entity, string code)
    {
        entity.Code = code;
        entity.Name = request.Name.Trim();
        entity.IsActive = request.IsActive;
    }

    // Kalite kayıtlarında kullanılan tür silinse de kayıtlarda adıyla görünmeye devam eder (soft delete).
}

public interface IQualityService
{
    /// <summary>Kalite kontrolü kaydeder ve sonucunu demete işler (ret, tamir, bekletme).</summary>
    Task<Result<InspectionDto>> InspectAsync(InspectionRequest request, CancellationToken cancellationToken);

    /// <summary>Demeti kalite bekletmesinden çıkarır; demet kaldığı yerden devam eder.</summary>
    Task<Result<BundleQualityDto>> ReleaseHoldAsync(int bundleId, CancellationToken cancellationToken);

    /// <summary>Demetin kalite durumu: kontrol girilebilecek operasyonlar, geçmiş ve tamir işleri.</summary>
    Task<Result<BundleQualityDto>> GetBundleAsync(int bundleId, CancellationToken cancellationToken);

    Task<PagedResult<InspectionDto>> GetInspectionsAsync(InspectionListQuery query, CancellationToken cancellationToken);
    Task<PagedResult<ReworkTaskDto>> GetReworkTasksAsync(ReworkTaskListQuery query, CancellationToken cancellationToken);

    /// <summary>Tarih aralığında hata türlerine göre dağılım ve hata oranı.</summary>
    Task<Result<DefectSummaryDto>> GetDefectSummaryAsync(DefectSummaryQuery query, CancellationToken cancellationToken);
}

/// <summary>
/// Kalite işlemleri. Adet ve akış kuralları <see cref="Bundle"/> içindedir; burada kontrol kaydı, tamir işi
/// ve eşzamanlılık yönetilir. Üretim işlemleriyle aynı demet kilidi kullanılır: kalite, makinenin tam o anda
/// bildirdiği adetle yarışmaz.
/// </summary>
public sealed class QualityService(
    IKonfeksiyonDbContext db, IAppLock appLock, ICurrentUser currentUser, ICurrentPermissions permissions, TimeProvider timeProvider)
    : IQualityService
{
    public const int MaxSummaryRangeDays = 366;
    private const string SystemUser = "system";
    private static readonly TimeSpan LockTimeout = TimeSpan.FromSeconds(10);

    private static readonly Error BundleNotFound = new(ErrorCodes.NotFound, "Demet bulunamadı.", ErrorType.NotFound);

    private static readonly Error Busy = new(
        ErrorCodes.ResourceBusy, "Demet şu an başka bir işlemle değiştiriliyor. Lütfen tekrar deneyin.", ErrorType.Conflict);

    private DateTime UtcNow => timeProvider.GetUtcNow().UtcDateTime;
    private string Actor => currentUser.UserName ?? SystemUser;

    public async Task<Result<InspectionDto>> InspectAsync(InspectionRequest request, CancellationToken cancellationToken)
    {
        // Hatalı (hurda) ayırma adedi kalıcı olarak düşürür ve maliyete yansır: ayrı yetki ister.
        if (request.RejectedQuantity > 0
            && !await permissions.HasAsync(ERP.Konfeksiyon.Domain.Authorization.KonfeksiyonPermissions.Quality.Reject, cancellationToken))
        {
            const string message = "Parçayı hatalı ayırma yetkiniz yok (Kalite kontrolünde hatalı ayırma).";
            return new Error(
                ErrorCodes.Forbidden, message, ErrorType.Forbidden,
                new Dictionary<string, string[]> { ["rejectedQuantity"] = [message] });
        }

        var bundleId = await db.BundleOperations
            .Where(o => o.Id == request.BundleOperationId)
            .Select(o => (int?)o.BundleId)
            .SingleOrDefaultAsync(cancellationToken);
        if (bundleId is null)
        {
            return new Error(ErrorCodes.NotFound, "Demet operasyonu bulunamadı.", ErrorType.NotFound);
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (!await appLock.TryAcquireAsync(BundleLock(bundleId.Value), LockTimeout, cancellationToken))
        {
            return Busy;
        }

        var bundle = await db.Bundles.Include(b => b.Operations).SingleAsync(b => b.Id == bundleId, cancellationToken);
        var operation = bundle.Operations.Single(o => o.Id == request.BundleOperationId);

        if (bundle.Status is BundleStatus.Created or BundleStatus.Cancelled or BundleStatus.Closed)
        {
            return new Error(ErrorCodes.BundleNotActive, "Üretime girmemiş, iptal edilmiş ya da kapatılmış demet kontrol edilemez.");
        }

        if (request.InspectedQuantity > operation.ProducedQuantity)
        {
            return QuantityError(
                "inspectedQuantity",
                $"Kontrol edilen adet ({request.InspectedQuantity}), operasyonun ürettiği adedi ({operation.ProducedQuantity}) aşamaz.");
        }

        var takeBack = request.RejectedQuantity + request.ReworkQuantity;
        var flow = await LoadFlowAsync(bundle, cancellationToken);
        switch (takeBack > 0 ? bundle.CheckTakeBack(operation, takeBack, flow) : QualityTakeBackBlock.None)
        {
            case QualityTakeBackBlock.OperationInProgress:
                return new Error(
                    ErrorCodes.WorkInProgress,
                    "Operasyon şu an makinede işleniyor; ret / tamir için önce adedinin bildirilmesi gerekir.",
                    ErrorType.Conflict);
            case QualityTakeBackBlock.ExceedsProduced:
                return QuantityError("rejectedQuantity", "Ret ve tamir adedi operasyonun ürettiği adedi aşamaz.");
            case QualityTakeBackBlock.ConsumedDownstream:
                return new Error(
                    ErrorCodes.QualityConsumedDownstream,
                    "Bu parçalar sonraki operasyonda işlenmiş; kontrolü o operasyon üzerinden kaydedin.");
        }

        if (request.ReworkQuantity > 0)
        {
            // Tamir, operasyonun makinede yeniden işlenmesidir; üretime kapalı siparişte demet başlatılamaz ve
            // tamirde takılı kalır.
            var orderStatus = await db.ProductionOrders
                .Where(o => o.Id == bundle.ProductionOrderId)
                .Select(o => o.Status)
                .SingleAsync(cancellationToken);
            if (orderStatus is not (ProductionOrderStatus.Released or ProductionOrderStatus.InProduction))
            {
                return new Error(
                    ErrorCodes.OrderNotInProduction,
                    "Demetin siparişi üretime kapalı (tamamlanmış ya da iptal); tamire gönderilemez. "
                    + "Kullanılamayacak parçaları ret olarak kaydedin.");
            }
        }

        if (request.Hold && !bundle.CanPlaceOnQualityHold && bundle.Status != BundleStatus.QualityHold)
        {
            return new Error(ErrorCodes.InvalidStatusTransition, "Demet bu durumdayken kalite bekletmesine alınamaz.");
        }

        var defects = request.Defects ?? [];
        var defectError = await ReferenceValidation.RequireSelectableAsync(
            db.DefectTypes, defects.Select(d => d.DefectTypeId).ToList(), new HashSet<int>(), "defects", "Hata türü",
            cancellationToken);
        if (defectError is not null)
        {
            return defectError;
        }

        var now = UtcNow;
        ReworkTask? reinspected = null;
        if (request.ReworkTaskId is { } reworkTaskId)
        {
            reinspected = await db.ReworkTasks.SingleOrDefaultAsync(
                t => t.Id == reworkTaskId && t.BundleOperationId == operation.Id, cancellationToken);
            if (reinspected is null)
            {
                return ReferenceValidation.InvalidReference("reworkTaskId", "Tamir işi bu operasyona ait değil ya da bulunamadı.");
            }

            if (reinspected.Status != ReworkStatus.Done)
            {
                return new Error(
                    ErrorCodes.ReworkNotDone,
                    reinspected.Status == ReworkStatus.Open
                        ? "Tamir henüz yapılmadı: parçalar operasyonda yeniden işlenip adedi bildirilmeli."
                        : "Bu tamir işi zaten kapatılmış.");
            }
        }

        var inspection = new QualityInspection
        {
            BundleId = bundle.Id,
            BundleOperationId = operation.Id,
            Result = QualityInspection.ResultOf(request.RejectedQuantity, request.ReworkQuantity, request.Hold),
            InspectedQuantity = request.InspectedQuantity,
            RejectedQuantity = request.RejectedQuantity,
            ReworkQuantity = request.ReworkQuantity,
            PlacedOnHold = request.Hold,
            ReworkTaskId = reinspected?.Id,
            Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
            InspectedAt = now,
            InspectedBy = Actor,
        };
        foreach (var defect in defects)
        {
            inspection.Records.Add(new QualityRecord { DefectTypeId = defect.DefectTypeId, Quantity = defect.Quantity });
        }

        db.QualityInspections.Add(inspection);

        bundle.ApplyInspection(operation, request.RejectedQuantity, request.ReworkQuantity, flow, now);
        if (request.Hold && bundle.Status != BundleStatus.QualityHold)
        {
            bundle.PlaceOnQualityHold();
        }

        // Tekrar kontrol tamir işini kapatır: sorunsuzsa onaylanır; yine ret / tamir varsa reddedilir.
        reinspected?.Close(approved: takeBack == 0, now, Actor);

        if (request.ReworkQuantity > 0)
        {
            db.ReworkTasks.Add(new ReworkTask
            {
                QualityInspection = inspection,
                BundleId = bundle.Id,
                BundleOperationId = operation.Id,
                Quantity = request.ReworkQuantity,
                CreatedAt = now,
                CreatedBy = Actor,
            });

            await ReattachCardForReworkAsync(bundle, now, cancellationToken);
        }

        await ReleaseCardIfCompletedAsync(bundle, now, cancellationToken);

        if (await SaveAsync(cancellationToken) is { } saveError)
        {
            return saveError;
        }

        await transaction.CommitAsync(cancellationToken);
        return await ProjectInspections(db.QualityInspections.Where(i => i.Id == inspection.Id)).SingleAsync(cancellationToken);
    }

    public async Task<Result<BundleQualityDto>> ReleaseHoldAsync(int bundleId, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (!await appLock.TryAcquireAsync(BundleLock(bundleId), LockTimeout, cancellationToken))
        {
            return Busy;
        }

        var bundle = await db.Bundles.Include(b => b.Operations).SingleOrDefaultAsync(b => b.Id == bundleId, cancellationToken);
        if (bundle is null)
        {
            return BundleNotFound;
        }

        if (bundle.Status != BundleStatus.QualityHold)
        {
            return new Error(ErrorCodes.BundleNotOnHold, "Demet kalite bekletmesinde değil.");
        }

        bundle.ReleaseQualityHold();
        await ReleaseCardIfCompletedAsync(bundle, UtcNow, cancellationToken);

        if (await SaveAsync(cancellationToken) is { } saveError)
        {
            return saveError;
        }

        await transaction.CommitAsync(cancellationToken);
        return await GetBundleAsync(bundleId, cancellationToken);
    }

    public async Task<Result<BundleQualityDto>> GetBundleAsync(int bundleId, CancellationToken cancellationToken)
    {
        var bundle = await db.Bundles
            .AsNoTracking()
            .Include(b => b.Operations)
            .SingleOrDefaultAsync(b => b.Id == bundleId, cancellationToken);
        if (bundle is null)
        {
            return BundleNotFound;
        }

        var flow = await LoadFlowAsync(bundle, cancellationToken);
        var operationIds = bundle.Operations.Select(o => o.OperationId).ToList();
        var names = await db.Operations
            .IgnoreQueryFilters()
            .Where(o => operationIds.Contains(o.Id))
            .ToDictionaryAsync(o => o.Id, o => o.Name, cancellationToken);

        var operations = bundle.Operations
            .Where(o => o.ProducedQuantity > 0 || o.ReworkQuantity > 0)
            .OrderBy(o => o.SequenceNo)
            .Select(o => new InspectableOperationDto(
                o.Id, o.SequenceNo, names.GetValueOrDefault(o.OperationId, "?"), o.Status, o.ProducedQuantity,
                TakeBackLimit(bundle, o, flow), o.ReworkQuantity))
            .ToList();

        var inspections = await ProjectInspections(db.QualityInspections.Where(i => i.BundleId == bundleId).OrderByDescending(i => i.Id))
            .ToListAsync(cancellationToken);
        var tasks = await ProjectTasks(db.ReworkTasks.Where(t => t.BundleId == bundleId).OrderByDescending(t => t.Id))
            .ToListAsync(cancellationToken);

        return new BundleQualityDto(
            bundle.Id, bundle.BundleNo, bundle.Status, bundle.Status == BundleStatus.QualityHold,
            bundle.CanPlaceOnQualityHold, operations, inspections, tasks);
    }

    public Task<PagedResult<InspectionDto>> GetInspectionsAsync(InspectionListQuery query, CancellationToken cancellationToken)
    {
        var inspections = db.QualityInspections.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            inspections = inspections.Where(
                i => i.Bundle.BundleNo.Contains(search) || i.Bundle.ProductionOrder.OrderNo.Contains(search));
        }

        if (query.BundleId is { } bundleId)
        {
            inspections = inspections.Where(i => i.BundleId == bundleId);
        }

        if (query.Result is { } result)
        {
            inspections = inspections.Where(i => i.Result == result);
        }

        if (query.From is { } from)
        {
            inspections = inspections.Where(i => i.InspectedAt >= from);
        }

        if (query.To is { } to)
        {
            inspections = inspections.Where(i => i.InspectedAt < to);
        }

        return ProjectInspections(inspections.OrderByDescending(i => i.Id)).ToPagedResultAsync(query, cancellationToken);
    }

    public Task<PagedResult<ReworkTaskDto>> GetReworkTasksAsync(ReworkTaskListQuery query, CancellationToken cancellationToken)
    {
        var tasks = db.ReworkTasks.AsQueryable();

        if (query.Status is { } status)
        {
            tasks = tasks.Where(t => t.Status == status);
        }

        if (query.Pending is { } pending)
        {
            tasks = tasks.Where(t => (t.Status == ReworkStatus.Open || t.Status == ReworkStatus.Done) == pending);
        }

        if (query.BundleId is { } bundleId)
        {
            tasks = tasks.Where(t => t.BundleId == bundleId);
        }

        return ProjectTasks(tasks.OrderByDescending(t => t.Id)).ToPagedResultAsync(query, cancellationToken);
    }

    public async Task<Result<DefectSummaryDto>> GetDefectSummaryAsync(
        DefectSummaryQuery query, CancellationToken cancellationToken)
    {
        if (query.From == default || query.To == default || query.To <= query.From)
        {
            return RangeError("Geçerli bir tarih aralığı verin: bitiş, başlangıçtan sonra olmalıdır.");
        }

        if ((query.To - query.From).TotalDays > MaxSummaryRangeDays)
        {
            return RangeError($"Tarih aralığı en fazla {MaxSummaryRangeDays} gün olabilir.");
        }

        var from = DateTime.SpecifyKind(query.From.ToUniversalTime(), DateTimeKind.Utc);
        var to = DateTime.SpecifyKind(query.To.ToUniversalTime(), DateTimeKind.Utc);
        var inspections = db.QualityInspections.AsNoTracking().Where(i => i.InspectedAt >= from && i.InspectedAt < to);

        var totals = await inspections
            .GroupBy(i => 1)
            .Select(g => new
            {
                Count = g.Count(),
                Inspected = g.Sum(i => i.InspectedQuantity),
                Rejected = g.Sum(i => i.RejectedQuantity),
                Rework = g.Sum(i => i.ReworkQuantity),
            })
            .SingleOrDefaultAsync(cancellationToken);

        var defects = await db.QualityRecords
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(r => r.QualityInspection.InspectedAt >= from && r.QualityInspection.InspectedAt < to)
            .GroupBy(r => new { r.DefectTypeId, r.DefectType.Code, r.DefectType.Name })
            .Select(g => new DefectSummaryRowDto(
                g.Key.DefectTypeId, g.Key.Code, g.Key.Name, g.Sum(r => r.Quantity),
                g.Select(r => r.QualityInspectionId).Distinct().Count()))
            .ToListAsync(cancellationToken);

        var inspected = totals?.Inspected ?? 0;
        var defective = (totals?.Rejected ?? 0) + (totals?.Rework ?? 0);
        return new DefectSummaryDto(
            from, to, totals?.Count ?? 0, inspected, totals?.Rejected ?? 0, totals?.Rework ?? 0,
            inspected > 0 ? Math.Round(defective * 100.0 / inspected, 1) : null,
            defects.OrderByDescending(d => d.Quantity).ThenBy(d => d.Name, StringComparer.Ordinal).ToList());
    }

    /// <summary>Üretim işlemleriyle (başlat / bildir) aynı kilit.</summary>
    private static string BundleLock(int bundleId) => $"bundle:{bundleId}";

    /// <summary>Operasyondan geri alınabilecek en fazla adet (0: geri alınamaz).</summary>
    private static int TakeBackLimit(Bundle bundle, BundleOperation operation, OperationFlow flow)
    {
        for (var quantity = operation.ProducedQuantity; quantity > 0; quantity--)
        {
            if (bundle.CheckTakeBack(operation, quantity, flow) == QualityTakeBackBlock.None)
            {
                return quantity;
            }
        }

        return 0;
    }

    private Task<OperationFlow> LoadFlowAsync(Bundle bundle, CancellationToken cancellationToken) =>
        db.LoadFlowAsync(bundle, cancellationToken);

    /// <summary>Kalite işlemiyle tamamlanan demetin kartı serbest kalır (üretimde tamamlanınca olduğu gibi).</summary>
    private async Task ReleaseCardIfCompletedAsync(Bundle bundle, DateTime now, CancellationToken cancellationToken)
    {
        if (bundle.Status != BundleStatus.Completed)
        {
            return;
        }

        var assignment = await db.BundleCardAssignments
            .SingleOrDefaultAsync(a => a.BundleId == bundle.Id && a.ReleasedAt == null, cancellationToken);
        assignment?.Release(now);

        // Sağlam parçası kalmayan demet depoya alınamaz; bekleyen işlerde kalmasın diye kapanır.
        if (bundle.FinishedQuantity(await LoadFlowAsync(bundle, cancellationToken)) <= 0)
        {
            bundle.Close();
        }
    }

    /// <summary>
    /// Tamamlanmış demet tamire dönünce kartı serbest kalmıştır; tablette okutulabilmesi için son kartı yeniden bağlanır.
    /// Kart başka demetteyse planlamacı yeni kart atar (görev listesinde görünür).
    /// </summary>
    private async Task ReattachCardForReworkAsync(Bundle bundle, DateTime now, CancellationToken cancellationToken)
    {
        if (await db.BundleCardAssignments.AnyAsync(a => a.BundleId == bundle.Id && a.ReleasedAt == null, cancellationToken))
        {
            return;
        }

        var cardId = await db.BundleCardAssignments
            .Where(a => a.BundleId == bundle.Id)
            .OrderByDescending(a => a.AssignedAt)
            .Select(a => (int?)a.RfidCardId)
            .FirstOrDefaultAsync(cancellationToken);
        if (cardId is { } id && !await db.BundleCardAssignments.AnyAsync(a => a.RfidCardId == id && a.ReleasedAt == null, cancellationToken))
        {
            db.BundleCardAssignments.Add(new BundleCardAssignment { RfidCardId = id, BundleId = bundle.Id, AssignedAt = now });
        }
    }

    private async Task<Error?> SaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return null;
        }
        catch (DbUpdateConcurrencyException)
        {
            return new Error(
                ErrorCodes.ConcurrencyConflict, "Demet aynı anda başka bir işlemle değişti. Lütfen tekrar deneyin.",
                ErrorType.Conflict);
        }
    }

    // Geçmiş kayıtlar, sonradan silinen operasyon / hata türü için de eksiksiz görünür.
    private static IQueryable<InspectionDto> ProjectInspections(IQueryable<QualityInspection> inspections) =>
        inspections.AsNoTracking().IgnoreQueryFilters().Select(i => new InspectionDto(
            i.Id,
            i.BundleId,
            i.Bundle.BundleNo,
            i.Bundle.ProductionOrder.OrderNo,
            i.BundleOperationId,
            i.BundleOperation.SequenceNo,
            i.BundleOperation.Operation.Name,
            i.Result,
            i.InspectedQuantity,
            i.InspectedQuantity - i.RejectedQuantity - i.ReworkQuantity,
            i.RejectedQuantity,
            i.ReworkQuantity,
            i.PlacedOnHold,
            i.ReworkTaskId,
            i.Note,
            i.InspectedAt,
            i.InspectedBy,
            i.Records
                .OrderBy(r => r.Id)
                .Select(r => new InspectionDefectDto(r.DefectTypeId, r.DefectType.Name, r.Quantity))
                .ToList()));

    private static IQueryable<ReworkTaskDto> ProjectTasks(IQueryable<ReworkTask> tasks) =>
        tasks.AsNoTracking().IgnoreQueryFilters().Select(t => new ReworkTaskDto(
            t.Id,
            t.QualityInspectionId,
            t.BundleId,
            t.Bundle.BundleNo,
            t.Bundle.ProductionOrder.OrderNo,
            t.BundleOperationId,
            t.BundleOperation.SequenceNo,
            t.BundleOperation.Operation.Name,
            t.Quantity,
            t.Status,
            t.CreatedAt,
            t.CreatedBy,
            t.DoneAt,
            t.ClosedAt,
            t.ClosedBy));

    private static Error QuantityError(string field, string message) => new(
        ErrorCodes.QualityQuantityInvalid, message, ErrorType.BusinessRule,
        new Dictionary<string, string[]> { [field] = [message] });

    private static Error RangeError(string message) => new(
        ErrorCodes.ValidationError, message, ErrorType.Validation,
        new Dictionary<string, string[]> { ["from"] = [message] });
}
