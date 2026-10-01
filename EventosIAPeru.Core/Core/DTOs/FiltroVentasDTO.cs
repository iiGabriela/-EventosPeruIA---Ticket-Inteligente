using System.ComponentModel.DataAnnotations;

namespace EventosIAPeru.Core.Core.DTOs
{
    public class FiltroVentasDTO
    {
        /// <summary>
        /// TODO (histórico completo) | MES (este mes) | SEMANA (últimos 7 días).
        /// Si se envían FechaDesde o FechaHasta, se usa ese rango en su lugar.
        /// </summary>
        [RegularExpression("^(TODO|MES|SEMANA)$", ErrorMessage = "El periodo debe ser TODO, MES o SEMANA.")]
        public string Periodo { get; set; } = "TODO";

        public DateTime? FechaDesde { get; set; }

        public DateTime? FechaHasta { get; set; }
    }
}
