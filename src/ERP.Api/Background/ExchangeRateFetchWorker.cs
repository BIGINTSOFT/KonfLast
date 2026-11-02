using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Features.Finance;
using ERP.Core.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ERP.Api.Background;

/// <summary>
/// Hafta içi her gün, TCMB'nin kurları ilan ettiği saatten sonra günün bültenini çeker. Bülten henüz yoksa ya da
/// TCMB'ye ulaşılamazsa <c>ExchangeRates:RetryMinutes</c> sonra yeniden dener. <c>ExchangeRates:AutoFetch</c> ile açılır.
/// </summary>
public sealed class ExchangeRateFetchWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<ExchangeRateOptions> options,
    TimeProvider timeProvider,
    ILogger<ExchangeRateFetchWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.AutoFetch)
        {
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(options.Value.RetryMinutes), timeProvider);
        do
        {
            try
            {
                await FetchTodayAsync(stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Günün döviz kurları çekilemedi.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task FetchTodayAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetLocalNow();
        var today = DateOnly.FromDateTime(now.DateTime);
        if (now.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday || TimeOnly.FromDateTime(now.DateTime) < options.Value.FetchAfter)
        {
            return;
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        if (await db.ExchangeRates.AnyAsync(r => r.RateDate == today && r.Source == ExchangeRateSource.Tcmb, cancellationToken))
        {
            return;
        }

        var result = await scope.ServiceProvider.GetRequiredService<IExchangeRateService>()
            .FetchAsync(new FetchExchangeRatesRequest(today), cancellationToken);
        if (!result.IsSuccess)
        {
            logger.LogWarning("TCMB kurları çekilemedi: {Message}", result.Error!.Message);
        }
        else if (result.Value.BulletinDate is null)
        {
            logger.LogInformation("TCMB {Date} bülteni henüz yayımlanmamış; daha sonra yeniden denenecek.", today);
        }
        else
        {
            logger.LogInformation("TCMB {Date} kurları alındı ({Count} döviz).", today, result.Value.Imported);
        }
    }
}
