using ERP.Boyahane.Domain.Entities;
using ERP.Boyahane.Domain.Enums;

namespace ERP.Boyahane.Application.Features.Batches;

/// <summary>Partinin hangi makinede işlenebileceği: parti açılırken ve kapasite planında makine değiştirilirken aynı kural.</summary>
public static class BoyahaneMachineFit
{
    /// <summary>
    /// Partinin asıl operasyonu: reçeteli (boyama) partide ilk boyama adımı, diğerlerinde ilk makine adımı. Partinin makinesi bu
    /// operasyonu yapabilmelidir; rotada böyle adım yoksa boş döner ve makine kısıtı yoktur.
    /// </summary>
    public static BoyahaneOperation? MainOperation(IEnumerable<(int Sequence, BoyahaneOperation Operation)> steps, bool usesRecipe) =>
        steps
            .Where(x => usesRecipe ? x.Operation.Behavior == OperationBehavior.Dyeing : x.Operation.NeedsMachine)
            .OrderBy(x => x.Sequence)
            .Select(x => x.Operation)
            .FirstOrDefault();

    /// <summary>Kapasite aşımı uyarısı; parti açmadaki metinle aynıdır.</summary>
    public static string OverCapacityMessage(decimal totalKilograms, decimal capacityKg, bool canOverride) =>
        canOverride
            ? $"Parti {totalKilograms:0.##} kg, makine kapasitesi {capacityKg:0.##} kg. Devam etmek için gerekçe yazın."
            : $"Parti {totalKilograms:0.##} kg, makine kapasitesi {capacityKg:0.##} kg. Aşım için yetkili onayı gerekir.";

    /// <summary>
    /// Makine gerektiren rota adımlarının standart süreleri toplamı (dakika). Makinenin operasyonları verilirse yalnızca o makinede
    /// yapılan adımlar sayılır (örneğin ram adımı boya makinesinin süresine girmez). Hiçbir adımda standart süre yoksa boş döner;
    /// o zaman işlem türünün varsayılan süresi kullanılır.
    /// </summary>
    public static decimal? StandardMinutes(IEnumerable<(int OperationId, bool NeedsMachine, decimal? StandardMinutes)> steps, IReadOnlySet<int>? machineOperations)
    {
        var timed = steps
            .Where(s => s.NeedsMachine && s.StandardMinutes is > 0 && (machineOperations is null || machineOperations.Contains(s.OperationId)))
            .ToList();
        return timed.Count == 0 ? null : timed.Sum(s => s.StandardMinutes!.Value);
    }
}
