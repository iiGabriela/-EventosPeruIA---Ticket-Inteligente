using System.Security.Authentication;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Mvc;
using EventosIAPeru.Core.Core.DTOs;
using EventosIAPeru.Core.Core.Interfaces;

namespace EventosIAPeru.API.Controllers
{
    /// <summary>
    /// Controller: panel de moderación (US-15).
    /// Todas las rutas son solo para ADMINISTRADOR (lo valida el service),
    /// excepto "reportar", que puede usar cualquier usuario con sesión.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ModeracionController : BaseApiController
    {
        private readonly IModeracionService _moderacionService;

        public ModeracionController(IModeracionService moderacionService, IWebHostEnvironment environment)
            : base(environment)
        {
            _moderacionService = moderacionService;
        }

        // ---------------------------- Panel ----------------------------

        /// <summary>Indicadores globales: usuarios activos, eventos publicados, revisiones, suspendidas.</summary>
        [HttpGet("panel")]
        public async Task<IActionResult> GetPanel()
        {
            var firebaseUid = ObtenerFirebaseUid();
            if (firebaseUid == null) return Unauthorized(new { mensaje = "Debe iniciar sesión." });

            var (result, panel) = await _moderacionService.GetPanel(firebaseUid);
            if (!result.Exito) return Responder(result);

            // Mostramos el cifrado real de esta conexión (por ejemplo TLS 1.3)
            panel!.Encriptacion = ObtenerNivelCifrado();
            return Ok(panel);
        }

        // ---------------------------- Eventos ----------------------------

        /// <summary>Lista de eventos para moderar (filtros: estadoModeracion y texto).</summary>
        [HttpGet("eventos")]
        public async Task<IActionResult> BuscarEventos([FromQuery] string? estadoModeracion, [FromQuery] string? texto, [FromQuery] int pagina = 1)
        {
            var firebaseUid = ObtenerFirebaseUid();
            if (firebaseUid == null) return Unauthorized(new { mensaje = "Debe iniciar sesión." });

            var (result, lista) = await _moderacionService.BuscarEventos(firebaseUid, estadoModeracion, texto, pagina);
            if (!result.Exito) return Responder(result);
            return Ok(lista);
        }

        /// <summary>Detalle de un evento: validaciones, aforo y reportes activos.</summary>
        [HttpGet("eventos/{id:int}")]
        public async Task<IActionResult> GetDetalleEvento(int id)
        {
            var firebaseUid = ObtenerFirebaseUid();
            if (firebaseUid == null) return Unauthorized(new { mensaje = "Debe iniciar sesión." });

            var (result, detalle) = await _moderacionService.GetDetalleEvento(firebaseUid, id);
            if (!result.Exito) return Responder(result);
            return Ok(detalle);
        }

        /// <summary>Aprueba (CONFORME), suspende (DESACTIVADO) o deja en revisión un evento.</summary>
        [HttpPut("eventos/{id:int}/moderar")]
        public async Task<IActionResult> ModerarEvento(int id, [FromBody] ModerarEventoDTO dto)
        {
            var firebaseUid = ObtenerFirebaseUid();
            if (firebaseUid == null) return Unauthorized(new { mensaje = "Debe iniciar sesión." });

            var result = await _moderacionService.ModerarEvento(firebaseUid, id, dto);
            return Responder(result);
        }

        // ---------------------------- Reportes ----------------------------

        /// <summary>Un usuario con sesión reporta un evento (no hace falta ser admin).</summary>
        [HttpPost("eventos/{id:int}/reportar")]
        public async Task<IActionResult> ReportarEvento(int id, [FromBody] CrearReporteDTO dto)
        {
            var firebaseUid = ObtenerFirebaseUid();
            if (firebaseUid == null) return Unauthorized(new { mensaje = "Debe iniciar sesión." });

            var result = await _moderacionService.ReportarEvento(firebaseUid, id, dto);
            if (!result.Exito) return Responder(result);

            return StatusCode(StatusCodes.Status201Created, new { mensaje = "Reporte enviado. Un administrador lo revisará.", reporteId = result.Id });
        }

        /// <summary>Reportes de eventos (filtro opcional por estado: PENDIENTE, RESUELTO, DESCARTADO).</summary>
        [HttpGet("reportes")]
        public async Task<IActionResult> GetReportes([FromQuery] string? estado)
        {
            var firebaseUid = ObtenerFirebaseUid();
            if (firebaseUid == null) return Unauthorized(new { mensaje = "Debe iniciar sesión." });

            var (result, reportes) = await _moderacionService.GetReportes(firebaseUid, estado);
            if (!result.Exito) return Responder(result);
            return Ok(reportes);
        }

        /// <summary>El admin resuelve o descarta un reporte.</summary>
        [HttpPut("reportes/{id:int}/resolver")]
        public async Task<IActionResult> ResolverReporte(int id, [FromBody] ResolverReporteDTO dto)
        {
            var firebaseUid = ObtenerFirebaseUid();
            if (firebaseUid == null) return Unauthorized(new { mensaje = "Debe iniciar sesión." });

            var result = await _moderacionService.ResolverReporte(firebaseUid, id, dto);
            return Responder(result);
        }

        // ---------------------------- Usuarios ----------------------------

        /// <summary>Busca usuarios por nombre o correo (filtro opcional por estado).</summary>
        [HttpGet("usuarios")]
        public async Task<IActionResult> BuscarUsuarios([FromQuery] string? texto, [FromQuery] string? estado)
        {
            var firebaseUid = ObtenerFirebaseUid();
            if (firebaseUid == null) return Unauthorized(new { mensaje = "Debe iniciar sesión." });

            var (result, usuarios) = await _moderacionService.BuscarUsuarios(firebaseUid, texto, estado);
            if (!result.Exito) return Responder(result);
            return Ok(usuarios);
        }

        /// <summary>Bloquea, desactiva o reactiva una cuenta.</summary>
        [HttpPut("usuarios/{id:int}/estado")]
        public async Task<IActionResult> CambiarEstadoUsuario(int id, [FromBody] CambiarEstadoUsuarioDTO dto)
        {
            var firebaseUid = ObtenerFirebaseUid();
            if (firebaseUid == null) return Unauthorized(new { mensaje = "Debe iniciar sesión." });

            var result = await _moderacionService.CambiarEstadoUsuario(firebaseUid, id, dto);
            return Responder(result);
        }

        /// <summary>Marca una cuenta como verificada.</summary>
        [HttpPut("usuarios/{id:int}/verificar")]
        public async Task<IActionResult> VerificarUsuario(int id)
        {
            var firebaseUid = ObtenerFirebaseUid();
            if (firebaseUid == null) return Unauthorized(new { mensaje = "Debe iniciar sesión." });

            var result = await _moderacionService.VerificarUsuario(firebaseUid, id);
            return Responder(result);
        }

        // ---------------------------- Auditoría ----------------------------

        /// <summary>Log de auditoría (últimos 200 registros, con filtro opcional por fechas).</summary>
        [HttpGet("auditoria")]
        public async Task<IActionResult> GetAuditoria([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
        {
            var firebaseUid = ObtenerFirebaseUid();
            if (firebaseUid == null) return Unauthorized(new { mensaje = "Debe iniciar sesión." });

            var (result, registros) = await _moderacionService.GetAuditoria(firebaseUid, desde, hasta);
            if (!result.Exito) return Responder(result);
            return Ok(registros);
        }

        /// <summary>Descarga el log de auditoría como CSV (se abre en Excel).</summary>
        [HttpGet("auditoria/exportar")]
        public async Task<IActionResult> ExportarAuditoria()
        {
            var firebaseUid = ObtenerFirebaseUid();
            if (firebaseUid == null) return Unauthorized(new { mensaje = "Debe iniciar sesión." });

            var (result, archivo) = await _moderacionService.ExportarAuditoria(firebaseUid);
            if (!result.Exito) return Responder(result);

            return File(archivo!, "text/csv", "auditoria.csv");
        }

        // ---------------------------- Reglas ----------------------------

        /// <summary>Reglas automáticas de moderación (simples y de IA).</summary>
        [HttpGet("reglas")]
        public async Task<IActionResult> GetReglas()
        {
            var firebaseUid = ObtenerFirebaseUid();
            if (firebaseUid == null) return Unauthorized(new { mensaje = "Debe iniciar sesión." });

            var (result, reglas) = await _moderacionService.GetReglas(firebaseUid);
            if (!result.Exito) return Responder(result);
            return Ok(reglas);
        }

        /// <summary>Crea una regla. SIMPLE: campo + operador + valor. IA: solo el criterio en texto.</summary>
        [HttpPost("reglas")]
        public async Task<IActionResult> CrearRegla([FromBody] CrearReglaDTO dto)
        {
            var firebaseUid = ObtenerFirebaseUid();
            if (firebaseUid == null) return Unauthorized(new { mensaje = "Debe iniciar sesión." });

            var result = await _moderacionService.CrearRegla(firebaseUid, dto);
            if (!result.Exito) return Responder(result);

            return StatusCode(StatusCodes.Status201Created, new { mensaje = "Regla creada.", reglaId = result.Id });
        }

        /// <summary>Activa o desactiva una regla: PUT reglas/3/activa?activa=false</summary>
        [HttpPut("reglas/{id:int}/activa")]
        public async Task<IActionResult> CambiarEstadoRegla(int id, [FromQuery] bool activa)
        {
            var firebaseUid = ObtenerFirebaseUid();
            if (firebaseUid == null) return Unauthorized(new { mensaje = "Debe iniciar sesión." });

            var result = await _moderacionService.CambiarEstadoRegla(firebaseUid, id, activa);
            return Responder(result);
        }

        // ---------------------------- Ayudantes ----------------------------

        // Lee el protocolo de cifrado de la conexión actual (TLS 1.2, TLS 1.3...)
        private string ObtenerNivelCifrado()
        {
            var tls = HttpContext.Features.Get<ITlsHandshakeFeature>();
            if (tls == null || tls.Protocol == SslProtocols.None)
                return "Sin cifrado (solo desarrollo)";

            return tls.Protocol.ToString().Replace("Tls1", "TLS 1.");
        }
    }
}
