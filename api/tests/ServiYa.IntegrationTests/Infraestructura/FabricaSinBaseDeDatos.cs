using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ServiYa.Comun.Tareas;

namespace ServiYa.IntegrationTests.Infraestructura;

/// <summary>
/// Levanta la API en memoria con configuración ficticia y sin base de datos, para probar el host
/// (arranque, errores, encabezados, registro) sin Docker. La fábrica con PostgreSQL llega en T046.
/// </summary>
internal static class FabricaSinBaseDeDatos
{
    /// <summary>Valores ficticios; nunca credenciales reales.</summary>
    public static readonly IReadOnlyDictionary<string, string> ConfiguracionFicticia = new Dictionary<string, string>
    {
        ["Storage:Endpoint"] = "https://almacen.serviya.test",
        ["Storage:AccessKey"] = "clave-ficticia",
        ["Storage:SecretKey"] = "secreto-ficticio",
        ["Storage:UrlPublica"] = "https://archivos.serviya.test",
        ["MercadoPago:AccessToken"] = "TEST-ficticio",
        ["MercadoPago:WebhookSecret"] = "firma-ficticia",
        ["Email:Proveedor"] = "Consola",
        ["Email:Remitente"] = "no-responder@serviya.test",
        ["WebPush:PublicKey"] = "publica-ficticia",
        ["WebPush:PrivateKey"] = "privada-ficticia",
        ["AllowedHosts"] = "localhost",
        // Nada se conecta: estas pruebas no tocan la base de datos.
        ["ConnectionStrings:Default"] = "Host=localhost;Database=ficticia;Username=ficticio",
    };

    public static WebApplicationFactory<Program> Crear(
        string entorno,
        IReadOnlyDictionary<string, string>? configuracion = null,
        Action<IServiceCollection>? servicios = null) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(entorno);
            foreach (var (clave, valor) in configuracion ?? ConfiguracionFicticia)
            {
                builder.UseSetting(clave, valor);
            }

            QuitarUserSecrets(builder);

            // El planificador nunca toma el candado de PostgreSQL: no hay base de datos.
            builder.ConfigureServices(s => s.Replace(ServiceDescriptor.Singleton<ICandadoTareas, CandadoNuncaDisponible>()));
            if (servicios is not null)
            {
                builder.ConfigureServices(servicios);
            }
        });

    /// <summary>
    /// Sin los user-secrets del desarrollador: en "Development" se cargarían y las pruebas dependerían
    /// de la máquina.
    /// </summary>
    public static void QuitarUserSecrets(IWebHostBuilder builder) =>
        builder.ConfigureAppConfiguration((_, fuentes) =>
        {
            var secretos = fuentes.Sources.OfType<JsonConfigurationSource>()
                .Where(fuente => fuente.Path == "secrets.json")
                .ToList();
            foreach (var fuente in secretos)
            {
                fuentes.Sources.Remove(fuente);
            }
        });

    /// <summary>Como si otra instancia tuviera siempre el candado: ninguna tarea corre.</summary>
    private sealed class CandadoNuncaDisponible : ICandadoTareas
    {
        public Task<IAsyncDisposable?> IntentarTomarAsync(string nombreTarea, CancellationToken cancellationToken) =>
            Task.FromResult<IAsyncDisposable?>(null);
    }

    public static Dictionary<string, string> ConfiguracionCon(params (string Clave, string Valor)[] cambios)
    {
        var configuracion = new Dictionary<string, string>(ConfiguracionFicticia);
        foreach (var (clave, valor) in cambios)
        {
            configuracion[clave] = valor;
        }

        return configuracion;
    }
}
