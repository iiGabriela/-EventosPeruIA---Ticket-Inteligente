using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EventosIAPeru.Core.Core.Entities;
using EventosIAPeru.Core.Infrastructure.Data;

namespace EventosIAPeruAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HistorialVistasController : ControllerBase
    {
        private readonly EventosPeruIaContext _context;

        public HistorialVistasController(EventosPeruIaContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetHistorialVistas()
        {
            var items = await _context.HistorialVistas.ToListAsync();
            return Ok(items);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetHistorialVistaById(int id)
        {
            var item = await _context.HistorialVistas.FirstOrDefaultAsync(x => x.VistaId == id);
            if (item == null) return NotFound();
            return Ok(item);
        }

        [HttpPost]
        public async Task<IActionResult> CreateHistorialVista([FromBody] HistorialVistas entidad)
        {
            _context.HistorialVistas.Add(entidad);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetHistorialVistaById), new { id = entidad.VistaId }, entidad);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateHistorialVista(int id, [FromBody] HistorialVistas entidad)
        {
            // Ajusta la propiedad de clave si es diferente
            if (id != entidad.VistaId) return BadRequest();
            _context.Entry(entidad).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteHistorialVista(int id)
        {
            var item = await _context.HistorialVistas.FindAsync(id);
            if (item == null) return NotFound();
            _context.HistorialVistas.Remove(item);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
