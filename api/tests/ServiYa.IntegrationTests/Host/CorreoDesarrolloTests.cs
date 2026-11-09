using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ServiYa.Comun.Correo;
using ServiYa.Comun.Correo.Plantillas;
using ServiYa.IntegrationTests.Infraestructura;

namespace ServiYa.IntegrationTests.Host;

/// <summary>El correo simulado solo existe en desarrollo y pruebas (T051).</summary>
public sealed class CorreoDesarrolloTests
{
    [Fact]
    public async Task En_desarrollo_los_correos_se_leen_en_dev_correos()
    {
        await using var fabrica = FabricaSinBaseDeDatos.Crear("Development");
        using var cliente = fabrica.CreateClient();
        var correo = fabrica.Services.GetRequiredService<IEmailSender>();
        await correo.EnviarAsync(PlantillasCorreo.CodigoVerificacion("persona@correo.test", "123456"), Cancelacion);

        var recientes = await cliente.GetFromJsonAsync<List<CorreoGuardado>>(
            new Uri("/api/v1/dev/correos", UriKind.Relative),
            Cancelacion);

        recientes.Should().ContainSingle().Which.Texto.Should().Contain("123456");
    }

    [Fact]
    public async Task En_produccion_no_existe_dev_correos()
    {
        await using var fabrica = FabricaSinBaseDeDatos.Crear(
            "Production",
            FabricaSinBaseDeDatos.ConfiguracionCon(("Email:Proveedor", "Resend"), ("Email:ApiKey", "re_ficticia")));
        using var cliente = fabrica.CreateClient();

        using var respuesta = await cliente.GetAsync(new Uri("/api/v1/dev/correos", UriKind.Relative), Cancelacion);

        respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound);
        fabrica.Services.GetRequiredService<IEmailSender>().Should().BeOfType<ResendEmailSender>();
    }

    [Fact]
    public async Task En_produccion_no_arranca_con_el_correo_simulado()
    {
        await using var fabrica = FabricaSinBaseDeDatos.Crear("Production");

        var arrancar = () => fabrica.CreateClient();

        arrancar.Should().Throw<OptionsValidationException>()
            .Which.Message.Should().Contain(RegistroCorreo.MensajeConsolaFueraDeDesarrollo);
    }

    private static CancellationToken Cancelacion => TestContext.Current.CancellationToken;
}
