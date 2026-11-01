using EventosIAPeru.Core.Core.Entities;

namespace EventosIAPeru.Core.Core.Interfaces
{
    /// <summary>
    /// Repository de usuarios (US-01, US-02, US-03).
    /// Solo habla con la base de datos, sin reglas de negocio.
    /// </summary>
    public interface IUsuarioRepository
    {
        
        Task<Usuario?> GetUsuarioByFirebaseUid(string firebaseUid);

        // Igual que el anterior pero trae también sus roles
        Task<Usuario?> GetUsuarioConRoles(string firebaseUid);

        // ¿Ya existe un usuario con ese correo?
        Task<bool> ExisteEmail(string email);

        // Guarda el usuario nuevo con un rol inicial (ej: ASISTENTE)
        Task<bool> CrearUsuario(Usuario usuario, string nombreRol);

        // Le agrega un rol a un usuario que ya existe (ej: ORGANIZADOR)
        Task<bool> AgregarRol(int usuarioId, string nombreRol);

        // ¿El usuario tiene ese rol? (sirve para validar permisos)
        Task<bool> TieneRol(int usuarioId, string nombreRol);

        Task<List<CategoriaEvento>> GetIntereses(int usuarioId);
        Task<bool> ActualizarIntereses(int usuarioId, IReadOnlyCollection<CategoriaEvento> categorias);
    }
}
