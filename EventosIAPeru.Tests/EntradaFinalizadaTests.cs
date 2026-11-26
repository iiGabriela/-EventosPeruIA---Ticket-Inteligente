using EventosIAPeru.Core.Core.Entities;
using EventosIAPeru.Core.Core.Interfaces;
using EventosIAPeru.Core.Core.Services;
using Moq;

namespace EventosIAPeru.Tests;

/// <summary>US-07: una entrada sin ingreso de un evento que ya terminó no genera QR y envía la fecha de fin.</summary>
public class EntradaFinalizadaTests
{
    private readonly Mock<IEntradaRepository> _entradas = new();
    private readonly Mock<IUsuarioRepository> _usuarios = new();
    private readonly Mock<IQrService> _qr = new();
    private readonly EntradaService _service;

    public EntradaFinalizadaTests()
    {
        _usuarios.Setup(r => r.GetUsuarioByEmail("ana@correo.pe"))
                 .ReturnsAsync(new Usuario { UsuarioId = 3, Email = "ana@correo.pe", Estado = "ACTIVO" });
        _qr.Setup(q => q.GenerarToken(It.IsAny<string>())).Returns(("token", DateTime.UtcNow.AddSeconds(24)));
        _service = new EntradaService(_entradas.Object, _usuarios.Object, _qr.Object);
    }

    private static Entrada EntradaDe(DateTime fechaFin) => new()
    {
        EntradaId = 9, Estado = "VALIDA", CodigoQr = "abc", TitularNombre = "Ana", TitularDocumento = "12345678", TipoAcceso = "GENERAL",
        Compra = new Compra { UsuarioId = 3, EventoId = 4, Estado = "CONFIRMADA",
                              Evento = new Evento { EventoId = 4, Nombre = "Feria", FechaInicio = fechaFin.AddHours(-2), FechaFin = fechaFin } }
    };

    [Fact]
    public async Task NoGeneraQrSiElEventoYaTermino()
    {
        _entradas.Setup(r => r.GetEntradaPorId(9)).ReturnsAsync(EntradaDe(DateTime.UtcNow.AddMinutes(-5)));

        var (resultado, qr) = await _service.GenerarQrDinamico("ana@correo.pe", 9);

        Assert.False(resultado.Exito);
        Assert.Null(qr);
    }

    [Fact]
    public async Task GeneraQrSiElEventoNoTermino()
    {
        _entradas.Setup(r => r.GetEntradaPorId(9)).ReturnsAsync(EntradaDe(DateTime.UtcNow.AddHours(3)));

        var (resultado, qr) = await _service.GenerarQrDinamico("ana@correo.pe", 9);

        Assert.True(resultado.Exito);
        Assert.NotNull(qr);
    }

    [Fact]
    public async Task MisEntradasIncluyeLaFechaDeFin()
    {
        var fin = DateTime.UtcNow.AddHours(3);
        _usuarios.Setup(r => r.GetUsuarioByEmail("ana@correo.pe"))
                 .ReturnsAsync(new Usuario { UsuarioId = 3, Email = "ana@correo.pe", Estado = "ACTIVO" });
        _entradas.Setup(r => r.GetEntradasDeUsuario(3)).ReturnsAsync(new List<Entrada> { EntradaDe(fin) });

        var entradas = await _service.GetMisEntradas("ana@correo.pe");

        Assert.Equal(fin, entradas!.Single().FechaFin);
    }
}
