using System.Linq.Expressions;
using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Core.Application.Features.MasterData;
using ERP.Boyahane.Domain.Entities;
using ERP.Boyahane.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using static ERP.Core.Application.Features.MasterData.MasterDataRules;

namespace ERP.Boyahane.Application.Features.Floor;

/// <param name="StartTime">Fabrika yerel saati (SS:dd).</param>
/// <param name="EndTime">Başlangıçtan küçükse vardiya gece yarısını aşar.</param>
public sealed record BoyahaneShiftDto(int Id, string Code, string Name, TimeOnly StartTime, TimeOnly EndTime, bool IsActive)
{
    /// <summary>Vardiya uzunluğu (dakika).</summary>
    public int LengthMinutes => ShiftWindow.MinutesBetween(StartTime, EndTime);
}

public sealed record BoyahaneShiftRequest(string Code, string Name, TimeOnly StartTime, TimeOnly EndTime, bool IsActive = true);

public sealed class BoyahaneShiftRequestValidator : AbstractValidator<BoyahaneShiftRequest>
{
    public BoyahaneShiftRequestValidator()
    {
        RuleFor(x => x.Code).ValidCode();
        RuleFor(x => x.Name).ValidName(100);
        RuleFor(x => x.EndTime).NotEqual(x => x.StartTime).WithMessage("Bitiş saati başlangıç saatinden farklı olmalıdır.");
    }
}

public sealed class BoyahaneShiftService(IAppDbContext db) : MasterDataService<BoyahaneShift, BoyahaneShiftDto, BoyahaneShiftRequest>(db)
{
    protected override DbSet<BoyahaneShift> Set => Db.Set<BoyahaneShift>();
    protected override string Label => "Vardiya";
    protected override string CodeField => "code";

    protected override string CodeOf(BoyahaneShiftRequest request) => request.Code;
    protected override Expression<Func<BoyahaneShift, bool>> WithCode(string code) => e => e.Code == code;
    protected override Expression<Func<BoyahaneShift, bool>> Matches(string term) => e => e.Code.Contains(term) || e.Name.Contains(term);
    protected override IQueryable<BoyahaneShift> Order(IQueryable<BoyahaneShift> query) => query.OrderBy(e => e.StartTime).ThenBy(e => e.Code);
    protected override Expression<Func<BoyahaneShift, BoyahaneShiftDto>> ToDto => e => new BoyahaneShiftDto(e.Id, e.Code, e.Name, e.StartTime, e.EndTime, e.IsActive);
    protected override Expression<Func<BoyahaneShift, LookupDto>> ToLookup => e => new LookupDto(e.Id, e.Code, e.Name);

    protected override void Apply(BoyahaneShiftRequest request, BoyahaneShift entity, string code)
    {
        entity.Code = code;
        entity.Name = request.Name.Trim();
        entity.StartTime = request.StartTime;
        entity.EndTime = request.EndTime;
        entity.IsActive = request.IsActive;
    }

    protected override async Task<Error?> ValidateAsync(BoyahaneShiftRequest request, BoyahaneShift? existing, CancellationToken cancellationToken)
    {
        if (existing is null)
        {
            return null;
        }

        // Saatleri değişen vardiyanın molaları dışarıda kalmamalı.
        var candidate = new ShiftWindow(request.StartTime, request.EndTime);
        var breaks = await Db.Set<BoyahaneBreakSchedule>().Where(b => b.ShiftId == existing.Id).Select(b => new { b.Name, b.StartTime, b.EndTime }).ToListAsync(cancellationToken);
        var outside = breaks.FirstOrDefault(b => !candidate.Fits(b.StartTime, b.EndTime));
        if (outside is null)
        {
            return null;
        }

        var message = $"'{outside.Name}' molası ({outside.StartTime:HH\\:mm} - {outside.EndTime:HH\\:mm}) yeni vardiya saatlerinin dışında kalıyor.";
        return new Error(ErrorCodes.BreakOutsideShift, message, ErrorType.BusinessRule, new Dictionary<string, string[]> { ["startTime"] = [message] });
    }

    protected override async Task<string?> FindUsageAsync(int id, CancellationToken cancellationToken) =>
        await Db.Set<BoyahaneBreakSchedule>().AnyAsync(b => b.ShiftId == id, cancellationToken) ? "mola planları" : null;
}

public sealed record BoyahaneBreakGroupDto(int Id, string Code, string Name, int OperatorCount, bool IsActive);

public sealed record BoyahaneBreakGroupRequest(string Code, string Name, bool IsActive = true);

public sealed class BoyahaneBreakGroupRequestValidator : AbstractValidator<BoyahaneBreakGroupRequest>
{
    public BoyahaneBreakGroupRequestValidator()
    {
        RuleFor(x => x.Code).ValidCode();
        RuleFor(x => x.Name).ValidName(100);
    }
}

public sealed class BoyahaneBreakGroupService(IAppDbContext db) : MasterDataService<BoyahaneBreakGroup, BoyahaneBreakGroupDto, BoyahaneBreakGroupRequest>(db)
{
    protected override DbSet<BoyahaneBreakGroup> Set => Db.Set<BoyahaneBreakGroup>();
    protected override string Label => "Mola grubu";
    protected override string CodeField => "code";

    protected override string CodeOf(BoyahaneBreakGroupRequest request) => request.Code;
    protected override Expression<Func<BoyahaneBreakGroup, bool>> WithCode(string code) => e => e.Code == code;
    protected override Expression<Func<BoyahaneBreakGroup, bool>> Matches(string term) => e => e.Code.Contains(term) || e.Name.Contains(term);
    protected override IQueryable<BoyahaneBreakGroup> Order(IQueryable<BoyahaneBreakGroup> query) => query.OrderBy(e => e.Code);
    protected override Expression<Func<BoyahaneBreakGroup, BoyahaneBreakGroupDto>> ToDto => e => new BoyahaneBreakGroupDto(e.Id, e.Code, e.Name, Db.Set<BoyahaneOperator>().Count(o => o.BreakGroupId == e.Id), e.IsActive);
    protected override Expression<Func<BoyahaneBreakGroup, LookupDto>> ToLookup => e => new LookupDto(e.Id, e.Code, e.Name);

    protected override void Apply(BoyahaneBreakGroupRequest request, BoyahaneBreakGroup entity, string code)
    {
        entity.Code = code;
        entity.Name = request.Name.Trim();
        entity.IsActive = request.IsActive;
    }

    protected override async Task<string?> FindUsageAsync(int id, CancellationToken cancellationToken)
    {
        if (await Db.Set<BoyahaneOperator>().AnyAsync(o => o.BreakGroupId == id, cancellationToken))
        {
            return "operatörler";
        }

        return await Db.Set<BoyahaneBreakSchedule>().AnyAsync(b => b.BreakGroupId == id, cancellationToken) ? "mola planları" : null;
    }
}

/// <param name="BreakGroupId">Boşsa mola vardiyadaki herkes içindir.</param>
public sealed record BoyahaneBreakScheduleDto(
    int Id,
    string Code,
    string Name,
    int ShiftId,
    string ShiftName,
    int? BreakGroupId,
    string? BreakGroupName,
    TimeOnly StartTime,
    TimeOnly EndTime,
    BoyahaneBreakPolicy Policy,
    bool IsActive)
{
    public int DurationMinutes => ShiftWindow.MinutesBetween(StartTime, EndTime);
}

public sealed record BoyahaneBreakScheduleRequest(
    string Code,
    string Name,
    int ShiftId,
    int? BreakGroupId,
    TimeOnly StartTime,
    TimeOnly EndTime,
    BoyahaneBreakPolicy Policy,
    bool IsActive = true);

public sealed class BoyahaneBreakScheduleRequestValidator : AbstractValidator<BoyahaneBreakScheduleRequest>
{
    public BoyahaneBreakScheduleRequestValidator()
    {
        RuleFor(x => x.Code).ValidCode();
        RuleFor(x => x.Name).ValidName(100);
        RuleFor(x => x.ShiftId).GreaterThan(0).WithMessage("Vardiya seçilmelidir.");
        RuleFor(x => x.Policy).IsInEnum().WithMessage("Geçersiz mola politikası.");
        RuleFor(x => x.EndTime).NotEqual(x => x.StartTime).WithMessage("Bitiş saati başlangıç saatinden farklı olmalıdır.");
    }
}

public sealed class BoyahaneBreakScheduleService(IAppDbContext db) : MasterDataService<BoyahaneBreakSchedule, BoyahaneBreakScheduleDto, BoyahaneBreakScheduleRequest>(db)
{
    protected override DbSet<BoyahaneBreakSchedule> Set => Db.Set<BoyahaneBreakSchedule>();
    protected override string Label => "Mola planı";
    protected override string CodeField => "code";

    protected override string CodeOf(BoyahaneBreakScheduleRequest request) => request.Code;
    protected override Expression<Func<BoyahaneBreakSchedule, bool>> WithCode(string code) => e => e.Code == code;
    protected override Expression<Func<BoyahaneBreakSchedule, bool>> Matches(string term) => e => e.Code.Contains(term) || e.Name.Contains(term) || e.Shift.Name.Contains(term);
    protected override IQueryable<BoyahaneBreakSchedule> Order(IQueryable<BoyahaneBreakSchedule> query) => query.OrderBy(e => e.Shift.StartTime).ThenBy(e => e.Shift.Code).ThenBy(e => e.StartTime).ThenBy(e => e.Code);

    protected override Expression<Func<BoyahaneBreakSchedule, BoyahaneBreakScheduleDto>> ToDto => e => new BoyahaneBreakScheduleDto(
        e.Id,
        e.Code,
        e.Name,
        e.ShiftId,
        e.Shift.Name,
        e.BreakGroupId,
        e.BreakGroup!.Name,
        e.StartTime,
        e.EndTime,
        e.Policy,
        e.IsActive);

    protected override Expression<Func<BoyahaneBreakSchedule, LookupDto>> ToLookup => e => new LookupDto(e.Id, e.Code, e.Name);

    protected override void Apply(BoyahaneBreakScheduleRequest request, BoyahaneBreakSchedule entity, string code)
    {
        entity.Code = code;
        entity.Name = request.Name.Trim();
        entity.ShiftId = request.ShiftId;
        entity.BreakGroupId = request.BreakGroupId;
        entity.StartTime = request.StartTime;
        entity.EndTime = request.EndTime;
        entity.Policy = request.Policy;
        entity.IsActive = request.IsActive;
    }

    protected override async Task<Error?> ValidateAsync(BoyahaneBreakScheduleRequest request, BoyahaneBreakSchedule? existing, CancellationToken cancellationToken)
    {
        if (await ReferenceValidation.RequireSelectableAsync(Db.Set<BoyahaneShift>(), request.ShiftId, existing?.ShiftId, "shiftId", "Vardiya", cancellationToken) is { } shiftError)
        {
            return shiftError;
        }

        if (await ReferenceValidation.RequireSelectableAsync(Db.Set<BoyahaneBreakGroup>(), request.BreakGroupId, existing?.BreakGroupId, "breakGroupId", "Mola grubu", cancellationToken) is { } groupError)
        {
            return groupError;
        }

        var shift = await Db.Set<BoyahaneShift>().AsNoTracking().SingleAsync(s => s.Id == request.ShiftId, cancellationToken);
        if (new ShiftWindow(shift.StartTime, shift.EndTime).Fits(request.StartTime, request.EndTime))
        {
            return null;
        }

        var message = $"Mola, vardiya saatlerinin ({shift.StartTime:HH\\:mm} - {shift.EndTime:HH\\:mm}) içinde olmalı ve başlangıcı bitişinden önce gelmelidir.";
        return new Error(ErrorCodes.BreakOutsideShift, message, ErrorType.BusinessRule, new Dictionary<string, string[]> { ["startTime"] = [message] });
    }
}

/// <summary>Operatör için şu an zamanı gelmiş planlı mola.</summary>
/// <param name="CanStartNow">Mola şimdi başlatılabilir mi.</param>
/// <param name="Hint">Başlatılamıyorsa operatöre gösterilecek açıklama.</param>
public sealed record BoyahaneDueBreakDto(int BreakScheduleId, string Name, TimeOnly StartTime, TimeOnly EndTime, int PlannedMinutes, bool CanStartNow, string? Hint);

/// <summary>Planlı mola kuralı (Konfeksiyon ile aynı): operatörün grubuna ve saate göre zamanı gelmiş, henüz çıkılmamış mola.</summary>
public static class BoyahaneBreaks
{
    public static async Task<BoyahaneDueBreakDto?> FindDueAsync(IAppDbContext db, BoyahaneTabletSession session, DateTime now, FloorSessionOptions options, CancellationToken cancellationToken)
    {
        var localTime = TimeOnly.FromDateTime(options.ToLocal(now));
        var breakGroupId = await db.Set<BoyahaneOperator>().IgnoreQueryFilters().Where(o => o.Id == session.OperatorId).Select(o => o.BreakGroupId).SingleOrDefaultAsync(cancellationToken);

        // Mola planı sayısı azdır; zaman karşılaştırması (gece vardiyası dahil) bellekte yapılır.
        var schedules = await db.Set<BoyahaneBreakSchedule>().AsNoTracking()
            .Include(b => b.Shift)
            .Where(b => b.IsActive && b.Shift.IsActive && (b.BreakGroupId == null || b.BreakGroupId == breakGroupId))
            .ToListAsync(cancellationToken);

        var due = schedules
            .Select(schedule =>
            {
                var shift = new ShiftWindow(schedule.Shift.StartTime, schedule.Shift.EndTime);
                return new
                {
                    schedule,
                    sinceShiftStart = shift.OffsetOf(localTime),
                    from = shift.OffsetOf(schedule.StartTime),
                    planned = ShiftWindow.MinutesBetween(schedule.StartTime, schedule.EndTime),
                };
            })
            .Where(x => x.sinceShiftStart >= x.from && x.sinceShiftStart <= x.from + x.planned + options.BreakLateStartMinutes)
            // Gruba özel mola, herkese açık moladan önce gelir.
            .OrderByDescending(x => x.schedule.BreakGroupId.HasValue)
            .ThenBy(x => x.from)
            .ToList();

        foreach (var candidate in due)
        {
            // Bugünkü bu mola başlangıcından beri operatör bu molaya çıktı mı?
            var occurrenceStart = now.AddMinutes(-(candidate.sinceShiftStart - candidate.from) - 1);
            var taken = await db.Set<BoyahaneSessionPause>().AnyAsync(
                p => p.BreakScheduleId == candidate.schedule.Id && p.Session.OperatorId == session.OperatorId && p.StartedAt >= occurrenceStart,
                cancellationToken);
            if (taken)
            {
                continue;
            }

            var working = candidate.schedule.Policy != BoyahaneBreakPolicy.Immediate
                && await db.Set<BoyahaneBatchStep>().AnyAsync(
                    s => s.Status == BatchStepStatus.InProgress && s.TabletId == session.TabletId && s.StartedByOperatorId == session.OperatorId,
                    cancellationToken);
            return new BoyahaneDueBreakDto(
                candidate.schedule.Id, candidate.schedule.Name, candidate.schedule.StartTime, candidate.schedule.EndTime, candidate.planned,
                !working, working ? "Molaya çıkmadan önce süren adımı bitirin." : null);
        }

        return null;
    }
}
