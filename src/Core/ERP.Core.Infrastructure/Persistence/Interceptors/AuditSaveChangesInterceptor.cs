using ERP.Core.Application.Abstractions;
using ERP.Core.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ERP.Core.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Audit alanlarını server zamanı ve aktif kullanıcı ile doldurur; soft delete destekleyen
/// entity'lerde fiziksel silmeyi IsDeleted güncellemesine çevirir.
/// </summary>
public sealed class AuditSaveChangesInterceptor(ICurrentUser currentUser, TimeProvider timeProvider)
    : SaveChangesInterceptor
{
    private const string SystemUser = "system";

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Apply(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Apply(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Apply(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var userName = currentUser.UserName ?? SystemUser;

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry is { State: EntityState.Deleted, Entity: ISoftDeletable softDeletable })
            {
                entry.State = EntityState.Modified;
                softDeletable.IsDeleted = true;
                softDeletable.DeletedAt = now;
                softDeletable.DeletedBy = userName;
            }

            if (entry.Entity is not AuditableEntity auditable)
            {
                continue;
            }

            if (entry.State == EntityState.Added)
            {
                auditable.CreatedAt = now;
                auditable.CreatedBy = userName;
            }
            else if (entry.State == EntityState.Modified)
            {
                auditable.UpdatedAt = now;
                auditable.UpdatedBy = userName;
                entry.Property(nameof(AuditableEntity.CreatedAt)).IsModified = false;
                entry.Property(nameof(AuditableEntity.CreatedBy)).IsModified = false;
            }
        }
    }
}
