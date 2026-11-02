using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Core.Application.Features.MasterData;
using ERP.Core.Domain.Enums;
using ERP.Orme.Application.Features.Floor;
using ERP.Orme.Application.Features.Receipts;
using ERP.Orme.Domain.Entities;
using ERP.Orme.Domain.Enums;
using ERP.Orme.Domain.Rules;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using static ERP.Core.Application.Features.MasterData.MasterDataRules;

namespace ERP.Orme.Application.Features.Knitting;

public sealed class OrmeKnitOrderQuery : PageRequest
{
    /// <summary>Emir no, sipariş no, kumaş ya da müşteride aranır.</summary>
    public string? Search { get; set; }
}

public sealed class OrmeFabricRollQuery : PageRequest
{
    /// <summary>Top no, emir no, makine, sipariş no ya da kumaşta aranır.</summary>
    public string? Search { get; set; }
}

public sealed record OrmeKnitOrderListItemDto(
    int Id,
    string KnitOrderNo,
    DateOnly PlannedDate,
    int OrderId,
    string OrderNo,
    string? CustomerName,
    string FabricCode,
    string FabricName,
    KnitBedType BedType,
    decimal PlannedKilograms,
    decimal KnittedKilograms,
    int RollCount,
    int RunningMachineCount,
    decimal IssuedKilograms,
    KnitOrderStatus Status);

/// <param name="RequiredKilograms">Planlanan kilonun bu iplikten fireli ihtiyacı.</param>
/// <param name="IssuedKilograms">Emre verilen net iplik (geri alınan düşülmüş).</param>
public sealed record OrmeKnitOrderYarnDto(int YarnId, string YarnCode, string YarnName, string? YarnColor, decimal Percent, decimal RequiredKilograms, decimal IssuedKilograms);

/// <param name="FitWarning">Çap ya da fine kumaş kartıyla uyuşmuyorsa uyarı; engel değildir.</param>
public sealed record OrmeKnitOrderMachineDto(
    int Id,
    int MachineId,
    string MachineCode,
    string MachineName,
    int DiameterInch,
    int Gauge,
    DateOnly AssignedOn,
    DateOnly? ReleasedOn,
    string? FitWarning,
    decimal KnittedKilograms,
    int RollCount);

public sealed record OrmeYarnIssueDto(int Id, int YarnLotId, string LotNo, string? ProducerLotNo, string YarnCode, string YarnName, DateOnly IssuedOn, decimal Kilograms);

public sealed record OrmeYarnTakeBackDto(int Id, int YarnLotId, string LotNo, string YarnCode, string YarnName, DateOnly TakenOn, decimal Kilograms, string? Notes);

/// <summary>Emirde kullanılan lot: toplar tek tek lota bağlanmadığı için izleme bu listeden yapılır.</summary>
/// <param name="NetKilograms">Verilen − geri alınan; geri alınabilecek üst sınır.</param>
public sealed record OrmeKnitOrderLotDto(int YarnLotId, string LotNo, string? ProducerLotNo, string YarnCode, string YarnName, decimal IssuedKilograms, decimal TakenBackKilograms, decimal NetKilograms);

/// <param name="Meters">Girilmişse ölçülen metre.</param>
/// <param name="EstimatedMeters">Metre girilmediyse gramaj ve enden tahmin; en bilinmiyorsa boş.</param>
/// <param name="InspectedMeters">Kontrolde puana esas metre.</param>
/// <param name="InspectedMetersEstimated">Kontrolde metre ölçülmedi, tahmin kullanıldı.</param>
/// <param name="SpecDeviation">En ve gramajın karttan sapması; uyarıdır, kaliteyi değiştirmez.</param>
/// <param name="OverrideNote">Dolu ise kalite yetkili kararıyla değişti.</param>
/// <param name="CutByName">Topu tabletten kaydeden personel; ofiste girilende boş.</param>
public sealed record OrmeFabricRollDto(
    int Id,
    string RollNo,
    int KnitOrderId,
    string KnitOrderNo,
    int MachineId,
    string MachineCode,
    int OrderId,
    string OrderNo,
    string? CustomerName,
    string FabricCode,
    string FabricName,
    DateOnly CutOn,
    decimal Kilograms,
    decimal? Meters,
    decimal? EstimatedMeters,
    FabricRollStatus Status,
    string? Notes,
    string? CutByName,
    DateOnly? InspectedOn,
    decimal? InspectedMeters,
    bool InspectedMetersEstimated,
    decimal? InspectedWidthCm,
    decimal? InspectedWeightGsm,
    decimal? PointsPer100M2,
    string? SpecDeviation,
    string? OverrideNote);

/// <param name="MixedProducerLotYarns">Aynı ipliğin birden çok üretici lotundan verildiği iplikler (renk farkı riski).</param>
/// <param name="CustomerFabricCode">Müşterinin sipariş satırındaki kendi kumaş kodu; etikette basılır.</param>
/// <param name="DocumentLanguage">Etiket dili: siparişin müşterisinden, stoğa üretimde Türkçe.</param>
public sealed record OrmeKnitOrderDto(
    int Id,
    string KnitOrderNo,
    int OrderId,
    string OrderNo,
    int OrderLineId,
    string? CustomerName,
    OrderOwnership Ownership,
    decimal OrderLineKilograms,
    string FabricCode,
    string FabricName,
    KnitBedType BedType,
    int? DiameterInch,
    int? Gauge,
    DateOnly PlannedDate,
    decimal PlannedKilograms,
    decimal KnittedKilograms,
    decimal RemainingKilograms,
    DateOnly? CompletedOn,
    string? Notes,
    KnitOrderStatus Status,
    IReadOnlyList<OrmeKnitOrderYarnDto> Yarns,
    IReadOnlyList<OrmeKnitOrderMachineDto> Machines,
    IReadOnlyList<OrmeYarnIssueDto> Issues,
    IReadOnlyList<OrmeYarnTakeBackDto> TakeBacks,
    IReadOnlyList<OrmeKnitOrderLotDto> Lots,
    IReadOnlyList<string> MixedProducerLotYarns,
    IReadOnlyList<OrmeFabricRollDto> Rolls,
    string? CustomerOrderNo,
    string? CustomerFabricCode,
    DocumentLanguage DocumentLanguage);

/// <param name="BusyKnitOrderNo">Makinede çalışan başka emir; doluysa atanamaz.</param>
public sealed record OrmeMachineOptionDto(int Id, string Code, string Name, int DiameterInch, int Gauge, string? BusyKnitOrderNo, string? FitWarning);

public sealed record OrmeIssuableLotDto(int Id, string LotNo, DateOnly ReceiptDate, int YarnId, string YarnCode, string YarnName, string? ProducerLotNo, decimal RemainingKilograms);

/// <param name="KnittedKilograms">Emrin tüm makinelerde örülen toplamı.</param>
/// <param name="MachineKilograms">Emrin bu makinede örülen kısmı.</param>
/// <param name="RemainingKilograms">Plandan kalan; plan aşıldıysa sıfır.</param>
/// <param name="OpenStopCategory">Makine duruyorsa duruşun türü.</param>
public sealed record OrmeMachineBoardItemDto(
    int MachineId,
    string MachineCode,
    string MachineName,
    KnitBedType BedType,
    int DiameterInch,
    int Gauge,
    int? KnitOrderId,
    string? KnitOrderNo,
    string? OrderNo,
    string? CustomerName,
    string? FabricCode,
    string? FabricName,
    DateOnly? AssignedOn,
    decimal? PlannedKilograms,
    decimal? KnittedKilograms,
    decimal? MachineKilograms,
    decimal? RemainingKilograms,
    int RollCount,
    string? FitWarning,
    OrmeStopCategory? OpenStopCategory,
    string? OpenStopReason,
    DateTime? OpenStopSince);

public sealed record CreateOrmeKnitOrderRequest(int OrderLineId, DateOnly PlannedDate, decimal PlannedKilograms, string? Notes = null);

public sealed record UpdateOrmeKnitOrderRequest(DateOnly PlannedDate, decimal PlannedKilograms, string? Notes = null);

public sealed record AssignOrmeMachineRequest(int MachineId, DateOnly AssignedOn);

public sealed record OrmeYarnIssueRequest(int YarnLotId, decimal Kilograms, DateOnly IssuedOn);

public sealed record OrmeYarnTakeBackRequest(int YarnLotId, decimal Kilograms, DateOnly TakenOn, string? Notes = null);

/// <param name="Kilograms">Makine başında tartılan kilo; zorunludur.</param>
/// <param name="Meters">Ölçüldüyse metre; boşsa gramaj ve enden tahmin edilir.</param>
public sealed record OrmeFabricRollRequest(int MachineId, DateOnly CutOn, decimal Kilograms, decimal? Meters = null, string? Notes = null);

public sealed class CreateOrmeKnitOrderRequestValidator : AbstractValidator<CreateOrmeKnitOrderRequest>
{
    public CreateOrmeKnitOrderRequestValidator()
    {
        RuleFor(x => x.OrderLineId).GreaterThan(0).WithMessage("Sipariş satırı seçilmelidir.");
        RuleFor(x => x.PlannedDate).NotEmpty().WithMessage("Plan tarihi zorunludur.");
        RuleFor(x => x.PlannedKilograms)
            .GreaterThan(0).WithMessage("Plan kilosu sıfırdan büyük olmalıdır.")
            .LessThanOrEqualTo(9_999_999).WithMessage("Plan kilosu çok büyük.")
            .PrecisionScale(12, 2, true).WithMessage("Kilo en fazla iki ondalık olabilir.");
        RuleFor(x => x.Notes).OptionalText(1000);
    }
}

public sealed class UpdateOrmeKnitOrderRequestValidator : AbstractValidator<UpdateOrmeKnitOrderRequest>
{
    public UpdateOrmeKnitOrderRequestValidator()
    {
        RuleFor(x => x.PlannedDate).NotEmpty().WithMessage("Plan tarihi zorunludur.");
        RuleFor(x => x.PlannedKilograms)
            .GreaterThan(0).WithMessage("Plan kilosu sıfırdan büyük olmalıdır.")
            .LessThanOrEqualTo(9_999_999).WithMessage("Plan kilosu çok büyük.")
            .PrecisionScale(12, 2, true).WithMessage("Kilo en fazla iki ondalık olabilir.");
        RuleFor(x => x.Notes).OptionalText(1000);
    }
}

public sealed class AssignOrmeMachineRequestValidator : AbstractValidator<AssignOrmeMachineRequest>
{
    public AssignOrmeMachineRequestValidator()
    {
        RuleFor(x => x.MachineId).GreaterThan(0).WithMessage("Makine seçilmelidir.");
        RuleFor(x => x.AssignedOn).NotEmpty().WithMessage("Tarih zorunludur.");
    }
}

public sealed class OrmeYarnIssueRequestValidator : AbstractValidator<OrmeYarnIssueRequest>
{
    public OrmeYarnIssueRequestValidator()
    {
        RuleFor(x => x.YarnLotId).GreaterThan(0).WithMessage("İplik lotu seçilmelidir.");
        RuleFor(x => x.IssuedOn).NotEmpty().WithMessage("Çıkış tarihi zorunludur.");
        RuleFor(x => x.Kilograms)
            .GreaterThan(0).WithMessage("Kilo sıfırdan büyük olmalıdır.")
            .PrecisionScale(12, 2, true).WithMessage("Kilo en fazla iki ondalık olabilir.");
    }
}

public sealed class OrmeYarnTakeBackRequestValidator : AbstractValidator<OrmeYarnTakeBackRequest>
{
    public OrmeYarnTakeBackRequestValidator()
    {
        RuleFor(x => x.YarnLotId).GreaterThan(0).WithMessage("İplik lotu seçilmelidir.");
        RuleFor(x => x.TakenOn).NotEmpty().WithMessage("Tarih zorunludur.");
        RuleFor(x => x.Kilograms)
            .GreaterThan(0).WithMessage("Kilo sıfırdan büyük olmalıdır.")
            .PrecisionScale(12, 2, true).WithMessage("Kilo en fazla iki ondalık olabilir.");
        RuleFor(x => x.Notes).OptionalText(500);
    }
}

public sealed class OrmeFabricRollRequestValidator : AbstractValidator<OrmeFabricRollRequest>
{
    public OrmeFabricRollRequestValidator()
    {
        RuleFor(x => x.MachineId).GreaterThan(0).WithMessage("Makine seçilmelidir.");
        RuleFor(x => x.CutOn).NotEmpty().WithMessage("Tarih zorunludur.");
        RuleFor(x => x.Kilograms)
            .GreaterThan(0).WithMessage("Topun kilosu tartılıp girilmelidir.")
            .LessThanOrEqualTo(99_999).WithMessage("Kilo çok büyük.")
            .PrecisionScale(12, 2, true).WithMessage("Kilo en fazla iki ondalık olabilir.");
        RuleFor(x => x.Meters)
            .GreaterThan(0).WithMessage("Metre sıfırdan büyük olmalıdır.")
            .PrecisionScale(12, 2, true).WithMessage("Metre en fazla iki ondalık olabilir.")
            .When(x => x.Meters is not null);
        RuleFor(x => x.Notes).OptionalText(500);
    }
}

public interface IOrmeKnitOrderService
{
    Task<PagedResult<OrmeKnitOrderListItemDto>> GetListAsync(OrmeKnitOrderQuery query, CancellationToken cancellationToken);
    Task<Result<OrmeKnitOrderDto>> GetAsync(int id, CancellationToken cancellationToken);
    Task<Result<OrmeKnitOrderDto>> CreateAsync(CreateOrmeKnitOrderRequest request, CancellationToken cancellationToken);
    Task<Result<OrmeKnitOrderDto>> UpdateAsync(int id, UpdateOrmeKnitOrderRequest request, CancellationToken cancellationToken);
    Task<Result<OrmeKnitOrderDto>> CompleteAsync(int id, CancellationToken cancellationToken);
    Task<Result<OrmeKnitOrderDto>> ReopenAsync(int id, CancellationToken cancellationToken);
    Task<Result<OrmeKnitOrderDto>> CancelAsync(int id, CancellationToken cancellationToken);
    Task<Result<IReadOnlyList<OrmeMachineOptionDto>>> GetMachineOptionsAsync(int id, CancellationToken cancellationToken);
    Task<Result<OrmeKnitOrderDto>> AssignMachineAsync(int id, AssignOrmeMachineRequest request, CancellationToken cancellationToken);
    Task<Result<OrmeKnitOrderDto>> ReleaseMachineAsync(int id, int assignmentId, CancellationToken cancellationToken);
    Task<Result<IReadOnlyList<OrmeIssuableLotDto>>> GetIssuableLotsAsync(int id, CancellationToken cancellationToken);
    Task<Result<OrmeKnitOrderDto>> AddIssueAsync(int id, OrmeYarnIssueRequest request, CancellationToken cancellationToken);
    Task<Result<OrmeKnitOrderDto>> RemoveIssueAsync(int id, int issueId, CancellationToken cancellationToken);
    Task<Result<OrmeKnitOrderDto>> AddTakeBackAsync(int id, OrmeYarnTakeBackRequest request, CancellationToken cancellationToken);
    Task<Result<OrmeKnitOrderDto>> RemoveTakeBackAsync(int id, int takeBackId, CancellationToken cancellationToken);
    Task<Result<OrmeKnitOrderDto>> AddRollAsync(int id, OrmeFabricRollRequest request, CancellationToken cancellationToken);
    Task<Result<OrmeTabletRollDto>> AddRollFromTabletAsync(int id, OrmeTabletRollRequest request, int operatorId, CancellationToken cancellationToken);
    Task<Result<OrmeKnitOrderDto>> RemoveRollAsync(int id, int rollId, CancellationToken cancellationToken);
    Task<PagedResult<OrmeFabricRollDto>> GetRollsAsync(OrmeFabricRollQuery query, CancellationToken cancellationToken);
    Task<IReadOnlyList<OrmeMachineBoardItemDto>> GetBoardAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Örme emri: onaylı sipariş satırından açılır, aynı plakadaki makinelere atanır (bir makinede aynı anda tek emir), iplik
/// lottan verilir, artan iplik geri alınır, makineden çıkan toplar kilo tartılarak kaydedilir.
/// </summary>
public sealed class OrmeKnitOrderService(IAppDbContext db, IDocumentNumberGenerator numbers, IAppLock appLock) : IOrmeKnitOrderService
{
    private const string Series = "OrmeKnitOrder";
    private const string RollSeries = "OrmeFabricRoll";
    private const string MachineLock = "OrmeMachines";
    private const string InvalidState = "ORME_KNIT_ORDER_INVALID_STATE";
    private static readonly TimeSpan LockTimeout = TimeSpan.FromSeconds(10);

    private static readonly Error NotFound = new(ErrorCodes.NotFound, "Örme emri bulunamadı.", ErrorType.NotFound);
    private static readonly Error Busy = new("ORME_BUSY", "Aynı anda başka bir işlem yapılıyor; tekrar deneyin.", ErrorType.Conflict);
    private static readonly Error NotOpen = new(InvalidState, "Emir açık değil; bu işlem yapılamaz.", ErrorType.Conflict);

    public Task<PagedResult<OrmeKnitOrderListItemDto>> GetListAsync(OrmeKnitOrderQuery query, CancellationToken cancellationToken)
    {
        var orders = db.Set<OrmeKnitOrder>().AsNoTracking();
        if (NullIfBlank(query.Search) is { } term)
        {
            orders = orders.Where(k => k.KnitOrderNo.Contains(term)
                || k.OrderLine.Order.OrderNo.Contains(term)
                || k.OrderLine.Fabric.FabricCode.Contains(term)
                || k.OrderLine.Fabric.Name.Contains(term)
                || (k.OrderLine.Order.Customer != null && k.OrderLine.Order.Customer.Name.Contains(term)));
        }

        return orders
            .OrderByDescending(k => k.PlannedDate).ThenByDescending(k => k.Id)
            .Select(k => new OrmeKnitOrderListItemDto(
                k.Id,
                k.KnitOrderNo,
                k.PlannedDate,
                k.OrderLine.OrderId,
                k.OrderLine.Order.OrderNo,
                k.OrderLine.Order.Customer != null ? k.OrderLine.Order.Customer.Name : null,
                k.OrderLine.Fabric.FabricCode,
                k.OrderLine.Fabric.Name,
                k.OrderLine.Fabric.BedType,
                k.PlannedKilograms,
                k.Rolls.Sum(r => (decimal?)r.Kilograms) ?? 0,
                k.Rolls.Count,
                k.Machines.Count(m => m.ReleasedOn == null),
                (k.Issues.Sum(i => (decimal?)i.Kilograms) ?? 0) - (k.TakeBacks.Sum(t => (decimal?)t.Kilograms) ?? 0),
                k.Status))
            .ToPagedResultAsync(query, cancellationToken);
    }

    public async Task<Result<OrmeKnitOrderDto>> GetAsync(int id, CancellationToken cancellationToken)
    {
        var knit = await db.Set<OrmeKnitOrder>().AsNoTracking()
            .Include(k => k.OrderLine).ThenInclude(l => l.Order).ThenInclude(o => o.Customer)
            .Include(k => k.OrderLine).ThenInclude(l => l.Fabric).ThenInclude(f => f.Yarns).ThenInclude(y => y.Yarn)
            .Include(k => k.Machines).ThenInclude(m => m.Machine)
            .Include(k => k.Issues).ThenInclude(i => i.YarnLot).ThenInclude(l => l.Yarn)
            .Include(k => k.TakeBacks).ThenInclude(t => t.YarnLot).ThenInclude(l => l.Yarn)
            .SingleOrDefaultAsync(k => k.Id == id, cancellationToken);
        if (knit is null)
        {
            return NotFound;
        }

        var fabric = knit.OrderLine.Fabric;
        var rolls = await ProjectRolls(db.Set<OrmeFabricRoll>().AsNoTracking().Where(r => r.KnitOrderId == id).OrderBy(r => r.Id)).ToListAsync(cancellationToken);
        var knitted = rolls.Sum(r => r.Kilograms);

        var issuedByYarn = knit.Issues.GroupBy(i => i.YarnLot.YarnId).ToDictionary(g => g.Key, g => g.Sum(i => i.Kilograms));
        var takenByYarn = knit.TakeBacks.GroupBy(t => t.YarnLot.YarnId).ToDictionary(g => g.Key, g => g.Sum(t => t.Kilograms));
        var yarns = fabric.Yarns.OrderBy(y => y.Sequence).Select(y => new OrmeKnitOrderYarnDto(
            y.YarnId,
            y.Yarn.YarnCode,
            y.Yarn.Name,
            y.Yarn.Color,
            y.Percent,
            Math.Round(YarnRequirement.WithWaste(YarnRequirement.NetKilograms(knit.PlannedKilograms, y.Percent), fabric.WastePercent), 2, MidpointRounding.AwayFromZero),
            issuedByYarn.GetValueOrDefault(y.YarnId) - takenByYarn.GetValueOrDefault(y.YarnId))).ToList();

        var machines = knit.Machines
            .OrderBy(m => m.ReleasedOn is null ? 0 : 1).ThenBy(m => m.Machine.Code).ThenBy(m => m.Id)
            .Select(m =>
            {
                var own = rolls.Where(r => r.MachineId == m.MachineId).ToList();
                return new OrmeKnitOrderMachineDto(
                    m.Id, m.MachineId, m.Machine.Code, m.Machine.Name, m.Machine.DiameterInch, m.Machine.Gauge, m.AssignedOn, m.ReleasedOn,
                    FitWarning(fabric.DiameterInch, fabric.Gauge, m.Machine), own.Sum(r => r.Kilograms), own.Count);
            })
            .ToList();

        var issues = knit.Issues.OrderBy(i => i.IssuedOn).ThenBy(i => i.Id).Select(i => new OrmeYarnIssueDto(
            i.Id, i.YarnLotId, i.YarnLot.LotNo, i.YarnLot.ProducerLotNo, i.YarnLot.Yarn.YarnCode, i.YarnLot.Yarn.Name, i.IssuedOn, i.Kilograms)).ToList();
        var takeBacks = knit.TakeBacks.OrderBy(t => t.TakenOn).ThenBy(t => t.Id).Select(t => new OrmeYarnTakeBackDto(
            t.Id, t.YarnLotId, t.YarnLot.LotNo, t.YarnLot.Yarn.YarnCode, t.YarnLot.Yarn.Name, t.TakenOn, t.Kilograms, t.Notes)).ToList();

        var lots = knit.Issues
            .GroupBy(i => i.YarnLotId)
            .Select(g =>
            {
                var lot = g.First().YarnLot;
                var issued = g.Sum(i => i.Kilograms);
                var taken = knit.TakeBacks.Where(t => t.YarnLotId == g.Key).Sum(t => t.Kilograms);
                return new OrmeKnitOrderLotDto(g.Key, lot.LotNo, lot.ProducerLotNo, lot.Yarn.YarnCode, lot.Yarn.Name, issued, taken, issued - taken);
            })
            .OrderBy(l => l.YarnCode).ThenBy(l => l.LotNo)
            .ToList();

        var mixed = knit.Issues
            .GroupBy(i => i.YarnLot.Yarn.YarnCode)
            .Where(g => g.Select(i => NullIfBlank(i.YarnLot.ProducerLotNo) ?? i.YarnLot.LotNo).Distinct().Count() > 1)
            .Select(g => g.Key)
            .OrderBy(code => code)
            .ToList();

        var order = knit.OrderLine.Order;
        return new OrmeKnitOrderDto(
            knit.Id,
            knit.KnitOrderNo,
            order.Id,
            order.OrderNo,
            knit.OrderLineId,
            order.Customer?.Name,
            order.Ownership,
            knit.OrderLine.Kilograms,
            fabric.FabricCode,
            fabric.Name,
            fabric.BedType,
            fabric.DiameterInch,
            fabric.Gauge,
            knit.PlannedDate,
            knit.PlannedKilograms,
            knitted,
            Math.Max(0, knit.PlannedKilograms - knitted),
            knit.CompletedOn,
            knit.Notes,
            knit.Status,
            yarns,
            machines,
            issues,
            takeBacks,
            lots,
            mixed,
            rolls,
            order.Customer is null ? null : order.CustomerOrderNo,
            order.Customer is null ? null : knit.OrderLine.CustomerFabricCode,
            order.Customer?.DocumentLanguage ?? DocumentLanguage.Turkish);
    }

    public async Task<Result<OrmeKnitOrderDto>> CreateAsync(CreateOrmeKnitOrderRequest request, CancellationToken cancellationToken)
    {
        var status = await db.Set<OrmeOrderLine>().Where(l => l.Id == request.OrderLineId).Select(l => (OrderStatus?)l.Order.Status).SingleOrDefaultAsync(cancellationToken);
        if (status is null)
        {
            return ReferenceValidation.InvalidReference("orderLineId", "Sipariş satırı bulunamadı.");
        }

        if (status != OrderStatus.Confirmed)
        {
            return new Error(InvalidState, "Örme emri yalnızca onaylı siparişten açılır.", ErrorType.Conflict);
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var year = request.PlannedDate.Year;
        var sequence = await numbers.NextAsync(Series, year, cancellationToken);
        var knit = new OrmeKnitOrder
        {
            KnitOrderNo = $"OE-{year}-{sequence:D5}",
            OrderLineId = request.OrderLineId,
            PlannedDate = request.PlannedDate,
            PlannedKilograms = request.PlannedKilograms,
            Notes = NullIfBlank(request.Notes),
        };
        db.Set<OrmeKnitOrder>().Add(knit);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetAsync(knit.Id, cancellationToken);
    }

    public async Task<Result<OrmeKnitOrderDto>> UpdateAsync(int id, UpdateOrmeKnitOrderRequest request, CancellationToken cancellationToken)
    {
        var knit = await db.Set<OrmeKnitOrder>().SingleOrDefaultAsync(k => k.Id == id, cancellationToken);
        if (knit is null)
        {
            return NotFound;
        }

        if (knit.Status != KnitOrderStatus.Open)
        {
            return NotOpen;
        }

        knit.PlannedDate = request.PlannedDate;
        knit.PlannedKilograms = request.PlannedKilograms;
        knit.Notes = NullIfBlank(request.Notes);
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    /// <summary>Emri bitirir ve makineleri boşaltır; artan iplik bundan sonra da geri alınabilir.</summary>
    public async Task<Result<OrmeKnitOrderDto>> CompleteAsync(int id, CancellationToken cancellationToken)
    {
        var knit = await db.Set<OrmeKnitOrder>().Include(k => k.Machines).SingleOrDefaultAsync(k => k.Id == id, cancellationToken);
        if (knit is null)
        {
            return NotFound;
        }

        if (knit.Status != KnitOrderStatus.Open)
        {
            return NotOpen;
        }

        if (!await db.Set<OrmeFabricRoll>().AnyAsync(r => r.KnitOrderId == id, cancellationToken))
        {
            return new Error(InvalidState, "Emir bitirilmeden önce en az bir top kaydedilmelidir; hiç örülmediyse iptal edin.", ErrorType.Conflict);
        }

        var today = DateOnly.FromDateTime(DateTime.Today);
        foreach (var machine in knit.Machines.Where(m => m.ReleasedOn is null))
        {
            machine.ReleasedOn = machine.AssignedOn > today ? machine.AssignedOn : today;
        }

        knit.Status = KnitOrderStatus.Completed;
        knit.CompletedOn = today;
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    /// <summary>Yanlışlıkla bitirilen emri açar; makineler yeniden atanır.</summary>
    public async Task<Result<OrmeKnitOrderDto>> ReopenAsync(int id, CancellationToken cancellationToken)
    {
        var knit = await db.Set<OrmeKnitOrder>().SingleOrDefaultAsync(k => k.Id == id, cancellationToken);
        if (knit is null)
        {
            return NotFound;
        }

        if (knit.Status != KnitOrderStatus.Completed)
        {
            return new Error(InvalidState, "Yalnızca biten emir yeniden açılır.", ErrorType.Conflict);
        }

        knit.Status = KnitOrderStatus.Open;
        knit.CompletedOn = null;
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    /// <summary>Top çıkmamış emri iptal eder: verilen iplik lotlara döner, makineler boşalır.</summary>
    public async Task<Result<OrmeKnitOrderDto>> CancelAsync(int id, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (!await appLock.TryAcquireAsync(OrmeYarnStock.YarnLotLock, LockTimeout, cancellationToken))
        {
            return Busy;
        }

        var knit = await db.Set<OrmeKnitOrder>()
            .Include(k => k.Machines).Include(k => k.Issues).Include(k => k.TakeBacks).Include(k => k.Rolls)
            .SingleOrDefaultAsync(k => k.Id == id, cancellationToken);
        if (knit is null)
        {
            return NotFound;
        }

        if (knit.Status != KnitOrderStatus.Open)
        {
            return NotOpen;
        }

        if (knit.Rolls.Count > 0)
        {
            return new Error(InvalidState, "Top kaydedilmiş emir iptal edilemez; önce topları silin.", ErrorType.Conflict);
        }

        db.Set<OrmeYarnTakeBack>().RemoveRange(knit.TakeBacks);
        db.Set<OrmeYarnIssue>().RemoveRange(knit.Issues);
        db.Set<OrmeKnitOrderMachine>().RemoveRange(knit.Machines);
        knit.Status = KnitOrderStatus.Cancelled;
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    /// <summary>Emre atanabilecek makineler: etkin, kumaşla aynı plaka, bu emirde çalışmayan. Dolu makine bilgi olarak gösterilir.</summary>
    public async Task<Result<IReadOnlyList<OrmeMachineOptionDto>>> GetMachineOptionsAsync(int id, CancellationToken cancellationToken)
    {
        var fabric = await db.Set<OrmeKnitOrder>().AsNoTracking().Where(k => k.Id == id).Select(k => k.OrderLine.Fabric).SingleOrDefaultAsync(cancellationToken);
        if (fabric is null)
        {
            return NotFound;
        }

        var machines = await db.Set<OrmeMachine>().AsNoTracking()
            .Where(m => m.IsActive && m.BedType == fabric.BedType)
            .OrderBy(m => m.Code)
            .ToListAsync(cancellationToken);
        var running = await db.Set<OrmeKnitOrderMachine>().AsNoTracking()
            .Where(m => m.ReleasedOn == null)
            .Select(m => new { m.MachineId, m.KnitOrderId, m.KnitOrder.KnitOrderNo })
            .ToDictionaryAsync(m => m.MachineId, cancellationToken);

        return machines
            .Where(m => running.GetValueOrDefault(m.Id)?.KnitOrderId != id)
            .Select(m => new OrmeMachineOptionDto(m.Id, m.Code, m.Name, m.DiameterInch, m.Gauge, running.GetValueOrDefault(m.Id)?.KnitOrderNo, FitWarning(fabric.DiameterInch, fabric.Gauge, m)))
            .ToList();
    }

    public async Task<Result<OrmeKnitOrderDto>> AssignMachineAsync(int id, AssignOrmeMachineRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (!await appLock.TryAcquireAsync(MachineLock, LockTimeout, cancellationToken))
        {
            return Busy;
        }

        var knit = await db.Set<OrmeKnitOrder>().AsNoTracking().Include(k => k.OrderLine).ThenInclude(l => l.Fabric).SingleOrDefaultAsync(k => k.Id == id, cancellationToken);
        if (knit is null)
        {
            return NotFound;
        }

        if (knit.Status != KnitOrderStatus.Open)
        {
            return NotOpen;
        }

        var machine = await db.Set<OrmeMachine>().AsNoTracking().SingleOrDefaultAsync(m => m.Id == request.MachineId, cancellationToken);
        if (machine is null || !machine.IsActive)
        {
            return ReferenceValidation.InvalidReference("machineId", "Makine bulunamadı ya da pasif.");
        }

        if (machine.BedType != knit.OrderLine.Fabric.BedType)
        {
            return ReferenceValidation.InvalidReference("machineId", "Makinenin plaka yapısı kumaşla aynı değil.");
        }

        if (await db.Set<OrmeMachineStop>().AnyAsync(s => s.MachineId == request.MachineId && s.EndedAt == null && s.Category == OrmeStopCategory.Breakdown, cancellationToken))
        {
            return new Error(InvalidState, "Makine arızalı; arıza kapanmadan emir atanamaz.", ErrorType.Conflict);
        }

        var busy = await db.Set<OrmeKnitOrderMachine>().Where(m => m.MachineId == request.MachineId && m.ReleasedOn == null)
            .Select(m => m.KnitOrder.KnitOrderNo).FirstOrDefaultAsync(cancellationToken);
        if (busy is not null)
        {
            return new Error(InvalidState, $"{machine.Code} makinesinde {busy} emri çalışıyor; önce o emirden alın.", ErrorType.Conflict);
        }

        db.Set<OrmeKnitOrderMachine>().Add(new OrmeKnitOrderMachine { KnitOrderId = id, MachineId = request.MachineId, AssignedOn = request.AssignedOn });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    /// <summary>Makineyi emirden alır. Top çıkmışsa kayıt geçmiş olarak kalır, çıkmamışsa silinir.</summary>
    public async Task<Result<OrmeKnitOrderDto>> ReleaseMachineAsync(int id, int assignmentId, CancellationToken cancellationToken)
    {
        var assignment = await db.Set<OrmeKnitOrderMachine>().Include(m => m.KnitOrder).SingleOrDefaultAsync(m => m.Id == assignmentId && m.KnitOrderId == id, cancellationToken);
        if (assignment is null)
        {
            return new Error(ErrorCodes.NotFound, "Makine ataması bulunamadı.", ErrorType.NotFound);
        }

        if (assignment.KnitOrder.Status != KnitOrderStatus.Open)
        {
            return NotOpen;
        }

        if (assignment.ReleasedOn is not null)
        {
            return new Error(InvalidState, "Makine zaten bu emirden alınmış.", ErrorType.Conflict);
        }

        if (await db.Set<OrmeFabricRoll>().AnyAsync(r => r.KnitOrderId == id && r.MachineId == assignment.MachineId, cancellationToken))
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            assignment.ReleasedOn = assignment.AssignedOn > today ? assignment.AssignedOn : today;
        }
        else
        {
            db.Set<OrmeKnitOrderMachine>().Remove(assignment);
        }

        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    /// <summary>Emre verilebilecek lotlar: kumaşın iplikleri, malın sahibi siparişle aynı, kalanı olan.</summary>
    public async Task<Result<IReadOnlyList<OrmeIssuableLotDto>>> GetIssuableLotsAsync(int id, CancellationToken cancellationToken)
    {
        var knit = await db.Set<OrmeKnitOrder>().AsNoTracking()
            .Include(k => k.OrderLine).ThenInclude(l => l.Order)
            .Include(k => k.OrderLine).ThenInclude(l => l.Fabric).ThenInclude(f => f.Yarns)
            .SingleOrDefaultAsync(k => k.Id == id, cancellationToken);
        if (knit is null)
        {
            return NotFound;
        }

        var owner = OwnerOf(knit.OrderLine.Order);
        var yarnIds = knit.OrderLine.Fabric.Yarns.Select(y => y.YarnId).Distinct().ToList();
        var lots = await db.Set<OrmeYarnLot>().AsNoTracking()
            .Where(l => yarnIds.Contains(l.YarnId) && l.CustomerId == owner)
            .Select(l => new
            {
                l.Id,
                l.LotNo,
                l.ReceiptDate,
                l.YarnId,
                l.Yarn.YarnCode,
                l.Yarn.Name,
                l.ProducerLotNo,
                Remaining = l.Kilograms - (l.Issues.Sum(i => (decimal?)i.Kilograms) ?? 0) + (l.TakeBacks.Sum(t => (decimal?)t.Kilograms) ?? 0) - (l.Returns.Sum(r => (decimal?)r.Kilograms) ?? 0),
            })
            .Where(l => l.Remaining > 0)
            .OrderBy(l => l.YarnCode).ThenBy(l => l.ReceiptDate).ThenBy(l => l.Id)
            .ToListAsync(cancellationToken);

        return lots.Select(l => new OrmeIssuableLotDto(l.Id, l.LotNo, l.ReceiptDate, l.YarnId, l.YarnCode, l.Name, l.ProducerLotNo, l.Remaining)).ToList();
    }

    public async Task<Result<OrmeKnitOrderDto>> AddIssueAsync(int id, OrmeYarnIssueRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (!await appLock.TryAcquireAsync(OrmeYarnStock.YarnLotLock, LockTimeout, cancellationToken))
        {
            return Busy;
        }

        var knit = await db.Set<OrmeKnitOrder>()
            .Include(k => k.OrderLine).ThenInclude(l => l.Order)
            .Include(k => k.OrderLine).ThenInclude(l => l.Fabric).ThenInclude(f => f.Yarns)
            .SingleOrDefaultAsync(k => k.Id == id, cancellationToken);
        if (knit is null)
        {
            return NotFound;
        }

        if (knit.Status != KnitOrderStatus.Open)
        {
            return NotOpen;
        }

        var lot = await db.Set<OrmeYarnLot>().SingleOrDefaultAsync(l => l.Id == request.YarnLotId, cancellationToken);
        if (lot is null)
        {
            return ReferenceValidation.InvalidReference("yarnLotId", "İplik lotu bulunamadı.");
        }

        if (lot.CustomerId != OwnerOf(knit.OrderLine.Order))
        {
            return ReferenceValidation.InvalidReference("yarnLotId", "Lotun sahibi siparişle aynı değil: fasonda müşterinin, kendi malında işletmenin ipliği verilir.");
        }

        if (knit.OrderLine.Fabric.Yarns.All(y => y.YarnId != lot.YarnId))
        {
            return ReferenceValidation.InvalidReference("yarnLotId", "Bu iplik kumaş kartında yok.");
        }

        var remaining = lot.Kilograms - await OrmeYarnStock.IssuedFromLotAsync(db, lot.Id, cancellationToken);
        if (request.Kilograms > remaining)
        {
            return new Error(InvalidState, $"{lot.LotNo} lotunda {remaining:0.##} kg kaldı; daha fazlası verilemez.", ErrorType.Conflict);
        }

        db.Set<OrmeYarnIssue>().Add(new OrmeYarnIssue { KnitOrderId = id, YarnLotId = lot.Id, IssuedOn = request.IssuedOn, Kilograms = request.Kilograms });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    /// <summary>Yanlış girilen çıkışı siler; iplik lota döner. Yalnızca açık emirde, geri alınan miktarın altına inmeden.</summary>
    public async Task<Result<OrmeKnitOrderDto>> RemoveIssueAsync(int id, int issueId, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (!await appLock.TryAcquireAsync(OrmeYarnStock.YarnLotLock, LockTimeout, cancellationToken))
        {
            return Busy;
        }

        var issue = await db.Set<OrmeYarnIssue>().Include(i => i.KnitOrder).SingleOrDefaultAsync(i => i.Id == issueId && i.KnitOrderId == id, cancellationToken);
        if (issue is null)
        {
            return new Error(ErrorCodes.NotFound, "İplik çıkışı bulunamadı.", ErrorType.NotFound);
        }

        if (issue.KnitOrder.Status != KnitOrderStatus.Open)
        {
            return NotOpen;
        }

        var (issued, taken) = await OnOrderAsync(id, issue.YarnLotId, cancellationToken);
        if (issued - issue.Kilograms < taken)
        {
            return new Error(InvalidState, $"Bu lottan {taken:0.##} kg geri alınmış; çıkış silinemez, önce geri alımı silin.", ErrorType.Conflict);
        }

        db.Set<OrmeYarnIssue>().Remove(issue);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    /// <summary>Artan ipliği geri alır: verilenden düşer, lota döner. Emir açıkken de bittikten sonra da yapılır.</summary>
    public async Task<Result<OrmeKnitOrderDto>> AddTakeBackAsync(int id, OrmeYarnTakeBackRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (!await appLock.TryAcquireAsync(OrmeYarnStock.YarnLotLock, LockTimeout, cancellationToken))
        {
            return Busy;
        }

        var status = await db.Set<OrmeKnitOrder>().Where(k => k.Id == id).Select(k => (KnitOrderStatus?)k.Status).SingleOrDefaultAsync(cancellationToken);
        if (status is null)
        {
            return NotFound;
        }

        if (status == KnitOrderStatus.Cancelled)
        {
            return new Error(InvalidState, "İptal edilen emirde iplik zaten lota döndü.", ErrorType.Conflict);
        }

        var (issued, taken) = await OnOrderAsync(id, request.YarnLotId, cancellationToken);
        if (request.Kilograms > issued - taken)
        {
            return new Error(InvalidState, $"Bu lottan emre verilip geri alınmamış {issued - taken:0.##} kg var; daha fazlası geri alınamaz.", ErrorType.Conflict);
        }

        db.Set<OrmeYarnTakeBack>().Add(new OrmeYarnTakeBack
        {
            KnitOrderId = id,
            YarnLotId = request.YarnLotId,
            TakenOn = request.TakenOn,
            Kilograms = request.Kilograms,
            Notes = NullIfBlank(request.Notes),
        });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    /// <summary>Yanlış geri alımı siler: kilo yeniden üretimde sayılır, bu yüzden lotta o kadar kalan olmalıdır.</summary>
    public async Task<Result<OrmeKnitOrderDto>> RemoveTakeBackAsync(int id, int takeBackId, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (!await appLock.TryAcquireAsync(OrmeYarnStock.YarnLotLock, LockTimeout, cancellationToken))
        {
            return Busy;
        }

        var item = await db.Set<OrmeYarnTakeBack>().Include(t => t.YarnLot).SingleOrDefaultAsync(t => t.Id == takeBackId && t.KnitOrderId == id, cancellationToken);
        if (item is null)
        {
            return new Error(ErrorCodes.NotFound, "İplik geri alımı bulunamadı.", ErrorType.NotFound);
        }

        var remaining = item.YarnLot.Kilograms - await OrmeYarnStock.IssuedFromLotAsync(db, item.YarnLotId, cancellationToken);
        if (item.Kilograms > remaining)
        {
            return new Error(InvalidState, $"{item.YarnLot.LotNo} lotunda {remaining:0.##} kg kaldı; geri alım silinemez, iplik başka yere verilmiş.", ErrorType.Conflict);
        }

        db.Set<OrmeYarnTakeBack>().Remove(item);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    public async Task<Result<OrmeKnitOrderDto>> AddRollAsync(int id, OrmeFabricRollRequest request, CancellationToken cancellationToken)
    {
        var added = await AddRollAsync(id, request.MachineId, request.CutOn, request.Kilograms, request.Meters, request.Notes, null, null, cancellationToken);
        return added.IsSuccess ? await GetAsync(id, cancellationToken) : added.Error!;
    }

    public async Task<Result<OrmeTabletRollDto>> AddRollFromTabletAsync(int id, OrmeTabletRollRequest request, int operatorId, CancellationToken cancellationToken)
    {
        var added = await AddRollAsync(id, request.MachineId, request.CutOn, request.Kilograms, request.Meters, request.Notes, request.RequestId, operatorId, cancellationToken);
        return added.IsSuccess ? new OrmeTabletRollDto(added.Value!.RollNo, added.Value.Kilograms) : added.Error!;
    }

    private async Task<Result<OrmeFabricRoll>> AddRollAsync(
        int id, int machineId, DateOnly cutOn, decimal kilograms, decimal? meters, string? notes, Guid? requestId, int? operatorId, CancellationToken cancellationToken)
    {
        var knit = await db.Set<OrmeKnitOrder>().AsNoTracking().SingleOrDefaultAsync(k => k.Id == id, cancellationToken);
        if (knit is null)
        {
            return NotFound;
        }

        if (knit.Status != KnitOrderStatus.Open)
        {
            return NotOpen;
        }

        var assignment = await db.Set<OrmeKnitOrderMachine>().AsNoTracking().SingleOrDefaultAsync(m => m.KnitOrderId == id && m.MachineId == machineId && m.ReleasedOn == null, cancellationToken);
        if (assignment is null)
        {
            return ReferenceValidation.InvalidReference("machineId", "Bu makinede emir çalışmıyor; önce makineyi emre atayın.");
        }

        if (cutOn < assignment.AssignedOn)
        {
            return ReferenceValidation.InvalidReference("cutOn", "Top tarihi makineye atama tarihinden önce olamaz.");
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var year = cutOn.Year;
        var sequence = await numbers.NextAsync(RollSeries, year, cancellationToken);
        var roll = new OrmeFabricRoll
        {
            RollNo = $"OT-{year}-{sequence:D6}",
            KnitOrderId = id,
            MachineId = machineId,
            CutOn = cutOn,
            Kilograms = kilograms,
            Meters = meters,
            Notes = NullIfBlank(notes),
            ClientRequestId = requestId,
            CutByOperatorId = operatorId,
        };
        db.Set<OrmeFabricRoll>().Add(roll);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return roll;
    }

    /// <summary>Yanlış girilen topu siler: emir açık ve top kontrol edilmemiş olmalıdır.</summary>
    public async Task<Result<OrmeKnitOrderDto>> RemoveRollAsync(int id, int rollId, CancellationToken cancellationToken)
    {
        var roll = await db.Set<OrmeFabricRoll>().Include(r => r.KnitOrder).SingleOrDefaultAsync(r => r.Id == rollId && r.KnitOrderId == id, cancellationToken);
        if (roll is null)
        {
            return new Error(ErrorCodes.NotFound, "Top bulunamadı.", ErrorType.NotFound);
        }

        if (roll.KnitOrder.Status != KnitOrderStatus.Open)
        {
            return NotOpen;
        }

        if (roll.Status != FabricRollStatus.PendingInspection)
        {
            return new Error(InvalidState, "Kontrol edilmiş top silinemez.", ErrorType.Conflict);
        }

        db.Set<OrmeFabricRoll>().Remove(roll);
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    public Task<PagedResult<OrmeFabricRollDto>> GetRollsAsync(OrmeFabricRollQuery query, CancellationToken cancellationToken)
    {
        var rolls = db.Set<OrmeFabricRoll>().AsNoTracking();
        if (NullIfBlank(query.Search) is { } term)
        {
            rolls = rolls.Where(r => r.RollNo.Contains(term)
                || r.KnitOrder.KnitOrderNo.Contains(term)
                || r.Machine.Code.Contains(term)
                || r.KnitOrder.OrderLine.Order.OrderNo.Contains(term)
                || r.KnitOrder.OrderLine.Fabric.FabricCode.Contains(term));
        }

        return ProjectRolls(rolls.OrderByDescending(r => r.Id)).ToPagedResultAsync(query, cancellationToken);
    }

    public async Task<IReadOnlyList<OrmeMachineBoardItemDto>> GetBoardAsync(CancellationToken cancellationToken)
    {
        var running = await db.Set<OrmeKnitOrderMachine>().AsNoTracking()
            .Where(m => m.ReleasedOn == null)
            .Select(m => new
            {
                m.MachineId,
                m.KnitOrderId,
                m.AssignedOn,
                m.KnitOrder.KnitOrderNo,
                m.KnitOrder.PlannedKilograms,
                m.KnitOrder.OrderLine.Order.OrderNo,
                CustomerName = m.KnitOrder.OrderLine.Order.Customer != null ? m.KnitOrder.OrderLine.Order.Customer.Name : null,
                m.KnitOrder.OrderLine.Fabric.FabricCode,
                FabricName = m.KnitOrder.OrderLine.Fabric.Name,
                m.KnitOrder.OrderLine.Fabric.DiameterInch,
                m.KnitOrder.OrderLine.Fabric.Gauge,
                Knitted = m.KnitOrder.Rolls.Sum(r => (decimal?)r.Kilograms) ?? 0,
            })
            .ToDictionaryAsync(m => m.MachineId, cancellationToken);
        var runningIds = running.Keys.ToList();

        // Pasife alınmış makine üzerinde emir çalışıyorsa panoda görünmeye devam eder.
        var machines = await db.Set<OrmeMachine>().AsNoTracking()
            .Where(m => m.IsActive || runningIds.Contains(m.Id))
            .OrderBy(m => m.Code)
            .ToListAsync(cancellationToken);

        var knitOrderIds = running.Values.Select(m => m.KnitOrderId).Distinct().ToList();
        var produced = await db.Set<OrmeFabricRoll>().AsNoTracking()
            .Where(r => knitOrderIds.Contains(r.KnitOrderId))
            .GroupBy(r => new { r.KnitOrderId, r.MachineId })
            .Select(g => new { g.Key.KnitOrderId, g.Key.MachineId, Kilograms = g.Sum(r => r.Kilograms), Count = g.Count() })
            .ToDictionaryAsync(g => (g.KnitOrderId, g.MachineId), cancellationToken);
        var stops = await db.Set<OrmeMachineStop>().AsNoTracking()
            .Where(s => s.EndedAt == null)
            .Select(s => new { s.MachineId, s.Category, ReasonName = s.Reason.Name, s.StartedAt })
            .ToDictionaryAsync(s => s.MachineId, cancellationToken);

        return machines.Select(m =>
        {
            var run = running.GetValueOrDefault(m.Id);
            var own = run is null ? null : produced.GetValueOrDefault((run.KnitOrderId, m.Id));
            var stop = stops.GetValueOrDefault(m.Id);
            return new OrmeMachineBoardItemDto(
                m.Id, m.Code, m.Name, m.BedType, m.DiameterInch, m.Gauge,
                run?.KnitOrderId, run?.KnitOrderNo, run?.OrderNo, run?.CustomerName, run?.FabricCode, run?.FabricName, run?.AssignedOn,
                run?.PlannedKilograms, run?.Knitted, run is null ? null : own?.Kilograms ?? 0, run is null ? null : Math.Max(0, run.PlannedKilograms - run.Knitted),
                own?.Count ?? 0,
                run is null ? null : FitWarning(run.DiameterInch, run.Gauge, m),
                stop?.Category,
                stop?.ReasonName,
                stop?.StartedAt);
        }).ToList();
    }

    private async Task<(decimal Issued, decimal Taken)> OnOrderAsync(int id, int lotId, CancellationToken cancellationToken)
    {
        var issued = await db.Set<OrmeYarnIssue>().Where(i => i.KnitOrderId == id && i.YarnLotId == lotId).SumAsync(i => (decimal?)i.Kilograms, cancellationToken) ?? 0;
        var taken = await db.Set<OrmeYarnTakeBack>().Where(t => t.KnitOrderId == id && t.YarnLotId == lotId).SumAsync(t => (decimal?)t.Kilograms, cancellationToken) ?? 0;
        return (issued, taken);
    }

    public static IQueryable<OrmeFabricRollDto> ProjectRolls(IQueryable<OrmeFabricRoll> rolls) => rolls.Select(r => new OrmeFabricRollDto(
        r.Id,
        r.RollNo,
        r.KnitOrderId,
        r.KnitOrder.KnitOrderNo,
        r.MachineId,
        r.Machine.Code,
        r.KnitOrder.OrderLine.OrderId,
        r.KnitOrder.OrderLine.Order.OrderNo,
        r.KnitOrder.OrderLine.Order.Customer != null ? r.KnitOrder.OrderLine.Order.Customer.Name : null,
        r.KnitOrder.OrderLine.Fabric.FabricCode,
        r.KnitOrder.OrderLine.Fabric.Name,
        r.CutOn,
        r.Kilograms,
        r.Meters,
        r.Meters == null && r.KnitOrder.OrderLine.Fabric.RawWidthCm > 0 && r.KnitOrder.OrderLine.Fabric.RawWeightGsm > 0
            ? Math.Round(r.Kilograms * 1000m / (r.KnitOrder.OrderLine.Fabric.RawWeightGsm * r.KnitOrder.OrderLine.Fabric.RawWidthCm!.Value / 100m), 2)
            : null,
        r.Status,
        r.Notes,
        r.CutByOperator != null ? r.CutByOperator.Name : null,
        r.InspectedOn,
        r.InspectedMeters,
        r.InspectedMetersEstimated,
        r.InspectedWidthCm,
        r.InspectedWeightGsm,
        r.PointsPer100M2,
        r.SpecDeviation,
        r.OverrideNote));

    /// <summary>Çap ya da fine kumaş kartıyla uyuşmuyorsa uyarı metni. Kumaşta boşsa karşılaştırılmaz.</summary>
    internal static string? FitWarning(int? diameterInch, int? gauge, OrmeMachine machine)
    {
        var differences = new List<string>();
        if (diameterInch is { } diameter && diameter != machine.DiameterInch)
        {
            differences.Add($"çap makinede {machine.DiameterInch}\", kumaşta {diameter}\"");
        }

        if (gauge is { } fine && fine != machine.Gauge)
        {
            differences.Add($"fine makinede E{machine.Gauge}, kumaşta E{fine}");
        }

        return differences.Count == 0 ? null : string.Join("; ", differences);
    }

    private static int? OwnerOf(OrmeOrder order) => order.Ownership == OrderOwnership.Commission ? order.CustomerId : null;
}
