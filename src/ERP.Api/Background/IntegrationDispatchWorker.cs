using ERP.Core.Application.Features.Integrations;
using Microsoft.Extensions.Options;

namespace ERP.Api.Background;

/// <summary>
/// Bekleyen entegrasyon teslimlerini düzenli aralıkla dış sistemlere gönderir.
/// <c>Integrations:DispatchEnabled</c> ile kapatılabilir (olaylar yine yazılır, gönderilmez).
/// </summary>
public sealed class IntegrationDispatchWorker(
    IServiceScopeFactory scopeFactory, IOptions<IntegrationOptions> options, ILogger<IntegrationDispatchWorker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.DispatchEnabled)
        {
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(options.Value.PollSeconds));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                int processed;
                do
                {
                    // Her tur kendi kapsamında: işlenen teslimler bellekte birikmez.
                    await using var scope = scopeFactory.CreateAsyncScope();
                    processed = await scope.ServiceProvider.GetRequiredService<IIntegrationDispatcher>()
                        .DispatchDueAsync(stoppingToken);
                }
                while (processed >= options.Value.BatchSize);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                // Bir tur başarısız olursa (ör. veritabanı geçici olarak erişilemez) sonraki turda yeniden denenir.
                logger.LogError(exception, "Entegrasyon teslimleri gönderilemedi.");
            }
        }
    }
}
