using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

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
        ["Storage:Endpoint"] = "http://localhost:9000",
        ["Storage:AccessKey"] = "clave-ficticia",
        ["Storage:SecretKey"] = "secreto-ficticio",
        ["Storage:UrlPublica"] = "http://localhost:9000/serviya-publico",
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

            if (servicios is not null)
            {
                builder.ConfigureServices(servicios);
            }
        });

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
