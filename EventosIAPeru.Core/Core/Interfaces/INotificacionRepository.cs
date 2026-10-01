using EventosIAPeru.Core.Core.Entities;

namespace EventosIAPeru.Core.Core.Interfaces;

public interface INotificacionRepository
{
    Task<List<Notificacion>> GetNotificacionesDeUsuario(int usuarioId);
    Task<Notificacion?> GetNotificacionDeUsuario(int usuarioId, int notificacionId);
    Task<bool> ExisteNotificacion(int usuarioId, int eventoId, string titulo);
    Task<List<Compra>> GetComprasDeEventosProximos(DateTime desde, DateTime hasta, int? usuarioId = null);
    Task<List<int>> GetUsuariosConCompraConfirmada(int eventoId);
    Task CrearNotificaciones(IEnumerable<Notificacion> notificaciones);
    Task MarcarComoLeida(Notificacion notificacion);
    Task MarcarTodasComoLeidas(int usuarioId);
}
