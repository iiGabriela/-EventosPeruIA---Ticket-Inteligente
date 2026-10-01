using Microsoft.AspNetCore.Mvc;
using EventosIAPeru.Core.Core.DTOs;
using EventosIAPeru.Core.Core.Interfaces;

namespace EventosIAPeru.API.Controllers
{
    /// <summary>Controller: puerta de entrada para calificar y reseñar eventos (US-13).</summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ResenaController : BaseApiController
    {
        private readonly IResenaService _resenaService;

        public ResenaController(IResenaService resenaService, IWebHostEnvironment environment)
            : base(environment)
        {
            _resenaService = resenaService;
        }

        /// <summary>Crea la reseña (1 a 5) de un evento al que asistió el usuario.</summary>
        [HttpPost]
        public async Task<IActionResult> CrearResena([FromBody] CrearResenaDTO dto)
        {
            var firebaseUid = ObtenerFirebaseUid();
            if (firebaseUid == null) return Unauthorized(new { mensaje = "Debe iniciar sesión." });

            var result = await _resenaService.CrearResena(firebaseUid, dto);
            if (!result.Exito) return Responder(result);

            return CreatedAtAction(nameof(GetResenasDeEvento),
                                   new { eventoId = dto.EventoId },
                                   new { mensaje = "Reseña registrada correctamente.", resenaId = result.Id });
        }

        /// <summary>Promedio de calificaciones y reseñas publicadas de un evento (público).</summary>
        [HttpGet("evento/{eventoId:int}")]
        public async Task<IActionResult> GetResenasDeEvento(int eventoId)
        {
            var resumen = await _resenaService.GetResenasDeEvento(eventoId);
            if (resumen == null) return NotFound(new { mensaje = "El evento no existe." });
            return Ok(resumen);
        }

        /// <summary>Historial: las reseñas que escribió el usuario logueado.</summary>
        [HttpGet("mias")]
        public async Task<IActionResult> GetMisResenas()
        {
            var firebaseUid = ObtenerFirebaseUid();
            if (firebaseUid == null) return Unauthorized(new { mensaje = "Debe iniciar sesión." });

            var resenas = await _resenaService.GetMisResenas(firebaseUid);
            if (resenas == null) return NotFound(new { mensaje = "Tu usuario no está registrado." });
            return Ok(resenas);
        }

        /// <summary>Eventos terminados que el usuario aún no reseña (con contador para la pestaña).</summary>
        [HttpGet("pendientes")]
        public async Task<IActionResult> GetPendientes()
        {
            var firebaseUid = ObtenerFirebaseUid();
            if (firebaseUid == null) return Unauthorized(new { mensaje = "Debe iniciar sesión." });

            var pendientes = await _resenaService.GetPendientes(firebaseUid);
            if (pendientes == null) return NotFound(new { mensaje = "Tu usuario no está registrado." });
            return Ok(pendientes);
        }
    }
}
