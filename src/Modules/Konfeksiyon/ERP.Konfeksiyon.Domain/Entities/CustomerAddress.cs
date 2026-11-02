using ERP.Core.Domain.Common;

namespace ERP.Konfeksiyon.Domain.Entities;

/// <summary>
/// Müşteri adresi. Bir müşterinin birden fazla adresi olabilir (merkez, depo, mağaza …); biri varsayılan fatura,
/// biri varsayılan teslimat adresidir (aynı adres ikisi de olabilir).
/// </summary>
public class CustomerAddress : AuditableEntity
{
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    /// <summary>Adresin kısa adı (örn. "Merkez", "Depo").</summary>
    public string Title { get; set; } = null!;

    /// <summary>Mahalle, cadde / sokak, bina ve kapı numarası.</summary>
    public string AddressLine { get; set; } = null!;
    public string District { get; set; } = null!;
    public string City { get; set; } = null!;
    public string? PostalCode { get; set; }

    /// <summary>ISO 3166-1 alfa-2 ülke kodu (TR, DE …).</summary>
    public string CountryCode { get; set; } = "TR";

    public string? ContactName { get; set; }
    public string? Phone { get; set; }

    public bool IsDefaultInvoice { get; set; }
    public bool IsDefaultDelivery { get; set; }
}
