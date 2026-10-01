using EventosIAPeru.Core.Core.DTOs;
using EventosIAPeru.Core.Shared;

namespace EventosIAPeru.Core.Core.Interfaces
{
    /// <summary>Reglas de negocio de US-13 (calificación y reseñas de eventos).</summary>
    public interface IResenaService
    {
        // Crea la reseña si cumple las reglas (entrada válida, evento terminado, una por evento)
        Task<ResultadoOperacion> CrearResena(string firebaseUid, CrearResenaDTO dto);

        // Promedio y reseñas publicadas de un evento (null si el evento no existe)
        Task<ResumenResenasDTO?> GetResenasDeEvento(int eventoId);

        // Historial: reseñas que escribió el usuario (null si el usuario no está registrado)
        Task<IEnumerable<ResenaDTO>?> GetMisResenas(string firebaseUid);

        // Pestaña "Historial & Reseñas": eventos pendientes de reseñar con su contador
        Task<PendientesResenaDTO?> GetPendientes(string firebaseUid);
    }
}
