using EventosIAPeru.Core.Core.DTOs;
using EventosIAPeru.Core.Core.Entities;
using EventosIAPeru.Core.Core.Interfaces;
using EventosIAPeru.Core.Shared;
using System.Text.RegularExpressions;

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

        private readonly IModeradorResenasIA? _moderadorIA;

        public ResenaService(IResenaRepository resenaRepository, IUsuarioRepository usuarioRepository, IModeradorResenasIA? moderadorIA = null)
        {
            _resenaRepository = resenaRepository;
            _usuarioRepository = usuarioRepository;
            _moderadorIA = moderadorIA;
        }

        // Filtro rápido sin IA: enlaces, correos y teléfonos no tienen lugar en una reseña
        private static readonly Regex Enlace = new(@"(https?://|www\.|\b[\w-]+\.(com|net|org|pe|io|ly|me|xyz)\b)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex Correo = new(@"[\w.+-]+@[\w-]+\.[\w.]+", RegexOptions.Compiled);
        private static readonly Regex Telefono = new(@"(\+?51[\s-]?)?\b9\d{2}[\s-]?\d{3}[\s-]?\d{3}\b", RegexOptions.Compiled);

        /// <summary>US-13: motivo por el que un comentario no se publica sin consultar a la IA, o null si pasa.</summary>
        public static string? MotivoBloqueoLocal(string comentario)
        {
            if (Enlace.IsMatch(comentario)) return "no se permiten enlaces ni publicidad en las reseñas";
            if (Correo.IsMatch(comentario) || Telefono.IsMatch(comentario)) return "no compartas datos personales como correos o teléfonos";
            return null;
        }

        public async Task<ResultadoOperacion> CrearResena(string emailUsuario, CrearResenaDTO dto)
        {
            // Debe ser un usuario registrado y activo
            var usuario = await _usuarioRepository.GetUsuarioByEmail(emailUsuario);
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
                return ResultadoOperacion.NoAutorizado("Solo puedes reseñar eventos a los que asististe (entrada validada en el ingreso).");

            // Una sola reseña por evento
            var yaReseno = await _resenaRepository.ExisteResena(usuario.UsuarioId, evento.EventoId);
            if (yaReseno)
                return ResultadoOperacion.Invalido("Ya registraste una reseña para este evento.");

            // Si el comentario viene vacío, lo guardamos como null
            var comentario = dto.Comentario?.Trim();

            // Filtro de reseñas maliciosas: primero reglas rápidas y luego la IA.
            // Una crítica negativa honesta sí se publica; se bloquean insultos, odio, spam, etc.
            if (!string.IsNullOrWhiteSpace(comentario))
            {
                var motivoLocal = MotivoBloqueoLocal(comentario);
                if (motivoLocal != null)
                    return ResultadoOperacion.Invalido("Tu reseña no se publicó: " + motivoLocal + ". Puedes corregirla y enviarla otra vez.");

                if (_moderadorIA is { Disponible: true })
                {
                    try
                    {
                        var (bloquear, motivo) = await _moderadorIA.Evaluar(comentario, evento.Nombre);
                        if (bloquear)
                            return ResultadoOperacion.Invalido("Tu reseña no se publicó: " + motivo + " Puedes corregirla y enviarla otra vez.");
                    }
                    catch (Exception)
                    {
                        // Si la IA no responde, la reseña no se bloquea: ya pasó el filtro rápido
                    }
                }
            }

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

        public async Task<IEnumerable<ResenaDTO>?> GetMisResenas(string emailUsuario)
        {
            var usuario = await _usuarioRepository.GetUsuarioByEmail(emailUsuario);
            if (usuario == null) return null;

            var resenas = await _resenaRepository.GetResenasDeUsuario(usuario.UsuarioId);
            return resenas.Select(Mapear).ToList();
        }

        public async Task<PendientesResenaDTO?> GetPendientes(string emailUsuario)
        {
            var usuario = await _usuarioRepository.GetUsuarioByEmail(emailUsuario);
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
