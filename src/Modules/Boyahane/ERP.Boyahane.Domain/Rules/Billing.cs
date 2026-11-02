using ERP.Boyahane.Domain.Enums;
using ERP.Core.Domain.Enums;

namespace ERP.Boyahane.Domain.Rules;

/// <summary>Fatura ölçüsü ve fire hesabı.</summary>
public static class Billing
{
    /// <summary>
    /// Satırın fatura ölçüsü: önce satırdaki seçim, yoksa müşteri ayarı, o da yoksa sipariş birimi.
    /// İplikte metre tutulmadığı için ölçü kilodur.
    /// </summary>
    public static StockUnit Resolve(BillingUnit? line, BillingUnit? customer, StockUnit orderUnit, bool isYarn)
    {
        if (isYarn)
        {
            return StockUnit.Kilogram;
        }

        return (line ?? customer ?? BillingUnit.OrderUnit) switch
        {
            BillingUnit.Meter => StockUnit.Meter,
            BillingUnit.Kilogram => StockUnit.Kilogram,
            _ => orderUnit,
        };
    }

    /// <summary>Çıkış ölçüsü girdinin bu oranından fazla olamaz; aşım yalnızca yetkiyle ve gerekçeyle geçer.</summary>
    public const decimal MaxFinalExcessPercent = 5;

    public static bool ExceedsFinalLimit(decimal? input, decimal? final) =>
        input is > 0 && final is { } value && value > input.Value * (1 + MaxFinalExcessPercent / 100);

    /// <summary>Girişe göre kayıp yüzdesi; çıkış girişten fazlaysa eksi olur.</summary>
    public static decimal? LossPercent(decimal? input, decimal? final) =>
        input is > 0 && final is { } value ? Math.Round((input.Value - value) / input.Value * 100, 2) : null;
}
