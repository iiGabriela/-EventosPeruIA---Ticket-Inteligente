using EventosIAPeru.Core.Core.Entities;

namespace EventosIAPeru.Core.Core.Interfaces
{
    public interface ICategoriaRepository
    {
        Task<IEnumerable<CategoriaEvento>> GetCategorias();
        Task<CategoriaEvento?> GetCategoriaById(int id);
        Task<IEnumerable<CategoriaEvento>> GetCategoriasPopulares(int cantidad);
        Task<bool> RegistrarConsulta(int categoriaId);
    }
}
