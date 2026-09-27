using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EventosIAPeru.Core.Core.Entities;
using EventosIAPeru.Core.Infrastructure.Data;

namespace EventosIAPeruAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EventosController : ControllerBase
    {
        private readonly EventosPeruIaContext _context;

        public EventosController(EventosPeruIaContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetEventos()
        {
            var items = await _context.Eventos.ToListAsync();
            return Ok(items);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetEventoById(int id)
        {
            var item = await _context.Eventos.FirstOrDefaultAsync(x => x.EventoId == id);
            if (item == null) return NotFound();
            return Ok(item);
        }

        [HttpPost]
        public async Task<IActionResult> CreateEvento([FromBody] Eventos evento)
        {
            _context.Eventos.Add(evento);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetEventoById), new { id = evento.EventoId }, evento);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEvento(int id, [FromBody] Eventos evento)
        {
            if (id != evento.EventoId) return BadRequest();
            _context.Entry(evento).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEvento(int id)
        {
            var item = await _context.Eventos.FindAsync(id);
            if (item == null) return NotFound();
            _context.Eventos.Remove(item);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
