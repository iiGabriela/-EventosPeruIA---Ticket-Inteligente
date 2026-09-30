using System.ComponentModel.DataAnnotations;

namespace EventosIAPeru.Core.Core.DTOs
{
    /// <summary>Mismos campos y validaciones que al crear, más el Id del evento.</summary>
    public class ActualizarEventoDTO : CrearEventoDTO
    {
        [Range(1, int.MaxValue)]
        public int EventoId { get; set; }
    }
}
