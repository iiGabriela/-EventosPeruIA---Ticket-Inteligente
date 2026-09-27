using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EventosIAPeru.Core.Core.Entities;
using EventosIAPeru.Core.Infrastructure.Data;

namespace EventosIAPeruAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EntradasController : ControllerBase
    {
        private readonly EventosPeruIaContext _context;

        public EntradasController(EventosPeruIaContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetEntradas()
        {
            var items = await _context.Entradas.ToListAsync();
            return Ok(items);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetEntradaById(int id)
        {
            var item = await _context.Entradas.FirstOrDefaultAsync(x => x.EntradaId == id);
            if (item == null) return NotFound();
            return Ok(item);
        }

        [HttpPost]
        public async Task<IActionResult> CreateEntrada([FromBody] Entradas entrada)
        {
            _context.Entradas.Add(entrada);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetEntradaById), new { id = entrada.EntradaId }, entrada);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEntrada(int id, [FromBody] Entradas entrada)
        {
            if (id != entrada.EntradaId) return BadRequest();
            _context.Entry(entrada).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEntrada(int id)
        {
            var item = await _context.Entradas.FindAsync(id);
            if (item == null) return NotFound();
            _context.Entradas.Remove(item);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
