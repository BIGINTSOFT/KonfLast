namespace ERP.Konfeksiyon.Domain.Balancing;

/// <param name="Key">Operasyonu tanıyan anahtar (rota adımı).</param>
/// <param name="Sam">Standart süre (dakika / adet).</param>
/// <param name="AvailableMachines">Bu operasyonu yapmaya ayarlı, kullanılabilir makine sayısı.</param>
public sealed record BalanceInput(int Key, decimal Sam, int AvailableMachines);

/// <param name="Workload">Hedefi tutturmak için gereken iş gücü, operatör cinsinden (kesirli).</param>
/// <param name="RequiredStations">Gereken istasyon (makine + operatör) sayısı: iş yükünün yukarı yuvarlanmışı.</param>
/// <param name="LoadPercent">Gereken istasyonların doluluk oranı; %100'e yakın olan boşluksuz çalışır.</param>
/// <param name="MachineGap">Eksik makine (artı) ya da fazla makine (eksi).</param>
/// <param name="AvailableCapacityPerHour">Eldeki makinelerle saatte yapılabilecek adet; süresi sıfır operasyonda null.</param>
public sealed record BalanceLine(
    int Key,
    decimal Sam,
    double Workload,
    int RequiredStations,
    double? LoadPercent,
    int AvailableMachines,
    int MachineGap,
    double? AvailableCapacityPerHour);

/// <param name="TaktMinutes">Hedefi tutturmak için bir parçanın hattan çıkması gereken aralık (dakika).</param>
/// <param name="TheoreticalOperators">İş yüklerinin toplamı: kusursuz dengede gereken operatör sayısı.</param>
/// <param name="BalanceEfficiencyPercent">Teorik operatör ÷ gereken istasyon; yuvarlamadan doğan boş kapasiteyi gösterir.</param>
/// <param name="AchievablePerHour">Eldeki makinelerle hattın saatte çıkarabileceği adet (en yavaş operasyon belirler).</param>
/// <param name="BottleneckKey">Hattı sınırlayan operasyon.</param>
public sealed record BalanceResult(
    double TaktMinutes,
    decimal TotalSam,
    double TheoreticalOperators,
    int RequiredStations,
    double? BalanceEfficiencyPercent,
    double? AchievablePerHour,
    int? BottleneckKey,
    IReadOnlyList<BalanceLine> Lines);

/// <summary>
/// Bant dengeleme hesabı:
///
///   iş yükü          = hedef (adet / saat) × SAM ÷ 60 ÷ verim
///   gereken istasyon = ⌈iş yükü⌉
///   yük              = iş yükü ÷ gereken istasyon
///   eldeki kapasite  = makine sayısı × 60 ÷ SAM × verim
///   ulaşılabilir     = operasyonların eldeki kapasitelerinin en küçüğü (darboğaz)
///
/// Verim, operatörlerin standart sürenin ne kadarını tutturduğu beklentisidir (%100 = tam SAM).
/// </summary>
public static class LineBalancer
{
    /// <summary>Yuvarlama hatası yüzünden tam sayıya çok yakın iş yükü bir üst istasyona taşmasın.</summary>
    private const double Tolerance = 1e-9;

    public static BalanceResult Calculate(IReadOnlyList<BalanceInput> operations, decimal targetPerHour, decimal efficiencyPercent)
    {
        if (targetPerHour <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(targetPerHour), "Hedef sıfırdan büyük olmalıdır.");
        }

        if (efficiencyPercent <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(efficiencyPercent), "Verim sıfırdan büyük olmalıdır.");
        }

        var efficiency = (double)efficiencyPercent / 100;
        var target = (double)targetPerHour;

        var lines = operations
            .Select(o =>
            {
                var sam = (double)o.Sam;
                var workload = target * sam / 60 / efficiency;
                var required = (int)Math.Ceiling(workload - Tolerance);
                return new BalanceLine(
                    o.Key,
                    o.Sam,
                    Round(workload, 2),
                    required,
                    required > 0 ? Round(workload / required * 100, 1) : null,
                    o.AvailableMachines,
                    required - o.AvailableMachines,
                    sam > 0 ? Round(o.AvailableMachines * 60 / sam * efficiency, 1) : null);
            })
            .ToList();

        var theoretical = lines.Sum(l => l.Workload);
        var stations = lines.Sum(l => l.RequiredStations);

        // Darboğaz: eldeki kapasitesi en düşük operasyon (eşitlikte rotada önce gelen).
        var bottleneck = lines.Where(l => l.AvailableCapacityPerHour is not null).MinBy(l => l.AvailableCapacityPerHour);

        return new BalanceResult(
            Round(60 / target, 3),
            operations.Sum(o => o.Sam),
            Round(theoretical, 2),
            stations,
            stations > 0 ? Round(theoretical / stations * 100, 1) : null,
            bottleneck?.AvailableCapacityPerHour,
            bottleneck?.Key,
            lines);
    }

    private static double Round(double value, int digits) => Math.Round(value, digits, MidpointRounding.AwayFromZero);
}

/// <summary>
/// İstasyonlara operatör önerir: her istasyona, o operasyonda yetkin bir operatör; bir operatör tek istasyona.
/// En fazla istasyonu dolduran eşleştirme bulunur (artıran yol yöntemi), yani bir operatörü yanlış yere koyup
/// başka bir istasyonu boş bırakmaz.
/// </summary>
public static class OperatorAssigner
{
    /// <param name="stations">Operasyon anahtarı → doldurulacak istasyon sayısı.</param>
    /// <param name="qualifications">Operatör → yapabildiği operasyonlar. Sıra, eşit seçeneklerde önceliği belirler.</param>
    /// <returns>Operasyon anahtarı → önerilen operatörler (istasyon sayısından az olabilir).</returns>
    public static IReadOnlyDictionary<int, IReadOnlyList<int>> Assign(
        IReadOnlyList<(int Key, int Stations)> stations,
        IReadOnlyList<(int OperatorId, IReadOnlySet<int> Operations)> qualifications)
    {
        // Yetkin operatörü az olan operasyon önce doldurulur; sonuç böylece kararlı ve öngörülebilir olur.
        var slots = stations
            .OrderBy(s => qualifications.Count(q => q.Operations.Contains(s.Key)))
            .SelectMany(s => Enumerable.Repeat(s.Key, Math.Max(s.Stations, 0)))
            .ToList();

        var operatorOfSlot = new int?[slots.Count];
        var slotOfOperator = new Dictionary<int, int>();

        for (var slot = 0; slot < slots.Count; slot++)
        {
            TryFill(slot, []);
        }

        return stations.ToDictionary(
            s => s.Key,
            s => (IReadOnlyList<int>)Enumerable.Range(0, slots.Count)
                .Where(i => slots[i] == s.Key && operatorOfSlot[i] is not null)
                .Select(i => operatorOfSlot[i]!.Value)
                .ToList());

        bool TryFill(int slot, HashSet<int> visited)
        {
            foreach (var (operatorId, operations) in qualifications)
            {
                if (!operations.Contains(slots[slot]) || !visited.Add(operatorId))
                {
                    continue;
                }

                // Operatör boştaysa ya da şu anki istasyonu başka birine devredilebiliyorsa bu istasyona alınır.
                if (!slotOfOperator.TryGetValue(operatorId, out var current) || TryFill(current, visited))
                {
                    operatorOfSlot[slot] = operatorId;
                    slotOfOperator[operatorId] = slot;
                    return true;
                }
            }

            return false;
        }
    }
}
