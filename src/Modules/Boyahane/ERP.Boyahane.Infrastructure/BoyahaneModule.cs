using ERP.Core.Infrastructure.Background;
using ERP.Boyahane.Application.Features.Floor;
using System.Reflection;
using ERP.Core.Domain.Authorization;
using ERP.Core.Infrastructure.Modules;
using ERP.Core.Infrastructure.Persistence;
using ERP.Boyahane.Application;
using ERP.Boyahane.Domain.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace ERP.Boyahane.Infrastructure;

/// <summary>Boyahane modülü: boya partileri, reçete ve laboratuvar onayı, makine yükleme (geliştiriliyor).</summary>
public sealed class BoyahaneModule : ErpModule
{
    public const string ModuleKey = "Boyahane";

    public override string Key => ModuleKey;

    public override string DisplayName => "Boyahane";

    public override Assembly DomainAssembly => typeof(BoyahanePermissions).Assembly;

    public override IReadOnlyList<PermissionDefinition> Permissions => BoyahanePermissions.All;

    public override void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddBoyahaneApplication();
        services.AddHostedService<FloorSessionAutoCloseWorker<IBoyahaneWorkSessionService>>();
    }
}

/// <summary>Boyahane şemasının migration'ları (<c>Boyahane.__EFMigrationsHistory</c>).</summary>
public sealed class BoyahaneMigrationsDbContext(DbContextOptions options)
    : ModuleMigrationsDbContext(options, Module)
{
    private static readonly BoyahaneModule Module = new();

    public static BoyahaneMigrationsDbContext Create(string connectionString) =>
        new(BuildOptions(connectionString, Module, typeof(BoyahaneMigrationsDbContext)));
}

public sealed class BoyahaneMigrationsDbContextFactory : IDesignTimeDbContextFactory<BoyahaneMigrationsDbContext>
{
    public BoyahaneMigrationsDbContext CreateDbContext(string[] args) =>
        BoyahaneMigrationsDbContext.Create(MigrationOptions.DesignTimeConnectionString);
}
