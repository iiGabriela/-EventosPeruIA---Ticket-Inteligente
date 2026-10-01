using EventosIAPeru.Core.Core.DTOs;
using EventosIAPeru.Core.Core.Entities;
using EventosIAPeru.Core.Core.Interfaces;
using EventosIAPeru.Core.Shared;

namespace EventosIAPeru.Core.Core.Services
{
    /// <summary>
    /// Service: reglas de US-13. Un asistente puede reseñar un evento si:
    /// 1) su cuenta está activa, 2) el evento ya terminó, 3) tiene una entrada válida
    /// de una compra confirmada y 4) todavía no lo reseñó (una reseña por evento).
    /// </summary>
    public class ResenaService : IResenaService
    {
        private readonly IResenaRepository _resenaRepository;
        private readonly IUsuarioRepository _usuarioRepository;

        public ResenaService(IResenaRepository resenaRepository, IUsuarioRepository usuarioRepository)
        {
            _resenaRepository = resenaRepository;
            _usuarioRepository = usuarioRepository;
        }

        public async Task<ResultadoOperacion> CrearResena(string firebaseUid, CrearResenaDTO dto)
        {
            // Debe ser un usuario registrado y activo
            var usuario = await _usuarioRepository.GetUsuarioByFirebaseUid(firebaseUid);
            if (usuario == null) return ResultadoOperacion.NoAutorizado("Tu usuario no está registrado.");
            if (usuario.Estado != "ACTIVO") return ResultadoOperacion.NoAutorizado("Tu cuenta no está activa.");

            // El evento debe existir
            var evento = await _resenaRepository.ObtenerEvento(dto.EventoId);
            if (evento == null) return ResultadoOperacion.NoEncontrado("El evento no existe.");

            // Solo se reseña cuando el evento ya terminó
            if (evento.FechaFin > DateTime.UtcNow)
                return ResultadoOperacion.Invalido("Solo puedes reseñar el evento cuando haya terminado.");

            // Debe tener una entrada válida de ese evento
            var tieneEntrada = await _resenaRepository.TieneEntradaValida(usuario.UsuarioId, evento.EventoId);
            if (!tieneEntrada)
                return ResultadoOperacion.NoAutorizado("Solo puedes reseñar eventos para los que tienes una entrada válida.");

            // Una sola reseña por evento
            var yaReseno = await _resenaRepository.ExisteResena(usuario.UsuarioId, evento.EventoId);
            if (yaReseno)
                return ResultadoOperacion.Invalido("Ya registraste una reseña para este evento.");

            // Si el comentario viene vacío, lo guardamos como null
            var comentario = dto.Comentario?.Trim();

            var resena = new ResenaEvento
            {
                UsuarioId = usuario.UsuarioId,
                EventoId = evento.EventoId,
                Calificacion = (short)dto.Calificacion,
                Comentario = string.IsNullOrWhiteSpace(comentario) ? null : comentario,
                FechaCreacion = DateTime.UtcNow
            };

            var creada = await _resenaRepository.CrearResena(resena);
            if (!creada) return ResultadoOperacion.Invalido("No se pudo registrar la reseña.");

            return ResultadoOperacion.Ok(resena.ResenaId);
        }

        public async Task<ResumenResenasDTO?> GetResenasDeEvento(int eventoId)
        {
            var evento = await _resenaRepository.ObtenerEvento(eventoId);
            if (evento == null) return null;

            var (promedio, total) = await _resenaRepository.GetResumen(eventoId);
            var resenas = await _resenaRepository.GetResenasDeEvento(eventoId);

            return new ResumenResenasDTO
            {
                EventoId = eventoId,
                Promedio = Math.Round(promedio, 1),
                Total = total,
                Resenas = resenas.Select(Mapear).ToList()
            };
        }

        public async Task<IEnumerable<ResenaDTO>?> GetMisResenas(string firebaseUid)
        {
            var usuario = await _usuarioRepository.GetUsuarioByFirebaseUid(firebaseUid);
            if (usuario == null) return null;

            var resenas = await _resenaRepository.GetResenasDeUsuario(usuario.UsuarioId);
            return resenas.Select(Mapear).ToList();
        }

        public async Task<PendientesResenaDTO?> GetPendientes(string firebaseUid)
        {
            var usuario = await _usuarioRepository.GetUsuarioByFirebaseUid(firebaseUid);
            if (usuario == null) return null;

            var eventos = await _resenaRepository.GetEventosPendientes(usuario.UsuarioId);

            return new PendientesResenaDTO
            {
                Total = eventos.Count,
                Eventos = eventos.Select(e => new EventoPendienteResenaDTO
                {
                    EventoId = e.EventoId,
                    Nombre = e.Nombre,
                    Sede = e.Sede,
                    FechaFin = e.FechaFin
                }).ToList()
            };
        }

        // Convierte la entidad en DTO
        private static ResenaDTO Mapear(ResenaEvento r)
        {
            return new ResenaDTO
            {
                ResenaId = r.ResenaId,
                EventoId = r.EventoId,
                EventoNombre = r.Evento?.Nombre,
                UsuarioNombre = r.Usuario?.Nombre,
                Calificacion = r.Calificacion,
                Comentario = r.Comentario,
                FechaCreacion = r.FechaCreacion
            };
        }
    }
}
