using Microsoft.EntityFrameworkCore;
using EventosIAPeru.Core.Core.DTOs;
using EventosIAPeru.Core.Core.Interfaces;
using EventosIAPeru.Core.Infrastructure.Data;

namespace EventosIAPeru.Core.Infrastructure.Repositories
{
    public class VentaRepository : IVentaRepository
    {
        private readonly EventosPeruIAContext _dbContext;

        public VentaRepository(EventosPeruIAContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// Consulta agregada (US-11): una fila por evento del organizador con sus ventas.
        /// Solo cuentan las compras CONFIRMADAS. El cálculo se hace en la base de datos.
        /// </summary>
        public async Task<IEnumerable<VentaEventoDTO>> GetVentasPorEvento(int organizadorId, DateTime? desde, DateTime? hasta)
        {
            var ventas = await _dbContext
                                .Evento
                                .AsNoTracking()
                                .Where(e => e.OrganizadorId == organizadorId)
                                .OrderByDescending(e => e.FechaInicio)
                                .Select(e => new VentaEventoDTO
                                {
                                    EventoId = e.EventoId,
                                    Nombre = e.Nombre,
                                    FechaInicio = e.FechaInicio,
                                    FechaFin = e.FechaFin,
                                    Sede = e.Sede,
                                    Precio = e.Precio,
                                    AforoTotal = e.AforoTotal,
                                    Estado = e.Estado,
                                    EntradasVendidasTotal = e.Compra
                                        .Where(c => c.Estado == "CONFIRMADA")
                                        .Sum(c => c.Cantidad),
                                    EntradasVendidas = e.Compra
                                        .Where(c => c.Estado == "CONFIRMADA"
                                                 && (desde == null || c.FechaCompra >= desde)
                                                 && (hasta == null || c.FechaCompra < hasta))
                                        .Sum(c => c.Cantidad),
                                    Recaudado = e.Compra
                                        .Where(c => c.Estado == "CONFIRMADA"
                                                 && (desde == null || c.FechaCompra >= desde)
                                                 && (hasta == null || c.FechaCompra < hasta))
                                        .Sum(c => c.Cantidad * c.PrecioUnitario)
                                })
                                .ToListAsync();
            return ventas;
        }
    }
}
