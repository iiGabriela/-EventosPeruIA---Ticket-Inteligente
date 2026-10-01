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

    public CompraService(ICompraRepository compraRepository, IUsuarioRepository usuarioRepository, IQrService qrService)
    {
        _compraRepository = compraRepository;
        _usuarioRepository = usuarioRepository;
        _qrService = qrService;
    }

    public async Task<ResultadoOperacion> CrearCompra(string firebaseUid, CrearCompraDTO dto)
    {
        var usuario = await _usuarioRepository.GetUsuarioByFirebaseUid(firebaseUid);
        if (usuario == null) return ResultadoOperacion.NoAutorizado("Usuario no encontrado.");

        if (dto.Cantidad < 1 || dto.Cantidad > 4)
            return ResultadoOperacion.Invalido("La cantidad debe estar entre 1 y 4.");

        if (dto.Asistentes.Count != dto.Cantidad)
            return ResultadoOperacion.Invalido("Debes enviar los datos de cada asistente.");

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
                TitularNombre = asistente.TitularNombre,
                TitularDocumento = asistente.TitularDocumento,
                TipoAcceso = string.IsNullOrWhiteSpace(asistente.TipoAcceso) ? "GENERAL" : asistente.TipoAcceso.ToUpper(),
                Estado = "VALIDA",
                FechaGeneracion = DateTime.UtcNow
            });
        }

        var compraId = await _compraRepository.CrearCompraConEntradas(compra);
        return ResultadoOperacion.Ok(compraId);
    }

    public async Task<ResultadoOperacion> GetMisCompras(string firebaseUid)
    {
        var usuario = await _usuarioRepository.GetUsuarioByFirebaseUid(firebaseUid);
        if (usuario == null) return ResultadoOperacion.NoAutorizado("Usuario no encontrado.");

        var compras = await _compraRepository.GetComprasDeUsuario(usuario.UsuarioId);
        return ResultadoOperacion.Ok();
    }

    public async Task<ResultadoOperacion> GetCompraPorId(string firebaseUid, int compraId)
    {
        var usuario = await _usuarioRepository.GetUsuarioByFirebaseUid(firebaseUid);
        if (usuario == null) return ResultadoOperacion.NoAutorizado("Usuario no encontrado.");

        var compra = await _compraRepository.GetCompraPorId(compraId);
        if (compra == null) return ResultadoOperacion.NoEncontrado("Compra no encontrada.");

        if (compra.UsuarioId != usuario.UsuarioId)
            return ResultadoOperacion.NoAutorizado("Esta compra no te pertenece.");

        return ResultadoOperacion.Ok(compra.CompraId);
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
            Entradas = compra.Entrada.Select(e => new EntradaDTO
            {
                EntradaId = e.EntradaId,
                CodigoQr = e.CodigoQr,
                TitularNombre = e.TitularNombre,
                TitularDocumento = e.TitularDocumento,
                TipoAcceso = e.TipoAcceso,
                Estado = e.Estado,
                FechaGeneracion = e.FechaGeneracion
            }).ToList()
        };
    }
}