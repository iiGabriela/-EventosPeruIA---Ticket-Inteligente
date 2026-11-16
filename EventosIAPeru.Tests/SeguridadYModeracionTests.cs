using EventosIAPeru.Core.Core.Entities;
using EventosIAPeru.Core.Core.Interfaces;
using EventosIAPeru.Core.Core.Services;
using Moq;

namespace EventosIAPeru.Tests;

/// <summary>Seguridad (US-02): la sesión se enlaza al perfil por UID de Firebase, no solo por correo.</summary>
public class IdentidadServiceTests
{
    private readonly Mock<IUsuarioRepository> _usuarios = new();

    private IdentidadService Servicio() => new(_usuarios.Object);

    [Fact]
    public async Task Uid_ya_enlazado_usa_el_perfil_de_la_base_aunque_el_token_traiga_otro_correo()
    {
        _usuarios.Setup(r => r.GetUsuarioByFirebaseUid("uid-1"))
                 .ReturnsAsync(new Usuario { UsuarioId = 1, Email = "real@test.com", FirebaseUid = "uid-1" });

        var resultado = await Servicio().Resolver("uid-1", "otro@test.com", emailVerificado: false);

        Assert.True(resultado.Aceptada);
        Assert.Equal("real@test.com", resultado.Email);
    }

    [Fact]
    public async Task Usuario_nuevo_sin_perfil_puede_continuar_para_registrarse()
    {
        var resultado = await Servicio().Resolver("uid-nuevo", "  Nuevo@Test.com ", emailVerificado: false);

        Assert.True(resultado.Aceptada);
        Assert.Equal("nuevo@test.com", resultado.Email);
    }

    [Fact]
    public async Task Perfil_sin_uid_no_se_entrega_a_un_correo_sin_verificar()
    {
        // Caso real: admin@eventia.pe existe en la base pero no en Firebase.
        // Quien cree esa cuenta en Firebase no debe heredar el rol de administrador.
        _usuarios.Setup(r => r.GetUsuarioByEmail("admin@eventia.pe"))
                 .ReturnsAsync(new Usuario { UsuarioId = 4, Email = "admin@eventia.pe", FirebaseUid = null });

        var resultado = await Servicio().Resolver("uid-atacante", "admin@eventia.pe", emailVerificado: false);

        Assert.False(resultado.Aceptada);
        Assert.Null(resultado.Email);
        _usuarios.Verify(r => r.VincularFirebaseUid(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Perfil_sin_uid_se_enlaza_cuando_el_correo_esta_verificado()
    {
        _usuarios.Setup(r => r.GetUsuarioByEmail("dueno@test.com"))
                 .ReturnsAsync(new Usuario { UsuarioId = 7, Email = "dueno@test.com", FirebaseUid = null });
        _usuarios.Setup(r => r.VincularFirebaseUid(7, "uid-dueno")).ReturnsAsync(true);

        var resultado = await Servicio().Resolver("uid-dueno", "dueno@test.com", emailVerificado: true);

        Assert.True(resultado.Aceptada);
        Assert.Equal("dueno@test.com", resultado.Email);
        _usuarios.Verify(r => r.VincularFirebaseUid(7, "uid-dueno"), Times.Once);
    }

    [Fact]
    public async Task Perfil_enlazado_a_otro_uid_se_rechaza_aunque_el_correo_este_verificado()
    {
        _usuarios.Setup(r => r.GetUsuarioByEmail("dueno@test.com"))
                 .ReturnsAsync(new Usuario { UsuarioId = 7, Email = "dueno@test.com", FirebaseUid = "uid-original" });

        var resultado = await Servicio().Resolver("uid-otro", "dueno@test.com", emailVerificado: true);

        Assert.False(resultado.Aceptada);
        Assert.Equal(IdentidadService.MensajeOtraCuenta, resultado.MensajeRechazo);
    }

    [Fact]
    public async Task Token_sin_correo_se_rechaza()
    {
        var resultado = await Servicio().Resolver("uid-x", null, emailVerificado: false);

        Assert.False(resultado.Aceptada);
    }
}

/// <summary>US-15: reglas automáticas de moderación (simples y de IA).</summary>
public class ModeracionAutomaticaTests
{
    private readonly Mock<IModeracionRepository> _repo = new();
    private readonly Mock<IEvaluadorReglasIA> _ia = new();
    private readonly List<ReporteEvento> _reportes = new();

    public ModeracionAutomaticaTests()
    {
        _repo.Setup(r => r.AgregarReporte(It.IsAny<ReporteEvento>())).Callback<ReporteEvento>(_reportes.Add);
        _repo.Setup(r => r.GuardarCambios()).ReturnsAsync(true);
        _ia.SetupGet(e => e.Disponible).Returns(true);
    }

    private static Evento EventoPublicado(int aforo = 100, decimal precio = 50) => new()
    {
        EventoId = 10, Nombre = "Concierto", Descripcion = "Rock", Sede = "Estadio", Direccion = "Av. 1",
        AforoTotal = aforo, Precio = precio, Estado = "PUBLICADO", EstadoModeracion = "CONFORME"
    };

    private static ReglaModeracion ReglaAforoMayorA(decimal valor, string accion = "EN_REVISION") => new()
    {
        ReglaId = 1, Nombre = "Aforo alto", Tipo = "SIMPLE", Criterio = "Aforo mayor al permitido", Accion = accion,
        Activa = true, Campo = "AFORO_TOTAL", Operador = "MAYOR", Valor = valor
    };

    [Theory]
    [InlineData("MAYOR", 5000, 5001, true)]
    [InlineData("MAYOR", 5000, 5000, false)]
    [InlineData("MAYOR_IGUAL", 5000, 5000, true)]
    [InlineData("MENOR", 10, 9, true)]
    [InlineData("MENOR_IGUAL", 10, 11, false)]
    [InlineData("IGUAL", 0, 0, true)]
    public void Regla_simple_compara_el_campo_con_el_valor(string operador, decimal valor, int aforo, bool esperado)
    {
        var regla = ReglaAforoMayorA(valor);
        regla.Operador = operador;

        Assert.Equal(esperado, ModeracionAutomaticaService.CumpleCondicion(EventoPublicado(aforo), regla));
    }

    [Fact]
    public async Task Regla_simple_incumplida_crea_reporte_del_sistema_y_pasa_el_evento_a_revision()
    {
        var evento = EventoPublicado(aforo: 8000);
        _repo.Setup(r => r.GetReglas()).ReturnsAsync(new List<ReglaModeracion> { ReglaAforoMayorA(5000) });
        _repo.Setup(r => r.GetEventoParaModerar(10)).ReturnsAsync(evento);

        var incumplidas = await new ModeracionAutomaticaService(_repo.Object).EvaluarEvento(10);

        Assert.Equal(1, incumplidas);
        Assert.Equal("EN_REVISION", evento.EstadoModeracion);
        var reporte = Assert.Single(_reportes);
        Assert.Equal("SISTEMA", reporte.Origen);
        Assert.Equal(1, reporte.ReglaId);
        Assert.Null(reporte.ReportadoPorUsuarioId);
    }

    [Fact]
    public async Task Evento_que_cumple_las_reglas_no_cambia()
    {
        var evento = EventoPublicado(aforo: 100);
        _repo.Setup(r => r.GetReglas()).ReturnsAsync(new List<ReglaModeracion> { ReglaAforoMayorA(5000) });
        _repo.Setup(r => r.GetEventoParaModerar(10)).ReturnsAsync(evento);

        var incumplidas = await new ModeracionAutomaticaService(_repo.Object).EvaluarEvento(10);

        Assert.Equal(0, incumplidas);
        Assert.Equal("CONFORME", evento.EstadoModeracion);
        Assert.Empty(_reportes);
    }

    [Fact]
    public async Task Regla_inactiva_no_se_evalua()
    {
        var regla = ReglaAforoMayorA(5000);
        regla.Activa = false;
        _repo.Setup(r => r.GetReglas()).ReturnsAsync(new List<ReglaModeracion> { regla });

        var incumplidas = await new ModeracionAutomaticaService(_repo.Object).EvaluarEvento(10);

        Assert.Equal(0, incumplidas);
        _repo.Verify(r => r.GetEventoParaModerar(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Regla_de_ia_incumplida_crea_reporte_con_origen_ia()
    {
        var evento = EventoPublicado();
        var regla = new ReglaModeracion { ReglaId = 2, Nombre = "Sin apuestas", Tipo = "IA", Criterio = "No se permiten apuestas", Accion = "DESACTIVADO", Activa = true };
        _repo.Setup(r => r.GetReglas()).ReturnsAsync(new List<ReglaModeracion> { regla });
        _repo.Setup(r => r.GetEventoParaModerar(10)).ReturnsAsync(evento);
        _ia.Setup(e => e.Evaluar(evento, "No se permiten apuestas")).ReturnsAsync((true, "El evento promueve apuestas."));

        var incumplidas = await new ModeracionAutomaticaService(_repo.Object, _ia.Object).EvaluarEvento(10);

        Assert.Equal(1, incumplidas);
        Assert.Equal("DESACTIVADO", evento.EstadoModeracion);
        Assert.Equal("IA", Assert.Single(_reportes).Origen);
        Assert.Contains("apuestas", evento.MotivoModeracion);
    }

    [Fact]
    public async Task Si_el_proveedor_de_ia_falla_el_evento_no_se_toca()
    {
        var evento = EventoPublicado();
        var regla = new ReglaModeracion { ReglaId = 2, Nombre = "Sin apuestas", Tipo = "IA", Criterio = "No se permiten apuestas", Accion = "DESACTIVADO", Activa = true };
        _repo.Setup(r => r.GetReglas()).ReturnsAsync(new List<ReglaModeracion> { regla });
        _repo.Setup(r => r.GetEventoParaModerar(10)).ReturnsAsync(evento);
        _ia.Setup(e => e.Evaluar(It.IsAny<Evento>(), It.IsAny<string>())).ThrowsAsync(new HttpRequestException("sin conexión"));

        var incumplidas = await new ModeracionAutomaticaService(_repo.Object, _ia.Object).EvaluarEvento(10);

        Assert.Equal(0, incumplidas);
        Assert.Equal("CONFORME", evento.EstadoModeracion);
        Assert.Empty(_reportes);
    }

    [Fact]
    public async Task No_duplica_el_reporte_si_el_anterior_sigue_pendiente()
    {
        var evento = EventoPublicado(aforo: 8000);
        _repo.Setup(r => r.GetReglas()).ReturnsAsync(new List<ReglaModeracion> { ReglaAforoMayorA(5000) });
        _repo.Setup(r => r.GetEventoParaModerar(10)).ReturnsAsync(evento);
        _repo.Setup(r => r.ExisteReportePendienteDeRegla(10, 1)).ReturnsAsync(true);

        var incumplidas = await new ModeracionAutomaticaService(_repo.Object).EvaluarEvento(10);

        Assert.Equal(0, incumplidas);
        Assert.Empty(_reportes);
    }
}
