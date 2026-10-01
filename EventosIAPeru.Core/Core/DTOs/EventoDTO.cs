namespace EventosIAPeru.Core.Core.DTOs
{
    public class EventoDTO
    {
        public int EventoId { get; set; }
        public int OrganizadorId { get; set; }
        public string? OrganizadorNombre { get; set; }
        public int CategoriaId { get; set; }
        public string? CategoriaNombre { get; set; }
        public string Nombre { get; set; } = null!;
        public string Descripcion { get; set; } = null!;
        public string? ImagenUrl { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public string Sede { get; set; } = null!;
        public string Direccion { get; set; } = null!;
        public string Departamento { get; set; } = null!;
        public string Provincia { get; set; } = null!;
        public string Distrito { get; set; } = null!;
        public int AforoTotal { get; set; }
        public int EntradasVendidas { get; set; }
        public int CuposDisponibles { get; set; }
        public decimal Precio { get; set; }
        public string Estado { get; set; } = null!;
        public string EstadoModeracion { get; set; } = null!;
        public bool Agotado { get; set; }
        public bool Finalizado { get; set; }
        public bool DisponibleParaCompra { get; set; }
    }
}
