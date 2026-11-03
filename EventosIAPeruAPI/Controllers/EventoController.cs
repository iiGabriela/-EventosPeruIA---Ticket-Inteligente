using Microsoft.AspNetCore.Mvc;
using EventosIAPeru.Core.Core.DTOs;
using EventosIAPeru.Core.Core.Interfaces;

namespace EventosIAPeru.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EventoController : BaseApiController
    {
        private readonly IEventoService _eventoService;
        private readonly IRecomendacionService _recomendacionService;

        public EventoController(IEventoService eventoService,
                                IRecomendacionService recomendacionService,
                                IWebHostEnvironment environment)
            : base(environment)
        {
            _eventoService = eventoService;
            _recomendacionService = recomendacionService;
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

        /// <summary>US-10: recomendaciones personalizadas para el asistente autenticado.</summary>
        [HttpGet("recomendados")]
        public async Task<IActionResult> GetRecomendados([FromQuery] int cantidad = 6)
        {
            var emailUsuario = ObtenerEmailUsuario();
            if (emailUsuario == null)
                return Unauthorized(new { mensaje = "Debe iniciar sesión para recibir recomendaciones personalizadas." });

            var (result, recomendaciones) = await _recomendacionService.ObtenerRecomendaciones(emailUsuario, cantidad);
            if (!result.Exito)
                return Responder(result);

            return Ok(recomendaciones);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetEventoById(int id)
        {
            var evento = await _eventoService.GetEventoById(id, ObtenerEmailUsuario());
            if (evento == null) return NotFound();
            return Ok(evento);
        }

        // ---------------------------- US-04 ----------------------------

        /// <summary>Reglas de validación para mostrarlas en el formulario antes de guardar.</summary>
        [HttpGet("reglas")]
        public IActionResult GetReglasValidacion()
        {
            return Ok(new
            {
                camposObligatorios = new[] { "nombre", "descripcion", "categoria", "fechaInicio", "fechaFin", "sede", "direccion", "departamento", "provincia", "distrito", "aforoTotal", "precio" },
                reglas = new[]
                {
                    "El aforo debe ser mayor a cero.",
                    "El precio no puede ser negativo.",
                    "La fecha del evento debe ser posterior a hoy.",
                    "La fecha de fin debe ser posterior a la de inicio.",
                    "Al editar, el aforo no puede ser menor a las entradas ya vendidas.",
                    "Solo puedes editar tus propios eventos."
                }
            });
        }

        /// <summary>Eventos creados por el organizador autenticado.</summary>
        [HttpGet("mis-eventos")]
        public async Task<IActionResult> GetMisEventos()
        {
            var emailUsuario = ObtenerEmailUsuario();
            if (emailUsuario == null) return Unauthorized(new { mensaje = "Debe iniciar sesión." });

            var eventos = await _eventoService.GetMisEventos(emailUsuario);
            if (eventos == null) return NotFound(new { mensaje = "Tu usuario no está registrado." });
            return Ok(eventos);
        }

        /// <summary>Crea el evento en estado BORRADOR.</summary>
        [HttpPost]
        public async Task<IActionResult> CrearEvento([FromBody] CrearEventoDTO eventoDTO)
        {
            var emailUsuario = ObtenerEmailUsuario();
            if (emailUsuario == null) return Unauthorized(new { mensaje = "Debe iniciar sesión." });

            var result = await _eventoService.CrearEvento(emailUsuario, eventoDTO);
            if (!result.Exito) return Responder(result);

            var evento = await _eventoService.GetEventoById(result.Id!.Value, emailUsuario);
            return CreatedAtAction(nameof(GetEventoById), new { id = result.Id }, evento);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> ActualizarEvento(int id, [FromBody] ActualizarEventoDTO eventoDTO)
        {
            if (id != eventoDTO.EventoId)
                return BadRequest(new { mensaje = "El id de la ruta no coincide con el del evento." });

            var emailUsuario = ObtenerEmailUsuario();
            if (emailUsuario == null) return Unauthorized(new { mensaje = "Debe iniciar sesión." });

            var result = await _eventoService.ActualizarEvento(emailUsuario, eventoDTO);
            return Responder(result);
        }

        [HttpPut("{id:int}/publicar")]
        public async Task<IActionResult> PublicarEvento(int id)
        {
            var emailUsuario = ObtenerEmailUsuario();
            if (emailUsuario == null) return Unauthorized(new { mensaje = "Debe iniciar sesión." });

            var result = await _eventoService.PublicarEvento(emailUsuario, id);
            return Responder(result);
        }

        [HttpPut("{id:int}/cancelar")]
        public async Task<IActionResult> CancelarEvento(int id)
        {
            var emailUsuario = ObtenerEmailUsuario();
            if (emailUsuario == null) return Unauthorized(new { mensaje = "Debe iniciar sesión." });

            var result = await _eventoService.CancelarEvento(emailUsuario, id);
            return Responder(result);
        }
    }
}
