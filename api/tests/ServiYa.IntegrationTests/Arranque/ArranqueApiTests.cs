using System.Net;
using Microsoft.Extensions.Options;
using ServiYa.IntegrationTests.Infraestructura;

namespace ServiYa.IntegrationTests.Arranque;

/// <summary>La API valida su configuración al arrancar (T041).</summary>
public sealed class ArranqueApiTests
{
    [Fact]
    public async Task Con_la_configuracion_completa_arranca_y_publica_OpenAPI_en_desarrollo()
    {
        await using var fabrica = FabricaSinBaseDeDatos.Crear("Development");
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
        await using var fabrica = FabricaSinBaseDeDatos.Crear("Testing", new Dictionary<string, string>());

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
        await using var fabrica = FabricaSinBaseDeDatos.Crear(
            "Production",
            FabricaSinBaseDeDatos.ConfiguracionCon(("Email:Proveedor", "Resend"), ("Email:ApiKey", "re_ficticia")));
        using var cliente = fabrica.CreateClient();

        using var respuesta = await cliente.GetAsync(
            new Uri("/openapi/v1.json", UriKind.Relative),
            TestContext.Current.CancellationToken);

        respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
