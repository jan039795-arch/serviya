using Amazon.S3;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ServiYa.Comun.Configuracion;

namespace ServiYa.Comun.Almacenamiento;

public static class RegistroAlmacenamiento
{
    public static IServiceCollection AgregarAlmacenamiento(this IServiceCollection services)
    {
        services.AddSingleton<IAmazonS3>(proveedor =>
            AlmacenR2.CrearCliente(proveedor.GetRequiredService<IOptions<StorageOptions>>().Value));
        services.AddSingleton<IAlmacenArchivos, AlmacenR2>();
        return services;
    }
}
