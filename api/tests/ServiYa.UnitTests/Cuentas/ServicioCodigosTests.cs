using Microsoft.Extensions.Time.Testing;
using ServiYa.Cuentas.Dominio;
using ServiYa.Cuentas.Servicios;

namespace ServiYa.UnitTests.Cuentas;

/// <summary>Códigos de un solo uso para verificar el correo y recuperar la contraseña (T060, research R2).</summary>
public sealed class ServicioCodigosTests
{
    private static readonly Guid Usuario = Guid.CreateVersion7();
    private readonly FakeTimeProvider reloj = new(new DateTimeOffset(2026, 11, 8, 12, 0, 0, TimeSpan.Zero));
    private readonly ServicioCodigos servicio;

    public ServicioCodigosTests() => servicio = new ServicioCodigos(reloj);

    [Fact]
    public void Crea_un_codigo_de_6_digitos_que_vence_en_10_minutos_y_guarda_solo_su_hash()
    {
        var (registro, codigo) = servicio.Crear(Usuario, PropositoCodigo.VerificarCorreo);

        codigo.Should().MatchRegex("^[0-9]{6}$");
        registro.UsuarioId.Should().Be(Usuario);
        registro.Proposito.Should().Be(PropositoCodigo.VerificarCorreo);
        registro.ExpiraEn.Should().Be(reloj.GetUtcNow().AddMinutes(10));
        registro.Intentos.Should().Be(0);
        registro.UsadoEn.Should().BeNull();
        registro.CodigoHash.Should().NotContain(codigo).And.HaveLength(64);
    }

    [Fact]
    public void Los_codigos_son_aleatorios()
    {
        var codigos = Enumerable.Range(0, 30).Select(_ => servicio.Crear(Usuario, PropositoCodigo.VerificarCorreo).Codigo);

        codigos.Distinct().Should().HaveCountGreaterThan(20);
    }

    [Fact]
    public void El_codigo_correcto_se_acepta_una_sola_vez()
    {
        var (registro, codigo) = servicio.Crear(Usuario, PropositoCodigo.VerificarCorreo);

        servicio.Verificar(registro, codigo).Should().Be(ResultadoVerificacion.Correcto);
        registro.UsadoEn.Should().Be(reloj.GetUtcNow());
        servicio.Verificar(registro, codigo).Should().Be(ResultadoVerificacion.YaUsado);
    }

    [Fact]
    public void Acepta_el_codigo_con_espacios_como_se_pega_desde_el_correo()
    {
        var (registro, codigo) = servicio.Crear(Usuario, PropositoCodigo.VerificarCorreo);

        servicio.Verificar(registro, $" {codigo[..3]} {codigo[3..]} ").Should().Be(ResultadoVerificacion.Correcto);
    }

    [Fact]
    public void Un_codigo_incorrecto_cuenta_como_intento()
    {
        var (registro, codigo) = servicio.Crear(Usuario, PropositoCodigo.VerificarCorreo);

        servicio.Verificar(registro, Otro(codigo)).Should().Be(ResultadoVerificacion.Incorrecto);
        registro.Intentos.Should().Be(1);
        registro.UsadoEn.Should().BeNull();
    }

    [Fact]
    public void Despues_de_10_minutos_vence_aunque_sea_correcto()
    {
        var (registro, codigo) = servicio.Crear(Usuario, PropositoCodigo.VerificarCorreo);

        reloj.Advance(TimeSpan.FromMinutes(10));

        servicio.Verificar(registro, codigo).Should().Be(ResultadoVerificacion.Vencido);
        registro.UsadoEn.Should().BeNull();
    }

    [Fact]
    public void Un_segundo_antes_de_vencer_todavia_sirve()
    {
        var (registro, codigo) = servicio.Crear(Usuario, PropositoCodigo.VerificarCorreo);

        reloj.Advance(TimeSpan.FromMinutes(10) - TimeSpan.FromSeconds(1));

        servicio.Verificar(registro, codigo).Should().Be(ResultadoVerificacion.Correcto);
    }

    [Fact]
    public void Al_quinto_intento_fallido_se_bloquea_y_ya_no_acepta_ni_el_correcto()
    {
        var (registro, codigo) = servicio.Crear(Usuario, PropositoCodigo.VerificarCorreo);

        for (var intento = 1; intento < ServicioCodigos.IntentosMaximos; intento++)
        {
            servicio.Verificar(registro, Otro(codigo)).Should().Be(ResultadoVerificacion.Incorrecto);
        }

        servicio.Verificar(registro, Otro(codigo)).Should().Be(ResultadoVerificacion.Agotado);
        servicio.Verificar(registro, codigo).Should().Be(ResultadoVerificacion.Agotado);
        registro.Intentos.Should().Be(ServicioCodigos.IntentosMaximos);
    }

    [Theory]
    [InlineData("")]
    [InlineData("12345")]
    [InlineData("1234567")]
    [InlineData("abcdef")]
    public void Un_formato_invalido_es_incorrecto(string ingresado)
    {
        var (registro, _) = servicio.Crear(Usuario, PropositoCodigo.VerificarCorreo);

        servicio.Verificar(registro, ingresado).Should().Be(ResultadoVerificacion.Incorrecto);
    }

    [Fact]
    public void El_codigo_solo_sirve_para_su_usuario_y_su_proposito()
    {
        var (verificacion, codigo) = servicio.Crear(Usuario, PropositoCodigo.VerificarCorreo);
        var (deOtroUsuario, _) = servicio.Crear(Guid.CreateVersion7(), PropositoCodigo.VerificarCorreo);
        var (deRecuperacion, _) = servicio.Crear(Usuario, PropositoCodigo.RecuperarContrasena);

        // El hash incluye usuario y propósito: el mismo número no coincide en otro registro.
        servicio.Verificar(deOtroUsuario, codigo).Should().NotBe(ResultadoVerificacion.Correcto);
        servicio.Verificar(deRecuperacion, codigo).Should().NotBe(ResultadoVerificacion.Correcto);
        servicio.Verificar(verificacion, codigo).Should().Be(ResultadoVerificacion.Correcto);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(2, true)]
    [InlineData(3, false)]
    [InlineData(10, false)]
    public void Permite_hasta_3_envios_por_hora(int enviadosEnLaUltimaHora, bool permitido)
    {
        ServicioCodigos.PuedeEnviar(enviadosEnLaUltimaHora).Should().Be(permitido);
    }

    [Fact]
    public void La_ventana_de_envios_es_la_ultima_hora()
    {
        servicio.InicioVentanaEnvios().Should().Be(reloj.GetUtcNow().AddHours(-1));
    }

    private static string Otro(string codigo) => codigo == "000000" ? "111111" : "000000";
}
