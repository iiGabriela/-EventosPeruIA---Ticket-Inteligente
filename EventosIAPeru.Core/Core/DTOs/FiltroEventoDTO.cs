using System.ComponentModel.DataAnnotations;

namespace EventosIAPeru.Core.Core.DTOs
{
    public class FiltroEventoDTO
    {
        /// <summary>Palabra clave: busca en nombre y descripción.</summary>
        public string? Texto { get; set; }

        public int? CategoriaId { get; set; }

        /// <summary>Sede, distrito, provincia o departamento.</summary>
        public string? Ubicacion { get; set; }

        public DateTime? FechaDesde { get; set; }

        public DateTime? FechaHasta { get; set; }

        /// <summary>true = oculta los eventos agotados.</summary>
        public bool SoloDisponibles { get; set; }

        [Range(1, int.MaxValue)]
        public int Pagina { get; set; } = 1;

        [Range(1, 50)]
        public int TamanoPagina { get; set; } = 12;
    }
}
