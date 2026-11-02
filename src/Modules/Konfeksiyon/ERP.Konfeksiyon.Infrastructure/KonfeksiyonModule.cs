using System.Reflection;
using ERP.Core.Domain.Authorization;
using ERP.Core.Infrastructure.Modules;
using ERP.Core.Infrastructure.Persistence;
using ERP.Konfeksiyon.Application;
using ERP.Konfeksiyon.Application.Abstractions;
using ERP.Konfeksiyon.Domain.Authorization;
using ERP.Konfeksiyon.Domain.Entities;
using ERP.Konfeksiyon.Infrastructure.Background;
using ERP.Konfeksiyon.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace ERP.Konfeksiyon.Infrastructure;

/// <summary>Konfeksiyon modülü: sipariş, üretim emri, rota, bant, demet, istasyon tableti, kalite, depo, sevkiyat.</summary>
public sealed class KonfeksiyonModule : ErpModule
{
    public const string ModuleKey = "Konfeksiyon";

    public override string Key => ModuleKey;

    public override string DisplayName => "Konfeksiyon";

    public override Assembly DomainAssembly => typeof(ProductionOrder).Assembly;

    public override IReadOnlyList<PermissionDefinition> Permissions => KonfeksiyonPermissions.All;

    public override void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddKonfeksiyonApplication();
        services.AddScoped<IKonfeksiyonDbContext, KonfeksiyonDbContext>();
        services.AddHostedService<SessionAutoCloseWorker>();
    }
}

/// <summary>Konfeksiyon şemasının migration'ları (<c>Konfeksiyon.__EFMigrationsHistory</c>).</summary>
public sealed class KonfeksiyonMigrationsDbContext(DbContextOptions options)
    : ModuleMigrationsDbContext(options, Module)
{
    private static readonly KonfeksiyonModule Module = new();

    public static KonfeksiyonMigrationsDbContext Create(string connectionString) =>
        new(BuildOptions(connectionString, Module, typeof(KonfeksiyonMigrationsDbContext)));
}

public sealed class KonfeksiyonMigrationsDbContextFactory : IDesignTimeDbContextFactory<KonfeksiyonMigrationsDbContext>
{
    public KonfeksiyonMigrationsDbContext CreateDbContext(string[] args) =>
        KonfeksiyonMigrationsDbContext.Create(MigrationOptions.DesignTimeConnectionString);
}
