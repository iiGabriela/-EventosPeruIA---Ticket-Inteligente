namespace EventosIAPeru.Core.Core.DTOs
{
    public class PanelVentasDTO
    {
        public string Periodo { get; set; } = null!;
        public DateTime? FechaDesde { get; set; }
        public DateTime? FechaHasta { get; set; }

        /// <summary>Ingresos del periodo consultado.</summary>
        public decimal IngresosTotales { get; set; }

        /// <summary>Entradas vendidas en el periodo consultado.</summary>
        public int EntradasVendidas { get; set; }

        /// <summary>Entradas vendidas en total (para la ocupación).</summary>
        public int EntradasVendidasTotal { get; set; }

        public int AforoTotal { get; set; }

        /// <summary>Porcentaje de ocupación: vendidas totales / aforo total.</summary>
        public decimal TasaOcupacion { get; set; }

        /// <summary>Eventos publicados y vigentes (con control de aforo activo).</summary>
        public int EventosConControlAforo { get; set; }

        public int TotalEventos { get; set; }

        public List<VentaEventoDTO> Eventos { get; set; } = new List<VentaEventoDTO>();
    }
}
