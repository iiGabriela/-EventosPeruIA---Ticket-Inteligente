using Microsoft.AspNetCore.Mvc;
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
        /// Correo del usuario autenticado (viene en el token de Firebase).
        /// Solo en Development se acepta el header "X-User-Email" para probar sin token.
        /// </summary>
        protected string? ObtenerEmailUsuario()
        {
            var email = User.ObtenerEmail();
            if (email == null && _environment.IsDevelopment())
            {
                email = Request.Headers["X-User-Email"].FirstOrDefault();
            }
            return string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
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
