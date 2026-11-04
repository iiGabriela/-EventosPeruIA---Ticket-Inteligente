using EventosIAPeru.Core.Core.Services;
using EventosIAPeru.Core.Core.Settings;

namespace EventosIAPeru.Tests;

/// <summary>US-07 / US-08: el QR dinámico solo es válido si la firma es correcta y no expiró.</summary>
public class QrServiceTests
{
    private const string Clave = "clave-de-pruebas-con-mas-de-32-caracteres";

    private sealed class RelojFijo : TimeProvider
    {
        public DateTimeOffset Ahora { get; set; } = new(2026, 11, 20, 15, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Ahora;
    }

    private static QrService CrearServicio(RelojFijo reloj, string clave = Clave) =>
        new QrService(new QrSettings { ClaveFirma = clave, SegundosValidez = 24 }, reloj);

    [Fact]
    public void TokenRecienGenerado_EsValido_YDevuelveElCodigo()
    {
        var reloj = new RelojFijo();
        var servicio = CrearServicio(reloj);

        var (token, _) = servicio.GenerarToken("ABC123");
        var (codigo, error) = servicio.ValidarToken(token);

        Assert.Null(error);
        Assert.Equal("ABC123", codigo);
    }

    [Fact]
    public void TokenExpirado_EsRechazado()
    {
        var reloj = new RelojFijo();
        var servicio = CrearServicio(reloj);
        var (token, _) = servicio.GenerarToken("ABC123");

        // Una captura de pantalla usada 25 segundos después ya no sirve
        reloj.Ahora = reloj.Ahora.AddSeconds(25);
        var (codigo, error) = servicio.ValidarToken(token);

        Assert.Null(codigo);
        Assert.Contains("expiró", error);
    }

    [Fact]
    public void TokenAlterado_EsRechazado()
    {
        var reloj = new RelojFijo();
        var servicio = CrearServicio(reloj);
        var (token, _) = servicio.GenerarToken("ABC123");

        var alterado = "X" + token.Substring(1);
        var (codigo, error) = servicio.ValidarToken(alterado);

        Assert.Null(codigo);
        Assert.NotNull(error);
    }

    [Fact]
    public void TokenFirmadoConOtraClave_EsRechazado()
    {
        var reloj = new RelojFijo();
        var falsificador = CrearServicio(reloj, "otra-clave-distinta-de-mas-de-32-caracteres");
        var servicio = CrearServicio(reloj);

        var (tokenFalso, _) = falsificador.GenerarToken("ABC123");
        var (codigo, error) = servicio.ValidarToken(tokenFalso);

        Assert.Null(codigo);
        Assert.Contains("inválido", error);
    }

    [Fact]
    public void ClaveCorta_NoPermiteIniciarElServicio()
    {
        Assert.Throws<InvalidOperationException>(() =>
            new QrService(new QrSettings { ClaveFirma = "corta" }));
    }
}
