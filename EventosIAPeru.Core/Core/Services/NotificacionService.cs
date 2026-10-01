using EventosIAPeru.Core.Core.DTOs;
using EventosIAPeru.Core.Core.Entities;
using EventosIAPeru.Core.Core.Interfaces;
using EventosIAPeru.Core.Shared;

namespace EventosIAPeru.Core.Core.Services;

public class NotificacionService : INotificacionService
{
    private const string RecordatorioTitulo = "Recordatorio de evento";
    private const string PrioridadInformativa = "INFORMATIVO";
    private const string PrioridadPrioritaria = "PRIORITARIO";

    private readonly INotificacionRepository _notificacionRepository;
    private readonly IUsuarioRepository _usuarioRepository;

    public NotificacionService(INotificacionRepository notificacionRepository,
                               IUsuarioRepository usuarioRepository)
    {
        _notificacionRepository = notificacionRepository;
        _usuarioRepository = usuarioRepository;
    }

    public async Task<List<NotificacionDTO>?> ObtenerMisNotificaciones(string firebaseUid)
    {
        var usuario = await _usuarioRepository.GetUsuarioByFirebaseUid(firebaseUid);
        if (usuario == null)
            return null;

        // Respalda al proceso en segundo plano cuando el usuario abre la bandeja.
        await GenerarRecordatoriosProximos(usuario.UsuarioId);

        var notificaciones = await _notificacionRepository.GetNotificacionesDeUsuario(usuario.UsuarioId);
        return notificaciones.Select(Mapear).ToList();
    }

    public async Task<ResultadoOperacion> MarcarComoLeida(string firebaseUid, int notificacionId)
    {
        var usuario = await _usuarioRepository.GetUsuarioByFirebaseUid(firebaseUid);
        if (usuario == null)
            return ResultadoOperacion.NoAutorizado("Tu usuario no está registrado.");

        var notificacion = await _notificacionRepository.GetNotificacionDeUsuario(usuario.UsuarioId, notificacionId);
        if (notificacion == null)
            return ResultadoOperacion.NoEncontrado("La notificación no existe.");

        if (!notificacion.Leida)
            await _notificacionRepository.MarcarComoLeida(notificacion);

        return ResultadoOperacion.Ok(notificacionId);
    }

    public async Task<ResultadoOperacion> MarcarTodasComoLeidas(string firebaseUid)
    {
        var usuario = await _usuarioRepository.GetUsuarioByFirebaseUid(firebaseUid);
        if (usuario == null)
            return ResultadoOperacion.NoAutorizado("Tu usuario no está registrado.");

        await _notificacionRepository.MarcarTodasComoLeidas(usuario.UsuarioId);
        return ResultadoOperacion.Ok();
    }

    public async Task GenerarRecordatoriosProximos(int? usuarioId = null)
    {
        var ahora = DateTime.UtcNow;
        var hasta = ahora.AddHours(24);
        var compras = await _notificacionRepository.GetComprasDeEventosProximos(ahora, hasta, usuarioId);
        var nuevas = new List<Notificacion>();
        var procesadas = new HashSet<(int UsuarioId, int EventoId)>();

        foreach (var compra in compras)
        {
            if (compra.Evento == null)
                continue;

            // Un usuario puede comprar más de una vez para el mismo evento.
            // El recordatorio debe ser único por usuario y evento.
            if (!procesadas.Add((compra.UsuarioId, compra.EventoId)))
                continue;

            var existe = await _notificacionRepository.ExisteNotificacion(
                compra.UsuarioId,
                compra.EventoId,
                RecordatorioTitulo);

            if (existe)
                continue;

            nuevas.Add(new Notificacion
            {
                UsuarioId = compra.UsuarioId,
                EventoId = compra.EventoId,
                Titulo = RecordatorioTitulo,
                Mensaje = $"El evento \"{compra.Evento.Nombre}\" se realizará el {compra.Evento.FechaInicio:dd/MM/yyyy HH:mm}.",
                Prioridad = PrioridadInformativa,
                Leida = false,
                FechaCreacion = ahora
            });
        }

        await _notificacionRepository.CrearNotificaciones(nuevas);
    }

    public async Task NotificarCambioEvento(Evento eventoAnterior, Evento eventoActualizado)
    {
        var cambioFecha = eventoAnterior.FechaInicio != eventoActualizado.FechaInicio
                       || eventoAnterior.FechaFin != eventoActualizado.FechaFin;
        var cambioUbicacion = !string.Equals(eventoAnterior.Sede, eventoActualizado.Sede, StringComparison.OrdinalIgnoreCase)
                           || !string.Equals(eventoAnterior.Direccion, eventoActualizado.Direccion, StringComparison.OrdinalIgnoreCase)
                           || !string.Equals(eventoAnterior.Departamento, eventoActualizado.Departamento, StringComparison.OrdinalIgnoreCase)
                           || !string.Equals(eventoAnterior.Provincia, eventoActualizado.Provincia, StringComparison.OrdinalIgnoreCase)
                           || !string.Equals(eventoAnterior.Distrito, eventoActualizado.Distrito, StringComparison.OrdinalIgnoreCase);

        if (!cambioFecha && !cambioUbicacion)
            return;

        var cambios = new List<string>();
        if (cambioFecha)
            cambios.Add($"la nueva fecha es {eventoActualizado.FechaInicio:dd/MM/yyyy HH:mm}");
        if (cambioUbicacion)
            cambios.Add($"la nueva ubicación es {eventoActualizado.Sede}, {eventoActualizado.Direccion}");

        var usuarios = await _notificacionRepository.GetUsuariosConCompraConfirmada(eventoActualizado.EventoId);
        var ahora = DateTime.UtcNow;
        var notificaciones = usuarios.Select(usuarioId => new Notificacion
        {
            UsuarioId = usuarioId,
            EventoId = eventoActualizado.EventoId,
            Titulo = "Cambio importante en tu evento",
            Mensaje = $"El evento \"{eventoActualizado.Nombre}\" fue actualizado: {string.Join(" y ", cambios)}.",
            Prioridad = PrioridadPrioritaria,
            Leida = false,
            FechaCreacion = ahora
        });

        await _notificacionRepository.CrearNotificaciones(notificaciones);
    }

    public async Task NotificarCancelacionEvento(Evento evento)
    {
        var usuarios = await _notificacionRepository.GetUsuariosConCompraConfirmada(evento.EventoId);
        var ahora = DateTime.UtcNow;
        var notificaciones = usuarios.Select(usuarioId => new Notificacion
        {
            UsuarioId = usuarioId,
            EventoId = evento.EventoId,
            Titulo = "Evento cancelado",
            Mensaje = $"El evento \"{evento.Nombre}\" fue cancelado por el organizador.",
            Prioridad = PrioridadPrioritaria,
            Leida = false,
            FechaCreacion = ahora
        });

        await _notificacionRepository.CrearNotificaciones(notificaciones);
    }

    private static NotificacionDTO Mapear(Notificacion notificacion)
    {
        var prioritaria = string.Equals(notificacion.Prioridad, PrioridadPrioritaria, StringComparison.OrdinalIgnoreCase);
        return new NotificacionDTO
        {
            NotificacionId = notificacion.NotificacionId,
            EventoId = notificacion.EventoId,
            EventoNombre = notificacion.Evento?.Nombre ?? string.Empty,
            Titulo = notificacion.Titulo,
            Mensaje = notificacion.Mensaje,
            Prioridad = prioritaria ? PrioridadPrioritaria : PrioridadInformativa,
            Icono = prioritaria ? "alert-triangle" : "info",
            Color = prioritaria ? "#DC2626" : "#2563EB",
            Leida = notificacion.Leida,
            FechaCreacion = notificacion.FechaCreacion
        };
    }
}
