using System.Security.Claims;

namespace EventosIAPeru.Core.Shared
{
    public static class ClaimsPrincipalExtensions
    {
        /// <summary>El token de Firebase trae el UID en "user_id" y en "sub".</summary>
        public static string? ObtenerFirebaseUid(this ClaimsPrincipal usuario)
        {
            return usuario.FindFirst("user_id")?.Value
                ?? usuario.FindFirst("sub")?.Value
                ?? usuario.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
    }
}
