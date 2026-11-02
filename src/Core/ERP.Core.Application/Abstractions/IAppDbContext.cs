using ERP.Core.Domain.Entities;
using ERP.Core.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace ERP.Core.Application.Abstractions;

/// <summary>
/// Application katmanının veritabanına erişimi. Generic repository yerine EF Core'un
/// sorgulama gücünden doğrudan yararlanılır.
/// </summary>
public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<Permission> Permissions { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<RolePermission> RolePermissions { get; }
    DbSet<RefreshToken> RefreshTokens { get; }

    DbSet<Currency> Currencies { get; }
    DbSet<ExchangeRate> ExchangeRates { get; }

    DbSet<UserShortcut> UserShortcuts { get; }
    DbSet<UserFormDraft> UserFormDrafts { get; }
    DbSet<PasswordResetToken> PasswordResetTokens { get; }


    DbSet<IntegrationMessage> IntegrationMessages { get; }
    DbSet<IntegrationDelivery> IntegrationDeliveries { get; }

    DbSet<AuditLog> AuditLogs { get; }

    /// <summary>Modüllerin kendi tablolarına erişimi (modül arayüzleri bunu kullanır).</summary>
    DbSet<TEntity> Set<TEntity>() where TEntity : class;

    EntityEntry<TEntity> Entry<TEntity>(TEntity entity) where TEntity : class;

    /// <summary>Birden fazla adımı tek transaction'da toplamak için.</summary>
    DatabaseFacade Database { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
