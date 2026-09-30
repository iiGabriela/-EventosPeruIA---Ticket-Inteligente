using System.ComponentModel.DataAnnotations;

namespace EventosIAPeru.Core.Core.DTOs
{
    public class CrearEventoDTO
    {
        [Range(1, int.MaxValue, ErrorMessage = "Seleccione una categoría.")]
        public int CategoriaId { get; set; }

        [Required(ErrorMessage = "El nombre del evento es obligatorio.")]
        [StringLength(150, ErrorMessage = "El nombre no puede superar los 150 caracteres.")]
        public string Nombre { get; set; } = null!;

        [Required(ErrorMessage = "La descripción es obligatoria.")]
        public string Descripcion { get; set; } = null!;

        [StringLength(500)]
        public string? ImagenUrl { get; set; }

        [Required(ErrorMessage = "La fecha y hora de inicio son obligatorias.")]
        public DateTime FechaInicio { get; set; }

        [Required(ErrorMessage = "La fecha y hora de fin son obligatorias.")]
        public DateTime FechaFin { get; set; }

        [Required(ErrorMessage = "El lugar/sede es obligatorio.")]
        [StringLength(150)]
        public string Sede { get; set; } = null!;

        [Required(ErrorMessage = "La dirección es obligatoria.")]
        [StringLength(300)]
        public string Direccion { get; set; } = null!;

        [Required(ErrorMessage = "El departamento es obligatorio.")]
        [StringLength(60)]
        public string Departamento { get; set; } = null!;

        [Required(ErrorMessage = "La provincia es obligatoria.")]
        [StringLength(60)]
        public string Provincia { get; set; } = null!;

        [Required(ErrorMessage = "El distrito es obligatorio.")]
        [StringLength(60)]
        public string Distrito { get; set; } = null!;

        [Range(1, int.MaxValue, ErrorMessage = "El aforo debe ser mayor a cero.")]
        public int AforoTotal { get; set; }

        [Range(typeof(decimal), "0", "99999999", ErrorMessage = "El precio no puede ser negativo.")]
        public decimal Precio { get; set; }
    }
}
