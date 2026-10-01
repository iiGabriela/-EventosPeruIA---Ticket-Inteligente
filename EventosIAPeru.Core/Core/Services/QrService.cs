using System.Security.Cryptography;
using EventosIAPeru.Core.Core.Interfaces;

namespace EventosIAPeru.Core.Core.Services;

public class QrService : IQrService
{
    public string GenerarCodigoQr()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToHexString(bytes);
    }
}

