using Microsoft.AspNetCore.Mvc;
using EventosIAPeru.Core.Core.DTOs;
using EventosIAPeru.Core.Core.Interfaces;
using EventosIAPeru.Core.Shared;

namespace EventosIAPeru.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VentaController : ControllerBase
    {
        private readonly IVentaService _ventaService;
        private readonly IWebHostEnvironment _environment;

        public VentaController(IVentaService ventaService, IWebHostEnvironment environment)
        {
            _ventaService = ventaService;
            _environment = environment;
        }

        /// <summary>US-11: panel de ventas del organizador (solo sus eventos).</summary>
        [HttpGet("panel")]
        public async Task<IActionResult> GetPanelVentas([FromQuery] FiltroVentasDTO filtro)
        {
            var firebaseUid = ObtenerFirebaseUid();
            if (firebaseUid == null) return Unauthorized(new { mensaje = "Debe iniciar sesión." });
            if (!FechasValidas(filtro)) return BadRequest(new { mensaje = "La fecha 'desde' no puede ser mayor que la fecha 'hasta'." });

            var panel = await _ventaService.GetPanelVentas(firebaseUid, filtro);
            if (panel == null) return NotFound(new { mensaje = "Tu usuario no está registrado." });
            return Ok(panel);
        }

        /// <summary>US-12: exporta a Excel los mismos datos del panel.</summary>
        [HttpGet("exportar/excel")]
        public async Task<IActionResult> ExportarExcel([FromQuery] FiltroVentasDTO filtro)
        {
            var firebaseUid = ObtenerFirebaseUid();
            if (firebaseUid == null) return Unauthorized(new { mensaje = "Debe iniciar sesión." });
            if (!FechasValidas(filtro)) return BadRequest(new { mensaje = "La fecha 'desde' no puede ser mayor que la fecha 'hasta'." });

            var panel = await _ventaService.GetPanelVentas(firebaseUid, filtro);
            if (panel == null) return NotFound(new { mensaje = "Tu usuario no está registrado." });
            if (panel.EntradasVendidas == 0) return NotFound(new { mensaje = "No existen ventas para el periodo seleccionado." });

            var archivo = _ventaService.GenerarExcel(panel);
            var nombre = $"reporte-ventas-{FechaHelper.AHoraPeru(DateTime.UtcNow):yyyyMMdd-HHmm}.xlsx";
            return File(archivo, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", nombre);
        }

        /// <summary>US-12: exporta a PDF los mismos datos del panel.</summary>
        [HttpGet("exportar/pdf")]
        public async Task<IActionResult> ExportarPdf([FromQuery] FiltroVentasDTO filtro)
        {
            var firebaseUid = ObtenerFirebaseUid();
            if (firebaseUid == null) return Unauthorized(new { mensaje = "Debe iniciar sesión." });
            if (!FechasValidas(filtro)) return BadRequest(new { mensaje = "La fecha 'desde' no puede ser mayor que la fecha 'hasta'." });

            var panel = await _ventaService.GetPanelVentas(firebaseUid, filtro);
            if (panel == null) return NotFound(new { mensaje = "Tu usuario no está registrado." });
            if (panel.EntradasVendidas == 0) return NotFound(new { mensaje = "No existen ventas para el periodo seleccionado." });

            var archivo = _ventaService.GenerarPdf(panel);
            var nombre = $"reporte-ventas-{FechaHelper.AHoraPeru(DateTime.UtcNow):yyyyMMdd-HHmm}.pdf";
            return File(archivo, "application/pdf", nombre);
        }

        // ---------------------------- Ayudantes ----------------------------

        private static bool FechasValidas(FiltroVentasDTO filtro)
        {
            return !(filtro.FechaDesde.HasValue && filtro.FechaHasta.HasValue && filtro.FechaDesde > filtro.FechaHasta);
        }

        /// <summary>
        /// UID del usuario autenticado (token de Firebase).
        /// Solo en Development, mientras se configura Firebase (US-02), se acepta el header
        /// "X-Firebase-Uid" para poder probar la API.
        /// </summary>
        private string? ObtenerFirebaseUid()
        {
            var firebaseUid = User.ObtenerFirebaseUid();
            if (string.IsNullOrWhiteSpace(firebaseUid) && _environment.IsDevelopment())
            {
                firebaseUid = Request.Headers["X-Firebase-Uid"].FirstOrDefault();
            }
            return string.IsNullOrWhiteSpace(firebaseUid) ? null : firebaseUid;
        }
    }
}
