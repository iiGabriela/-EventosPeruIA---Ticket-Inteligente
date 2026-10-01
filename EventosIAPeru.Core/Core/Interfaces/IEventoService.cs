using EventosIAPeru.Core.Core.DTOs;
using EventosIAPeru.Core.Shared;

namespace EventosIAPeru.Core.Core.Interfaces
{
    public interface IEventoService
    {
        Task<(IEnumerable<EventoDTO> Eventos, int Total)> BuscarEventos(FiltroEventoDTO filtro);
        Task<EventoDTO?> GetEventoById(int id, string? firebaseUid);
        Task<IEnumerable<EventoDTO>?> GetMisEventos(string firebaseUid);
        Task<ResultadoOperacion> CrearEvento(string firebaseUid, CrearEventoDTO eventoDTO);
        Task<ResultadoOperacion> ActualizarEvento(string firebaseUid, ActualizarEventoDTO eventoDTO);
        Task<ResultadoOperacion> PublicarEvento(string firebaseUid, int id);
        Task<ResultadoOperacion> CancelarEvento(string firebaseUid, int id);
    }
}
