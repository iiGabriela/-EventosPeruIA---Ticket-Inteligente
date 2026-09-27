using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EventosIAPeru.Core.Core.Entities;
using EventosIAPeru.Core.Infrastructure.Data;

namespace EventosIAPeruAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsuarioRolesController : ControllerBase
    {
        private readonly EventosPeruIaContext _context;

        public UsuarioRolesController(EventosPeruIaContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetUsuarioRoles()
        {
            var items = await _context.UsuarioRoles.ToListAsync();
            return Ok(items);
        }

        [HttpGet("{usuarioId}/{rolId}")]
        public async Task<IActionResult> GetUsuarioRol(int usuarioId, int rolId)
        {
            var item = await _context.UsuarioRoles.FirstOrDefaultAsync(x => x.UsuarioId == usuarioId && x.RolId == rolId);
            if (item == null) return NotFound();
            return Ok(item);
        }

        [HttpPost]
        public async Task<IActionResult> CreateUsuarioRol([FromBody] UsuarioRoles entidad)
        {
            _context.UsuarioRoles.Add(entidad);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetUsuarioRol), new { usuarioId = entidad.UsuarioId, rolId = entidad.RolId }, entidad);
        }

        [HttpPut("{usuarioId}/{rolId}")]
        public async Task<IActionResult> UpdateUsuarioRol(int usuarioId, int rolId, [FromBody] UsuarioRoles entidad)
        {
            if (usuarioId != entidad.UsuarioId || rolId != entidad.RolId) return BadRequest();
            _context.Entry(entidad).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{usuarioId}/{rolId}")]
        public async Task<IActionResult> DeleteUsuarioRol(int usuarioId, int rolId)
        {
            var item = await _context.UsuarioRoles.FirstOrDefaultAsync(x => x.UsuarioId == usuarioId && x.RolId == rolId);
            if (item == null) return NotFound();
            _context.UsuarioRoles.Remove(item);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
