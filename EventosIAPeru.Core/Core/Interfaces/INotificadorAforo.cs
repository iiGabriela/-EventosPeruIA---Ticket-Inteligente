namespace EventosIAPeru.Core.Core.Interfaces;

/// <summary>
/// US-09: avisa a los clientes conectados que cambiaron los cupos de un evento,
/// para que la cartelera se actualice sin recargar la página.
/// La implementación (SignalR) vive en la API; los servicios solo conocen esta interfaz.
/// </summary>
public interface INotificadorAforo
{
    Task CuposActualizados(int eventoId, int aforoTotal, int entradasVendidas);
}
