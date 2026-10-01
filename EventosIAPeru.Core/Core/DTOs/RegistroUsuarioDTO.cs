using System.ComponentModel.DataAnnotations;

namespace EventosIAPeru.Core.Core.DTOs
{
    // DTO: datos que manda el frontend al registrarse (US-01).
    // La contraseña NO viaja aquí: la maneja Firebase, nunca la guardamos nosotros.
    public class RegistroUsuarioDTO
    {
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(80, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 80 caracteres.")]
        public string Nombre { get; set; } = null!;

        [Required(ErrorMessage = "El correo es obligatorio.")]
        [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
        [StringLength(254, ErrorMessage = "El correo es demasiado largo.")]
        public string Email { get; set; } = null!;
    }
}
