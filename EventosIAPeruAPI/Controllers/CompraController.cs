using Microsoft.AspNetCore.Mvc;
using EventosIAPeru.Core.Core.DTOs;
using EventosIAPeru.Core.Core.Interfaces;

namespace EventosIAPeru.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CompraController : BaseApiController
    {
        private readonly ICompraService _compraService;

        public CompraController(ICompraService compraService, IWebHostEnvironment environment)
            : base(environment)
        {
            _compraService = compraService;
        }

        [HttpPost]
        public async Task<IActionResult> CrearCompra([FromBody] CrearCompraDTO dto)
        {
            var emailUsuario = ObtenerEmailUsuario();
            if (emailUsuario == null)
                return Unauthorized(new { mensaje = "Debe iniciar sesión." });

            var result = await _compraService.CrearCompra(emailUsuario, dto);
            if (!result.Exito)
                return Responder(result);

            return CreatedAtAction(nameof(GetCompraById), new { id = result.Id }, new { compraId = result.Id });
        }

        [HttpGet("mis-compras")]
        public async Task<IActionResult> GetMisCompras()
        {
            var emailUsuario = ObtenerEmailUsuario();
            if (emailUsuario == null)
                return Unauthorized(new { mensaje = "Debe iniciar sesión." });

            var (result, compras) = await _compraService.GetMisCompras(emailUsuario);
            if (!result.Exito)
                return Responder(result);

            return Ok(compras);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetCompraById(int id)
        {
            var emailUsuario = ObtenerEmailUsuario();
            if (emailUsuario == null)
                return Unauthorized(new { mensaje = "Debe iniciar sesión." });

            var (result, compra) = await _compraService.GetCompraPorId(emailUsuario, id);
            if (!result.Exito)
                return Responder(result);

            return Ok(compra);
        }
    }
}
