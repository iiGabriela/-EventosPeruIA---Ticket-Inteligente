using Moq;
using EventosIAPeru.Core.Core.DTOs;
using EventosIAPeru.Core.Core.Entities;
using EventosIAPeru.Core.Core.Interfaces;
using EventosIAPeru.Core.Core.Services;
using EventosIAPeru.Core.Shared;

namespace EventosIAPeru.Tests;

/// <summary>US-11 (indicadores del panel) y US-01 (registro con DNI único).</summary>
public class VentaYUsuarioServiceTests
{
    [Fact]
    public async Task PanelVentas_CalculaIndicadoresConsolidados()
    {
        var usuarioRepo = new Mock<IUsuarioRepository>();
        usuarioRepo.Setup(r => r.GetUsuarioByEmail("org@test.com"))
                   .ReturnsAsync(new Usuario { UsuarioId = 3, Email = "org@test.com", Estado = "ACTIVO", Nombre = "Org" });

        var ventaRepo = new Mock<IVentaRepository>();
        var futuro = DateTime.UtcNow.AddDays(10);
        ventaRepo.Setup(r => r.GetVentasPorEvento(3, null, null)).ReturnsAsync(new List<VentaEventoDTO>
        {
            new() { EventoId = 1, Nombre = "A", Sede = "S", Estado = "PUBLICADO", AforoTotal = 100, EntradasVendidas = 50, EntradasVendidasTotal = 50, Recaudado = 500, FechaInicio = futuro, FechaFin = futuro.AddHours(2) },
            new() { EventoId = 2, Nombre = "B", Sede = "S", Estado = "PUBLICADO", AforoTotal = 100, EntradasVendidas = 100, EntradasVendidasTotal = 100, Recaudado = 1000, FechaInicio = futuro, FechaFin = futuro.AddHours(2) }
        });

        var servicio = new VentaService(ventaRepo.Object, usuarioRepo.Object);
        var panel = await servicio.GetPanelVentas("org@test.com", new FiltroVentasDTO());

        Assert.NotNull(panel);
        Assert.Equal(1500, panel!.IngresosTotales);
        Assert.Equal(150, panel.EntradasVendidas);
        Assert.Equal(75, panel.TasaOcupacion);
        Assert.Equal("AGOTADO", panel.Eventos.Single(e => e.EventoId == 2).EstadoVenta);
    }

    [Fact]
    public async Task Registro_ConDniRepetido_EsInvalido()
    {
        var usuarioRepo = new Mock<IUsuarioRepository>();
        usuarioRepo.Setup(r => r.ExisteEmail(It.IsAny<string>())).ReturnsAsync(false);
        usuarioRepo.Setup(r => r.ExisteDni("12345678")).ReturnsAsync(true);

        var servicio = new UsuarioService(usuarioRepo.Object, new Mock<ICategoriaRepository>().Object);
        var resultado = await servicio.Registrar("nuevo@test.com", new RegistroUsuarioDTO { Nombre = "Nuevo", Dni = "12345678" });

        Assert.Equal(TipoResultado.Invalido, resultado.Tipo);
        usuarioRepo.Verify(r => r.CrearUsuario(It.IsAny<Usuario>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Registro_Valido_EmpiezaComoAsistente_ConCorreoDelToken()
    {
        var usuarioRepo = new Mock<IUsuarioRepository>();
        Usuario? creado = null;
        usuarioRepo.Setup(r => r.CrearUsuario(It.IsAny<Usuario>(), "ASISTENTE"))
                   .Callback<Usuario, string>((u, _) => creado = u)
                   .ReturnsAsync(true);

        var servicio = new UsuarioService(usuarioRepo.Object, new Mock<ICategoriaRepository>().Object);
        var resultado = await servicio.Registrar("  Nuevo@Test.com ", new RegistroUsuarioDTO { Nombre = "Nuevo", Dni = "87654321" });

        Assert.True(resultado.Exito);
        Assert.Equal("nuevo@test.com", creado!.Email);
        Assert.Equal("87654321", creado.Dni);
    }
}
