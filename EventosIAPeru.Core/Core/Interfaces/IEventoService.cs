using EventosIAPeru.Core.Core.DTOs;
using EventosIAPeru.Core.Shared;

namespace EventosIAPeru.Core.Core.Interfaces
{
    public interface IEventoService
    {
        Task<(IEnumerable<EventoDTO> Eventos, int Total)> BuscarEventos(FiltroEventoDTO filtro);
        Task<EventoDTO?> GetEventoById(int id, string? emailUsuario);
        Task<IEnumerable<EventoDTO>?> GetMisEventos(string emailUsuario);
        Task<ResultadoOperacion> CrearEvento(string emailUsuario, CrearEventoDTO eventoDTO);
        Task<ResultadoOperacion> ActualizarEvento(string emailUsuario, ActualizarEventoDTO eventoDTO);
        Task<ResultadoOperacion> PublicarEvento(string emailUsuario, int id);
        Task<ResultadoOperacion> CancelarEvento(string emailUsuario, int id);
    }
}
