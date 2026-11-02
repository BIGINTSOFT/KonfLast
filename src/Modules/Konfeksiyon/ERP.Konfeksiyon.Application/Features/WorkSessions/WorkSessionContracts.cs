using ERP.Core.Application.Common;
using ERP.Core.Application.Features.MasterData;
using ERP.Konfeksiyon.Domain.Enums;
using FluentValidation;

namespace ERP.Konfeksiyon.Application.Features.WorkSessions;

public sealed class ProductionOptions
{
    public const string SectionName = "Production";

    /// <summary>
    /// Açıkken operatör yalnızca yetkin olduğu operasyonları (ve tanımlıysa atandığı makineleri) çalışabilir.
    /// Kapatılırsa her aktif operatör her makinede çalışabilir.
    /// </summary>
    public bool RequireOperatorQualification { get; set; } = true;

    /// <summary>Fabrikanın saat dilimi; vardiya ve mola saatleri bu yerel saate göredir.</summary>
    public string TimeZone { get; set; } = "Europe/Istanbul";

    /// <summary>
    /// Planlı mola, bitiş saatinden en fazla bu kadar dakika sonrasına kadar başlatılabilir (eldeki işi
    /// bitirmek için molaya geç çıkan operatör).
    /// </summary>
    public int BreakLateStartMinutes { get; set; } = 30;

    /// <summary>Vardiyası biten açık oturumların kendiliğinden kapatılması.</summary>
    public bool SessionAutoCloseEnabled { get; set; } = true;

    /// <summary>Oturum, vardiyanın bitişinden bu kadar dakika sonra kapatılır.</summary>
    public int AutoCloseAfterShiftMinutes { get; set; } = 60;

    /// <summary>Hiçbir vardiyaya denk gelmeyen oturumun açık kalabileceği en uzun süre (saat).</summary>
    public int MaxSessionHours { get; set; } = 16;

    /// <summary>
    /// Bir işin verimi bu yüzdeye ulaşırsa raporda "olağan dışı hızlı" olarak işaretlenir (%200: hedef sürenin
    /// yarısında bitmiş). İş engellenmez; işaret, sürenin erken kesilmiş olabileceğini gösterir.
    /// </summary>
    public int SuspiciousEfficiencyPercent { get; set; } = 200;

    /// <summary>Önceki operasyon tamamlandıktan sonra sıradaki bu kadar dakika içinde başlamazsa "geç devir" sayılır.</summary>
    public int HandoverWarningMinutes { get; set; } = 15;

    /// <summary>
    /// Oturum açık, iş yok ve demet beklediği de bildirilmemişse tablet bu kadar dakika sonra operatöre sorar.
    /// 0: sorulmaz.
    /// </summary>
    public int IdlePromptMinutes { get; set; } = 5;
}

/// <summary>Tabletteki oturum bilgisi.</summary>
/// <param name="Duplicate">Operatörün bu makinede zaten açık oturumu vardı; yeni oturum açılmadı.</param>
/// <param name="Status">Operatörün şu anki durumu (çalışıyor, molada, ara verdi, arıza, demet bekliyor).</param>
/// <param name="CurrentPause">Süren duraklama (varsa).</param>
/// <param name="DueBreak">Zamanı gelmiş, henüz çıkılmamış planlı mola (varsa).</param>
/// <param name="Role">Oturum sahibinin sahadaki görevi.</param>
/// <param name="CanPause">Oturum sahibi kendi yetkisiyle ara verebilir mi (veremiyorsa yetkili kartı istenir).</param>
/// <param name="CanReportBreakdown">Oturum sahibi kendi yetkisiyle arıza bildirebilir mi.</param>
public sealed record TabletSessionDto(
    bool Duplicate,
    long SessionId,
    int OperatorId,
    string OperatorCode,
    string FullName,
    DateTime LoginAt,
    WorkSessionStatus Status,
    TabletPauseDto? CurrentPause,
    DueBreakDto? DueBreak,
    StaffRole Role = StaffRole.Operator,
    bool CanPause = false,
    bool CanReportBreakdown = false);

/// <param name="ReasonName">Arada seçilen nedenin adı (Duruş Nedenleri ana verisinden).</param>
public sealed record TabletPauseDto(WorkSessionPauseType Type, string? ReasonName, string? BreakName, DateTime StartedAt);

/// <param name="Type">BREAK (planlı mola), PAUSE (ara) ya da WAITING_FOR_BUNDLE (demet bekleme).</param>
/// <param name="ReasonId">PAUSE için zorunlu: Duruş Nedenleri ana verisinden aktif bir ara nedeni.</param>
/// <param name="AuthorizerUid">Ara için okutulan kart: kişiye özel yetkili oturum sahibi ya da bant şefi.</param>
public sealed record PauseSessionRequest(
    WorkSessionPauseType Type, int? ReasonId = null, string? Note = null, string? AuthorizerUid = null);

/// <param name="AuthorizerUid">Aradan dönüşte okutulan kart; mola ve demet beklemeden dönüşte gerekmez.</param>
public sealed record ResumeSessionRequest(string? AuthorizerUid = null);

public sealed class PauseSessionRequestValidator : AbstractValidator<PauseSessionRequest>
{
    public PauseSessionRequestValidator()
    {
        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Geçersiz duraklama türü.")
            .NotEqual(WorkSessionPauseType.MachineBreakdown)
            .WithMessage("Makine arızası 'Arıza bildir' ile bildirilir.");
        RuleFor(x => x.ReasonId)
            .NotNull().WithMessage("Ara nedeni seçilmelidir.")
            .When(x => x.Type == WorkSessionPauseType.Pause);
        RuleFor(x => x.Note).MaximumLength(200).WithMessage("Not en fazla 200 karakter olabilir.");
    }
}

/// <param name="ReasonName">Arada seçilen nedenin adı.</param>
/// <param name="BreakName">Planlı molada mola planının adı.</param>
/// <param name="DurationMinutes">Süre; duraklama sürüyorsa şu ana kadar geçen süre.</param>
public sealed record WorkSessionPauseDto(
    long Id, WorkSessionPauseType Type, string? ReasonName, string? BreakName, string? Note, DateTime StartedAt,
    DateTime? EndedAt, double DurationMinutes);

/// <param name="DurationMinutes">Oturum süresi; açık oturumda şu ana kadar geçen süre.</param>
/// <param name="CanClose">Oturum açık mı (yetkili kullanıcı kapatabilir).</param>
public sealed record WorkSessionDto(
    long Id,
    int OperatorId,
    string OperatorCode,
    string OperatorName,
    int MachineId,
    string MachineCode,
    string TabletCode,
    DateTime LoginAt,
    DateTime? LogoutAt,
    double DurationMinutes,
    WorkSessionStatus Status,
    WorkSessionEndReason? EndReason,
    string? ClosedBy,
    bool CanClose);

public sealed class WorkSessionListQuery : PageRequest
{
    /// <summary>Operatör adı / sicili ya da makine kodu içinde aranır.</summary>
    public string? Search { get; set; }

    /// <summary>true: yalnızca açık oturumlar; false: yalnızca kapanmışlar.</summary>
    public bool? IsOpen { get; set; }
    public int? OperatorId { get; set; }
    public int? MachineId { get; set; }

    /// <summary>Giriş zamanı bu andan sonra (UTC).</summary>
    public DateTime? From { get; set; }

    /// <summary>Giriş zamanı bu andan önce (UTC).</summary>
    public DateTime? To { get; set; }
}

/// <param name="Operations">Operatörün yapabildiği operasyonlar.</param>
/// <param name="Machines">
/// Operatörün çalışabileceği makineler. Boşsa makine kısıtı yoktur: yetkin olduğu operasyonu yapan her
/// makinede çalışabilir.
/// </param>
public sealed record OperatorQualificationsDto(
    int OperatorId, string OperatorName, IReadOnlyList<LookupDto> Operations, IReadOnlyList<LookupDto> Machines);

public sealed record OperatorQualificationsRequest(IReadOnlyList<int> OperationIds, IReadOnlyList<int>? MachineIds = null);

public sealed class OperatorQualificationsRequestValidator : AbstractValidator<OperatorQualificationsRequest>
{
    public const int MaxItems = 500;

    public OperatorQualificationsRequestValidator()
    {
        RuleFor(x => x.OperationIds)
            .NotNull().WithMessage("Operasyon listesi zorunludur (boş olabilir).")
            .Must(ids => ids is null || ids.Count <= MaxItems).WithMessage($"En fazla {MaxItems} operasyon seçilebilir.")
            .Must(ids => ids is null || ids.All(id => id > 0)).WithMessage("Geçersiz operasyon.");
        RuleFor(x => x.MachineIds)
            .Must(ids => ids is null || ids.Count <= MaxItems).WithMessage($"En fazla {MaxItems} makine seçilebilir.")
            .Must(ids => ids is null || ids.All(id => id > 0)).WithMessage("Geçersiz makine.");
    }
}
