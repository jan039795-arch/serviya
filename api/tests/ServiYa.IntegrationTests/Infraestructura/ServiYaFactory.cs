using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Time.Testing;
using ServiYa.Comun;
using ServiYa.Comun.Correo;
using ServiYa.Comun.Datos;
using ServiYa.Comun.Tareas;

namespace ServiYa.IntegrationTests.Infraestructura;

/// <summary>
/// La API completa contra PostgreSQL y RustFS reales (T046). Cada fábrica tiene su propia base con
/// todas las migraciones aplicadas por <c>serviya_owner</c>; la API se conecta como <c>serviya_app</c>,
/// igual que en producción. Usarla con <c>IClassFixture&lt;ServiYaFactory&gt;</c>.
/// <list type="bullet">
///   <item><see cref="Reloj"/>: <see cref="FakeTimeProvider"/> para adelantar el tiempo (vencimientos, 48 h...).</item>
///   <item><see cref="Correos"/>: los correos enviados, para leer los códigos.</item>
///   <item>El planificador no corre solo: las pruebas llaman a <c>PlanificadorTareas.EjecutarRondaAsync</c>.</item>
/// </list>
/// </summary>
public sealed class ServiYaFactory(ContenedoresServiYa contenedores) : WebApplicationFactory<Program>, IAsyncLifetime
{
    private BaseDePrueba? baseDePrueba;

    /// <summary>Empieza el 2026-11-08 a mediodía, hora de la Ciudad de México.</summary>
    public FakeTimeProvider Reloj { get; } = new(new DateTimeOffset(2026, 11, 8, 18, 0, 0, TimeSpan.Zero));

    public ConsolaEmailSender Correos => Services.GetRequiredService<ConsolaEmailSender>();

    /// <summary>Conexión del dueño del esquema, para preparar datos o revisar privilegios.</summary>
    internal string ConexionDueno => Base.ConexionDueno;

    internal string ConexionApp => Base.ConexionApp;

    internal ContenedoresServiYa Contenedores => contenedores;

    private BaseDePrueba Base => baseDePrueba ?? throw new InvalidOperationException("La fábrica no se ha inicializado.");

    public async ValueTask InitializeAsync()
    {
        baseDePrueba = await contenedores.CrearBaseAsync().ConfigureAwait(false);
        await AplicarMigracionesAsync().ConfigureAwait(false);
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }

    /// <summary>Un ámbito de servicios de la API (para usar un <c>DbContext</c> o un servicio en la prueba).</summary>
    public AsyncServiceScope CrearAmbito() => Services.CreateAsyncScope();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        var almacen = contenedores.OpcionesAlmacen();
        builder.UseEnvironment(Entornos.Pruebas);
        foreach (var (clave, valor) in new Dictionary<string, string>
        {
            ["ConnectionStrings:Default"] = Base.ConexionApp,
            ["Storage:Endpoint"] = almacen.Endpoint,
            ["Storage:AccessKey"] = almacen.AccessKey,
            ["Storage:SecretKey"] = almacen.SecretKey,
            ["Storage:Region"] = almacen.Region,
            ["Storage:UrlPublica"] = almacen.UrlPublica,
            ["MercadoPago:AccessToken"] = "TEST-ficticio",
            ["MercadoPago:WebhookSecret"] = "firma-ficticia",
            ["Email:Proveedor"] = "Consola",
            ["Email:Remitente"] = "no-responder@serviya.test",
            ["WebPush:PublicKey"] = "publica-ficticia",
            ["WebPush:PrivateKey"] = "privada-ficticia",
            ["AllowedHosts"] = "localhost",
        })
        {
            builder.UseSetting(clave, valor);
        }

        FabricaSinBaseDeDatos.QuitarUserSecrets(builder);
        builder.ConfigureServices(services =>
        {
            services.Replace(ServiceDescriptor.Singleton<TimeProvider>(Reloj));

            // Solo el planificador: los demás servicios en segundo plano (como el del host web) se quedan.
            var planificador = services.Where(d =>
                d.ServiceType == typeof(IHostedService) && d.ImplementationType == typeof(PlanificadorTareas)).ToList();
            foreach (var descriptor in planificador)
            {
                services.Remove(descriptor);
            }

            services.TryAddSingleton<PlanificadorTareas>();
        });
    }

    /// <summary>
    /// Migra cada <c>DbContext</c> registrado con la conexión del dueño. Se usa el contexto de la API
    /// (mismo modelo y opciones) cambiando solo la cadena de conexión.
    /// </summary>
    private async Task AplicarMigracionesAsync()
    {
        var catalogo = Services.GetRequiredService<CatalogoDbContexts>();
        foreach (var tipo in catalogo.Tipos)
        {
            await using var ambito = CrearAmbito();
            var contexto = (DbContext)ambito.ServiceProvider.GetRequiredService(tipo);
            contexto.Database.SetConnectionString(Base.ConexionDueno);
            await contexto.Database.MigrateAsync().ConfigureAwait(false);
        }
    }
}
