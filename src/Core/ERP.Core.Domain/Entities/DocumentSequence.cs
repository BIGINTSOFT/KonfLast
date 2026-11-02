using ERP.Core.Domain.Common;

namespace ERP.Core.Domain.Entities;

/// <summary>Belge numaraları için yıl bazında sayaç (seri + yıl başına tek satır).</summary>
[NotAudited]
public class DocumentSequence
{
    public string Series { get; set; } = null!;
    public int Year { get; set; }
    public int LastValue { get; set; }
}

public static class DocumentSeries
{
    public const string ProductionOrder = "ProductionOrder";
    public const string Bundle = "Bundle";
    public const string PurchaseOrder = "PurchaseOrder";
    public const string Shipment = "Shipment";
    public const string StockCount = "StockCount";
    public const string SalesOrder = "SalesOrder";

    /// <summary>Yıla göre sıfırlanmayan seriler için sabit "yıl" değeri.</summary>
    public const int NoYear = 0;
}
