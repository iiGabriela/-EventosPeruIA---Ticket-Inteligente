using EventosIAPeru.Core.Core.DTOs;
using EventosIAPeru.Core.Shared;

using EventosIAPeru.Core.Core.DTOs;
using EventosIAPeru.Core.Shared;

namespace EventosIAPeru.Core.Core.Interfaces;

public interface ICompraService
{
    Task<ResultadoOperacion> CrearCompra(string firebaseUid, CrearCompraDTO dto);
    Task<ResultadoOperacion> GetMisCompras(string firebaseUid);
    Task<ResultadoOperacion> GetCompraPorId(string firebaseUid, int compraId);
}

