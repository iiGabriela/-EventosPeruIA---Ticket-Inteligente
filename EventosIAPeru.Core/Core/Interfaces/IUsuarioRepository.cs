using EventosIAPeru.Core.Core.Entities;

namespace EventosIAPeru.Core.Core.Interfaces
{
    /// <summary>
    /// US-01, US-02, US-03 (Gabriela) completará este repositorio.
    /// Por ahora solo tiene lo que necesitan Eventos y Ventas para identificar al organizador.
    /// </summary>
    public interface IUsuarioRepository
    {
        Task<Usuario?> GetUsuarioByFirebaseUid(string firebaseUid);
    }
}
