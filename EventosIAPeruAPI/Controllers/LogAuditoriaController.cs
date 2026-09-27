using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EventosIAPeru.Core.Core.Entities;
using EventosIAPeru.Core.Infrastructure.Data;

namespace EventosIAPeruAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LogAuditoriaController : ControllerBase
    {
        private readonly EventosPeruIaContext _context;

        public LogAuditoriaController(EventosPeruIaContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetLogs()
        {
            var items = await _context.LogAuditoria.ToListAsync();
            return Ok(items);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetLogById(int id)
        {
            var item = await _context.LogAuditoria.FirstOrDefaultAsync(x => x.LogId == id);
            if (item == null) return NotFound();
            return Ok(item);
        }

        [HttpPost]
        public async Task<IActionResult> CreateLog([FromBody] LogAuditoria log)
        {
            _context.LogAuditoria.Add(log);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetLogById), new { id = log.LogId }, log);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateLog(int id, [FromBody] LogAuditoria log)
        {
            if (id != log.LogId) return BadRequest();
            _context.Entry(log).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteLog(int id)
        {
            var item = await _context.LogAuditoria.FindAsync(id);
            if (item == null) return NotFound();
            _context.LogAuditoria.Remove(item);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
