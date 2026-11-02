using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Dokuma.Domain.Entities;
using ERP.Dokuma.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using static ERP.Core.Application.Features.MasterData.MasterDataRules;

namespace ERP.Dokuma.Application.Features.Reports;

public sealed class DokumaLoomReportQuery
{
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }

    /// <summary>Tezgâhın günde çalıştığı saat (vardiya düzeni).</summary>
    public int DailyHours { get; set; } = 24;
}

public sealed class DokumaLoomReportQueryValidator : AbstractValidator<DokumaLoomReportQuery>
{
    public DokumaLoomReportQueryValidator()
    {
        RuleFor(x => x.From).NotEmpty().WithMessage("Başlangıç tarihi zorunludur.");
        RuleFor(x => x.To).NotEmpty().WithMessage("Bitiş tarihi zorunludur.").GreaterThanOrEqualTo(x => x.From).WithMessage("Bitiş başlangıçtan önce olamaz.");
        RuleFor(x => x.DailyHours).InclusiveBetween(1, 24).WithMessage("Günlük saat 1 ile 24 arasında olmalıdır.");
    }
}

/// <param name="LoadedDays">Dönemde tezgâhta levent bulunan gün.</param>
/// <param name="AvailableMinutes">Yüklü gün × günlük saat, planlı duruş düşülmüş.</param>
/// <param name="RunningPercent">Kullanılabilir sürenin üretim duruşu ve arıza dışında kalan payı.</param>
/// <param name="EfficiencyPercent">Atılan atkı / anma hızıyla atılabilecek atkı; anma hızı yazılı değilse boş.</param>
public sealed record DokumaLoomReportRowDto(
    int LoomId,
    string LoomCode,
    string LoomName,
    int? NominalSpeedRpm,
    int RollCount,
    decimal WovenMeters,
    int LoadedDays,
    int StopMinutes,
    int BreakdownMinutes,
    int PlannedMinutes,
    int AvailableMinutes,
    decimal? RunningPercent,
    decimal? EfficiencyPercent);

public sealed class DokumaOrderReportQuery : PageRequest
{
    /// <summary>Sipariş no, müşteri ya da artikelde aranır.</summary>
    public string? Search { get; set; }

    /// <summary>Yalnızca sevki tamamlanmamış satırlar.</summary>
    public bool OpenOnly { get; set; }
}

/// <param name="BeamMeters">Çözgüsü çekilen levent metresi.</param>
/// <param name="ShippedMeters">Onaylı irsaliyeyle ya da Boyahane'ye gönderilen.</param>
public sealed record DokumaOrderReportRowDto(
    int OrderLineId,
    int OrderId,
    string OrderNo,
    string? CustomerName,
    string ArticleCode,
    string ArticleName,
    OrderOwnership Ownership,
    DateOnly? DueDate,
    decimal OrderedMeters,
    decimal BeamMeters,
    decimal WovenMeters,
    decimal FirstQualityMeters,
    decimal SecondQualityMeters,
    decimal RejectedMeters,
    decimal PackedMeters,
    decimal ShippedMeters,
    decimal InvoicedMeters);

public sealed class DokumaQualityReportQuery
{
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
}

public sealed class DokumaQualityReportQueryValidator : AbstractValidator<DokumaQualityReportQuery>
{
    public DokumaQualityReportQueryValidator()
    {
        RuleFor(x => x.From).NotEmpty().WithMessage("Başlangıç tarihi zorunludur.");
        RuleFor(x => x.To).NotEmpty().WithMessage("Bitiş tarihi zorunludur.").GreaterThanOrEqualTo(x => x.From).WithMessage("Bitiş başlangıçtan önce olamaz.");
    }
}

/// <param name="AveragePointsPer100M2">Metreyle ağırlıklı ortalama.</param>
public sealed record DokumaQualityGroupDto(
    string Code,
    string Name,
    int RollCount,
    decimal Meters,
    decimal FirstQualityMeters,
    decimal SecondQualityMeters,
    decimal RejectedMeters,
    decimal? AveragePointsPer100M2);

public sealed record DokumaDefectSummaryDto(string Code, string Name, int Count, int Points);

public sealed record DokumaQualityReportDto(
    DokumaQualityGroupDto Total,
    IReadOnlyList<DokumaQualityGroupDto> ByArticle,
    IReadOnlyList<DokumaQualityGroupDto> ByLoom,
    IReadOnlyList<DokumaDefectSummaryDto> Defects);

public sealed class DokumaShipmentReportQuery
{
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
}

public sealed class DokumaShipmentReportQueryValidator : AbstractValidator<DokumaShipmentReportQuery>
{
    public const int MaxDays = 366;

    public DokumaShipmentReportQueryValidator()
    {
        RuleFor(x => x.From).NotEmpty().WithMessage("Başlangıç tarihi zorunludur.");
        RuleFor(x => x.To).NotEmpty().WithMessage("Bitiş tarihi zorunludur.").GreaterThanOrEqualTo(x => x.From).WithMessage("Bitiş başlangıçtan önce olamaz.");
        RuleFor(x => x)
            .Must(x => x.To.DayNumber - x.From.DayNumber < MaxDays).WithMessage($"Dönem en fazla {MaxDays} gün olabilir.")
            .When(x => x.To >= x.From);
    }
}

/// <summary>Günlük sevk satırı: bir günde bir müşteriye (ya da Boyahane'ye) giden.</summary>
/// <param name="Name">Müşteri adı; Boyahane'ye gidende "Boyahane'ye".</param>
/// <param name="DocumentCount">İrsaliye sayısı; Boyahane'ye gidende çeki listesi sayısı.</param>
/// <param name="Kilograms">Tartılmış topların kilosu.</param>
/// <param name="UnweighedRollCount">Tartılmamış top sayısı; sıfırdan büyükse kilo eksiktir.</param>
public sealed record DokumaShipmentRowDto(
    int? CustomerId,
    string Name,
    bool IsBoyahane,
    int DocumentCount,
    int RollCount,
    decimal Meters,
    decimal Kilograms,
    int UnweighedRollCount);

public sealed record DokumaShipmentDayDto(DateOnly Day, IReadOnlyList<DokumaShipmentRowDto> Rows, DokumaShipmentRowDto Total);

/// <summary>Günlük sevk: onaylı irsaliyeler (sevk tarihi) ve Boyahane'ye gönderilen çeki listeleri (gönderim tarihi).</summary>
public sealed record DokumaShipmentReportDto(IReadOnlyList<DokumaShipmentDayDto> Days, DokumaShipmentRowDto Total);

public sealed class DokumaIdleStockQuery
{
    /// <summary>Bu kadar gündür hareket görmeyen stok listelenir.</summary>
    public int Days { get; set; } = 90;
}

public sealed class DokumaIdleStockQueryValidator : AbstractValidator<DokumaIdleStockQuery>
{
    public DokumaIdleStockQueryValidator()
    {
        RuleFor(x => x.Days).InclusiveBetween(1, 3650).WithMessage("Gün 1 ile 3650 arasında olmalıdır.");
    }
}

/// <param name="OwnerName">Malın sahibi müşteri; boşsa işletmenin malı.</param>
/// <param name="LastMovementOn">Son çözgü / atkı çıkışı, iade ya da geri alım; hiç yoksa giriş tarihi.</param>
public sealed record DokumaIdleYarnLotDto(
    int LotId,
    string LotNo,
    string YarnCode,
    string YarnName,
    string? OwnerName,
    DateOnly ReceiptDate,
    decimal RemainingKilograms,
    DateOnly LastMovementOn,
    int IdleDays);

/// <param name="Since">Kontrol tarihi; kontrol edilmediyse kesim tarihi.</param>
public sealed record DokumaIdleRollDto(
    int RollId,
    string RollNo,
    string ArticleCode,
    string ArticleName,
    string OrderNo,
    string? CustomerName,
    decimal Meters,
    FabricRollStatus Status,
    DateOnly Since,
    int IdleDays);

/// <param name="Since">Hazır olma, haşıl ya da çözgü tarihi (en sonuncusu).</param>
public sealed record DokumaIdleBeamDto(
    int BeamId,
    string BeamNo,
    string WarpNo,
    string OrderNo,
    string ArticleCode,
    decimal Meters,
    BeamStatus Status,
    DateOnly Since,
    int IdleDays);

/// <summary>Hareketsiz stok: kalanı olan ama uzun süredir çıkış görmeyen iplik lotları, listeye girmemiş ham toplar, bekleyen leventler.</summary>
/// <param name="RollsTruncated">Top listesi üst sınırda kesildi; en eski toplar gösterilir.</param>
public sealed record DokumaIdleStockDto(
    int Days,
    DateOnly Cutoff,
    IReadOnlyList<DokumaIdleYarnLotDto> YarnLots,
    IReadOnlyList<DokumaIdleRollDto> Rolls,
    IReadOnlyList<DokumaIdleBeamDto> Beams,
    bool RollsTruncated);

public interface IDokumaReportService
{
    Task<IReadOnlyList<DokumaLoomReportRowDto>> GetLoomsAsync(DokumaLoomReportQuery query, CancellationToken cancellationToken);
    Task<PagedResult<DokumaOrderReportRowDto>> GetOrdersAsync(DokumaOrderReportQuery query, CancellationToken cancellationToken);
    Task<DokumaQualityReportDto> GetQualityAsync(DokumaQualityReportQuery query, CancellationToken cancellationToken);
    Task<DokumaShipmentReportDto> GetShipmentsAsync(DokumaShipmentReportQuery query, CancellationToken cancellationToken);
    Task<DokumaIdleStockDto> GetIdleStockAsync(DokumaIdleStockQuery query, CancellationToken cancellationToken);
}

public sealed class DokumaReportService(IAppDbContext db, TimeProvider timeProvider) : IDokumaReportService
{
    private const decimal CmPerMeter = 100m;

    public async Task<IReadOnlyList<DokumaLoomReportRowDto>> GetLoomsAsync(DokumaLoomReportQuery query, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var zone = timeProvider.LocalTimeZone;
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(now, zone));
        var periodStart = LocalDayStartUtc(query.From, zone);
        var periodEnd = LocalDayStartUtc(query.To.AddDays(1), zone);

        var looms = await db.Set<DokumaLoom>().AsNoTracking().OrderBy(l => l.Code).Select(l => new { l.Id, l.Code, l.Name, l.NominalSpeedRpm, l.IsActive }).ToListAsync(cancellationToken);
        var rolls = await db.Set<DokumaFabricRoll>().AsNoTracking()
            .Where(r => r.CutOn >= query.From && r.CutOn <= query.To)
            .Select(r => new { r.LoomId, r.Meters, r.Beam.WarpOrder.OrderLine.Article.WeftDensityPerCm })
            .ToListAsync(cancellationToken);
        var beams = await db.Set<DokumaBeam>().AsNoTracking()
            .Where(b => b.LoomId != null && b.MountedOn != null && b.MountedOn <= query.To && (b.FinishedOn == null || b.FinishedOn >= query.From))
            .Select(b => new { LoomId = b.LoomId!.Value, MountedOn = b.MountedOn!.Value, b.FinishedOn })
            .ToListAsync(cancellationToken);
        var stops = await db.Set<DokumaLoomStop>().AsNoTracking()
            .Where(s => s.StartedAt < periodEnd && (s.EndedAt == null || s.EndedAt > periodStart))
            .Select(s => new { s.LoomId, s.Category, s.StartedAt, s.EndedAt })
            .ToListAsync(cancellationToken);

        var rows = new List<DokumaLoomReportRowDto>();
        foreach (var loom in looms)
        {
            var loomRolls = rolls.Where(r => r.LoomId == loom.Id).ToList();
            var loadedDays = new HashSet<DateOnly>();
            foreach (var beam in beams.Where(b => b.LoomId == loom.Id))
            {
                var first = beam.MountedOn > query.From ? beam.MountedOn : query.From;
                var last = Earliest(beam.FinishedOn ?? today, query.To, today);
                for (var day = first; day <= last; day = day.AddDays(1))
                {
                    loadedDays.Add(day);
                }
            }

            var minutes = new Dictionary<DokumaStopCategory, double>();
            foreach (var stop in stops.Where(s => s.LoomId == loom.Id))
            {
                var start = stop.StartedAt > periodStart ? stop.StartedAt : periodStart;
                var end = stop.EndedAt ?? now;
                end = end < periodEnd ? end : periodEnd;
                if (end > start)
                {
                    minutes[stop.Category] = minutes.GetValueOrDefault(stop.Category) + (end - start).TotalMinutes;
                }
            }

            var stopMinutes = Whole(minutes.GetValueOrDefault(DokumaStopCategory.Stop));
            var breakdownMinutes = Whole(minutes.GetValueOrDefault(DokumaStopCategory.Breakdown));
            var plannedMinutes = Whole(minutes.GetValueOrDefault(DokumaStopCategory.Planned));
            if (!loom.IsActive && loomRolls.Count == 0 && loadedDays.Count == 0 && minutes.Count == 0)
            {
                continue;
            }

            // Süre olmadan oran anlamsızdır; saat planı dışındaki duruşlar yüzünden oran eksiye düşmesin.
            var available = Math.Max(0, loadedDays.Count * query.DailyHours * 60 - plannedMinutes);
            decimal? running = available == 0 ? null : Percent(Math.Max(0, available - stopMinutes - breakdownMinutes), available);
            decimal? efficiency = available == 0 || loom.NominalSpeedRpm is not > 0
                ? null
                : Percent(loomRolls.Sum(r => r.Meters * r.WeftDensityPerCm * CmPerMeter), loom.NominalSpeedRpm.Value * (decimal)available);

            rows.Add(new DokumaLoomReportRowDto(
                loom.Id, loom.Code, loom.Name, loom.NominalSpeedRpm, loomRolls.Count, loomRolls.Sum(r => r.Meters), loadedDays.Count,
                stopMinutes, breakdownMinutes, plannedMinutes, available, running, efficiency));
        }

        return rows;
    }

    public Task<PagedResult<DokumaOrderReportRowDto>> GetOrdersAsync(DokumaOrderReportQuery query, CancellationToken cancellationToken)
    {
        var lines = db.Set<DokumaOrderLine>().AsNoTracking().Where(l => l.Order.Status == OrderStatus.Confirmed);
        if (NullIfBlank(query.Search) is { } term)
        {
            lines = lines.Where(l => l.Order.OrderNo.Contains(term) || l.Article.ArticleCode.Contains(term) || l.Article.Name.Contains(term)
                || (l.Order.Customer != null && l.Order.Customer.Name.Contains(term)));
        }

        var rolls = db.Set<DokumaFabricRoll>();
        var packed = db.Set<DokumaPackingItem>().Where(i => i.IsActive && i.PackingList.Status == PackingListStatus.Confirmed);
        if (query.OpenOnly)
        {
            lines = lines.Where(l => (packed.Where(i => i.Roll.Beam.WarpOrder.OrderLineId == l.Id
                && (i.PackingList.BoyahaneReceiptNo != null || (i.PackingList.Despatch != null && i.PackingList.Despatch.Status == DespatchStatus.Confirmed)))
                .Sum(i => (decimal?)i.Meters) ?? 0) < l.Meters);
        }

        return lines
            .OrderByDescending(l => l.OrderId).ThenBy(l => l.Id)
            .Select(l => new DokumaOrderReportRowDto(
                l.Id,
                l.OrderId,
                l.Order.OrderNo,
                l.Order.Customer != null ? l.Order.Customer.Name : null,
                l.Article.ArticleCode,
                l.Article.Name,
                l.Order.Ownership,
                l.Order.DueDate,
                l.Meters,
                db.Set<DokumaBeam>().Where(b => b.WarpOrder.OrderLineId == l.Id).Sum(b => (decimal?)b.Meters) ?? 0,
                rolls.Where(r => r.Beam.WarpOrder.OrderLineId == l.Id).Sum(r => (decimal?)r.Meters) ?? 0,
                rolls.Where(r => r.Beam.WarpOrder.OrderLineId == l.Id && r.Status == FabricRollStatus.FirstQuality).Sum(r => (decimal?)(r.InspectedMeters ?? r.Meters)) ?? 0,
                rolls.Where(r => r.Beam.WarpOrder.OrderLineId == l.Id && r.Status == FabricRollStatus.SecondQuality).Sum(r => (decimal?)(r.InspectedMeters ?? r.Meters)) ?? 0,
                rolls.Where(r => r.Beam.WarpOrder.OrderLineId == l.Id && r.Status == FabricRollStatus.Rejected).Sum(r => (decimal?)(r.InspectedMeters ?? r.Meters)) ?? 0,
                packed.Where(i => i.Roll.Beam.WarpOrder.OrderLineId == l.Id).Sum(i => (decimal?)i.Meters) ?? 0,
                packed.Where(i => i.Roll.Beam.WarpOrder.OrderLineId == l.Id
                    && (i.PackingList.BoyahaneReceiptNo != null || (i.PackingList.Despatch != null && i.PackingList.Despatch.Status == DespatchStatus.Confirmed)))
                    .Sum(i => (decimal?)i.Meters) ?? 0,
                db.Set<DokumaInvoiceLine>().Where(i => i.OrderLineId == l.Id && i.Invoice.Status == InvoiceStatus.Confirmed).Sum(i => (decimal?)i.Quantity) ?? 0))
            .ToPagedResultAsync(query, cancellationToken);
    }

    public async Task<DokumaQualityReportDto> GetQualityAsync(DokumaQualityReportQuery query, CancellationToken cancellationToken)
    {
        var inRange = db.Set<DokumaFabricRoll>().AsNoTracking().Where(r => r.InspectedOn >= query.From && r.InspectedOn <= query.To && r.Status != FabricRollStatus.PendingInspection);
        var rolls = await inRange
            .Select(r => new RollRow(
                r.Status,
                r.InspectedMeters ?? r.Meters,
                r.PointsPer100M2,
                r.Beam.WarpOrder.OrderLine.Article.ArticleCode,
                r.Beam.WarpOrder.OrderLine.Article.Name,
                r.Loom.Code,
                r.Loom.Name))
            .ToListAsync(cancellationToken);
        var defects = await db.Set<DokumaRollDefect>().AsNoTracking()
            .Where(d => d.Roll.InspectedOn >= query.From && d.Roll.InspectedOn <= query.To && d.Roll.Status != FabricRollStatus.PendingInspection)
            .GroupBy(d => new { d.DefectType.Code, d.DefectType.Name })
            .Select(g => new { g.Key.Code, g.Key.Name, Count = g.Count(), Points = g.Sum(d => d.Points) })
            .ToListAsync(cancellationToken);

        return new DokumaQualityReportDto(
            Group(string.Empty, string.Empty, rolls),
            rolls.GroupBy(r => (r.ArticleCode, r.ArticleName)).OrderByDescending(g => g.Sum(r => r.Meters)).Select(g => Group(g.Key.ArticleCode, g.Key.ArticleName, g.ToList())).ToList(),
            rolls.GroupBy(r => (r.LoomCode, r.LoomName)).OrderBy(g => g.Key.LoomCode).Select(g => Group(g.Key.LoomCode, g.Key.LoomName, g.ToList())).ToList(),
            defects.OrderByDescending(d => d.Points).ThenBy(d => d.Code).Select(d => new DokumaDefectSummaryDto(d.Code, d.Name, d.Count, d.Points)).ToList());
    }

    /// <summary>
    /// Günlük sevk: onaylı irsaliyeler sevk tarihine ve müşterisine göre; Boyahane'ye gönderilen çeki listeleri gönderim
    /// tarihinde ayrı bir satırda. Gün ara toplamı ve dönem toplamı hesaplanır.
    /// </summary>
    public async Task<DokumaShipmentReportDto> GetShipmentsAsync(DokumaShipmentReportQuery query, CancellationToken cancellationToken)
    {
        var items = db.Set<DokumaPackingItem>().AsNoTracking().Where(i => i.IsActive);
        var despatched = await items
            .Where(i => i.PackingList.Despatch != null
                && i.PackingList.Despatch.Status == DespatchStatus.Confirmed
                && i.PackingList.Despatch.DespatchDate >= query.From
                && i.PackingList.Despatch.DespatchDate <= query.To)
            .Select(i => new ShipmentItem(
                i.PackingList.Despatch!.DespatchDate,
                i.PackingList.Despatch!.CustomerId,
                i.PackingList.Despatch!.Customer.Name,
                i.PackingList.Despatch!.Id,
                i.Meters,
                i.Kilograms))
            .ToListAsync(cancellationToken);
        // Gönderim sürerken işaretli liste tarihsizdir; yalnızca tamamlanan gönderimler sayılır.
        var toBoyahane = await items
            .Where(i => i.PackingList.BoyahaneSentOn != null
                && i.PackingList.BoyahaneSentOn >= query.From
                && i.PackingList.BoyahaneSentOn <= query.To)
            .Select(i => new ShipmentItem(i.PackingList.BoyahaneSentOn!.Value, null, BoyahaneLabel, i.PackingListId, i.Meters, i.Kilograms))
            .ToListAsync(cancellationToken);

        var days = despatched.Concat(toBoyahane)
            .GroupBy(i => i.Day)
            .OrderBy(g => g.Key)
            .Select(day => new DokumaShipmentDayDto(
                day.Key,
                day.GroupBy(i => i.CustomerId)
                    .Select(g => ShipmentRow(g.Key, g.First().Name, g.ToList()))
                    // Müşteriler adına göre, Boyahane satırı en sonda.
                    .OrderBy(r => r.IsBoyahane).ThenBy(r => r.Name)
                    .ToList(),
                ShipmentRow(null, string.Empty, day.ToList())))
            .ToList();

        return new DokumaShipmentReportDto(days, ShipmentRow(null, string.Empty, despatched.Concat(toBoyahane).ToList()));
    }

    private const string BoyahaneLabel = "Boyahane'ye";

    /// <param name="CustomerId">İrsaliyenin müşterisi; Boyahane'ye gidende boş.</param>
    /// <param name="DocumentId">İrsaliye Id'si; Boyahane'ye gidende çeki listesi Id'si.</param>
    private sealed record ShipmentItem(DateOnly Day, int? CustomerId, string Name, int DocumentId, decimal Meters, decimal? Kilograms);

    private static DokumaShipmentRowDto ShipmentRow(int? customerId, string name, IReadOnlyCollection<ShipmentItem> items) =>
        new(
            customerId,
            name,
            customerId is null && name == BoyahaneLabel,
            // İrsaliye ve çeki listesi Id'leri ayrı tablolardandır; toplamda ikisi ayrı sayılır.
            items.Select(i => (i.CustomerId is null, i.DocumentId)).Distinct().Count(),
            items.Count,
            items.Sum(i => i.Meters),
            items.Sum(i => i.Kilograms ?? 0),
            items.Count(i => i.Kilograms is null));

    /// <summary>
    /// Hareketsiz stok. İplik: kalanı olan ve son çıkışı (çözgü, atkı, iade, geri alım; hiç yoksa girişi) eşikten eski lotlar.
    /// Ham top: aktif çeki listesinde olmayan ve kontrolü (kontrol edilmediyse kesimi) eşikten eski toplar. Levent: çözgüsü
    /// çekilmiş ya da tezgâha hazır, eşikten uzun süredir bekleyenler.
    /// </summary>
    public async Task<DokumaIdleStockDto> GetIdleStockAsync(DokumaIdleStockQuery query, CancellationToken cancellationToken)
    {
        var zone = timeProvider.LocalTimeZone;
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(timeProvider.GetUtcNow().UtcDateTime, zone));
        var cutoff = today.AddDays(-query.Days);

        var lots = await db.Set<DokumaYarnLot>().AsNoTracking()
            .Where(l => l.ReceiptDate <= cutoff)
            .Select(l => new
            {
                l.Id,
                l.LotNo,
                l.Yarn.YarnCode,
                YarnName = l.Yarn.Name,
                OwnerName = l.Customer != null ? l.Customer.Name : null,
                l.ReceiptDate,
                Remaining = l.Kilograms
                    - (l.Issues.Sum(i => (decimal?)i.Kilograms) ?? 0)
                    - (l.WeftIssues.Sum(i => (decimal?)i.Kilograms) ?? 0)
                    - (l.Returns.Sum(r => (decimal?)r.Kilograms) ?? 0)
                    + (l.TakeBacks.Sum(t => (decimal?)t.Kilograms) ?? 0),
                LastWarp = l.Issues.Max(i => (DateOnly?)i.IssuedOn),
                LastWeft = l.WeftIssues.Max(i => (DateOnly?)i.IssuedOn),
                LastReturn = l.Returns.Max(r => (DateOnly?)r.ReturnDate),
                LastTakeBack = l.TakeBacks.Max(t => (DateOnly?)t.TakenOn),
            })
            .Where(l => l.Remaining > 0)
            .ToListAsync(cancellationToken);
        var yarnLots = lots
            .Select(l =>
            {
                var last = new[] { l.LastWarp, l.LastWeft, l.LastReturn, l.LastTakeBack }.Max() ?? l.ReceiptDate;
                return new DokumaIdleYarnLotDto(l.Id, l.LotNo, l.YarnCode, l.YarnName, l.OwnerName, l.ReceiptDate, l.Remaining, last, today.DayNumber - last.DayNumber);
            })
            .Where(l => l.LastMovementOn <= cutoff)
            .OrderByDescending(l => l.IdleDays).ThenBy(l => l.LotNo)
            .ToList();

        var rolls = await db.Set<DokumaFabricRoll>().AsNoTracking()
            .Where(r => (r.InspectedOn ?? r.CutOn) <= cutoff
                && !db.Set<DokumaPackingItem>().Any(i => i.RollId == r.Id && i.IsActive))
            .OrderBy(r => r.InspectedOn ?? r.CutOn).ThenBy(r => r.RollNo)
            .Take(MaxIdleRolls + 1)
            .Select(r => new
            {
                r.Id,
                r.RollNo,
                r.Beam.WarpOrder.OrderLine.Article.ArticleCode,
                ArticleName = r.Beam.WarpOrder.OrderLine.Article.Name,
                r.Beam.WarpOrder.OrderLine.Order.OrderNo,
                CustomerName = r.Beam.WarpOrder.OrderLine.Order.Customer != null ? r.Beam.WarpOrder.OrderLine.Order.Customer.Name : null,
                Meters = r.InspectedMeters ?? r.Meters,
                r.Status,
                Since = r.InspectedOn ?? r.CutOn,
            })
            .ToListAsync(cancellationToken);

        var beams = await db.Set<DokumaBeam>().AsNoTracking()
            .Where(b => b.Status == BeamStatus.Ready || b.Status == BeamStatus.Warped)
            .Select(b => new
            {
                b.Id,
                b.BeamNo,
                b.WarpOrder.WarpNo,
                b.WarpOrder.OrderLine.Order.OrderNo,
                b.WarpOrder.OrderLine.Article.ArticleCode,
                b.Meters,
                b.Status,
                b.ReadyOn,
                b.SizedOn,
                b.CreatedAt,
            })
            .ToListAsync(cancellationToken);
        var idleBeams = beams
            .Select(b =>
            {
                // Çözgü tarihi leventin kaydedildiği gündür (yerel saat).
                var warped = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(b.CreatedAt, DateTimeKind.Utc), zone));
                var since = new[] { b.ReadyOn, b.SizedOn, warped }.Max()!.Value;
                return new DokumaIdleBeamDto(b.Id, b.BeamNo, b.WarpNo, b.OrderNo, b.ArticleCode, b.Meters, b.Status, since, today.DayNumber - since.DayNumber);
            })
            .Where(b => b.Since <= cutoff)
            .OrderByDescending(b => b.IdleDays).ThenBy(b => b.BeamNo)
            .ToList();

        return new DokumaIdleStockDto(
            query.Days,
            cutoff,
            yarnLots,
            rolls.Take(MaxIdleRolls)
                .Select(r => new DokumaIdleRollDto(r.Id, r.RollNo, r.ArticleCode, r.ArticleName, r.OrderNo, r.CustomerName, r.Meters, r.Status, r.Since, today.DayNumber - r.Since.DayNumber))
                .ToList(),
            idleBeams,
            rolls.Count > MaxIdleRolls);
    }

    private const int MaxIdleRolls = 1000;

    private sealed record RollRow(FabricRollStatus Status, decimal Meters, decimal? Points, string ArticleCode, string ArticleName, string LoomCode, string LoomName);

    private static DokumaQualityGroupDto Group(string code, string name, IReadOnlyCollection<RollRow> rolls)
    {
        var scored = rolls.Where(r => r.Points is not null).ToList();
        var scoredMeters = scored.Sum(r => r.Meters);
        return new DokumaQualityGroupDto(
            code,
            name,
            rolls.Count,
            rolls.Sum(r => r.Meters),
            rolls.Where(r => r.Status == FabricRollStatus.FirstQuality).Sum(r => r.Meters),
            rolls.Where(r => r.Status == FabricRollStatus.SecondQuality).Sum(r => r.Meters),
            rolls.Where(r => r.Status == FabricRollStatus.Rejected).Sum(r => r.Meters),
            scoredMeters == 0 ? null : Math.Round(scored.Sum(r => r.Points!.Value * r.Meters) / scoredMeters, 2, MidpointRounding.AwayFromZero));
    }

    private static DateTime LocalDayStartUtc(DateOnly day, TimeZoneInfo zone) => TimeZoneInfo.ConvertTimeToUtc(day.ToDateTime(TimeOnly.MinValue), zone);

    private static DateOnly Earliest(DateOnly a, DateOnly b, DateOnly c)
    {
        var first = a < b ? a : b;
        return first < c ? first : c;
    }

    private static int Whole(double minutes) => (int)Math.Round(minutes, MidpointRounding.AwayFromZero);

    private static decimal Percent(decimal part, decimal whole) => Math.Round(part * 100 / whole, 1, MidpointRounding.AwayFromZero);
}
