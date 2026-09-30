using Microsoft.AspNetCore.Mvc;
using EventosIAPeru.Core.Core.DTOs;
using EventosIAPeru.Core.Core.Interfaces;
using EventosIAPeru.Core.Shared;

namespace EventosIAPeru.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EventoController : ControllerBase
    {
        private readonly IEventoService _eventoService;
        private readonly IWebHostEnvironment _environment;

        public EventoController(IEventoService eventoService, IWebHostEnvironment environment)
        {
            _eventoService = eventoService;
            _environment = environment;
        }

        // ---------------------------- US-05 ----------------------------

        /// <summary>Búsqueda con filtros combinables: texto, categoría, ubicación y fechas.</summary>
        [HttpGet]
        public async Task<IActionResult> BuscarEventos([FromQuery] FiltroEventoDTO filtro)
        {
            if (filtro.FechaDesde.HasValue && filtro.FechaHasta.HasValue && filtro.FechaDesde > filtro.FechaHasta)
                return BadRequest(new { mensaje = "La fecha 'desde' no puede ser mayor que la fecha 'hasta'." });

            var (eventos, total) = await _eventoService.BuscarEventos(filtro);
            var mensaje = total == 0 ? "No se encontraron eventos con los filtros seleccionados." : null;

            return Ok(new { total, mensaje, eventos });
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetEventoById(int id)
        {
            var evento = await _eventoService.GetEventoById(id, ObtenerFirebaseUid());
            if (evento == null) return NotFound();
            return Ok(evento);
        }

        // ---------------------------- US-04 ----------------------------

        /// <summary>Eventos creados por el organizador autenticado.</summary>
        [HttpGet("mis-eventos")]
        public async Task<IActionResult> GetMisEventos()
        {
            var firebaseUid = ObtenerFirebaseUid();
            if (firebaseUid == null) return Unauthorized(new { mensaje = "Debe iniciar sesión." });

            var eventos = await _eventoService.GetMisEventos(firebaseUid);
            if (eventos == null) return NotFound(new { mensaje = "Tu usuario no está registrado." });
            return Ok(eventos);
        }

        /// <summary>Crea el evento en estado BORRADOR.</summary>
        [HttpPost]
        public async Task<IActionResult> CrearEvento([FromBody] CrearEventoDTO eventoDTO)
        {
            var firebaseUid = ObtenerFirebaseUid();
            if (firebaseUid == null) return Unauthorized(new { mensaje = "Debe iniciar sesión." });

            var result = await _eventoService.CrearEvento(firebaseUid, eventoDTO);
            if (!result.Exito) return Responder(result);

            var evento = await _eventoService.GetEventoById(result.Id!.Value, firebaseUid);
            return CreatedAtAction(nameof(GetEventoById), new { id = result.Id }, evento);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> ActualizarEvento(int id, [FromBody] ActualizarEventoDTO eventoDTO)
        {
            if (id != eventoDTO.EventoId)
                return BadRequest(new { mensaje = "El id de la ruta no coincide con el del evento." });

            var firebaseUid = ObtenerFirebaseUid();
            if (firebaseUid == null) return Unauthorized(new { mensaje = "Debe iniciar sesión." });

            var result = await _eventoService.ActualizarEvento(firebaseUid, eventoDTO);
            return Responder(result);
        }

        [HttpPut("{id:int}/publicar")]
        public async Task<IActionResult> PublicarEvento(int id)
        {
            var firebaseUid = ObtenerFirebaseUid();
            if (firebaseUid == null) return Unauthorized(new { mensaje = "Debe iniciar sesión." });

            var result = await _eventoService.PublicarEvento(firebaseUid, id);
            return Responder(result);
        }

        [HttpPut("{id:int}/cancelar")]
        public async Task<IActionResult> CancelarEvento(int id)
        {
            var firebaseUid = ObtenerFirebaseUid();
            if (firebaseUid == null) return Unauthorized(new { mensaje = "Debe iniciar sesión." });

            var result = await _eventoService.CancelarEvento(firebaseUid, id);
            return Responder(result);
        }

        // ---------------------------- Ayudantes ----------------------------

        private IActionResult Responder(ResultadoOperacion result)
        {
            switch (result.Tipo)
            {
                case TipoResultado.NoEncontrado:
                    return NotFound(new { mensaje = result.Mensaje });
                case TipoResultado.NoAutorizado:
                    return StatusCode(StatusCodes.Status403Forbidden, new { mensaje = result.Mensaje });
                case TipoResultado.Invalido:
                    return BadRequest(new { mensaje = result.Mensaje });
                default:
                    return NoContent();
            }
        }

        /// <summary>
        /// UID del usuario autenticado (token de Firebase).
        /// Solo en Development, mientras se configura Firebase (US-02), se acepta el header
        /// "X-Firebase-Uid" para poder probar la API.
        /// </summary>
        private string? ObtenerFirebaseUid()
        {
            var firebaseUid = User.ObtenerFirebaseUid();
            if (string.IsNullOrWhiteSpace(firebaseUid) && _environment.IsDevelopment())
            {
                firebaseUid = Request.Headers["X-Firebase-Uid"].FirstOrDefault();
            }
            return string.IsNullOrWhiteSpace(firebaseUid) ? null : firebaseUid;
        }
    }
}
