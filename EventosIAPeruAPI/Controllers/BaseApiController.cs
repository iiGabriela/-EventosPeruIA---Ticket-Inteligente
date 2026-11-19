using Microsoft.AspNetCore.Mvc;
using EventosIAPeru.API.Middleware;
using EventosIAPeru.Core.Shared;

namespace EventosIAPeru.API.Controllers
{
    /// <summary>
    /// Controller base: junta lo que se repite en los controllers de Gabriela
    /// (leer el correo del usuario logueado y convertir el resultado en respuesta HTTP).
    /// </summary>
    public abstract class BaseApiController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;

        protected BaseApiController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        /// <summary>
        /// Correo del perfil que corresponde a la sesión. Lo resuelve IdentidadMiddleware a partir
        /// del UID de Firebase; aquí nunca se lee directamente el correo del token.
        /// </summary>
        protected string? ObtenerEmailUsuario()
        {
            return HttpContext.Items[IdentidadMiddleware.ClaveEmail] as string;
        }

        /// <summary>UID de Firebase de la sesión (null con el atajo de pruebas).</summary>
        protected string? ObtenerFirebaseUid()
        {
            return HttpContext.Items[IdentidadMiddleware.ClaveUid] as string;
        }

        // Convierte el ResultadoOperacion en el código HTTP que corresponde
        protected IActionResult Responder(ResultadoOperacion result)
        {
            switch (result.Tipo)
            {
                case TipoResultado.NoEncontrado:
                    return NotFound(new { mensaje = result.Mensaje });
                case TipoResultado.NoAutorizado:
                    return StatusCode(StatusCodes.Status403Forbidden, new { mensaje = result.Mensaje });
                case TipoResultado.Invalido:
                    return BadRequest(new { mensaje = result.Mensaje });
                default:
                    return NoContent();
            }
        }
    }
}
