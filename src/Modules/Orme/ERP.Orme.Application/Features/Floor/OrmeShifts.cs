using System.Linq.Expressions;
using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Core.Application.Features.MasterData;
using ERP.Orme.Domain.Entities;
using ERP.Orme.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using static ERP.Core.Application.Features.MasterData.MasterDataRules;

namespace ERP.Orme.Application.Features.Floor;

/// <param name="StartTime">Fabrika yerel saati (SS:dd).</param>
/// <param name="EndTime">Başlangıçtan küçükse vardiya gece yarısını aşar.</param>
public sealed record OrmeShiftDto(int Id, string Code, string Name, TimeOnly StartTime, TimeOnly EndTime, bool IsActive)
{
    /// <summary>Vardiya uzunluğu (dakika).</summary>
    public int LengthMinutes => ShiftWindow.MinutesBetween(StartTime, EndTime);
}

public sealed record OrmeShiftRequest(string Code, string Name, TimeOnly StartTime, TimeOnly EndTime, bool IsActive = true);

public sealed class OrmeShiftRequestValidator : AbstractValidator<OrmeShiftRequest>
{
    public OrmeShiftRequestValidator()
    {
        RuleFor(x => x.Code).ValidCode();
        RuleFor(x => x.Name).ValidName(100);
        RuleFor(x => x.EndTime).NotEqual(x => x.StartTime).WithMessage("Bitiş saati başlangıç saatinden farklı olmalıdır.");
    }
}

public sealed class OrmeShiftService(IAppDbContext db) : MasterDataService<OrmeShift, OrmeShiftDto, OrmeShiftRequest>(db)
{
    protected override DbSet<OrmeShift> Set => Db.Set<OrmeShift>();
    protected override string Label => "Vardiya";
    protected override string CodeField => "code";

    protected override string CodeOf(OrmeShiftRequest request) => request.Code;
    protected override Expression<Func<OrmeShift, bool>> WithCode(string code) => e => e.Code == code;
    protected override Expression<Func<OrmeShift, bool>> Matches(string term) => e => e.Code.Contains(term) || e.Name.Contains(term);
    protected override IQueryable<OrmeShift> Order(IQueryable<OrmeShift> query) => query.OrderBy(e => e.StartTime).ThenBy(e => e.Code);
    protected override Expression<Func<OrmeShift, OrmeShiftDto>> ToDto => e => new OrmeShiftDto(e.Id, e.Code, e.Name, e.StartTime, e.EndTime, e.IsActive);
    protected override Expression<Func<OrmeShift, LookupDto>> ToLookup => e => new LookupDto(e.Id, e.Code, e.Name);

    protected override void Apply(OrmeShiftRequest request, OrmeShift entity, string code)
    {
        entity.Code = code;
        entity.Name = request.Name.Trim();
        entity.StartTime = request.StartTime;
        entity.EndTime = request.EndTime;
        entity.IsActive = request.IsActive;
    }

    protected override async Task<Error?> ValidateAsync(OrmeShiftRequest request, OrmeShift? existing, CancellationToken cancellationToken)
    {
        if (existing is null)
        {
            return null;
        }

        // Saatleri değişen vardiyanın molaları dışarıda kalmamalı.
        var candidate = new ShiftWindow(request.StartTime, request.EndTime);
        var breaks = await Db.Set<OrmeBreakSchedule>().Where(b => b.ShiftId == existing.Id).Select(b => new { b.Name, b.StartTime, b.EndTime }).ToListAsync(cancellationToken);
        var outside = breaks.FirstOrDefault(b => !candidate.Fits(b.StartTime, b.EndTime));
        if (outside is null)
        {
            return null;
        }

        var message = $"'{outside.Name}' molası ({outside.StartTime:HH\\:mm} - {outside.EndTime:HH\\:mm}) yeni vardiya saatlerinin dışında kalıyor.";
        return new Error(ErrorCodes.BreakOutsideShift, message, ErrorType.BusinessRule, new Dictionary<string, string[]> { ["startTime"] = [message] });
    }

    protected override async Task<string?> FindUsageAsync(int id, CancellationToken cancellationToken) =>
        await Db.Set<OrmeBreakSchedule>().AnyAsync(b => b.ShiftId == id, cancellationToken) ? "mola planları" : null;
}

public sealed record OrmeBreakGroupDto(int Id, string Code, string Name, int OperatorCount, bool IsActive);

public sealed record OrmeBreakGroupRequest(string Code, string Name, bool IsActive = true);

public sealed class OrmeBreakGroupRequestValidator : AbstractValidator<OrmeBreakGroupRequest>
{
    public OrmeBreakGroupRequestValidator()
    {
        RuleFor(x => x.Code).ValidCode();
        RuleFor(x => x.Name).ValidName(100);
    }
}

public sealed class OrmeBreakGroupService(IAppDbContext db) : MasterDataService<OrmeBreakGroup, OrmeBreakGroupDto, OrmeBreakGroupRequest>(db)
{
    protected override DbSet<OrmeBreakGroup> Set => Db.Set<OrmeBreakGroup>();
    protected override string Label => "Mola grubu";
    protected override string CodeField => "code";

    protected override string CodeOf(OrmeBreakGroupRequest request) => request.Code;
    protected override Expression<Func<OrmeBreakGroup, bool>> WithCode(string code) => e => e.Code == code;
    protected override Expression<Func<OrmeBreakGroup, bool>> Matches(string term) => e => e.Code.Contains(term) || e.Name.Contains(term);
    protected override IQueryable<OrmeBreakGroup> Order(IQueryable<OrmeBreakGroup> query) => query.OrderBy(e => e.Code);
    protected override Expression<Func<OrmeBreakGroup, OrmeBreakGroupDto>> ToDto => e => new OrmeBreakGroupDto(e.Id, e.Code, e.Name, Db.Set<OrmeOperator>().Count(o => o.BreakGroupId == e.Id), e.IsActive);
    protected override Expression<Func<OrmeBreakGroup, LookupDto>> ToLookup => e => new LookupDto(e.Id, e.Code, e.Name);

    protected override void Apply(OrmeBreakGroupRequest request, OrmeBreakGroup entity, string code)
    {
        entity.Code = code;
        entity.Name = request.Name.Trim();
        entity.IsActive = request.IsActive;
    }

    protected override async Task<string?> FindUsageAsync(int id, CancellationToken cancellationToken)
    {
        if (await Db.Set<OrmeOperator>().AnyAsync(o => o.BreakGroupId == id, cancellationToken))
        {
            return "operatörler";
        }

        return await Db.Set<OrmeBreakSchedule>().AnyAsync(b => b.BreakGroupId == id, cancellationToken) ? "mola planları" : null;
    }
}

/// <param name="BreakGroupId">Boşsa mola vardiyadaki herkes içindir.</param>
public sealed record OrmeBreakScheduleDto(
    int Id,
    string Code,
    string Name,
    int ShiftId,
    string ShiftName,
    int? BreakGroupId,
    string? BreakGroupName,
    TimeOnly StartTime,
    TimeOnly EndTime,
    bool IsActive)
{
    public int DurationMinutes => ShiftWindow.MinutesBetween(StartTime, EndTime);
}

public sealed record OrmeBreakScheduleRequest(
    string Code,
    string Name,
    int ShiftId,
    int? BreakGroupId,
    TimeOnly StartTime,
    TimeOnly EndTime,
    bool IsActive = true);

public sealed class OrmeBreakScheduleRequestValidator : AbstractValidator<OrmeBreakScheduleRequest>
{
    public OrmeBreakScheduleRequestValidator()
    {
        RuleFor(x => x.Code).ValidCode();
        RuleFor(x => x.Name).ValidName(100);
        RuleFor(x => x.ShiftId).GreaterThan(0).WithMessage("Vardiya seçilmelidir.");
        RuleFor(x => x.EndTime).NotEqual(x => x.StartTime).WithMessage("Bitiş saati başlangıç saatinden farklı olmalıdır.");
    }
}

public sealed class OrmeBreakScheduleService(IAppDbContext db) : MasterDataService<OrmeBreakSchedule, OrmeBreakScheduleDto, OrmeBreakScheduleRequest>(db)
{
    protected override DbSet<OrmeBreakSchedule> Set => Db.Set<OrmeBreakSchedule>();
    protected override string Label => "Mola planı";
    protected override string CodeField => "code";

    protected override string CodeOf(OrmeBreakScheduleRequest request) => request.Code;
    protected override Expression<Func<OrmeBreakSchedule, bool>> WithCode(string code) => e => e.Code == code;
    protected override Expression<Func<OrmeBreakSchedule, bool>> Matches(string term) => e => e.Code.Contains(term) || e.Name.Contains(term) || e.Shift.Name.Contains(term);
    protected override IQueryable<OrmeBreakSchedule> Order(IQueryable<OrmeBreakSchedule> query) => query.OrderBy(e => e.Shift.StartTime).ThenBy(e => e.Shift.Code).ThenBy(e => e.StartTime).ThenBy(e => e.Code);

    protected override Expression<Func<OrmeBreakSchedule, OrmeBreakScheduleDto>> ToDto => e => new OrmeBreakScheduleDto(
        e.Id,
        e.Code,
        e.Name,
        e.ShiftId,
        e.Shift.Name,
        e.BreakGroupId,
        e.BreakGroup!.Name,
        e.StartTime,
        e.EndTime,
        e.IsActive);

    protected override Expression<Func<OrmeBreakSchedule, LookupDto>> ToLookup => e => new LookupDto(e.Id, e.Code, e.Name);

    protected override void Apply(OrmeBreakScheduleRequest request, OrmeBreakSchedule entity, string code)
    {
        entity.Code = code;
        entity.Name = request.Name.Trim();
        entity.ShiftId = request.ShiftId;
        entity.BreakGroupId = request.BreakGroupId;
        entity.StartTime = request.StartTime;
        entity.EndTime = request.EndTime;
        entity.IsActive = request.IsActive;
    }

    protected override async Task<Error?> ValidateAsync(OrmeBreakScheduleRequest request, OrmeBreakSchedule? existing, CancellationToken cancellationToken)
    {
        if (await ReferenceValidation.RequireSelectableAsync(Db.Set<OrmeShift>(), request.ShiftId, existing?.ShiftId, "shiftId", "Vardiya", cancellationToken) is { } shiftError)
        {
            return shiftError;
        }

        if (await ReferenceValidation.RequireSelectableAsync(Db.Set<OrmeBreakGroup>(), request.BreakGroupId, existing?.BreakGroupId, "breakGroupId", "Mola grubu", cancellationToken) is { } groupError)
        {
            return groupError;
        }

        var shift = await Db.Set<OrmeShift>().AsNoTracking().SingleAsync(s => s.Id == request.ShiftId, cancellationToken);
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
public sealed record OrmeDueBreakDto(int BreakScheduleId, string Name, TimeOnly StartTime, TimeOnly EndTime, int PlannedMinutes, bool CanStartNow, string? Hint);

/// <summary>Planlı mola kuralı (Konfeksiyon ile aynı): operatörün grubuna ve saate göre zamanı gelmiş, henüz çıkılmamış mola.</summary>
public static class OrmeBreaks
{
    public static async Task<OrmeDueBreakDto?> FindDueAsync(IAppDbContext db, OrmeTabletSession session, DateTime now, FloorSessionOptions options, CancellationToken cancellationToken)
    {
        var localTime = TimeOnly.FromDateTime(options.ToLocal(now));
        var breakGroupId = await db.Set<OrmeOperator>().IgnoreQueryFilters().Where(o => o.Id == session.OperatorId).Select(o => o.BreakGroupId).SingleOrDefaultAsync(cancellationToken);

        // Mola planı sayısı azdır; zaman karşılaştırması (gece vardiyası dahil) bellekte yapılır.
        var schedules = await db.Set<OrmeBreakSchedule>().AsNoTracking()
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
            var taken = await db.Set<OrmeSessionPause>().AnyAsync(
                p => p.BreakScheduleId == candidate.schedule.Id && p.Session.OperatorId == session.OperatorId && p.StartedAt >= occurrenceStart,
                cancellationToken);
            if (taken)
            {
                continue;
            }

            return new OrmeDueBreakDto(candidate.schedule.Id, candidate.schedule.Name, candidate.schedule.StartTime, candidate.schedule.EndTime, candidate.planned, true, null);
        }

        return null;
    }
}
