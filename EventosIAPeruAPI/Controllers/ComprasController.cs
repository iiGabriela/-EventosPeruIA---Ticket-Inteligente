using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EventosIAPeru.Core.Core.Entities;
using EventosIAPeru.Core.Infrastructure.Data;

namespace EventosIAPeruAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ComprasController : ControllerBase
    {
        private readonly EventosPeruIaContext _context;

        public ComprasController(EventosPeruIaContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetCompras()
        {
            var items = await _context.Compras.ToListAsync();
            return Ok(items);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCompraById(int id)
        {
            var item = await _context.Compras.FirstOrDefaultAsync(x => x.CompraId == id);
            if (item == null) return NotFound();
            return Ok(item);
        }

        [HttpPost]
        public async Task<IActionResult> CreateCompra([FromBody] Compras compra)
        {
            _context.Compras.Add(compra);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetCompraById), new { id = compra.CompraId }, compra);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCompra(int id, [FromBody] Compras compra)
        {
            if (id != compra.CompraId) return BadRequest();
            _context.Entry(compra).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCompra(int id)
        {
            var item = await _context.Compras.FindAsync(id);
            if (item == null) return NotFound();
            _context.Compras.Remove(item);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
