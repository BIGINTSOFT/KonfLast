using ERP.Konfeksiyon.Application.Features.WorkSessions;
using Microsoft.Extensions.Options;

namespace ERP.Konfeksiyon.Infrastructure.Background;

/// <summary>
/// Vardiyası biten açık oturumları düzenli aralıkla kapatır (çıkış yapmayı unutan operatörler).
/// <c>Production:SessionAutoCloseEnabled</c> ile kapatılabilir.
/// </summary>
public sealed class SessionAutoCloseWorker(
    IServiceScopeFactory scopeFactory, IOptions<ProductionOptions> options, ILogger<SessionAutoCloseWorker> logger)
    : BackgroundService
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
                var closed = await scope.ServiceProvider.GetRequiredService<IWorkSessionService>()
                    .CloseExpiredAsync(stoppingToken);
                if (closed > 0)
                {
                    logger.LogInformation("Vardiyası biten {Count} çalışma oturumu kapatıldı.", closed);
                }
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                // Bir tur başarısız olursa (ör. veritabanı geçici olarak erişilemez) sonraki turda yeniden denenir.
                logger.LogError(exception, "Açık oturumların vardiya sonu kapanışı başarısız oldu.");
            }
        }
    }
}