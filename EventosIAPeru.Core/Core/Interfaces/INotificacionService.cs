using EventosIAPeru.Core.Core.DTOs;
using EventosIAPeru.Core.Core.Entities;
using EventosIAPeru.Core.Shared;

namespace EventosIAPeru.Core.Core.Interfaces;

public interface INotificacionService
{
    Task<List<NotificacionDTO>?> ObtenerMisNotificaciones(string firebaseUid);
    Task<ResultadoOperacion> MarcarComoLeida(string firebaseUid, int notificacionId);
    Task<ResultadoOperacion> MarcarTodasComoLeidas(string firebaseUid);
    Task GenerarRecordatoriosProximos(int? usuarioId = null);
    Task NotificarCambioEvento(Evento eventoAnterior, Evento eventoActualizado);
    Task NotificarCancelacionEvento(Evento evento);
}
