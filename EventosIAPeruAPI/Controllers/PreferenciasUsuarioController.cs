using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EventosIAPeru.Core.Core.Entities;
using EventosIAPeru.Core.Infrastructure.Data;

namespace EventosIAPeruAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PreferenciasUsuarioController : ControllerBase
    {
        private readonly EventosPeruIaContext _context;

        public PreferenciasUsuarioController(EventosPeruIaContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetPreferencias()
        {
            var items = await _context.PreferenciasUsuario.ToListAsync();
            return Ok(items);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPreferenciaById(int id)
        {
            var item = await _context.PreferenciasUsuario.FirstOrDefaultAsync(x => x.PreferenciaId == id);
            if (item == null) return NotFound();
            return Ok(item);
        }

        [HttpPost]
        public async Task<IActionResult> CreatePreferencia([FromBody] PreferenciasUsuario entidad)
        {
            _context.PreferenciasUsuario.Add(entidad);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetPreferenciaById), new { id = entidad.PreferenciaId }, entidad);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePreferencia(int id, [FromBody] PreferenciasUsuario entidad)
        {
            if (id != entidad.PreferenciaId) return BadRequest();
            _context.Entry(entidad).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePreferencia(int id)
        {
            var item = await _context.PreferenciasUsuario.FindAsync(id);
            if (item == null) return NotFound();
            _context.PreferenciasUsuario.Remove(item);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
