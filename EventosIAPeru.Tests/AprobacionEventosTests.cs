using Moq;
using EventosIAPeru.Core.Core.DTOs;
using EventosIAPeru.Core.Core.Entities;
using EventosIAPeru.Core.Core.Interfaces;
using EventosIAPeru.Core.Core.Services;

namespace EventosIAPeru.Tests;

/// <summary>US-15: el organizador solicita aprobación y el evento solo se publica cuando un admin lo aprueba.</summary>
public class AprobacionEventosTests
{
    [Fact]
    public async Task Solicitar_aprobacion_no_publica_directo_y_avisa_a_los_admins()
    {
        var eventos = new Mock<IEventoRepository>();
        var usuarios = new Mock<IUsuarioRepository>();
        var notificaciones = new Mock<INotificacionService>();
        var evento = new Evento { EventoId = 5, OrganizadorId = 10, Nombre = "Feria", Estado = "BORRADOR", EstadoModeracion = "EN_REVISION", FechaInicio = DateTime.UtcNow.AddDays(3) };
        usuarios.Setup(r => r.GetUsuarioByEmail("org@test.com")).ReturnsAsync(new Usuario { UsuarioId = 10, Estado = "ACTIVO" });
        eventos.Setup(r => r.GetEventoById(5)).ReturnsAsync(evento);
        eventos.Setup(r => r.SolicitarAprobacion(5)).ReturnsAsync(true);
        var servicio = new EventoService(eventos.Object, new Mock<ICategoriaRepository>().Object, usuarios.Object, notificaciones.Object, new Mock<INotificadorAforo>().Object);

        var resultado = await servicio.PublicarEvento("org@test.com", 5);

        Assert.True(resultado.Exito);
        eventos.Verify(r => r.SolicitarAprobacion(5), Times.Once);
        eventos.Verify(r => r.UpdateEstado(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        notificaciones.Verify(n => n.NotificarSolicitudAprobacion(evento), Times.Once);
    }

    [Theory]
    [InlineData("CONFORME", null)]
    [InlineData("DESACTIVADO", "Faltan datos del local")]
    public async Task La_decision_del_admin_se_notifica_al_organizador(string estado, string? motivo)
    {
        var moderacion = new Mock<IModeracionRepository>();
        var usuarios = new Mock<IUsuarioRepository>();
        var notificaciones = new Mock<INotificacionService>();
        var evento = new Evento { EventoId = 5, OrganizadorId = 10, Nombre = "Feria", Estado = "PUBLICADO", EstadoModeracion = "EN_REVISION" };
        usuarios.Setup(r => r.GetUsuarioByEmail("admin@test.com")).ReturnsAsync(new Usuario { UsuarioId = 1, Estado = "ACTIVO" });
        usuarios.Setup(r => r.TieneRol(1, "ADMINISTRADOR")).ReturnsAsync(true);
        moderacion.Setup(r => r.GetEventoParaModerar(5)).ReturnsAsync(evento);
        moderacion.Setup(r => r.GuardarCambios()).ReturnsAsync(true);
        var servicio = new ModeracionService(moderacion.Object, usuarios.Object, notificaciones.Object);

        var resultado = await servicio.ModerarEvento("admin@test.com", 5, new ModerarEventoDTO { EstadoModeracion = estado, Motivo = motivo, Confirmado = true });

        Assert.True(resultado.Exito);
        Assert.Equal(estado, evento.EstadoModeracion);
        notificaciones.Verify(n => n.NotificarDecisionModeracion(evento, estado, It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task No_se_puede_comprar_un_evento_pendiente_de_aprobacion()
    {
        var compras = new Mock<ICompraRepository>();
        var usuarios = new Mock<IUsuarioRepository>();
        usuarios.Setup(r => r.GetUsuarioByEmail("ana@test.com")).ReturnsAsync(new Usuario { UsuarioId = 1, Estado = "ACTIVO" });
        compras.Setup(r => r.GetEventoParaCompra(7)).ReturnsAsync(new Evento { EventoId = 7, Estado = "PUBLICADO", EstadoModeracion = "EN_REVISION", AforoTotal = 10, FechaInicio = DateTime.UtcNow.AddDays(5) });
        var servicio = new CompraService(compras.Object, usuarios.Object, new Mock<IQrService>().Object, new Mock<INotificadorAforo>().Object);

        var resultado = await servicio.CrearCompra("ana@test.com", new CrearCompraDTO
        {
            EventoId = 7, Cantidad = 1, MetodoPago = "YAPE",
            Asistentes = new() { new AsistenteEntradaDTO { TitularNombre = "Ana", TitularDocumento = "12345678" } }
        });

        Assert.False(resultado.Exito);
        compras.Verify(r => r.CrearCompraConEntradas(It.IsAny<Compra>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Las_resenas_de_la_portada_muestran_solo_el_nombre_de_pila()
    {
        var resenas = new Mock<IResenaRepository>();
        resenas.Setup(r => r.GetResenasRecientes(6)).ReturnsAsync(new List<ResenaEvento>
        {
            new() { ResenaId = 1, Calificacion = 5, Comentario = "Genial", Usuario = new Usuario { Nombre = "Lucía Ramos Torres" }, Evento = new Evento { Nombre = "Feria" } }
        });
        var servicio = new ResenaService(resenas.Object, new Mock<IUsuarioRepository>().Object);

        var lista = await servicio.GetResenasRecientes(6);

        Assert.Equal("Lucía R.", Assert.Single(lista).UsuarioNombre);
    }
}
