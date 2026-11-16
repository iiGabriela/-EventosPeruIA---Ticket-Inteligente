using System.Security.Claims;
using EventosIAPeru.Core.Core.Interfaces;
using EventosIAPeru.Core.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace EventosIAPeru.API.Middleware
{
    /// <summary>
    /// US-02: después de validar el token de Firebase, averigua a qué perfil de la base corresponde
    /// (por UID) y lo deja disponible para los controllers.
    /// Además aplica "denegar por defecto": toda acción de la API exige sesión salvo las marcadas
    /// con [AllowAnonymous], así un endpoint nuevo nunca queda abierto por olvido.
    /// </summary>
    public class IdentidadMiddleware
    {
        public const string ClaveEmail = "Eventia.EmailUsuario";
        public const string ClaveUid = "Eventia.FirebaseUid";

        private readonly RequestDelegate _next;
        private readonly bool _permitirHeaderPrueba;

        public IdentidadMiddleware(RequestDelegate next, IConfiguration config, IWebHostEnvironment environment)
        {
            _next = next;
            // El atajo X-User-Email exige las dos cosas: entorno Development y activarlo a propósito.
            _permitirHeaderPrueba = environment.IsDevelopment() && config.GetValue("Auth:PermitirHeaderPrueba", false);
        }

        public async Task InvokeAsync(HttpContext context, IIdentidadService identidad)
        {
            var endpoint = context.GetEndpoint();
            var esAccionApi = endpoint?.Metadata.GetMetadata<ControllerActionDescriptor>() != null;
            if (!esAccionApi)
            {
                await _next(context);
                return;
            }

            var esPublica = endpoint!.Metadata.GetMetadata<IAllowAnonymous>() != null;
            string? email = null;

            if (context.User.Identity?.IsAuthenticated == true)
            {
                var uid = context.User.FindFirst("user_id")?.Value
                    ?? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? context.User.FindFirst("sub")?.Value;

                if (!string.IsNullOrWhiteSpace(uid))
                {
                    var verificado = string.Equals(context.User.FindFirst("email_verified")?.Value, "true",
                        StringComparison.OrdinalIgnoreCase);
                    var resultado = await identidad.Resolver(uid, context.User.ObtenerEmail(), verificado);

                    if (resultado.Aceptada)
                    {
                        email = resultado.Email;
                        context.Items[ClaveUid] = uid;
                    }
                    else if (!esPublica)
                    {
                        await Responder(context, StatusCodes.Status403Forbidden, resultado.MensajeRechazo!);
                        return;
                    }
                }
            }
            else if (_permitirHeaderPrueba)
            {
                var deHeader = context.Request.Headers["X-User-Email"].FirstOrDefault();
                email = string.IsNullOrWhiteSpace(deHeader) ? null : deHeader.Trim().ToLowerInvariant();
            }

            if (email != null) context.Items[ClaveEmail] = email;

            if (email == null && !esPublica)
            {
                await Responder(context, StatusCodes.Status401Unauthorized, "Debes iniciar sesión para continuar.");
                return;
            }

            await _next(context);
        }

        private static Task Responder(HttpContext context, int estado, string mensaje)
        {
            context.Response.StatusCode = estado;
            return context.Response.WriteAsJsonAsync(new { mensaje });
        }
    }
}
