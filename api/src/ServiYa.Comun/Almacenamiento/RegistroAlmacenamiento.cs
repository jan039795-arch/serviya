using Amazon.S3;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using ServiYa.Comun.Configuracion;

namespace ServiYa.Comun.Almacenamiento;

public static class RegistroAlmacenamiento
{
    public const string MensajeHttpsObligatorio =
        "Storage:Endpoint y Storage:UrlPublica deben usar https fuera de desarrollo y pruebas.";

    public static IServiceCollection AgregarAlmacenamiento(this IServiceCollection services)
    {
        // Las URL firmadas de documentos de identidad nunca deben viajar sin cifrar.
        services.AddOptions<StorageOptions>().Validate<IHostEnvironment>(
            (opciones, entorno) => entorno.PermiteSimulaciones() || (EsHttps(opciones.Endpoint) && EsHttps(opciones.UrlPublica)),
            MensajeHttpsObligatorio);
        services.AddSingleton<IAmazonS3>(proveedor =>
            AlmacenR2.CrearCliente(proveedor.GetRequiredService<IOptions<StorageOptions>>().Value));
        services.AddSingleton<IAlmacenArchivos, AlmacenR2>();
        return services;
    }

    private static bool EsHttps(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps;
}
