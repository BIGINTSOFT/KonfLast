using System.Text.Json;
using ERP.Api;
using ERP.Api.Middleware;
using ERP.Core.Application;
using ERP.Core.Application.Abstractions;
using ERP.Core.Infrastructure;
using ERP.Core.Infrastructure.Modules;
using ERP.Core.Infrastructure.Persistence.Seed;
using ERP.Core.Infrastructure.Web;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;
using Serilog.Context;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, logger) => logger
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

// Kurulumdaki (ve lisanstaki) modüller: servisleri, tabloları ve uçları yalnızca bunlar için eklenir.
var modules = ErpModuleLoader.Load(builder.Configuration);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration, modules);
builder.Services.AddHostedService<ERP.Api.Background.IntegrationDispatchWorker>();
builder.Services.AddHostedService<ERP.Api.Background.ExchangeRateFetchWorker>();
builder.Services.AddApi(builder.Configuration, modules);

var app = builder.Build();

if (modules.License is { IsValid: false } license)
{
    app.Logger.LogError("Lisans geçersiz, modüller devreye alınmadı: {Error} ({Path})", license.Error, license.Path ?? "-");
}

app.Logger.LogInformation(
    "Devredeki modüller: {Modules} (kurulu: {Installed}; lisans: {Customer})",
    modules.Modules.Count > 0 ? modules.Signature : "yok",
    string.Join(',', modules.Installed.Select(m => m.Key)),
    modules.License?.License?.Customer ?? "-");

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging();
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();

if (app.Configuration.GetValue("Swagger:Enabled", false))
{
    app.UseSwagger();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "Tekstil ERP API v1"));
}

app.UseCors(CorsPolicies.Web);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// Kimlik doğrulandıktan sonraki tüm loglara kullanıcı bilgisi eklenir.
app.Use(async (context, next) =>
{
    var currentUser = context.RequestServices.GetRequiredService<ICurrentUser>();
    using (LogContext.PushProperty("UserId", currentUser.UserId))
    {
        await next(context);
    }
});

app.MapControllers();
app.MapHub<ERP.Api.Realtime.ProductionHub>(ERP.Api.Realtime.ProductionHub.Path);

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = (context, report) => context.Response.WriteAsJsonAsync(new
    {
        status = report.Status.ToString(),
        checks = report.Entries.ToDictionary(
            entry => JsonNamingPolicy.CamelCase.ConvertName(entry.Key), entry => entry.Value.Status.ToString()),
    }),
}).AllowAnonymous();

await SeedInitialAdminAsync(app);

app.Run();

static async Task SeedInitialAdminAsync(WebApplication app)
{
    await using var scope = app.Services.CreateAsyncScope();
    try
    {
        await scope.ServiceProvider.GetRequiredService<InitialAdminSeeder>().SeedAsync();
    }
    catch (Exception exception)
    {
        // Veritabanı henüz kurulmamış olabilir; uygulama yine de ayağa kalkar, /health durumu gösterir.
        app.Logger.LogWarning(exception, "İlk yönetici kullanıcısı oluşturulamadı; veritabanı hazır olmayabilir.");
    }
}
