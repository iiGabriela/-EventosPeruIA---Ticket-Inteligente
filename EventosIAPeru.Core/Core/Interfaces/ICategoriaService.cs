using EventosIAPeru.Core.Core.DTOs;

namespace EventosIAPeru.Core.Core.Interfaces
{
    public interface ICategoriaService
    {
        Task<IEnumerable<CategoriaDTO>> GetCategorias();
        Task<CategoriaDTO?> GetCategoriaById(int id);
        Task<IEnumerable<CategoriaDTO>> GetCategoriasPopulares(int cantidad);
    }
}
