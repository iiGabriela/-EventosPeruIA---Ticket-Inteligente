namespace EventosIAPeru.Core.Core.Settings;

/// <summary>Configuración del QR dinámico (US-07). Se lee de appsettings -> "Qr".</summary>
public class QrSettings
{
    /// <summary>Clave secreta para firmar los tokens (HMAC-SHA256). Nunca se envía al cliente.</summary>
    public string ClaveFirma { get; set; } = string.Empty;

    /// <summary>Tiempo de vida de cada QR en pantalla antes de que el frontend pida uno nuevo.</summary>
    public int SegundosValidez { get; set; } = 24;
}
