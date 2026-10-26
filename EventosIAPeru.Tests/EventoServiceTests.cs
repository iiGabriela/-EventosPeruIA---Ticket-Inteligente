using Moq;
using EventosIAPeru.Core.Core.DTOs;
using EventosIAPeru.Core.Core.Entities;
using EventosIAPeru.Core.Core.Interfaces;
using EventosIAPeru.Core.Core.Services;
using EventosIAPeru.Core.Shared;

namespace EventosIAPeru.Tests;

/// <summary>US-04: reglas de creación y edición de eventos.</summary>
public class EventoServiceTests
{
    private const string Email = "org@test.com";

    private readonly Mock<IEventoRepository> _eventoRepo = new();
    private readonly Mock<ICategoriaRepository> _categoriaRepo = new();
    private readonly Mock<IUsuarioRepository> _usuarioRepo = new();
    private readonly Mock<INotificacionService> _notificaciones = new();
    private readonly EventoService _servicio;

    public EventoServiceTests()
    {
        _usuarioRepo.Setup(r => r.GetUsuarioByEmail(Email))
                    .ReturnsAsync(new Usuario { UsuarioId = 10, Email = Email, Estado = "ACTIVO", Nombre = "Org" });
        _usuarioRepo.Setup(r => r.TieneRol(10, "ORGANIZADOR")).ReturnsAsync(true);
        _categoriaRepo.Setup(r => r.GetCategoriaById(1)).ReturnsAsync(new CategoriaEvento { CategoriaId = 1 });

        _servicio = new EventoService(_eventoRepo.Object, _categoriaRepo.Object, _usuarioRepo.Object, _notificaciones.Object);
    }

    private static ActualizarEventoDTO EventoValido(int eventoId = 0, int aforo = 100) => new()
    {
        EventoId = eventoId,
        CategoriaId = 1,
        Nombre = "Concierto",
        Descripcion = "Descripción",
        FechaInicio = DateTime.UtcNow.AddDays(10),
        FechaFin = DateTime.UtcNow.AddDays(10).AddHours(3),
        Sede = "Sede",
        Direccion = "Av. 1",
        Departamento = "Lima",
        Provincia = "Lima",
        Distrito = "Miraflores",
        AforoTotal = aforo,
        Precio = 50
    };

    [Fact]
    public async Task CrearEvento_ConFechaPasada_EsInvalido()
    {
        var dto = EventoValido();
        dto.FechaInicio = DateTime.UtcNow.AddDays(-1);
        dto.FechaFin = DateTime.UtcNow.AddDays(-1).AddHours(2);

        var resultado = await _servicio.CrearEvento(Email, dto);

        Assert.Equal(TipoResultado.Invalido, resultado.Tipo);
        _eventoRepo.Verify(r => r.CreateEvento(It.IsAny<Evento>()), Times.Never);
    }

    [Fact]
    public async Task CrearEvento_SinRolOrganizador_NoEstaAutorizado()
    {
        _usuarioRepo.Setup(r => r.TieneRol(10, "ORGANIZADOR")).ReturnsAsync(false);

        var resultado = await _servicio.CrearEvento(Email, EventoValido());

        Assert.Equal(TipoResultado.NoAutorizado, resultado.Tipo);
    }

    [Fact]
    public async Task CrearEvento_Valido_QuedaEnBorrador()
    {
        Evento? guardado = null;
        _eventoRepo.Setup(r => r.CreateEvento(It.IsAny<Evento>()))
                   .Callback<Evento>(e => guardado = e)
                   .ReturnsAsync(true);

        var resultado = await _servicio.CrearEvento(Email, EventoValido());

        Assert.True(resultado.Exito);
        Assert.Equal("BORRADOR", guardado!.Estado);
        Assert.Equal(10, guardado.OrganizadorId);
    }

    [Fact]
    public async Task ActualizarEvento_DeOtroOrganizador_NoEstaAutorizado()
    {
        _eventoRepo.Setup(r => r.GetEventoById(5))
                   .ReturnsAsync(new Evento { EventoId = 5, OrganizadorId = 99, Estado = "PUBLICADO", EstadoModeracion = "CONFORME" });

        var resultado = await _servicio.ActualizarEvento(Email, EventoValido(5));

        Assert.Equal(TipoResultado.NoAutorizado, resultado.Tipo);
        _eventoRepo.Verify(r => r.UpdateEvento(It.IsAny<Evento>()), Times.Never);
    }

    [Fact]
    public async Task ActualizarEvento_AforoMenorALasVendidas_EsInvalido()
    {
        var fecha = DateTime.UtcNow.AddDays(10);
        _eventoRepo.Setup(r => r.GetEventoById(5))
                   .ReturnsAsync(new Evento { EventoId = 5, OrganizadorId = 10, Estado = "PUBLICADO", EstadoModeracion = "CONFORME", FechaInicio = fecha });
        _eventoRepo.Setup(r => r.GetEntradasVendidas(It.IsAny<IEnumerable<int>>()))
                   .ReturnsAsync(new Dictionary<int, int> { [5] = 30 });

        var resultado = await _servicio.ActualizarEvento(Email, EventoValido(5, aforo: 20));

        Assert.Equal(TipoResultado.Invalido, resultado.Tipo);
        Assert.Contains("30", resultado.Mensaje);
    }

    [Fact]
    public async Task ActualizarEvento_Valido_InvalidaEmbeddingsDelRag()
    {
        _eventoRepo.Setup(r => r.GetEventoById(5))
                   .ReturnsAsync(new Evento { EventoId = 5, OrganizadorId = 10, Estado = "PUBLICADO", EstadoModeracion = "CONFORME", FechaInicio = DateTime.UtcNow.AddDays(10) });
        _eventoRepo.Setup(r => r.GetEntradasVendidas(It.IsAny<IEnumerable<int>>()))
                   .ReturnsAsync(new Dictionary<int, int>());
        _eventoRepo.Setup(r => r.UpdateEvento(It.IsAny<Evento>())).ReturnsAsync(true);

        var resultado = await _servicio.ActualizarEvento(Email, EventoValido(5));

        Assert.True(resultado.Exito);
        _eventoRepo.Verify(r => r.EliminarEmbeddings(5), Times.Once);
    }
}
