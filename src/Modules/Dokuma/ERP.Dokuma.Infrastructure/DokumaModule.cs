using ERP.Core.Infrastructure.Background;
using ERP.Dokuma.Application.Features.Floor;
using System.Reflection;
using ERP.Core.Domain.Authorization;
using ERP.Core.Infrastructure.Modules;
using ERP.Core.Infrastructure.Persistence;
using ERP.Dokuma.Application;
using ERP.Dokuma.Domain.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace ERP.Dokuma.Infrastructure;

/// <summary>Dokuma modülü: tezgah, çözgü / levent, desen ve top takibi (geliştiriliyor).</summary>
public sealed class DokumaModule : ErpModule
{
    public const string ModuleKey = "Dokuma";

    public override string Key => ModuleKey;

    public override string DisplayName => "Dokuma";

    public override Assembly DomainAssembly => typeof(DokumaPermissions).Assembly;

    public override IReadOnlyList<PermissionDefinition> Permissions => DokumaPermissions.All;

    public override void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDokumaApplication();
        services.AddHostedService<FloorSessionAutoCloseWorker<IDokumaWorkSessionService>>();
    }
}

/// <summary>Dokuma şemasının migration'ları (<c>Dokuma.__EFMigrationsHistory</c>).</summary>
public sealed class DokumaMigrationsDbContext(DbContextOptions options)
    : ModuleMigrationsDbContext(options, Module)
{
    private static readonly DokumaModule Module = new();

    public static DokumaMigrationsDbContext Create(string connectionString) =>
        new(BuildOptions(connectionString, Module, typeof(DokumaMigrationsDbContext)));
}

public sealed class DokumaMigrationsDbContextFactory : IDesignTimeDbContextFactory<DokumaMigrationsDbContext>
{
    public DokumaMigrationsDbContext CreateDbContext(string[] args) =>
        DokumaMigrationsDbContext.Create(MigrationOptions.DesignTimeConnectionString);
}
