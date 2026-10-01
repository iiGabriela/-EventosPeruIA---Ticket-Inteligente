using Microsoft.AspNetCore.Mvc;
using EventosIAPeru.Core.Core.Interfaces;

namespace EventosIAPeru.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class NotificacionController : BaseApiController
{
    private readonly INotificacionService _notificacionService;

    public NotificacionController(INotificacionService notificacionService,
                                  IWebHostEnvironment environment)
        : base(environment)
    {
        _notificacionService = notificacionService;
    }

    /// <summary>Obtiene las notificaciones internas del asistente.</summary>
    [HttpGet]
    public async Task<IActionResult> GetMisNotificaciones()
    {
        var firebaseUid = ObtenerFirebaseUid();
        if (firebaseUid == null)
            return Unauthorized(new { mensaje = "Debe iniciar sesión." });

        var notificaciones = await _notificacionService.ObtenerMisNotificaciones(firebaseUid);
        if (notificaciones == null)
            return NotFound(new { mensaje = "Tu usuario no está registrado." });

        return Ok(new
        {
            total = notificaciones.Count,
            noLeidas = notificaciones.Count(n => !n.Leida),
            notificaciones
        });
    }

    /// <summary>Marca una notificación propia como leída.</summary>
    [HttpPut("{id:int}/leer")]
    public async Task<IActionResult> MarcarComoLeida(int id)
    {
        var firebaseUid = ObtenerFirebaseUid();
        if (firebaseUid == null)
            return Unauthorized(new { mensaje = "Debe iniciar sesión." });

        return Responder(await _notificacionService.MarcarComoLeida(firebaseUid, id));
    }

    /// <summary>Marca todas las notificaciones propias como leídas.</summary>
    [HttpPut("leer-todas")]
    public async Task<IActionResult> MarcarTodasComoLeidas()
    {
        var firebaseUid = ObtenerFirebaseUid();
        if (firebaseUid == null)
            return Unauthorized(new { mensaje = "Debe iniciar sesión." });

        return Responder(await _notificacionService.MarcarTodasComoLeidas(firebaseUid));
    }
}
