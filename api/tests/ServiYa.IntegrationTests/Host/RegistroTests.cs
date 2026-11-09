using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ServiYa.IntegrationTests.Infraestructura;

namespace ServiYa.IntegrationTests.Host;

/// <summary>El registro de una petición no contiene datos personales (T055, principio I).</summary>
public sealed class RegistroTests
{
    private const string Correo = "persona.ficticia@correo.test";
    private const string Telefono = "5512345678";
    private const string Contrasena = "ContrasenaFicticia123";

    [Theory]
    [InlineData("Development")]
    [InlineData("Production")]
    public async Task Una_peticion_con_datos_personales_no_los_deja_en_el_registro(string entorno)
    {
        var registro = new RegistroEnMemoria();
        await using var fabrica = FabricaSinBaseDeDatos.Crear(
            entorno,
            FabricaSinBaseDeDatos.ConfiguracionCon(("Email:Proveedor", "Resend"), ("Email:ApiKey", "re_ficticia")),
            servicios => servicios.AddLogging(logging => logging.AddProvider(registro)));
        using var cliente = fabrica.CreateClient();

        using var respuesta = await cliente.PostAsJsonAsync(
            new Uri($"/api/v1/cuentas/login?email={Correo}&telefono={Telefono}", UriKind.Relative),
            new { email = Correo, password = Contrasena, telefono = Telefono },
            TestContext.Current.CancellationToken);

        // La petición sí quedó registrada (método, ruta y estado)...
        registro.Entradas.Should().Contain(entrada =>
            entrada.Contains("HttpLogging", StringComparison.Ordinal)
            && entrada.Contains("/api/v1/cuentas/login", StringComparison.Ordinal)
            && entrada.Contains("POST", StringComparison.Ordinal));
        // ...pero sin la consulta ni el cuerpo.
        registro.Entradas.Should().NotContain(entrada =>
            entrada.Contains(Correo, StringComparison.OrdinalIgnoreCase)
            || entrada.Contains(Telefono, StringComparison.Ordinal)
            || entrada.Contains(Contrasena, StringComparison.Ordinal));
    }
}
