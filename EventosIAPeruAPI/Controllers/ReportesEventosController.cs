using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EventosIAPeru.Core.Core.Entities;
using EventosIAPeru.Core.Infrastructure.Data;

namespace EventosIAPeruAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReportesEventosController : ControllerBase
    {
        private readonly EventosPeruIaContext _context;

        public ReportesEventosController(EventosPeruIaContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetReportes()
        {
            var items = await _context.ReportesEventos.ToListAsync();
            return Ok(items);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetReporteById(int id)
        {
            var item = await _context.ReportesEventos.FirstOrDefaultAsync(x => x.ReporteId == id);
            if (item == null) return NotFound();
            return Ok(item);
        }

        [HttpPost]
        public async Task<IActionResult> CreateReporte([FromBody] ReportesEventos entidad)
        {
            _context.ReportesEventos.Add(entidad);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetReporteById), new { id = entidad.ReporteId }, entidad);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateReporte(int id, [FromBody] ReportesEventos entidad)
        {
            if (id != entidad.ReporteId) return BadRequest();
            _context.Entry(entidad).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteReporte(int id)
        {
            var item = await _context.ReportesEventos.FindAsync(id);
            if (item == null) return NotFound();
            _context.ReportesEventos.Remove(item);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
