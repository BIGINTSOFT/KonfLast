using ERP.Boyahane.Application.Features.Floor;
using ERP.Boyahane.Domain.Authorization;
using ERP.Boyahane.Application.Features.Floor;
using ERP.Boyahane.Domain.Entities;
using ERP.Boyahane.Domain.Enums;
using ERP.Boyahane.Domain.Rules;
using ERP.Core.Application.Common;
using ERP.Core.Domain.Common;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using static ERP.Core.Application.Features.MasterData.MasterDataRules;

namespace ERP.Boyahane.Application.Features.Batches;

/// <summary>Adımı yürüten: tablet (operatör ve bağlı makineyle) ya da ofisten elle bildirimde boş tablet.</summary>
public sealed record BatchStepActor(int? TabletId, int? OperatorId, int? MachineId);

public sealed record BatchConsumptionRequest(int DyeChemicalId, decimal ActualKilograms);

/// <param name="Kilograms">Ölçülen kilo; uygun malda zorunludur.</param>
/// <param name="Meters">Ölçülen metre; kumaşta uygun malda zorunludur, iplikte boştur.</param>
/// <param name="OverrideNote">Çıkış ölçüsü sınırını aşma gerekçesi; yalnızca ofisten ve yetkiyle.</param>
public sealed record BatchItemResultRequest(int ItemId, FinalQualityResult Result, string? Note = null, decimal? Kilograms = null, decimal? Meters = null, string? OverrideNote = null);

/// <param name="RequestId">Tablette işlem kimliği; aynı kimlik ikinci kez işlenmez. Ofisten boş gelir.</param>
/// <param name="Consumptions">Boyama, söküm ve baskı: gerçek miktarlar. Gönderilmeyen reçete satırı hesaplanan miktarla yazılır.</param>
/// <param name="ColorResult">Renk kontrolü sonucu.</param>
/// <param name="ItemResults">Final kalite: top ya da lot bazında sonuç; hepsi zorunludur.</param>
/// <param name="StrippingRecipeId">Renk kontrolünde "söküm + yeniden boya" seçildiyse uygulanacak söküm reçetesi; zorunludur.</param>
public sealed record CompleteBatchStepRequest(
    Guid? RequestId = null,
    IReadOnlyList<BatchConsumptionRequest>? Consumptions = null,
    ColorCheckResult? ColorResult = null,
    IReadOnlyList<BatchItemResultRequest>? ItemResults = null,
    string? Note = null,
    int? StrippingRecipeId = null);

public sealed class CompleteBatchStepRequestValidator : AbstractValidator<CompleteBatchStepRequest>
{
    public CompleteBatchStepRequestValidator()
    {
        RuleFor(x => x.Note).OptionalText(500);
        RuleFor(x => x.StrippingRecipeId).NotNull().WithMessage("Söküm reçetesini seçin.").When(x => x.ColorResult == ColorCheckResult.Stripping);
        RuleForEach(x => x.Consumptions).ChildRules(line =>
        {
            line.RuleFor(l => l.DyeChemicalId).GreaterThan(0).WithMessage("Boya ya da kimyasal seçilmelidir.");
            line.RuleFor(l => l.ActualKilograms).InclusiveBetween(0, 1_000_000).WithMessage("Miktar sıfır ya da daha büyük olmalıdır.");
        });
        RuleForEach(x => x.ItemResults).ChildRules(item =>
        {
            item.RuleFor(i => i.ItemId).GreaterThan(0);
            item.RuleFor(i => i.Result).IsInEnum().WithMessage("Geçerli bir sonuç seçin.");
            item.RuleFor(i => i.Note).OptionalText(500);
            item.RuleFor(i => i.OverrideNote).OptionalText(500);
            item.RuleFor(i => i.Kilograms).InclusiveBetween(0.01m, 9_999_999).WithMessage("Kilo sıfırdan büyük olmalıdır.").When(i => i.Kilograms is not null);
            item.RuleFor(i => i.Meters).InclusiveBetween(0.01m, 9_999_999).WithMessage("Metre sıfırdan büyük olmalıdır.").When(i => i.Meters is not null);
        });
    }
}

/// <summary>Web'den elle bildirim: makine adımında makine seçilir, boşsa partinin makinesi kullanılır.</summary>
public sealed record StartBatchStepRequest(int? MachineId = null);

public sealed record SetBatchCardRequest(string? Uid);

public sealed partial class BoyahaneBatchService
{
    private const string StepState = "BATCH_STEP_INVALID_STATE";

    public async Task<Result<BoyahaneBatchDto>> SetCardAsync(int id, SetBatchCardRequest request, CancellationToken cancellationToken)
    {
        var batch = await db.Set<BoyahaneBatch>().SingleOrDefaultAsync(b => b.Id == id, cancellationToken);
        if (batch is null)
        {
            return NotFound;
        }

        if (batch.Status is BatchStatus.Completed or BatchStatus.Cancelled)
        {
            return new Error(InvalidState, "Tamamlanan ya da iptal edilen partiye kart atanamaz.", ErrorType.Conflict);
        }

        string? uid = null;
        if (!string.IsNullOrWhiteSpace(request.Uid))
        {
            uid = RfidUid.Normalize(request.Uid);
            if (uid is null)
            {
                return new Error(ErrorCodes.ValidationError, "Kart numarası en fazla 10 harf ya da rakam olmalıdır.", ErrorType.Validation,
                    new Dictionary<string, string[]> { ["uid"] = ["Kart numarası en fazla 10 harf ya da rakam olmalıdır."] });
            }

            if (await BoyahaneCards.CheckAsync(db, uid, BoyahaneCardType.Batch, cancellationToken) is { } cardError)
            {
                return cardError;
            }

            var other = await db.Set<BoyahaneBatch>().Where(b => b.CardUid == uid && b.Id != id).Select(b => b.BatchNo).FirstOrDefaultAsync(cancellationToken);
            if (other is not null)
            {
                return new Error(InvalidState, $"Bu kart {other} partisinde; önce o partiden çıkarın.", ErrorType.Conflict);
            }
        }

        batch.CardUid = uid;
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    /// <summary>Okutulan kod: RFID kart numarası ya da kağıt karttaki barkod (parti no).</summary>
    public async Task<Result<BoyahaneBatchDto>> FindByCardAsync(string code, CancellationToken cancellationToken)
    {
        var uid = RfidUid.Normalize(code);
        if (uid is not null && await BoyahaneCards.CheckAsync(db, uid, BoyahaneCardType.Batch, cancellationToken) is { } cardError)
        {
            return cardError;
        }

        var batchNo = code.Trim().ToUpperInvariant();
        var batch = await db.Set<BoyahaneBatch>().AsNoTracking()
            .Where(b => uid != null ? b.CardUid == uid : b.BatchNo == batchNo)
            .Select(b => new { b.Id, b.BatchNo, b.Status })
            .SingleOrDefaultAsync(cancellationToken);
        if (batch is null)
        {
            return new Error("BATCH_CARD_NOT_FOUND", "Bu kart bir partiye atanmamış.", ErrorType.Conflict);
        }

        return batch.Status is BatchStatus.Completed or BatchStatus.Cancelled
            ? new Error("BATCH_CARD_NOT_FOUND", $"{batch.BatchNo} partisi kapanmış.", ErrorType.Conflict)
            : await GetAsync(batch.Id, cancellationToken);
    }

    public async Task<Result<BoyahaneBatchDto>> StartStepAsync(int batchId, int stepId, BatchStepActor actor, Guid? requestId, CancellationToken cancellationToken)
    {
        if (requestId is { } duplicate && await db.Set<BoyahaneStepRequest>().AnyAsync(r => r.RequestId == duplicate, cancellationToken))
        {
            return await GetAsync(batchId, cancellationToken);
        }

        var batch = await db.Set<BoyahaneBatch>().Include(b => b.Steps).ThenInclude(s => s.Operation).SingleOrDefaultAsync(b => b.Id == batchId, cancellationToken);
        if (batch is null)
        {
            return NotFound;
        }

        var step = batch.Steps.SingleOrDefault(s => s.Id == stepId);
        if (step is null)
        {
            return NotFound;
        }

        if (batch.Status is BatchStatus.Completed or BatchStatus.Cancelled)
        {
            return new Error(StepState, "Parti kapanmış.", ErrorType.Conflict);
        }

        var current = batch.Steps.Where(s => s.Status is BatchStepStatus.InProgress or BatchStepStatus.Pending).OrderBy(s => s.Sequence).FirstOrDefault();
        if (current is null || current.Id != step.Id || step.Status != BatchStepStatus.Pending)
        {
            return new Error(StepState, current is null ? "Partide bekleyen adım yok." : $"Parti şu an {current.Operation.Name} adımında.", ErrorType.Conflict);
        }

        if (await StepAuthorityErrorAsync(step, actor, cancellationToken) is { } authorityError)
        {
            return authorityError;
        }

        int? machineId = null;
        if (step.Operation.NeedsMachine)
        {
            // Ofisten elle bildirimde makine seçilmediyse partinin makinesi kullanılır.
            machineId = actor.TabletId is null ? actor.MachineId ?? batch.MachineId : actor.MachineId;
            if (machineId is null)
            {
                return new Error("STEP_NEEDS_MACHINE", $"{step.Operation.Name} makinede yapılır; bu tablet bir makineye bağlı değil.", ErrorType.Conflict);
            }

            // Boyama ve söküm partinin planlandığı makinede yapılır; kapasite o makineye göre denetlendi.
            if ((step.Operation.Behavior is OperationBehavior.Dyeing or OperationBehavior.Stripping) && machineId != batch.MachineId)
            {
                var planned = await db.Set<BoyahaneMachine>().Where(m => m.Id == batch.MachineId).Select(m => m.Code).SingleAsync(cancellationToken);
                return new Error("STEP_WRONG_MACHINE", $"Bu parti {planned} makinesinde boyanacak.", ErrorType.Conflict);
            }

            var machine = await db.Set<BoyahaneMachine>().AsNoTracking().SingleAsync(m => m.Id == machineId, cancellationToken);
            if (!await db.Set<BoyahaneMachineOperation>().AnyAsync(o => o.MachineId == machine.Id && o.OperationId == step.OperationId, cancellationToken))
            {
                return new Error("STEP_MACHINE_NOT_ALLOWED", $"{machine.Code} makinesinde {step.Operation.Name} yapılmaz.", ErrorType.Conflict);
            }

            if (machine.Status != DyeMachineStatus.Available)
            {
                return new Error("MACHINE_NOT_AVAILABLE", machine.Status == DyeMachineStatus.Breakdown ? $"{machine.Code} makinesi arızalı." : $"{machine.Code} makinesi bakımda.", ErrorType.Conflict);
            }

            var busy = await db.Set<BoyahaneBatchStep>()
                .Where(s => s.MachineId == machineId && s.Status == BatchStepStatus.InProgress)
                .Select(s => s.Batch.BatchNo)
                .FirstOrDefaultAsync(cancellationToken);
            if (busy is not null)
            {
                return new Error("MACHINE_BUSY", $"Makinede {busy} partisi var; önce onun adımını bitirin.", ErrorType.Conflict);
            }
        }

        // Tabletten başlatılan adımda operatörün makine ve operasyon yetkinliği aranır; ofisten bildirimde aranmaz.
        if (actor.TabletId is not null && actor.OperatorId is { } operatorId
            && await BoyahaneOperatorCardService.CheckQualificationAsync(db, operatorId, machineId, step.OperationId, cancellationToken) is { } qualificationError)
        {
            return qualificationError;
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        step.Status = BatchStepStatus.InProgress;
        step.MachineId = machineId;
        step.TabletId = actor.TabletId;
        step.StartedByOperatorId = actor.OperatorId;
        step.StartedAt = now;
        batch.Status = BatchStatus.OnMachine;
        batch.StartedAt ??= now;
        Remember(requestId, step.Id, now);
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(batchId, cancellationToken);
    }

    public async Task<Result<BoyahaneBatchDto>> CompleteStepAsync(int batchId, int stepId, CompleteBatchStepRequest request, BatchStepActor actor, CancellationToken cancellationToken)
    {
        if (request.RequestId is { } duplicate && await db.Set<BoyahaneStepRequest>().AnyAsync(r => r.RequestId == duplicate, cancellationToken))
        {
            return await GetAsync(batchId, cancellationToken);
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var batch = await db.Set<BoyahaneBatch>()
            .Include(b => b.Steps).ThenInclude(s => s.Operation)
            .Include(b => b.Items)
            .Include(b => b.RecipeTrial).ThenInclude(t => t!.Items).ThenInclude(i => i.DyeChemical)
            .Include(b => b.Steps).ThenInclude(s => s.StrippingRecipe!).ThenInclude(r => r.Lines)
            .SingleOrDefaultAsync(b => b.Id == batchId, cancellationToken);
        var step = batch?.Steps.SingleOrDefault(s => s.Id == stepId);
        if (batch is null || step is null)
        {
            return NotFound;
        }

        if (step.Status != BatchStepStatus.InProgress)
        {
            return new Error(StepState, "Bu adım işlemde değil.", ErrorType.Conflict);
        }

        if (actor.TabletId is not null && step.MachineId is not null && actor.MachineId != step.MachineId)
        {
            return new Error("STEP_WRONG_MACHINE", "Bu adım başka bir makinede işlemde.", ErrorType.Conflict);
        }

        // Arıza kapanmadan tablette adım bitirilmez; ofisten elle bildirim geçmişe dönük düzeltme için açık kalır.
        if (actor.TabletId is not null && step.MachineId is { } busyMachine
            && await db.Set<BoyahaneMachine>().AnyAsync(m => m.Id == busyMachine && m.Status != DyeMachineStatus.Available, cancellationToken))
        {
            return new Error("MACHINE_NOT_AVAILABLE", "Makine arızalı ya da bakımda; önce arızayı kapatın.", ErrorType.Conflict);
        }

        if (await StepAuthorityErrorAsync(step, actor, cancellationToken) is { } authorityError)
        {
            return authorityError;
        }

        var error = step.Operation.Behavior switch
        {
            OperationBehavior.Dyeing => await RecordConsumptionAsync(batch, step, request, DyeingPlan(batch, step), cancellationToken),
            OperationBehavior.Stripping => await RecordConsumptionAsync(
                batch, step, request, StrippingPlan(step.StrippingRecipe, batch.Items.Sum(i => i.Kilograms), batch.LiquorRatio ?? 0), cancellationToken),
            OperationBehavior.Printing => await RecordConsumptionAsync(
                batch, step, request, PrintPlan(await PrintColorsAsync(await FirstColorwayIdAsync(batchId, cancellationToken), cancellationToken), batch.Items.Sum(i => i.Meters ?? 0)), cancellationToken),
            OperationBehavior.ColorCheck => await RecordColorResultAsync(batch, step, request, cancellationToken),
            OperationBehavior.FinalQuality => RecordItemResults(batch, step, request, actor.TabletId is null && await permissions.HasAsync(BoyahanePermissions.Batch.FinalOverride, cancellationToken)),
            _ => null,
        };
        if (error is not null)
        {
            return error;
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        step.Status = BatchStepStatus.Completed;
        step.CompletedAt = now;
        step.CompletedByOperatorId = actor.OperatorId;
        step.Note = NullIfBlank(request.Note);
        Remember(request.RequestId, step.Id, now);

        if (!batch.Steps.Any(s => s.Status == BatchStepStatus.Pending))
        {
            // Son adım: parti kapanır, RFID kart başka partiye verilebilir.
            batch.Status = BatchStatus.Completed;
            batch.CompletedAt = now;
            batch.CardUid = null;
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetAsync(batchId, cancellationToken);
    }

    /// <summary>Parti başlamadan gereksiz adımı kaldırır.</summary>
    public async Task<Result<BoyahaneBatchDto>> SkipStepAsync(int batchId, int stepId, CancellationToken cancellationToken)
    {
        var batch = await db.Set<BoyahaneBatch>().Include(b => b.Steps).ThenInclude(s => s.Operation).SingleOrDefaultAsync(b => b.Id == batchId, cancellationToken);
        var step = batch?.Steps.SingleOrDefault(s => s.Id == stepId);
        if (batch is null || step is null)
        {
            return NotFound;
        }

        if (batch.Status != BatchStatus.Planned)
        {
            return new Error(StepState, "Adım yalnızca parti başlamadan kaldırılabilir.", ErrorType.Conflict);
        }

        if (step.Operation.Behavior == OperationBehavior.Dyeing)
        {
            return new Error(StepState, "Boyama adımı kaldırılamaz.", ErrorType.Conflict);
        }

        if (step.Operation.Behavior == OperationBehavior.FinalQuality)
        {
            return new Error(StepState, "Final kalite adımı kaldırılamaz; sevk ve yeniden işlem bu karara bağlıdır.", ErrorType.Conflict);
        }

        step.Status = step.Status == BatchStepStatus.Skipped ? BatchStepStatus.Pending : BatchStepStatus.Skipped;
        if (!batch.Steps.Any(s => s.Status == BatchStepStatus.Pending))
        {
            step.Status = BatchStepStatus.Pending;
            return new Error(StepState, "Partide en az bir adım kalmalıdır.", ErrorType.Conflict);
        }

        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(batchId, cancellationToken);
    }

    /// <summary>Final kalite kararı: tablette vardiya amiri kartı, ofiste ayrı yetki. Diğer adımlar ofiste parti yönetimi yetkisi ister.</summary>
    private async Task<Error?> StepAuthorityErrorAsync(BoyahaneBatchStep step, BatchStepActor actor, CancellationToken cancellationToken)
    {
        var isFinal = step.Operation.Behavior == OperationBehavior.FinalQuality;
        if (actor.TabletId is not null)
        {
            return !isFinal || await db.Set<BoyahaneOperator>().AnyAsync(o => o.Id == actor.OperatorId && o.Role == BoyahaneOperatorRole.Supervisor, cancellationToken)
                ? null
                : new Error("NOT_AUTHORIZED_FOR_ACTION", "Final kalite kararını yalnızca vardiya amiri verebilir.", ErrorType.Forbidden);
        }

        return await permissions.HasAsync(isFinal ? BoyahanePermissions.Batch.FinalQuality : BoyahanePermissions.Batch.Manage, cancellationToken)
            ? null
            : new Error("NOT_AUTHORIZED_FOR_ACTION", isFinal ? "Final kalite kararı için yetkiniz yok." : "Bu adımı bildirmek için yetkiniz yok.", ErrorType.Forbidden);
    }

    private void Remember(Guid? requestId, int stepId, DateTime now)
    {
        if (requestId is { } id)
        {
            db.Set<BoyahaneStepRequest>().Add(new BoyahaneStepRequest { RequestId = id, StepId = stepId, ReceivedAt = now });
        }
    }

    /// <summary>Boyama planı: onaylı reçete parti kilosu ve flottesine göre. İlavede reçete hesaplanmaz.</summary>
    private static Dictionary<int, decimal> DyeingPlan(BoyahaneBatch batch, BoyahaneBatchStep step)
    {
        if (step.ReworkKind == ColorCheckResult.Addition || batch.RecipeTrial is null)
        {
            return [];
        }

        var total = batch.Items.Sum(i => i.Kilograms);
        var ratio = batch.LiquorRatio ?? 0;
        return batch.RecipeTrial.Items.ToDictionary(i => i.DyeChemicalId, i => RecipeScaling.RequiredKilograms(i.DyeChemical.Kind, i.Amount, total, ratio));
    }

    /// <summary>Baskı satırının renk yolu (partinin ilk malından; partideki mal aynı desen ve renk yolundadır).</summary>
    private Task<int?> FirstColorwayIdAsync(int batchId, CancellationToken cancellationToken) =>
        db.Set<BoyahaneBatchItem>().Where(i => i.BatchId == batchId).OrderBy(i => i.Id).Select(i => i.OrderLine.ColorwayId).FirstOrDefaultAsync(cancellationToken);

    /// <summary>
    /// Boyama, söküm ve baskı tüketimi: hesaplanan plan (reçete, söküm reçetesi ya da pasta reçetesi) hazır gelir, gönderilen
    /// gerçek miktar yazılır. Planda olmayan satır planı sıfır olarak eklenir. İlavede reçete hesaplanmaz, operatörün girdiği
    /// satırlar yazılır. Gerçek miktar lotlardan düşer.
    /// </summary>
    private async Task<Error?> RecordConsumptionAsync(
        BoyahaneBatch batch, BoyahaneBatchStep step, CompleteBatchStepRequest request, Dictionary<int, decimal> planned, CancellationToken cancellationToken)
    {
        var sent = (request.Consumptions ?? []).GroupBy(c => c.DyeChemicalId).ToDictionary(g => g.Key, g => g.Last().ActualKilograms);

        if (step.ReworkKind == ColorCheckResult.Addition && sent.Count == 0)
        {
            return new Error(StepState, "İlavede kullanılan boya ya da kimyasalı girin.", ErrorType.Conflict);
        }

        var unknown = sent.Keys.Except(planned.Keys).ToList();
        var known = await db.Set<BoyahaneDyeChemical>().CountAsync(c => unknown.Contains(c.Id), cancellationToken);
        if (known != unknown.Count)
        {
            return new Error(StepState, "Seçilen boya ya da kimyasal bulunamadı.", ErrorType.Conflict);
        }

        foreach (var chemicalId in planned.Keys.Union(sent.Keys))
        {
            var plan = planned.GetValueOrDefault(chemicalId);
            var consumption = new BoyahaneBatchConsumption
            {
                DyeChemicalId = chemicalId,
                PlannedKilograms = plan,
                ActualKilograms = sent.TryGetValue(chemicalId, out var actual) ? actual : plan,
            };
            step.Consumptions.Add(consumption);

            // Stok yetmezse kalan lotsuz kalır; boyama gerçekleştiği için adım engellenmez.
            if (!await chemicalStock.AllocateAsync(consumption, chemicalId, consumption.ActualKilograms, batch.BatchNo, cancellationToken))
            {
                return new Error(ErrorCodes.ResourceBusy, "Stok şu an başka bir işlemle değişiyor. Lütfen tekrar deneyin.", ErrorType.Conflict);
            }
        }

        return null;
    }

    /// <summary>
    /// Renk kontrolü bütün parti içindir. Uygun değilse araya boyama ve yeni bir renk kontrolü girer. Söküm + yeniden boyada
    /// boyamadan önce seçilen söküm reçetesiyle bir söküm adımı girer; ardından reçete baştan uygulanır.
    /// </summary>
    private async Task<Error?> RecordColorResultAsync(BoyahaneBatch batch, BoyahaneBatchStep step, CompleteBatchStepRequest request, CancellationToken cancellationToken)
    {
        if (request.ColorResult is not { } result)
        {
            return new Error(StepState, "Renk kontrolü sonucunu seçin.", ErrorType.Conflict);
        }

        if (result != ColorCheckResult.Stripping && request.StrippingRecipeId is not null)
        {
            return new Error(StepState, "Söküm reçetesi yalnızca söküm + yeniden boya sonucunda seçilir.", ErrorType.Conflict);
        }

        int? strippingOperationId = null;
        if (result == ColorCheckResult.Stripping)
        {
            if (request.StrippingRecipeId is not { } recipeId)
            {
                return new Error(StepState, "Söküm reçetesini seçin.", ErrorType.Conflict);
            }

            if (!await db.Set<BoyahaneStrippingRecipe>().AnyAsync(r => r.Id == recipeId && r.IsActive, cancellationToken))
            {
                return new Error(StepState, "Söküm reçetesi bulunamadı ya da pasif.", ErrorType.Conflict);
            }

            var operation = await db.Set<BoyahaneOperation>().AsNoTracking()
                .Where(o => o.IsActive && o.Behavior == OperationBehavior.Stripping)
                .OrderBy(o => o.Code)
                .Select(o => new { o.Id, o.Name, o.NeedsMachine })
                .FirstOrDefaultAsync(cancellationToken);
            if (operation is null)
            {
                return new Error(StepState, "Söküm operasyonu tanımlı değil; Operasyonlar ekranında davranışı Söküm olan bir operasyon tanımlayın.", ErrorType.Conflict);
            }

            // Söküm boyamayla aynı makinede yapılır; makine bu operasyonu yapamıyorsa adım hiç başlatılamaz.
            if (operation.NeedsMachine
                && !await db.Set<BoyahaneMachineOperation>().AnyAsync(o => o.MachineId == batch.MachineId && o.OperationId == operation.Id, cancellationToken))
            {
                var machine = await db.Set<BoyahaneMachine>().Where(m => m.Id == batch.MachineId).Select(m => m.Code).SingleAsync(cancellationToken);
                return new Error(StepState, $"{machine} makinesinde {operation.Name} yapılmaz; makine kartında operasyonu işaretleyin.", ErrorType.Conflict);
            }

            strippingOperationId = operation.Id;
        }

        step.ColorResult = result;
        if (result == ColorCheckResult.Approved)
        {
            return null;
        }

        var inserted = strippingOperationId is null ? 2 : 3;
        foreach (var later in batch.Steps.Where(s => s.Sequence > step.Sequence))
        {
            later.Sequence += inserted;
        }

        // Geri dönüş aynı boyama operasyonuyla yapılır.
        var dyeing = batch.Steps.Where(s => s.Operation.Behavior == OperationBehavior.Dyeing).OrderBy(s => s.Sequence).First();
        var sequence = step.Sequence;
        if (strippingOperationId is { } strippingId)
        {
            batch.Steps.Add(new BoyahaneBatchStep { Sequence = ++sequence, OperationId = strippingId, ReworkKind = result, StrippingRecipeId = request.StrippingRecipeId });
        }

        batch.Steps.Add(new BoyahaneBatchStep { Sequence = ++sequence, OperationId = dyeing.OperationId, ReworkKind = result });
        batch.Steps.Add(new BoyahaneBatchStep { Sequence = ++sequence, OperationId = step.OperationId });
        return null;
    }

    /// <summary>
    /// Final kalite her top ya da lot için ayrı sonuçlanır. Yeniden işleme ayrılan mal boşa çıkar ve yeni partiye girebilir.
    /// Uygun mal kalmadıysa paketleme atlanır.
    /// </summary>
    private Error? RecordItemResults(BoyahaneBatch batch, BoyahaneBatchStep step, CompleteBatchStepRequest request, bool canOverride)
    {
        var results = (request.ItemResults ?? []).GroupBy(r => r.ItemId).ToDictionary(g => g.Key, g => g.Last());
        var items = batch.Items.Where(i => i.IsActive).ToList();
        if (items.Any(i => !results.ContainsKey(i.Id)))
        {
            return new Error(StepState, "Her top ya da lot için sonuç girin.", ErrorType.Conflict);
        }

        // Fire ve fatura ölçüye dayandığı için uygun malın ölçüsü zorunludur.
        var unmeasured = items.Any(i => results[i.Id].Result == FinalQualityResult.Approved
            && (results[i.Id].Kilograms is null || (i.Meters is not null && results[i.Id].Meters is null)));
        if (unmeasured)
        {
            return new Error(StepState, "Uygun mal için çıkış ölçüsünü girin.", ErrorType.Conflict);
        }

        // Çıkış ölçüsü faturaya gider: sınırı aşan ölçü yalnızca yetkili kullanıcıyla ve gerekçeyle geçer.
        bool Exceeds(BoyahaneBatchItem i) => results[i.Id].Result == FinalQualityResult.Approved
            && (Billing.ExceedsFinalLimit(i.Kilograms, results[i.Id].Kilograms) || Billing.ExceedsFinalLimit(i.Meters, results[i.Id].Meters));
        if (items.Any(i => Exceeds(i) && (!canOverride || NullIfBlank(results[i.Id].OverrideNote) is null)))
        {
            return new Error("FINAL_MEASURE_OVER_LIMIT", canOverride
                ? $"Çıkış ölçüsü girdinin %{Billing.MaxFinalExcessPercent:0} üzerinde; gerekçe girin."
                : $"Çıkış ölçüsü girdinin %{Billing.MaxFinalExcessPercent:0} üzerinde olamaz; yetkili kullanıcı ofisten gerekçeyle girebilir.", ErrorType.Conflict);
        }

        foreach (var item in items)
        {
            var result = results[item.Id];
            var exceeds = Exceeds(item);
            item.FinalOverrideBy = exceeds ? currentUser.UserName : null;
            item.FinalOverrideNote = exceeds ? NullIfBlank(result.OverrideNote) : null;
            item.FinalKilograms = result.Kilograms;
            item.FinalMeters = item.Meters is null ? null : result.Meters;
            item.FinalResult = result.Result;
            item.FinalNote = NullIfBlank(result.Note);
            item.IsActive = result.Result != FinalQualityResult.Rework;
        }

        if (items.All(i => i.FinalResult != FinalQualityResult.Approved))
        {
            foreach (var later in batch.Steps.Where(s => s.Sequence > step.Sequence && s.Status == BatchStepStatus.Pending))
            {
                later.Status = BatchStepStatus.Skipped;
            }
        }

        return null;
    }
}
