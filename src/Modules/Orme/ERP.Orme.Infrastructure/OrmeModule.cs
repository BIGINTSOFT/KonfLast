using ERP.Core.Infrastructure.Background;
using ERP.Orme.Application.Features.Floor;
using System.Reflection;
using ERP.Core.Domain.Authorization;
using ERP.Core.Infrastructure.Modules;
using ERP.Core.Infrastructure.Persistence;
using ERP.Orme.Application;
using ERP.Orme.Domain.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace ERP.Orme.Infrastructure;

/// <summary>Örme modülü: yuvarlak örme makinesi, kumaş kartı, iplik ve top takibi.</summary>
public sealed class OrmeModule : ErpModule
{
    public const string ModuleKey = "Orme";

    public override string Key => ModuleKey;

    public override string DisplayName => "Örme";

    public override Assembly DomainAssembly => typeof(OrmePermissions).Assembly;

    public override IReadOnlyList<PermissionDefinition> Permissions => OrmePermissions.All;

    public override void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOrmeApplication();
        services.AddHostedService<FloorSessionAutoCloseWorker<IOrmeWorkSessionService>>();
    }
}

/// <summary>Örme şemasının migration'ları (<c>Orme.__EFMigrationsHistory</c>).</summary>
public sealed class OrmeMigrationsDbContext(DbContextOptions options)
    : ModuleMigrationsDbContext(options, Module)
{
    private static readonly OrmeModule Module = new();

    public static OrmeMigrationsDbContext Create(string connectionString) =>
        new(BuildOptions(connectionString, Module, typeof(OrmeMigrationsDbContext)));
}

public sealed class OrmeMigrationsDbContextFactory : IDesignTimeDbContextFactory<OrmeMigrationsDbContext>
{
    public OrmeMigrationsDbContext CreateDbContext(string[] args) =>
        OrmeMigrationsDbContext.Create(MigrationOptions.DesignTimeConnectionString);
}
