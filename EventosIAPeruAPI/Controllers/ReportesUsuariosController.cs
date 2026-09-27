using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EventosIAPeru.Core.Core.Entities;
using EventosIAPeru.Core.Infrastructure.Data;

namespace EventosIAPeruAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReportesUsuariosController : ControllerBase
    {
        private readonly EventosPeruIaContext _context;

        public ReportesUsuariosController(EventosPeruIaContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetReportes()
        {
            var items = await _context.ReportesUsuarios.ToListAsync();
            return Ok(items);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetReporteById(int id)
        {
            var item = await _context.ReportesUsuarios.FirstOrDefaultAsync(x => x.ReporteId == id);
            if (item == null) return NotFound();
            return Ok(item);
        }

        [HttpPost]
        public async Task<IActionResult> CreateReporte([FromBody] ReportesUsuarios entidad)
        {
            _context.ReportesUsuarios.Add(entidad);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetReporteById), new { id = entidad.ReporteId }, entidad);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateReporte(int id, [FromBody] ReportesUsuarios entidad)
        {
            if (id != entidad.ReporteId) return BadRequest();
            _context.Entry(entidad).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteReporte(int id)
        {
            var item = await _context.ReportesUsuarios.FindAsync(id);
            if (item == null) return NotFound();
            _context.ReportesUsuarios.Remove(item);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
