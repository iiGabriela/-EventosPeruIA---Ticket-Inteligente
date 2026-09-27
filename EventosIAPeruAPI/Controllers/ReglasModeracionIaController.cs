using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EventosIAPeru.Core.Core.Entities;
using EventosIAPeru.Core.Infrastructure.Data;

namespace EventosIAPeruAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReglasModeracionIaController : ControllerBase
    {
        private readonly EventosPeruIaContext _context;

        public ReglasModeracionIaController(EventosPeruIaContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetReglas()
        {
            var items = await _context.ReglasModeracionIa.ToListAsync();
            return Ok(items);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetReglaById(int id)
        {
            var item = await _context.ReglasModeracionIa.FirstOrDefaultAsync(x => x.ReglaId == id);
            if (item == null) return NotFound();
            return Ok(item);
        }

        [HttpPost]
        public async Task<IActionResult> CreateRegla([FromBody] ReglasModeracionIa entidad)
        {
            _context.ReglasModeracionIa.Add(entidad);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetReglaById), new { id = entidad.ReglaId }, entidad);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRegla(int id, [FromBody] ReglasModeracionIa entidad)
        {
            if (id != entidad.ReglaId) return BadRequest();
            _context.Entry(entidad).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRegla(int id)
        {
            var item = await _context.ReglasModeracionIa.FindAsync(id);
            if (item == null) return NotFound();
            _context.ReglasModeracionIa.Remove(item);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
