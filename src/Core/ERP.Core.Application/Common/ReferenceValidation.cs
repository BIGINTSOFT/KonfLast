using ERP.Core.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace ERP.Core.Application.Common;

/// <summary>Başka kayıtlara verilen referansların (müşteri, ürün, renk ...) doğrulanması.</summary>
public static class ReferenceValidation
{
    /// <summary>
    /// Referans verilen kaydın var olduğunu doğrular. Pasif bir kayıt yalnızca zaten seçiliyse
    /// (<paramref name="currentId"/>) kabul edilir; yeni seçimlerde aktif olmalıdır.
    /// </summary>
    public static async Task<Error?> RequireSelectableAsync<TReference>(
        IQueryable<TReference> set,
        int? id,
        int? currentId,
        string field,
        string label,
        CancellationToken cancellationToken)
        where TReference : SoftDeletableEntity, IActivatable
    {
        if (id is null)
        {
            return null;
        }

        var states = await set.Where(e => e.Id == id).Select(e => e.IsActive).ToListAsync(cancellationToken);
        if (states.Count == 0)
        {
            return InvalidReference(field, $"{label} bulunamadı.");
        }

        return states[0] || id == currentId ? null : InvalidReference(field, $"{label} pasif durumda.");
    }

    /// <summary>
    /// Birden fazla referansı tek sorguyla doğrular. <paramref name="alreadySelected"/> içindeki
    /// kayıtlar pasif olsa da kabul edilir.
    /// </summary>
    public static async Task<Error?> RequireSelectableAsync<TReference>(
        IQueryable<TReference> set,
        IReadOnlyCollection<int> ids,
        IReadOnlySet<int> alreadySelected,
        string field,
        string label,
        CancellationToken cancellationToken)
        where TReference : SoftDeletableEntity, IActivatable
    {
        var found = await set
            .Where(e => ids.Contains(e.Id))
            .Select(e => new { e.Id, e.IsActive })
            .ToDictionaryAsync(e => e.Id, e => e.IsActive, cancellationToken);

        if (ids.Any(id => !found.ContainsKey(id)))
        {
            return InvalidReference(field, $"{label} bulunamadı.");
        }

        return ids.Any(id => !found[id] && !alreadySelected.Contains(id))
            ? InvalidReference(field, $"Seçilen {label.ToLowerInvariant()} pasif durumda.")
            : null;
    }

    public static Error InvalidReference(string field, string message) => new(
        ErrorCodes.InvalidReference,
        message,
        ErrorType.Validation,
        new Dictionary<string, string[]> { [field] = [message] });
}
