using System.Globalization;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace ServiYa.Api.Seguridad;

/// <summary>
/// Límites de uso (T053). Responden 429 como <c>ProblemDetails</c> con <c>Retry-After</c>.
/// La IP sale de <c>RemoteIpAddress</c>: detrás del proxy del hospedaje hay que configurar
/// <c>ForwardedHeaders</c> con sus redes conocidas al desplegar (T147), o todos compartirían el límite.
/// </summary>
internal static class LimitesUso
{
    /// <summary>Login, registro, verificación, reenvío y recuperación: 5 por minuto por IP.</summary>
    public const string LoginCodigos = "login-codigos";

    /// <summary>Mensajes del chat: 30 por minuto por usuario.</summary>
    public const string Mensajes = "mensajes";

    private const int PeticionesDeLoginPorMinuto = 5;
    private const int MensajesPorMinuto = 30;
    private static readonly TimeSpan Ventana = TimeSpan.FromMinutes(1);

    public static IServiceCollection AgregarLimitesUso(this IServiceCollection services) =>
        services.AddRateLimiter(opciones =>
        {
            opciones.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            opciones.AddPolicy(LoginCodigos, contexto =>
                RateLimitPartition.GetFixedWindowLimiter(ClaveIp(contexto), _ => VentanaFija(PeticionesDeLoginPorMinuto)));
            opciones.AddPolicy(Mensajes, contexto =>
                RateLimitPartition.GetFixedWindowLimiter(ClaveUsuario(contexto), _ => VentanaFija(MensajesPorMinuto)));
            opciones.OnRejected = EscribirRechazoAsync;
        });

    private static FixedWindowRateLimiterOptions VentanaFija(int permitidas) => new()
    {
        PermitLimit = permitidas,
        Window = Ventana,
        QueueLimit = 0,
    };

    private static string ClaveIp(HttpContext contexto) =>
        "ip:" + (contexto.Connection.RemoteIpAddress?.ToString() ?? "desconocida");

    private static string ClaveUsuario(HttpContext contexto) =>
        contexto.User.FindFirstValue(ClaimTypes.NameIdentifier) is { } usuario ? "usuario:" + usuario : ClaveIp(contexto);

    private static async ValueTask EscribirRechazoAsync(OnRejectedContext rechazo, CancellationToken cancellationToken)
    {
        var http = rechazo.HttpContext;
        var detalle = "Espera un momento e inténtalo de nuevo.";
        if (rechazo.Lease.TryGetMetadata(MetadataName.RetryAfter, out var espera))
        {
            var segundos = Math.Max(1, (int)Math.Ceiling(espera.TotalSeconds));
            http.Response.Headers.RetryAfter = segundos.ToString(CultureInfo.InvariantCulture);
            detalle = $"Espera {segundos} segundos e inténtalo de nuevo.";
        }

        var problemas = http.RequestServices.GetRequiredService<IProblemDetailsService>();
        await problemas.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = http,
            ProblemDetails = { Status = StatusCodes.Status429TooManyRequests, Detail = detalle },
        }).ConfigureAwait(false);
    }
}
