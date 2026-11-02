namespace ERP.Konfeksiyon.Domain.Routing;

/// <summary>Rotadaki bir operasyon: sıra numarası ve ondan önce tamamlanması gereken operasyonlar.</summary>
public sealed record RouteNode(int SequenceNo, IReadOnlyCollection<int> Predecessors);

public enum RouteGraphErrorType
{
    DuplicateSequence,
    SelfDependency,
    UnknownPredecessor,
    Cycle,
}

public sealed record RouteGraphError(RouteGraphErrorType Type, string Message);

/// <summary>
/// Operasyon rotasının bağımlılık grafiği. Rota yönlü ve döngüsüz (DAG) olmalıdır: bir operasyonun
/// birden fazla öncülü ve ardılı olabilir (paralel akış), ama hiçbir operasyon dolaylı olarak
/// kendisine bağlı olamaz.
/// </summary>
public sealed class RouteGraph
{
    private readonly Dictionary<int, IReadOnlyCollection<int>> _predecessors;

    private RouteGraph(
        Dictionary<int, IReadOnlyCollection<int>> predecessors,
        IReadOnlyList<RouteGraphError> errors,
        IReadOnlyDictionary<int, int> stages)
    {
        _predecessors = predecessors;
        Errors = errors;
        Stages = stages;
    }

    public IReadOnlyList<RouteGraphError> Errors { get; }
    public bool IsValid => Errors.Count == 0;

    /// <summary>
    /// Her operasyonun aşaması (0'dan başlar): öncülü olmayanlar 0, diğerleri en geç öncülünden bir
    /// sonraki aşamadadır. Aynı aşamadaki operasyonlar paralel yürüyebilir. Grafik geçersizse boştur.
    /// </summary>
    public IReadOnlyDictionary<int, int> Stages { get; }

    public static RouteGraph Analyze(IReadOnlyCollection<RouteNode> nodes)
    {
        var errors = new List<RouteGraphError>();

        foreach (var duplicate in nodes.GroupBy(n => n.SequenceNo).Where(g => g.Count() > 1))
        {
            errors.Add(new RouteGraphError(
                RouteGraphErrorType.DuplicateSequence, $"{duplicate.Key} sıra numarası birden fazla kez kullanılmış."));
        }

        var predecessors = nodes
            .GroupBy(n => n.SequenceNo)
            .ToDictionary(g => g.Key, g => (IReadOnlyCollection<int>)g.First().Predecessors.Distinct().ToList());

        foreach (var (sequenceNo, required) in predecessors.OrderBy(p => p.Key))
        {
            foreach (var predecessor in required)
            {
                if (predecessor == sequenceNo)
                {
                    errors.Add(new RouteGraphError(
                        RouteGraphErrorType.SelfDependency, $"{sequenceNo} numaralı operasyon kendisine bağlı olamaz."));
                }
                else if (!predecessors.ContainsKey(predecessor))
                {
                    errors.Add(new RouteGraphError(
                        RouteGraphErrorType.UnknownPredecessor,
                        $"{sequenceNo} numaralı operasyonun öncülü olan {predecessor} rotada yok."));
                }
            }
        }

        if (errors.Count > 0)
        {
            return new RouteGraph(predecessors, errors, new Dictionary<int, int>());
        }

        var stages = ComputeStages(predecessors);
        if (stages.Count < predecessors.Count)
        {
            var cycle = FindCycle(predecessors, stages);
            errors.Add(new RouteGraphError(
                RouteGraphErrorType.Cycle,
                $"Operasyonlar arasında döngü var: {string.Join(" → ", cycle)}."));
            return new RouteGraph(predecessors, errors, new Dictionary<int, int>());
        }

        return new RouteGraph(predecessors, errors, stages);
    }

    /// <summary>Operasyonun henüz tamamlanmamış öncülleri (başlatılamıyorsa nedenini söylemek için).</summary>
    public IReadOnlyList<int> MissingPredecessors(int sequenceNo, IReadOnlySet<int> completed)
    {
        EnsureValid();
        return _predecessors[sequenceNo].Where(p => !completed.Contains(p)).Order().ToList();
    }

    private void EnsureValid()
    {
        if (!IsValid)
        {
            throw new InvalidOperationException("Geçersiz rota grafiği üzerinde sorgu yapılamaz.");
        }
    }

    /// <summary>Kahn algoritması: öncülleri yerleşen operasyonlar sırayla aşamalara atanır.</summary>
    private static Dictionary<int, int> ComputeStages(Dictionary<int, IReadOnlyCollection<int>> predecessors)
    {
        var stages = new Dictionary<int, int>();
        var remaining = new HashSet<int>(predecessors.Keys);

        while (remaining.Count > 0)
        {
            var placeable = remaining.Where(n => predecessors[n].All(stages.ContainsKey)).ToList();
            if (placeable.Count == 0)
            {
                break; // kalanlar bir döngünün içinde ya da arkasında
            }

            foreach (var node in placeable)
            {
                stages[node] = predecessors[node].Count == 0 ? 0 : predecessors[node].Max(p => stages[p]) + 1;
                remaining.Remove(node);
            }
        }

        return stages;
    }

    /// <summary>Yerleştirilemeyen operasyonlar arasında öncülleri geriye doğru izleyerek bir döngü bulur.</summary>
    private static List<int> FindCycle(
        Dictionary<int, IReadOnlyCollection<int>> predecessors, Dictionary<int, int> placed)
    {
        var path = new List<int>();
        var current = predecessors.Keys.Where(n => !placed.ContainsKey(n)).Min();

        while (!path.Contains(current))
        {
            path.Add(current);
            // Yerleşmemiş her operasyonun en az bir yerleşmemiş öncülü vardır.
            current = predecessors[current].Where(p => !placed.ContainsKey(p)).Min();
        }

        var cycle = path.Skip(path.IndexOf(current)).ToList();
        cycle.Reverse(); // öncül → ardıl yönünde okunsun

        // Mesaj her seferinde aynı olsun: döngü en küçük sıra numarasından başlatılır.
        var start = cycle.IndexOf(cycle.Min());
        cycle = [.. cycle.Skip(start), .. cycle.Take(start)];
        cycle.Add(cycle[0]);
        return cycle;
    }
}
