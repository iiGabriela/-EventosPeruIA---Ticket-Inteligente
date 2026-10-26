using EventosIAPeru.Core.Core.DTOs;
using EventosIAPeru.Core.Shared;

namespace EventosIAPeru.Core.Core.Interfaces;

public interface IEntradaService
{
    Task<List<EntradaDTO>?> GetMisEntradas(string emailUsuario);

    // US-07: QR firmado de una entrada propia, válido por pocos segundos
    Task<(ResultadoOperacion Resultado, QrDinamicoDTO? Qr)> GenerarQrDinamico(string emailUsuario, int entradaId);

    Task<(ResultadoValidacionDTO? Resultado, ResultadoOperacion? Error)> ValidarEntrada(string emailUsuario, ValidarEntradaDTO dto);
}
