using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ServiYa.Comun.Errores;

namespace ServiYa.Comun.Seguridad;

/// <summary>
/// Protección CSRF (research R2, T059) con el antiforgery de ASP.NET Core en modo SPA:
/// <list type="bullet">
///   <item>la cookie HttpOnly <c>serviya.xsrf</c> guarda la mitad que JavaScript no ve;</item>
///   <item>la cookie <c>XSRF-TOKEN</c>, legible por la app, trae la otra mitad, que la app devuelve en
///   el encabezado <c>X-XSRF-TOKEN</c> (<c>web/src/app/core/http/xsrf.interceptor.ts</c>);</item>
///   <item>todo <c>POST</c>, <c>PUT</c>, <c>PATCH</c> y <c>DELETE</c> se valida, salvo los endpoints
///   marcados con <see cref="SinValidacionXsrfAttribute"/> (el webhook).</item>
/// </list>
/// El token va ligado al usuario: al entrar o salir hay que emitir uno nuevo con <see cref="EmisorXsrf"/>.
/// </summary>
public static class Xsrf
{
    public const string CookieLegible = "XSRF-TOKEN";
    public const string Encabezado = "X-XSRF-TOKEN";
    public const string CookieInterna = "serviya.xsrf";

    /// <summary>Dominio padre de las cookies (<c>serviya.mx</c>) para que la app en otro subdominio las use. Vacío: solo el host de la API.</summary>
    public const string ClaveDominioCookie = "Sesion:DominioCookie";

    public static IServiceCollection AgregarXsrf(this IServiceCollection services, IConfiguration configuracion, IHostEnvironment entorno)
    {
        ArgumentNullException.ThrowIfNull(configuracion);
        var dominio = DominioCookie(configuracion);
        var politica = PoliticaSecure(entorno);
        services.AddAntiforgery(opciones =>
        {
            opciones.HeaderName = Encabezado;
            opciones.Cookie.Name = CookieInterna;
            opciones.Cookie.HttpOnly = true;
            opciones.Cookie.SecurePolicy = politica;
            opciones.Cookie.SameSite = SameSiteMode.Strict;
            opciones.Cookie.Domain = dominio;

            // La API ya manda X-Frame-Options: DENY en toda respuesta.
            opciones.SuppressXFrameOptionsHeader = true;
        });
        services.AddSingleton(new EmisorXsrf(dominio, politica));
        return services;
    }

    /// <summary>Valida el token en lo que modifica datos. Va después de la autenticación (el token es del usuario).</summary>
    public static IApplicationBuilder UsarValidacionXsrf(this IApplicationBuilder app) =>
        app.Use(async (contexto, siguiente) =>
        {
            if (DebeValidarse(contexto)
                && !await contexto.RequestServices.GetRequiredService<IAntiforgery>().IsRequestValidAsync(contexto).ConfigureAwait(false))
            {
                await EscribirRechazoAsync(contexto).ConfigureAwait(false);
                return;
            }

            await siguiente(contexto).ConfigureAwait(false);
        });

    /// <summary>
    /// Cookies solo por https, salvo en desarrollo: ahí la app llega por el proxy de Angular a
    /// <c>http://localhost:5189</c> y el navegador acepta cookies seguras en <c>localhost</c>.
    /// </summary>
    public static CookieSecurePolicy PoliticaSecure(IHostEnvironment entorno)
    {
        ArgumentNullException.ThrowIfNull(entorno);
        return entorno.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
    }

    public static string? DominioCookie(IConfiguration configuracion)
    {
        ArgumentNullException.ThrowIfNull(configuracion);
        return configuracion[ClaveDominioCookie] is { Length: > 0 } dominio ? dominio : null;
    }

    private static bool DebeValidarse(HttpContext contexto)
    {
        var metodo = contexto.Request.Method;
        if (HttpMethods.IsGet(metodo) || HttpMethods.IsHead(metodo) || HttpMethods.IsOptions(metodo) || HttpMethods.IsTrace(metodo))
        {
            return false;
        }

        // Sin endpoint la respuesta será 404 o 405: no hay nada que proteger.
        return contexto.GetEndpoint() is { } endpoint && endpoint.Metadata.GetMetadata<SinValidacionXsrfAttribute>() is null;
    }

    private static async Task EscribirRechazoAsync(HttpContext contexto)
    {
        contexto.Response.StatusCode = StatusCodes.Status400BadRequest;
        var problemas = contexto.RequestServices.GetRequiredService<IProblemDetailsService>();
        await problemas.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = contexto,
            ProblemDetails =
            {
                Status = StatusCodes.Status400BadRequest,
                Type = TiposProblema.XsrfInvalido,
                Title = "La sesión de seguridad venció. Recarga la página.",
                Detail = "Recarga la página e inténtalo de nuevo.",
            },
        }).ConfigureAwait(false);
    }
}

/// <summary>Emite el par de tokens XSRF para el usuario de <see cref="HttpContext.User"/>.</summary>
public sealed class EmisorXsrf(string? dominio, CookieSecurePolicy politica)
{
    public void Emitir(HttpContext contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        var tokens = contexto.RequestServices.GetRequiredService<IAntiforgery>().GetAndStoreTokens(contexto);
        contexto.Response.Cookies.Append(Xsrf.CookieLegible, tokens.RequestToken!, new CookieOptions
        {
            // La app la lee para copiarla al encabezado; por eso no es HttpOnly.
            HttpOnly = false,
            Secure = politica == CookieSecurePolicy.Always || contexto.Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Path = "/",
            Domain = dominio,
            IsEssential = true,
        });
    }
}
