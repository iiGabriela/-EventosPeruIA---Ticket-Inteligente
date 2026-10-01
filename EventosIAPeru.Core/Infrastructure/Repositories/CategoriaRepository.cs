using Microsoft.EntityFrameworkCore;
using EventosIAPeru.Core.Core.Entities;
using EventosIAPeru.Core.Core.Interfaces;
using EventosIAPeru.Core.Infrastructure.Data;

namespace EventosIAPeru.Core.Infrastructure.Repositories
{
    public class CategoriaRepository : ICategoriaRepository
    {
        private readonly EventosPeruIAContext _dbContext;

        public CategoriaRepository(EventosPeruIAContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IEnumerable<CategoriaEvento>> GetCategorias()
        {
            var categorias = await _dbContext
                                .CategoriaEvento
                                .AsNoTracking()
                                .OrderBy(c => c.Nombre)
                                .ToListAsync();
            return categorias;
        }

        public async Task<CategoriaEvento?> GetCategoriaById(int id)
        {
            var categoria = await _dbContext
                                .CategoriaEvento
                                .AsNoTracking()
                                .Where(c => c.CategoriaId == id)
                                .FirstOrDefaultAsync();
            return categoria;
        }

        public async Task<List<CategoriaEvento>> GetCategoriasByIds(IEnumerable<int> ids)
        {
            var categoriaIds = ids.Distinct().ToList();
            if (categoriaIds.Count == 0)
                return new List<CategoriaEvento>();

            return await _dbContext.CategoriaEvento
                                .AsNoTracking()
                                .Where(c => categoriaIds.Contains(c.CategoriaId))
                                .OrderBy(c => c.Nombre)
                                .ToListAsync();
        }

        /// <summary>Categorías más consultadas (chips de acceso rápido, US-05).</summary>
        public async Task<IEnumerable<CategoriaEvento>> GetCategoriasPopulares(int cantidad)
        {
            var categorias = await _dbContext
                                .CategoriaEvento
                                .AsNoTracking()
                                .OrderByDescending(c => c.TotalConsultas)
                                .ThenBy(c => c.Nombre)
                                .Take(cantidad)
                                .ToListAsync();
            return categorias;
        }

        /// <summary>Suma 1 al contador de consultas cuando alguien filtra por esta categoría.</summary>
        public async Task<bool> RegistrarConsulta(int categoriaId)
        {
            var rows = await _dbContext
                                .CategoriaEvento
                                .Where(c => c.CategoriaId == categoriaId)
                                .ExecuteUpdateAsync(s => s.SetProperty(c => c.TotalConsultas, c => c.TotalConsultas + 1));
            return rows > 0;
        }
    }
}
