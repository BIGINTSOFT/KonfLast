using System.ComponentModel.DataAnnotations;
using ERP.Core.Domain.Common;
using ERP.Konfeksiyon.Domain.Enums;
using FluentValidation;

namespace ERP.Konfeksiyon.Application.Features.Rfid;

public sealed class RfidOptions
{
    public const string SectionName = "Rfid";

    /// <summary>
    /// Aynı tablette aynı kart bu süre içinde yeniden okunursa tek okuma sayılır (okuyucular kartı
    /// art arda birkaç kez bildirebilir). 0: sunucu tarafı debounce kapalı.
    /// </summary>
    [Range(0, 60)]
    public int DebounceSeconds { get; set; } = 3;
}

/// <param name="AssignedTo">Kartın şu an atandığı demet ya da operatör (yoksa boş).</param>
public sealed record RfidCardDto(
    int Id, string Uid, RfidCardType CardType, string? Label, string? AssignedTo, bool IsActive);

/// <param name="Uid">Okuyucunun yazdığı kart numarası ("0012629368"); baştaki sıfırlar atlanabilir.</param>
public sealed record RfidCardRequest(string Uid, RfidCardType CardType, string? Label, bool IsActive = true);

public sealed record AssignCardRequest(string Uid);

/// <summary>Okutulan demet kartı ve şu an bağlı olduğu demet (kart boşsa demet alanları boş).</summary>
public sealed record BundleCardDto(string Uid, string? Label, bool IsActive, int? BundleId, string? BundleNo);

/// <summary>Kart boşaltma sonucu: kartın bağı kaldırılan demet.</summary>
public sealed record BundleCardReleaseDto(string Uid, int BundleId, string BundleNo);

/// <param name="Uid">Okunan kartın UID'si.</param>
/// <param name="ReadId">
/// İstemcinin bu okuma için ürettiği kimlik (GUID). Aynı okuma yeniden gönderilirse (ağ tekrarı, çevrimdışı
/// kuyruk) ikinci kez işlenmez.
/// </param>
public sealed record RfidReadRequest(string Uid, Guid ReadId);

/// <param name="Duplicate">Bu yanıt daha önce işlenmiş bir okumanın tekrarı mı (çift okuma).</param>
public sealed record OperatorReadDto(bool Duplicate, int OperatorId, string OperatorCode, string FullName);

/// <summary>Demetin rotasındaki bir adım.</summary>
/// <param name="MachineCanDo">Okutan tabletin makinesi bu operasyonu yapabiliyor mu.</param>
/// <param name="CanStart">Adım şu an bu makinede başlatılabilir mi (hazır ve makine yapabiliyor).</param>
/// <param name="Sam">Bir adedin standart süresi (dakika).</param>
public sealed record BundleReadOperationDto(
    long BundleOperationId, int SequenceNo, string OperationName, BundleOperationStatus Status, int PlannedQuantity,
    int ProducedQuantity, int RejectedQuantity, int RemainingQuantity, bool MachineCanDo = false, bool CanStart = false,
    decimal Sam = 0);

/// <param name="Duplicate">Bu yanıt daha önce işlenmiş bir okumanın tekrarı mı (çift okuma).</param>
/// <param name="MachineOperation">
/// Bu makinede işlenecek adım: başlatılabilir ilk adım, yoksa makinenin yapabildiği ilk bitmemiş adım;
/// demette bu makinenin yapabildiği adım yoksa (ya da tablet bir makineye bağlı değilse) boş.
/// </param>
/// <param name="Operations">Demetin rotasındaki tüm adımlar, sırasıyla.</param>
public sealed record BundleReadDto(
    bool Duplicate,
    int BundleId,
    string BundleNo,
    string OrderNo,
    string ModelCode,
    string ColorName,
    string SizeName,
    int Quantity,
    BundleStatus Status,
    BundleReadOperationDto? MachineOperation,
    IReadOnlyList<BundleReadOperationDto> Operations);

public static class RfidValidation
{
    public const string InvalidUidMessage = "Geçerli bir kart numarası girin (en fazla 10 harf ya da rakam, örn. 0012629368).";

    public static IRuleBuilderOptions<T, string> ValidUid<T>(this IRuleBuilder<T, string> rule) => rule
        .NotEmpty().WithMessage("Kart UID'si zorunludur.")
        .Must(uid => RfidUid.Normalize(uid) is not null).WithMessage(InvalidUidMessage);
}

public sealed class RfidCardRequestValidator : AbstractValidator<RfidCardRequest>
{
    public RfidCardRequestValidator()
    {
        RuleFor(x => x.Uid).ValidUid();
        RuleFor(x => x.CardType).IsInEnum().WithMessage("Kart türü geçersiz.");
        RuleFor(x => x.Label).MaximumLength(50).WithMessage("Etiket en fazla 50 karakter olabilir.");
    }
}

public sealed class AssignCardRequestValidator : AbstractValidator<AssignCardRequest>
{
    public AssignCardRequestValidator()
    {
        RuleFor(x => x.Uid).ValidUid();
    }
}

public sealed class RfidReadRequestValidator : AbstractValidator<RfidReadRequest>
{
    public RfidReadRequestValidator()
    {
        RuleFor(x => x.Uid).ValidUid();
        RuleFor(x => x.ReadId).NotEmpty().WithMessage("Okuma kimliği (readId) zorunludur.");
    }
}
