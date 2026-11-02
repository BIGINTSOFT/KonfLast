using System.Linq.Expressions;
using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Core.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace ERP.Core.Application.Features.MasterData;

/// <summary>
/// Ana veri kaynaklarının ortak akışı: sayfalı liste, seçim listesi, benzersiz kod kontrolü,
/// referans doğrulama, "kullanımda olan silinemez" kuralı ve soft delete.
/// Kaynağa özel her şey (alanlar, arama, referanslar, kullanım kontrolü) alt sınıftadır.
/// </summary>
public abstract class MasterDataService<TEntity, TDto, TRequest>(IAppDbContext db)
    : IMasterDataService<TDto, TRequest>
    where TEntity : SoftDeletableEntity, IActivatable, new()
{
    protected IAppDbContext Db { get; } = db;

    protected abstract DbSet<TEntity> Set { get; }

    /// <summary>Hata mesajlarında kullanılan ad, örn. "Müşteri".</summary>
    protected abstract string Label { get; }

    /// <summary>İstekteki kod alanının JSON adı (hata ayrıntısı bu alana bağlanır), örn. "customerCode".</summary>
    protected abstract string CodeField { get; }

    protected abstract string CodeOf(TRequest request);
    protected abstract Expression<Func<TEntity, bool>> WithCode(string code);

    /// <summary>
    /// Kodun çakıştığı kayıtlar. Kod yalnızca bir üst kayıt içinde benzersizse (ör. depo adresi, deposunda)
    /// alt sınıf istekteki üst kayda göre daraltır.
    /// </summary>
    protected virtual Expression<Func<TEntity, bool>> WithCode(string code, TRequest request) => WithCode(code);
    protected abstract Expression<Func<TEntity, bool>> Matches(string term);
    protected abstract IQueryable<TEntity> Order(IQueryable<TEntity> query);
    protected abstract Expression<Func<TEntity, TDto>> ToDto { get; }
    protected abstract Expression<Func<TEntity, LookupDto>> ToLookup { get; }

    /// <summary>İstekteki değerleri entity'ye yazar. <paramref name="code"/> normalize edilmiş koddur.</summary>
    protected abstract void Apply(TRequest request, TEntity entity, string code);

    /// <summary>Referans ve iş kuralı kontrolleri. <paramref name="existing"/> yeni kayıtta null'dır.</summary>
    protected virtual Task<Error?> ValidateAsync(
        TRequest request, TEntity? existing, CancellationToken cancellationToken) => Task.FromResult<Error?>(null);

    /// <summary>Kayıt başka kayıtlarca kullanılıyorsa nerede kullanıldığını döner (örn. "model"); yoksa null.</summary>
    /// <summary>Kaynağa özgü seçim süzgeci (ör. ürünlerde müşteri); varsayılan süzmez.</summary>
    protected virtual IQueryable<TEntity> FilterLookup(IQueryable<TEntity> items, LookupQuery query) => items;

    protected virtual Task<string?> FindUsageAsync(int id, CancellationToken cancellationToken) =>
        Task.FromResult<string?>(null);

    /// <summary>
    /// Kaydın kullanıcıya gösterilmeden önceki son hali: yetkisi olmayan kullanıcıdan gizlenen alanlar (maliyet,
    /// ücret, fiyat) burada boşaltılır. İstek başına bir kez çağrılır; null dönerse kayıtlar olduğu gibi gider.
    /// </summary>
    protected virtual Task<Func<TDto, TDto>?> PresenterAsync(CancellationToken cancellationToken) =>
        Task.FromResult<Func<TDto, TDto>?>(null);

    public async Task<PagedResult<TDto>> GetListAsync(MasterDataQuery query, CancellationToken cancellationToken)
    {
        var items = Set.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            items = items.Where(Matches(query.Search.Trim()));
        }

        if (query.IsActive is { } isActive)
        {
            items = items.Where(e => e.IsActive == isActive);
        }

        var page = await Order(items).Select(ToDto).ToPagedResultAsync(query, cancellationToken);
        return await PresenterAsync(cancellationToken) is { } present
            ? page with { Items = page.Items.Select(present).ToList() }
            : page;
    }

    public async Task<IReadOnlyList<LookupDto>> LookupAsync(LookupQuery query, CancellationToken cancellationToken)
    {
        var items = Set.AsNoTracking().Where(e => e.IsActive);
        items = FilterLookup(items, query);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            items = items.Where(Matches(query.Search.Trim()));
        }

        var result = await Order(items).Select(ToLookup).Take(query.NormalizedTake).ToListAsync(cancellationToken);

        // Formda seçili olan değer; pasif olsa, aramaya uymasa ya da sınırın dışında kalsa da listede olmalı.
        if (query.IncludeId is { } includeId && result.All(item => item.Id != includeId))
        {
            result.InsertRange(
                0, await Set.AsNoTracking().Where(e => e.Id == includeId).Select(ToLookup).ToListAsync(cancellationToken));
        }

        return result;
    }

    public async Task<Result<TDto>> GetAsync(int id, CancellationToken cancellationToken)
    {
        var item = await Set.AsNoTracking().Where(e => e.Id == id).Select(ToDto).ToListAsync(cancellationToken);
        if (item.Count == 0)
        {
            return NotFound();
        }

        return await PresenterAsync(cancellationToken) is { } present ? present(item[0]) : item[0];
    }

    public async Task<Result<TDto>> CreateAsync(TRequest request, CancellationToken cancellationToken)
    {
        var code = MasterDataRules.NormalizeCode(CodeOf(request));
        if (await Set.AnyAsync(WithCode(code, request), cancellationToken))
        {
            return CodeAlreadyExists(code);
        }

        if (await ValidateAsync(request, null, cancellationToken) is { } error)
        {
            return error;
        }

        var entity = new TEntity();
        Apply(request, entity, code);
        Set.Add(entity);

        if (await SaveAsync(code, request, entity.Id, cancellationToken) is { } conflict)
        {
            return conflict;
        }

        return await GetAsync(entity.Id, cancellationToken);
    }

    public async Task<Result<TDto>> UpdateAsync(int id, TRequest request, CancellationToken cancellationToken)
    {
        var entity = await Set.SingleOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (entity is null)
        {
            return NotFound();
        }

        var code = MasterDataRules.NormalizeCode(CodeOf(request));
        if (await Set.Where(WithCode(code, request)).AnyAsync(e => e.Id != id, cancellationToken))
        {
            return CodeAlreadyExists(code);
        }

        if (await ValidateAsync(request, entity, cancellationToken) is { } error)
        {
            return error;
        }

        Apply(request, entity, code);

        if (await SaveAsync(code, request, id, cancellationToken) is { } conflict)
        {
            return conflict;
        }

        return await GetAsync(id, cancellationToken);
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var entity = await Set.SingleOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (entity is null)
        {
            return NotFound();
        }

        if (await FindUsageAsync(id, cancellationToken) is { } usage)
        {
            return new Error(
                ErrorCodes.RecordInUse,
                $"{Label} kullanımda olduğu için silinemez ({usage}). Bunun yerine pasif yapabilirsiniz.",
                ErrorType.Conflict);
        }

        // Fiziksel silme değildir: kayıt IsDeleted olarak işaretlenir ve kodu yeniden kullanılabilir.
        Set.Remove(entity);
        await Db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    protected static Task<Error?> RequireSelectableAsync<TReference>(
        IQueryable<TReference> set,
        int? id,
        int? currentId,
        string field,
        string label,
        CancellationToken cancellationToken)
        where TReference : SoftDeletableEntity, IActivatable =>
        ReferenceValidation.RequireSelectableAsync(set, id, currentId, field, label, cancellationToken);

    private async Task<Error?> SaveAsync(string code, TRequest request, int id, CancellationToken cancellationToken)
    {
        try
        {
            await Db.SaveChangesAsync(cancellationToken);
            return null;
        }
        catch (DbUpdateException)
        {
            // Aynı kod eşzamanlı başka bir istekle alınmış olabilir (unique index).
            if (await Set.AsNoTracking().Where(WithCode(code, request)).AnyAsync(e => e.Id != id, cancellationToken))
            {
                return CodeAlreadyExists(code);
            }

            throw;
        }
    }

    private Error NotFound() => new(ErrorCodes.NotFound, $"{Label} bulunamadı.", ErrorType.NotFound);

    private Error CodeAlreadyExists(string code)
    {
        var message = $"'{code}' kodu zaten kullanılıyor.";
        return new Error(
            ErrorCodes.CodeAlreadyExists,
            message,
            ErrorType.Conflict,
            new Dictionary<string, string[]> { [CodeField] = [message] });
    }
}
