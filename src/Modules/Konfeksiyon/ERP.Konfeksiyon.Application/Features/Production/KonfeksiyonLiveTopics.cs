using ERP.Core.Application.Abstractions;
using ERP.Konfeksiyon.Domain.Entities;

namespace ERP.Konfeksiyon.Application.Features.Production;

/// <summary>Konfeksiyon kayıtlarının etkilediği canlı ekran alanları.</summary>
public sealed class KonfeksiyonLiveTopics : ILiveTopicSource
{
    public IReadOnlyDictionary<Type, LiveTopic> Topics { get; } = new Dictionary<Type, LiveTopic>
    {
        [typeof(Machine)] = LiveTopic.Machines,
        [typeof(MachineStatusLog)] = LiveTopic.Machines,
        [typeof(LineMachineAssignment)] = LiveTopic.Machines,
        [typeof(WorkSession)] = LiveTopic.Sessions,
        [typeof(WorkSessionPause)] = LiveTopic.Sessions,
        [typeof(Bundle)] = LiveTopic.Production,
        [typeof(BundleOperation)] = LiveTopic.Production,
        [typeof(BundleOperationEvent)] = LiveTopic.Production,
        [typeof(QualityInspection)] = LiveTopic.Quality,
        [typeof(ReworkTask)] = LiveTopic.Quality,
        [typeof(ProductionOrder)] = LiveTopic.Planning,
        [typeof(LineProductionPlan)] = LiveTopic.Planning,
        [typeof(Stock)] = LiveTopic.Stock,
        [typeof(StockTransaction)] = LiveTopic.Stock,
    };
}
