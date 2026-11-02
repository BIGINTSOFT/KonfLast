using ERP.Konfeksiyon.Application.Abstractions;
using ERP.Konfeksiyon.Domain.Entities;
using ERP.Konfeksiyon.Domain.Enums;
using ERP.Konfeksiyon.Domain.Routing;
using Microsoft.EntityFrameworkCore;

namespace ERP.Konfeksiyon.Application.Features.Bundles;

public static class BundleFlows
{
    /// <summary>
    /// Demetin operasyon akışı, sıra moduna göre: sıralıda sıra numarası, sırasızda bağımlılık yok, rota modunda
    /// (bu özellikten önce açılmış demetler) rotadaki öncül tanımları.
    /// </summary>
    public static async Task<OperationFlow> LoadFlowAsync(
        this IKonfeksiyonDbContext db, Bundle bundle, CancellationToken cancellationToken)
    {
        List<(int RouteOperationId, int PredecessorId)> dependencies = [];
        if (bundle.SequenceMode == OperationSequenceMode.Route)
        {
            var routeOperationIds = bundle.Operations.Select(o => o.RouteOperationId).ToList();
            dependencies = (await db.OperationDependencies
                    .AsNoTracking()
                    .Where(d => routeOperationIds.Contains(d.RouteOperationId))
                    .Select(d => new { d.RouteOperationId, d.PredecessorId })
                    .ToListAsync(cancellationToken))
                .Select(d => (d.RouteOperationId, d.PredecessorId))
                .ToList();
        }

        return OperationFlow.For(
            bundle.SequenceMode, bundle.Operations.Select(o => (o.RouteOperationId, o.SequenceNo)), dependencies);
    }

    /// <summary>
    /// Demetlerin son adımları (rota adımı Id'leri), demet başına; her demetin kendi sıra moduna göre,
    /// <see cref="Bundle.FinishedQuantity"/> ile aynı kuralla. Raporlar için çok sayıda demette tek seferde yüklenir.
    /// </summary>
    /// <param name="bundleIds">Demet Id'leri sorgusu; veritabanında alt sorgu olarak çalışır.</param>
    public static async Task<Dictionary<int, IReadOnlyCollection<int>>> LoadFinalStepsAsync(
        this IKonfeksiyonDbContext db, IQueryable<int> bundleIds, CancellationToken cancellationToken)
    {
        var steps = await db.BundleOperations
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(o => bundleIds.Contains(o.BundleId))
            .Select(o => new { o.BundleId, o.RouteOperationId, o.SequenceNo, o.Bundle.SequenceMode })
            .ToListAsync(cancellationToken);

        // Rota modundaki (eski) demetler için öncüller rotadan okunur; farklı rota adımı sayısı azdır.
        var routeOperationIds = steps
            .Where(s => s.SequenceMode == OperationSequenceMode.Route)
            .Select(s => s.RouteOperationId)
            .Distinct()
            .ToList();
        List<(int RouteOperationId, int PredecessorId)> dependencies = [];
        if (routeOperationIds.Count > 0)
        {
            dependencies = (await db.OperationDependencies
                    .AsNoTracking()
                    .Where(d => routeOperationIds.Contains(d.RouteOperationId))
                    .Select(d => new { d.RouteOperationId, d.PredecessorId })
                    .ToListAsync(cancellationToken))
                .Select(d => (d.RouteOperationId, d.PredecessorId))
                .ToList();
        }

        var dependenciesOf = dependencies.ToLookup(d => d.RouteOperationId);
        return steps
            .GroupBy(s => s.BundleId)
            .ToDictionary(g => g.Key, g =>
            {
                var own = g.Select(s => s.RouteOperationId).ToList();
                var flow = OperationFlow.For(
                    g.First().SequenceMode,
                    g.Select(s => (s.RouteOperationId, s.SequenceNo)),
                    own.SelectMany(id => dependenciesOf[id]));
                return flow.FinalSteps(own);
            });
    }
}
