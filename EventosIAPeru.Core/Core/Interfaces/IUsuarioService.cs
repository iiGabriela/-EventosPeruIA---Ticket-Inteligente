using EventosIAPeru.Core.Core.DTOs;
using EventosIAPeru.Core.Shared;

namespace EventosIAPeru.Core.Core.Interfaces
{
    /// <summary>Reglas de negocio de US-01 (registro), US-02 (sesión) y US-03 (roles).</summary>
    public interface IUsuarioService
    {
        // US-01: crea el perfil del usuario en nuestra base (la cuenta ya existe en Firebase)
        Task<ResultadoOperacion> Registrar(string firebaseUid, RegistroUsuarioDTO dto);

        // US-02: devuelve el perfil si la cuenta existe y está activa
        Task<(ResultadoOperacion Resultado, UsuarioDTO? Usuario)> ObtenerPerfil(string firebaseUid);

        // US-03: el usuario activa el rol de organizador en su misma cuenta
        Task<ResultadoOperacion> ActivarRolOrganizador(string firebaseUid);

        // US-03: valida que el usuario tenga el rol y devuelve el mensaje para el toast
        Task<(ResultadoOperacion Resultado, CambioRolDTO? Cambio)> CambiarRolActivo(string firebaseUid, string rol);

        Task<(ResultadoOperacion Resultado, List<CategoriaDTO>? Intereses)> ObtenerIntereses(string firebaseUid);
        Task<ResultadoOperacion> ActualizarIntereses(string firebaseUid, ActualizarInteresesDTO dto);
    }
}
