using ERP.Core.Domain.Common;

namespace ERP.Konfeksiyon.Domain.Entities;

/// <summary>
/// Müşteri adına sipariş veren alıcı (buyer / alt müşteri): örn. bir mağaza zincirinin marka ya da bölge alıcısı.
/// Fatura ve e-belge müşteri adına kesilir; buyer siparişte kimin adına sipariş verildiğini gösterir.
/// </summary>
public class CustomerBuyer : AuditableEntity
{
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    /// <summary>Buyer kodu; müşteri içinde tekildir.</summary>
    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;
    public string? ContactName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }

    /// <summary>Pasif buyer yeni siparişte seçilemez; eski siparişlerde görünmeye devam eder.</summary>
    public bool IsActive { get; set; } = true;
}
