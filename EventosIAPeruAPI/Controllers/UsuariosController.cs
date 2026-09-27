using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EventosIAPeru.Core.Core.Entities;
using EventosIAPeru.Core.Infrastructure.Data;

namespace EventosIAPeruAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsuariosController : ControllerBase
    {
        private readonly EventosPeruIaContext _context;

        public UsuariosController(EventosPeruIaContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetUsuarios()
        {
            var items = await _context.Usuarios.ToListAsync();
            return Ok(items);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUsuarioById(int id)
        {
            var item = await _context.Usuarios.FirstOrDefaultAsync(x => x.UsuarioId == id);
            if (item == null) return NotFound();
            return Ok(item);
        }

        [HttpPost]
        public async Task<IActionResult> CreateUsuario([FromBody] Usuarios entidad)
        {
            _context.Usuarios.Add(entidad);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetUsuarioById), new { id = entidad.UsuarioId }, entidad);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUsuario(int id, [FromBody] Usuarios entidad)
        {
            if (id != entidad.UsuarioId) return BadRequest();
            _context.Entry(entidad).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUsuario(int id)
        {
            var item = await _context.Usuarios.FindAsync(id);
            if (item == null) return NotFound();
            _context.Usuarios.Remove(item);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
