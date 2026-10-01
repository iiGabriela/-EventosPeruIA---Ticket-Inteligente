using Microsoft.AspNetCore.Mvc;
using EventosIAPeru.Core.Core.DTOs;
using EventosIAPeru.Core.Core.Interfaces;
using EventosIAPeru.Core.Shared;

namespace EventosIAPeru.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CompraController : ControllerBase
    {
        private readonly ICompraService _compraService;
        private readonly IWebHostEnvironment _environment;

        public CompraController(ICompraService compraService, IWebHostEnvironment environment)
        {
            _compraService = compraService;
            _environment = environment;
        }

        [HttpPost]
        public async Task<IActionResult> CrearCompra([FromBody] CrearCompraDTO dto)
        {
            var firebaseUid = ObtenerFirebaseUid();
            if (firebaseUid == null)
                return Unauthorized(new { mensaje = "Debe iniciar sesión." });

            var result = await _compraService.CrearCompra(firebaseUid, dto);
            if (!result.Exito)
                return Responder(result);

            return CreatedAtAction(nameof(GetCompraById), new { id = result.Id }, new { compraId = result.Id });
        }

        [HttpGet("mis-compras")]
        public async Task<IActionResult> GetMisCompras()
        {
            var firebaseUid = ObtenerFirebaseUid();
            if (firebaseUid == null)
                return Unauthorized(new { mensaje = "Debe iniciar sesión." });

            var result = await _compraService.GetMisCompras(firebaseUid);
            if (!result.Exito)
                return Responder(result);

            return Ok(new { mensaje = "Compras obtenidas." });
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetCompraById(int id)
        {
            var firebaseUid = ObtenerFirebaseUid();
            if (firebaseUid == null)
                return Unauthorized(new { mensaje = "Debe iniciar sesión." });

            var result = await _compraService.GetCompraPorId(firebaseUid, id);
            if (!result.Exito)
                return Responder(result);

            return Ok(new { compraId = result.Id });
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
