using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ServiYa.Comun.Configuracion;

public static class RegistroOpciones
{
    /// <summary>
    /// Registra todas las opciones de ServiYa. Si falta un valor obligatorio la API no arranca y el
    /// mensaje dice qué clave falta, sin mostrar su valor.
    /// </summary>
    public static IServiceCollection AgregarOpcionesDeServiYa(
        this IServiceCollection services,
        IConfiguration configuracion)
    {
        services.AgregarOpcionesValidadas<StorageOptions>(configuracion);
        services.AgregarOpcionesValidadas<MercadoPagoOptions>(configuracion);
        services.AgregarOpcionesValidadas<EmailOptions>(configuracion);
        services.AgregarOpcionesValidadas<WebPushOptions>(configuracion);
        return services;
    }

    public static OptionsBuilder<T> AgregarOpcionesValidadas<T>(
        this IServiceCollection services,
        IConfiguration configuracion)
        where T : class, IOpcionesConSeccion
    {
        ArgumentNullException.ThrowIfNull(configuracion);
        return services
            .AddOptions<T>()
            .Bind(configuracion.GetSection(T.Seccion))
            .ValidateDataAnnotations()
            .ValidateOnStart();
    }
}
