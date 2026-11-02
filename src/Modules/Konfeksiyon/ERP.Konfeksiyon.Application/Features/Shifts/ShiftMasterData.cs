using ERP.Konfeksiyon.Application.Abstractions;
using System.Linq.Expressions;
using ERP.Core.Application.Common;
using ERP.Core.Application.Features.MasterData;
using ERP.Konfeksiyon.Application.Features.MasterData;
using ERP.Konfeksiyon.Domain.Entities;
using ERP.Konfeksiyon.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ERP.Konfeksiyon.Application.Features.Shifts;

/// <param name="StartTime">Fabrika yerel saati (SS:dd).</param>
/// <param name="EndTime">Başlangıçtan küçükse vardiya gece yarısını aşar.</param>
public sealed record ShiftDto(int Id, string Code, string Name, TimeOnly StartTime, TimeOnly EndTime, bool IsActive)
{
    /// <summary>Vardiya uzunluğu (dakika); gece yarısını aşan vardiyada bitiş ertesi gündedir.</summary>
    public int LengthMinutes => ShiftTimes.MinutesBetween(StartTime, EndTime);
}

public static class ShiftTimes
{
    /// <summary>İki saat arasındaki süre; bitiş başlangıçtan küçük ya da eşitse ertesi güne sarar.</summary>
    public static int MinutesBetween(TimeOnly start, TimeOnly end)
    {
        var minutes = (int)(end - start).TotalMinutes;
        return minutes > 0 ? minutes : minutes + Shift.MinutesPerDay;
    }
}

public sealed record ShiftRequest(string Code, string Name, TimeOnly StartTime, TimeOnly EndTime, bool IsActive = true);

public sealed class ShiftRequestValidator : AbstractValidator<ShiftRequest>
{
    public ShiftRequestValidator()
    {
        RuleFor(x => x.Code).ValidCode();
        RuleFor(x => x.Name).ValidName(100);
        RuleFor(x => x.EndTime)
            .NotEqual(x => x.StartTime).WithMessage("Bitiş saati başlangıç saatinden farklı olmalıdır.");
    }
}

public sealed class ShiftService(IKonfeksiyonDbContext db) : KonfeksiyonMasterDataService<Shift, ShiftDto, ShiftRequest>(db)
{
    protected override DbSet<Shift> Set => Db.Shifts;
    protected override string Label => "Vardiya";
    protected override string CodeField => "code";

    protected override string CodeOf(ShiftRequest request) => request.Code;
    protected override Expression<Func<Shift, bool>> WithCode(string code) => e => e.Code == code;

    protected override Expression<Func<Shift, bool>> Matches(string term) =>
        e => e.Code.Contains(term) || e.Name.Contains(term);

    protected override IQueryable<Shift> Order(IQueryable<Shift> query) => query.OrderBy(e => e.StartTime).ThenBy(e => e.Code);

    protected override Expression<Func<Shift, ShiftDto>> ToDto => e => new ShiftDto(
        e.Id,
        e.Code,
        e.Name,
        e.StartTime,
        e.EndTime,
        e.IsActive);

    protected override Expression<Func<Shift, LookupDto>> ToLookup => e => new LookupDto(e.Id, e.Code, e.Name);

    protected override void Apply(ShiftRequest request, Shift entity, string code)
    {
        entity.Code = code;
        entity.Name = request.Name.Trim();
        entity.StartTime = request.StartTime;
        entity.EndTime = request.EndTime;
        entity.IsActive = request.IsActive;
    }

    protected override async Task<Error?> ValidateAsync(
        ShiftRequest request, Shift? existing, CancellationToken cancellationToken)
    {
        if (existing is null)
        {
            return null;
        }

        // Saatleri değişen vardiyanın molaları dışarıda kalmamalı.
        var candidate = new Shift { StartTime = request.StartTime, EndTime = request.EndTime };
        var breaks = await Db.BreakSchedules
            .Where(b => b.ShiftId == existing.Id)
            .Select(b => new { b.Name, b.StartTime, b.EndTime })
            .ToListAsync(cancellationToken);
        var outside = breaks.FirstOrDefault(b => !BreakSchedule.FitsIn(candidate, b.StartTime, b.EndTime));
        if (outside is null)
        {
            return null;
        }

        var message = $"'{outside.Name}' molası ({outside.StartTime:HH\\:mm} - {outside.EndTime:HH\\:mm}) yeni vardiya saatlerinin dışında kalıyor.";
        return new Error(
            ErrorCodes.BreakOutsideShift, message, ErrorType.BusinessRule,
            new Dictionary<string, string[]> { ["startTime"] = [message] });
    }

    protected override async Task<string?> FindUsageAsync(int id, CancellationToken cancellationToken) =>
        await Db.BreakSchedules.AnyAsync(b => b.ShiftId == id, cancellationToken) ? "mola planları" : null;
}

public sealed record BreakGroupDto(int Id, string Code, string Name, int OperatorCount, bool IsActive);

public sealed record BreakGroupRequest(string Code, string Name, bool IsActive = true);

public sealed class BreakGroupRequestValidator : AbstractValidator<BreakGroupRequest>
{
    public BreakGroupRequestValidator()
    {
        RuleFor(x => x.Code).ValidCode();
        RuleFor(x => x.Name).ValidName(100);
    }
}

public sealed class BreakGroupService(IKonfeksiyonDbContext db)
    : KonfeksiyonMasterDataService<BreakGroup, BreakGroupDto, BreakGroupRequest>(db)
{
    protected override DbSet<BreakGroup> Set => Db.BreakGroups;
    protected override string Label => "Mola grubu";
    protected override string CodeField => "code";

    protected override string CodeOf(BreakGroupRequest request) => request.Code;
    protected override Expression<Func<BreakGroup, bool>> WithCode(string code) => e => e.Code == code;

    protected override Expression<Func<BreakGroup, bool>> Matches(string term) =>
        e => e.Code.Contains(term) || e.Name.Contains(term);

    protected override IQueryable<BreakGroup> Order(IQueryable<BreakGroup> query) => query.OrderBy(e => e.Code);

    protected override Expression<Func<BreakGroup, BreakGroupDto>> ToDto => e => new BreakGroupDto(
        e.Id, e.Code, e.Name, Db.Operators.Count(o => o.BreakGroupId == e.Id), e.IsActive);

    protected override Expression<Func<BreakGroup, LookupDto>> ToLookup => e => new LookupDto(e.Id, e.Code, e.Name);

    protected override void Apply(BreakGroupRequest request, BreakGroup entity, string code)
    {
        entity.Code = code;
        entity.Name = request.Name.Trim();
        entity.IsActive = request.IsActive;
    }

    protected override async Task<string?> FindUsageAsync(int id, CancellationToken cancellationToken)
    {
        if (await Db.Operators.AnyAsync(o => o.BreakGroupId == id, cancellationToken))
        {
            return "operatörler";
        }

        return await Db.BreakSchedules.AnyAsync(b => b.BreakGroupId == id, cancellationToken) ? "mola planları" : null;
    }
}

/// <param name="BreakGroupId">Boşsa mola vardiyadaki herkes içindir.</param>
public sealed record BreakScheduleDto(
    int Id,
    string Code,
    string Name,
    int ShiftId,
    string ShiftName,
    int? BreakGroupId,
    string? BreakGroupName,
    TimeOnly StartTime,
    TimeOnly EndTime,
    BreakPolicy Policy,
    bool IsActive)
{
    public int DurationMinutes => ShiftTimes.MinutesBetween(StartTime, EndTime);
}

public sealed record BreakScheduleRequest(
    string Code,
    string Name,
    int ShiftId,
    int? BreakGroupId,
    TimeOnly StartTime,
    TimeOnly EndTime,
    BreakPolicy Policy,
    bool IsActive = true);

public sealed class BreakScheduleRequestValidator : AbstractValidator<BreakScheduleRequest>
{
    public BreakScheduleRequestValidator()
    {
        RuleFor(x => x.Code).ValidCode();
        RuleFor(x => x.Name).ValidName(100);
        RuleFor(x => x.ShiftId).GreaterThan(0).WithMessage("Vardiya seçilmelidir.");
        RuleFor(x => x.Policy).IsInEnum().WithMessage("Geçersiz mola politikası.");
        RuleFor(x => x.EndTime)
            .NotEqual(x => x.StartTime).WithMessage("Bitiş saati başlangıç saatinden farklı olmalıdır.");
    }
}

public sealed class BreakScheduleService(IKonfeksiyonDbContext db)
    : KonfeksiyonMasterDataService<BreakSchedule, BreakScheduleDto, BreakScheduleRequest>(db)
{
    protected override DbSet<BreakSchedule> Set => Db.BreakSchedules;
    protected override string Label => "Mola planı";
    protected override string CodeField => "code";

    protected override string CodeOf(BreakScheduleRequest request) => request.Code;
    protected override Expression<Func<BreakSchedule, bool>> WithCode(string code) => e => e.Code == code;

    protected override Expression<Func<BreakSchedule, bool>> Matches(string term) =>
        e => e.Code.Contains(term) || e.Name.Contains(term) || e.Shift.Name.Contains(term);

    protected override IQueryable<BreakSchedule> Order(IQueryable<BreakSchedule> query) =>
        query.OrderBy(e => e.Shift.StartTime).ThenBy(e => e.Shift.Code).ThenBy(e => e.StartTime).ThenBy(e => e.Code);

    protected override Expression<Func<BreakSchedule, BreakScheduleDto>> ToDto => e => new BreakScheduleDto(
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

    protected override Expression<Func<BreakSchedule, LookupDto>> ToLookup => e => new LookupDto(e.Id, e.Code, e.Name);

    protected override void Apply(BreakScheduleRequest request, BreakSchedule entity, string code)
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

    protected override async Task<Error?> ValidateAsync(
        BreakScheduleRequest request, BreakSchedule? existing, CancellationToken cancellationToken)
    {
        var shiftError = await ReferenceValidation.RequireSelectableAsync(
            Db.Shifts, request.ShiftId, existing?.ShiftId, "shiftId", "Vardiya", cancellationToken);
        if (shiftError is not null)
        {
            return shiftError;
        }

        var groupError = await ReferenceValidation.RequireSelectableAsync(
            Db.BreakGroups, request.BreakGroupId, existing?.BreakGroupId, "breakGroupId", "Mola grubu", cancellationToken);
        if (groupError is not null)
        {
            return groupError;
        }

        var shift = await Db.Shifts.AsNoTracking().SingleAsync(s => s.Id == request.ShiftId, cancellationToken);
        if (BreakSchedule.FitsIn(shift, request.StartTime, request.EndTime))
        {
            return null;
        }

        var message =
            $"Mola, vardiya saatlerinin ({shift.StartTime:HH\\:mm} - {shift.EndTime:HH\\:mm}) içinde olmalı ve " +
            "başlangıcı bitişinden önce gelmelidir.";
        return new Error(
            ErrorCodes.BreakOutsideShift, message, ErrorType.BusinessRule,
            new Dictionary<string, string[]> { ["startTime"] = [message] });
    }
}
