using System.Security.Cryptography;
using System.Text;
using EventosIAPeru.Core.Core.Interfaces;
using EventosIAPeru.Core.Core.Settings;

namespace EventosIAPeru.Core.Core.Services;

/// <summary>
/// US-07 / US-08: QR dinámico. El QR no lleva el código de la entrada "a la vista", sino un token
/// "{datos}.{firma}" donde datos = código|expiración y firma = HMAC-SHA256 con una clave del servidor.
/// Una captura de pantalla deja de servir a los pocos segundos y nadie puede fabricar un QR sin la clave.
/// </summary>
public class QrService : IQrService
{
    private readonly byte[] _clave;
    private readonly int _segundosValidez;
    private readonly TimeProvider _reloj;

    public QrService(QrSettings settings, TimeProvider? reloj = null)
    {
        if (string.IsNullOrWhiteSpace(settings.ClaveFirma) || settings.ClaveFirma.Length < 32)
            throw new InvalidOperationException("Configura Qr:ClaveFirma (mínimo 32 caracteres).");

        _clave = Encoding.UTF8.GetBytes(settings.ClaveFirma);
        _segundosValidez = settings.SegundosValidez > 0 ? settings.SegundosValidez : 24;
        // Reloj inyectable para poder probar la expiración en las pruebas unitarias
        _reloj = reloj ?? TimeProvider.System;
    }

    public int SegundosValidez => _segundosValidez;

    public string GenerarCodigoQr()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToHexString(bytes);
    }

    public (string Token, DateTime ExpiraEn) GenerarToken(string codigoQr)
    {
        var expiraEn = _reloj.GetUtcNow().UtcDateTime.AddSeconds(_segundosValidez);
        var datos = $"{codigoQr}|{new DateTimeOffset(expiraEn).ToUnixTimeSeconds()}";
        var datosBytes = Encoding.UTF8.GetBytes(datos);

        var token = $"{Base64Url(datosBytes)}.{Base64Url(Firmar(datosBytes))}";
        return (token, expiraEn);
    }

    public (string? CodigoQr, string? Error) ValidarToken(string token)
    {
        var partes = token.Trim().Split('.');
        if (partes.Length != 2) return (null, "Código QR no reconocido.");

        byte[] datosBytes, firma;
        try
        {
            datosBytes = DesdeBase64Url(partes[0]);
            firma = DesdeBase64Url(partes[1]);
        }
        catch (FormatException)
        {
            return (null, "Código QR no reconocido.");
        }

        // Comparación en tiempo constante para no filtrar información de la firma
        if (!CryptographicOperations.FixedTimeEquals(Firmar(datosBytes), firma))
            return (null, "Código QR inválido o alterado.");

        var datos = Encoding.UTF8.GetString(datosBytes).Split('|');
        if (datos.Length != 2 || !long.TryParse(datos[1], out var expiraUnix))
            return (null, "Código QR no reconocido.");

        if (_reloj.GetUtcNow().ToUnixTimeSeconds() > expiraUnix)
            return (null, "El código QR expiró. Pide al asistente que lo actualice en su app.");

        return (datos[0], null);
    }

    private byte[] Firmar(byte[] datos) => HMACSHA256.HashData(_clave, datos);

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] DesdeBase64Url(string texto)
    {
        var base64 = texto.Replace('-', '+').Replace('_', '/');
        base64 = base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '=');
        return Convert.FromBase64String(base64);
    }
}
