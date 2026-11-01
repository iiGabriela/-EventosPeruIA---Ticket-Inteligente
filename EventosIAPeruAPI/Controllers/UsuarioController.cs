using Microsoft.AspNetCore.Mvc;
using EventosIAPeru.Core.Core.DTOs;
using EventosIAPeru.Core.Core.Interfaces;

namespace EventosIAPeru.API.Controllers
{
    /// <summary>
    /// Controller: puerta de entrada para registro, sesión y roles.
    /// El cierre de sesión lo hace el frontend con Firebase (signOut); al no tener
    /// token, el backend rechaza las rutas protegidas con 401.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class UsuarioController : BaseApiController
    {
        private readonly IUsuarioService _usuarioService;

        public UsuarioController(IUsuarioService usuarioService, IWebHostEnvironment environment)
            : base(environment)
        {
            _usuarioService = usuarioService;
        }

        // ---------------------------- US-01 ----------------------------

        /// <summary>Crea el perfil del usuario nuevo (nombre y correo). Empieza como ASISTENTE.</summary>
        [HttpPost("registro")]
        public async Task<IActionResult> Registrar([FromBody] RegistroUsuarioDTO dto)
        {
            var firebaseUid = ObtenerFirebaseUid();
            if (firebaseUid == null) return Unauthorized(new { mensaje = "Debe iniciar sesión." });

            var result = await _usuarioService.Registrar(firebaseUid, dto);
            if (!result.Exito) return Responder(result);

            // Devolvemos el perfil creado con un mensaje de confirmación
            var (_, perfil) = await _usuarioService.ObtenerPerfil(firebaseUid);
            return CreatedAtAction(nameof(GetMiPerfil), null, new { mensaje = "Cuenta creada correctamente.", usuario = perfil });
        }

        // ---------------------------- US-02 ----------------------------

        /// <summary>Perfil y roles del usuario logueado. Rechaza cuentas bloqueadas.</summary>
        [HttpGet("me")]
        public async Task<IActionResult> GetMiPerfil()
        {
            var firebaseUid = ObtenerFirebaseUid();
            if (firebaseUid == null) return Unauthorized(new { mensaje = "Debe iniciar sesión." });

            var (result, perfil) = await _usuarioService.ObtenerPerfil(firebaseUid);
            if (!result.Exito) return Responder(result);

            return Ok(perfil);
        }

        // ---------------------------- US-03 ----------------------------

        /// <summary>El usuario activa el rol ORGANIZADOR en su misma cuenta.</summary>
        [HttpPost("me/rol-organizador")]
        public async Task<IActionResult> ActivarRolOrganizador()
        {
            var firebaseUid = ObtenerFirebaseUid();
            if (firebaseUid == null) return Unauthorized(new { mensaje = "Debe iniciar sesión." });

            var result = await _usuarioService.ActivarRolOrganizador(firebaseUid);
            if (!result.Exito) return Responder(result);

            var (_, perfil) = await _usuarioService.ObtenerPerfil(firebaseUid);
            return Ok(perfil);
        }

        /// <summary>Valida el cambio de rol activo y devuelve el mensaje para el toast.</summary>
        [HttpPost("me/cambiar-rol")]
        public async Task<IActionResult> CambiarRol([FromBody] CambiarRolDTO dto)
        {
            var firebaseUid = ObtenerFirebaseUid();
            if (firebaseUid == null) return Unauthorized(new { mensaje = "Debe iniciar sesión." });

            var (result, cambio) = await _usuarioService.CambiarRolActivo(firebaseUid, dto.Rol);
            if (!result.Exito) return Responder(result);

            return Ok(cambio);
        }

        /// <summary>Obtiene las categorías de interés del usuario.</summary>
        [HttpGet("me/intereses")]
        public async Task<IActionResult> GetMisIntereses()
        {
            var firebaseUid = ObtenerFirebaseUid();
            if (firebaseUid == null) return Unauthorized(new { mensaje = "Debe iniciar sesión." });

            var (result, intereses) = await _usuarioService.ObtenerIntereses(firebaseUid);
            if (!result.Exito) return Responder(result);

            return Ok(intereses);
        }

        /// <summary>Reemplaza las categorías de interés usadas por US-10.</summary>
        [HttpPut("me/intereses")]
        public async Task<IActionResult> ActualizarMisIntereses([FromBody] ActualizarInteresesDTO dto)
        {
            var firebaseUid = ObtenerFirebaseUid();
            if (firebaseUid == null) return Unauthorized(new { mensaje = "Debe iniciar sesión." });

            var result = await _usuarioService.ActualizarIntereses(firebaseUid, dto);
            return Responder(result);
        }
    }
}
