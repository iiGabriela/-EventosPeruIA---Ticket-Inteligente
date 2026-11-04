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

    /// <summary>
    /// US-09: bloquea la fila del evento (SELECT ... FOR UPDATE) y vuelve a contar los cupos
    /// dentro de la transacción. Si dos personas compran el último cupo a la vez, la segunda
    /// espera al bloqueo, ve el aforo lleno y no se confirma. Devuelve null si no hay cupos.
    /// </summary>
    public async Task<int?> CrearCompraConEntradas(Compra compra, int aforoTotal)
    {
        await using var transaccion = await _dbContext.Database.BeginTransactionAsync();

        await _dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM evento WHERE evento_id = {compra.EventoId} FOR UPDATE");

        var vendidas = await GetEntradasVendidas(compra.EventoId);
        if (vendidas + compra.Cantidad > aforoTotal)
        {
            await transaccion.RollbackAsync();
            return null;
        }

        await _dbContext.Compra.AddAsync(compra);
        await _dbContext.SaveChangesAsync();
        await transaccion.CommitAsync();

        return compra.CompraId;
    }
}
