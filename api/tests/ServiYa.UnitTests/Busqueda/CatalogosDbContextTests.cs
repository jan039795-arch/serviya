using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting.Internal;
using ServiYa.Busqueda;
using ServiYa.Busqueda.Datos;
using ServiYa.Busqueda.Dominio;

namespace ServiYa.UnitTests.Busqueda;

/// <summary>Modelo de catálogos (T065), armado igual que en la API pero sin conectarse a PostgreSQL.</summary>
public sealed class CatalogosDbContextTests : IDisposable
{
    private readonly ServiceProvider proveedor;
    private readonly IServiceScope ambito;
    private readonly CatalogosDbContext contexto;

    public CatalogosDbContextTests()
    {
        var configuracion = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = "Host=localhost;Database=ficticia;Username=ficticio",
            })
            .Build();
        var services = new ServiceCollection().AddSingleton(TimeProvider.System);
        new BusquedaModulo().AgregarServicios(services, configuracion, new HostingEnvironment { EnvironmentName = "Testing" });
        proveedor = services.BuildServiceProvider();
        ambito = proveedor.CreateScope();
        contexto = ambito.ServiceProvider.GetRequiredService<CatalogosDbContext>();
    }

    [Fact]
    public void Las_migraciones_estan_al_dia_con_el_modelo()
    {
        // Si falla: dotnet ef migrations add <Nombre> --project src/Modulos/ServiYa.Busqueda
        //   --startup-project src/ServiYa.Api --context CatalogosDbContext --output-dir Datos/Migraciones
        contexto.Database.HasPendingModelChanges().Should().BeFalse();
    }

    [Theory]
    [InlineData(typeof(Oficio), "oficios")]
    [InlineData(typeof(Municipio), "municipios")]
    public void Cada_entidad_tiene_su_tabla_en_el_esquema_catalogos(Type tipo, string tabla)
    {
        var entidad = contexto.Model.FindEntityType(tipo)!;

        entidad.GetSchema().Should().Be("catalogos");
        entidad.GetTableName().Should().Be(tabla);
    }

    [Fact]
    public void El_poligono_es_geografia_con_indice_GIST()
    {
        var entidad = contexto.Model.FindEntityType(typeof(Municipio))!;
        var poligono = entidad.FindProperty(nameof(Municipio.Poligono))!;

        poligono.GetColumnType().Should().Be("geography (multipolygon, 4326)");
        entidad.FindProperty(nameof(Municipio.Centroide))!.GetColumnType().Should().Be("geography (point, 4326)");
        var indice = entidad.GetIndexes().Single(i => i.Properties.Contains(poligono));
        indice.FindAnnotation("Npgsql:IndexMethod")!.Value.Should().Be("gist");
    }

    [Fact]
    public void El_slug_del_oficio_y_la_clave_del_municipio_son_unicos()
    {
        contexto.Model.FindEntityType(typeof(Oficio))!.GetIndexes()
            .Should().Contain(i => i.IsUnique && i.Properties.Single().Name == nameof(Oficio.Slug));
        contexto.Model.FindEntityType(typeof(Municipio))!.GetIndexes()
            .Should().Contain(i => i.IsUnique && i.Properties.Single().Name == nameof(Municipio.ClaveInegi));
    }

    [Fact]
    public void Hay_12_oficios_semilla_activos_con_slug_en_minusculas()
    {
        // La semilla solo está en el modelo de diseño, no en el optimizado que usa la app.
        var modelo = contexto.GetService<IDesignTimeModel>().Model;
        var semilla = modelo.FindEntityType(typeof(Oficio))!.GetSeedData().ToList();

        semilla.Should().HaveCount(12);
        semilla.Select(o => (string)o[nameof(Oficio.Slug)]!).Should().AllSatisfy(slug => slug.Should().MatchRegex("^[a-z]+(-[a-z]+)*$"));
        semilla.Select(o => (bool)o[nameof(Oficio.Activo)]!).Should().AllBeEquivalentTo(true);
    }

    public void Dispose()
    {
        ambito.Dispose();
        proveedor.Dispose();
    }
}
