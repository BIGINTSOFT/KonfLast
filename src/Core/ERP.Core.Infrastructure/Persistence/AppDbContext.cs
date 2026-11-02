using System.Linq.Expressions;
using ERP.Core.Application.Abstractions;
using ERP.Core.Domain.Common;
using ERP.Core.Domain.Entities;
using ERP.Core.Domain.Entities.Identity;
using ERP.Core.Infrastructure.Modules;
using ERP.Core.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace ERP.Core.Infrastructure.Persistence;

/// <summary>
/// Uygulamanın tek DbContext'i. Modelde çekirdek tablolar (<see cref="CoreSchema"/> şeması) ve devredeki
/// modüllerin tabloları (her modül kendi şemasında) birlikte yer alır; modüller arası işlemler tek transaction'dır.
/// </summary>
public class AppDbContext : DbContext, IAppDbContext
{
    /// <summary>Çekirdek tabloların şeması.</summary>
    public const string CoreSchema = "Core";

    private readonly IReadOnlyList<ErpModule> _modules;

    public AppDbContext(DbContextOptions<AppDbContext> options, ErpModuleCatalog modules)
        : this((DbContextOptions)options, modules.Modules)
    {
    }

    protected AppDbContext(DbContextOptions options, IReadOnlyList<ErpModule> modules) : base(options)
    {
        _modules = modules;

        // Soft delete, Remove() çağrısını SaveChanges sırasında güncellemeye çevirir. Zincirleme
        // silme kontrolü anında yapılırsa, alt kayıtları yüklenmiş bir entity silinirken EF bu
        // dönüşümden önce hata verir; kontrol kayıt anına ertelenir. (Fiziksel silmelerde
        // alt kayıt varsa hata yine alınır: ilişkiler Restrict'tir.)
        ChangeTracker.CascadeDeleteTiming = CascadeTiming.OnSaveChanges;
        ChangeTracker.DeleteOrphansTiming = CascadeTiming.OnSaveChanges;
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<Currency> Currencies => Set<Currency>();
    public DbSet<ExchangeRate> ExchangeRates => Set<ExchangeRate>();

    public DbSet<DocumentSequence> DocumentSequences => Set<DocumentSequence>();

    public DbSet<UserShortcut> UserShortcuts => Set<UserShortcut>();
    public DbSet<UserFormDraft> UserFormDrafts => Set<UserFormDraft>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

    public DbSet<IntegrationMessage> IntegrationMessages => Set<IntegrationMessage>();
    public DbSet<IntegrationDelivery> IntegrationDeliveries => Set<IntegrationDelivery>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Tüm zamanlar UTC saklanır; okunurken de UTC olarak işaretlenir ki API'den "Z" ile çıksın.
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();

        // Açıkça belirtilmedikçe nvarchar(max) / decimal(18,2) oluşmasın.
        configurationBuilder.Properties<string>().HaveMaxLength(200);
        configurationBuilder.Properties<decimal>().HavePrecision(18, 4);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(CoreSchema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        if (IncludeCoreSeedData)
        {
            CoreSeedData.Apply(modelBuilder);
        }

        foreach (var module in _modules)
        {
            // Konfigürasyon sırasında tabloya bağlanan nesneler (trigger vb.) şemayı o anda alır.
            modelBuilder.HasDefaultSchema(module.Schema);
            module.ConfigureModel(modelBuilder);
            modelBuilder.HasDefaultSchema(CoreSchema);

            // Modülün entity'leri modülün şemasına yerleşir.
            foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                         .Where(e => e.ClrType.Assembly == module.DomainAssembly))
            {
                entityType.SetSchema(module.Schema);
            }
        }

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;

            if (typeof(ISoftDeletable).IsAssignableFrom(clrType))
            {
                var parameter = Expression.Parameter(clrType, "e");
                var notDeleted = Expression.Not(Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted)));
                modelBuilder.Entity(clrType).HasQueryFilter(Expression.Lambda(notDeleted, parameter));
                modelBuilder.Entity(clrType).Property(nameof(ISoftDeletable.DeletedBy)).HasMaxLength(100);
            }

            if (typeof(AuditableEntity).IsAssignableFrom(clrType))
            {
                modelBuilder.Entity(clrType).Property(nameof(AuditableEntity.CreatedBy)).HasMaxLength(100);
                modelBuilder.Entity(clrType).Property(nameof(AuditableEntity.UpdatedBy)).HasMaxLength(100);
            }

            // Enum'lar BÜYÜK_SNAKE_CASE metin olarak saklanır (raporlarda okunabilir olsun).
            foreach (var property in entityType.GetProperties())
            {
                var enumType = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;
                if (enumType.IsEnum)
                {
                    property.SetValueConverter(UpperSnakeEnumConverter.Create(enumType));
                    property.SetMaxLength(30);
                }
            }

            // ERP verisinde zincirleme silme istenmez; silme her zaman açık bir işlemdir.
            foreach (var foreignKey in entityType.GetForeignKeys())
            {
                foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
            }
        }
    }

    /// <summary>Çekirdeğin başlangıç verisi modele eklensin mi (modül migration'larında eklenmez).</summary>
    protected virtual bool IncludeCoreSeedData => true;

    /// <summary>Model, devredeki modül setine göre önbelleğe alınır.</summary>
    internal string ModuleSignature => string.Join(',', _modules.Select(m => m.Key));
}

/// <summary>Aynı bağlam tipinin farklı modül setleriyle kurulan modellerini ayrı tutar.</summary>
public sealed class ModuleAwareModelCacheKeyFactory : IModelCacheKeyFactory
{
    public object Create(DbContext context, bool designTime) =>
        context is AppDbContext app ? (context.GetType(), app.ModuleSignature, designTime) : (context.GetType(), designTime);
}
