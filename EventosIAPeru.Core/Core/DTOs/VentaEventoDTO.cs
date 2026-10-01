namespace EventosIAPeru.Core.Core.DTOs
{
    public class VentaEventoDTO
    {
        public int EventoId { get; set; }
        public string Nombre { get; set; } = null!;
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public string Sede { get; set; } = null!;
        public decimal Precio { get; set; }
        public int AforoTotal { get; set; }

        /// <summary>Entradas vendidas dentro del periodo consultado.</summary>
        public int EntradasVendidas { get; set; }

        /// <summary>Entradas vendidas desde que se publicó (define la ocupación real).</summary>
        public int EntradasVendidasTotal { get; set; }

        public int CuposDisponibles { get; set; }

        /// <summary>Total recaudado dentro del periodo consultado.</summary>
        public decimal Recaudado { get; set; }

        public decimal PorcentajeOcupacion { get; set; }

        /// <summary>Estado del evento en BD: BORRADOR | PUBLICADO | CANCELADO.</summary>
        public string Estado { get; set; } = null!;

        /// <summary>ACTIVO | AGOTADO | FINALIZADO | CANCELADO | BORRADOR.</summary>
        public string EstadoVenta { get; set; } = null!;
    }
}
