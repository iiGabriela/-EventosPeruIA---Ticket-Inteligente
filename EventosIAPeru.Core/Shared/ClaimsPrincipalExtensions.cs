using System.Security.Claims;

namespace EventosIAPeru.Core.Shared
{
    public static class ClaimsPrincipalExtensions
    {
        /// <summary>
        /// Firebase solo valida el login; en nuestra base identificamos al usuario por su correo.
        /// El token trae el correo en "email" (ASP.NET puede mapearlo a ClaimTypes.Email).
        /// </summary>
        public static string? ObtenerEmail(this ClaimsPrincipal usuario)
        {
            var email = usuario.FindFirst("email")?.Value
                ?? usuario.FindFirst(ClaimTypes.Email)?.Value;
            return string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
        }
    }
}
