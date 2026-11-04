using Microsoft.AspNetCore.SignalR;
using EventosIAPeru.Core.Core.Interfaces;

namespace EventosIAPeru.API.Hubs
{
    /// <summary>
    /// US-09: canal en tiempo real (SignalR) en /hubs/aforo.
    /// El frontend se conecta y escucha el mensaje "cuposActualizados" para refrescar
    /// la barra de aforo y el estado "Agotado" sin recargar. Es público: solo envía cupos, no datos personales.
    /// </summary>
    public class AforoHub : Hub
    {
    }

    /// <summary>Implementación de INotificadorAforo que envía el aviso a todos los clientes conectados.</summary>
    public class NotificadorAforoSignalR : INotificadorAforo
    {
        private readonly IHubContext<AforoHub> _hub;
        private readonly ILogger<NotificadorAforoSignalR> _logger;

        public NotificadorAforoSignalR(IHubContext<AforoHub> hub, ILogger<NotificadorAforoSignalR> logger)
        {
            _hub = hub;
            _logger = logger;
        }

        public async Task CuposActualizados(int eventoId, int aforoTotal, int entradasVendidas)
        {
            var cupos = Math.Max(aforoTotal - entradasVendidas, 0);
            try
            {
                await _hub.Clients.All.SendAsync("cuposActualizados", new
                {
                    eventoId,
                    aforoTotal,
                    entradasVendidas,
                    cuposDisponibles = cupos,
                    agotado = cupos == 0
                });
            }
            catch (Exception ex)
            {
                // Si falla el aviso, la compra ya está guardada: solo se registra el problema
                _logger.LogWarning(ex, "No se pudo avisar el cambio de cupos del evento {EventoId}", eventoId);
            }
        }
    }
}
