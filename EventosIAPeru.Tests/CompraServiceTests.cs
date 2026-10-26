using Moq;
using EventosIAPeru.Core.Core.DTOs;
using EventosIAPeru.Core.Core.Entities;
using EventosIAPeru.Core.Core.Interfaces;
using EventosIAPeru.Core.Core.Services;
using EventosIAPeru.Core.Shared;

namespace EventosIAPeru.Tests;

/// <summary>US-06 / US-09: validaciones de compra y control de aforo.</summary>
public class CompraServiceTests
{
    private const string Email = "asistente@test.com";

    private readonly Mock<ICompraRepository> _compraRepo = new();
    private readonly Mock<IUsuarioRepository> _usuarioRepo = new();
    private readonly Mock<IQrService> _qr = new();
    private readonly CompraService _servicio;

    public CompraServiceTests()
    {
        _usuarioRepo.Setup(r => r.GetUsuarioByEmail(Email))
                    .ReturnsAsync(new Usuario { UsuarioId = 1, Email = Email, Estado = "ACTIVO", Nombre = "Ana" });
        _compraRepo.Setup(r => r.GetEventoParaCompra(7))
                   .ReturnsAsync(new Evento { EventoId = 7, Estado = "PUBLICADO", EstadoModeracion = "CONFORME", AforoTotal = 10, Precio = 25, FechaInicio = DateTime.UtcNow.AddDays(5) });
        _qr.Setup(q => q.GenerarCodigoQr()).Returns(() => Guid.NewGuid().ToString("N"));

        _servicio = new CompraService(_compraRepo.Object, _usuarioRepo.Object, _qr.Object);
    }

    private static CrearCompraDTO Compra(int cantidad) => new()
    {
        EventoId = 7,
        Cantidad = cantidad,
        MetodoPago = "yape",
        Asistentes = Enumerable.Range(1, cantidad)
                               .Select(i => new AsistenteEntradaDTO { TitularNombre = $"Titular {i}", TitularDocumento = $"1234567{i}" })
                               .ToList()
    };

    [Fact]
    public async Task MasDeCuatroEntradas_EsInvalido()
    {
        var resultado = await _servicio.CrearCompra(Email, Compra(5));
        Assert.Equal(TipoResultado.Invalido, resultado.Tipo);
    }

    [Fact]
    public async Task UsuarioBloqueado_NoPuedeComprar()
    {
        _usuarioRepo.Setup(r => r.GetUsuarioByEmail(Email))
                    .ReturnsAsync(new Usuario { UsuarioId = 1, Email = Email, Estado = "BLOQUEADO", Nombre = "Ana" });

        var resultado = await _servicio.CrearCompra(Email, Compra(1));

        Assert.Equal(TipoResultado.NoAutorizado, resultado.Tipo);
    }

    [Fact]
    public async Task SinCuposAlConfirmar_NoSeRegistraLaCompra()
    {
        // El repositorio devuelve null cuando, con la fila bloqueada, ya no hay cupos (otra compra ganó)
        _compraRepo.Setup(r => r.CrearCompraConEntradas(It.IsAny<Compra>(), 10)).ReturnsAsync((int?)null);

        var resultado = await _servicio.CrearCompra(Email, Compra(1));

        Assert.Equal(TipoResultado.Invalido, resultado.Tipo);
        Assert.Contains("entradas disponibles", resultado.Mensaje);
    }

    [Fact]
    public async Task CompraValida_GeneraUnaEntradaPorUnidad()
    {
        Compra? guardada = null;
        _compraRepo.Setup(r => r.CrearCompraConEntradas(It.IsAny<Compra>(), 10))
                   .Callback<Compra, int>((c, _) => guardada = c)
                   .ReturnsAsync(42);

        var resultado = await _servicio.CrearCompra(Email, Compra(3));

        Assert.True(resultado.Exito);
        Assert.Equal(42, resultado.Id);
        Assert.Equal(3, guardada!.Entrada.Count);
        Assert.Equal("YAPE", guardada.MetodoPago);
        Assert.Equal(25, guardada.PrecioUnitario);
        Assert.Equal(3, guardada.Entrada.Select(e => e.CodigoQr).Distinct().Count());
    }

    [Fact]
    public async Task EventoDesactivadoPorModeracion_NoSePuedeComprar()
    {
        _compraRepo.Setup(r => r.GetEventoParaCompra(7))
                   .ReturnsAsync(new Evento { EventoId = 7, Estado = "PUBLICADO", EstadoModeracion = "DESACTIVADO", AforoTotal = 10, FechaInicio = DateTime.UtcNow.AddDays(5) });

        var resultado = await _servicio.CrearCompra(Email, Compra(1));

        Assert.Equal(TipoResultado.Invalido, resultado.Tipo);
    }
}
