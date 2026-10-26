using EventosIAPeru.Core.Core.DTOs;
using EventosIAPeru.Core.Shared;

namespace EventosIAPeru.Core.Core.Interfaces;

public interface ICompraService
{
    Task<ResultadoOperacion> CrearCompra(string emailUsuario, CrearCompraDTO dto);
    Task<(ResultadoOperacion Resultado, List<CompraDTO>? Compras)> GetMisCompras(string emailUsuario);
    Task<(ResultadoOperacion Resultado, CompraDTO? Compra)> GetCompraPorId(string emailUsuario, int compraId);
}
