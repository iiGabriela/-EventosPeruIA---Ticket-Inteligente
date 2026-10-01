using EventosIAPeru.Core.Core.DTOs;
using EventosIAPeru.Core.Shared;

namespace EventosIAPeru.Core.Core.Interfaces;

public interface IEntradaService
{
    Task<List<EntradaDTO>?> GetMisEntradas(string firebaseUid);
    Task<(ResultadoValidacionDTO? Resultado, ResultadoOperacion? Error)> ValidarEntrada(string firebaseUid, ValidarEntradaDTO dto);
}
