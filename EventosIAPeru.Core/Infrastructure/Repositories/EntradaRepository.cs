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

    public async Task<List<Entrada>> GetEntradasDeUsuario(int usuarioId)
    {
        return await _dbContext.Entrada
                               .AsNoTracking()
                               .Include(e => e.Compra)
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

    public async Task RegistrarCheckIn(CheckIn checkIn)
    {
        await _dbContext.CheckIn.AddAsync(checkIn);
        await _dbContext.SaveChangesAsync();
    }

    public async Task MarcarEntradaUtilizada(Entrada entrada)
    {
        _dbContext.Entrada.Update(entrada);
        await _dbContext.SaveChangesAsync();
    }
}
