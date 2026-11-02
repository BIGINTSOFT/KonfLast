namespace ERP.Konfeksiyon.Domain.Rules;

/// <summary>
/// Fatura tutarları. KDV satır bazında (ürünün oranıyla) hesaplanıp toplanır. Ödenecek: matrah + KDV − tevkifat.
/// Kuruş yuvarlaması yukarı (0,125 → 0,13).
/// </summary>
public sealed record InvoiceAmounts(decimal Subtotal, decimal Vat, decimal Withholding, decimal Payable)
{
    /// <summary>Satır tutarı: adet × birim fiyat, varsa iskonto düşülmüş.</summary>
    public static decimal Line(decimal quantity, decimal unitPrice, decimal? discountPercent = null) =>
        Round(quantity * unitPrice * (1 - (discountPercent ?? 0) / 100));

    public static decimal LineVat(decimal lineAmount, decimal vatRate) => Round(lineAmount * vatRate / 100);

    public static InvoiceAmounts Of(IEnumerable<(decimal Amount, decimal VatRate)> lines, decimal? withholdingPercent)
    {
        var list = lines.ToList();
        var subtotal = list.Sum(l => l.Amount);
        var vat = list.Sum(l => LineVat(l.Amount, l.VatRate));
        var withholding = Round(vat * (withholdingPercent ?? 0) / 100);
        return new InvoiceAmounts(subtotal, vat, withholding, subtotal + vat - withholding);
    }

    /// <summary>Açılış borcu: KDV'siz, doğrudan ödenecek tutar.</summary>
    public static InvoiceAmounts Opening(decimal amount) => new(amount, 0, 0, amount);

    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
