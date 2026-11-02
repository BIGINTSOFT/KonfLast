using System.ComponentModel.DataAnnotations;
using ERP.Core.Application.Features.MasterData;
using ERP.Konfeksiyon.Application.Features.Shifts;
using ERP.Konfeksiyon.Application.Features.WorkSessions;
using ERP.Konfeksiyon.Domain.Enums;
using FluentValidation;

namespace ERP.Konfeksiyon.Application.Features.Tablets;

public sealed class TabletOptions
{
    public const string SectionName = "Tablets";

    /// <summary>Tabletten bu süre içinde istek geldiyse çevrimiçi sayılır.</summary>
    [Range(10, 3600)]
    public int OnlineThresholdSeconds { get; set; } = 90;
}

/// <param name="IsEnrolled">Fiziksel bir cihaz eşleştirilmiş mi.</param>
/// <param name="IsOnline">Tabletten yakın zamanda istek geldi mi (sunucu saatiyle).</param>
public sealed record TabletDto(
    int Id,
    string TabletCode,
    string? Name,
    int? MachineId,
    string? MachineCode,
    string? MachineName,
    string? ProductionLineCode,
    string? DeviceIdentifier,
    bool IsEnrolled,
    bool IsOnline,
    DateTime? LastSeenAt,
    bool IsActive);

public sealed record TabletRequest(string TabletCode, string? Name, int? MachineId, bool IsActive = true);

public sealed record TabletTokenRequest(string TabletCode, string DeviceKey, string DeviceIdentifier);

public sealed record TabletTokenResponse(string AccessToken, DateTime AccessTokenExpiresAt);

/// <param name="StatusReason">Makinenin şu anki durumu için kaydedilmiş neden (ör. arıza nedeni); yoksa boş.</param>
public sealed record TabletMachineDto(
    int Id, string MachineCode, string MachineName, string MachineTypeName, MachineStatus Status, string? StatusReason);

public sealed record TabletOperationDto(int Id, string OperationCode, string Name, decimal Sam);

public sealed record TabletLineDto(int Id, string LineCode, string Name);

public sealed record TabletPlanDto(
    int ProductionOrderId, string OrderNo, string ModelCode, string ProductName, string CustomerName, int PlannedQuantity);

/// <summary>
/// Tabletin "neredeyim?" bilgisi: bağlı olduğu makine, makinenin operasyonu, bantı ve bantın şu anki planı.
/// Eksik halkalar null gelir (ör. tablet henüz bir makineye bağlanmamış).
/// </summary>
/// <param name="ServerTime">Sunucu saati (UTC); üretim zamanları için cihaz saati kullanılmaz.</param>
/// <param name="PauseReasons">"Ara ver" seçenekleri (Duruş Nedenleri ana verisinden, sırasıyla).</param>
/// <param name="BreakdownReasons">"Arıza bildir" seçenekleri (Duruş Nedenleri ana verisinden, sırasıyla).</param>
/// <param name="IdlePromptMinutes">
/// Oturum açık, iş yok ve demet beklediği bildirilmemişse tabletin operatöre sormadan önce bekleyeceği süre (0: sorulmaz).
/// </param>
/// <param name="StaffRoles">
/// Görevlerin ortak yetkileri ("Roller ve Yetkiler" ekranından); tablet, hangi görevin kartının istendiğini bundan gösterir.
/// </param>
public sealed record TabletContextDto(
    int TabletId,
    string TabletCode,
    DateTime ServerTime,
    TabletMachineDto? Machine,
    TabletOperationDto? Operation,
    TabletLineDto? Line,
    TabletPlanDto? CurrentPlan,
    IReadOnlyList<StopReasonOptionDto> PauseReasons,
    IReadOnlyList<StopReasonOptionDto> BreakdownReasons,
    IReadOnlyList<StaffRoleSettingDto> StaffRoles,
    int IdlePromptMinutes = 0);

/// <param name="ReasonId">Duruş Nedenleri ana verisinden aktif bir arıza nedeni (isteğe bağlı).</param>
/// <param name="Note">Operatörün ek açıklaması (isteğe bağlı).</param>
/// <param name="AuthorizerUid">
/// Oturum sahibinin arıza bildirme yetkisi yoksa (ya da oturum yoksa), onaylayan yetkilinin okutulan kart numarası.
/// </param>
public sealed record ReportBreakdownRequest(int? ReasonId = null, string? Note = null, string? AuthorizerUid = null);

/// <param name="AuthorizerUid">Arızanın giderildiğini doğrulayan bakım operatörünün ya da bant şefinin kart numarası.</param>
public sealed record ReportRecoveryRequest(string? AuthorizerUid = null);

public sealed class TabletRequestValidator : AbstractValidator<TabletRequest>
{
    public TabletRequestValidator()
    {
        RuleFor(x => x.TabletCode).ValidCode();
        RuleFor(x => x.Name).OptionalText(150);
    }
}

public sealed class TabletTokenRequestValidator : AbstractValidator<TabletTokenRequest>
{
    public TabletTokenRequestValidator()
    {
        RuleFor(x => x.TabletCode).NotEmpty().MaximumLength(30);
        RuleFor(x => x.DeviceKey).NotEmpty().MaximumLength(200);
        RuleFor(x => x.DeviceIdentifier).NotEmpty().MaximumLength(200);
    }
}

public sealed class ReportBreakdownRequestValidator : AbstractValidator<ReportBreakdownRequest>
{
    public ReportBreakdownRequestValidator()
    {
        RuleFor(x => x.Note).MaximumLength(150).WithMessage("Açıklama en fazla 150 karakter olabilir.");
    }
}
