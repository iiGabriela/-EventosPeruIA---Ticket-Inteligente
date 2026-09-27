using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EventosIAPeru.Core.Core.Entities;
using EventosIAPeru.Core.Infrastructure.Data;

namespace EventosIAPeruAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RolesController : ControllerBase
    {
        private readonly EventosPeruIaContext _context;

        public RolesController(EventosPeruIaContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetRoles()
        {
            var items = await _context.Roles.ToListAsync();
            return Ok(items);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetRolById(int id)
        {
            var item = await _context.Roles.FirstOrDefaultAsync(x => x.RolId == id);
            if (item == null) return NotFound();
            return Ok(item);
        }

        [HttpPost]
        public async Task<IActionResult> CreateRol([FromBody] Roles entidad)
        {
            _context.Roles.Add(entidad);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetRolById), new { id = entidad.RolId }, entidad);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRol(int id, [FromBody] Roles entidad)
        {
            if (id != entidad.RolId) return BadRequest();
            _context.Entry(entidad).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRol(int id)
        {
            var item = await _context.Roles.FindAsync(id);
            if (item == null) return NotFound();
            _context.Roles.Remove(item);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
