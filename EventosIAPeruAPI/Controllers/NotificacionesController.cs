using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EventosIAPeru.Core.Core.Entities;
using EventosIAPeru.Core.Infrastructure.Data;

namespace EventosIAPeruAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificacionesController : ControllerBase
    {
        private readonly EventosPeruIaContext _context;

        public NotificacionesController(EventosPeruIaContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetNotificaciones()
        {
            var items = await _context.Notificaciones.ToListAsync();
            return Ok(items);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetNotificacionById(int id)
        {
            var item = await _context.Notificaciones.FirstOrDefaultAsync(x => x.NotificacionId == id);
            if (item == null) return NotFound();
            return Ok(item);
        }

        [HttpPost]
        public async Task<IActionResult> CreateNotificacion([FromBody] Notificaciones entidad)
        {
            _context.Notificaciones.Add(entidad);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetNotificacionById), new { id = entidad.NotificacionId }, entidad);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateNotificacion(int id, [FromBody] Notificaciones entidad)
        {
            if (id != entidad.NotificacionId) return BadRequest();
            _context.Entry(entidad).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNotificacion(int id)
        {
            var item = await _context.Notificaciones.FindAsync(id);
            if (item == null) return NotFound();
            _context.Notificaciones.Remove(item);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
