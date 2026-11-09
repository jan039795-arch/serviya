using Microsoft.AspNetCore.HttpLogging;

namespace ServiYa.Api.Seguridad;

/// <summary>
/// Registro de la API (T055): JSON fuera de desarrollo y, por cada petición, solo método, ruta,
/// estado y duración. Nunca cuerpos, encabezados ni cadenas de consulta, que pueden traer correos,
/// teléfonos, contraseñas o la ubicación (principio I).
/// </summary>
internal static class Registro
{
    public static WebApplicationBuilder AgregarRegistro(this WebApplicationBuilder builder)
    {
        builder.Logging.ClearProviders();
        if (builder.Environment.IsDevelopment())
        {
            builder.Logging.AddSimpleConsole(consola => consola.SingleLine = true);
        }
        else
        {
            builder.Logging.AddJsonConsole(json =>
            {
                json.UseUtcTimestamp = true;
                json.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
                json.IncludeScopes = true;
            });
        }

        builder.Services.AddHttpLogging(peticiones =>
        {
            peticiones.LoggingFields = HttpLoggingFields.RequestMethod
                | HttpLoggingFields.RequestPath
                | HttpLoggingFields.ResponseStatusCode
                | HttpLoggingFields.Duration;
            peticiones.CombineLogs = true;
        });
        return builder;
    }
}
