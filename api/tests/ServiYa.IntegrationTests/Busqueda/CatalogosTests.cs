using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using ServiYa.Busqueda.Servicios;
using ServiYa.Comun.Errores;
using ServiYa.IntegrationTests.Infraestructura;

namespace ServiYa.IntegrationTests.Busqueda;

/// <summary>Catálogos de oficios y municipios (T065–T067, R5).</summary>
public sealed class CatalogosTests(ServiYaFactory fabrica) : IClassFixture<ServiYaFactory>, IAsyncLifetime
{
    private static CancellationToken Cancelacion => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync() => await fabrica.ImportarAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task Los_12_oficios_semilla_se_listan_por_nombre_y_se_pueden_guardar_en_cache()
    {
        using var cliente = fabrica.CrearCliente();

        using var respuesta = await cliente.GetAsync(Ruta("catalogos/oficios"), Cancelacion);

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        respuesta.Headers.CacheControl!.Public.Should().BeTrue();
        var oficios = await respuesta.Content.ReadFromJsonAsync<OficioDto[]>(Cancelacion);
        oficios.Should().HaveCount(12);
        oficios!.Select(o => o.Nombre).Should().BeInAscendingOrder(StringComparer.Create(new System.Globalization.CultureInfo("es-MX"), false));
        oficios.Should().ContainEquivalentOf(new OficioDto(new Guid("a0000000-0000-7000-8000-000000000001"), "Albañil", "albanil", "ladrillo"));
        oficios.Select(o => o.Slug).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task Un_oficio_inactivo_no_se_lista()
    {
        await ComoDuenoAsync("UPDATE catalogos.oficios SET activo = false WHERE slug = 'cerrajero'");
        try
        {
            using var cliente = fabrica.CrearCliente();

            var oficios = await cliente.GetFromJsonAsync<OficioDto[]>(Ruta("catalogos/oficios"), Cancelacion);

            oficios!.Select(o => o.Slug).Should().NotContain("cerrajero").And.HaveCount(11);
        }
        finally
        {
            await ComoDuenoAsync("UPDATE catalogos.oficios SET activo = true WHERE slug = 'cerrajero'");
        }
    }

    [Fact]
    public async Task Los_municipios_se_listan_sin_poligono()
    {
        using var cliente = fabrica.CrearCliente();

        using var respuesta = await cliente.GetAsync(Ruta("catalogos/municipios"), Cancelacion);

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await respuesta.Content.ReadFromJsonAsync<JsonElement>(Cancelacion);
        json.GetArrayLength().Should().Be(ZonaDePrueba.Municipios);
        json.EnumerateArray().First().EnumerateObject().Select(p => p.Name)
            .Should().BeEquivalentTo(["id", "claveInegi", "nombre", "estado"]);
        json.EnumerateArray().Select(m => m.GetProperty("nombre").GetString())
            .Should().Contain(["Valle Alto", "Real de Minas"]);
        json.EnumerateArray().Select(m => m.GetProperty("estado").GetString()).Should().AllBe("Estado de Prueba");
    }

    [Theory]
    [InlineData("99001", "Valle Alto")]
    [InlineData("99003", "Santa Lucía")]
    [InlineData("99005", "Puerto Nuevo")]
    public async Task Ubicar_devuelve_el_municipio_que_contiene_el_punto(string clave, string nombre)
    {
        var (lat, lng) = ZonaDePrueba.CentroDe(clave);
        using var cliente = await ClienteAsync();

        using var respuesta = await cliente.PostAsJsonAsync(Ruta("catalogos/municipios/ubicar"), new { lat, lng }, Cancelacion);

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var municipio = await respuesta.Content.ReadFromJsonAsync<MunicipioDto>(Cancelacion);
        municipio!.ClaveInegi.Should().Be(clave);
        municipio.Nombre.Should().Be(nombre);
        respuesta.Headers.CacheControl!.NoStore.Should().BeTrue("la ubicación es un dato personal");
    }

    [Fact]
    public async Task Ubicar_fuera_de_la_zona_responde_404()
    {
        using var cliente = await ClienteAsync();

        using var respuesta = await cliente.PostAsJsonAsync(Ruta("catalogos/municipios/ubicar"), new { lat = 19.43, lng = -99.13 }, Cancelacion);

        respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problema = await respuesta.Content.ReadFromJsonAsync<ProblemDetails>(Cancelacion);
        problema!.Type.Should().Be(TiposProblema.FueraDeZona);
        problema.Title.Should().Be("Tu ubicación está fuera de la zona de servicio.");
    }

    [Theory]
    [InlineData(91, -100.5)]
    [InlineData(-91, -100.5)]
    [InlineData(20.5, 181)]
    [InlineData(20.5, -181)]
    public async Task Ubicar_rechaza_coordenadas_imposibles(double lat, double lng)
    {
        using var cliente = await ClienteAsync();

        using var respuesta = await cliente.PostAsJsonAsync(Ruta("catalogos/municipios/ubicar"), new { lat, lng }, Cancelacion);

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Ubicar_sin_coordenadas_responde_400()
    {
        using var cliente = await ClienteAsync();

        using var respuesta = await cliente.PostAsJsonAsync(Ruta("catalogos/municipios/ubicar"), new { lat = 20.55 }, Cancelacion);

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await respuesta.Content.ReadFromJsonAsync<ValidationProblemDetails>(Cancelacion))!.Errors.Should().ContainKey("lng");
    }

    [Fact]
    public async Task Importar_otra_vez_actualiza_sin_duplicar()
    {
        var importados = await fabrica.ImportarAsync();

        importados.Should().Be(ZonaDePrueba.Municipios);
        using var cliente = fabrica.CrearCliente();
        (await cliente.GetFromJsonAsync<MunicipioDto[]>(Ruta("catalogos/municipios"), Cancelacion)).Should().HaveCount(ZonaDePrueba.Municipios);
    }

    [Theory]
    [InlineData("""{"type":"FeatureCollection"}""")]
    [InlineData("""{"type":"FeatureCollection","features":[{"type":"Feature","properties":{"CVEGEO":"9X001","NOMGEO":"Mal"},"geometry":{"type":"Polygon","coordinates":[[[0,0],[1,0],[1,1],[0,0]]]}}]}""")]
    [InlineData("""{"type":"FeatureCollection","features":[{"type":"Feature","properties":{"CVEGEO":"77001","NOMGEO":"Sin estado"},"geometry":{"type":"Polygon","coordinates":[[[0,0],[1,0],[1,1],[0,0]]]}}]}""")]
    [InlineData("""{"type":"FeatureCollection","features":[{"type":"Feature","properties":{"CVEGEO":"99009","NOMGEO":"Punto"},"geometry":{"type":"Point","coordinates":[0,0]}}]}""")]
    public async Task El_importador_rechaza_un_GeoJSON_que_no_es_de_municipios(string geojson)
    {
        var importar = () => fabrica.ImportarAsync(geojson);

        await importar.Should().ThrowAsync<PostgresException>();
    }

    [Theory]
    [InlineData("INSERT INTO catalogos.oficios (id, nombre, slug, icono, activo, created_at, updated_at) VALUES (gen_random_uuid(), 'X', 'x', 'x', true, now(), now())")]
    [InlineData("UPDATE catalogos.municipios SET nombre = 'Otro'")]
    [InlineData("DELETE FROM catalogos.oficios")]
    [InlineData("SELECT catalogos.importar_municipios('{}'::jsonb)")]
    public async Task La_app_solo_puede_leer_los_catalogos(string sql)
    {
        await using var conexion = new NpgsqlConnection(fabrica.ConexionApp);
        await conexion.OpenAsync(Cancelacion);
#pragma warning disable CA2100 // SQL fijo de la prueba.
        await using var comando = new NpgsqlCommand(sql, conexion);
#pragma warning restore CA2100

        var ejecutar = () => comando.ExecuteNonQueryAsync(Cancelacion);

        (await ejecutar.Should().ThrowAsync<PostgresException>()).Which.SqlState.Should().Be(PostgresErrorCodes.InsufficientPrivilege);
    }

    [Fact]
    public async Task El_servicio_valida_oficios_y_municipios_para_otros_modulos()
    {
        await using var ambito = fabrica.CrearAmbito();
        var catalogos = ambito.ServiceProvider.GetRequiredService<ICatalogosService>();
        var municipios = await catalogos.MunicipiosAsync(Cancelacion);
        var albanil = new Guid("a0000000-0000-7000-8000-000000000001");
        var (lat, lng) = ZonaDePrueba.CentroDe("99006");

        (await catalogos.SonOficiosActivosAsync([albanil], Cancelacion)).Should().BeTrue();
        (await catalogos.SonOficiosActivosAsync([albanil, Guid.CreateVersion7()], Cancelacion)).Should().BeFalse();
        (await catalogos.SonOficiosActivosAsync([], Cancelacion)).Should().BeFalse();
        (await catalogos.ExistenMunicipiosAsync([.. municipios.Take(2).Select(m => m.Id)], Cancelacion)).Should().BeTrue();
        (await catalogos.ExistenMunicipiosAsync([Guid.CreateVersion7()], Cancelacion)).Should().BeFalse();
        (await catalogos.UbicarAsync(lat, lng, Cancelacion))!.Nombre.Should().Be("Real de Minas");
    }

    private async Task<HttpClient> ClienteAsync()
    {
        var cliente = fabrica.CrearCliente();
        await cliente.PrepararXsrfAsync();
        return cliente;
    }

    private async Task ComoDuenoAsync(string sql)
    {
        await using var conexion = new NpgsqlConnection(fabrica.ConexionDueno);
        await conexion.OpenAsync(Cancelacion);
#pragma warning disable CA2100 // SQL fijo de la prueba.
        await using var comando = new NpgsqlCommand(sql, conexion);
#pragma warning restore CA2100
        await comando.ExecuteNonQueryAsync(Cancelacion);
    }

    private static Uri Ruta(string ruta) => new($"/api/v1/{ruta}", UriKind.Relative);

    private sealed record OficioDto(Guid Id, string Nombre, string Slug, string Icono);

    private sealed record MunicipioDto(Guid Id, string ClaveInegi, string Nombre, string Estado);
}
