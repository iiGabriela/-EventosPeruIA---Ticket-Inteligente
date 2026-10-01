using EventosIAPeru.Core.Core.Entities;

namespace EventosIAPeru.Core.Core.Interfaces;

public interface IEntradaRepository
{
    Task<Entrada?> GetEntradaPorCodigoQr(string codigoQr);
    Task<List<Entrada>> GetEntradasDeUsuario(int usuarioId);
    Task<bool> ExisteCodigoQr(string codigoQr);
    Task<bool> EsOrganizadorDelEvento(int eventoId, int usuarioId);
    Task RegistrarCheckIn(CheckIn checkIn);
    Task MarcarEntradaUtilizada(Entrada entrada);
}
