using Moq;
using EventosIAPeru.Core.Core.DTOs;
using EventosIAPeru.Core.Core.Entities;
using EventosIAPeru.Core.Core.Interfaces;
using EventosIAPeru.Core.Core.Services;
using EventosIAPeru.Core.Shared;

namespace EventosIAPeru.Tests;

/// <summary>US-15: solo un administrador puede nombrar a otro administrador, y queda en auditoría.</summary>
public class NombrarAdministradorTests
{
    private readonly Mock<IModeracionRepository> _moderacion = new();
    private readonly Mock<IUsuarioRepository> _usuarios = new();
    private readonly ModeracionService _servicio;

    private static readonly NombrarAdministradorDTO Valido = new() { Motivo = "Coordina la moderación", Confirmado = true };

    public NombrarAdministradorTests()
    {
        _usuarios.Setup(r => r.GetUsuarioByEmail("admin@test.com"))
                 .ReturnsAsync(new Usuario { UsuarioId = 1, Email = "admin@test.com", Estado = "ACTIVO", Nombre = "Admin" });
        _usuarios.Setup(r => r.TieneRol(1, "ADMINISTRADOR")).ReturnsAsync(true);

        _usuarios.Setup(r => r.GetUsuarioByEmail("org@test.com"))
                 .ReturnsAsync(new Usuario { UsuarioId = 2, Email = "org@test.com", Estado = "ACTIVO", Nombre = "Org" });
        _usuarios.Setup(r => r.TieneRol(2, "ADMINISTRADOR")).ReturnsAsync(false);

        _moderacion.Setup(r => r.GetUsuarioParaModerar(2))
                   .ReturnsAsync(new Usuario { UsuarioId = 2, Email = "org@test.com", Estado = "ACTIVO", Nombre = "Org" });
        _moderacion.Setup(r => r.AsignarRol(2, "ADMINISTRADOR")).ReturnsAsync(true);
        _moderacion.Setup(r => r.GuardarCambios()).ReturnsAsync(true);

        _servicio = new ModeracionService(_moderacion.Object, _usuarios.Object);
    }

    [Fact]
    public async Task Admin_NombraAdministrador_YQuedaEnAuditoria()
    {
        var resultado = await _servicio.NombrarAdministrador("admin@test.com", 2, Valido);

        Assert.True(resultado.Exito);
        _moderacion.Verify(r => r.AsignarRol(2, "ADMINISTRADOR"), Times.Once);
        _moderacion.Verify(r => r.AgregarAuditoria(It.Is<Auditoria>(a =>
            a.ActorUsuarioId == 1 && a.UsuarioAfectadoId == 2 && a.Accion == "USUARIO_ADMINISTRADOR")), Times.Once);
    }

    [Fact]
    public async Task NoAdmin_NoPuedeNombrarAdministrador()
    {
        var resultado = await _servicio.NombrarAdministrador("org@test.com", 2, Valido);

        Assert.Equal(TipoResultado.NoAutorizado, resultado.Tipo);
        _moderacion.Verify(r => r.AsignarRol(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SinMotivo_EsInvalido()
    {
        var resultado = await _servicio.NombrarAdministrador("admin@test.com", 2, new NombrarAdministradorDTO { Confirmado = true });

        Assert.Equal(TipoResultado.Invalido, resultado.Tipo);
        _moderacion.Verify(r => r.AsignarRol(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CuentaBloqueada_NoPuedeSerAdministrador()
    {
        _moderacion.Setup(r => r.GetUsuarioParaModerar(3))
                   .ReturnsAsync(new Usuario { UsuarioId = 3, Email = "b@test.com", Estado = "BLOQUEADO", Nombre = "B" });

        var resultado = await _servicio.NombrarAdministrador("admin@test.com", 3, Valido);

        Assert.Equal(TipoResultado.Invalido, resultado.Tipo);
        _moderacion.Verify(r => r.AsignarRol(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task YaEsAdministrador_EsInvalido()
    {
        _moderacion.Setup(r => r.GetUsuarioParaModerar(1))
                   .ReturnsAsync(new Usuario { UsuarioId = 1, Email = "admin@test.com", Estado = "ACTIVO", Nombre = "Admin" });

        var resultado = await _servicio.NombrarAdministrador("admin@test.com", 1, Valido);

        Assert.Equal(TipoResultado.Invalido, resultado.Tipo);
        _moderacion.Verify(r => r.AsignarRol(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }
}
