namespace EventosIAPeru.Core.Core.DTOs;

public class ResultadoValidacionDTO
{
    public bool Valida { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public string? TitularNombre { get; set; }
    public string? TipoAcceso { get; set; }
    public DateTime? FechaCheckIn { get; set; }
}
