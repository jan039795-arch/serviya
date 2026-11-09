using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Options;

namespace ServiYa.IntegrationTests.Arranque;

/// <summary>
/// La API valida su configuración al arrancar (T041). No usa base de datos, así que corre sin Docker.
/// </summary>
public sealed class ArranqueApiTests
{
    /// <summary>Valores ficticios; nunca credenciales reales.</summary>
    private static readonly Dictionary<string, string> ConfiguracionFicticia = new()
    {
        ["Storage:Endpoint"] = "http://localhost:9000",
        ["Storage:AccessKey"] = "clave-ficticia",
        ["Storage:SecretKey"] = "secreto-ficticio",
        ["Storage:UrlPublica"] = "http://localhost:9000/serviya-publico",
        ["MercadoPago:AccessToken"] = "TEST-ficticio",
        ["MercadoPago:WebhookSecret"] = "firma-ficticia",
        ["Email:Proveedor"] = "Consola",
        ["Email:Remitente"] = "no-responder@serviya.test",
        ["WebPush:PublicKey"] = "publica-ficticia",
        ["WebPush:PrivateKey"] = "privada-ficticia",
    };

    [Fact]
    public async Task Con_la_configuracion_completa_arranca_y_publica_OpenAPI_en_desarrollo()
    {
        await using var fabrica = Fabrica("Development", ConfiguracionFicticia);
        using var cliente = fabrica.CreateClient();

        using var respuesta = await cliente.GetAsync(
            new Uri("/openapi/v1.json", UriKind.Relative),
            TestContext.Current.CancellationToken);

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var documento = await respuesta.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        documento.Should().Contain("\"openapi\"");
    }

    [Fact]
    public async Task Sin_secretos_no_arranca_y_dice_que_clave_falta()
    {
        // "Testing" no carga user-secrets ni appsettings.Development.json.
        await using var fabrica = Fabrica("Testing", new Dictionary<string, string>());

        var arrancar = () => fabrica.CreateClient();

        // Una excepción por cada grupo de opciones inválido, todas juntas.
        var error = arrancar.Should().Throw<AggregateException>().Which;
        error.InnerExceptions.Should().AllBeOfType<OptionsValidationException>();
        error.Message.Should().Contain("StorageOptions").And.Contain("SecretKey")
            .And.Contain("MercadoPagoOptions").And.Contain("WebPushOptions");
    }

    [Fact]
    public async Task En_produccion_no_publica_OpenAPI()
    {
        var configuracion = new Dictionary<string, string>(ConfiguracionFicticia)
        {
            ["AllowedHosts"] = "localhost",
        };
        await using var fabrica = Fabrica("Production", configuracion);
        using var cliente = fabrica.CreateClient();

        using var respuesta = await cliente.GetAsync(
            new Uri("/openapi/v1.json", UriKind.Relative),
            TestContext.Current.CancellationToken);

        respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static WebApplicationFactory<Program> Fabrica(
        string entorno,
        Dictionary<string, string> configuracion) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(entorno);
            foreach (var (clave, valor) in configuracion)
            {
                builder.UseSetting(clave, valor);
            }
        });
}
