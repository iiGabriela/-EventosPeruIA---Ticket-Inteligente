using Microsoft.EntityFrameworkCore;
using EventosIAPeru.Core.Core.Entities;
using EventosIAPeru.Core.Core.Interfaces;
using EventosIAPeru.Core.Infrastructure.Data;

namespace EventosIAPeru.Core.Infrastructure.Repositories;

public class CompraRepository : ICompraRepository
{
    private readonly EventosPeruIAContext _dbContext;

    public CompraRepository(EventosPeruIAContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Evento?> GetEventoParaCompra(int eventoId)
    {
        return await _dbContext.Evento
                               .AsNoTracking()
                               .FirstOrDefaultAsync(e => e.EventoId == eventoId);
    }

    public async Task<int> GetEntradasVendidas(int eventoId)
    {
        return await _dbContext.Compra
                               .AsNoTracking()
                               .Where(c => c.EventoId == eventoId && c.Estado == "CONFIRMADA")
                               .SumAsync(c => (int?)c.Cantidad) ?? 0;
    }

    public async Task<Compra?> GetCompraPorCodigoOperacion(int usuarioId, Guid codigoOperacion)
    {
        return await _dbContext.Compra
                               .AsNoTracking()
                               .Include(c => c.Entrada)
                               .Include(c => c.Evento)
                               .FirstOrDefaultAsync(c => c.UsuarioId == usuarioId && c.CodigoOperacion == codigoOperacion);
    }

    public async Task<Compra?> GetCompraPorId(int compraId)
    {
        return await _dbContext.Compra
                               .AsNoTracking()
                               .Include(c => c.Entrada)
                               .Include(c => c.Evento)
                               .FirstOrDefaultAsync(c => c.CompraId == compraId);
    }

    public async Task<List<Compra>> GetComprasDeUsuario(int usuarioId)
    {
        return await _dbContext.Compra
                               .AsNoTracking()
                               .Include(c => c.Entrada)
                               .Include(c => c.Evento)
                               .Where(c => c.UsuarioId == usuarioId)
                               .OrderByDescending(c => c.FechaCompra)
                               .ToListAsync();
    }

    public async Task<int> CrearCompraConEntradas(Compra compra)
    {
        using var transaccion = await _dbContext.Database.BeginTransactionAsync();

        await _dbContext.Compra.AddAsync(compra);
        await _dbContext.SaveChangesAsync();
        await transaccion.CommitAsync();

        return compra.CompraId;
    }
}
