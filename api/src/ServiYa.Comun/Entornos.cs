using Microsoft.Extensions.Hosting;

namespace ServiYa.Comun;

public static class Entornos
{
    /// <summary>Entorno de las pruebas de integración: no carga user-secrets ni appsettings.Development.json.</summary>
    public const string Pruebas = "Testing";

    /// <summary>Solo en desarrollo y pruebas se permiten piezas simuladas, como el correo en memoria.</summary>
    public static bool PermiteSimulaciones(this IHostEnvironment entorno)
    {
        ArgumentNullException.ThrowIfNull(entorno);
        return entorno.IsDevelopment() || entorno.IsEnvironment(Pruebas);
    }
}
