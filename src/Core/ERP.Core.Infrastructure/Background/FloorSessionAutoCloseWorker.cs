using ERP.Core.Application.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ERP.Core.Infrastructure.Background;

/// <summary>
/// Vardiyası biten açık saha oturumlarını düzenli aralıkla kapatır (çıkış yapmayı unutan operatörler).
/// <c>Production:SessionAutoCloseEnabled</c> ile kapatılabilir.
/// </summary>
public sealed class FloorSessionAutoCloseWorker<TService>(
    IServiceScopeFactory scopeFactory, IOptions<FloorSessionOptions> options, ILogger<FloorSessionAutoCloseWorker<TService>> logger)
    : BackgroundService
    where TService : IFloorSessionExpiry
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.SessionAutoCloseEnabled)
        {
            return;
        }

        using var timer = new PeriodicTimer(Interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var closed = await scope.ServiceProvider.GetRequiredService<TService>().CloseExpiredAsync(stoppingToken);
                if (closed > 0)
                {
                    logger.LogInformation("Vardiyası biten {Count} saha oturumu kapatıldı.", closed);
                }
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                // Bir tur başarısız olursa (ör. veritabanı geçici olarak erişilemez) sonraki turda yeniden denenir.
                logger.LogError(exception, "Açık saha oturumlarının vardiya sonu kapanışı başarısız oldu.");
            }
        }
    }
}
