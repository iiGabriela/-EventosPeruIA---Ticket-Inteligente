namespace EventosIAPeru.Core.Core.Interfaces;

public interface IQrService
{
    // Código secreto y único de cada entrada (se guarda en la BD, no se muestra al usuario)
    string GenerarCodigoQr();

    // US-07: token firmado que va dentro del QR; vence a los pocos segundos
    (string Token, DateTime ExpiraEn) GenerarToken(string codigoQr);

    // US-08: verifica firma y expiración. Devuelve el código de la entrada o el motivo del rechazo
    (string? CodigoQr, string? Error) ValidarToken(string token);

    int SegundosValidez { get; }
}
