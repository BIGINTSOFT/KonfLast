using ERP.Konfeksiyon.Application.Abstractions;
using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Konfeksiyon.Application.Features.Bundles;
using ERP.Konfeksiyon.Application.Features.Subcontracting;
using ERP.Konfeksiyon.Application.Features.WorkSessions;
using ERP.Konfeksiyon.Domain.Entities;
using ERP.Konfeksiyon.Domain.Enums;
using ERP.Konfeksiyon.Domain.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace ERP.Konfeksiyon.Application.Features.Production;

public interface IProductionService
{
    /// <summary>İsteği yapan tabletin makinesinde demet operasyonunu başlatır ya da kalan adet için sürdürür.</summary>
    Task<Result<OperationProgressDto>> StartAsync(
        long bundleOperationId, StartOperationRequest request, CancellationToken cancellationToken);

    /// <summary>İşlemdeki operasyon için üretilen / hatalı adetleri bildirir; kalan biterse operasyon tamamlanır.</summary>
    Task<Result<OperationProgressDto>> ReportAsync(
        long bundleOperationId, ReportOperationRequest request, CancellationToken cancellationToken);

    /// <summary>İsteği yapan tabletin makinesinde şu an işlemde olan operasyon (yoksa boş).</summary>
    Task<Result<OperationProgressDto?>> GetCurrentAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Ofisten: işlemde kalmış operasyonu adet bildirmeden bırakır (tablet arızalı / başka makineye bağlanmış,
    /// operatör bildirmeden ayrılmış). Operasyon "bekliyor"a döner; kalan adet herhangi bir uygun makinede sürdürülür.
    /// </summary>
    Task<Result<ReleasedOperationDto>> ReleaseAsync(long bundleOperationId, CancellationToken cancellationToken);

    /// <summary>Elle bildirim penceresi: operasyonun durumu ve operasyonu yapabilen makineler.</summary>
    Task<Result<ManualReportOptionsDto>> GetManualReportOptionsAsync(
        long bundleOperationId, CancellationToken cancellationToken);

    /// <summary>
    /// Ofisten, tablet ve kart olmadan adet bildirir. İşlemde (askıda) kalmış operasyonda işi başlatan makine ve
    /// operatör adına bildirilir; başlamamış / bekletilmiş operasyonda seçilen makine ve operatör adına başlatılıp
    /// bildirilir. Demetin kartı olması gerekmez.
    /// </summary>
    Task<Result<OperationProgressDto>> ManualReportAsync(
        long bundleOperationId, ManualReportRequest request, CancellationToken cancellationToken);

    /// <summary>Demetin üretim olayları, eskiden yeniye.</summary>
    Task<Result<IReadOnlyList<BundleOperationEventDto>>> GetBundleEventsAsync(
        int bundleId, CancellationToken cancellationToken);
}

/// <summary>
/// Operasyon motorunun uygulama katmanı. Kuralların kendisi <see cref="Bundle"/> içindedir; burada
/// makine / operatör / sipariş kontrolleri, eşzamanlılık ve olay kaydı yapılır.
///
/// Eşzamanlılık: her işlem demet kilidi altında tek transaction'da yürür; demetin rowversion'ı ve
/// filtreli tekil index'ler (demet / makine / operatör başına tek işlemdeki operasyon) son güvencedir.
/// </summary>
public sealed class ProductionService(
    IKonfeksiyonDbContext db,
    IAppLock appLock,
    ICurrentUser currentUser,
    IStationResolver stations,
    IOperatorQualificationService qualifications,
    ISessionInterruptions interruptions,
    IFloorAuthorization floorAuthorization,
    TimeProvider timeProvider) : IProductionService
{
    private const int MaxEvents = 1000;
    private const string SystemUser = "system";
    private static readonly TimeSpan LockTimeout = TimeSpan.FromSeconds(10);

    private static readonly Error OperationNotFound = new(
        ErrorCodes.NotFound, "Demet operasyonu bulunamadı.", ErrorType.NotFound);

    private static readonly Error Busy = new(
        ErrorCodes.ResourceBusy, "Demet şu an başka bir cihazda işleniyor. Lütfen tekrar deneyin.", ErrorType.Conflict);

    private static readonly Error ConcurrencyConflict = new(
        ErrorCodes.ConcurrencyConflict, "Demet aynı anda başka bir işlemle değişti. Lütfen tekrar deneyin.",
        ErrorType.Conflict);


    private DateTime UtcNow => timeProvider.GetUtcNow().UtcDateTime;

    public async Task<Result<OperationProgressDto>> StartAsync(
        long bundleOperationId, StartOperationRequest request, CancellationToken cancellationToken)
    {
        var resolved = await stations.ResolveAsync(cancellationToken);
        if (!resolved.IsSuccess)
        {
            return resolved.Error!;
        }

        var station = resolved.Value;
        var bundleId = await FindBundleIdAsync(bundleOperationId, cancellationToken);
        if (bundleId is null)
        {
            return OperationNotFound;
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (!await appLock.TryAcquireAsync(BundleLock(bundleId.Value), LockTimeout, cancellationToken))
        {
            return Busy;
        }

        if (await FindReplayAsync(request.RequestId, bundleOperationId, cancellationToken) is { } replay)
        {
            return replay.IsSuccess ? await LoadProgressAsync(bundleOperationId, true, cancellationToken) : replay.Error!;
        }

        var bundle = await db.Bundles.Include(b => b.Operations).SingleAsync(b => b.Id == bundleId, cancellationToken);
        var operation = bundle.Operations.Single(o => o.Id == bundleOperationId);

        if (!station.MachineActive)
        {
            return new Error(ErrorCodes.MachineInactive, $"{station.MachineCode} makinesi pasif durumda.");
        }

        if (station.OperationIds.Count == 0)
        {
            return new Error(
                ErrorCodes.MachineHasNoOperation, $"{station.MachineCode} makinesine bir operasyon tanımlanmamış.");
        }

        // Makine, ana operasyonunu ve kendisine tanımlı ek operasyonları yapabilir.
        if (!station.OperationIds.Contains(operation.OperationId))
        {
            return new Error(
                ErrorCodes.OperationMachineMismatch,
                station.OperationIds.Count == 1 && station.OperationName is not null
                    ? $"Bu makine '{station.OperationName}' operasyonunu yapıyor; demetin bu adımı burada işlenemez."
                    : $"Demetin bu adımı {station.MachineCode} makinesinin yapabildiği operasyonlar arasında değil.");
        }

        var block = bundle.CheckStart(operation);
        if (block == OperationStartBlock.AlreadyInProgress && operation.MachineId == station.MachineId)
        {
            // Aynı makine aynı operasyonu yeniden başlatmak istiyor (yanıt tablete ulaşmamış olabilir).
            return await LoadProgressAsync(bundleOperationId, true, cancellationToken);
        }

        if (block != OperationStartBlock.None)
        {
            return await StartBlockedAsync(bundle, operation, block, cancellationToken);
        }

        if (station.MachineStatus is MachineStatus.Breakdown or MachineStatus.Maintenance)
        {
            return new Error(
                ErrorCodes.MachineNotAvailable,
                station.MachineStatus == MachineStatus.Breakdown
                    ? $"{station.MachineCode} makinesi arızalı; üretim başlatılamaz."
                    : $"{station.MachineCode} makinesi bakımda; üretim başlatılamaz.");
        }

        var machineBundle = await db.BundleOperations
            .Where(o => o.MachineId == station.MachineId && o.Status == BundleOperationStatus.InProgress)
            .Select(o => o.Bundle.BundleNo)
            .FirstOrDefaultAsync(cancellationToken);
        if (machineBundle is not null)
        {
            return new Error(
                ErrorCodes.MachineBusy,
                $"Bu makinede {machineBundle} numaralı demet işlemde; önce onun adedini bildirin.",
                ErrorType.Conflict);
        }

        // Operatör, makinedeki açık oturumdan gelir. Oturum kilidi, başlatma ile çıkışın yarışmasını önler.
        if (!await appLock.TryAcquireAsync(WorkSessionService.MachineLock(station.MachineId), LockTimeout, cancellationToken))
        {
            return Busy;
        }

        var session = await db.WorkSessions
            .AsNoTracking()
            .Where(s => s.MachineId == station.MachineId && s.LogoutAt == null)
            .Select(s => new { s.Id, s.OperatorId })
            .SingleOrDefaultAsync(cancellationToken);
        if (session is null)
        {
            return new Error(
                ErrorCodes.NoActiveSession, "Bu makinede açık oturum yok. Önce personel kartınızı okutun.");
        }

        if (await CheckOperatorAsync(session.OperatorId, cancellationToken) is { } operatorError)
        {
            return operatorError;
        }

        // Makinenin operasyonu oturum açıldıktan sonra değişmiş olabilir: yetkinlik burada da doğrulanır.
        if (await qualifications.CheckAsync(session.OperatorId, station.MachineId, operation.OperationId, cancellationToken)
            is { } notQualified)
        {
            return notQualified;
        }

        if (await EnsureOrderInProductionAsync(bundle.ProductionOrderId, cancellationToken) is { } orderError)
        {
            return orderError;
        }

        // Demeti okutup işe başlayan operatör molada / arada / demet beklemede değildir.
        await interruptions.ResumeForProductionAsync(station.MachineId, cancellationToken);

        var now = UtcNow;
        var resumed = bundle.StartOperation(operation, station.MachineId, session.OperatorId, now);
        AddEvent(
            request.RequestId,
            resumed ? BundleOperationEventType.OperationResumed : BundleOperationEventType.OperationStarted,
            operation, station, now, session.Id);

        if (await SetMachineStatusAsync(
                station, [MachineStatus.Available, MachineStatus.Idle, MachineStatus.Offline], MachineStatus.Running, now,
                cancellationToken) is { } machineError)
        {
            return machineError;
        }

        if (await SaveAsync(transaction, cancellationToken) is { } saveError)
        {
            return saveError;
        }

        return await LoadProgressAsync(bundleOperationId, false, cancellationToken);
    }

    public async Task<Result<OperationProgressDto>> ReportAsync(
        long bundleOperationId, ReportOperationRequest request, CancellationToken cancellationToken)
    {
        var resolved = await stations.ResolveAsync(cancellationToken);
        if (!resolved.IsSuccess)
        {
            return resolved.Error!;
        }

        var station = resolved.Value;
        var bundleId = await FindBundleIdAsync(bundleOperationId, cancellationToken);
        if (bundleId is null)
        {
            return OperationNotFound;
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (!await appLock.TryAcquireAsync(BundleLock(bundleId.Value), LockTimeout, cancellationToken))
        {
            return Busy;
        }

        if (await FindReplayAsync(request.RequestId, bundleOperationId, cancellationToken) is { } replay)
        {
            return replay.IsSuccess ? await LoadProgressAsync(bundleOperationId, true, cancellationToken) : replay.Error!;
        }

        var bundle = await db.Bundles.Include(b => b.Operations).SingleAsync(b => b.Id == bundleId, cancellationToken);
        var operation = bundle.Operations.Single(o => o.Id == bundleOperationId);

        if (operation.Status != BundleOperationStatus.InProgress)
        {
            return new Error(
                ErrorCodes.OperationNotInProgress, "Operasyon işlemde değil; adet bildirmek için önce başlatın.");
        }

        if (operation.MachineId != station.MachineId)
        {
            return new Error(
                ErrorCodes.OperationOnAnotherMachine,
                "Bu operasyon başka bir makinede başlatılmış; adedi o makinenin tabletinden bildirin.",
                ErrorType.Conflict);
        }

        if (request.ProducedQuantity + request.RejectedQuantity > operation.RemainingQuantity)
        {
            var message =
                $"Bildirilen adet ({request.ProducedQuantity} sağlam + {request.RejectedQuantity} hatalı) " +
                $"kalan adedi ({operation.RemainingQuantity}) aşıyor.";
            return new Error(
                ErrorCodes.QuantityExceedsRemaining, message, ErrorType.BusinessRule,
                new Dictionary<string, string[]> { ["producedQuantity"] = [message] });
        }

        // Hatalı adet adedi kalıcı olarak düşürür: işi yapan operatörün görevi hatalı bildirme yetkisi taşımalı ya da
        // yetkili (ör. bant şefi) kartını okutmalıdır.
        if (request.RejectedQuantity > 0
            && await floorAuthorization.AuthorizeAsync(
                FloorAction.ReportReject, operation.OperatorId, request.AuthorizerUid, cancellationToken) is { IsSuccess: false } denied)
        {
            return denied.Error!;
        }

        if (!await appLock.TryAcquireAsync(WorkSessionService.MachineLock(station.MachineId), LockTimeout, cancellationToken))
        {
            return Busy;
        }

        // Adet bildiren operatör işinin başındadır: süren ara / mola kaydı burada biter.
        await interruptions.ResumeForProductionAsync(station.MachineId, cancellationToken);

        // Bildirim oturum gerektirmez (yarım kalan işi başkası da bildirebilir); işi yapan operatörün
        // oturumu hâlâ açıksa olaylar o oturuma bağlanır.
        var sessionId = await db.WorkSessions
            .Where(s => s.MachineId == station.MachineId && s.OperatorId == operation.OperatorId && s.LogoutAt == null)
            .Select(s => (long?)s.Id)
            .SingleOrDefaultAsync(cancellationToken);

        var flow = await LoadFlowAsync(bundle, cancellationToken);
        var now = UtcNow;
        // Verim: bildirilen sağlam adedin standart süresi / son başlatmadan bu yana harcanan net süre.
        var runSeconds = await RunElapsedSecondsAsync(operation.Id, station.MachineId, now, cancellationToken);
        int? efficiency = request.ProducedQuantity > 0 && runSeconds is > 0
            ? (int)Math.Round((double)(request.ProducedQuantity * operation.Sam) * 60 / runSeconds.Value * 100)
            : null;
        bundle.ReportOperation(operation, request.ProducedQuantity, request.RejectedQuantity, flow, now);

        if (request.ProducedQuantity > 0)
        {
            AddEvent(
                request.RequestId, BundleOperationEventType.QuantityReported, operation, station, now, sessionId,
                produced: request.ProducedQuantity);
        }

        if (request.RejectedQuantity > 0)
        {
            AddEvent(
                request.RequestId, BundleOperationEventType.RejectionReported, operation, station, now, sessionId,
                rejected: request.RejectedQuantity);
        }

        AddEvent(
            request.RequestId,
            operation.Status == BundleOperationStatus.Completed
                ? BundleOperationEventType.OperationCompleted
                : BundleOperationEventType.OperationPaused,
            operation, station, now, sessionId,
            runSeconds: runSeconds is null ? null : (int)Math.Round(runSeconds.Value), efficiencyPercent: efficiency);

        if (operation.ReworkQuantity == 0)
        {
            // Tamire gönderilen parçalar yeniden işlendi: tamir işleri tekrar kontrol bekler.
            var reworked = await db.ReworkTasks
                .Where(t => t.BundleOperationId == operation.Id && t.Status == ReworkStatus.Open)
                .ToListAsync(cancellationToken);
            foreach (var task in reworked)
            {
                task.MarkDone(now);
            }
        }

        if (bundle.Status == BundleStatus.Completed)
        {
            // Tamamlanan demetin kartı serbest kalır; yeni bir demete atanabilir.
            var assignment = await db.BundleCardAssignments
                .SingleOrDefaultAsync(a => a.BundleId == bundle.Id && a.ReleasedAt == null, cancellationToken);
            assignment?.Release(now);

            // Sağlam parçası kalmayan demet depoya alınamaz; bekleyen işlerde kalmasın diye kapanır.
            if (bundle.FinishedQuantity(await LoadFlowAsync(bundle, cancellationToken)) <= 0)
            {
                bundle.Close();
            }
        }

        if (await SetMachineStatusAsync(station, [MachineStatus.Running], MachineStatus.Idle, now, cancellationToken)
            is { } machineError)
        {
            return machineError;
        }

        if (await SaveAsync(transaction, cancellationToken) is { } saveError)
        {
            return saveError;
        }

        return await LoadProgressAsync(bundleOperationId, false, cancellationToken, efficiency);
    }

    public async Task<Result<ReleasedOperationDto>> ReleaseAsync(long bundleOperationId, CancellationToken cancellationToken)
    {
        var bundleId = await FindBundleIdAsync(bundleOperationId, cancellationToken);
        if (bundleId is null)
        {
            return OperationNotFound;
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (!await appLock.TryAcquireAsync(BundleLock(bundleId.Value), LockTimeout, cancellationToken))
        {
            return Busy;
        }

        var bundle = await db.Bundles.Include(b => b.Operations).SingleAsync(b => b.Id == bundleId, cancellationToken);
        var operation = bundle.Operations.Single(o => o.Id == bundleOperationId);
        if (operation.Status != BundleOperationStatus.InProgress || operation.MachineId is not { } machineId)
        {
            return new Error(ErrorCodes.OperationNotInProgress, "Operasyon işlemde değil; bırakılacak süren iş yok.");
        }

        if (!await appLock.TryAcquireAsync(WorkSessionService.MachineLock(machineId), LockTimeout, cancellationToken))
        {
            return Busy;
        }

        // Olay, işi başlatan operatörün hâlâ açık olan oturumuna bağlanır; oturum kapanabilir hale gelir.
        var sessionId = await db.WorkSessions
            .Where(s => s.MachineId == machineId && s.OperatorId == operation.OperatorId && s.LogoutAt == null)
            .Select(s => (long?)s.Id)
            .SingleOrDefaultAsync(cancellationToken);

        var flow = await LoadFlowAsync(bundle, cancellationToken);
        var now = UtcNow;
        var runSeconds = await RunElapsedSecondsAsync(operation.Id, machineId, now, cancellationToken);
        // Adetsiz bildirim: üretilen / hatalı değişmez, operasyon kalan adediyle bekletilir.
        bundle.ReportOperation(operation, 0, 0, flow, now);
        AddEvent(
            Guid.NewGuid(), BundleOperationEventType.OperationPaused, operation, machineId, null, now, sessionId,
            runSeconds: runSeconds is null ? null : (int)Math.Round(runSeconds.Value));

        if (await SetMachineStatusAsync(machineId, null, [MachineStatus.Running], MachineStatus.Idle, now, cancellationToken)
            is { } machineError)
        {
            return machineError;
        }

        if (await SaveAsync(transaction, cancellationToken) is { } saveError)
        {
            return saveError;
        }

        return new ReleasedOperationDto(bundle.Id, operation.Id, operation.RemainingQuantity);
    }

    public async Task<Result<ManualReportOptionsDto>> GetManualReportOptionsAsync(
        long bundleOperationId, CancellationToken cancellationToken)
    {
        var operation = await db.BundleOperations
            .AsNoTracking()
            .Where(o => o.Id == bundleOperationId)
            .Select(o => new
            {
                o.Id,
                o.SequenceNo,
                o.OperationId,
                OperationName = o.Operation.Name,
                o.Status,
                Remaining = o.PlannedQuantity - o.ProducedQuantity - o.RejectedQuantity,
                o.MachineId,
                MachineCode = o.Machine == null ? null : o.Machine.MachineCode,
                o.OperatorId,
                OperatorName = o.Operator == null ? null : o.Operator.FirstName + " " + o.Operator.LastName,
            })
            .SingleOrDefaultAsync(cancellationToken);
        if (operation is null)
        {
            return OperationNotFound;
        }

        var machines = await CapableMachines(operation.OperationId)
            .OrderBy(m => m.MachineCode)
            .Select(m => new ManualReportMachineDto(m.Id, m.MachineCode, m.MachineName, m.Status))
            .ToListAsync(cancellationToken);

        var inProgress = operation.Status == BundleOperationStatus.InProgress;
        return new ManualReportOptionsDto(
            operation.Id, operation.SequenceNo, operation.OperationName, operation.Status, operation.Remaining, inProgress,
            inProgress ? operation.MachineId : null, inProgress ? operation.MachineCode : null,
            inProgress ? operation.OperatorId : null, inProgress ? operation.OperatorName : null,
            machines);
    }

    public async Task<Result<OperationProgressDto>> ManualReportAsync(
        long bundleOperationId, ManualReportRequest request, CancellationToken cancellationToken)
    {
        var bundleId = await FindBundleIdAsync(bundleOperationId, cancellationToken);
        if (bundleId is null)
        {
            return OperationNotFound;
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (!await appLock.TryAcquireAsync(BundleLock(bundleId.Value), LockTimeout, cancellationToken))
        {
            return Busy;
        }

        if (await FindReplayAsync(request.RequestId, bundleOperationId, cancellationToken) is { } replay)
        {
            return replay.IsSuccess ? await LoadProgressAsync(bundleOperationId, true, cancellationToken) : replay.Error!;
        }

        var bundle = await db.Bundles.Include(b => b.Operations).SingleAsync(b => b.Id == bundleId, cancellationToken);
        var operation = bundle.Operations.Single(o => o.Id == bundleOperationId);
        var enteredBy = currentUser.UserName ?? SystemUser;
        var now = UtcNow;
        var wasInProgress = operation.Status == BundleOperationStatus.InProgress;
        var withoutOperator = request.Attribution == ManualReportAttribution.NoOperator;
        var countsForPerformance = request.Attribution == ManualReportAttribution.Operator;
        int machineId;
        long? sessionId = null;

        if (wasInProgress)
        {
            // Askıda kalan iş: işi başlatan makine ve operatör adına bildirilir; operatörsüz seçildiyse kimseye yazılmaz.
            machineId = operation.MachineId!.Value;
            if (!await appLock.TryAcquireAsync(WorkSessionService.MachineLock(machineId), LockTimeout, cancellationToken))
            {
                return Busy;
            }

            sessionId = await db.WorkSessions
                .Where(s => s.MachineId == machineId && s.OperatorId == operation.OperatorId && s.LogoutAt == null)
                .Select(s => (long?)s.Id)
                .SingleOrDefaultAsync(cancellationToken);
        }
        else
        {
            if (request.MachineId is not { } chosenMachine)
            {
                return ReferenceValidation.InvalidReference("machineId", "İşin yapıldığı makineyi seçin.");
            }

            var chosenOperator = withoutOperator ? null : request.OperatorId;
            if (!withoutOperator && chosenOperator is null)
            {
                return ReferenceValidation.InvalidReference("operatorId", "İşi yapan operatörü seçin ya da operatörsüz bildirin.");
            }

            var block = bundle.CheckStart(operation, requireCard: false);
            if (block != OperationStartBlock.None)
            {
                return await StartBlockedAsync(bundle, operation, block, cancellationToken);
            }

            if (!await CapableMachines(operation.OperationId).AnyAsync(m => m.Id == chosenMachine, cancellationToken))
            {
                return ReferenceValidation.InvalidReference(
                    "machineId", "Seçilen makine bu operasyonu yapamıyor ya da pasif.");
            }

            if (chosenOperator is not null)
            {
                var operatorActive = await db.Operators
                    .Where(o => o.Id == chosenOperator)
                    .Select(o => (bool?)o.IsActive)
                    .SingleOrDefaultAsync(cancellationToken);
                if (operatorActive is null)
                {
                    return ReferenceValidation.InvalidReference("operatorId", "Operatör bulunamadı.");
                }

                if (operatorActive == false)
                {
                    return new Error(ErrorCodes.OperatorInactive, "Operatör pasif durumda.");
                }
            }

            if (await EnsureOrderInProductionAsync(bundle.ProductionOrderId, cancellationToken) is { } orderError)
            {
                return orderError;
            }

            machineId = chosenMachine;
            var resumed = bundle.StartOperation(operation, machineId, chosenOperator, now, requireCard: false);
            AddEvent(
                request.RequestId,
                resumed ? BundleOperationEventType.OperationResumed : BundleOperationEventType.OperationStarted,
                operation, machineId, null, now, null, enteredBy: enteredBy, countsForPerformance: countsForPerformance);
        }

        if (withoutOperator)
        {
            // Operatörsüz kapatmada adet kimsenin oturumuna da yazılmaz.
            sessionId = null;
        }

        if (request.ProducedQuantity + request.RejectedQuantity > operation.RemainingQuantity)
        {
            var message =
                $"Bildirilen adet ({request.ProducedQuantity} sağlam + {request.RejectedQuantity} hatalı) " +
                $"kalan adedi ({operation.RemainingQuantity}) aşıyor.";
            return new Error(
                ErrorCodes.QuantityExceedsRemaining, message, ErrorType.BusinessRule,
                new Dictionary<string, string[]> { ["producedQuantity"] = [message] });
        }

        // Elle bildirimde gerçek çalışma süresi bilinmez: süre ve verim yazılmaz. Adet seçime göre operatörün performansına
        // ve primine sayılır ya da sayılmaz; operatörsüz bildirimde kimseye yazılmaz.
        var flow = await LoadFlowAsync(bundle, cancellationToken);
        bundle.ReportOperation(operation, request.ProducedQuantity, request.RejectedQuantity, flow, now);

        if (request.ProducedQuantity > 0)
        {
            AddEvent(
                request.RequestId, BundleOperationEventType.QuantityReported, operation, machineId, null, now, sessionId,
                produced: request.ProducedQuantity, enteredBy: enteredBy, withoutOperator: withoutOperator,
                countsForPerformance: countsForPerformance);
        }

        if (request.RejectedQuantity > 0)
        {
            AddEvent(
                request.RequestId, BundleOperationEventType.RejectionReported, operation, machineId, null, now, sessionId,
                rejected: request.RejectedQuantity, enteredBy: enteredBy, withoutOperator: withoutOperator,
                countsForPerformance: countsForPerformance);
        }

        AddEvent(
            request.RequestId,
            operation.Status == BundleOperationStatus.Completed
                ? BundleOperationEventType.OperationCompleted
                : BundleOperationEventType.OperationPaused,
            operation, machineId, null, now, sessionId, enteredBy: enteredBy, withoutOperator: withoutOperator,
            countsForPerformance: countsForPerformance);

        if (withoutOperator)
        {
            // Askıdaki iş operatörsüz kapatıldıysa operasyon da işi başlatan operatörde görünmez.
            operation.OperatorId = null;
        }

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

        if (bundle.Status == BundleStatus.Completed)
        {
            var assignment = await db.BundleCardAssignments
                .SingleOrDefaultAsync(a => a.BundleId == bundle.Id && a.ReleasedAt == null, cancellationToken);
            assignment?.Release(now);
        }

        // Askıdaki iş bitti: makine serbest kalır. Başlamamış işte makinenin durumuna dokunulmaz.
        if (wasInProgress
            && await SetMachineStatusAsync(machineId, null, [MachineStatus.Running], MachineStatus.Idle, now, cancellationToken)
                is { } machineError)
        {
            return machineError;
        }

        if (await SaveAsync(transaction, cancellationToken) is { } saveError)
        {
            return saveError;
        }

        return await LoadProgressAsync(bundleOperationId, false, cancellationToken);
    }

    /// <summary>Operasyonu ana ya da ek operasyonu olarak yapabilen aktif makineler.</summary>
    private IQueryable<Machine> CapableMachines(int operationId) =>
        db.Machines.Where(m => m.IsActive
            && (m.OperationId == operationId
                || db.MachineOperations.Any(x => x.MachineId == m.Id && x.OperationId == operationId)));

    public async Task<Result<OperationProgressDto?>> GetCurrentAsync(CancellationToken cancellationToken)
    {
        var resolved = await stations.ResolveAsync(cancellationToken);
        if (!resolved.IsSuccess)
        {
            return resolved.Error!;
        }

        var machineId = resolved.Value.MachineId;
        var currentId = await db.BundleOperations
            .Where(o => o.MachineId == machineId && o.Status == BundleOperationStatus.InProgress)
            .Select(o => (long?)o.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return Result<OperationProgressDto?>.Success(
            currentId is null ? null : await LoadProgressAsync(currentId.Value, false, cancellationToken));
    }

    public async Task<Result<IReadOnlyList<BundleOperationEventDto>>> GetBundleEventsAsync(
        int bundleId, CancellationToken cancellationToken)
    {
        if (!await db.Bundles.AnyAsync(b => b.Id == bundleId, cancellationToken))
        {
            return new Error(ErrorCodes.NotFound, "Demet bulunamadı.", ErrorType.NotFound);
        }

        // Geçmiş kayıtlar, sonradan silinen makine / operatör için de eksiksiz görünür.
        var events = await db.BundleOperationEvents
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(e => e.BundleId == bundleId)
            .OrderBy(e => e.Id)
            .Take(MaxEvents)
            .Select(e => new BundleOperationEventDto(
                e.Id,
                e.EventType,
                e.BundleOperation.SequenceNo,
                e.BundleOperation.Operation.Name,
                e.Machine.MachineCode,
                e.Operator == null ? NoOperatorName : e.Operator.FirstName + " " + e.Operator.LastName,
                e.ProducedQuantity,
                e.RejectedQuantity,
                e.OccurredAt,
                e.EnteredBy,
                e.CountsForPerformance))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<BundleOperationEventDto>>.Success(events);
    }

    /// <summary>Operatörsüz elle kapatılan işin olaylarında operatör yerine görünen ad.</summary>
    public const string NoOperatorName = "Ofis kapatması";

    private static string BundleLock(int bundleId) => $"bundle:{bundleId}";

    private Task<int?> FindBundleIdAsync(long bundleOperationId, CancellationToken cancellationToken) =>
        db.BundleOperations
            .Where(o => o.Id == bundleOperationId)
            .Select(o => (int?)o.BundleId)
            .SingleOrDefaultAsync(cancellationToken);

    /// <summary>
    /// İstek daha önce işlendiyse başarılı (tekrar) döner; aynı kimlik başka bir operasyon için
    /// kullanılmışsa hata. Hiç görülmediyse null.
    /// </summary>
    private async Task<Result?> FindReplayAsync(Guid requestId, long bundleOperationId, CancellationToken cancellationToken)
    {
        var processedFor = await db.BundleOperationEvents
            .Where(e => e.RequestId == requestId)
            .Select(e => (long?)e.BundleOperationId)
            .FirstOrDefaultAsync(cancellationToken);

        if (processedFor is null)
        {
            return null;
        }

        return processedFor == bundleOperationId
            ? Result.Success()
            : new Error(
                ErrorCodes.RequestIdReused, "Bu istek kimliği başka bir işlem için kullanılmış.", ErrorType.Conflict);
    }

    private async Task<Error> StartBlockedAsync(
        Bundle bundle, BundleOperation operation, OperationStartBlock block, CancellationToken cancellationToken)
    {
        switch (block)
        {
            case OperationStartBlock.BundleNotReleased:
                return new Error(ErrorCodes.BundleNotReleased, "Demete kart atanmamış; üretime verilmemiş.");

            case OperationStartBlock.BundleClosed:
                return new Error(ErrorCodes.BundleNotActive, "Demet tamamlanmış, kapatılmış ya da iptal edilmiş.");

            case OperationStartBlock.BundleOnQualityHold or OperationStartBlock.OperationOnQualityHold:
                return new Error(ErrorCodes.BundleOnQualityHold, "Demet kalite bekletmesinde; kalite serbest bırakmadan devam edilemez.");

            case OperationStartBlock.OperationCompleted:
                return new Error(ErrorCodes.OperationAlreadyCompleted, "Bu operasyon bu demet için zaten tamamlanmış.");

            case OperationStartBlock.OperationCancelled:
                return new Error(ErrorCodes.OperationCancelled, "Bu operasyon iptal edilmiş; işlenecek parça kalmamış.");

            case OperationStartBlock.BundleAtSubcontractor:
            {
                var sendNo = await db.SubcontractSendLines
                    .Where(l => l.BundleId == bundle.Id
                        && (l.SubcontractSend.Status == SubcontractSendStatus.Open
                            || l.SubcontractSend.Status == SubcontractSendStatus.PartiallyReturned))
                    .OrderByDescending(l => l.Id)
                    .Select(l => l.SubcontractSend.SendNo + " (" + l.SubcontractSend.Supplier.Name + ")")
                    .FirstOrDefaultAsync(cancellationToken);
                return new Error(
                    SubcontractErrors.BundleAtSubcontractor,
                    $"Demet fasonda{(sendNo is null ? "" : $": {sendNo}")}. Fason dönüşü işlenmeden sahada işlenemez.");
            }

            case OperationStartBlock.AnotherOperationUnfinished:
            {
                var unfinished = bundle.UnfinishedOperations().First(o => o != operation);
                var name = await db.Operations
                    .IgnoreQueryFilters()
                    .Where(o => o.Id == unfinished.OperationId)
                    .Select(o => o.Name)
                    .SingleOrDefaultAsync(cancellationToken);
                return new Error(
                    ErrorCodes.OperationUnfinished,
                    $"Demetin {unfinished.SequenceNo} {name} operasyonu yarım kalmış ({unfinished.RemainingQuantity} adet). " +
                    "O operasyon tamamlanmadan başka operasyona geçilemez.");
            }

            case OperationStartBlock.PredecessorsIncomplete:
            {
                var flow = await LoadFlowAsync(bundle, cancellationToken);
                var missingIds = bundle.MissingPredecessors(operation, flow).Select(o => o.OperationId).ToList();
                var names = await db.Operations
                    .IgnoreQueryFilters()
                    .Where(o => missingIds.Contains(o.Id))
                    .ToDictionaryAsync(o => o.Id, o => o.Name, cancellationToken);
                var missing = bundle.MissingPredecessors(operation, flow)
                    .Select(o => $"{o.SequenceNo} {names.GetValueOrDefault(o.OperationId)}".TrimEnd());
                return new Error(
                    ErrorCodes.PredecessorsNotCompleted,
                    $"Önce şu operasyonlar tamamlanmalı: {string.Join(", ", missing)}.");
            }

            default:
            {
                // Demetin bu ya da başka bir operasyonu başka bir makinede işlemde.
                var active = bundle.Operations.First(o => o.Status == BundleOperationStatus.InProgress);
                var machineCode = await db.Machines
                    .IgnoreQueryFilters()
                    .Where(m => m.Id == active.MachineId)
                    .Select(m => m.MachineCode)
                    .SingleOrDefaultAsync(cancellationToken);
                return new Error(
                    ErrorCodes.BundleInUse,
                    $"Demet şu an {machineCode} makinesinde işlemde; aynı anda ikinci bir yerde işlenemez.",
                    ErrorType.Conflict);
            }
        }
    }

    private async Task<Error?> CheckOperatorAsync(int operatorId, CancellationToken cancellationToken)
    {
        var machineOperator = await db.Operators
            .AsNoTracking()
            .Where(o => o.Id == operatorId)
            .Select(o => new { o.IsActive })
            .SingleOrDefaultAsync(cancellationToken);
        if (machineOperator is null)
        {
            return ReferenceValidation.InvalidReference("operatorId", "Operatör bulunamadı.");
        }

        if (!machineOperator.IsActive)
        {
            return new Error(ErrorCodes.OperatorInactive, "Operatör pasif durumda.");
        }

        var busyOn = await db.BundleOperations
            .Where(o => o.OperatorId == operatorId && o.Status == BundleOperationStatus.InProgress)
            .Select(o => o.Machine!.MachineCode)
            .FirstOrDefaultAsync(cancellationToken);

        return busyOn is null
            ? null
            : new Error(
                ErrorCodes.OperatorBusy,
                $"Operatörün {busyOn} makinesinde süren bir işlemi var; önce onun adedi bildirilmeli.",
                ErrorType.Conflict);
    }

    /// <summary>Sipariş üretime açık mı; onaylı sipariş ilk operasyon başlatıldığında "üretimde" olur.</summary>
    private async Task<Error?> EnsureOrderInProductionAsync(int orderId, CancellationToken cancellationToken)
    {
        var status = await db.ProductionOrders
            .Where(o => o.Id == orderId)
            .Select(o => o.Status)
            .SingleAsync(cancellationToken);

        if (status == ProductionOrderStatus.InProduction)
        {
            return null;
        }

        if (status != ProductionOrderStatus.Released)
        {
            return new Error(ErrorCodes.OrderNotInProduction, "Demetin siparişi üretime açık değil.");
        }

        // Siparişin birçok demeti aynı anda başlatılabilir: durum geçişi sıraya girer, yalnızca ilki yapar.
        if (!await appLock.TryAcquireAsync($"production-order-start:{orderId}", LockTimeout, cancellationToken))
        {
            return Busy;
        }

        var order = await db.ProductionOrders.SingleAsync(o => o.Id == orderId, cancellationToken);
        if (order.Status == ProductionOrderStatus.Released)
        {
            order.TransitionTo(ProductionOrderStatus.InProduction);
        }

        return order.Status == ProductionOrderStatus.InProduction
            ? null
            : new Error(ErrorCodes.OrderNotInProduction, "Demetin siparişi üretime açık değil.");
    }

    private Task<OperationFlow> LoadFlowAsync(Bundle bundle, CancellationToken cancellationToken) =>
        db.LoadFlowAsync(bundle, cancellationToken);

    private void AddEvent(
        Guid requestId, BundleOperationEventType type, BundleOperation operation, Station station, DateTime now,
        long? workSessionId, int produced = 0, int rejected = 0, int? runSeconds = null, int? efficiencyPercent = null) =>
        AddEvent(
            requestId, type, operation, station.MachineId, station.TabletId, now, workSessionId, produced, rejected, runSeconds,
            efficiencyPercent);

    private void AddEvent(
        Guid requestId, BundleOperationEventType type, BundleOperation operation, int machineId, int? tabletId, DateTime now,
        long? workSessionId, int produced = 0, int rejected = 0, int? runSeconds = null, int? efficiencyPercent = null,
        string? enteredBy = null, bool withoutOperator = false, bool countsForPerformance = true) =>
        db.BundleOperationEvents.Add(new BundleOperationEvent
        {
            EnteredBy = enteredBy,
            CountsForPerformance = countsForPerformance && !withoutOperator,
            RequestId = requestId,
            EventType = type,
            BundleId = operation.BundleId,
            BundleOperationId = operation.Id,
            MachineId = machineId,
            OperatorId = withoutOperator ? null : operation.OperatorId,
            WorkSessionId = workSessionId,
            TabletId = tabletId,
            ProducedQuantity = produced,
            RejectedQuantity = rejected,
            RunSeconds = runSeconds,
            EfficiencyPercent = efficiencyPercent,
            OccurredAt = now,
        });

    /// <summary>
    /// Makinenin çalışma durumunu üretim akışına göre günceller (Çalışıyor / Boşta) ve durum geçmişine
    /// yazar. Makine <paramref name="from"/> durumlarından birinde değilse (örn. arızalı) dokunulmaz.
    /// Bu geçişler çok sık olduğundan makine kaydının denetim izine yazılmaz; geçmişi durum kayıtlarıdır.
    /// </summary>
    private Task<Error?> SetMachineStatusAsync(
        Station station, MachineStatus[] from, MachineStatus target, DateTime now, CancellationToken cancellationToken) =>
        SetMachineStatusAsync(station.MachineId, station.TabletId, from, target, now, cancellationToken);

    private async Task<Error?> SetMachineStatusAsync(
        int machineId, int? tabletId, MachineStatus[] from, MachineStatus target, DateTime now,
        CancellationToken cancellationToken)
    {
        if (!await appLock.TryAcquireAsync($"machine-status:{machineId}", LockTimeout, cancellationToken))
        {
            return Busy;
        }

        var current = await db.Machines
            .Where(m => m.Id == machineId)
            .Select(m => m.Status)
            .SingleAsync(cancellationToken);
        if (!from.Contains(current))
        {
            return null;
        }

        await db.MachineStatusLogs
            .Where(l => l.MachineId == machineId && l.EndedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(l => l.EndedAt, now), cancellationToken);
        await db.Machines
            .Where(m => m.Id == machineId)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.Status, target), cancellationToken);

        db.MachineStatusLogs.Add(new MachineStatusLog
        {
            MachineId = machineId,
            Status = target,
            StartedAt = now,
            ChangedBy = currentUser.UserName ?? SystemUser,
            TabletId = tabletId,
        });

        return null;
    }

    private async Task<Error?> SaveAsync(IDbContextTransaction transaction, CancellationToken cancellationToken)
    {
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
            // Tekil index'ler: makine ya da operatör aynı anda başka bir demette işleme başladı.
            return new Error(
                ErrorCodes.ResourceBusy,
                "Makine ya da operatör aynı anda başka bir demette işleme başladı. Lütfen tekrar deneyin.",
                ErrorType.Conflict);
        }

        await transaction.CommitAsync(cancellationToken);
        return null;
    }

    /// <summary>
    /// İşlemdeki operasyonda son başlatmadan (ya da kaldığı yerden sürdürmeden) bu yana geçen net süre:
    /// o makinedeki mola, ara ve arıza süreleri düşülür (demet bekleme düşülmez). Başlatma kaydı yoksa boş.
    /// </summary>
    private async Task<double?> RunElapsedSecondsAsync(
        long bundleOperationId, int machineId, DateTime now, CancellationToken cancellationToken)
    {
        var runStart = await db.BundleOperationEvents
            .Where(e => e.BundleOperationId == bundleOperationId
                && (e.EventType == BundleOperationEventType.OperationStarted
                    || e.EventType == BundleOperationEventType.OperationResumed))
            .OrderByDescending(e => e.Id)
            .Select(e => (DateTime?)e.OccurredAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (runStart is not { } start || start >= now)
        {
            return runStart is null ? null : 0;
        }

        var pauses = await db.WorkSessionPauses
            .AsNoTracking()
            .Where(p => p.WorkSession.MachineId == machineId
                && p.Type != WorkSessionPauseType.WaitingForBundle
                && p.StartedAt < now
                && (p.EndedAt == null || p.EndedAt > start))
            .Select(p => new { p.StartedAt, p.EndedAt })
            .ToListAsync(cancellationToken);
        var paused = pauses.Sum(p =>
        {
            var from = p.StartedAt > start ? p.StartedAt : start;
            var to = p.EndedAt is { } ended && ended < now ? ended : now;
            return Math.Max((to - from).TotalSeconds, 0);
        });

        return Math.Max((now - start).TotalSeconds - paused, 0);
    }

    private async Task<OperationProgressDto> LoadProgressAsync(
        long bundleOperationId, bool duplicate, CancellationToken cancellationToken, int? efficiencyPercent = null)
    {
        var row = await db.BundleOperations
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(o => o.Id == bundleOperationId)
            .Select(o => new
            {
                o.MachineId,
                Progress = new OperationProgressDto(
                    duplicate,
                    o.Id,
                    o.BundleId,
                    o.Bundle.BundleNo,
                    o.Bundle.Status,
                    o.SequenceNo,
                    o.Operation.Name,
                    o.Status,
                    o.PlannedQuantity,
                    o.ProducedQuantity,
                    o.RejectedQuantity,
                    o.PlannedQuantity - o.ProducedQuantity - o.RejectedQuantity,
                    o.Machine!.MachineCode,
                    o.OperatorId,
                    o.Operator == null ? null : o.Operator.FirstName + " " + o.Operator.LastName,
                    o.StartedAt,
                    o.CompletedAt,
                    o.Bundle.Operations
                        .Where(x => x.Status == BundleOperationStatus.Ready)
                        .OrderBy(x => x.SequenceNo)
                        .Select(x => x.Operation.Name)
                        .ToList(),
                    o.Sam,
                    null,
                    null,
                    efficiencyPercent),
            })
            .SingleAsync(cancellationToken);

        // Geri sayım yalnızca işlemdeki operasyon içindir: hedef süre kalan adede göre, geçen süre net olarak.
        if (row.Progress.Status != BundleOperationStatus.InProgress || row.MachineId is not { } machineId)
        {
            return row.Progress;
        }

        var elapsed = await RunElapsedSecondsAsync(bundleOperationId, machineId, UtcNow, cancellationToken);
        return row.Progress with
        {
            RunTargetSeconds = (int)Math.Round(row.Progress.Sam * row.Progress.RemainingQuantity * 60),
            RunElapsedSeconds = elapsed is null ? null : (int)Math.Round(elapsed.Value),
        };
    }
}
