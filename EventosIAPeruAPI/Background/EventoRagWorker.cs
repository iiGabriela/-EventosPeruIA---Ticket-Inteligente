using EventosIAPeru.API.Services;

namespace EventosIAPeru.API.Background;

/// <summary>
/// US-16: revisa el índice al iniciar y cada 30 segundos, sin hacer esperar las preguntas del chatbot.
/// Cada ejecución procesa un lote de eventos nuevos o modificados.
/// </summary>
public sealed class EventoRagWorker(IServiceScopeFactory scopes, ILogger<EventoRagWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
        // La primera revisión es inmediata; las siguientes esperan el intervalo del temporizador.
        do
        {
            try
            {
                // Creamos un scope por lote para obtener un DbContext independiente de las solicitudes HTTP.
                await using var scope = scopes.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<EventoRagIndex>().Refresh(ct);
            }
            // Al cerrar la aplicación se cancela el trabajo pendiente; los demás fallos se reintentan en el siguiente ciclo.
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogError(ex, "No se pudo actualizar el índice de eventos; se reintentará."); }
        } while (await timer.WaitForNextTickAsync(ct));
    }
}
