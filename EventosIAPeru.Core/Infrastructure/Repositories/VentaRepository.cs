using Dapper;
using Microsoft.EntityFrameworkCore;
using EventosIAPeru.Core.Core.DTOs;
using EventosIAPeru.Core.Core.Interfaces;
using EventosIAPeru.Core.Infrastructure.Data;

namespace EventosIAPeru.Core.Infrastructure.Repositories
{
    /// <summary>
    /// Reporte de ventas (US-11 / US-12). Usa Dapper sobre la función almacenada
    /// fn_ventas_por_evento: la agregación (SUM/GROUP BY) se resuelve en la base de datos.
    /// Script: Infrastructure/Database/US11_FnVentasPorEvento.sql
    /// </summary>
    public class VentaRepository : IVentaRepository
    {
        private const string ConsultaVentas = @"
            SELECT evento_id               AS ""EventoId"",
                   nombre                  AS ""Nombre"",
                   fecha_inicio            AS ""FechaInicio"",
                   fecha_fin               AS ""FechaFin"",
                   sede                    AS ""Sede"",
                   precio                  AS ""Precio"",
                   aforo_total             AS ""AforoTotal"",
                   estado                  AS ""Estado"",
                   entradas_vendidas_total AS ""EntradasVendidasTotal"",
                   entradas_vendidas       AS ""EntradasVendidas"",
                   recaudado               AS ""Recaudado""
            FROM fn_ventas_por_evento(@OrganizadorId, @Desde::timestamptz, @Hasta::timestamptz)";

        private readonly EventosPeruIAContext _dbContext;

        public VentaRepository(EventosPeruIAContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IEnumerable<VentaEventoDTO>> GetVentasPorEvento(int organizadorId, DateTime? desde, DateTime? hasta)
        {
            // Reutilizamos la conexión del DbContext (misma cadena de conexión y pool)
            var conexion = _dbContext.Database.GetDbConnection();

            var parametros = new { OrganizadorId = organizadorId, Desde = desde, Hasta = hasta };
            return await conexion.QueryAsync<VentaEventoDTO>(ConsultaVentas, parametros);
        }
    }
}
