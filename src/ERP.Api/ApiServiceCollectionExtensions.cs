using ERP.Core.Infrastructure.Modules;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using ERP.Core.Infrastructure.Web.Authorization;
using ERP.Core.Infrastructure.Web;
using ERP.Api.Realtime;
using ERP.Api.Services;
using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Core.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

namespace ERP.Api;

public static class ApiServiceCollectionExtensions
{
    private const string HubsPath = "/hubs";

    // Not: configuration değerleri bilinçli olarak lambda'ların içinde (geç) okunur; böylece
    // host oluşturulurken eklenen kaynaklar (ortam değişkenleri, test ayarları) da geçerli olur.
    public static IServiceCollection AddApi(
        this IServiceCollection services, IConfiguration configuration, ErpModuleCatalog modules)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();

        var mvc = services
            .AddControllers(options =>
            {
                options.Filters.Add<ValidationFilter>();
                options.ModelBinderProviders.Insert(0, new SnakeUpperEnumModelBinderProvider());
            })
            .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(
                new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseUpper)))
            .ConfigureApiBehaviorOptions(options => options.InvalidModelStateResponseFactory = context =>
            {
                var details = context.ModelState
                    .Where(entry => entry.Value?.Errors.Count > 0)
                    .ToDictionary(
                        entry => JsonNamingPolicy.CamelCase.ConvertName(entry.Key),
                        entry => entry.Value!.Errors.Select(e => e.ErrorMessage).ToArray());

                return new BadRequestObjectResult(ApiResponse.Failure(
                    context.HttpContext, ErrorCodes.ValidationError, ValidationFilter.ValidationMessage, details));
            });

        // Modül uçları: modül assembly'leri API'ye derleme anında bağlı olmadığından açıkça eklenir.
        foreach (var module in modules.Modules)
        {
            mvc.AddApplicationPart(module.GetType().Assembly);
        }

        services.AddJwtAuthentication();
        services.AddPermissionAuthorization();
        services.AddAuthRateLimiting(configuration);

        services.AddCors(options => options.AddPolicy(CorsPolicies.Web, policy => policy
            .WithOrigins(configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
            .AllowAnyHeader()
            .AllowAnyMethod()
            .WithExposedHeaders(Middleware.CorrelationIdMiddleware.HeaderName)));

        services.AddSwagger();
        services.AddLiveUpdates();

        return services;
    }

    /// <summary>SignalR hub'ı ve commit edilen değişiklikleri yayınlayan arka plan işi.</summary>
    private static void AddLiveUpdates(this IServiceCollection services)
    {
        services.AddSignalR().AddJsonProtocol(options => options.PayloadSerializerOptions.Converters.Add(
            new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseUpper)));

        services.AddOptions<LiveUpdateOptions>()
            .BindConfiguration(LiveUpdateOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddSingleton<LiveUpdateQueue>();
        services.AddSingleton<ILiveNotifier>(provider => provider.GetRequiredService<LiveUpdateQueue>());
        services.AddHostedService<LiveUpdateBroadcaster>();
    }

    private static void AddJwtAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwtOptions) =>
            {
                var jwt = jwtOptions.Value;

                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = JwtRegisteredClaimNames.UniqueName,
                    RoleClaimType = JwtTokenService.RoleClaimType,
                };

                // 401 / 403 yanıtları da standart API zarfı ile dönsün.
                bearer.Events = new JwtBearerEvents
                {
                    // Tarayıcı WebSocket / SSE isteğine Authorization başlığı ekleyemez; SignalR token'ı
                    // sorgu parametresiyle gönderir. Yalnızca hub yolunda kabul edilir.
                    OnMessageReceived = context =>
                    {
                        if (context.HttpContext.Request.Path.StartsWithSegments(HubsPath)
                            && context.Request.Query["access_token"].ToString() is { Length: > 0 } token)
                        {
                            context.Token = token;
                        }

                        return Task.CompletedTask;
                    },
                    OnChallenge = async context =>
                    {
                        context.HandleResponse();
                        if (!context.Response.HasStarted)
                        {
                            await ApiResponse.WriteFailureAsync(
                                context.HttpContext,
                                StatusCodes.Status401Unauthorized,
                                ErrorCodes.Unauthorized,
                                "Bu işlem için oturum açmanız gerekiyor.");
                        }
                    },
                    OnForbidden = context => ApiResponse.WriteFailureAsync(
                        context.HttpContext,
                        StatusCodes.Status403Forbidden,
                        ErrorCodes.Forbidden,
                        "Bu işlem için yetkiniz bulunmuyor."),
                };
            });
    }

    private static void AddPermissionAuthorization(this IServiceCollection services)
    {
        // Açıkça [AllowAnonymous] denmedikçe her endpoint KULLANICI oturumu ister. Tablet cihaz token'ı
        // kullanıcı uçlarında geçmez; tabletler yalnızca TabletDevice policy'li uçlara erişir.
        var userPolicy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .RequireAssertion(context => !context.User.HasClaim(c => c.Type == JwtTokenService.TabletIdClaimType))
            .Build();

        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(userPolicy)
            .AddPolicy(AuthorizationPolicies.User, userPolicy)
            .AddPolicy(AuthorizationPolicies.TabletDevice, policy => policy
                .RequireAuthenticatedUser()
                .RequireClaim(JwtTokenService.TabletIdClaimType)
                .RequireClaim(JwtTokenService.TabletModuleClaimType, "Konfeksiyon"))
            .AddPolicy(AuthorizationPolicies.BoyahaneTablet, policy => policy
                .RequireAuthenticatedUser()
                .RequireClaim(JwtTokenService.TabletIdClaimType)
                .RequireClaim(JwtTokenService.TabletModuleClaimType, "Boyahane"))
            .AddPolicy(AuthorizationPolicies.DokumaTablet, policy => policy
                .RequireAuthenticatedUser()
                .RequireClaim(JwtTokenService.TabletIdClaimType)
                .RequireClaim(JwtTokenService.TabletModuleClaimType, "Dokuma"))
            .AddPolicy(AuthorizationPolicies.OrmeTablet, policy => policy
                .RequireAuthenticatedUser()
                .RequireClaim(JwtTokenService.TabletIdClaimType)
                .RequireClaim(JwtTokenService.TabletModuleClaimType, "Orme"));

        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
    }

    private static void AddAuthRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, _) => await ApiResponse.WriteFailureAsync(
                context.HttpContext,
                StatusCodes.Status429TooManyRequests,
                ErrorCodes.RateLimited,
                "Çok fazla istek gönderildi. Lütfen biraz sonra tekrar deneyin.");

            // Giriş / token yenileme uçları IP başına sınırlandırılır (brute force koruması).
            options.AddPolicy(RateLimitPolicies.Auth, httpContext => RateLimitPartition.GetFixedWindowLimiter(
                httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = configuration.GetValue("RateLimiting:Auth:PermitLimit", 20),
                    Window = TimeSpan.FromSeconds(configuration.GetValue("RateLimiting:Auth:WindowSeconds", 60)),
                }));
        });
    }

    private static void AddSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Tekstil ERP API",
                Version = "v1",
                Description = "Tekstil üretim ERP sistemi: çekirdek ve kurulumdaki modüller (Konfeksiyon, Boyahane, Dokuma, Örme).",
            });

            const string schemeName = "Bearer";
            options.AddSecurityDefinition(schemeName, new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Login endpoint'inden alınan access token.",
            });
            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(schemeName, document)] = [],
            });

            var moduleDirectory = Path.Combine(AppContext.BaseDirectory, ErpModuleLoader.ModulesFolder);
            foreach (var xmlFile in Directory.EnumerateFiles(AppContext.BaseDirectory, "ERP.*.xml")
                         .Concat(Directory.Exists(moduleDirectory) ? Directory.EnumerateFiles(moduleDirectory, "ERP.*.xml") : []))
            {
                options.IncludeXmlComments(xmlFile);
            }
        });
    }
}
