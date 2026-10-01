namespace EventosIAPeru.Core.Core.DTOs;

public class NotificacionDTO
{
    public int NotificacionId { get; set; }
    public int EventoId { get; set; }
    public string EventoNombre { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
    public string Prioridad { get; set; } = "INFORMATIVO";
    public string Icono { get; set; } = "info";
    public string Color { get; set; } = "#2563EB";
    public bool Leida { get; set; }
    public DateTime FechaCreacion { get; set; }
}
