using ERP.Konfeksiyon.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Konfeksiyon.Domain.Balancing;
using ERP.Konfeksiyon.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ERP.Konfeksiyon.Application.Features.ProductionLines;

public sealed class LineBalanceQuery
{
    /// <summary>Dengelenecek model; aktif rotası kullanılır.</summary>
    public int ModelId { get; set; }

    /// <summary>Verilirse yalnızca bu bantın makineleri sayılır; verilmezse fabrikanın tüm aktif makineleri.</summary>
    public int? ProductionLineId { get; set; }

    /// <summary>Hedef: saatte hattan çıkması istenen adet.</summary>
    public decimal TargetPerHour { get; set; }

    /// <summary>Beklenen verim (%100 = operatörler tam standart sürede çalışır).</summary>
    public decimal EfficiencyPercent { get; set; } = 100;
}

public sealed record BalanceOperatorDto(int Id, string Code, string Name);

/// <summary>Bir rota adımının dengesi. Hesap tanımları için bkz. <see cref="LineBalancer"/>.</summary>
/// <param name="Workload">Hedef için gereken iş gücü (operatör cinsinden, kesirli).</param>
/// <param name="RequiredStations">Gereken istasyon (makine + operatör) sayısı.</param>
/// <param name="LoadPercent">Gereken istasyonların doluluk oranı.</param>
/// <param name="AvailableMachines">Bu operasyona ayarlı, çalışabilir makine sayısı.</param>
/// <param name="UnavailableMachines">Bu operasyona ayarlı ama arızalı / bakımdaki makine sayısı.</param>
/// <param name="SpareMachines">Uygun tipte olup bu rotanın hiçbir operasyonuna ayarlı olmayan makine sayısı.</param>
/// <param name="MachineGap">Eksik makine (artı) ya da fazla makine (eksi).</param>
/// <param name="QualifiedOperators">Bu operasyonda yetkin aktif operatör sayısı.</param>
/// <param name="SuggestedOperators">İstasyonlar için önerilen operatörler (her operatör tek istasyona önerilir).</param>
/// <param name="OperatorGap">Yetkin operatör bulunamayan istasyon sayısı.</param>
public sealed record LineBalanceOperationDto(
    int RouteOperationId,
    int SequenceNo,
    int OperationId,
    string OperationCode,
    string OperationName,
    string? MachineTypeName,
    decimal Sam,
    double Workload,
    int RequiredStations,
    double? LoadPercent,
    int AvailableMachines,
    int UnavailableMachines,
    int SpareMachines,
    int MachineGap,
    double? AvailableCapacityPerHour,
    int QualifiedOperators,
    IReadOnlyList<BalanceOperatorDto> SuggestedOperators,
    int OperatorGap,
    bool IsBottleneck,
    IReadOnlyList<string> Suggestions);

/// <param name="TaktMinutes">Hedef için bir parçanın hattan çıkması gereken aralık (dakika).</param>
/// <param name="TheoreticalOperators">Kusursuz dengede gereken operatör sayısı (iş yüklerinin toplamı).</param>
/// <param name="BalanceEfficiencyPercent">Teorik operatör ÷ gereken istasyon.</param>
/// <param name="AchievablePerHour">Eldeki makinelerle hattın saatte çıkarabileceği adet.</param>
/// <param name="TargetReachable">Eldeki makine ve operatörlerle hedef tutturulabilir mi.</param>
/// <param name="UnfilledStations">Yetkin operatör bulunamayan istasyonların toplamı.</param>
public sealed record LineBalanceDto(
    int ModelId,
    string ModelCode,
    string ModelName,
    int ProductionRouteId,
    int RouteVersion,
    int? ProductionLineId,
    string? LineCode,
    string? LineName,
    decimal TargetPerHour,
    decimal EfficiencyPercent,
    double TaktMinutes,
    decimal TotalSam,
    double TheoreticalOperators,
    int RequiredStations,
    double? BalanceEfficiencyPercent,
    double? AchievablePerHour,
    bool TargetReachable,
    string? BottleneckOperationName,
    int UnfilledStations,
    IReadOnlyList<LineBalanceOperationDto> Operations);

public interface ILineBalancingService
{
    /// <summary>
    /// Modelin aktif rotası için hedef adede göre gereken istasyonları, eldeki makine ve operatörlerle
    /// karşılaştırmasını ve önerileri hesaplar. Hiçbir şey kaydetmez.
    /// </summary>
    Task<Result<LineBalanceDto>> CalculateAsync(LineBalanceQuery query, CancellationToken cancellationToken);
}

/// <summary>
/// Bant dengeleme: hesabı <see cref="LineBalancer"/> ve <see cref="OperatorAssigner"/> yapar; burada rota,
/// makine ve operatör verisi toplanır ve sonuç önerilere çevrilir.
/// </summary>
public sealed class LineBalancingService(IKonfeksiyonDbContext db) : ILineBalancingService
{
    public const decimal MaxTargetPerHour = 100_000;
    public const decimal MinEfficiencyPercent = 1;
    public const decimal MaxEfficiencyPercent = 200;
    private const int MaxOperators = 2000;

    private static readonly MachineStatus[] OutOfService = [MachineStatus.Breakdown, MachineStatus.Maintenance];

    public async Task<Result<LineBalanceDto>> CalculateAsync(LineBalanceQuery query, CancellationToken cancellationToken)
    {
        if (query.TargetPerHour <= 0 || query.TargetPerHour > MaxTargetPerHour)
        {
            return Invalid("targetPerHour", $"Hedef 0'dan büyük ve en fazla {MaxTargetPerHour:N0} adet / saat olmalıdır.");
        }

        if (query.EfficiencyPercent < MinEfficiencyPercent || query.EfficiencyPercent > MaxEfficiencyPercent)
        {
            return Invalid(
                "efficiencyPercent", $"Verim %{MinEfficiencyPercent:0} ile %{MaxEfficiencyPercent:0} arasında olmalıdır.");
        }

        var model = await db.Models
            .AsNoTracking()
            .Where(m => m.Id == query.ModelId)
            .Select(m => new { m.Id, m.ModelCode, m.Name })
            .SingleOrDefaultAsync(cancellationToken);
        if (model is null)
        {
            return ReferenceValidation.InvalidReference("modelId", "Model bulunamadı.");
        }

        var route = await db.ProductionRoutes
            .AsNoTracking()
            .Where(r => r.ModelId == model.Id && r.Status == RouteStatus.Active)
            .Select(r => new { r.Id, r.Version })
            .SingleOrDefaultAsync(cancellationToken);
        if (route is null)
        {
            return new Error(
                ErrorCodes.RouteRequired, $"{model.ModelCode} modelinin aktif bir operasyon rotası yok; önce rota etkinleştirilmelidir.");
        }

        var line = query.ProductionLineId is { } lineId
            ? await db.ProductionLines
                .AsNoTracking()
                .Where(l => l.Id == lineId)
                .Select(l => new { l.Id, l.LineCode, l.Name })
                .SingleOrDefaultAsync(cancellationToken)
            : null;
        if (query.ProductionLineId is not null && line is null)
        {
            return ReferenceValidation.InvalidReference("productionLineId", "Bant bulunamadı.");
        }

        // Rotada kullanılan operasyon sonradan silinmiş olsa da adım adıyla görünür.
        var steps = await db.ProductionRouteOperations
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(o => o.ProductionRouteId == route.Id)
            .OrderBy(o => o.SequenceNo)
            .Select(o => new
            {
                o.Id,
                o.SequenceNo,
                o.OperationId,
                o.Operation.OperationCode,
                o.Operation.Name,
                o.Operation.MachineTypeId,
                MachineTypeName = o.Operation.MachineType == null ? null : o.Operation.MachineType.Name,
                o.Sam,
            })
            .ToListAsync(cancellationToken);
        var routeOperationIds = steps.Select(s => s.OperationId).ToHashSet();

        var machineQuery = db.Machines.AsNoTracking().Where(m => m.IsActive);
        if (line is not null)
        {
            machineQuery = machineQuery.Where(m => db.LineMachineAssignments.Any(
                a => a.MachineId == m.Id && a.ProductionLineId == line.Id && a.UnassignedAt == null));
        }

        var machines = await machineQuery
            .Select(m => new { m.MachineTypeId, m.OperationId, m.Status })
            .ToListAsync(cancellationToken);
        var usable = machines.Where(m => !OutOfService.Contains(m.Status)).ToList();

        var operators = await db.Operators
            .AsNoTracking()
            .Where(o => o.IsActive)
            .OrderBy(o => o.OperatorCode)
            .Take(MaxOperators)
            .Select(o => new { o.Id, o.OperatorCode, Name = o.FirstName + " " + o.LastName })
            .ToListAsync(cancellationToken);
        var operatorIds = operators.Select(o => o.Id).ToList();
        var qualifications = (await db.OperatorOperationQualifications
                .AsNoTracking()
                .Where(q => operatorIds.Contains(q.OperatorId) && routeOperationIds.Contains(q.OperationId))
                .Select(q => new { q.OperatorId, q.OperationId })
                .ToListAsync(cancellationToken))
            .ToLookup(q => q.OperatorId, q => q.OperationId);

        var balance = LineBalancer.Calculate(
            steps
                .Select(s => new BalanceInput(s.Id, s.Sam, usable.Count(m => m.OperationId == s.OperationId)))
                .ToList(),
            query.TargetPerHour,
            query.EfficiencyPercent);

        // Operatör önerisi: her operatör, yetkin olduğu operasyonların rota adımlarına aday.
        var suggested = OperatorAssigner.Assign(
            balance.Lines.Select(l => (l.Key, l.RequiredStations)).ToList(),
            operators
                .Where(o => qualifications[o.Id].Any())
                .Select(o =>
                {
                    var own = qualifications[o.Id].ToHashSet();
                    return (o.Id, (IReadOnlySet<int>)steps.Where(s => own.Contains(s.OperationId)).Select(s => s.Id).ToHashSet());
                })
                .ToList());
        var operatorsById = operators.ToDictionary(o => o.Id);

        var scope = line is null ? "Fabrikada" : "Bantta";
        var rows = steps
            .Zip(balance.Lines)
            .Select(pair =>
            {
                var (step, result) = pair;
                var unavailable = machines.Count(m => m.OperationId == step.OperationId && OutOfService.Contains(m.Status));
                var spare = usable.Count(m =>
                    (m.OperationId is null || !routeOperationIds.Contains(m.OperationId.Value))
                    && (step.MachineTypeId is null || m.MachineTypeId == step.MachineTypeId));
                var assigned = suggested[step.Id]
                    .Select(id => operatorsById[id])
                    .Select(o => new BalanceOperatorDto(o.Id, o.OperatorCode, o.Name))
                    .ToList();
                var operatorGap = result.RequiredStations - assigned.Count;

                return new LineBalanceOperationDto(
                    step.Id, step.SequenceNo, step.OperationId, step.OperationCode, step.Name, step.MachineTypeName, step.Sam,
                    result.Workload, result.RequiredStations, result.LoadPercent, result.AvailableMachines, unavailable, spare,
                    result.MachineGap, result.AvailableCapacityPerHour,
                    operators.Count(o => qualifications[o.Id].Contains(step.OperationId)),
                    assigned, operatorGap, result.Key == balance.BottleneckKey,
                    Suggest(scope, result, unavailable, spare, operatorGap));
            })
            .ToList();

        var unfilled = rows.Sum(r => Math.Max(r.OperatorGap, 0));
        return new LineBalanceDto(
            model.Id, model.ModelCode, model.Name, route.Id, route.Version, line?.Id, line?.LineCode, line?.Name,
            query.TargetPerHour, query.EfficiencyPercent, balance.TaktMinutes, balance.TotalSam, balance.TheoreticalOperators,
            balance.RequiredStations, balance.BalanceEfficiencyPercent, balance.AchievablePerHour,
            rows.All(r => r.MachineGap <= 0) && unfilled == 0,
            rows.FirstOrDefault(r => r.IsBottleneck)?.OperationName,
            unfilled, rows);
    }

    /// <summary>Operasyonun dengesinden çıkan öneriler (spec: "1 makine daha ekle", "1 operatör daha ata").</summary>
    private static List<string> Suggest(string scope, BalanceLine line, int unavailable, int spare, int operatorGap)
    {
        var suggestions = new List<string>();

        if (line.MachineGap > 0)
        {
            suggestions.Add(spare > 0
                ? $"{line.MachineGap} makine eksik. {scope} bu operasyona ayarlanabilecek {spare} boş makine var."
                : $"{line.MachineGap} makine daha ekleyin.");
        }
        else if (line.MachineGap < 0)
        {
            suggestions.Add($"{-line.MachineGap} makine fazla; başka bir operasyona kaydırılabilir.");
        }

        if (unavailable > 0)
        {
            suggestions.Add($"{unavailable} makine arızalı ya da bakımda; hesaba katılmadı.");
        }

        if (operatorGap > 0)
        {
            suggestions.Add($"{operatorGap} istasyon için yetkin operatör yok; operatör yetiştirin ya da yetkinlik tanımlayın.");
        }

        if (line is { RequiredStations: > 0, LoadPercent: <= 50 })
        {
            suggestions.Add("İstasyon en fazla yarı dolu; operatör başka bir operasyonla paylaştırılabilir.");
        }

        return suggestions;
    }

    private static Error Invalid(string field, string message) => new(
        ErrorCodes.ValidationError, message, ErrorType.Validation, new Dictionary<string, string[]> { [field] = [message] });
}
