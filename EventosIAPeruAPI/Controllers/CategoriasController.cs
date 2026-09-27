using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EventosIAPeru.Core.Core.Entities;
using EventosIAPeru.Core.Infrastructure.Data;

namespace EventosIAPeruAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriasController : ControllerBase
    {
        private readonly EventosPeruIaContext _context;

        public CategoriasController(EventosPeruIaContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetCategorias()
        {
            var items = await _context.Categorias.ToListAsync();
            return Ok(items);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCategoriaById(int id)
        {
            var item = await _context.Categorias.FirstOrDefaultAsync(x => x.CategoriaId == id);
            if (item == null) return NotFound();
            return Ok(item);
        }

        [HttpPost]
        public async Task<IActionResult> CreateCategoria([FromBody] Categorias categoria)
        {
            _context.Categorias.Add(categoria);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetCategoriaById), new { id = categoria.CategoriaId }, categoria);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCategoria(int id, [FromBody] Categorias categoria)
        {
            if (id != categoria.CategoriaId) return BadRequest();
            _context.Entry(categoria).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategoria(int id)
        {
            var item = await _context.Categorias.FindAsync(id);
            if (item == null) return NotFound();
            _context.Categorias.Remove(item);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
