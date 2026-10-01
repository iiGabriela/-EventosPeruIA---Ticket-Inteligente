using EventosIAPeru.Core.Core.DTOs;

namespace EventosIAPeru.Core.Core.Interfaces
{
    public interface IVentaService
    {
        /// <summary>Null si el usuario no está registrado.</summary>
        Task<PanelVentasDTO?> GetPanelVentas(string firebaseUid, FiltroVentasDTO filtro);

        /// <summary>Genera el Excel con los mismos datos del panel.</summary>
        byte[] GenerarExcel(PanelVentasDTO panel);

        /// <summary>Genera el PDF con los mismos datos del panel.</summary>
        byte[] GenerarPdf(PanelVentasDTO panel);
    }
}
