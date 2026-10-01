using Microsoft.EntityFrameworkCore;
using EventosIAPeru.Core.Core.DTOs;
using EventosIAPeru.Core.Core.Interfaces;
using EventosIAPeru.Core.Infrastructure.Data;

namespace EventosIAPeru.Core.Infrastructure.Repositories;

public class RecomendacionRepository : IRecomendacionRepository
{
    private readonly EventosPeruIAContext _dbContext;

    public RecomendacionRepository(EventosPeruIAContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<RecomendacionPerfil> ObtenerPerfil(int usuarioId, DateTime comprasDesde)
    {
        var categoriasInteres = await _dbContext.Usuario
            .AsNoTracking()
            .Where(u => u.UsuarioId == usuarioId)
            .SelectMany(u => u.Categoria.Select(c => c.CategoriaId))
            .Distinct()
            .ToListAsync();

        var compras = _dbContext.Compra
            .AsNoTracking()
            .Where(c => c.UsuarioId == usuarioId
                     && c.Estado == "CONFIRMADA"
                     && c.FechaCompra >= comprasDesde);

        var categoriasCompradas = await compras
            .Select(c => c.Evento.CategoriaId)
            .Distinct()
            .ToListAsync();

        var eventosComprados = await _dbContext.Compra
            .AsNoTracking()
            .Where(c => c.UsuarioId == usuarioId && c.Estado == "CONFIRMADA")
            .Select(c => c.EventoId)
            .Distinct()
            .ToListAsync();

        return new RecomendacionPerfil
        {
            CategoriasInteres = categoriasInteres,
            CategoriasCompradas = categoriasCompradas,
            EventosComprados = eventosComprados
        };
    }

    public async Task<List<RecomendacionEventoItem>> ObtenerEventosPorCategorias(IEnumerable<int> categoriaIds, int limite)
    {
        var ids = categoriaIds.Distinct().ToList();
        if (ids.Count == 0)
            return new List<RecomendacionEventoItem>();

        var query = CrearConsultaBase()
            .Where(e => ids.Contains(e.CategoriaId));

        return await AplicarOrden(query, limite).ToListAsync();
    }

    public async Task<List<RecomendacionEventoItem>> ObtenerEventosPopulares(int limite, IEnumerable<int> excluirEventoIds)
    {
        var idsExcluidos = excluirEventoIds.Distinct().ToList();
        var query = CrearConsultaBase();

        if (idsExcluidos.Count > 0)
            query = query.Where(e => !idsExcluidos.Contains(e.EventoId));

        return await AplicarOrden(query, limite).ToListAsync();
    }

    private IQueryable<EventoRecomendacionProjection> CrearConsultaBase()
    {
        var ahora = DateTime.UtcNow;

        return _dbContext.Evento
            .AsNoTracking()
            .Where(e => e.Estado == "PUBLICADO"
                     && e.EstadoModeracion != "DESACTIVADO"
                     && e.FechaInicio > ahora
                     && e.FechaFin > ahora)
            .Select(e => new EventoRecomendacionProjection
            {
                EventoId = e.EventoId,
                OrganizadorId = e.OrganizadorId,
                OrganizadorNombre = e.Organizador.Nombre,
                CategoriaId = e.CategoriaId,
                CategoriaNombre = e.Categoria.Nombre,
                Nombre = e.Nombre,
                Descripcion = e.Descripcion,
                ImagenUrl = e.ImagenUrl,
                FechaInicio = e.FechaInicio,
                FechaFin = e.FechaFin,
                Sede = e.Sede,
                Direccion = e.Direccion,
                Departamento = e.Departamento,
                Provincia = e.Provincia,
                Distrito = e.Distrito,
                AforoTotal = e.AforoTotal,
                EntradasVendidas = e.Compra
                    .Where(c => c.Estado == "CONFIRMADA")
                    .Sum(c => (int?)c.Cantidad) ?? 0,
                CuposDisponibles = e.AforoTotal - (e.Compra
                    .Where(c => c.Estado == "CONFIRMADA")
                    .Sum(c => (int?)c.Cantidad) ?? 0),
                Precio = e.Precio,
                Estado = e.Estado,
                EstadoModeracion = e.EstadoModeracion
            });
    }

    private static IQueryable<RecomendacionEventoItem> AplicarOrden(
        IQueryable<EventoRecomendacionProjection> query,
        int limite)
    {
        return query
            .OrderByDescending(e => e.CuposDisponibles > 0)
            .ThenByDescending(e => e.EntradasVendidas)
            .ThenBy(e => e.FechaInicio)
            .Take(limite)
            .Select(e => new RecomendacionEventoItem
            {
                EventoId = e.EventoId,
                OrganizadorId = e.OrganizadorId,
                OrganizadorNombre = e.OrganizadorNombre,
                CategoriaId = e.CategoriaId,
                CategoriaNombre = e.CategoriaNombre,
                Nombre = e.Nombre,
                Descripcion = e.Descripcion,
                ImagenUrl = e.ImagenUrl,
                FechaInicio = e.FechaInicio,
                FechaFin = e.FechaFin,
                Sede = e.Sede,
                Direccion = e.Direccion,
                Departamento = e.Departamento,
                Provincia = e.Provincia,
                Distrito = e.Distrito,
                AforoTotal = e.AforoTotal,
                EntradasVendidas = e.EntradasVendidas,
                CuposDisponibles = e.CuposDisponibles,
                Precio = e.Precio,
                Estado = e.Estado,
                EstadoModeracion = e.EstadoModeracion
            });
    }

    private sealed class EventoRecomendacionProjection
    {
        public int EventoId { get; set; }
        public int OrganizadorId { get; set; }
        public string? OrganizadorNombre { get; set; }
        public int CategoriaId { get; set; }
        public string? CategoriaNombre { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string? ImagenUrl { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public string Sede { get; set; } = string.Empty;
        public string Direccion { get; set; } = string.Empty;
        public string Departamento { get; set; } = string.Empty;
        public string Provincia { get; set; } = string.Empty;
        public string Distrito { get; set; } = string.Empty;
        public int AforoTotal { get; set; }
        public int EntradasVendidas { get; set; }
        public int CuposDisponibles { get; set; }
        public decimal Precio { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string EstadoModeracion { get; set; } = string.Empty;
    }
}
