using EventosIAPeru.Core.Core.DTOs;
using EventosIAPeru.Core.Core.Entities;
using EventosIAPeru.Core.Shared;

namespace EventosIAPeru.Core.Core.Interfaces;

public interface INotificacionService
{
    Task<List<NotificacionDTO>?> ObtenerMisNotificaciones(string emailUsuario);
    Task<ResultadoOperacion> MarcarComoLeida(string emailUsuario, int notificacionId);
    Task<ResultadoOperacion> MarcarTodasComoLeidas(string emailUsuario);
    Task GenerarRecordatoriosProximos(int? usuarioId = null);
    Task NotificarCambioEvento(Evento eventoAnterior, Evento eventoActualizado);
    Task NotificarCancelacionEvento(Evento evento);
    /// <summary>US-15: avisa a los administradores que hay un evento por aprobar.</summary>
    Task NotificarSolicitudAprobacion(Evento evento);
    /// <summary>US-15: avisa al organizador si su evento fue aprobado, rechazado o suspendido.</summary>
    Task NotificarDecisionModeracion(Evento evento, string estadoModeracion, string? motivo);
}
