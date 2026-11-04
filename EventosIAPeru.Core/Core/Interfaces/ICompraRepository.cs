using EventosIAPeru.Core.Core.Entities;

namespace EventosIAPeru.Core.Core.Interfaces;

public interface ICompraRepository
{
    Task<Evento?> GetEventoParaCompra(int eventoId);
    Task<int> GetEntradasVendidas(int eventoId);
    Task<Compra?> GetCompraPorCodigoOperacion(int usuarioId, Guid codigoOperacion);
    Task<Compra?> GetCompraPorId(int compraId);
    Task<List<Compra>> GetComprasDeUsuario(int usuarioId);
    // Devuelve null si al confirmar ya no quedan cupos suficientes
    Task<int?> CrearCompraConEntradas(Compra compra, int aforoTotal);
}
