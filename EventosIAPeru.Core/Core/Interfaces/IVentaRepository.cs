using EventosIAPeru.Core.Core.DTOs;

namespace EventosIAPeru.Core.Core.Interfaces
{
    public interface IVentaRepository
    {
        /// <summary>Ventas agregadas por evento del organizador. Fechas en UTC; null = sin límite.</summary>
        Task<IEnumerable<VentaEventoDTO>> GetVentasPorEvento(int organizadorId, DateTime? desde, DateTime? hasta);
    }
}
