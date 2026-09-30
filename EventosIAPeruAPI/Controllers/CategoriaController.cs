using Microsoft.AspNetCore.Mvc;
using EventosIAPeru.Core.Core.Interfaces;

namespace EventosIAPeru.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriaController : ControllerBase
    {
        private readonly ICategoriaService _categoriaService;

        public CategoriaController(ICategoriaService categoriaService)
        {
            _categoriaService = categoriaService;
        }

        [HttpGet]
        public async Task<IActionResult> GetCategorias()
        {
            var categorias = await _categoriaService.GetCategorias();
            return Ok(categorias);
        }

        /// <summary>Categorías más consultadas, para los chips de acceso rápido (US-05).</summary>
        [HttpGet("populares")]
        public async Task<IActionResult> GetCategoriasPopulares([FromQuery] int cantidad = 4)
        {
            if (cantidad < 1 || cantidad > 20)
                return BadRequest(new { mensaje = "La cantidad debe estar entre 1 y 20." });

            var categorias = await _categoriaService.GetCategoriasPopulares(cantidad);
            return Ok(categorias);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetCategoriaById(int id)
        {
            var categoria = await _categoriaService.GetCategoriaById(id);
            if (categoria == null) return NotFound();
            return Ok(categoria);
        }
    }
}
