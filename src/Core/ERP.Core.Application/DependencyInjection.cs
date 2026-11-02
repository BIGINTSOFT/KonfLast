using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Core.Application.Features.Auth;
using ERP.Core.Application.Features.AuditLogs;
using ERP.Core.Application.Features.Finance;
using ERP.Core.Application.Features.Integrations;
using ERP.Core.Application.Features.MasterData;
using ERP.Core.Application.Features.Roles;
using ERP.Core.Application.Features.Users;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using ERP.Core.Application.Features.Workspace;

namespace ERP.Core.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddOptions<AuthOptions>()
            .BindConfiguration(AuthOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<PasswordPolicyOptions>()
            .BindConfiguration(PasswordPolicyOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<TabletPairingOptions>().BindConfiguration(TabletPairingOptions.SectionName);
        services.AddOptions<FloorSessionOptions>().BindConfiguration(FloorSessionOptions.SectionName);

        services.AddScoped<ICurrentPermissions, CurrentPermissions>();
        services.AddScoped<IReferenceUsage, ReferenceUsage>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IAuditLogService, AuditLogService>();

        services.AddOptions<ExchangeRateOptions>()
            .BindConfiguration(ExchangeRateOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddScoped<IExchangeRateService, ExchangeRateService>();

        services.AddScoped<IFormDraftService, FormDraftService>();
        services.AddScoped<IShortcutService, ShortcutService>();

        services.AddOptions<IntegrationOptions>()
            .BindConfiguration(IntegrationOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddSingleton<IIntegrationChannelRegistry, IntegrationChannelRegistry>();
        services.AddScoped<IIntegrationOutbox, IntegrationOutbox>();
        services.AddScoped<IIntegrationDispatcher, IntegrationDispatcher>();
        services.AddScoped<IIntegrationService, IntegrationService>();

        services.AddScoped<IMasterDataService<CurrencyDto, CurrencyRequest>, CurrencyService>();

        return services;
    }
}
