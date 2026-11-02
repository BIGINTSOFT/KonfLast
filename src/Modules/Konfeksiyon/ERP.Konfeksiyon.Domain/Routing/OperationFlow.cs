using ERP.Konfeksiyon.Domain.Enums;

namespace ERP.Konfeksiyon.Domain.Routing;

/// <summary>
/// Bir demetin operasyonları arasındaki bağımlılıklar: rota adımı Id'si → ondan önce bitmesi gereken
/// rota adımlarının Id'leri. Demetin rotası ve sıra modu kilitli olduğundan bu bilgi demetin ömrü boyunca değişmez.
/// </summary>
public sealed class OperationFlow
{
    private readonly Dictionary<int, IReadOnlyCollection<int>> _predecessors;

    public OperationFlow(IEnumerable<(int RouteOperationId, int PredecessorId)> dependencies, bool independent = false)
    {
        _predecessors = dependencies
            .GroupBy(d => d.RouteOperationId)
            .ToDictionary(g => g.Key, g => (IReadOnlyCollection<int>)g.Select(d => d.PredecessorId).ToHashSet());
        Independent = independent;
    }

    /// <summary>
    /// Sırasız mod: operasyonların öncülü yoktur, istenen sırayla yapılır. Bir operasyonda hatalı ayrılan parça
    /// demetten çıkar; diğer operasyonlar kalan sağlam adetle devam eder.
    /// </summary>
    public bool Independent { get; }

    /// <summary>
    /// Demetin sıra moduna göre akış: sıralıda her adım sıra numarasına göre bir öncekini bekler, sırasızda
    /// bağımlılık yoktur, rota modunda rotadaki öncül tanımları geçerlidir.
    /// </summary>
    /// <param name="steps">Demetin rota adımları (rota adımı Id'si, sıra no).</param>
    /// <param name="routeDependencies">Rotadaki öncül tanımları; yalnızca rota modunda kullanılır.</param>
    public static OperationFlow For(
        OperationSequenceMode mode,
        IEnumerable<(int RouteOperationId, int SequenceNo)> steps,
        IEnumerable<(int RouteOperationId, int PredecessorId)> routeDependencies)
    {
        switch (mode)
        {
            case OperationSequenceMode.Free:
                return new OperationFlow([], independent: true);

            case OperationSequenceMode.Sequential:
            {
                var ordered = steps.OrderBy(s => s.SequenceNo).Select(s => s.RouteOperationId).ToList();
                return new OperationFlow(ordered.Skip(1).Select((id, index) => (id, ordered[index])));
            }

            default:
                return new OperationFlow(routeDependencies);
        }
    }

    /// <summary>Öncülü olmayan adım için boş döner.</summary>
    public IReadOnlyCollection<int> PredecessorsOf(int routeOperationId) =>
        _predecessors.GetValueOrDefault(routeOperationId) ?? [];

    /// <summary>
    /// Akışın son adım(lar)ı: verilen adımlardan hiçbirinin öncülü olmayanlar. Sıralıda yalnız son sıradaki adım,
    /// sırasızda bütün adımlar, rota modunda ardılı olmayan adımlardır. Demetten çıkan sağlam adet, bu adımlardan
    /// geçen en küçük adettir.
    /// </summary>
    public IReadOnlyCollection<int> FinalSteps(IEnumerable<int> routeOperationIds)
    {
        var ids = routeOperationIds.ToList();
        var predecessors = ids.SelectMany(PredecessorsOf).ToHashSet();
        return ids.Where(id => !predecessors.Contains(id)).ToHashSet();
    }
}
