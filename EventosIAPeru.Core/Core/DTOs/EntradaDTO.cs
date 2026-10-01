namespace EventosIAPeru.Core.Core.DTOs;

public class EntradaDTO
{
    public int EntradaId { get; set; }
    public string CodigoQr { get; set; } = string.Empty;
    public string TitularNombre { get; set; } = string.Empty;
    public string TitularDocumento { get; set; } = string.Empty;
    public string TipoAcceso { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaGeneracion { get; set; }
}
