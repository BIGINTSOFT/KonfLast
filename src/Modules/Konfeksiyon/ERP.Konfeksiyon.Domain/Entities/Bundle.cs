using ERP.Core.Domain.Common;
using ERP.Konfeksiyon.Domain.Enums;
using ERP.Konfeksiyon.Domain.Routing;

namespace ERP.Konfeksiyon.Domain.Entities;

/// <summary>
/// Demet: üretimin fiziksel takip birimi. Bir siparişin tek renk ve bedeninden belirli adette parça
/// taşır ve modelin rotasındaki operasyonlardan sırayla (ya da paralel) geçer.
/// </summary>
public class Bundle : AuditableEntity
{
    /// <summary>Sistem tarafından üretilir, örn. 000154; değiştirilemez ve yeniden kullanılmaz.</summary>
    public string BundleNo { get; set; } = null!;

    public int ProductionOrderId { get; set; }
    public ProductionOrder ProductionOrder { get; set; } = null!;

    public int ColorId { get; set; }
    public Color Color { get; set; } = null!;

    public int SizeId { get; set; }
    public Size Size { get; set; } = null!;

    /// <summary>Demetteki parça adedi.</summary>
    public int Quantity { get; set; }

    /// <summary>Demet oluşturulurken geçerli olan rota; demetin operasyonları bundan üretilmiştir.</summary>
    public int ProductionRouteId { get; set; }
    public ProductionRoute ProductionRoute { get; set; } = null!;

    public BundleStatus Status { get; private set; } = BundleStatus.Created;

    /// <summary>Demet bir kesim planının kesiminden açıldıysa o plan; elle / toplu demetlemede boştur.</summary>
    public int? CuttingPlanId { get; set; }
    public CuttingPlan? CuttingPlan { get; set; }

    /// <summary>
    /// Operasyonların sırası; demet açılırken siparişten kopyalanır, demetin ömrü boyunca değişmez.
    /// Akış (<see cref="OperationFlow"/>) bu moda göre kurulur.
    /// </summary>
    public OperationSequenceMode SequenceMode { get; set; } = OperationSequenceMode.Sequential;

    /// <summary>Aynı demetin iki cihaz / kullanıcı tarafından eşzamanlı değiştirilmesini engeller.</summary>
    public byte[] RowVersion { get; set; } = null!;

    public ICollection<BundleOperation> Operations { get; } = new List<BundleOperation>();

    /// <summary>
    /// Demet fason gönderim fişiyle fasoncuya gönderildi ve henüz tamamı dönmedi. Bu sürede sahada (tablet ya da
    /// ofisten elle bildirim) hiçbir operasyonu başlatılamaz; gönderilen operasyonun adedi dönüş fişiyle işlenir.
    /// </summary>
    public bool AtSubcontractor { get; private set; }

    /// <summary>Üretime girmemiş demet iptal edilebilir; fasoncudaki demet önce dönmelidir.</summary>
    public bool CanCancel => (Status is BundleStatus.Created or BundleStatus.Waiting) && !AtSubcontractor;

    /// <summary>Tamamlanmış, kapanmış ya da iptal edilmiş demete artık kart atanmaz / okutulmaz.</summary>
    public bool IsOpen => Status is not (BundleStatus.Completed or BundleStatus.Closed or BundleStatus.Cancelled);

    /// <summary>Kart atanan demet üretime hazırdır.</summary>
    public void OnCardAssigned()
    {
        if (Status == BundleStatus.Created)
        {
            Status = BundleStatus.Waiting;
        }
    }

    /// <summary>Henüz üretime girmemiş demetin kartı bırakılırsa demet yeniden kartsız hale döner.</summary>
    public void OnCardReleased()
    {
        if (Status == BundleStatus.Waiting)
        {
            Status = BundleStatus.Created;
        }
    }

    public void Cancel()
    {
        if (!CanCancel)
        {
            throw new InvalidOperationException($"{Status} durumundaki demet iptal edilemez.");
        }

        Status = BundleStatus.Cancelled;
        foreach (var operation in Operations)
        {
            operation.Cancel();
        }
    }

    /// <summary>Yalnızca tüm operasyonları bitmiş demet depoya teslim edilip kapatılabilir.</summary>
    public bool CanClose => Status == BundleStatus.Completed;

    /// <summary>
    /// Demetten çıkan sağlam adet: rotanın son operasyon(lar)ından geçen en küçük adet. Hepsi
    /// reddedilmiş demette sıfırdır.
    /// </summary>
    public int FinishedQuantity(OperationFlow flow)
    {
        var final = flow.FinalSteps(Operations.Select(o => o.RouteOperationId));
        var last = Operations.Where(o => final.Contains(o.RouteOperationId)).ToList();
        return last.Count == 0 ? 0 : last.Min(o => o.ProducedQuantity);
    }

    /// <summary>
    /// Demeti kapatır: parçalar depoya teslim edilmiştir, demet üzerinde artık üretim ya da kalite işlemi
    /// (ret, tamir) yapılamaz.
    /// </summary>
    public void Close()
    {
        if (!CanClose)
        {
            throw new InvalidOperationException($"{Status} durumundaki demet kapatılamaz.");
        }

        Status = BundleStatus.Closed;
    }

    /// <summary>Operasyonun şu an başlatılıp başlatılamayacağı; başlatılamıyorsa nedeni.</summary>
    /// <param name="requireCard">
    /// Tabletten başlatmada demetin kartı olmalıdır; ofisten elle bildirimde (kartsız) kart aranmaz.
    /// </param>
    public OperationStartBlock CheckStart(BundleOperation operation, bool requireCard = true)
    {
        EnsureOwns(operation);

        // Fasoncudaki demet fiziksel olarak fabrikada değildir.
        if (AtSubcontractor)
        {
            return OperationStartBlock.BundleAtSubcontractor;
        }

        switch (Status)
        {
            case BundleStatus.Created when requireCard:
                return OperationStartBlock.BundleNotReleased;
            case BundleStatus.Completed or BundleStatus.Closed or BundleStatus.Cancelled:
                return OperationStartBlock.BundleClosed;
            // Tamirdeki demet çalışmaya devam eder (tamir, operasyonun yeniden işlenmesidir); bekletmedeki durur.
            case BundleStatus.QualityHold:
                return OperationStartBlock.BundleOnQualityHold;
        }

        if (operation.Status == BundleOperationStatus.InProgress)
        {
            return OperationStartBlock.AlreadyInProgress;
        }

        // Demet fiziksel olarak tek yerdedir: paralel adımlar istenen sırayla ama birer birer işlenir.
        if (Operations.Any(o => o.Status == BundleOperationStatus.InProgress))
        {
            return OperationStartBlock.AnotherOperationInProgress;
        }

        return operation.Status switch
        {
            // Başlanıp yarım bırakılan operasyon tamamlanmadan demet başka operasyona geçemez (sıralı ve sırasız).
            BundleOperationStatus.Ready or BundleOperationStatus.Paused when UnfinishedOperations().Any(o => o != operation) =>
                OperationStartBlock.AnotherOperationUnfinished,
            BundleOperationStatus.Ready or BundleOperationStatus.Paused => OperationStartBlock.None,
            BundleOperationStatus.Waiting => OperationStartBlock.PredecessorsIncomplete,
            BundleOperationStatus.Completed => OperationStartBlock.OperationCompleted,
            BundleOperationStatus.QualityHold => OperationStartBlock.OperationOnQualityHold,
            _ => OperationStartBlock.OperationCancelled,
        };
    }

    /// <summary>
    /// Başlanıp yarım bırakılmış operasyonlar: kısmi adet bildirilmiş ya da süren işi bırakılmış, kalanı var.
    /// Kalite tamiri için geri açılan adet sayılmaz; tamir sürerken demet diğer operasyonlarda ilerleyebilir.
    /// </summary>
    public IEnumerable<BundleOperation> UnfinishedOperations() =>
        Operations.Where(o => o.Status == BundleOperationStatus.Paused && o.RemainingQuantity - o.ReworkQuantity > 0);

    /// <summary>Operasyonun henüz bitmemiş öncülleri (başlatılamıyorsa nedenini söylemek için).</summary>
    public IReadOnlyList<BundleOperation> MissingPredecessors(BundleOperation operation, OperationFlow flow)
    {
        var required = flow.PredecessorsOf(operation.RouteOperationId);
        return Operations
            .Where(o => required.Contains(o.RouteOperationId) && !o.IsFinished)
            .OrderBy(o => o.SequenceNo)
            .ToList();
    }

    /// <summary>
    /// Operasyonu başlatır ya da kısmi üretimden sonra kalan adet için sürdürür.
    /// </summary>
    /// <returns>Kısmi üretimin devamıysa true, ilk başlatmaysa false.</returns>
    public bool StartOperation(
        BundleOperation operation, int machineId, int operatorId, DateTime utcNow, bool requireCard = true)
    {
        var block = CheckStart(operation, requireCard);
        if (block != OperationStartBlock.None)
        {
            throw new InvalidOperationException($"Operasyon başlatılamaz: {block}.");
        }

        var resumed = operation.Status == BundleOperationStatus.Paused;
        operation.Status = BundleOperationStatus.InProgress;
        operation.MachineId = machineId;
        operation.OperatorId = operatorId;
        operation.StartedAt ??= utcNow;
        Status = BundleStatus.InProduction;
        return resumed;
    }

    /// <summary>
    /// İşlemdeki operasyon için üretilen ve hatalı adetleri işler. Kalan adet biterse operasyon tamamlanır
    /// ve öncülleri biten ardıl operasyonlar başlatılabilir hale gelir; kalırsa operasyon bekletilir.
    /// </summary>
    /// <returns>Bu bildirimle başlatılabilir hale gelen operasyonlar.</returns>
    public IReadOnlyList<BundleOperation> ReportOperation(
        BundleOperation operation, int producedQuantity, int rejectedQuantity, OperationFlow flow, DateTime utcNow)
    {
        EnsureOwns(operation);

        if (operation.Status != BundleOperationStatus.InProgress)
        {
            throw new InvalidOperationException("Yalnızca işlemdeki operasyon için adet bildirilebilir.");
        }

        if (producedQuantity < 0 || rejectedQuantity < 0 || producedQuantity + rejectedQuantity > operation.RemainingQuantity)
        {
            throw new InvalidOperationException("Bildirilen adet operasyonun kalan adedini aşamaz.");
        }

        operation.ProducedQuantity += producedQuantity;
        operation.RejectedQuantity += rejectedQuantity;
        // Yeniden işlenen parçalar önce bekleyen tamiri kapatır.
        operation.ReworkQuantity = Math.Max(0, operation.ReworkQuantity - producedQuantity - rejectedQuantity);

        if (operation.RemainingQuantity == 0)
        {
            operation.Status = BundleOperationStatus.Completed;
            operation.CompletedAt = utcNow;
        }
        else
        {
            operation.Status = BundleOperationStatus.Paused;
        }

        Rebalance(flow, utcNow);
        var released = ReleaseSuccessors(flow);
        RefreshStatus();
        return released;
    }

    /// <summary>
    /// Operasyonun fasoncuya gönderilip gönderilemeyeceği: sahada başlatılabilecek durumda olmalıdır (kart aranmaz).
    /// </summary>
    public OperationStartBlock CheckSubcontractSend(BundleOperation operation) => CheckStart(operation, requireCard: false);

    /// <summary>
    /// Demeti, <paramref name="operation"/> operasyonu yapılmak üzere fasoncuya gönderir. Operasyonun durumu değişmez
    /// (hazır / bekletilmiş kalır); demet dönene kadar sahada işlenemez.
    /// </summary>
    public void SendToSubcontractor(BundleOperation operation)
    {
        var block = CheckSubcontractSend(operation);
        if (block != OperationStartBlock.None)
        {
            throw new InvalidOperationException($"Demet fasona gönderilemez: {block}.");
        }

        AtSubcontractor = true;
    }

    /// <summary>
    /// Fasoncudan dönen adetleri operasyona işler: sağlam adet üretilmiş, hatalı ve eksik adet hatalı sayılır. Etkisi
    /// sahadaki bildirimle (<see cref="ReportOperation"/>) aynıdır: kalan biterse operasyon tamamlanır, ardılları açılır;
    /// kalırsa operasyon bekletilir. Makine ve operatör yazılmaz (fason işçiliği operatör kazancına girmez).
    /// </summary>
    /// <returns>Bu bildirimle başlatılabilir hale gelen operasyonlar.</returns>
    public IReadOnlyList<BundleOperation> ReportSubcontracted(
        BundleOperation operation, int producedQuantity, int rejectedQuantity, OperationFlow flow, DateTime utcNow)
    {
        EnsureOwns(operation);

        if (!AtSubcontractor)
        {
            throw new InvalidOperationException("Demet fasoncuda değil.");
        }

        if (operation.Status is not (BundleOperationStatus.Ready or BundleOperationStatus.Paused))
        {
            throw new InvalidOperationException($"{operation.Status} durumundaki operasyona fason dönüşü işlenemez.");
        }

        if (producedQuantity + rejectedQuantity == 0)
        {
            return [];
        }

        // Sahadaki bildirimle aynı yol: operasyon an için işlemde sayılır, bildirim onu tamamlar ya da bekletir.
        operation.Status = BundleOperationStatus.InProgress;
        operation.StartedAt ??= utcNow;
        return ReportOperation(operation, producedQuantity, rejectedQuantity, flow, utcNow);
    }

    /// <summary>Gönderilen parçaların tamamı döndü (ya da gönderim iptal edildi): demet sahaya döner.</summary>
    public void ReturnFromSubcontractor() => AtSubcontractor = false;

    /// <summary>Demet kalite bekletmesine alınabilir mi (sahada ya da tamamlanmış olmalı).</summary>
    public bool CanPlaceOnQualityHold => Status is BundleStatus.Waiting or BundleStatus.InProduction
        or BundleStatus.PartiallyCompleted or BundleStatus.Completed or BundleStatus.Rework;

    /// <summary>Demeti kalite bekletmesine alır: serbest bırakılana kadar hiçbir operasyonu başlatılamaz.</summary>
    public void PlaceOnQualityHold()
    {
        if (!CanPlaceOnQualityHold)
        {
            throw new InvalidOperationException($"{Status} durumundaki demet kalite bekletmesine alınamaz.");
        }

        Status = BundleStatus.QualityHold;
    }

    /// <summary>Kalite bekletmesini kaldırır; demet operasyonlarının durumuna göre kaldığı yerden devam eder.</summary>
    public void ReleaseQualityHold()
    {
        if (Status != BundleStatus.QualityHold)
        {
            throw new InvalidOperationException("Demet kalite bekletmesinde değil.");
        }

        Status = FlowStatus();
    }

    /// <summary>Operasyonun ürettiği parçalardan kalite tarafından geri alınabilecek olanlar var mı?</summary>
    public QualityTakeBackBlock CheckTakeBack(BundleOperation operation, int quantity, OperationFlow flow)
    {
        EnsureOwns(operation);

        if (operation.Status == BundleOperationStatus.InProgress)
        {
            return QualityTakeBackBlock.OperationInProgress;
        }

        if (quantity > operation.ProducedQuantity)
        {
            return QualityTakeBackBlock.ExceedsProduced;
        }

        // Sonraki operasyonun işlediği (ya da işlemekte olduğu) parçalar buradan geri alınamaz.
        var remaining = operation.ProducedQuantity - quantity;
        var consumed = Operations
            .Where(o => flow.PredecessorsOf(o.RouteOperationId).Contains(operation.RouteOperationId))
            .Any(successor => successor.Status switch
            {
                BundleOperationStatus.InProgress => successor.PlannedQuantity > remaining,
                BundleOperationStatus.Cancelled or BundleOperationStatus.Waiting => false,
                _ => successor.ProducedQuantity + successor.RejectedQuantity + successor.ReworkQuantity > remaining,
            });

        return consumed ? QualityTakeBackBlock.ConsumedDownstream : QualityTakeBackBlock.None;
    }

    /// <summary>
    /// Kalite kontrolünün sonucunu operasyona işler: reddedilen parçalar "üretilen"den "hatalı"ya geçer,
    /// tamire gönderilenler operasyona yeniden işlenmek üzere geri döner. Sonraki operasyonların planı
    /// buna göre yeniden hesaplanır.
    /// </summary>
    public void ApplyInspection(
        BundleOperation operation, int rejectedQuantity, int reworkQuantity, OperationFlow flow, DateTime utcNow)
    {
        if (rejectedQuantity < 0 || reworkQuantity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(rejectedQuantity), "Adetler negatif olamaz.");
        }

        var quantity = rejectedQuantity + reworkQuantity;
        if (quantity == 0)
        {
            return;
        }

        var block = CheckTakeBack(operation, quantity, flow);
        if (block != QualityTakeBackBlock.None)
        {
            throw new InvalidOperationException($"Parçalar geri alınamaz: {block}.");
        }

        operation.ProducedQuantity -= quantity;
        operation.RejectedQuantity += rejectedQuantity;
        operation.ReworkQuantity += reworkQuantity;

        if (operation.RemainingQuantity > 0)
        {
            // Tamir için operasyon yeniden açılır; makinede "kalan adet" olarak sürdürülür.
            operation.Status = BundleOperationStatus.Paused;
            operation.CompletedAt = null;
        }

        Rebalance(flow, utcNow);
        ReleaseSuccessors(flow);
        RefreshStatus();
    }

    /// <summary>Kalite bekletmesi korunur; diğer durumlar operasyonlardan türetilir.</summary>
    private void RefreshStatus()
    {
        if (Status != BundleStatus.QualityHold)
        {
            Status = FlowStatus();
        }
    }

    /// <summary>Demetin akıştaki durumu, operasyonlarından türetilir.</summary>
    private BundleStatus FlowStatus()
    {
        if (Operations.Any(o => o.Status == BundleOperationStatus.InProgress))
        {
            return BundleStatus.InProduction;
        }

        if (Operations.Any(o => o.ReworkQuantity > 0))
        {
            return BundleStatus.Rework;
        }

        if (Operations.All(o => o.IsFinished))
        {
            return BundleStatus.Completed;
        }

        return Operations.All(o => o.ProducedQuantity + o.RejectedQuantity == 0)
            ? BundleStatus.Waiting
            : BundleStatus.PartiallyCompleted;
    }

    /// <summary>
    /// Öncüllerinin sağlam adedi değişen (kalite reddi, tamir, tamirin geri dönmesi) ve daha önce açılmış
    /// operasyonların planını ve durumunu yeniden hesaplar. Plan, öncüllerin ürettiği en küçük adettir ama
    /// operasyonun zaten işlediği adedin altına inmez.
    /// </summary>
    private void Rebalance(OperationFlow flow, DateTime utcNow)
    {
        var byRouteOperation = Operations.ToDictionary(o => o.RouteOperationId);

        bool changed;
        do
        {
            changed = false;
            foreach (var operation in Operations.OrderBy(o => o.SequenceNo))
            {
                var predecessorIds = flow.PredecessorsOf(operation.RouteOperationId);
                if ((predecessorIds.Count == 0 && !flow.Independent)
                    || operation.Status is BundleOperationStatus.Waiting or BundleOperationStatus.Cancelled
                        or BundleOperationStatus.QualityHold)
                {
                    continue;
                }

                // Sırasızda öncül yoktur: diğer operasyonlarda hatalı ayrılan parçalar demetten çıkmıştır.
                var predecessors = flow.Independent
                    ? Operations.Where(o => o != operation).ToList()
                    : predecessorIds.Select(id => byRouteOperation[id]).ToList();
                var available = flow.Independent
                    ? Math.Max(Quantity - predecessors.Sum(p => p.RejectedQuantity), 0)
                    : predecessors.Any(p => p.Status == BundleOperationStatus.Cancelled)
                        ? 0
                        : predecessors.Min(p => p.ProducedQuantity);
                var done = operation.ProducedQuantity + operation.RejectedQuantity;
                var planned = Math.Max(available, done);

                if (operation.Status == BundleOperationStatus.InProgress)
                {
                    if (planned > 0 && planned != operation.PlannedQuantity)
                    {
                        operation.PlannedQuantity = planned;
                        changed = true;
                    }

                    continue;
                }

                BundleOperationStatus status;
                if (planned == 0)
                {
                    // Hiç parça gelmiyor: öncüller bitmişse yapılacak iş yok; bitmemişse (tamir sürüyor) yeniden beklenir.
                    status = flow.Independent || predecessors.All(p => p.IsFinished)
                        ? BundleOperationStatus.Cancelled
                        : BundleOperationStatus.Waiting;
                    planned = operation.PlannedQuantity;
                }
                else if (planned == done)
                {
                    status = BundleOperationStatus.Completed;
                }
                else
                {
                    status = done == 0 && operation.ReworkQuantity == 0
                        ? BundleOperationStatus.Ready
                        : BundleOperationStatus.Paused;
                }

                if (status == operation.Status && planned == operation.PlannedQuantity)
                {
                    continue;
                }

                operation.PlannedQuantity = planned;
                operation.Status = status;
                operation.CompletedAt = status == BundleOperationStatus.Completed ? operation.CompletedAt ?? utcNow : null;
                changed = true;
            }
        }
        while (changed);
    }

    /// <summary>
    /// Tüm öncülleri biten bekleyen operasyonları açar. Sonraki operasyona yalnızca sağlam parçalar geçer:
    /// planlanan adet, öncüllerin ürettiği en küçük adettir. Hiç parça kalmadıysa operasyon iptal olur
    /// (yapılacak iş yok) ve bu durum onun ardıllarına da yayılır.
    /// </summary>
    private List<BundleOperation> ReleaseSuccessors(OperationFlow flow)
    {
        var released = new List<BundleOperation>();
        var byRouteOperation = Operations.ToDictionary(o => o.RouteOperationId);

        bool changed;
        do
        {
            changed = false;
            foreach (var candidate in Operations.Where(o => o.Status == BundleOperationStatus.Waiting).OrderBy(o => o.SequenceNo))
            {
                var predecessors = flow.PredecessorsOf(candidate.RouteOperationId).Select(id => byRouteOperation[id]).ToList();
                if (!predecessors.All(p => p.IsFinished))
                {
                    continue;
                }

                var available = predecessors.Any(p => p.Status == BundleOperationStatus.Cancelled)
                    ? 0
                    : predecessors.Min(p => p.ProducedQuantity);

                if (available == 0)
                {
                    candidate.Status = BundleOperationStatus.Cancelled;
                }
                else
                {
                    candidate.PlannedQuantity = available;
                    candidate.Status = BundleOperationStatus.Ready;
                    released.Add(candidate);
                }

                changed = true;
            }
        }
        while (changed);

        return released;
    }

    private void EnsureOwns(BundleOperation operation)
    {
        if (!Operations.Contains(operation))
        {
            throw new ArgumentException("Operasyon bu demete ait değil.", nameof(operation));
        }
    }

    /// <summary>
    /// Rotanın adımlarından demet operasyonlarını üretir: öncülü olmayan adımlar hemen başlatılabilir
    /// (<see cref="BundleOperationStatus.Ready"/>), diğerleri öncüllerini bekler. Öncüller demetin sıra moduna
    /// göredir: sıralıda yalnız ilk adım, sırasızda bütün adımlar hazırdır.
    /// </summary>
    /// <param name="unitRateOf">Adımın parça başı birim ücreti (demet açılırken sabitlenir); ücret tanımlı değilse null.</param>
    public void AddOperationsFrom(ProductionRoute route, Func<ProductionRouteOperation, decimal?>? unitRateOf = null)
    {
        var first = route.Operations.Min(o => o.SequenceNo);
        foreach (var step in route.Operations.OrderBy(o => o.SequenceNo))
        {
            var ready = SequenceMode switch
            {
                OperationSequenceMode.Free => true,
                OperationSequenceMode.Sequential => step.SequenceNo == first,
                _ => step.Predecessors.Count == 0,
            };
            Operations.Add(new BundleOperation
            {
                RouteOperationId = step.Id,
                OperationId = step.OperationId,
                SequenceNo = step.SequenceNo,
                Sam = step.Sam,
                UnitRate = unitRateOf?.Invoke(step),
                PlannedQuantity = Quantity,
                Status = ready ? BundleOperationStatus.Ready : BundleOperationStatus.Waiting,
            });
        }
    }
}

/// <summary>
/// Demetin rotadaki bir adımı. Adet alanları kısmi üretimi izler: planlanan = üretilen + hatalı + kalan.
/// Bu satırlar üretim hareketleriyle sık değişir; değişiklik geçmişi üretim event kayıtlarında tutulur.
/// </summary>
[NotAudited]
public class BundleOperation
{
    public long Id { get; set; }

    public int BundleId { get; set; }
    public Bundle Bundle { get; set; } = null!;

    /// <summary>Kaynak rota adımı; öncül bilgisi rotadan okunur (etkin rota kilitli olduğu için değişmez).</summary>
    public int RouteOperationId { get; set; }
    public ProductionRouteOperation RouteOperation { get; set; } = null!;

    public int OperationId { get; set; }
    public Operation Operation { get; set; } = null!;

    public int SequenceNo { get; set; }

    /// <summary>Demet oluşturulduğu andaki standart süre (dk/adet).</summary>
    public decimal Sam { get; set; }

    /// <summary>
    /// Demet oluşturulduğu andaki parça başı birim ücret (adet başına). Sonradan değişen ücretler geçmiş
    /// kazançları etkilemesin diye burada sabitlenir; ücret tanımlı değilken açılan demette boştur.
    /// </summary>
    public decimal? UnitRate { get; set; }

    public BundleOperationStatus Status { get; set; } = BundleOperationStatus.Waiting;

    public int PlannedQuantity { get; set; }
    public int ProducedQuantity { get; set; }
    public int RejectedQuantity { get; set; }

    /// <summary>Kalitenin tamire gönderdiği, henüz yeniden işlenmemiş adet (kalan adedin içindedir).</summary>
    public int ReworkQuantity { get; set; }

    public int RemainingQuantity => PlannedQuantity - ProducedQuantity - RejectedQuantity;

    /// <summary>Operasyonda yapılacak iş kalmadı: tamamlandı ya da iptal edildi.</summary>
    public bool IsFinished => Status is BundleOperationStatus.Completed or BundleOperationStatus.Cancelled;

    public int? MachineId { get; set; }
    public Machine? Machine { get; set; }

    public int? OperatorId { get; set; }
    public Operator? Operator { get; set; }

    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public void Cancel()
    {
        if (Status != BundleOperationStatus.Completed)
        {
            Status = BundleOperationStatus.Cancelled;
        }
    }
}
