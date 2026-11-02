using ERP.Konfeksiyon.Domain.Enums;
using FluentValidation;

namespace ERP.Konfeksiyon.Application.Features.Production;

/// <param name="RequestId">
/// İstemcinin bu işlem için ürettiği kimlik. Aynı kimlikle yeniden gönderilen istek (ağ tekrarı,
/// çevrimdışı kuyruk) ikinci kez işlenmez; mevcut durum <c>duplicate: true</c> ile döner.
/// </param>
/// <remarks>Operatör, tabletin makinesindeki açık çalışma oturumundan alınır.</remarks>
public sealed record StartOperationRequest(Guid RequestId);

/// <param name="RequestId">Bkz. <see cref="StartOperationRequest"/>.</param>
/// <param name="ProducedQuantity">Bu çalışmada üretilen sağlam adet.</param>
/// <param name="RejectedQuantity">Bu çalışmada hatalı çıkan adet.</param>
/// <param name="AuthorizerUid">
/// Hatalı adet bildirme yetkisi olmayan operatör için onaylayan yetkilinin (ör. bant şefi) okuttuğu kart; yoksa boş.
/// </param>
public sealed record ReportOperationRequest(
    Guid RequestId, int ProducedQuantity, int RejectedQuantity, string? AuthorizerUid = null);

/// <summary>Bir demet operasyonunun işlem sonrası durumu.</summary>
/// <param name="Duplicate">Bu yanıt daha önce işlenmiş bir isteğin tekrarı mı.</param>
/// <param name="NextOperations">Demette şu an başlatılabilecek operasyonlar.</param>
/// <param name="Sam">Bir adedin standart süresi (dakika).</param>
/// <param name="RunTargetSeconds">İşlemdeki operasyonda kalan adedin hedef süresi (SAM × kalan adet); işlemde değilse boş.</param>
/// <param name="RunElapsedSeconds">
/// İşlemdeki operasyonda son başlatmadan bu yana geçen net süre: mola, ara ve arıza süreleri düşülür; işlemde değilse boş.
/// </param>
/// <param name="EfficiencyPercent">
/// Yalnızca adet bildirimi yanıtında: bildirilen sağlam adedin standart süresinin, harcanan net süreye oranı (%).
/// </param>
public sealed record OperationProgressDto(
    bool Duplicate,
    long BundleOperationId,
    int BundleId,
    string BundleNo,
    BundleStatus BundleStatus,
    int SequenceNo,
    string OperationName,
    BundleOperationStatus Status,
    int PlannedQuantity,
    int ProducedQuantity,
    int RejectedQuantity,
    int RemainingQuantity,
    string? MachineCode,
    int? OperatorId,
    string? OperatorName,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    IReadOnlyList<string> NextOperations,
    decimal Sam = 0,
    int? RunTargetSeconds = null,
    int? RunElapsedSeconds = null,
    int? EfficiencyPercent = null);

/// <param name="EnteredBy">Olay ofisten elle girildiyse kaydı giren kullanıcı; tabletten gelen olaylarda boş.</param>
public sealed record BundleOperationEventDto(
    long Id,
    BundleOperationEventType EventType,
    int SequenceNo,
    string OperationName,
    string MachineCode,
    string OperatorName,
    int ProducedQuantity,
    int RejectedQuantity,
    DateTime OccurredAt,
    string? EnteredBy = null);

/// <summary>
/// Ofisten elle üretim bildirimi (tablet ve kart olmadan): askıda kalan ya da arızalı tablette başlatılamayan işin
/// adedi bildirilir. Kalan adet biterse operasyon tamamlanır; kalırsa bekletilir.
/// </summary>
/// <param name="RequestId">Bkz. <see cref="StartOperationRequest"/>; çift tıklama / ağ tekrarı ikinci kez işlenmez.</param>
/// <param name="OperatorId">İşi yapan operatör. İşlemdeki operasyonda işi başlatan operatör esas alınır, boş geçilebilir.</param>
/// <param name="MachineId">İşin yapıldığı makine. İşlemdeki operasyonda işin makinesi esas alınır, boş geçilebilir.</param>
public sealed record ManualReportRequest(
    Guid RequestId, int ProducedQuantity, int RejectedQuantity, int? OperatorId = null, int? MachineId = null);

/// <summary>Elle bildirim penceresi için operasyonun durumu ve seçilebilecek makineler.</summary>
/// <param name="InProgress">Operasyon bir makinede işlemde (askıda) mı; öyleyse makine ve operatör değiştirilemez.</param>
/// <param name="Machines">Operasyonu yapabilen makineler (ana ya da ek operasyonu olarak).</param>
public sealed record ManualReportOptionsDto(
    long BundleOperationId,
    int SequenceNo,
    string OperationName,
    BundleOperationStatus Status,
    int RemainingQuantity,
    bool InProgress,
    int? MachineId,
    string? MachineCode,
    int? OperatorId,
    string? OperatorName,
    IReadOnlyList<ManualReportMachineDto> Machines);

public sealed record ManualReportMachineDto(int Id, string MachineCode, string MachineName, MachineStatus Status);

public sealed class StartOperationRequestValidator : AbstractValidator<StartOperationRequest>
{
    public StartOperationRequestValidator()
    {
        RuleFor(x => x.RequestId).NotEmpty().WithMessage("İstek kimliği (requestId) zorunludur.");
    }
}

public sealed class ReportOperationRequestValidator : AbstractValidator<ReportOperationRequest>
{
    public ReportOperationRequestValidator()
    {
        RuleFor(x => x.RequestId).NotEmpty().WithMessage("İstek kimliği (requestId) zorunludur.");
        RuleFor(x => x.ProducedQuantity).GreaterThanOrEqualTo(0).WithMessage("Üretilen adet negatif olamaz.");
        RuleFor(x => x.RejectedQuantity).GreaterThanOrEqualTo(0).WithMessage("Hatalı adet negatif olamaz.");
    }
}

public sealed class ManualReportRequestValidator : AbstractValidator<ManualReportRequest>
{
    public ManualReportRequestValidator()
    {
        RuleFor(x => x.RequestId).NotEmpty().WithMessage("İstek kimliği (requestId) zorunludur.");
        RuleFor(x => x.ProducedQuantity).GreaterThanOrEqualTo(0).WithMessage("Üretilen adet negatif olamaz.");
        RuleFor(x => x.RejectedQuantity).GreaterThanOrEqualTo(0).WithMessage("Hatalı adet negatif olamaz.");
        RuleFor(x => x.ProducedQuantity)
            .Must((request, produced) => produced + request.RejectedQuantity > 0)
            .WithMessage("Sağlam ya da hatalı adetten en az biri girilmeli.");
    }
}

/// <summary>Ofisten bırakılan operasyon: operasyon kalan adediyle bekliyor durumuna döndü.</summary>
public sealed record ReleasedOperationDto(int BundleId, long BundleOperationId, int RemainingQuantity);
