using Microsoft.AspNetCore.Mvc;
using EventosIAPeru.Core.Shared;

namespace EventosIAPeru.API.Controllers
{
    /// <summary>
    /// Controller base: junta lo que se repite en los controllers de Gabriela
    /// (leer el UID de Firebase y convertir el resultado en respuesta HTTP).
    /// </summary>
    public abstract class BaseApiController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;

        protected BaseApiController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        /// <summary>
        /// UID del usuario autenticado (token de Firebase).
        /// Solo en Development se acepta el header "X-Firebase-Uid" para probar sin Firebase.
        /// </summary>
        protected string? ObtenerFirebaseUid()
        {
            var firebaseUid = User.ObtenerFirebaseUid();
            if (string.IsNullOrWhiteSpace(firebaseUid) && _environment.IsDevelopment())
            {
                firebaseUid = Request.Headers["X-Firebase-Uid"].FirstOrDefault();
            }
            return string.IsNullOrWhiteSpace(firebaseUid) ? null : firebaseUid;
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
