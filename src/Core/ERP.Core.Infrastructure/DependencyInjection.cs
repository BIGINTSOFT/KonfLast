using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Features.Finance;
using ERP.Core.Application.Features.Integrations;
using ERP.Core.Infrastructure.Email;
using ERP.Core.Infrastructure.Finance;
using ERP.Core.Infrastructure.Integrations;
using ERP.Core.Infrastructure.Modules;
using ERP.Core.Infrastructure.Persistence;
using ERP.Core.Infrastructure.Persistence.Interceptors;
using ERP.Core.Infrastructure.Persistence.Seed;
using ERP.Core.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace ERP.Core.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "DefaultConnection";

    /// <param name="modules">Bu kurulumda devrede olan modüller; servisleri ve tabloları buradan eklenir.</param>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration, ErpModuleCatalog modules)
    {
        services.AddSingleton(modules);
        services.AddSingleton<IActivePermissions, ActivePermissions>();
        services.AddSingleton(TimeProvider.System);
        services.AddMemoryCache();

        services.AddScoped<AuditSaveChangesInterceptor>();
        services.AddScoped<AuditLogInterceptor>();
        // Canlı yayını API katmanı sağlar; sağlamayan ortamda bildirimler yok sayılır.
        services.TryAddSingleton<ILiveNotifier, NullLiveNotifier>();
        services.AddScoped<LiveChangeInterceptor>();
        services.AddDbContext<AppDbContext>((provider, options) =>
        {
            var connectionString = provider.GetRequiredService<IConfiguration>()
                .GetConnectionString(ConnectionStringName)
                ?? throw new InvalidOperationException(
                    $"'{ConnectionStringName}' connection string tanımlı değil.");

            options.UseSqlServer(connectionString, sql => sql.MigrationsHistoryTable(
                MigrationOptions.HistoryTable, AppDbContext.CoreSchema));
            options.ReplaceService<IModelCacheKeyFactory, ModuleAwareModelCacheKeyFactory>();
            // Sıra önemli: önce audit alanları / soft delete uygulanır, sonra değişiklikler loglanır.
            options.AddInterceptors(
                provider.GetRequiredService<AuditSaveChangesInterceptor>(),
                provider.GetRequiredService<AuditLogInterceptor>(),
                provider.GetRequiredService<LiveChangeInterceptor>());
        });
        services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());

        services.AddOptions<JwtOptions>()
            .BindConfiguration(JwtOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddOptions<InitialAdminOptions>().BindConfiguration(InitialAdminOptions.SectionName);

        services.AddOptions<EmailOptions>().BindConfiguration(EmailOptions.SectionName).ValidateDataAnnotations();
        services.AddSingleton<IEmailSender, SmtpEmailSender>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IDocumentNumberGenerator, DocumentNumberGenerator>();
        services.AddScoped<IAppLock, SqlServerAppLock>();
        services.AddScoped<InitialAdminSeeder>();

        // Dış sistem kanalları: yeni bir entegrasyon (e-İrsaliye, SAP ...) buraya bir kanal olarak eklenir.
        services.AddSingleton<IIntegrationChannelProvider, WebhookChannelProvider>();

        // Kur kaynağı: sistem vekil sunucusu kullanılır (kurum ağı); bağlantılar paylaşılır.
        services.AddSingleton<IExchangeRateProvider>(provider => new TcmbExchangeRateProvider(
            new HttpMessageInvoker(new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(5),
                ConnectTimeout = TimeSpan.FromSeconds(10),
            }),
            provider.GetRequiredService<IOptions<ExchangeRateOptions>>()));

        services.AddHealthChecks().AddDbContextCheck<AppDbContext>("database");

        foreach (var module in modules.Modules)
        {
            module.AddServices(services, configuration);
        }

        return services;
    }
}
