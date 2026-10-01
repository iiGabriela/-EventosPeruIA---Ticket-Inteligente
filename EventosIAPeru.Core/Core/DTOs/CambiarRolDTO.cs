using System.ComponentModel.DataAnnotations;

namespace EventosIAPeru.Core.Core.DTOs
{
    // DTO: el frontend pide cambiar de rol activo (US-03).
    public class CambiarRolDTO
    {
        [Required(ErrorMessage = "Debes indicar el rol.")]
        public string Rol { get; set; } = null!;
    }

    // DTO: respuesta al cambiar de rol (US-03).
    // El frontend usa "Mensaje" para el aviso tipo toast y "RolActivo" para resaltar el menú.
    public class CambioRolDTO
    {
        public string RolActivo { get; set; } = null!;
        public List<string> RolesDisponibles { get; set; } = new List<string>();
        public string Mensaje { get; set; } = null!;
    }
}
