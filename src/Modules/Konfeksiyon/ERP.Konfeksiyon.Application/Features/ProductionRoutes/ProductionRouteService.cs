using ERP.Konfeksiyon.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Core.Application.Features.MasterData;
using ERP.Konfeksiyon.Domain.Entities;
using ERP.Konfeksiyon.Domain.Enums;
using ERP.Konfeksiyon.Domain.Routing;
using Microsoft.EntityFrameworkCore;

namespace ERP.Konfeksiyon.Application.Features.ProductionRoutes;

public interface IProductionRouteService
{
    Task<PagedResult<ProductionRouteListItemDto>> GetListAsync(
        ProductionRouteListQuery query, CancellationToken cancellationToken);

    Task<Result<ProductionRouteDto>> GetAsync(int id, CancellationToken cancellationToken);
    Task<Result<ProductionRouteDto>> CreateAsync(ProductionRouteRequest request, CancellationToken cancellationToken);

    Task<Result<ProductionRouteDto>> UpdateAsync(
        int id, ProductionRouteRequest request, CancellationToken cancellationToken);

    /// <summary>Rotayı modelin geçerli rotası yapar; modelin önceki etkin rotası arşivlenir.</summary>
    Task<Result<ProductionRouteDto>> ActivateAsync(int id, string rowVersion, CancellationToken cancellationToken);

    Task<Result<ProductionRouteDto>> ArchiveAsync(int id, string rowVersion, CancellationToken cancellationToken);

    /// <summary>Rotanın operasyon ve bağımlılıklarıyla yeni bir taslak sürümünü oluşturur.</summary>
    Task<Result<ProductionRouteDto>> CopyAsync(int id, CancellationToken cancellationToken);

    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken);

    /// <summary>
    /// Adımın fason ayarını değiştirir. Akışı ve SAM'i değiştirmediği için rota kilitliyken (etkin / arşiv) de yapılır.
    /// </summary>
    Task<Result<ProductionRouteDto>> SetSubcontractAsync(
        int id, int routeOperationId, RouteOperationSubcontractRequest request, CancellationToken cancellationToken);
}

public sealed class ProductionRouteService(IKonfeksiyonDbContext db) : IProductionRouteService
{
    private const string OperationsField = "operations";

    private static readonly Error NotFound = new(ErrorCodes.NotFound, "Rota bulunamadı.", ErrorType.NotFound);

    private static readonly Error ConcurrencyConflict = new(
        ErrorCodes.ConcurrencyConflict,
        "Rota siz açtıktan sonra başka bir kullanıcı tarafından değiştirildi. Sayfayı yenileyip tekrar deneyin.",
        ErrorType.Conflict);

    private static readonly IReadOnlyDictionary<RouteStatus, string> StatusLabels =
        new Dictionary<RouteStatus, string>
        {
            [RouteStatus.Draft] = "Taslak",
            [RouteStatus.Active] = "Etkin",
            [RouteStatus.Archived] = "Arşiv",
        };

    public Task<PagedResult<ProductionRouteListItemDto>> GetListAsync(
        ProductionRouteListQuery query, CancellationToken cancellationToken)
    {
        var routes = db.ProductionRoutes.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            routes = routes.Where(r =>
                r.Model.ModelCode.Contains(search) || r.Model.Name.Contains(search) || r.Name!.Contains(search));
        }

        if (query.ModelId is { } modelId)
        {
            routes = routes.Where(r => r.ModelId == modelId);
        }

        if (query.Status is { } status)
        {
            routes = routes.Where(r => r.Status == status);
        }

        return routes
            .OrderBy(r => r.Model.ModelCode).ThenByDescending(r => r.Version)
            .Select(r => new ProductionRouteListItemDto(
                r.Id,
                r.ModelId,
                r.Model.ModelCode,
                r.Model.Name,
                r.Version,
                r.Name,
                r.Status,
                r.Operations.Count,
                r.Operations.Sum(o => (decimal?)o.Sam) ?? 0))
            .ToPagedResultAsync(query, cancellationToken);
    }

    public async Task<Result<ProductionRouteDto>> GetAsync(int id, CancellationToken cancellationToken)
    {
        var route = await db.ProductionRoutes
            .AsNoTracking()
            .Include(r => r.Model)
            .Include(r => r.Operations).ThenInclude(o => o.Operation).ThenInclude(o => o.MachineType)
            .Include(r => r.Operations).ThenInclude(o => o.Predecessors).ThenInclude(d => d.Predecessor)
            .Include(r => r.Operations).ThenInclude(o => o.Subcontractor)
            .SingleOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (route is null)
        {
            return NotFound;
        }

        return ToDto(route);
    }

    public async Task<Result<ProductionRouteDto>> SetSubcontractAsync(
        int id, int routeOperationId, RouteOperationSubcontractRequest request, CancellationToken cancellationToken)
    {
        var step = await db.ProductionRouteOperations
            .SingleOrDefaultAsync(o => o.Id == routeOperationId && o.ProductionRouteId == id, cancellationToken);
        if (step is null)
        {
            return new Error(ErrorCodes.NotFound, "Rota adımı bulunamadı.", ErrorType.NotFound);
        }

        var subcontractorId = request.IsSubcontracted ? request.SubcontractorId : null;
        if (await ReferenceValidation.RequireSelectableAsync(
                db.Suppliers, subcontractorId, step.SubcontractorId, "subcontractorId", "Fasoncu", cancellationToken)
            is { } supplierError)
        {
            return supplierError;
        }

        step.IsSubcontracted = request.IsSubcontracted;
        step.SubcontractorId = subcontractorId;
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    public async Task<Result<ProductionRouteDto>> CreateAsync(
        ProductionRouteRequest request, CancellationToken cancellationToken)
    {
        var modelError = await ReferenceValidation.RequireSelectableAsync(
            db.Models, request.ModelId, null, "modelId", "Model", cancellationToken);
        if (modelError is not null)
        {
            return modelError;
        }

        var sams = await ValidateOperationsAsync(request.Operations, new HashSet<int>(), cancellationToken);
        if (!sams.IsSuccess)
        {
            return sams.Error!;
        }

        var route = new ProductionRoute
        {
            ModelId = request.ModelId,
            Version = await NextVersionAsync(request.ModelId, cancellationToken),
            Name = MasterDataRules.NullIfBlank(request.Name),
            Description = MasterDataRules.NullIfBlank(request.Description),
        };
        AddOperations(route, request.Operations.Select(o => (
            o.SequenceNo, o.OperationId, Sam: o.Sam ?? sams.Value[o.OperationId], Predecessors: o.Predecessors ?? [],
            o.IsSubcontracted, SubcontractorId: o.IsSubcontracted ? o.SubcontractorId : null)));

        db.ProductionRoutes.Add(route);
        if (await SaveNewAsync(cancellationToken) is { } error)
        {
            return error;
        }

        return await GetAsync(route.Id, cancellationToken);
    }

    public async Task<Result<ProductionRouteDto>> UpdateAsync(
        int id, ProductionRouteRequest request, CancellationToken cancellationToken)
    {
        var route = await LoadForChangeAsync(id, cancellationToken);
        if (route is null)
        {
            return NotFound;
        }

        if (RowVersionToken.Check(route.RowVersion, request.RowVersion, ConcurrencyConflict) is { } versionError)
        {
            return versionError;
        }

        if (!route.IsEditable)
        {
            return NotEditable(route, "değiştirilemez. Değişiklik için kopyalayıp yeni sürüm oluşturun.");
        }

        var sams = await ValidateOperationsAsync(
            request.Operations, route.Operations.Select(o => o.OperationId).ToHashSet(), cancellationToken,
            route.Operations.Where(o => o.SubcontractorId is not null).Select(o => o.SubcontractorId!.Value).ToHashSet());
        if (!sams.IsSuccess)
        {
            return sams.Error!;
        }

        route.Name = MasterDataRules.NullIfBlank(request.Name);
        route.Description = MasterDataRules.NullIfBlank(request.Description);
        SyncOperations(route, request.Operations, sams.Value);

        return await SaveAndReloadAsync(route, cancellationToken);
    }

    public async Task<Result<ProductionRouteDto>> ActivateAsync(
        int id, string rowVersion, CancellationToken cancellationToken)
    {
        var route = await LoadForChangeAsync(id, cancellationToken);
        if (route is null)
        {
            return NotFound;
        }

        if (RowVersionToken.Check(route.RowVersion, rowVersion, ConcurrencyConflict) is { } versionError)
        {
            return versionError;
        }

        if (!route.CanActivate)
        {
            return new Error(ErrorCodes.InvalidStatusTransition, "Rota zaten etkin.");
        }

        if (route.Operations.Count == 0)
        {
            return new Error(ErrorCodes.RouteHasNoOperations, "Operasyonu olmayan rota etkinleştirilemez.");
        }

        var inactive = await db.ProductionRouteOperations
            .Where(o => o.ProductionRouteId == id && !o.Operation.IsActive)
            .OrderBy(o => o.SequenceNo)
            .Select(o => o.SequenceNo)
            .ToListAsync(cancellationToken);
        if (inactive.Count > 0)
        {
            return new Error(
                ErrorCodes.RouteHasInactiveOperation,
                $"Rotada pasif operasyonlar var (sıra no: {string.Join(", ", inactive)}).");
        }

        if (!await db.Models.AnyAsync(m => m.Id == route.ModelId && m.IsActive, cancellationToken))
        {
            return new Error(ErrorCodes.InvalidReference, "Pasif bir modelin rotası etkinleştirilemez.");
        }

        var currentlyActive = await db.ProductionRoutes
            .Where(r => r.ModelId == route.ModelId && r.Status == RouteStatus.Active && r.Id != id)
            .ToListAsync(cancellationToken);

        try
        {
            // Model başına tek etkin rota kuralı veritabanında unique index ile de korunur; index'e
            // takılmamak için önce eskisi arşivlenir, sonra yenisi etkinleştirilir (tek transaction).
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            foreach (var previous in currentlyActive)
            {
                previous.Archive();
            }

            await db.SaveChangesAsync(cancellationToken);

            route.Activate();
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Aynı modelde eşzamanlı başka bir etkinleştirme ya da değişiklik.
            return ConcurrencyConflict;
        }

        return await GetAsync(id, cancellationToken);
    }

    public async Task<Result<ProductionRouteDto>> ArchiveAsync(
        int id, string rowVersion, CancellationToken cancellationToken)
    {
        var route = await LoadForChangeAsync(id, cancellationToken);
        if (route is null)
        {
            return NotFound;
        }

        if (RowVersionToken.Check(route.RowVersion, rowVersion, ConcurrencyConflict) is { } versionError)
        {
            return versionError;
        }

        if (!route.CanArchive)
        {
            return new Error(
                ErrorCodes.InvalidStatusTransition,
                $"{StatusLabels[route.Status]} durumundaki rota arşivlenemez; yalnızca etkin rota arşivlenir.");
        }

        route.Archive();
        return await SaveAndReloadAsync(route, cancellationToken);
    }

    public async Task<Result<ProductionRouteDto>> CopyAsync(int id, CancellationToken cancellationToken)
    {
        var source = await db.ProductionRoutes
            .AsNoTracking()
            .Include(r => r.Operations).ThenInclude(o => o.Predecessors).ThenInclude(d => d.Predecessor)
            .SingleOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (source is null)
        {
            return NotFound;
        }

        var copy = new ProductionRoute
        {
            ModelId = source.ModelId,
            Version = await NextVersionAsync(source.ModelId, cancellationToken),
            Name = source.Name,
            Description = source.Description,
        };
        AddOperations(copy, source.Operations.Select(o => (
            o.SequenceNo,
            o.OperationId,
            o.Sam,
            Predecessors: (IReadOnlyList<int>)o.Predecessors.Select(d => d.Predecessor.SequenceNo).ToList(),
            o.IsSubcontracted,
            o.SubcontractorId)));

        db.ProductionRoutes.Add(copy);
        if (await SaveNewAsync(cancellationToken) is { } error)
        {
            return error;
        }

        return await GetAsync(copy.Id, cancellationToken);
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var route = await db.ProductionRoutes.SingleOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (route is null)
        {
            return NotFound;
        }

        if (!route.IsEditable)
        {
            return NotEditable(route, "silinemez.");
        }

        // Soft delete: sürüm numarası yeniden kullanılmaz.
        db.ProductionRoutes.Remove(route);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private Task<ProductionRoute?> LoadForChangeAsync(int id, CancellationToken cancellationToken) =>
        db.ProductionRoutes
            .Include(r => r.Operations).ThenInclude(o => o.Predecessors).ThenInclude(d => d.Predecessor)
            .SingleOrDefaultAsync(r => r.Id == id, cancellationToken);

    /// <summary>
    /// Operasyon referanslarını ve bağımlılık grafiğini doğrular; başarılıysa operasyonların
    /// standart SAM değerlerini döner (adımda SAM verilmediğinde kullanılır).
    /// </summary>
    private async Task<Result<Dictionary<int, decimal>>> ValidateOperationsAsync(
        IReadOnlyList<RouteOperationRequest> operations,
        IReadOnlySet<int> alreadySelected,
        CancellationToken cancellationToken,
        IReadOnlySet<int>? currentSubcontractors = null)
    {
        var operationIds = operations.Select(o => o.OperationId).Distinct().ToList();
        var referenceError = await ReferenceValidation.RequireSelectableAsync(
            db.Operations, operationIds, alreadySelected, OperationsField, "Operasyon", cancellationToken);
        if (referenceError is not null)
        {
            return referenceError;
        }

        var subcontractorIds = operations
            .Where(o => o.IsSubcontracted && o.SubcontractorId is not null)
            .Select(o => o.SubcontractorId!.Value)
            .Distinct()
            .ToList();
        if (subcontractorIds.Count > 0
            && await ReferenceValidation.RequireSelectableAsync(
                db.Suppliers, subcontractorIds, currentSubcontractors ?? new HashSet<int>(), OperationsField, "Fasoncu",
                cancellationToken) is { } subcontractorError)
        {
            return subcontractorError;
        }

        var graph = RouteGraph.Analyze(
            operations.Select(o => new RouteNode(o.SequenceNo, o.Predecessors ?? [])).ToList());
        if (!graph.IsValid)
        {
            var isCycle = graph.Errors.Any(e => e.Type == RouteGraphErrorType.Cycle);
            return new Error(
                isCycle ? ErrorCodes.RouteDependencyCycle : ErrorCodes.RouteInvalidDependency,
                isCycle ? "Operasyon bağımlılıkları döngü oluşturuyor." : "Operasyon bağımlılıkları geçersiz.",
                ErrorType.Validation,
                new Dictionary<string, string[]> { [OperationsField] = graph.Errors.Select(e => e.Message).ToArray() });
        }

        return await db.Operations
            .Where(o => operationIds.Contains(o.Id))
            .ToDictionaryAsync(o => o.Id, o => o.Sam, cancellationToken);
    }

    private static void AddOperations(
        ProductionRoute route,
        IEnumerable<(int SequenceNo, int OperationId, decimal Sam, IReadOnlyList<int> Predecessors, bool IsSubcontracted,
            int? SubcontractorId)> operations)
    {
        var items = operations.ToList();
        var bySequence = items.ToDictionary(
            o => o.SequenceNo,
            o => new ProductionRouteOperation
            {
                SequenceNo = o.SequenceNo,
                OperationId = o.OperationId,
                Sam = o.Sam,
                IsSubcontracted = o.IsSubcontracted,
                SubcontractorId = o.IsSubcontracted ? o.SubcontractorId : null,
            });

        foreach (var item in items)
        {
            var operation = bySequence[item.SequenceNo];
            foreach (var predecessor in item.Predecessors.Distinct())
            {
                operation.Predecessors.Add(new OperationDependency { Predecessor = bySequence[predecessor] });
            }

            route.Operations.Add(operation);
        }
    }

    /// <summary>
    /// Adımları sıra numarasına göre istekle eşitler: kalan adım güncellenir, eksilen silinir, yeni gelen
    /// eklenir; bağımlılıklar da aynı şekilde yalnızca farkı kadar değişir.
    /// </summary>
    private void SyncOperations(
        ProductionRoute route, IReadOnlyList<RouteOperationRequest> requested, Dictionary<int, decimal> standardSams)
    {
        var wanted = requested.ToDictionary(o => o.SequenceNo);
        var existingDependencies = route.Operations.SelectMany(o => o.Predecessors).ToList();
        var bySequence = new Dictionary<int, ProductionRouteOperation>();

        foreach (var operation in route.Operations.ToList())
        {
            if (wanted.TryGetValue(operation.SequenceNo, out var request))
            {
                operation.OperationId = request.OperationId;
                operation.Sam = request.Sam ?? standardSams[request.OperationId];
                operation.IsSubcontracted = request.IsSubcontracted;
                operation.SubcontractorId = request.IsSubcontracted ? request.SubcontractorId : null;
                bySequence[operation.SequenceNo] = operation;
            }
            else
            {
                db.ProductionRouteOperations.Remove(operation);
            }
        }

        foreach (var request in requested.Where(r => !bySequence.ContainsKey(r.SequenceNo)))
        {
            var operation = new ProductionRouteOperation
            {
                SequenceNo = request.SequenceNo,
                OperationId = request.OperationId,
                Sam = request.Sam ?? standardSams[request.OperationId],
                IsSubcontracted = request.IsSubcontracted,
                SubcontractorId = request.IsSubcontracted ? request.SubcontractorId : null,
            };
            route.Operations.Add(operation);
            bySequence[request.SequenceNo] = operation;
        }

        var wantedPairs = requested
            .SelectMany(r => (r.Predecessors ?? []).Distinct().Select(p => (Successor: r.SequenceNo, Predecessor: p)))
            .ToHashSet();

        foreach (var dependency in existingDependencies)
        {
            var pair = (dependency.RouteOperation.SequenceNo, dependency.Predecessor.SequenceNo);
            var bothKept = ReferenceEquals(bySequence.GetValueOrDefault(pair.Item1), dependency.RouteOperation)
                && ReferenceEquals(bySequence.GetValueOrDefault(pair.Item2), dependency.Predecessor);

            if (!bothKept || !wantedPairs.Remove(pair))
            {
                db.OperationDependencies.Remove(dependency);
            }
        }

        foreach (var (successor, predecessor) in wantedPairs)
        {
            db.OperationDependencies.Add(new OperationDependency
            {
                RouteOperation = bySequence[successor], Predecessor = bySequence[predecessor],
            });
        }
    }

    private async Task<int> NextVersionAsync(int modelId, CancellationToken cancellationToken) =>
        (await db.ProductionRoutes
            .IgnoreQueryFilters() // silinen rotaların sürüm numarası da yeniden kullanılmaz
            .Where(r => r.ModelId == modelId)
            .MaxAsync(r => (int?)r.Version, cancellationToken) ?? 0) + 1;

    private async Task<Error?> SaveNewAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return null;
        }
        catch (DbUpdateException)
        {
            // Aynı model için eşzamanlı oluşturulan başka bir rota aynı sürüm numarasını aldı.
            return new Error(
                ErrorCodes.ConcurrencyConflict,
                "Bu model için aynı anda başka bir rota oluşturuldu. Lütfen tekrar deneyin.",
                ErrorType.Conflict);
        }
    }

    private async Task<Result<ProductionRouteDto>> SaveAndReloadAsync(
        ProductionRoute route, CancellationToken cancellationToken)
    {
        // Yalnızca adımlar değişse bile rota satırı güncellenir: rowversion ilerler ve
        // yükleme ile kayıt arasındaki eşzamanlı değişiklikleri veritabanı yakalar.
        db.Entry(route).State = EntityState.Modified;

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ConcurrencyConflict;
        }

        return await GetAsync(route.Id, cancellationToken);
    }

    private static Error NotEditable(ProductionRoute route, string consequence) => new(
        ErrorCodes.RouteNotEditable, $"{StatusLabels[route.Status]} durumundaki rota {consequence}");

    private static ProductionRouteDto ToDto(ProductionRoute route)
    {
        var stages = route.BuildGraph().Stages;

        return new ProductionRouteDto(
            route.Id,
            route.ModelId,
            route.Model.ModelCode,
            route.Model.Name,
            route.Version,
            route.Name,
            route.Description,
            route.Status,
            route.IsEditable,
            route.CanActivate,
            route.CanArchive,
            route.Operations.Sum(o => o.Sam),
            RowVersionToken.From(route.RowVersion),
            route.CreatedAt,
            route.CreatedBy,
            route.Operations
                .OrderBy(o => o.SequenceNo)
                .Select(o => new ProductionRouteOperationDto(
                    o.Id,
                    o.SequenceNo,
                    o.OperationId,
                    o.Operation.OperationCode,
                    o.Operation.Name,
                    o.Operation.MachineType?.Name,
                    o.Sam,
                    o.Predecessors.Select(d => d.Predecessor.SequenceNo).Order().ToList(),
                    stages.GetValueOrDefault(o.SequenceNo),
                    o.IsSubcontracted,
                    o.SubcontractorId,
                    o.Subcontractor?.Name))
                .ToList());
    }
}
