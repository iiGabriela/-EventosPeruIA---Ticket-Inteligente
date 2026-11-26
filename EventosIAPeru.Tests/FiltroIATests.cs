using EventosIAPeru.Core.Core.DTOs;
using EventosIAPeru.Core.Core.Entities;
using EventosIAPeru.Core.Core.Interfaces;
using EventosIAPeru.Core.Core.Services;
using Moq;

namespace EventosIAPeru.Tests;

/// <summary>US-13: filtro de reseñas maliciosas (reglas rápidas + IA).</summary>
public class FiltroResenasTests
{
    private readonly Mock<IResenaRepository> _resenas = new();
    private readonly Mock<IUsuarioRepository> _usuarios = new();
    private readonly Mock<IModeradorResenasIA> _ia = new();

    public FiltroResenasTests()
    {
        _usuarios.Setup(r => r.GetUsuarioByEmail("ana@correo.pe")).ReturnsAsync(new Usuario { UsuarioId = 3, Estado = "ACTIVO" });
        _resenas.Setup(r => r.ObtenerEvento(4)).ReturnsAsync(new Evento { EventoId = 4, Nombre = "Feria", FechaFin = DateTime.UtcNow.AddHours(-1) });
        _resenas.Setup(r => r.TieneEntradaValida(3, 4)).ReturnsAsync(true);
        _resenas.Setup(r => r.CrearResena(It.IsAny<ResenaEvento>())).ReturnsAsync(true);
        _ia.SetupGet(i => i.Disponible).Returns(true);
    }

    private Task<EventosIAPeru.Core.Shared.ResultadoOperacion> Enviar(string comentario) =>
        new ResenaService(_resenas.Object, _usuarios.Object, _ia.Object)
            .CrearResena("ana@correo.pe", new CrearResenaDTO { EventoId = 4, Calificacion = 4, Comentario = comentario });

    [Theory]
    [InlineData("Compra seguidores en www.ofertas.com")]
    [InlineData("Escríbeme al 987 654 321 y te vendo entradas")]
    [InlineData("Mi correo es vendo@estafa.pe")]
    public async Task Enlaces_y_datos_personales_se_bloquean_sin_consultar_a_la_ia(string comentario)
    {
        var resultado = await Enviar(comentario);

        Assert.False(resultado.Exito);
        _ia.Verify(i => i.Evaluar(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _resenas.Verify(r => r.CrearResena(It.IsAny<ResenaEvento>()), Times.Never);
    }

    [Fact]
    public async Task Resena_que_la_ia_marca_como_ofensiva_no_se_guarda()
    {
        _ia.Setup(i => i.Evaluar(It.IsAny<string>(), "Feria")).ReturnsAsync((true, "Evita los insultos."));

        var resultado = await Enviar("Los organizadores son unos idiotas");

        Assert.False(resultado.Exito);
        Assert.Contains("Evita los insultos.", resultado.Mensaje);
        _resenas.Verify(r => r.CrearResena(It.IsAny<ResenaEvento>()), Times.Never);
    }

    [Fact]
    public async Task Critica_negativa_respetuosa_se_publica()
    {
        _ia.Setup(i => i.Evaluar(It.IsAny<string>(), "Feria")).ReturnsAsync((false, ""));

        var resultado = await Enviar("La comida llegó fría y la fila fue muy larga.");

        Assert.True(resultado.Exito);
        _resenas.Verify(r => r.CrearResena(It.IsAny<ResenaEvento>()), Times.Once);
    }

    [Fact]
    public async Task Si_la_ia_falla_la_resena_igual_se_publica()
    {
        _ia.Setup(i => i.Evaluar(It.IsAny<string>(), It.IsAny<string>())).ThrowsAsync(new TimeoutException());

        var resultado = await Enviar("Muy buen evento");

        Assert.True(resultado.Exito);
    }
}

/// <summary>US-15: la regla de seguridad con IA se crea sola y revisa cada evento.</summary>
public class FiltroEventosTests
{
    [Fact]
    public async Task Al_evaluar_un_evento_se_asegura_la_regla_de_seguridad()
    {
        var repo = new Mock<IModeracionRepository>();
        repo.Setup(r => r.GetReglas()).ReturnsAsync(new List<ReglaModeracion>());

        await new ModeracionAutomaticaService(repo.Object).EvaluarEvento(10);

        repo.Verify(r => r.AsegurarReglaSistema(ModeracionAutomaticaService.NombreFiltroSeguridad, ModeracionAutomaticaService.CriterioFiltroSeguridad), Times.Once);
    }

    [Fact]
    public async Task Evento_malicioso_queda_en_revision_con_reporte_de_ia()
    {
        var repo = new Mock<IModeracionRepository>();
        var ia = new Mock<IEvaluadorReglasIA>();
        var reportes = new List<ReporteEvento>();
        var evento = new Evento { EventoId = 10, Nombre = "Fiesta", Descripcion = "Paga por Yape a este número y te doy la entrada", Estado = "PUBLICADO", EstadoModeracion = "CONFORME" };
        var regla = new ReglaModeracion { ReglaId = 7, Nombre = ModeracionAutomaticaService.NombreFiltroSeguridad, Tipo = "IA", Criterio = ModeracionAutomaticaService.CriterioFiltroSeguridad, Accion = "EN_REVISION", Activa = true };
        repo.Setup(r => r.GetReglas()).ReturnsAsync(new List<ReglaModeracion> { regla });
        repo.Setup(r => r.GetEventoParaModerar(10)).ReturnsAsync(evento);
        repo.Setup(r => r.AgregarReporte(It.IsAny<ReporteEvento>())).Callback<ReporteEvento>(reportes.Add);
        repo.Setup(r => r.GuardarCambios()).ReturnsAsync(true);
        ia.SetupGet(i => i.Disponible).Returns(true);
        ia.Setup(i => i.Evaluar(evento, regla.Criterio)).ReturnsAsync((true, "Pide pagos fuera de la plataforma."));

        var incumplidas = await new ModeracionAutomaticaService(repo.Object, ia.Object).EvaluarEvento(10);

        Assert.Equal(1, incumplidas);
        Assert.Equal("EN_REVISION", evento.EstadoModeracion);
        var reporte = Assert.Single(reportes);
        Assert.Equal("IA", reporte.Origen);
        Assert.Equal(7, reporte.ReglaId);
    }
}
