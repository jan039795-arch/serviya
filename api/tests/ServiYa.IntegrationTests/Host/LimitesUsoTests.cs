using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using ServiYa.Api.Seguridad;
using ServiYa.Comun.Errores;

namespace ServiYa.IntegrationTests.Host;

/// <summary>
/// Límites de uso (T053). Todavía no hay endpoints que los usen, así que se prueba la misma
/// configuración en un servidor mínimo con un endpoint de prueba.
/// </summary>
public sealed class LimitesUsoTests
{
    [Fact]
    public async Task Login_y_codigos_permiten_5_por_minuto_y_luego_responden_429_con_Retry_After()
    {
        await using var app = await ServidorConLimite(LimitesUso.LoginCodigos);
        using var cliente = app.GetTestClient();

        for (var intento = 1; intento <= 5; intento++)
        {
            using var permitida = await cliente.PostAsync(new Uri("/prueba", UriKind.Relative), null, Cancelacion);
            permitida.StatusCode.Should().Be(HttpStatusCode.NoContent, $"el intento {intento} está dentro del límite");
        }

        using var respuesta = await cliente.PostAsync(new Uri("/prueba", UriKind.Relative), null, Cancelacion);

        respuesta.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        int.Parse(respuesta.Headers.GetValues("Retry-After").Single(), System.Globalization.CultureInfo.InvariantCulture)
            .Should().BeInRange(1, 60);
        respuesta.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        var problema = await respuesta.Content.ReadFromJsonAsync<ProblemDetails>(Cancelacion);
        problema!.Title.Should().Be("Hiciste demasiados intentos. Espera un momento.");
        problema.Detail.Should().StartWith("Espera ");
    }

    [Fact]
    public async Task Mensajes_permiten_30_por_minuto()
    {
        await using var app = await ServidorConLimite(LimitesUso.Mensajes);
        using var cliente = app.GetTestClient();

        var estados = new List<HttpStatusCode>();
        for (var intento = 1; intento <= 31; intento++)
        {
            using var respuesta = await cliente.PostAsync(new Uri("/prueba", UriKind.Relative), null, Cancelacion);
            estados.Add(respuesta.StatusCode);
        }

        estados.Take(30).Should().AllBeEquivalentTo(HttpStatusCode.NoContent);
        estados[^1].Should().Be(HttpStatusCode.TooManyRequests);
    }

    private static async Task<WebApplication> ServidorConLimite(string politica)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        // El appsettings.json de la API llega a la carpeta de pruebas y solo admite api.serviya.mx.
        builder.Configuration["AllowedHosts"] = "*";
        builder.Services.AddProblemDetails(opciones => opciones.CustomizeProblemDetails = ProblemasEnEspanol.Traducir);
        builder.Services.AgregarLimitesUso();
        var app = builder.Build();
        app.UseRateLimiter();
        app.MapPost("/prueba", () => Results.NoContent()).RequireRateLimiting(politica);
        await app.StartAsync(Cancelacion);
        return app;
    }

    private static CancellationToken Cancelacion => TestContext.Current.CancellationToken;
}
