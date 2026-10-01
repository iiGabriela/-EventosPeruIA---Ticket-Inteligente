using EventosIAPeru.Core.Core.DTOs;
using EventosIAPeru.Core.Core.Entities;
using EventosIAPeru.Core.Core.Interfaces;
using EventosIAPeru.Core.Shared;

namespace EventosIAPeru.Core.Core.Services;

public class EntradaService : IEntradaService
{
    private readonly IEntradaRepository _entradaRepository;
    private readonly IUsuarioRepository _usuarioRepository;

    public EntradaService(IEntradaRepository entradaRepository, IUsuarioRepository usuarioRepository)
    {
        _entradaRepository = entradaRepository;
        _usuarioRepository = usuarioRepository;
    }

    public async Task<List<EntradaDTO>?> GetMisEntradas(string firebaseUid)
    {
        var usuario = await _usuarioRepository.GetUsuarioByFirebaseUid(firebaseUid);
        if (usuario == null) return null;

        var entradas = await _entradaRepository.GetEntradasDeUsuario(usuario.UsuarioId);

        return entradas.Select(e => new EntradaDTO
        {
            EntradaId = e.EntradaId,
            CodigoQr = e.CodigoQr,
            TitularNombre = e.TitularNombre,
            TitularDocumento = e.TitularDocumento,
            TipoAcceso = e.TipoAcceso,
            Estado = e.Estado,
            FechaGeneracion = e.FechaGeneracion
        }).ToList();
    }

    public async Task<(ResultadoValidacionDTO? Resultado, ResultadoOperacion? Error)> ValidarEntrada(string firebaseUid, ValidarEntradaDTO dto)
    {
        var usuario = await _usuarioRepository.GetUsuarioByFirebaseUid(firebaseUid);
        if (usuario == null)
            return (null, ResultadoOperacion.NoAutorizado("Tu usuario no está registrado."));

        if (string.IsNullOrWhiteSpace(dto.CodigoQr))
            return (null, ResultadoOperacion.Invalido("Debes enviar el código QR."));

        var esOrganizador = await _entradaRepository.EsOrganizadorDelEvento(dto.EventoId, usuario.UsuarioId);
        if (!esOrganizador)
            return (null, ResultadoOperacion.NoAutorizado("Solo el organizador del evento puede validar entradas."));

        var entrada = await _entradaRepository.GetEntradaPorCodigoQr(dto.CodigoQr.Trim());

        if (entrada == null)
            return (new ResultadoValidacionDTO { Valida = false, Mensaje = "Código QR no encontrado." }, null);

        if (entrada.Compra.EventoId != dto.EventoId)
            return (new ResultadoValidacionDTO { Valida = false, Mensaje = "Esta entrada es de otro evento." }, null);

        if (entrada.Compra.Estado != "CONFIRMADA")
            return (new ResultadoValidacionDTO { Valida = false, Mensaje = "La compra de esta entrada no está confirmada." }, null);

        if (entrada.Estado == "ANULADA")
            return (new ResultadoValidacionDTO { Valida = false, Mensaje = "Esta entrada fue anulada." }, null);

        if (entrada.Estado == "UTILIZADA" || entrada.CheckIn != null)
            return (new ResultadoValidacionDTO
            {
                Valida = false,
                Mensaje = "Esta entrada ya fue utilizada.",
                TitularNombre = entrada.TitularNombre,
                TipoAcceso = entrada.TipoAcceso,
                FechaCheckIn = entrada.CheckIn?.FechaHora
            }, null);

        var ahora = DateTime.UtcNow;

        await _entradaRepository.RegistrarCheckIn(new CheckIn
        {
            EntradaId = entrada.EntradaId,
            ValidadoPorUsuarioId = usuario.UsuarioId,
            FechaHora = ahora
        });

        entrada.Estado = "UTILIZADA";
        await _entradaRepository.MarcarEntradaUtilizada(entrada);

        return (new ResultadoValidacionDTO
        {
            Valida = true,
            Mensaje = "Entrada válida. Acceso permitido.",
            TitularNombre = entrada.TitularNombre,
            TipoAcceso = entrada.TipoAcceso,
            FechaCheckIn = ahora
        }, null);
    }
}
