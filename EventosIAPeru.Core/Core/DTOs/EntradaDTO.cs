namespace EventosIAPeru.Core.Core.DTOs;

/// <summary>
/// US-07: datos completos del pase. El código secreto de la entrada NO se envía:
/// el QR se pide aparte a GET /api/Entrada/{id}/qr y cambia cada pocos segundos.
/// </summary>
public class EntradaDTO
{
    public int EntradaId { get; set; }
    public int CompraId { get; set; }
    public int EventoId { get; set; }
    public string EventoNombre { get; set; } = string.Empty;
    public DateTime FechaInicio { get; set; }
    public string Sede { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public decimal MontoPagado { get; set; }
    public string TitularNombre { get; set; } = string.Empty;
    public string TitularDocumento { get; set; } = string.Empty;
    public string TipoAcceso { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaGeneracion { get; set; }
}
