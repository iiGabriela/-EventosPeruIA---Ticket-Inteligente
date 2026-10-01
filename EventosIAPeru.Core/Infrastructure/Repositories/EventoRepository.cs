using Microsoft.EntityFrameworkCore;
using EventosIAPeru.Core.Core.DTOs;
using EventosIAPeru.Core.Core.Entities;
using EventosIAPeru.Core.Core.Interfaces;
using EventosIAPeru.Core.Infrastructure.Data;

namespace EventosIAPeru.Core.Infrastructure.Repositories
{
    public class EventoRepository : IEventoRepository
    {
        private readonly EventosPeruIAContext _dbContext;

        public EventoRepository(EventosPeruIAContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// Catálogo público (US-05). Solo eventos PUBLICADOS, no desactivados por moderación
        /// y que aún no terminan. Los que tienen cupos aparecen antes que los agotados.
        /// Las fechas del filtro deben llegar en UTC.
        /// </summary>
        public async Task<(IEnumerable<Evento> Eventos, int Total)> BuscarEventos(FiltroEventoDTO filtro)
        {
            var ahora = DateTime.UtcNow;

            var query = _dbContext
                            .Evento
                            .AsNoTracking()
                            .Include(e => e.Categoria)
                            .Where(e => e.Estado == "PUBLICADO"
                                     && e.EstadoModeracion != "DESACTIVADO"
                                     && e.FechaFin > ahora);

            if (!string.IsNullOrWhiteSpace(filtro.Texto))
            {
                var texto = $"%{filtro.Texto.Trim()}%";
                query = query.Where(e => EF.Functions.ILike(e.Nombre, texto)
                                      || EF.Functions.ILike(e.Descripcion, texto));
            }

            if (filtro.CategoriaId.HasValue)
            {
                var categoriaId = filtro.CategoriaId.Value;
                query = query.Where(e => e.CategoriaId == categoriaId);
            }

            if (!string.IsNullOrWhiteSpace(filtro.Ubicacion))
            {
                var ubicacion = $"%{filtro.Ubicacion.Trim()}%";
                query = query.Where(e => EF.Functions.ILike(e.Sede, ubicacion)
                                      || EF.Functions.ILike(e.Distrito, ubicacion)
                                      || EF.Functions.ILike(e.Provincia, ubicacion)
                                      || EF.Functions.ILike(e.Departamento, ubicacion));
            }

            if (filtro.FechaDesde.HasValue)
            {
                var desde = filtro.FechaDesde.Value;
                query = query.Where(e => e.FechaInicio >= desde);
            }

            if (filtro.FechaHasta.HasValue)
            {
                var hasta = filtro.FechaHasta.Value;
                query = query.Where(e => e.FechaInicio < hasta);
            }

            if (filtro.SoloDisponibles)
            {
                query = query.Where(e => e.Compra
                                          .Where(c => c.Estado == "CONFIRMADA")
                                          .Sum(c => c.Cantidad) < e.AforoTotal);
            }

            var total = await query.CountAsync();

            var eventos = await query
                                .OrderByDescending(e => e.Compra
                                                         .Where(c => c.Estado == "CONFIRMADA")
                                                         .Sum(c => c.Cantidad) < e.AforoTotal)
                                .ThenBy(e => e.FechaInicio)
                                .Skip((filtro.Pagina - 1) * filtro.TamanoPagina)
                                .Take(filtro.TamanoPagina)
                                .ToListAsync();

            return (eventos, total);
        }

        public async Task<Evento?> GetEventoById(int id)
        {
            var evento = await _dbContext
                                .Evento
                                .AsNoTracking()
                                .Include(e => e.Categoria)
                                .Include(e => e.Organizador)
                                .Where(e => e.EventoId == id)
                                .FirstOrDefaultAsync();
            return evento;
        }

        public async Task<IEnumerable<Evento>> GetEventosByOrganizador(int organizadorId)
        {
            var eventos = await _dbContext
                                .Evento
                                .AsNoTracking()
                                .Include(e => e.Categoria)
                                .Where(e => e.OrganizadorId == organizadorId)
                                .OrderByDescending(e => e.FechaInicio)
                                .ToListAsync();
            return eventos;
        }

        /// <summary>Entradas vendidas (compras CONFIRMADAS) por evento.</summary>
        public async Task<Dictionary<int, int>> GetEntradasVendidas(IEnumerable<int> eventoIds)
        {
            var ids = eventoIds.Distinct().ToList();

            var vendidas = await _dbContext
                                .Compra
                                .AsNoTracking()
                                .Where(c => ids.Contains(c.EventoId) && c.Estado == "CONFIRMADA")
                                .GroupBy(c => c.EventoId)
                                .Select(g => new { EventoId = g.Key, Cantidad = g.Sum(c => c.Cantidad) })
                                .ToDictionaryAsync(x => x.EventoId, x => x.Cantidad);
            return vendidas;
        }

        public async Task<bool> CreateEvento(Evento evento)
        {
            await _dbContext.Evento.AddAsync(evento);
            var rows = await _dbContext.SaveChangesAsync();
            return rows > 0;
        }

        public async Task<bool> UpdateEvento(Evento evento)
        {
            var existingEvento = await _dbContext
                                .Evento
                                .Where(e => e.EventoId == evento.EventoId)
                                .FirstOrDefaultAsync();
            if (existingEvento != null)
            {
                existingEvento.CategoriaId = evento.CategoriaId;
                existingEvento.Nombre = evento.Nombre;
                existingEvento.Descripcion = evento.Descripcion;
                existingEvento.ImagenUrl = evento.ImagenUrl;
                existingEvento.FechaInicio = evento.FechaInicio;
                existingEvento.FechaFin = evento.FechaFin;
                existingEvento.Sede = evento.Sede;
                existingEvento.Direccion = evento.Direccion;
                existingEvento.Departamento = evento.Departamento;
                existingEvento.Provincia = evento.Provincia;
                existingEvento.Distrito = evento.Distrito;
                existingEvento.AforoTotal = evento.AforoTotal;
                existingEvento.Precio = evento.Precio;
                existingEvento.FechaActualizacion = DateTime.UtcNow;

                var rows = await _dbContext.SaveChangesAsync();
                return rows > 0;
            }
            return false;
        }

        public async Task<bool> UpdateEstado(int id, string estado)
        {
            var existingEvento = await _dbContext
                                .Evento
                                .Where(e => e.EventoId == id)
                                .FirstOrDefaultAsync();
            if (existingEvento != null)
            {
                existingEvento.Estado = estado;
                existingEvento.FechaActualizacion = DateTime.UtcNow;

                var rows = await _dbContext.SaveChangesAsync();
                return rows > 0;
            }
            return false;
        }
    }
}
