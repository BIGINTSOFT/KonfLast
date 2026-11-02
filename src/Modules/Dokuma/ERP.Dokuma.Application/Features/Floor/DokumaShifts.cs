using System.Linq.Expressions;
using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Core.Application.Features.MasterData;
using ERP.Dokuma.Domain.Entities;
using ERP.Dokuma.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using static ERP.Core.Application.Features.MasterData.MasterDataRules;

namespace ERP.Dokuma.Application.Features.Floor;

/// <param name="StartTime">Fabrika yerel saati (SS:dd).</param>
/// <param name="EndTime">Başlangıçtan küçükse vardiya gece yarısını aşar.</param>
public sealed record DokumaShiftDto(int Id, string Code, string Name, TimeOnly StartTime, TimeOnly EndTime, bool IsActive)
{
    /// <summary>Vardiya uzunluğu (dakika).</summary>
    public int LengthMinutes => ShiftWindow.MinutesBetween(StartTime, EndTime);
}

public sealed record DokumaShiftRequest(string Code, string Name, TimeOnly StartTime, TimeOnly EndTime, bool IsActive = true);

public sealed class DokumaShiftRequestValidator : AbstractValidator<DokumaShiftRequest>
{
    public DokumaShiftRequestValidator()
    {
        RuleFor(x => x.Code).ValidCode();
        RuleFor(x => x.Name).ValidName(100);
        RuleFor(x => x.EndTime).NotEqual(x => x.StartTime).WithMessage("Bitiş saati başlangıç saatinden farklı olmalıdır.");
    }
}

public sealed class DokumaShiftService(IAppDbContext db) : MasterDataService<DokumaShift, DokumaShiftDto, DokumaShiftRequest>(db)
{
    protected override DbSet<DokumaShift> Set => Db.Set<DokumaShift>();
    protected override string Label => "Vardiya";
    protected override string CodeField => "code";

    protected override string CodeOf(DokumaShiftRequest request) => request.Code;
    protected override Expression<Func<DokumaShift, bool>> WithCode(string code) => e => e.Code == code;
    protected override Expression<Func<DokumaShift, bool>> Matches(string term) => e => e.Code.Contains(term) || e.Name.Contains(term);
    protected override IQueryable<DokumaShift> Order(IQueryable<DokumaShift> query) => query.OrderBy(e => e.StartTime).ThenBy(e => e.Code);
    protected override Expression<Func<DokumaShift, DokumaShiftDto>> ToDto => e => new DokumaShiftDto(e.Id, e.Code, e.Name, e.StartTime, e.EndTime, e.IsActive);
    protected override Expression<Func<DokumaShift, LookupDto>> ToLookup => e => new LookupDto(e.Id, e.Code, e.Name);

    protected override void Apply(DokumaShiftRequest request, DokumaShift entity, string code)
    {
        entity.Code = code;
        entity.Name = request.Name.Trim();
        entity.StartTime = request.StartTime;
        entity.EndTime = request.EndTime;
        entity.IsActive = request.IsActive;
    }

    protected override async Task<Error?> ValidateAsync(DokumaShiftRequest request, DokumaShift? existing, CancellationToken cancellationToken)
    {
        if (existing is null)
        {
            return null;
        }

        // Saatleri değişen vardiyanın molaları dışarıda kalmamalı.
        var candidate = new ShiftWindow(request.StartTime, request.EndTime);
        var breaks = await Db.Set<DokumaBreakSchedule>().Where(b => b.ShiftId == existing.Id).Select(b => new { b.Name, b.StartTime, b.EndTime }).ToListAsync(cancellationToken);
        var outside = breaks.FirstOrDefault(b => !candidate.Fits(b.StartTime, b.EndTime));
        if (outside is null)
        {
            return null;
        }

        var message = $"'{outside.Name}' molası ({outside.StartTime:HH\\:mm} - {outside.EndTime:HH\\:mm}) yeni vardiya saatlerinin dışında kalıyor.";
        return new Error(ErrorCodes.BreakOutsideShift, message, ErrorType.BusinessRule, new Dictionary<string, string[]> { ["startTime"] = [message] });
    }

    protected override async Task<string?> FindUsageAsync(int id, CancellationToken cancellationToken) =>
        await Db.Set<DokumaBreakSchedule>().AnyAsync(b => b.ShiftId == id, cancellationToken) ? "mola planları" : null;
}

public sealed record DokumaBreakGroupDto(int Id, string Code, string Name, int OperatorCount, bool IsActive);

public sealed record DokumaBreakGroupRequest(string Code, string Name, bool IsActive = true);

public sealed class DokumaBreakGroupRequestValidator : AbstractValidator<DokumaBreakGroupRequest>
{
    public DokumaBreakGroupRequestValidator()
    {
        RuleFor(x => x.Code).ValidCode();
        RuleFor(x => x.Name).ValidName(100);
    }
}

public sealed class DokumaBreakGroupService(IAppDbContext db) : MasterDataService<DokumaBreakGroup, DokumaBreakGroupDto, DokumaBreakGroupRequest>(db)
{
    protected override DbSet<DokumaBreakGroup> Set => Db.Set<DokumaBreakGroup>();
    protected override string Label => "Mola grubu";
    protected override string CodeField => "code";

    protected override string CodeOf(DokumaBreakGroupRequest request) => request.Code;
    protected override Expression<Func<DokumaBreakGroup, bool>> WithCode(string code) => e => e.Code == code;
    protected override Expression<Func<DokumaBreakGroup, bool>> Matches(string term) => e => e.Code.Contains(term) || e.Name.Contains(term);
    protected override IQueryable<DokumaBreakGroup> Order(IQueryable<DokumaBreakGroup> query) => query.OrderBy(e => e.Code);
    protected override Expression<Func<DokumaBreakGroup, DokumaBreakGroupDto>> ToDto => e => new DokumaBreakGroupDto(e.Id, e.Code, e.Name, Db.Set<DokumaOperator>().Count(o => o.BreakGroupId == e.Id), e.IsActive);
    protected override Expression<Func<DokumaBreakGroup, LookupDto>> ToLookup => e => new LookupDto(e.Id, e.Code, e.Name);

    protected override void Apply(DokumaBreakGroupRequest request, DokumaBreakGroup entity, string code)
    {
        entity.Code = code;
        entity.Name = request.Name.Trim();
        entity.IsActive = request.IsActive;
    }

    protected override async Task<string?> FindUsageAsync(int id, CancellationToken cancellationToken)
    {
        if (await Db.Set<DokumaOperator>().AnyAsync(o => o.BreakGroupId == id, cancellationToken))
        {
            return "operatörler";
        }

        return await Db.Set<DokumaBreakSchedule>().AnyAsync(b => b.BreakGroupId == id, cancellationToken) ? "mola planları" : null;
    }
}

/// <param name="BreakGroupId">Boşsa mola vardiyadaki herkes içindir.</param>
public sealed record DokumaBreakScheduleDto(
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

public sealed record DokumaBreakScheduleRequest(
    string Code,
    string Name,
    int ShiftId,
    int? BreakGroupId,
    TimeOnly StartTime,
    TimeOnly EndTime,
    bool IsActive = true);

public sealed class DokumaBreakScheduleRequestValidator : AbstractValidator<DokumaBreakScheduleRequest>
{
    public DokumaBreakScheduleRequestValidator()
    {
        RuleFor(x => x.Code).ValidCode();
        RuleFor(x => x.Name).ValidName(100);
        RuleFor(x => x.ShiftId).GreaterThan(0).WithMessage("Vardiya seçilmelidir.");
        RuleFor(x => x.EndTime).NotEqual(x => x.StartTime).WithMessage("Bitiş saati başlangıç saatinden farklı olmalıdır.");
    }
}

public sealed class DokumaBreakScheduleService(IAppDbContext db) : MasterDataService<DokumaBreakSchedule, DokumaBreakScheduleDto, DokumaBreakScheduleRequest>(db)
{
    protected override DbSet<DokumaBreakSchedule> Set => Db.Set<DokumaBreakSchedule>();
    protected override string Label => "Mola planı";
    protected override string CodeField => "code";

    protected override string CodeOf(DokumaBreakScheduleRequest request) => request.Code;
    protected override Expression<Func<DokumaBreakSchedule, bool>> WithCode(string code) => e => e.Code == code;
    protected override Expression<Func<DokumaBreakSchedule, bool>> Matches(string term) => e => e.Code.Contains(term) || e.Name.Contains(term) || e.Shift.Name.Contains(term);
    protected override IQueryable<DokumaBreakSchedule> Order(IQueryable<DokumaBreakSchedule> query) => query.OrderBy(e => e.Shift.StartTime).ThenBy(e => e.Shift.Code).ThenBy(e => e.StartTime).ThenBy(e => e.Code);

    protected override Expression<Func<DokumaBreakSchedule, DokumaBreakScheduleDto>> ToDto => e => new DokumaBreakScheduleDto(
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

    protected override Expression<Func<DokumaBreakSchedule, LookupDto>> ToLookup => e => new LookupDto(e.Id, e.Code, e.Name);

    protected override void Apply(DokumaBreakScheduleRequest request, DokumaBreakSchedule entity, string code)
    {
        entity.Code = code;
        entity.Name = request.Name.Trim();
        entity.ShiftId = request.ShiftId;
        entity.BreakGroupId = request.BreakGroupId;
        entity.StartTime = request.StartTime;
        entity.EndTime = request.EndTime;
        entity.IsActive = request.IsActive;
    }

    protected override async Task<Error?> ValidateAsync(DokumaBreakScheduleRequest request, DokumaBreakSchedule? existing, CancellationToken cancellationToken)
    {
        if (await ReferenceValidation.RequireSelectableAsync(Db.Set<DokumaShift>(), request.ShiftId, existing?.ShiftId, "shiftId", "Vardiya", cancellationToken) is { } shiftError)
        {
            return shiftError;
        }

        if (await ReferenceValidation.RequireSelectableAsync(Db.Set<DokumaBreakGroup>(), request.BreakGroupId, existing?.BreakGroupId, "breakGroupId", "Mola grubu", cancellationToken) is { } groupError)
        {
            return groupError;
        }

        var shift = await Db.Set<DokumaShift>().AsNoTracking().SingleAsync(s => s.Id == request.ShiftId, cancellationToken);
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
public sealed record DokumaDueBreakDto(int BreakScheduleId, string Name, TimeOnly StartTime, TimeOnly EndTime, int PlannedMinutes, bool CanStartNow, string? Hint);

/// <summary>Planlı mola kuralı (Konfeksiyon ile aynı): operatörün grubuna ve saate göre zamanı gelmiş, henüz çıkılmamış mola.</summary>
public static class DokumaBreaks
{
    public static async Task<DokumaDueBreakDto?> FindDueAsync(IAppDbContext db, DokumaTabletSession session, DateTime now, FloorSessionOptions options, CancellationToken cancellationToken)
    {
        var localTime = TimeOnly.FromDateTime(options.ToLocal(now));
        var breakGroupId = await db.Set<DokumaOperator>().IgnoreQueryFilters().Where(o => o.Id == session.OperatorId).Select(o => o.BreakGroupId).SingleOrDefaultAsync(cancellationToken);

        // Mola planı sayısı azdır; zaman karşılaştırması (gece vardiyası dahil) bellekte yapılır.
        var schedules = await db.Set<DokumaBreakSchedule>().AsNoTracking()
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
            var taken = await db.Set<DokumaSessionPause>().AnyAsync(
                p => p.BreakScheduleId == candidate.schedule.Id && p.Session.OperatorId == session.OperatorId && p.StartedAt >= occurrenceStart,
                cancellationToken);
            if (taken)
            {
                continue;
            }

            return new DokumaDueBreakDto(candidate.schedule.Id, candidate.schedule.Name, candidate.schedule.StartTime, candidate.schedule.EndTime, candidate.planned, true, null);
        }

        return null;
    }
}
