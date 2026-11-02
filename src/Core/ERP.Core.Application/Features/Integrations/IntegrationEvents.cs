using ERP.Core.Domain.Enums;

namespace ERP.Core.Application.Features.Integrations;

/// <summary>Bir modülün dış sistemlere duyurduğu olay türleri (entegrasyon ekranında listelenir).</summary>
public interface IIntegrationEventCatalog
{
    IReadOnlyList<IntegrationEventTypeDto> EventTypes { get; }
}

public sealed record IntegrationEventTypeDto(string Type, string Label);

/// <summary>Müşteri ya da tedarikçi.</summary>
public sealed record PartyPayload(string Code, string Name, string? TaxNumber);

public sealed record StockLinePayload(string ItemCode, string ItemName, StockUnit Unit, decimal Quantity, decimal? UnitPrice = null);
