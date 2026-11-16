using EventosIAPeru.Core.Core.Interfaces;

namespace EventosIAPeru.Core.Core.Services
{
    /// <summary>
    /// US-02: enlaza la sesión de Firebase con el perfil de la base.
    /// 1) Si el UID ya está enlazado, ese es el perfil (aunque el correo haya cambiado).
    /// 2) Si no hay perfil con ese correo, es un usuario nuevo que aún debe registrarse.
    /// 3) Si hay un perfil sin UID (creado por script o antes de este cambio), solo se enlaza
    ///    cuando Firebase confirma que el correo fue verificado.
    /// </summary>
    public class IdentidadService : IIdentidadService
    {
        public const string MensajeVerificar =
            "Este correo ya tiene un perfil en Eventia. Verifica tu correo desde el enlace que te enviamos y vuelve a ingresar para vincularlo.";
        public const string MensajeOtraCuenta =
            "Este correo pertenece a otra cuenta. Ingresa con la cuenta original.";
        public const string MensajeSinCorreo =
            "Tu sesión no incluye un correo electrónico válido.";

        private readonly IUsuarioRepository _usuarioRepository;

        public IdentidadService(IUsuarioRepository usuarioRepository)
        {
            _usuarioRepository = usuarioRepository;
        }

        public async Task<IdentidadResuelta> Resolver(string firebaseUid, string? email, bool emailVerificado)
        {
            var enlazado = await _usuarioRepository.GetUsuarioByFirebaseUid(firebaseUid);
            if (enlazado != null)
                return new IdentidadResuelta(enlazado.Email, null);

            if (string.IsNullOrWhiteSpace(email))
                return new IdentidadResuelta(null, MensajeSinCorreo);

            var correo = email.Trim().ToLowerInvariant();
            var porCorreo = await _usuarioRepository.GetUsuarioByEmail(correo);

            // Aún no tiene perfil: podrá registrarse y el perfil nacerá con su UID
            if (porCorreo == null)
                return new IdentidadResuelta(correo, null);

            // El perfil ya es de otra cuenta de Firebase
            if (!string.IsNullOrEmpty(porCorreo.FirebaseUid))
                return new IdentidadResuelta(null, MensajeOtraCuenta);

            // Perfil sin dueño: solo lo reclama quien demuestre que el correo es suyo
            if (!emailVerificado)
                return new IdentidadResuelta(null, MensajeVerificar);

            var vinculado = await _usuarioRepository.VincularFirebaseUid(porCorreo.UsuarioId, firebaseUid);
            return vinculado
                ? new IdentidadResuelta(correo, null)
                : new IdentidadResuelta(null, MensajeOtraCuenta);
        }
    }
}
