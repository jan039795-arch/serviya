using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting.Internal;
using ServiYa.Comun.Datos;
using ServiYa.Cuentas;
using ServiYa.Cuentas.Datos;
using ServiYa.Cuentas.Dominio;

namespace ServiYa.UnitTests.Cuentas;

/// <summary>Modelo de Cuentas (T057), armado igual que en la API pero sin conectarse a PostgreSQL.</summary>
public sealed class CuentasDbContextTests : IDisposable
{
    private readonly ServiceProvider proveedor;
    private readonly IServiceScope ambito;
    private readonly CuentasDbContext contexto;

    public CuentasDbContextTests()
    {
        var configuracion = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = "Host=localhost;Database=ficticia;Username=ficticio",
            })
            .Build();
        var services = new ServiceCollection().AddSingleton(TimeProvider.System);
        new CuentasModulo().AgregarServicios(services, configuracion, new HostingEnvironment { EnvironmentName = "Testing" });
        proveedor = services.BuildServiceProvider();
        ambito = proveedor.CreateScope();
        contexto = ambito.ServiceProvider.GetRequiredService<CuentasDbContext>();
    }

    [Fact]
    public void Las_migraciones_estan_al_dia_con_el_modelo()
    {
        // Si falla: dotnet ef migrations add <Nombre> --project src/Modulos/ServiYa.Cuentas
        //   --startup-project src/ServiYa.Api --context CuentasDbContext --output-dir Datos/Migraciones
        contexto.Database.HasPendingModelChanges().Should().BeFalse();
    }

    [Theory]
    [InlineData(typeof(Usuario), "usuarios")]
    [InlineData(typeof(CodigoVerificacion), "codigos_verificacion")]
    [InlineData(typeof(Consentimiento), "consentimientos")]
    [InlineData(typeof(Bloqueo), "bloqueos")]
    public void Cada_entidad_tiene_su_tabla_en_el_esquema_cuentas(Type tipo, string tabla)
    {
        var entidad = contexto.Model.FindEntityType(tipo)!;

        entidad.GetSchema().Should().Be(CuentasDbContext.Esquema);
        entidad.GetTableName().Should().Be(tabla);
    }

    [Fact]
    public void Sin_tablas_de_passkeys()
    {
        contexto.Model.GetEntityTypes().Select(e => e.GetTableName())
            .Should().NotContain(tabla => tabla != null && tabla.Contains("passkey", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void El_codigo_tiene_token_de_concurrencia_en_xmin_para_que_no_se_salten_los_intentos()
    {
        var version = contexto.Model.FindEntityType(typeof(CodigoVerificacion))!
            .FindProperty(nameof(CodigoVerificacion.Version))!;

        version.IsConcurrencyToken.Should().BeTrue();
        version.GetColumnName().Should().Be("xmin");
        version.GetColumnType().Should().Be("xid");
    }

    [Fact]
    public void Cuentas_no_pide_la_extension_postgis()
    {
        contexto.Model.GetAnnotations().Select(a => a.Name)
            .Should().NotContain(nombre => nombre.Contains("postgis", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void El_contexto_usa_el_interceptor_de_marcas_de_tiempo()
    {
        var opciones = contexto.GetService<IDbContextOptions>().FindExtension<CoreOptionsExtension>()!;

        opciones.Interceptors.Should().ContainSingle(i => i is MarcasTiempoInterceptor);
    }

    [Fact]
    public void El_correo_es_unico()
    {
        var indice = contexto.Model.FindEntityType(typeof(Usuario))!.GetIndexes()
            .Single(i => i.Properties.Single().Name == nameof(Usuario.NormalizedEmail));

        indice.IsUnique.Should().BeTrue();
    }

    public void Dispose()
    {
        ambito.Dispose();
        proveedor.Dispose();
    }
}
