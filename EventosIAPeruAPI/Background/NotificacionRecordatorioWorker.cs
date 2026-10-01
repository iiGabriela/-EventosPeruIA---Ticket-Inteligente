using EventosIAPeru.Core.Core.Interfaces;

namespace EventosIAPeru.API.Background;

/// <summary>
/// Genera recordatorios internos para eventos que comienzan dentro de las próximas 24 horas.
/// </summary>
public class NotificacionRecordatorioWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NotificacionRecordatorioWorker> _logger;

    public NotificacionRecordatorioWorker(IServiceScopeFactory scopeFactory,
                                          ILogger<NotificacionRecordatorioWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Ejecutar(stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(15));
        while (await timer.WaitForNextTickAsync(stoppingToken))
            await Ejecutar(stoppingToken);
    }

    private async Task Ejecutar(CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var service = scope.ServiceProvider.GetRequiredService<INotificacionService>();
            await service.GenerarRecordatoriosProximos();
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // La aplicación se está cerrando.
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "No se pudieron generar los recordatorios de eventos.");
        }
    }
}
