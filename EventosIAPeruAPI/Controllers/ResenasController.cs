using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EventosIAPeru.Core.Core.Entities;
using EventosIAPeru.Core.Infrastructure.Data;

namespace EventosIAPeruAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ResenasController : ControllerBase
    {
        private readonly EventosPeruIaContext _context;

        public ResenasController(EventosPeruIaContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetResenas()
        {
            var items = await _context.Resenas.ToListAsync();
            return Ok(items);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetResenaById(int id)
        {
            var item = await _context.Resenas.FirstOrDefaultAsync(x => x.ResenaId == id);
            if (item == null) return NotFound();
            return Ok(item);
        }

        [HttpPost]
        public async Task<IActionResult> CreateResena([FromBody] Resenas entidad)
        {
            _context.Resenas.Add(entidad);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetResenaById), new { id = entidad.ResenaId }, entidad);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateResena(int id, [FromBody] Resenas entidad)
        {
            if (id != entidad.ResenaId) return BadRequest();
            _context.Entry(entidad).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteResena(int id)
        {
            var item = await _context.Resenas.FindAsync(id);
            if (item == null) return NotFound();
            _context.Resenas.Remove(item);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
