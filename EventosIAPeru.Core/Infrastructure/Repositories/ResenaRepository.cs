using Microsoft.EntityFrameworkCore;
using EventosIAPeru.Core.Core.Entities;
using EventosIAPeru.Core.Core.Interfaces;
using EventosIAPeru.Core.Infrastructure.Data;

namespace EventosIAPeru.Core.Infrastructure.Repositories
{
    /// <summary>Repository de reseñas (US-13). Solo consulta y guarda en la base de datos.</summary>
    public class ResenaRepository : IResenaRepository
    {
        private readonly EventosPeruIAContext _dbContext;

        public ResenaRepository(EventosPeruIAContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Evento?> ObtenerEvento(int eventoId)
        {
            return await _dbContext
                            .Evento
                            .AsNoTracking()
                            .FirstOrDefaultAsync(e => e.EventoId == eventoId);
        }

        // Entrada válida = compra CONFIRMADA y entrada que no esté ANULADA.
        // Sirve si el usuario compró la entrada o si figura como asistente.
        public async Task<bool> TieneEntradaValida(int usuarioId, int eventoId)
        {
            return await _dbContext
                            .Compra
                            .AnyAsync(c => c.EventoId == eventoId
                                        && c.Estado == "CONFIRMADA"
                                        && c.Entrada.Any(en => en.Estado != "ANULADA"
                                                            && (c.UsuarioId == usuarioId || en.AsistenteUsuarioId == usuarioId)));
        }

        public async Task<bool> ExisteResena(int usuarioId, int eventoId)
        {
            return await _dbContext
                            .ResenaEvento
                            .AnyAsync(r => r.UsuarioId == usuarioId && r.EventoId == eventoId);
        }

        public async Task<bool> CrearResena(ResenaEvento resena)
        {
            _dbContext.ResenaEvento.Add(resena);
            try
            {
                await _dbContext.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException)
            {
                // Por ejemplo: la base rechazó una reseña repetida (usuario + evento es único)
                return false;
            }
        }

        public async Task<(double Promedio, int Total)> GetResumen(int eventoId)
        {
            var query = _dbContext.ResenaEvento.Where(r => r.EventoId == eventoId);

            var total = await query.CountAsync();
            if (total == 0) return (0, 0);

            // El promedio lo calcula la base de datos
            var promedio = await query.AverageAsync(r => (double)r.Calificacion);
            return (promedio, total);
        }

        public async Task<List<ResenaEvento>> GetResenasDeEvento(int eventoId)
        {
            return await _dbContext
                            .ResenaEvento
                            .AsNoTracking()
                            .Include(r => r.Usuario)
                            .Where(r => r.EventoId == eventoId)
                            .OrderByDescending(r => r.FechaCreacion)
                            .ToListAsync();
        }

        public async Task<List<ResenaEvento>> GetResenasDeUsuario(int usuarioId)
        {
            return await _dbContext
                            .ResenaEvento
                            .AsNoTracking()
                            .Include(r => r.Evento)
                            .Where(r => r.UsuarioId == usuarioId)
                            .OrderByDescending(r => r.FechaCreacion)
                            .ToListAsync();
        }

        // Eventos ya terminados, con entrada válida del usuario y sin reseña suya
        public async Task<List<Evento>> GetEventosPendientes(int usuarioId)
        {
            var ahora = DateTime.UtcNow;

            return await _dbContext
                            .Evento
                            .AsNoTracking()
                            .Where(e => e.FechaFin < ahora
                                     && e.Compra.Any(c => c.Estado == "CONFIRMADA"
                                                       && c.Entrada.Any(en => en.Estado != "ANULADA"
                                                                           && (c.UsuarioId == usuarioId || en.AsistenteUsuarioId == usuarioId)))
                                     && !e.ResenaEvento.Any(r => r.UsuarioId == usuarioId))
                            .OrderByDescending(e => e.FechaFin)
                            .ToListAsync();
        }
    }
}
