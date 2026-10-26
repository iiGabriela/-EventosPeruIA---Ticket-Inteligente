using System.ComponentModel.DataAnnotations;

namespace EventosIAPeru.Core.Core.DTOs
{
    // DTO: datos que manda el frontend al registrarse (US-01).
    // La contraseña NO viaja aquí: la maneja Firebase, nunca la guardamos nosotros.
    // El correo tampoco: se toma del token, así nadie se registra con un correo ajeno.
    public class RegistroUsuarioDTO
    {
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(80, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 80 caracteres.")]
        public string Nombre { get; set; } = null!;

        [Required(ErrorMessage = "El DNI es obligatorio.")]
        [RegularExpression(@"^\d{8}$", ErrorMessage = "El DNI debe tener 8 dígitos.")]
        public string Dni { get; set; } = null!;
    }
}
