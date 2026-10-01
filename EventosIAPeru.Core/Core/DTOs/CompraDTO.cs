namespace EventosIAPeru.Core.Core.DTOs;

public class CompraDTO
{
    public int CompraId { get; set; }
    public int EventoId { get; set; }
    public string EventoTitulo { get; set; } = string.Empty;
    public DateTime FechaInicio { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Total { get; set; }
    public string MetodoPago { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaCompra { get; set; }
    public List<EntradaDTO> Entradas { get; set; } = new();
}