using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using ServiYa.IntegrationTests.Infraestructura;

namespace ServiYa.IntegrationTests.Host;

/// <summary>Errores como ProblemDetails (RFC 9457) con títulos en español (T044).</summary>
public sealed class ErroresTests
{
    [Theory]
    [InlineData("Development")]
    [InlineData("Production")]
    public async Task Una_ruta_inexistente_responde_404_como_ProblemDetails_en_espanol(string entorno)
    {
        await using var fabrica = FabricaSinBaseDeDatos.Crear(
            entorno,
            FabricaSinBaseDeDatos.ConfiguracionCon(("Email:Proveedor", "Resend"), ("Email:ApiKey", "re_ficticia")));
        using var cliente = fabrica.CreateClient();

        using var respuesta = await cliente.GetAsync(
            new Uri("/api/v1/no-existe", UriKind.Relative),
            TestContext.Current.CancellationToken);

        respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound);
        respuesta.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        var problema = await respuesta.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.Current.CancellationToken);
        problema!.Status.Should().Be(404);
        problema.Title.Should().Be("No encontramos lo que buscas.");
    }
}
