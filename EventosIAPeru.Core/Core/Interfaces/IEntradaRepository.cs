using EventosIAPeru.Core.Core.Entities;

namespace EventosIAPeru.Core.Core.Interfaces;

public interface IEntradaRepository
{
    Task<Entrada?> GetEntradaPorCodigoQr(string codigoQr);
    // Incluye la compra y el check-in (si lo tiene)
    Task<Entrada?> GetEntradaPorId(int entradaId);
    // Incluye la compra y el evento, para mostrar el pase completo (US-07)
    Task<List<Entrada>> GetEntradasDeUsuario(int usuarioId);
    Task<bool> ExisteCodigoQr(string codigoQr);
    Task<bool> EsOrganizadorDelEvento(int eventoId, int usuarioId);
    // false si la entrada ya tenía check-in (escaneo duplicado simultáneo)
    Task<bool> RegistrarCheckIn(CheckIn checkIn);
}
