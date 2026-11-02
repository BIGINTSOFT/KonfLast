using System.Linq.Expressions;
using ERP.Boyahane.Domain.Entities;
using ERP.Boyahane.Domain.Enums;
using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Core.Application.Features.MasterData;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using static ERP.Core.Application.Features.MasterData.MasterDataRules;

namespace ERP.Boyahane.Application.Features.Operations;

public sealed record BoyahaneOperationDto(int Id, string Code, string Name, string? Description, OperationBehavior Behavior, bool NeedsMachine, decimal? StandardMinutes, bool IsActive);

public sealed record BoyahaneOperationRequest(
    string Code,
    string Name,
    string? Description = null,
    OperationBehavior Behavior = OperationBehavior.Standard,
    bool NeedsMachine = true,
    decimal? StandardMinutes = null,
    bool IsActive = true);

public sealed class BoyahaneOperationRequestValidator : AbstractValidator<BoyahaneOperationRequest>
{
    public BoyahaneOperationRequestValidator()
    {
        RuleFor(x => x.Code).ValidCode();
        RuleFor(x => x.Name).ValidName(100);
        RuleFor(x => x.Description).OptionalText(500);
        RuleFor(x => x.Behavior).IsInEnum().WithMessage("Geçerli bir davranış seçin.");
        RuleFor(x => x.StandardMinutes).InclusiveBetween(0, 100_000).WithMessage("Standart süre geçersiz.").When(x => x.StandardMinutes is not null);
    }
}

public sealed class BoyahaneOperationService(IAppDbContext db) : MasterDataService<BoyahaneOperation, BoyahaneOperationDto, BoyahaneOperationRequest>(db)
{
    protected override DbSet<BoyahaneOperation> Set => Db.Set<BoyahaneOperation>();
    protected override string Label => "Operasyon";
    protected override string CodeField => "code";

    protected override string CodeOf(BoyahaneOperationRequest request) => request.Code;
    protected override Expression<Func<BoyahaneOperation, bool>> WithCode(string code) => e => e.Code == code;
    protected override Expression<Func<BoyahaneOperation, bool>> Matches(string term) => e => e.Code.Contains(term) || e.Name.Contains(term);
    protected override IQueryable<BoyahaneOperation> Order(IQueryable<BoyahaneOperation> query) => query.OrderBy(e => e.Code);
    protected override Expression<Func<BoyahaneOperation, BoyahaneOperationDto>> ToDto => e => new BoyahaneOperationDto(e.Id, e.Code, e.Name, e.Description, e.Behavior, e.NeedsMachine, e.StandardMinutes, e.IsActive);
    protected override Expression<Func<BoyahaneOperation, LookupDto>> ToLookup => e => new LookupDto(e.Id, e.Code, e.Name);

    protected override void Apply(BoyahaneOperationRequest request, BoyahaneOperation entity, string code)
    {
        entity.Code = code;
        entity.Name = request.Name.Trim();
        entity.Description = NullIfBlank(request.Description);
        entity.Behavior = request.Behavior;
        entity.NeedsMachine = request.NeedsMachine;
        entity.StandardMinutes = request.StandardMinutes;
        entity.IsActive = request.IsActive;
    }

    /// <summary>Rotada kullanılan operasyonun davranışı değişirse rotanın kuralları bozulur.</summary>
    protected override async Task<Error?> ValidateAsync(BoyahaneOperationRequest request, BoyahaneOperation? existing, CancellationToken cancellationToken)
    {
        if (existing is null || existing.Behavior == request.Behavior || !await IsUsedAsync(existing.Id, cancellationToken))
        {
            return null;
        }

        const string message = "Rotada ya da partide kullanılan operasyonun davranışı değiştirilemez.";
        return new Error(ErrorCodes.ValidationError, message, ErrorType.Validation, new Dictionary<string, string[]> { ["behavior"] = [message] });
    }

    protected override async Task<string?> FindUsageAsync(int id, CancellationToken cancellationToken) =>
        await IsUsedAsync(id, cancellationToken) ? "rotalar ya da partiler" : null;

    private async Task<bool> IsUsedAsync(int id, CancellationToken cancellationToken) =>
        await Db.Set<BoyahaneRouteStep>().AnyAsync(s => s.OperationId == id, cancellationToken)
        || await Db.Set<BoyahaneBatchStep>().AnyAsync(s => s.OperationId == id, cancellationToken);
}

public sealed record BoyahaneRouteStepDto(int Sequence, int OperationId, string OperationCode, string OperationName, OperationBehavior Behavior, bool NeedsMachine);

public sealed record BoyahaneRouteDto(int Id, string Code, string Name, OrderProcessType ProcessType, bool IsDefault, bool IsActive, IReadOnlyList<BoyahaneRouteStepDto> Steps);

/// <param name="OperationIds">Adımlar, sırayla.</param>
public sealed record BoyahaneRouteRequest(string Code, string Name, OrderProcessType ProcessType, IReadOnlyList<int> OperationIds, bool IsDefault = false, bool IsActive = true);

public sealed class BoyahaneRouteRequestValidator : AbstractValidator<BoyahaneRouteRequest>
{
    public BoyahaneRouteRequestValidator()
    {
        RuleFor(x => x.Code).ValidCode();
        RuleFor(x => x.Name).ValidName(100);
        RuleFor(x => x.ProcessType).IsInEnum().WithMessage("Geçerli bir işlem türü seçin.");
        RuleFor(x => x.OperationIds).NotEmpty().WithMessage("Rotada en az bir operasyon olmalıdır.");
    }
}

public interface IBoyahaneRouteService
{
    Task<IReadOnlyList<BoyahaneRouteDto>> GetListAsync(CancellationToken cancellationToken);
    Task<Result<BoyahaneRouteDto>> CreateAsync(BoyahaneRouteRequest request, CancellationToken cancellationToken);
    Task<Result<BoyahaneRouteDto>> UpdateAsync(int id, BoyahaneRouteRequest request, CancellationToken cancellationToken);
    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken);
    Task<Result<IReadOnlyList<int>>> GetMachineOperationsAsync(int machineId, CancellationToken cancellationToken);
    Task<Result<IReadOnlyList<int>>> SetMachineOperationsAsync(int machineId, IReadOnlyList<int> operationIds, CancellationToken cancellationToken);
}

public sealed record SetMachineOperationsRequest(IReadOnlyList<int> OperationIds);

public sealed class BoyahaneRouteService(IAppDbContext db) : IBoyahaneRouteService
{
    private const string InvalidRoute = "ROUTE_INVALID";

    private static readonly Error NotFound = new(ErrorCodes.NotFound, "Rota bulunamadı.", ErrorType.NotFound);
    private static readonly Error MachineNotFound = new(ErrorCodes.NotFound, "Makine bulunamadı.", ErrorType.NotFound);

    public async Task<IReadOnlyList<BoyahaneRouteDto>> GetListAsync(CancellationToken cancellationToken) =>
        (await db.Set<BoyahaneRoute>().AsNoTracking()
            .Include(r => r.Steps).ThenInclude(s => s.Operation)
            .OrderBy(r => r.ProcessType).ThenByDescending(r => r.IsDefault).ThenBy(r => r.Code)
            .ToListAsync(cancellationToken))
        .Select(ToDto)
        .ToList();

    public async Task<Result<BoyahaneRouteDto>> CreateAsync(BoyahaneRouteRequest request, CancellationToken cancellationToken)
    {
        var code = NormalizeCode(request.Code);
        if (await db.Set<BoyahaneRoute>().AnyAsync(r => r.Code == code, cancellationToken))
        {
            return new Error(ErrorCodes.CodeAlreadyExists, $"{code} kodlu rota zaten var.", ErrorType.Conflict);
        }

        var route = new BoyahaneRoute();
        return await SaveAsync(route, code, request, cancellationToken);
    }

    public async Task<Result<BoyahaneRouteDto>> UpdateAsync(int id, BoyahaneRouteRequest request, CancellationToken cancellationToken)
    {
        var route = await db.Set<BoyahaneRoute>().Include(r => r.Steps).SingleOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (route is null)
        {
            return NotFound;
        }

        var code = NormalizeCode(request.Code);
        if (await db.Set<BoyahaneRoute>().AnyAsync(r => r.Code == code && r.Id != id, cancellationToken))
        {
            return new Error(ErrorCodes.CodeAlreadyExists, $"{code} kodlu rota zaten var.", ErrorType.Conflict);
        }

        return await SaveAsync(route, code, request, cancellationToken);
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var route = await db.Set<BoyahaneRoute>().SingleOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (route is null)
        {
            return NotFound;
        }

        if (route.IsDefault)
        {
            return new Error(InvalidRoute, "Varsayılan rota silinemez; önce başka bir rotayı varsayılan yapın.", ErrorType.Conflict);
        }

        db.Set<BoyahaneRoute>().Remove(route);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<int>>> GetMachineOperationsAsync(int machineId, CancellationToken cancellationToken)
    {
        if (!await db.Set<BoyahaneMachine>().AnyAsync(m => m.Id == machineId, cancellationToken))
        {
            return MachineNotFound;
        }

        return await db.Set<BoyahaneMachineOperation>().Where(o => o.MachineId == machineId).Select(o => o.OperationId).ToListAsync(cancellationToken);
    }

    public async Task<Result<IReadOnlyList<int>>> SetMachineOperationsAsync(int machineId, IReadOnlyList<int> operationIds, CancellationToken cancellationToken)
    {
        if (!await db.Set<BoyahaneMachine>().AnyAsync(m => m.Id == machineId, cancellationToken))
        {
            return MachineNotFound;
        }

        var ids = operationIds.Distinct().ToList();
        var valid = await db.Set<BoyahaneOperation>().CountAsync(o => ids.Contains(o.Id) && o.NeedsMachine, cancellationToken);
        if (valid != ids.Count)
        {
            return new Error(InvalidRoute, "Seçilen operasyonlardan biri makinede yapılmıyor.", ErrorType.Conflict);
        }

        var current = await db.Set<BoyahaneMachineOperation>().Where(o => o.MachineId == machineId).ToListAsync(cancellationToken);
        db.Set<BoyahaneMachineOperation>().RemoveRange(current.Where(o => !ids.Contains(o.OperationId)));
        db.Set<BoyahaneMachineOperation>().AddRange(ids.Where(id => current.All(o => o.OperationId != id)).Select(id => new BoyahaneMachineOperation { MachineId = machineId, OperationId = id }));
        await db.SaveChangesAsync(cancellationToken);
        return ids;
    }

    private async Task<Result<BoyahaneRouteDto>> SaveAsync(BoyahaneRoute route, string code, BoyahaneRouteRequest request, CancellationToken cancellationToken)
    {
        var ids = request.OperationIds.Distinct().ToList();
        var operations = await db.Set<BoyahaneOperation>().AsNoTracking().Where(o => ids.Contains(o.Id)).ToDictionaryAsync(o => o.Id, cancellationToken);
        if (operations.Count != ids.Count)
        {
            return new Error(InvalidRoute, "Seçilen operasyonlardan biri bulunamadı.", ErrorType.Conflict);
        }

        if (Validate(request.ProcessType, request.OperationIds.Select(id => operations[id].Behavior).ToList()) is { } message)
        {
            return new Error(InvalidRoute, message, ErrorType.Conflict);
        }

        var otherDefault = await db.Set<BoyahaneRoute>()
            .Where(r => r.ProcessType == request.ProcessType && r.IsDefault && r.Id != route.Id)
            .ToListAsync(cancellationToken);
        var isDefault = request.IsDefault || otherDefault.Count == 0;
        if (!isDefault && route.IsDefault)
        {
            return new Error(InvalidRoute, "Varsayılan rota kaldırılamaz; başka bir rotayı varsayılan yapın.", ErrorType.Conflict);
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (isDefault)
        {
            foreach (var other in otherDefault)
            {
                other.IsDefault = false;
            }

            await db.SaveChangesAsync(cancellationToken);
        }

        route.Code = code;
        route.Name = request.Name.Trim();
        route.ProcessType = request.ProcessType;
        route.IsDefault = isDefault;
        route.IsActive = isDefault || request.IsActive;
        // Sıra numarası benzersiz olduğu için eski adımlar yenilerden önce silinir.
        if (route.Id != 0)
        {
            db.Set<BoyahaneRouteStep>().RemoveRange(route.Steps);
            await db.SaveChangesAsync(cancellationToken);
        }

        var sequence = 0;
        foreach (var id in request.OperationIds)
        {
            route.Steps.Add(new BoyahaneRouteStep { Sequence = ++sequence, OperationId = id });
        }

        if (route.Id == 0)
        {
            db.Set<BoyahaneRoute>().Add(route);
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var saved = await db.Set<BoyahaneRoute>().AsNoTracking().Include(r => r.Steps).ThenInclude(s => s.Operation).SingleAsync(r => r.Id == route.Id, cancellationToken);
        return ToDto(saved);
    }

    /// <summary>
    /// Boyama rotasında reçeteyi uygulayan tek boyama adımı olur; renk kontrolü boyamadan sonra gelir. Baskı ve terbiye lab
    /// reçetesi kullanmaz, boyama ve renk kontrolü içermez. Baskı rotasında en fazla bir baskı adımı olur; baskı reçetesi
    /// desenin renk yolundan gelir. Söküm adımı rotaya eklenmez, renk kontrolünden kendiliğinden girer. Final kalite top
    /// bazında sonuç aldığı için her rotada tam bir kez bulunur.
    /// </summary>
    private static string? Validate(OrderProcessType process, IReadOnlyList<OperationBehavior> steps)
    {
        var dyeing = steps.Count(s => s == OperationBehavior.Dyeing);
        var check = steps.Count(s => s == OperationBehavior.ColorCheck);
        var printing = steps.Count(s => s == OperationBehavior.Printing);
        var isDye = process is OrderProcessType.FabricDyeing or OrderProcessType.YarnDyeing;
        if (steps.Contains(OperationBehavior.Stripping))
        {
            return "Söküm adımı rotaya eklenmez; renk kontrolünde \"söküm + yeniden boya\" seçilince kendiliğinden girer.";
        }

        if (isDye && dyeing != 1)
        {
            return "Boyama rotasında tek boyama adımı olmalıdır.";
        }

        if (!isDye && dyeing + check > 0)
        {
            return "Baskı ve terbiye rotasında boyama ve renk kontrolü bulunamaz; bu işlemlerde lab reçetesi yoktur.";
        }

        if (process != OrderProcessType.Printing && printing > 0)
        {
            return "Baskı adımı yalnızca baskı rotasında bulunur.";
        }

        if (printing > 1)
        {
            return "Baskı rotasında en fazla bir baskı adımı olur.";
        }

        if (check > 1)
        {
            return "Rotada en fazla bir renk kontrolü olur; ilave ve yeniden işlem kendiliğinden eklenir.";
        }

        if (check == 1 && IndexOf(steps, OperationBehavior.ColorCheck) < IndexOf(steps, OperationBehavior.Dyeing))
        {
            return "Renk kontrolü boyamadan sonra gelmelidir.";
        }

        return steps.Count(s => s == OperationBehavior.FinalQuality) switch
        {
            0 => "Rotada final kalite adımı olmalıdır; sevk ve yeniden işlem bu karara bağlıdır.",
            > 1 => "Rotada en fazla bir final kalite adımı olur.",
            _ => null,
        };
    }

    private static int IndexOf(IReadOnlyList<OperationBehavior> steps, OperationBehavior behavior) => steps.ToList().IndexOf(behavior);

    private static BoyahaneRouteDto ToDto(BoyahaneRoute route) => new(
        route.Id,
        route.Code,
        route.Name,
        route.ProcessType,
        route.IsDefault,
        route.IsActive,
        route.Steps.OrderBy(s => s.Sequence).Select(s => new BoyahaneRouteStepDto(s.Sequence, s.OperationId, s.Operation.Code, s.Operation.Name, s.Operation.Behavior, s.Operation.NeedsMachine)).ToList());
}
