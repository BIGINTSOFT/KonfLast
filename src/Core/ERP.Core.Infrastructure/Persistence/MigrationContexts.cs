using ERP.Core.Infrastructure.Modules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace ERP.Core.Infrastructure.Persistence;

/// <summary>
/// Migration geçmişi her şemada ayrı tutulur (<c>Core.__EFMigrationsHistory</c>, <c>Konfeksiyon.__EFMigrationsHistory</c> ...):
/// bir modülün migration'ı diğerini etkilemez, modül sonradan eklenip kaldırılabilir.
/// </summary>
public static class MigrationOptions
{
    public const string HistoryTable = "__EFMigrationsHistory";

    /// <summary>dotnet ef araçlarının bağlantısı (migration üretmek veritabanı gerektirmez).</summary>
    public const string DesignTimeConnectionEnvironmentVariable = "TEKSTILERP_DESIGN_CONNECTION";

    public static string DesignTimeConnectionString =>
        Environment.GetEnvironmentVariable(DesignTimeConnectionEnvironmentVariable)
        ?? "Server=(localdb)\\MSSQLLocalDB;Database=TekstilERP_Design;Trusted_Connection=True;TrustServerCertificate=True";

    public static DbContextOptions Build(string connectionString, string schema, string migrationsAssembly) =>
        new DbContextOptionsBuilder()
            .UseSqlServer(connectionString, sql => sql
                .MigrationsHistoryTable(HistoryTable, schema)
                .MigrationsAssembly(migrationsAssembly))
            .ReplaceService<IModelCacheKeyFactory, ModuleAwareModelCacheKeyFactory>()
            .Options;
}

/// <summary>Çekirdek tabloların migration'ları (modül tabloları modelde yoktur).</summary>
public sealed class CoreMigrationsDbContext(DbContextOptions options) : AppDbContext(options, [])
{
    public static CoreMigrationsDbContext Create(string connectionString) =>
        new(MigrationOptions.Build(connectionString, CoreSchema, typeof(CoreMigrationsDbContext).Assembly.GetName().Name!));
}

public sealed class CoreMigrationsDbContextFactory : IDesignTimeDbContextFactory<CoreMigrationsDbContext>
{
    public CoreMigrationsDbContext CreateDbContext(string[] args) =>
        CoreMigrationsDbContext.Create(MigrationOptions.DesignTimeConnectionString);
}

/// <summary>
/// Bir modülün migration'ları. Modelde çekirdek (ve bağımlı olunan modüllerin) tabloları da bulunur, böylece
/// foreign key'ler kurulur; ancak bu tablolar migration'a girmez, yalnızca modülün kendi tabloları oluşturulur.
/// </summary>
public abstract class ModuleMigrationsDbContext(DbContextOptions options, ErpModule owner, IReadOnlyList<ErpModule>? dependencies = null)
    : AppDbContext(options, [.. dependencies ?? [], owner])
{
    protected static DbContextOptions BuildOptions(string connectionString, ErpModule owner, Type contextType) =>
        MigrationOptions.Build(connectionString, owner.Schema, contextType.Assembly.GetName().Name!);

    protected override bool IncludeCoreSeedData => false;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                     .Where(e => e.ClrType.Assembly != owner.DomainAssembly && e.FindOwnership() is null))
        {
            entityType.SetIsTableExcludedFromMigrations(true);
        }
    }
}
