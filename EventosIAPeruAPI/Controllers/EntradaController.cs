using Microsoft.AspNetCore.Mvc;
using EventosIAPeru.Core.Core.DTOs;
using EventosIAPeru.Core.Core.Interfaces;

namespace EventosIAPeru.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EntradaController : BaseApiController
    {
        private readonly IEntradaService _entradaService;

        public EntradaController(IEntradaService entradaService, IWebHostEnvironment environment)
            : base(environment)
        {
            _entradaService = entradaService;
        }

        [HttpGet("mis-entradas")]
        public async Task<IActionResult> GetMisEntradas()
        {
            var emailUsuario = ObtenerEmailUsuario();
            if (emailUsuario == null)
                return Unauthorized(new { mensaje = "Debe iniciar sesión." });

            var entradas = await _entradaService.GetMisEntradas(emailUsuario);
            if (entradas == null)
                return NotFound(new { mensaje = "Usuario no encontrado." });

            return Ok(entradas);
        }

        /// <summary>US-07: QR dinámico de una entrada propia. El frontend lo renueva antes de que expire.</summary>
        [HttpGet("{id:int}/qr")]
        public async Task<IActionResult> GetQrDinamico(int id)
        {
            var emailUsuario = ObtenerEmailUsuario();
            if (emailUsuario == null)
                return Unauthorized(new { mensaje = "Debe iniciar sesión." });

            var (result, qr) = await _entradaService.GenerarQrDinamico(emailUsuario, id);
            if (!result.Exito)
                return Responder(result);

            // El token vence en segundos: que ningún proxy ni navegador lo guarde
            Response.Headers.CacheControl = "no-store";
            return Ok(qr);
        }

        [HttpPost("validar")]
        public async Task<IActionResult> ValidarEntrada([FromBody] ValidarEntradaDTO dto)
        {
            var emailUsuario = ObtenerEmailUsuario();
            if (emailUsuario == null)
                return Unauthorized(new { mensaje = "Debe iniciar sesión." });

            var (resultado, error) = await _entradaService.ValidarEntrada(emailUsuario, dto);
            if (error != null)
                return Responder(error);

            return Ok(resultado);
        }
    }
}

