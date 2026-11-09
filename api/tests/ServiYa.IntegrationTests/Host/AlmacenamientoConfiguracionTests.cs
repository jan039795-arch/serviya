using Microsoft.Extensions.Options;
using ServiYa.Comun.Almacenamiento;
using ServiYa.IntegrationTests.Infraestructura;

namespace ServiYa.IntegrationTests.Host;

/// <summary>Fuera de desarrollo y pruebas el almacenamiento solo acepta https (revisión de seguridad).</summary>
public sealed class AlmacenamientoConfiguracionTests
{
    [Theory]
    [InlineData("Storage:Endpoint", "http://almacen.serviya.test")]
    [InlineData("Storage:UrlPublica", "http://archivos.serviya.test")]
    public async Task En_produccion_no_arranca_con_almacenamiento_sin_https(string clave, string valor)
    {
        await using var fabrica = FabricaSinBaseDeDatos.Crear(
            "Production",
            FabricaSinBaseDeDatos.ConfiguracionCon(("Email:Proveedor", "Resend"), ("Email:ApiKey", "re_ficticia"), (clave, valor)));

        var arrancar = () => fabrica.CreateClient();

        arrancar.Should().Throw<OptionsValidationException>()
            .Which.Message.Should().Contain(RegistroAlmacenamiento.MensajeHttpsObligatorio);
    }

    [Fact]
    public async Task En_desarrollo_se_permite_el_almacenamiento_local_por_http()
    {
        await using var fabrica = FabricaSinBaseDeDatos.Crear(
            "Development",
            FabricaSinBaseDeDatos.ConfiguracionCon(("Storage:Endpoint", "http://localhost:9000")));

        var arrancar = () => fabrica.CreateClient();

        arrancar.Should().NotThrow();
    }
}
