using Microsoft.AspNetCore.Mvc;
using EventosIAPeru.Core.Core.DTOs;
using EventosIAPeru.Core.Core.Interfaces;
using EventosIAPeru.Core.Shared;

namespace EventosIAPeru.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EntradaController : ControllerBase
    {
        private readonly IEntradaService _entradaService;
        private readonly IWebHostEnvironment _environment;

        public EntradaController(IEntradaService entradaService, IWebHostEnvironment environment)
        {
            _entradaService = entradaService;
            _environment = environment;
        }

        [HttpGet("mis-entradas")]
        public async Task<IActionResult> GetMisEntradas()
        {
            var firebaseUid = ObtenerFirebaseUid();
            if (firebaseUid == null)
                return Unauthorized(new { mensaje = "Debe iniciar sesión." });

            var entradas = await _entradaService.GetMisEntradas(firebaseUid);
            if (entradas == null)
                return NotFound(new { mensaje = "Usuario no encontrado." });

            return Ok(entradas);
        }

        [HttpPost("validar")]
        public async Task<IActionResult> ValidarEntrada([FromBody] ValidarEntradaDTO dto)
        {
            var firebaseUid = ObtenerFirebaseUid();
            if (firebaseUid == null)
                return Unauthorized(new { mensaje = "Debe iniciar sesión." });

            var (resultado, error) = await _entradaService.ValidarEntrada(firebaseUid, dto);
            if (error != null)
                return Responder(error);

            return Ok(resultado);
        }

        private IActionResult Responder(ResultadoOperacion result)
        {
            switch (result.Tipo)
            {
                case TipoResultado.NoEncontrado: return NotFound(new { mensaje = result.Mensaje });
                case TipoResultado.NoAutorizado: return StatusCode(StatusCodes.Status403Forbidden, new { mensaje = result.Mensaje });
                case TipoResultado.Invalido: return BadRequest(new { mensaje = result.Mensaje });
                default: return NoContent();
            }
        }

        private string? ObtenerFirebaseUid()
        {
            var firebaseUid = User.ObtenerFirebaseUid();
            if (string.IsNullOrWhiteSpace(firebaseUid) && _environment.IsDevelopment())
                firebaseUid = Request.Headers["X-Firebase-Uid"].FirstOrDefault();
            return string.IsNullOrWhiteSpace(firebaseUid) ? null : firebaseUid;
        }
    }
}

