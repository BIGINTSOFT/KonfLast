using FluentValidation;

namespace ERP.Konfeksiyon.Application.Features.Payroll;

/// <param name="MinEfficiencyPercent">Kademenin başladığı günlük verim (%).</param>
/// <param name="BonusPercent">Parça başı kazanca eklenecek prim oranı (%).</param>
public sealed record BonusTierDto(decimal MinEfficiencyPercent, decimal BonusPercent);

/// <param name="MinuteRate">Bir standart dakikanın ücreti; birim ücreti girilmemiş operasyonlarda SAM ile çarpılır.</param>
public sealed record PayrollSettingsDto(
    decimal MinuteRate, string Currency, IReadOnlyList<BonusTierDto> BonusTiers, DateTime? UpdatedAt, string? UpdatedBy);

public sealed record UpdatePayrollSettingsRequest(
    decimal MinuteRate, string? Currency, IReadOnlyList<BonusTierDto>? BonusTiers);

public sealed class EarningsQuery
{
    /// <summary>Aralığın başı (UTC, dahil).</summary>
    public DateTime From { get; set; }

    /// <summary>Aralığın sonu (UTC, hariç).</summary>
    public DateTime To { get; set; }
}

/// <summary>Bir operatörün aralıktaki kazancı. Tutarlar ayarlardaki para birimindedir.</summary>
/// <param name="Produced">Bildirilen sağlam adet.</param>
/// <param name="EarnedMinutes">Üretilen adet × SAM.</param>
/// <param name="EfficiencyPercent">Aralığın tamamındaki verim (Performans ekranıyla aynı hesap).</param>
/// <param name="PieceEarnings">Parça başı kazanç: adet × birim ücret.</param>
/// <param name="BonusAmount">Günlük verim kademelerinden gelen prim.</param>
/// <param name="UnpricedQuantity">Ücreti tanımlı olmayan operasyonlarda üretilen adet (kazanca girmez).</param>
public sealed record OperatorEarningsDto(
    int OperatorId,
    string OperatorCode,
    string OperatorName,
    int DaysWorked,
    int Produced,
    double EarnedMinutes,
    double? EfficiencyPercent,
    decimal PieceEarnings,
    decimal BonusAmount,
    decimal TotalEarnings,
    int UnpricedQuantity);

public sealed record PayrollReportDto(
    DateTime From,
    DateTime To,
    string Currency,
    decimal MinuteRate,
    OperatorEarningsDto Total,
    IReadOnlyList<OperatorEarningsDto> Rows);

/// <param name="BonusPercent">O günün veriminin ulaştığı kademenin prim oranı.</param>
public sealed record EarningsDayDto(
    DateOnly Day,
    int Produced,
    double? EfficiencyPercent,
    decimal PieceEarnings,
    decimal BonusPercent,
    decimal BonusAmount,
    decimal TotalEarnings);

/// <param name="UnitRate">Birim ücret; aralıkta ücret değiştiyse ortalaması. Ücret tanımlı değilse boş.</param>
public sealed record EarningsOperationDto(
    int OperationId, string OperationCode, string OperationName, int Produced, decimal? UnitRate, decimal Amount);

public sealed record OperatorEarningsDetailDto(
    DateTime From,
    DateTime To,
    string Currency,
    OperatorEarningsDto Summary,
    IReadOnlyList<EarningsDayDto> Days,
    IReadOnlyList<EarningsOperationDto> Operations);

public sealed class UpdatePayrollSettingsRequestValidator : AbstractValidator<UpdatePayrollSettingsRequest>
{
    public const int MaxTiers = 10;

    public UpdatePayrollSettingsRequestValidator()
    {
        RuleFor(x => x.MinuteRate)
            .InclusiveBetween(0, 10_000).WithMessage("Dakika ücreti 0 ile 10.000 arasında olmalıdır.")
            .PrecisionScale(18, 4, ignoreTrailingZeros: true).WithMessage("Dakika ücreti en fazla 4 ondalık basamak içerebilir.");
        RuleFor(x => x.Currency)
            .Matches("^\\s*[A-Za-z]{3}\\s*$").WithMessage("Para birimi 3 harfli olmalıdır (örn. TRY).")
            .When(x => !string.IsNullOrWhiteSpace(x.Currency));
        RuleFor(x => x.BonusTiers)
            .Must(tiers => tiers is null || tiers.Count <= MaxTiers)
            .WithMessage($"En fazla {MaxTiers} prim kademesi tanımlanabilir.")
            .Must(tiers => tiers is null || tiers.All(t => t.MinEfficiencyPercent is > 0 and <= 1000))
            .WithMessage("Kademenin verim eşiği 0'dan büyük ve en fazla 1000 olmalıdır.")
            .Must(tiers => tiers is null || tiers.All(t => t.BonusPercent is > 0 and <= 500))
            .WithMessage("Prim oranı 0'dan büyük ve en fazla 500 olmalıdır.")
            .Must(tiers => tiers is null || tiers.Select(t => t.MinEfficiencyPercent).Distinct().Count() == tiers.Count)
            .WithMessage("Aynı verim eşiği birden fazla kademede kullanılamaz.")
            .Must(tiers => tiers is null || tiers.All(t =>
                decimal.Round(t.MinEfficiencyPercent, 2) == t.MinEfficiencyPercent && decimal.Round(t.BonusPercent, 2) == t.BonusPercent))
            .WithMessage("Verim eşiği ve prim oranı en fazla 2 ondalık basamak içerebilir.");
    }
}
