using Microsoft.Extensions.Configuration;
using ServiYa.Comun.Tareas;
using ServiYa.IntegrationTests.Infraestructura;

namespace ServiYa.IntegrationTests.Tareas;

/// <summary>Dos instancias de la API no corren la misma tarea a la vez (T052, research R7).</summary>
public sealed class CandadoPostgresTests(ServiYaFactory fabrica) : IClassFixture<ServiYaFactory>
{
    private static CancellationToken Cancelacion => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Mientras_una_instancia_tiene_la_tarea_la_otra_no_la_toma()
    {
        var instanciaA = Candado();
        var instanciaB = Candado();

        var tomadoPorA = await instanciaA.IntentarTomarAsync("vencer-accesos", Cancelacion);
        var intentoDeB = await instanciaB.IntentarTomarAsync("vencer-accesos", Cancelacion);

        tomadoPorA.Should().NotBeNull();
        intentoDeB.Should().BeNull();

        await tomadoPorA!.DisposeAsync();
        var despues = await instanciaB.IntentarTomarAsync("vencer-accesos", Cancelacion);
        despues.Should().NotBeNull("al soltarlo, la otra instancia ya puede tomarlo");
        await despues!.DisposeAsync();
    }

    [Fact]
    public async Task Tareas_distintas_no_se_bloquean_entre_si()
    {
        var instanciaA = Candado();
        var instanciaB = Candado();

        await using var accesos = await instanciaA.IntentarTomarAsync("vencer-accesos-2", Cancelacion);
        await using var creditos = await instanciaB.IntentarTomarAsync("creditos-48h", Cancelacion);

        accesos.Should().NotBeNull();
        creditos.Should().NotBeNull();
    }

    [Fact]
    public async Task Diez_intentos_simultaneos_solo_uno_obtiene_el_candado()
    {
        var intentos = await Task.WhenAll(Enumerable.Range(0, 10)
            .Select(_ => Candado().IntentarTomarAsync("calificaciones-14d", Cancelacion)));

        intentos.Count(t => t is not null).Should().Be(1);
        foreach (var tomado in intentos.Where(t => t is not null))
        {
            await tomado!.DisposeAsync();
        }
    }

    /// <summary>Cada candado es como otra instancia de la API: su propia conexión como <c>serviya_app</c>.</summary>
    private CandadoPostgres Candado() => new(new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Default"] = fabrica.ConexionApp })
        .Build());
}
