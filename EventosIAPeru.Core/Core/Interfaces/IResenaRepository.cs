using EventosIAPeru.Core.Core.Entities;

namespace EventosIAPeru.Core.Core.Interfaces
{
    /// <summary>Repository de reseñas (US-13). Solo habla con la base de datos.</summary>
    public interface IResenaRepository
    {
        // Busca un evento por id
        Task<Evento?> ObtenerEvento(int eventoId);

        // ¿El usuario tiene una entrada válida (no anulada) de una compra confirmada de ese evento?
        Task<bool> TieneEntradaValida(int usuarioId, int eventoId);

        // ¿El usuario ya reseñó ese evento?
        Task<bool> ExisteResena(int usuarioId, int eventoId);

        // Guarda la reseña
        Task<bool> CrearResena(ResenaEvento resena);

        // Promedio de calificaciones y cantidad de reseñas de un evento
        Task<(double Promedio, int Total)> GetResumen(int eventoId);

        // Reseñas de un evento (más recientes primero)
        Task<List<ResenaEvento>> GetResenasDeEvento(int eventoId);

        // Reseñas que escribió un usuario
        Task<List<ResenaEvento>> GetResenasDeUsuario(int usuarioId);

        // Eventos terminados a los que asistió y aún no reseña
        Task<List<Evento>> GetEventosPendientes(int usuarioId);
    }
}
