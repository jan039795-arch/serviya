using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting.Internal;
using ServiYa.Administracion;
using ServiYa.Administracion.Datos;
using ServiYa.Administracion.Dominio;
using ServiYa.Comun.Bitacora;

namespace ServiYa.UnitTests.Administracion;

/// <summary>Modelo de Administración (T049), armado igual que en la API pero sin conectarse a PostgreSQL.</summary>
public sealed class AdministracionDbContextTests : IDisposable
{
    private readonly ServiceProvider proveedor;
    private readonly IServiceScope ambito;
    private readonly AdministracionDbContext contexto;

    public AdministracionDbContextTests()
    {
        var configuracion = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = "Host=localhost;Database=ficticia;Username=ficticio",
            })
            .Build();
        var services = new ServiceCollection().AddSingleton(TimeProvider.System);
        new AdministracionModulo().AgregarServicios(services, configuracion, new HostingEnvironment { EnvironmentName = "Testing" });
        proveedor = services.BuildServiceProvider();
        ambito = proveedor.CreateScope();
        contexto = ambito.ServiceProvider.GetRequiredService<AdministracionDbContext>();
    }

    [Fact]
    public void Las_migraciones_estan_al_dia_con_el_modelo()
    {
        // Si falla: dotnet ef migrations add <Nombre> --project src/Modulos/ServiYa.Administracion
        //   --startup-project src/ServiYa.Api --context AdministracionDbContext --output-dir Datos/Migraciones
        contexto.Database.HasPendingModelChanges().Should().BeFalse();
    }

    [Fact]
    public void La_bitacora_esta_en_el_esquema_admin()
    {
        var entidad = contexto.Model.FindEntityType(typeof(RegistroBitacora))!;

        entidad.GetSchema().Should().Be("admin");
        entidad.GetTableName().Should().Be("registro_bitacora");
        entidad.FindProperty(nameof(RegistroBitacora.Detalle))!.GetColumnType().Should().Be("jsonb");
        entidad.FindProperty(nameof(RegistroBitacora.CreadoEn))!.GetColumnName().Should().Be("created_at");
    }

    [Fact]
    public void Registra_la_bitacora_por_ambito_con_el_contexto_de_administracion()
    {
        var bitacora = ambito.ServiceProvider.GetRequiredService<IBitacora>();

        bitacora.Should().NotBeNull();
        ambito.ServiceProvider.GetRequiredService<IBitacora>().Should().BeSameAs(bitacora);
        using var otroAmbito = proveedor.CreateScope();
        otroAmbito.ServiceProvider.GetRequiredService<IBitacora>().Should().NotBeSameAs(bitacora);
    }

    public void Dispose()
    {
        ambito.Dispose();
        proveedor.Dispose();
    }
}
