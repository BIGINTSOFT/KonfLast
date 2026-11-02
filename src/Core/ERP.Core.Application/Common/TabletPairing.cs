using FluentValidation;

namespace ERP.Core.Application.Common;

public sealed class TabletPairingOptions
{
    public const string SectionName = "TabletPairing";

    /// <summary>Tablet sistem ayarlarının parolası; boşsa tablet eşleştirilemez.</summary>
    public string Password { get; set; } = string.Empty;
}

public sealed record TabletPairingListRequest(string Password, string DeviceIdentifier);

/// <param name="MachineId">Eşleştirilecek makine.</param>
/// <param name="TabletId">Makinesiz tablet kaydı (kontrol noktası); makine seçilmediyse.</param>
/// <param name="TakeOver">İstasyon başka bir cihaza bağlıysa bağı devral.</param>
public sealed record PairTabletRequest(
    string Password, string DeviceIdentifier, int? MachineId = null, int? TabletId = null, bool TakeOver = false);

/// <summary>Tabletin eşleştirilebileceği istasyon: bir makine ya da makinesiz tablet kaydı.</summary>
/// <param name="TabletCode">İstasyonun tablet kaydı; makineye henüz tablet tanımlanmadıysa boş.</param>
/// <param name="IsPaired">Bir cihaz bağlı mı.</param>
/// <param name="IsThisDevice">Bağlı cihaz soran tablet mi.</param>
public sealed record TabletPairingStationDto(
    int? MachineId,
    int? TabletId,
    string Code,
    string Name,
    string? Detail,
    string? TabletCode,
    bool IsPaired,
    bool IsThisDevice);

/// <param name="DeviceKey">Cihazın saklayacağı anahtar; yalnızca bu yanıtta görünür.</param>
public sealed record PairTabletResponse(int TabletId, string TabletCode, string DeviceKey, string StationCode, string StationName);

public sealed class TabletPairingListRequestValidator : AbstractValidator<TabletPairingListRequest>
{
    public TabletPairingListRequestValidator()
    {
        RuleFor(x => x.Password).NotEmpty().WithMessage("Parola zorunludur.").MaximumLength(100);
        RuleFor(x => x.DeviceIdentifier).Length(8, 200).WithMessage("Cihaz kimliği 8-200 karakter olmalıdır.");
    }
}

public sealed class PairTabletRequestValidator : AbstractValidator<PairTabletRequest>
{
    public PairTabletRequestValidator()
    {
        RuleFor(x => x.Password).NotEmpty().WithMessage("Parola zorunludur.").MaximumLength(100);
        RuleFor(x => x.DeviceIdentifier).Length(8, 200).WithMessage("Cihaz kimliği 8-200 karakter olmalıdır.");
        RuleFor(x => x).Must(x => x.MachineId is null != x.TabletId is null).WithMessage("Makine ya da kontrol noktası seçilmelidir.");
    }
}

public static class TabletPairing
{
    private const int MaxCodeLength = 30;

    public static readonly Error InvalidPassword = new(ErrorCodes.InvalidPairingPassword, "Parola geçersiz.", ErrorType.Unauthorized);

    public static readonly Error StationNotFound = new(ErrorCodes.NotFound, "Makine bulunamadı ya da pasif.", ErrorType.NotFound);

    public static readonly Error TabletInactive = new(
        ErrorCodes.TabletInactive, "Bu istasyonun tablet kaydı pasif. Yönetim ekranından aktif yapın.", ErrorType.Conflict);

    public static Error AlreadyPaired(string stationCode) => new(
        ErrorCodes.TabletAlreadyEnrolled, $"{stationCode} başka bir cihaza bağlı.", ErrorType.Conflict);

    /// <summary>Parola boş tanımlıysa kimse eşleştiremez; karşılaştırma sabit sürelidir.</summary>
    public static bool PasswordMatches(TabletPairingOptions options, string? supplied) =>
        !string.IsNullOrEmpty(options.Password) && Secrets.Matches(Secrets.Hash(options.Password), supplied);

    /// <summary>Tablet kaydı olmayan makineye eşleştirmede otomatik verilen kod; sığmazsa makine kimliğinden üretilir.</summary>
    public static string NewTabletCode(string prefix, string machineCode, int machineId)
    {
        var code = $"{prefix}-{machineCode}";
        return code.Length <= MaxCodeLength ? code : $"{prefix}-M{machineId}";
    }
}
