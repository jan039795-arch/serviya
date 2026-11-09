using System.Net;
using Microsoft.Extensions.Time.Testing;
using ServiYa.Comun;
using ServiYa.Cuentas.Dominio;

namespace ServiYa.UnitTests.Cuentas;

/// <summary>Reglas del dominio de Cuentas (T056).</summary>
public sealed class DominioCuentasTests
{
    [Theory]
    [InlineData("2008-11-08", "2026-11-08", true)] // cumple 18 hoy
    [InlineData("2008-11-09", "2026-11-08", false)] // los cumple mañana
    [InlineData("1990-01-01", "2026-11-08", true)]
    [InlineData("2008-02-29", "2026-02-28", false)] // nacido en 29 de febrero: aún no
    [InlineData("2008-02-29", "2026-03-01", true)]
    public void Es_mayor_de_edad_desde_el_dia_que_cumple_18(string nacimiento, string hoy, bool esperado)
    {
        var usuario = new Usuario("persona@correo.test", "Rita Ficticia Luna", DateOnly.Parse(nacimiento, System.Globalization.CultureInfo.InvariantCulture));

        usuario.EsMayorDeEdad(DateOnly.Parse(hoy, System.Globalization.CultureInfo.InvariantCulture)).Should().Be(esperado);
    }

    [Fact]
    public void Un_usuario_nuevo_esta_activo_y_su_correo_es_su_nombre_de_usuario()
    {
        var usuario = new Usuario("persona@correo.test", "  Rita Ficticia Luna  ", new DateOnly(1990, 1, 1));

        usuario.Estado.Should().Be(EstadoUsuario.Activo);
        usuario.UserName.Should().Be("persona@correo.test");
        usuario.Email.Should().Be("persona@correo.test");
        usuario.EmailConfirmed.Should().BeFalse();
        usuario.NombreCompleto.Should().Be("Rita Ficticia Luna");
        usuario.EliminadoEn.Should().BeNull();
    }

    [Theory]
    [InlineData("Al")]
    [InlineData("   ")]
    public void El_nombre_debe_tener_al_menos_3_caracteres(string nombre)
    {
        var crear = () => new Usuario("persona@correo.test", nombre, new DateOnly(1990, 1, 1));

        crear.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void El_nombre_no_puede_pasar_de_120_caracteres()
    {
        var crear = () => new Usuario("persona@correo.test", new string('a', 121), new DateOnly(1990, 1, 1));

        crear.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Hoy_en_Mexico_usa_la_hora_del_centro()
    {
        // 03:00 UTC del 9 de noviembre son las 21:00 del 8 de noviembre en la Ciudad de México.
        var reloj = new FakeTimeProvider(new DateTimeOffset(2026, 11, 9, 3, 0, 0, TimeSpan.Zero));

        reloj.HoyEnMexico().Should().Be(new DateOnly(2026, 11, 8));
    }

    [Fact]
    public void Nadie_puede_bloquearse_a_si_mismo()
    {
        var usuario = Guid.CreateVersion7();

        var bloquear = () => new Bloqueo(usuario, usuario);

        bloquear.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Un_consentimiento_guarda_documento_version_fecha_e_ip()
    {
        var usuario = Guid.CreateVersion7();
        var fecha = new DateTimeOffset(2026, 11, 8, 12, 0, 0, TimeSpan.Zero);

        var consentimiento = new Consentimiento(usuario, DocumentoLegal.AvisoPrivacidad, "2026-11", fecha, IPAddress.Loopback);

        consentimiento.UsuarioId.Should().Be(usuario);
        consentimiento.Documento.Should().Be(DocumentoLegal.AvisoPrivacidad);
        consentimiento.Version.Should().Be("2026-11");
        consentimiento.AceptadoEn.Should().Be(fecha);
        consentimiento.Ip.Should().Be(IPAddress.Loopback);
    }
}
