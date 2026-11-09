using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ServiYa.Comun.Datos;

public static class RegistroDatos
{
    /// <summary>Rol <c>serviya_app</c>: lee y escribe datos.</summary>
    public const string ConexionApp = "Default";

    /// <summary>Rol <c>serviya_owner</c>: dueño del esquema; solo para <c>dotnet ef</c>.</summary>
    public const string ConexionMigraciones = "Migraciones";

    public const string TablaHistorialMigraciones = "__ef_migraciones";

    /// <summary>
    /// Registra el <c>DbContext</c> de un módulo con PostgreSQL, su propio esquema (también para el
    /// historial de migraciones) y las marcas de tiempo. Con <c>dotnet ef</c> usa la conexión de
    /// migraciones; en la API, la de la app.
    /// </summary>
    /// <param name="usaGeografia">
    /// Solo los módulos con columnas geográficas (catálogos y prestadores) activan NetTopologySuite.
    /// La extensión <c>postgis</c> la crea el aprovisionamiento de la base, no cada migración.
    /// </param>
    public static IServiceCollection AgregarDbContextDeModulo<TContexto>(
        this IServiceCollection services,
        IConfiguration configuracion,
        string esquema,
        bool usaGeografia = false)
        where TContexto : DbContext
    {
        ArgumentNullException.ThrowIfNull(configuracion);
        var nombre = EF.IsDesignTime ? ConexionMigraciones : ConexionApp;
        var cadena = configuracion.GetConnectionString(nombre)
            ?? throw new InvalidOperationException($"Falta la cadena de conexión ConnectionStrings:{nombre}.");

        services.TryAddSingleton<MarcasTiempoInterceptor>();
        services.AddDbContext<TContexto>((proveedor, opciones) => opciones
            .UseNpgsql(cadena, npgsql =>
            {
                npgsql.MigrationsHistoryTable(TablaHistorialMigraciones, esquema);
                if (usaGeografia)
                {
                    npgsql.UseNetTopologySuite();
                }
            })
            .AddInterceptors(proveedor.GetRequiredService<MarcasTiempoInterceptor>()));
        return services;
    }
}
