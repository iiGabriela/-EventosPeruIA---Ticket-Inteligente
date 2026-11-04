using EventosIAPeru.Core.Core.DTOs;
using EventosIAPeru.Core.Core.Entities;
using EventosIAPeru.Core.Core.Interfaces;
using EventosIAPeru.Core.Shared;

namespace EventosIAPeru.Core.Core.Services;

/// <summary>US-07 (entrada digital con QR dinámico) y US-08 (check-in).</summary>
public class EntradaService : IEntradaService
{
    private readonly IEntradaRepository _entradaRepository;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IQrService _qrService;

    public EntradaService(IEntradaRepository entradaRepository, IUsuarioRepository usuarioRepository, IQrService qrService)
    {
        _entradaRepository = entradaRepository;
        _usuarioRepository = usuarioRepository;
        _qrService = qrService;
    }

    public async Task<List<EntradaDTO>?> GetMisEntradas(string emailUsuario)
    {
        var usuario = await _usuarioRepository.GetUsuarioByEmail(emailUsuario);
        if (usuario == null) return null;

        var entradas = await _entradaRepository.GetEntradasDeUsuario(usuario.UsuarioId);
        return entradas.Select(MapearEntrada).ToList();
    }

    // ---------------------------- US-07 ----------------------------

    public async Task<(ResultadoOperacion Resultado, QrDinamicoDTO? Qr)> GenerarQrDinamico(string emailUsuario, int entradaId)
    {
        var usuario = await _usuarioRepository.GetUsuarioByEmail(emailUsuario);
        if (usuario == null)
            return (ResultadoOperacion.NoAutorizado("Tu usuario no está registrado."), null);

        var entrada = await _entradaRepository.GetEntradaPorId(entradaId);
        if (entrada == null)
            return (ResultadoOperacion.NoEncontrado("La entrada no existe."), null);

        // Solo el comprador o el asistente asignado pueden mostrar el QR
        if (entrada.Compra.UsuarioId != usuario.UsuarioId && entrada.AsistenteUsuarioId != usuario.UsuarioId)
            return (ResultadoOperacion.NoAutorizado("Esta entrada no te pertenece."), null);

        if (entrada.Estado != "VALIDA" || entrada.CheckIn != null)
            return (ResultadoOperacion.Invalido("Esta entrada ya fue utilizada o anulada."), null);

        var (token, expiraEn) = _qrService.GenerarToken(entrada.CodigoQr);
        var qr = new QrDinamicoDTO
        {
            EntradaId = entrada.EntradaId,
            Token = token,
            ExpiraEn = expiraEn,
            SegundosValidez = _qrService.SegundosValidez
        };
        return (ResultadoOperacion.Ok(entrada.EntradaId), qr);
    }

    // ---------------------------- US-08 ----------------------------

    public async Task<(ResultadoValidacionDTO? Resultado, ResultadoOperacion? Error)> ValidarEntrada(string emailUsuario, ValidarEntradaDTO dto)
    {
        var usuario = await _usuarioRepository.GetUsuarioByEmail(emailUsuario);
        if (usuario == null)
            return (null, ResultadoOperacion.NoAutorizado("Tu usuario no está registrado."));

        var esOrganizador = await _entradaRepository.EsOrganizadorDelEvento(dto.EventoId, usuario.UsuarioId);
        if (!esOrganizador)
            return (null, ResultadoOperacion.NoAutorizado("Solo el organizador del evento puede validar entradas."));

        Entrada? entrada;
        if (!string.IsNullOrWhiteSpace(dto.CodigoQr))
        {
            // Escaneo: la firma y la expiración se verifican en el servidor
            var (codigoQr, errorToken) = _qrService.ValidarToken(dto.CodigoQr);
            if (codigoQr == null)
                return (Rechazo(errorToken!), null);

            entrada = await _entradaRepository.GetEntradaPorCodigoQr(codigoQr);
        }
        else if (dto.EntradaId.HasValue && !string.IsNullOrWhiteSpace(dto.TitularDocumento))
        {
            // Ingreso manual: ID de la entrada + documento del titular
            entrada = await _entradaRepository.GetEntradaPorId(dto.EntradaId.Value);
            if (entrada != null && entrada.TitularDocumento != dto.TitularDocumento.Trim())
                return (Rechazo("El documento no coincide con el titular de la entrada."), null);
        }
        else
        {
            return (null, ResultadoOperacion.Invalido("Escanea el QR o ingresa el ID de la entrada y el documento del titular."));
        }

        if (entrada == null)
            return (Rechazo("Entrada no encontrada."), null);

        if (entrada.Compra.EventoId != dto.EventoId)
            return (Rechazo("Esta entrada es de otro evento."), null);

        if (entrada.Compra.Estado != "CONFIRMADA")
            return (Rechazo("La compra de esta entrada no está confirmada."), null);

        if (entrada.Estado == "ANULADA")
            return (Rechazo("Esta entrada fue anulada."), null);

        if (entrada.Estado == "UTILIZADA" || entrada.CheckIn != null)
            return (YaUtilizada(entrada, entrada.CheckIn?.FechaHora), null);

        var ahora = DateTime.UtcNow;
        var registrado = await _entradaRepository.RegistrarCheckIn(new CheckIn
        {
            EntradaId = entrada.EntradaId,
            ValidadoPorUsuarioId = usuario.UsuarioId,
            FechaHora = ahora
        });

        // Otro escaneo de la misma entrada se registró justo antes
        if (!registrado)
            return (YaUtilizada(entrada, null), null);

        return (new ResultadoValidacionDTO
        {
            Valida = true,
            Mensaje = "Entrada válida. Acceso permitido.",
            TitularNombre = entrada.TitularNombre,
            TipoAcceso = entrada.TipoAcceso,
            FechaCheckIn = ahora
        }, null);
    }

    // ---------------------------- Ayudantes ----------------------------

    private static ResultadoValidacionDTO Rechazo(string mensaje) =>
        new ResultadoValidacionDTO { Valida = false, Mensaje = mensaje };

    private static ResultadoValidacionDTO YaUtilizada(Entrada entrada, DateTime? fechaCheckIn) =>
        new ResultadoValidacionDTO
        {
            Valida = false,
            Mensaje = "Esta entrada ya fue utilizada.",
            TitularNombre = entrada.TitularNombre,
            TipoAcceso = entrada.TipoAcceso,
            FechaCheckIn = fechaCheckIn
        };

    private static EntradaDTO MapearEntrada(Entrada e)
    {
        return new EntradaDTO
        {
            EntradaId = e.EntradaId,
            CompraId = e.CompraId,
            EventoId = e.Compra.EventoId,
            EventoNombre = e.Compra.Evento?.Nombre ?? string.Empty,
            FechaInicio = e.Compra.Evento?.FechaInicio ?? default,
            Sede = e.Compra.Evento?.Sede ?? string.Empty,
            Direccion = e.Compra.Evento?.Direccion ?? string.Empty,
            MontoPagado = e.Compra.PrecioUnitario,
            TitularNombre = e.TitularNombre,
            TitularDocumento = e.TitularDocumento,
            TipoAcceso = e.TipoAcceso,
            Estado = e.Estado,
            FechaGeneracion = e.FechaGeneracion
        };
    }
}
