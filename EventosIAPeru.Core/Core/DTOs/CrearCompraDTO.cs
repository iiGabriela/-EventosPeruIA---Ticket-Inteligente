namespace EventosIAPeru.Core.Core.DTOs;

public class CrearCompraDTO
{
    public int EventoId { get; set; }
    public int Cantidad { get; set; }
    public string MetodoPago { get; set; } = string.Empty;
    public Guid CodigoOperacion { get; set; }
    public List<AsistenteEntradaDTO> Asistentes { get; set; } = new();
}
