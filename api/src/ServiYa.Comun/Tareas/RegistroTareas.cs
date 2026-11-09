using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ServiYa.Comun.Tareas;

public static class RegistroTareas
{
    public static IServiceCollection AgregarTareasProgramadas(this IServiceCollection services)
    {
        ObtenerCatalogo(services);
        services.TryAddSingleton<ICandadoTareas, CandadoPostgres>();
        services.AddHostedService<PlanificadorTareas>();
        return services;
    }

    /// <summary>Lo llama cada módulo por sus tareas. Cada ejecución resuelve la tarea en un ámbito nuevo.</summary>
    public static IServiceCollection AgregarTarea<T>(this IServiceCollection services)
        where T : class, ITareaProgramada
    {
        ObtenerCatalogo(services).Agregar(typeof(T));
        services.TryAddScoped<T>();
        return services;
    }

    /// <summary>El catálogo se arma al registrar, antes de construir el proveedor de servicios.</summary>
    private static CatalogoTareas ObtenerCatalogo(IServiceCollection services)
    {
        var existente = services.FirstOrDefault(d => d.ServiceType == typeof(CatalogoTareas))?.ImplementationInstance;
        if (existente is CatalogoTareas catalogo)
        {
            return catalogo;
        }

        var nuevo = new CatalogoTareas();
        services.AddSingleton(nuevo);
        return nuevo;
    }
}
