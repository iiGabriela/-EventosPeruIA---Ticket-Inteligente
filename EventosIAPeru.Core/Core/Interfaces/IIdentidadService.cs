namespace EventosIAPeru.Core.Core.Interfaces
{
    /// <summary>Resultado de comprobar un token de Firebase contra nuestra base.</summary>
    /// <param name="Email">Correo del perfil que corresponde a la sesión (null si se rechaza).</param>
    /// <param name="MensajeRechazo">Motivo cuando la sesión no puede usar ese perfil.</param>
    public record IdentidadResuelta(string? Email, string? MensajeRechazo)
    {
        public bool Aceptada => MensajeRechazo == null;
    }

    /// <summary>
    /// Decide a qué perfil corresponde una sesión de Firebase.
    /// La cuenta se identifica por el UID, nunca solo por el correo: cualquiera puede crear en
    /// Firebase una cuenta con un correo ajeno sin verificarlo.
    /// </summary>
    public interface IIdentidadService
    {
        Task<IdentidadResuelta> Resolver(string firebaseUid, string? email, bool emailVerificado);
    }
}
