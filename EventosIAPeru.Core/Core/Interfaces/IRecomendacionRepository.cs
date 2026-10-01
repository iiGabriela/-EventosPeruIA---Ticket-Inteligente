using EventosIAPeru.Core.Core.DTOs;

namespace EventosIAPeru.Core.Core.Interfaces;

public interface IRecomendacionRepository
{
    Task<RecomendacionPerfil> ObtenerPerfil(int usuarioId, DateTime comprasDesde);
    Task<List<RecomendacionEventoItem>> ObtenerEventosPorCategorias(IEnumerable<int> categoriaIds, int limite);
    Task<List<RecomendacionEventoItem>> ObtenerEventosPopulares(int limite, IEnumerable<int> excluirEventoIds);
}
