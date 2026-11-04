using EventosIAPeru.Core.Core.DTOs;
using EventosIAPeru.Core.Core.Entities;
using EventosIAPeru.Core.Core.Interfaces;
using EventosIAPeru.Core.Shared;

namespace EventosIAPeru.Core.Core.Services;

public class CompraService : ICompraService
{
    private readonly ICompraRepository _compraRepository;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IQrService _qrService;
    private readonly INotificadorAforo _notificadorAforo;

    public CompraService(ICompraRepository compraRepository, IUsuarioRepository usuarioRepository, IQrService qrService,
                         INotificadorAforo notificadorAforo)
    {
        _compraRepository = compraRepository;
        _usuarioRepository = usuarioRepository;
        _qrService = qrService;
        _notificadorAforo = notificadorAforo;
    }

    public async Task<ResultadoOperacion> CrearCompra(string emailUsuario, CrearCompraDTO dto)
    {
        var usuario = await _usuarioRepository.GetUsuarioByEmail(emailUsuario);
        if (usuario == null) return ResultadoOperacion.NoAutorizado("Usuario no encontrado.");
        if (usuario.Estado != "ACTIVO") return ResultadoOperacion.NoAutorizado("Tu cuenta no está activa.");

        if (dto.Cantidad < 1 || dto.Cantidad > 4)
            return ResultadoOperacion.Invalido("La cantidad debe estar entre 1 y 4.");

        if (dto.Asistentes.Count != dto.Cantidad)
            return ResultadoOperacion.Invalido("Debes enviar los datos de cada asistente.");

        // US-06: cada entrada necesita titular y documento
        if (dto.Asistentes.Any(a => string.IsNullOrWhiteSpace(a.TitularNombre) || string.IsNullOrWhiteSpace(a.TitularDocumento)))
            return ResultadoOperacion.Invalido("Cada entrada debe tener nombre y documento del titular.");

        var metodos = new[] { "TARJETA", "YAPE", "PLIN" };
        if (!metodos.Contains(dto.MetodoPago?.ToUpper()))
            return ResultadoOperacion.Invalido("Método de pago no válido.");

        if (dto.CodigoOperacion != Guid.Empty)
        {
            var existente = await _compraRepository.GetCompraPorCodigoOperacion(usuario.UsuarioId, dto.CodigoOperacion);
            if (existente != null) return ResultadoOperacion.Ok(existente.CompraId);
        }

        var evento = await _compraRepository.GetEventoParaCompra(dto.EventoId);
        if (evento == null) return ResultadoOperacion.NoEncontrado("Evento no encontrado.");

        if (evento.Estado != "PUBLICADO" && evento.Estado != "ACTIVO")
            return ResultadoOperacion.Invalido("El evento no está disponible para la venta.");

        if (evento.EstadoModeracion == "DESACTIVADO")
            return ResultadoOperacion.Invalido("El evento fue desactivado por moderación.");

        if (evento.FechaInicio <= DateTime.UtcNow)
            return ResultadoOperacion.Invalido("El evento ya comenzó.");

        var vendidas = await _compraRepository.GetEntradasVendidas(dto.EventoId);
        if (vendidas + dto.Cantidad > evento.AforoTotal)
            return ResultadoOperacion.Invalido("No hay suficientes entradas disponibles.");

        var compra = new Compra
        {
            UsuarioId = usuario.UsuarioId,
            EventoId = dto.EventoId,
            CodigoOperacion = dto.CodigoOperacion == Guid.Empty ? Guid.NewGuid() : dto.CodigoOperacion,
            Cantidad = dto.Cantidad,
            PrecioUnitario = evento.Precio,
            MetodoPago = dto.MetodoPago!.ToUpper(),
            Estado = "CONFIRMADA",
            FechaCompra = DateTime.UtcNow
        };

        foreach (var asistente in dto.Asistentes)
        {
            compra.Entrada.Add(new Entrada
            {
                CodigoQr = _qrService.GenerarCodigoQr(),
                TitularNombre = asistente.TitularNombre.Trim(),
                TitularDocumento = asistente.TitularDocumento.Trim(),
                TipoAcceso = string.IsNullOrWhiteSpace(asistente.TipoAcceso) ? "GENERAL" : asistente.TipoAcceso.ToUpper(),
                Estado = "VALIDA",
                FechaGeneracion = DateTime.UtcNow
            });
        }

        // El aforo se vuelve a validar con bloqueo dentro de la transacción (US-09)
        var compraId = await _compraRepository.CrearCompraConEntradas(compra, evento.AforoTotal);
        if (compraId == null)
            return ResultadoOperacion.Invalido("No hay suficientes entradas disponibles.");

        // US-09: la cartelera de los demás usuarios se actualiza sin recargar
        var vendidasAhora = await _compraRepository.GetEntradasVendidas(dto.EventoId);
        await _notificadorAforo.CuposActualizados(dto.EventoId, evento.AforoTotal, vendidasAhora);

        return ResultadoOperacion.Ok(compraId);
    }

    public async Task<(ResultadoOperacion Resultado, List<CompraDTO>? Compras)> GetMisCompras(string emailUsuario)
    {
        var usuario = await _usuarioRepository.GetUsuarioByEmail(emailUsuario);
        if (usuario == null) return (ResultadoOperacion.NoAutorizado("Usuario no encontrado."), null);

        var compras = await _compraRepository.GetComprasDeUsuario(usuario.UsuarioId);
        return (ResultadoOperacion.Ok(), compras.Select(MapearCompra).ToList());
    }

    public async Task<(ResultadoOperacion Resultado, CompraDTO? Compra)> GetCompraPorId(string emailUsuario, int compraId)
    {
        var usuario = await _usuarioRepository.GetUsuarioByEmail(emailUsuario);
        if (usuario == null) return (ResultadoOperacion.NoAutorizado("Usuario no encontrado."), null);

        var compra = await _compraRepository.GetCompraPorId(compraId);
        if (compra == null) return (ResultadoOperacion.NoEncontrado("Compra no encontrada."), null);

        if (compra.UsuarioId != usuario.UsuarioId)
            return (ResultadoOperacion.NoAutorizado("Esta compra no te pertenece."), null);

        return (ResultadoOperacion.Ok(compra.CompraId), MapearCompra(compra));
    }

    private static CompraDTO MapearCompra(Compra compra)
    {
        return new CompraDTO
        {
            CompraId = compra.CompraId,
            EventoId = compra.EventoId,
            EventoTitulo = compra.Evento?.Nombre ?? string.Empty,
            FechaInicio = compra.Evento?.FechaInicio ?? default,
            Cantidad = compra.Cantidad,
            PrecioUnitario = compra.PrecioUnitario,
            Total = compra.PrecioUnitario * compra.Cantidad,
            MetodoPago = compra.MetodoPago,
            Estado = compra.Estado,
            FechaCompra = compra.FechaCompra,
            // El QR no se incluye: se pide a GET /api/Entrada/{id}/qr (US-07)
            Entradas = compra.Entrada.Select(e => new EntradaDTO
            {
                EntradaId = e.EntradaId,
                CompraId = compra.CompraId,
                EventoId = compra.EventoId,
                EventoNombre = compra.Evento?.Nombre ?? string.Empty,
                FechaInicio = compra.Evento?.FechaInicio ?? default,
                Sede = compra.Evento?.Sede ?? string.Empty,
                Direccion = compra.Evento?.Direccion ?? string.Empty,
                MontoPagado = compra.PrecioUnitario,
                TitularNombre = e.TitularNombre,
                TitularDocumento = e.TitularDocumento,
                TipoAcceso = e.TipoAcceso,
                Estado = e.Estado,
                FechaGeneracion = e.FechaGeneracion
            }).ToList()
        };
    }
}