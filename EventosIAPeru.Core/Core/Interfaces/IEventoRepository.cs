using EventosIAPeru.Core.Core.DTOs;
using EventosIAPeru.Core.Core.Entities;

namespace EventosIAPeru.Core.Core.Interfaces
{
    public interface IEventoRepository
    {
        Task<(IEnumerable<Evento> Eventos, int Total)> BuscarEventos(FiltroEventoDTO filtro);
        Task<Evento?> GetEventoById(int id);
        Task<IEnumerable<Evento>> GetEventosByOrganizador(int organizadorId);
        Task<Dictionary<int, int>> GetEntradasVendidas(IEnumerable<int> eventoIds);
        Task<bool> CreateEvento(Evento evento);
        Task<bool> UpdateEvento(Evento evento);
        Task<bool> UpdateEstado(int id, string estado);
        /// <summary>US-15: pasa el evento a PUBLICADO + EN_REVISION (pendiente de aprobación del admin).</summary>
        Task<bool> SolicitarAprobacion(int id);

        // US-04 / US-17: borra los embeddings del evento para que el RAG lo vuelva a indexar
        Task EliminarEmbeddings(int eventoId);
    }
}
