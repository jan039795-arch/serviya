using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using ServiYa.IntegrationTests.Infraestructura;

namespace ServiYa.IntegrationTests.Host;

/// <summary>Encabezados de seguridad, HSTS y CORS (T054).</summary>
public sealed class SeguridadHttpTests
{
    private static readonly Dictionary<string, string> Produccion = FabricaSinBaseDeDatos.ConfiguracionCon(
        ("Email:Proveedor", "Resend"),
        ("Email:ApiKey", "re_ficticia"),
        ("AllowedHosts", "*"));

    [Fact]
    public async Task Toda_respuesta_lleva_los_encabezados_de_seguridad()
    {
        await using var fabrica = FabricaSinBaseDeDatos.Crear("Production", Produccion);
        using var cliente = fabrica.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://api.serviya.test"),
        });

        using var respuesta = await cliente.GetAsync(new Uri("/api/v1/no-existe", UriKind.Relative), Cancelacion);

        respuesta.Headers.GetValues("X-Content-Type-Options").Should().Equal("nosniff");
        respuesta.Headers.GetValues("Referrer-Policy").Should().Equal("no-referrer");
        respuesta.Headers.GetValues("X-Frame-Options").Should().Equal("DENY");
        respuesta.Headers.GetValues("Strict-Transport-Security").Single()
            .Should().Contain("max-age=31536000").And.Contain("includeSubDomains");
    }

    [Fact]
    public async Task En_produccion_redirige_http_a_https()
    {
        await using var fabrica = FabricaSinBaseDeDatos.Crear(
            "Production",
            FabricaSinBaseDeDatos.ConfiguracionCon(
                ("Email:Proveedor", "Resend"),
                ("Email:ApiKey", "re_ficticia"),
                ("AllowedHosts", "*"),
                ("HTTPS_PORT", "443")));
        using var cliente = fabrica.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("http://api.serviya.test"),
            AllowAutoRedirect = false,
        });

        using var respuesta = await cliente.GetAsync(new Uri("/api/v1/no-existe", UriKind.Relative), Cancelacion);

        respuesta.StatusCode.Should().Be(HttpStatusCode.TemporaryRedirect);
        respuesta.Headers.Location!.Scheme.Should().Be("https");
    }

    [Theory]
    [InlineData("Production", "https://serviya.mx", true)]
    [InlineData("Production", "https://sitio-ajeno.test", false)]
    [InlineData("Production", "http://localhost:4200", false)]
    [InlineData("Development", "http://localhost:4200", true)]
    public async Task CORS_solo_permite_la_app_y_con_credenciales(string entorno, string origen, bool permitido)
    {
        await using var fabrica = FabricaSinBaseDeDatos.Crear(entorno, Produccion);
        using var cliente = fabrica.CreateClient();
        using var verificacion = new HttpRequestMessage(HttpMethod.Options, "/api/v1/cuentas/login");
        verificacion.Headers.Add("Origin", origen);
        verificacion.Headers.Add("Access-Control-Request-Method", "POST");
        verificacion.Headers.Add("Access-Control-Request-Headers", "content-type,x-xsrf-token");

        using var respuesta = await cliente.SendAsync(verificacion, Cancelacion);

        if (permitido)
        {
            respuesta.Headers.GetValues("Access-Control-Allow-Origin").Should().Equal(origen);
            respuesta.Headers.GetValues("Access-Control-Allow-Credentials").Should().Equal("true");
        }
        else
        {
            respuesta.Headers.Contains("Access-Control-Allow-Origin").Should().BeFalse();
        }
    }

    private static CancellationToken Cancelacion => TestContext.Current.CancellationToken;
}
