namespace ERP.Boyahane.Domain.Rules;

/// <summary>Fatura tutarları. Ödenecek: matrah + KDV − tevkifat. Kuruş yuvarlaması yukarı (0,125 → 0,13).</summary>
public sealed record InvoiceAmounts(decimal Subtotal, decimal Vat, decimal Withholding, decimal Payable)
{
    public static decimal Line(decimal quantity, decimal unitPrice) => Round(quantity * unitPrice);

    public static InvoiceAmounts Of(decimal subtotal, decimal? vatRate, decimal? withholdingPercent)
    {
        var vat = Round(subtotal * (vatRate ?? 0) / 100);
        var withholding = Round(vat * (withholdingPercent ?? 0) / 100);
        return new InvoiceAmounts(subtotal, vat, withholding, subtotal + vat - withholding);
    }

    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
