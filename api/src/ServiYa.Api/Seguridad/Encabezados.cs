namespace ServiYa.Api.Seguridad;

/// <summary>
/// HTTPS, HSTS, CORS y encabezados de seguridad (T054). Los orígenes de CORS vienen de
/// <c>Cors:Origenes</c>: <c>https://serviya.mx</c> en producción y <c>http://localhost:4200</c> en
/// desarrollo, siempre con credenciales porque la sesión va en una cookie.
/// </summary>
internal static class Encabezados
{
    public const string PoliticaCors = "web";

    private static readonly TimeSpan DuracionHsts = TimeSpan.FromDays(365);

    public static IServiceCollection AgregarSeguridadHttp(this IServiceCollection services, IConfiguration configuracion)
    {
        var origenes = configuracion.GetSection("Cors:Origenes").Get<string[]>() ?? [];
        services.AddCors(cors => cors.AddPolicy(PoliticaCors, politica => politica
            .WithOrigins(origenes)
            .AllowCredentials()
            .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE")
            .WithHeaders("Content-Type", "X-XSRF-TOKEN")
            // La app lee Retry-After para decir cuánto esperar tras un 429.
            .WithExposedHeaders("Retry-After")));
        services.AddHsts(hsts =>
        {
            hsts.MaxAge = DuracionHsts;
            hsts.IncludeSubDomains = true;
        });
        return services;
    }

    public static WebApplication UsarSeguridadHttp(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.Use((contexto, siguiente) =>
        {
            // En OnStarting para que también lleguen en las respuestas del manejador de errores.
            contexto.Response.OnStarting(() =>
            {
                var encabezados = contexto.Response.Headers;
                encabezados.XContentTypeOptions = "nosniff";
                encabezados["Referrer-Policy"] = "no-referrer";
                encabezados.XFrameOptions = "DENY";

                // La API solo devuelve JSON: nada se ejecuta ni se incrusta.
                encabezados.ContentSecurityPolicy = "default-src 'none'; frame-ancestors 'none'";

                // Respuestas con datos personales o URL firmadas: ni el navegador ni un proxy las guardan.
                // Un endpoint público puede poner su propio Cache-Control (p. ej. el perfil público).
                if (string.IsNullOrEmpty(encabezados.CacheControl))
                {
                    encabezados.CacheControl = "no-store";
                }

                return Task.CompletedTask;
            });
            return siguiente(contexto);
        });
        app.UseCors(PoliticaCors);
        return app;
    }
}
