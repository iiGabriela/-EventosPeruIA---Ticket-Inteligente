using Microsoft.EntityFrameworkCore;
using EventosIAPeru.Core.Core.Entities;
using EventosIAPeru.Core.Core.Interfaces;
using EventosIAPeru.Core.Infrastructure.Data;

namespace EventosIAPeru.Core.Infrastructure.Repositories;

public class EntradaRepository : IEntradaRepository
{
    private readonly EventosPeruIAContext _dbContext;

    public EntradaRepository(EventosPeruIAContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Entrada?> GetEntradaPorCodigoQr(string codigoQr)
    {
        return await _dbContext.Entrada
                               .Include(e => e.Compra)
                               .Include(e => e.CheckIn)
                               .FirstOrDefaultAsync(e => e.CodigoQr == codigoQr);
    }

    public async Task<Entrada?> GetEntradaPorId(int entradaId)
    {
        return await _dbContext.Entrada
                               .AsNoTracking()
                               .Include(e => e.Compra)
                               .Include(e => e.CheckIn)
                               .FirstOrDefaultAsync(e => e.EntradaId == entradaId);
    }

    public async Task<List<Entrada>> GetEntradasDeUsuario(int usuarioId)
    {
        return await _dbContext.Entrada
                               .AsNoTracking()
                               .Include(e => e.Compra)
                                   .ThenInclude(c => c.Evento)
                               .Where(e => e.Compra.UsuarioId == usuarioId || e.AsistenteUsuarioId == usuarioId)
                               .OrderByDescending(e => e.FechaGeneracion)
                               .ToListAsync();
    }

    public async Task<bool> ExisteCodigoQr(string codigoQr)
    {
        return await _dbContext.Entrada
                               .AsNoTracking()
                               .AnyAsync(e => e.CodigoQr == codigoQr);
    }

    public async Task<bool> EsOrganizadorDelEvento(int eventoId, int usuarioId)
    {
        return await _dbContext.Evento
                               .AsNoTracking()
                               .AnyAsync(e => e.EventoId == eventoId && e.OrganizadorId == usuarioId);
    }

    /// <summary>
    /// Registra el check-in y marca la entrada como UTILIZADA en una sola transacción.
    /// Si dos escaneos llegan a la vez, el índice único de check_in.entrada_id
    /// rechaza el segundo y se devuelve false.
    /// </summary>
    public async Task<bool> RegistrarCheckIn(CheckIn checkIn)
    {
        await using var transaccion = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            await _dbContext.CheckIn.AddAsync(checkIn);
            await _dbContext.SaveChangesAsync();

            await _dbContext.Entrada
                            .Where(e => e.EntradaId == checkIn.EntradaId)
                            .ExecuteUpdateAsync(s => s.SetProperty(e => e.Estado, "UTILIZADA"));

            await transaccion.CommitAsync();
            return true;
        }
        catch (DbUpdateException)
        {
            await transaccion.RollbackAsync();
            return false;
        }
    }
}
