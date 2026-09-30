using EventosIAPeru.Core.Core.DTOs;
using EventosIAPeru.Core.Core.Entities;
using EventosIAPeru.Core.Core.Interfaces;

namespace EventosIAPeru.Core.Core.Services
{
    public class CategoriaService : ICategoriaService
    {
        private readonly ICategoriaRepository _categoriaRepository;

        public CategoriaService(ICategoriaRepository categoriaRepository)
        {
            _categoriaRepository = categoriaRepository;
        }

        public async Task<IEnumerable<CategoriaDTO>> GetCategorias()
        {
            var categorias = await _categoriaRepository.GetCategorias();
            var categoriasDTO = new List<CategoriaDTO>();

            foreach (var categoria in categorias)
            {
                categoriasDTO.Add(MapearCategoria(categoria));
            }
            return categoriasDTO;
        }

        public async Task<CategoriaDTO?> GetCategoriaById(int id)
        {
            var categoria = await _categoriaRepository.GetCategoriaById(id);
            if (categoria == null) return null;

            return MapearCategoria(categoria);
        }

        public async Task<IEnumerable<CategoriaDTO>> GetCategoriasPopulares(int cantidad)
        {
            var categorias = await _categoriaRepository.GetCategoriasPopulares(cantidad);
            var categoriasDTO = new List<CategoriaDTO>();

            foreach (var categoria in categorias)
            {
                categoriasDTO.Add(MapearCategoria(categoria));
            }
            return categoriasDTO;
        }

        private static CategoriaDTO MapearCategoria(CategoriaEvento categoria)
        {
            var categoriaDTO = new CategoriaDTO();
            categoriaDTO.CategoriaId = categoria.CategoriaId;
            categoriaDTO.Nombre = categoria.Nombre;
            categoriaDTO.TotalConsultas = categoria.TotalConsultas;
            return categoriaDTO;
        }
    }
}
