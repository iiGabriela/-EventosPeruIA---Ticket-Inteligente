namespace EventosIAPeru.Core.Core.DTOs;

/// <summary>US-07: contenido del QR que muestra la app. El frontend lo renueva antes de ExpiraEn.</summary>
public class QrDinamicoDTO
{
    public int EntradaId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiraEn { get; set; }
    public int SegundosValidez { get; set; }
    public bool ValidacionEnTiempoReal { get; set; } = true;
    public string Garantia { get; set; } = "Pase con firma criptográfica única e intransferible contra la reventa.";
}
