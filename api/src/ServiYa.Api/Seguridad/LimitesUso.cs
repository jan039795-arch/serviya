using System.Globalization;
using System.Net;
using System.Net.Sockets;
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

    /// <summary>Límite general de todo endpoint, por usuario o por red: sobra para uso normal de la app.</summary>
    public const int PeticionesGeneralesPorMinuto = 300;

    private const int PeticionesDeLoginPorMinuto = 5;
    private const int MensajesPorMinuto = 30;

    /// <summary>Un hogar o un celular recibe una red /64 completa de IPv6: se limita por red, no por dirección.</summary>
    private const int BytesPrefijoIpv6 = 8;

    private static readonly TimeSpan Ventana = TimeSpan.FromMinutes(1);

    public static IServiceCollection AgregarLimitesUso(this IServiceCollection services) =>
        services.AddRateLimiter(opciones =>
        {
            opciones.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            opciones.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(contexto =>
                RateLimitPartition.GetFixedWindowLimiter(
                    "general:" + ClaveUsuario(contexto),
                    _ => VentanaFija(PeticionesGeneralesPorMinuto)));
            opciones.AddPolicy(LoginCodigos, contexto =>
                RateLimitPartition.GetFixedWindowLimiter(ClaveIp(contexto), _ => VentanaFija(PeticionesDeLoginPorMinuto)));
            opciones.AddPolicy(Mensajes, contexto =>
                RateLimitPartition.GetFixedWindowLimiter(ClaveUsuario(contexto), _ => VentanaFija(MensajesPorMinuto)));
            opciones.OnRejected = EscribirRechazoAsync;
        });

    /// <summary>
    /// Clave de la IP para los límites. IPv4 tal cual (también la que llega como IPv6 mapeada);
    /// IPv6 por su prefijo /64, porque quien tiene una red /64 puede usar 2^64 direcciones.
    /// </summary>
    internal static string ClaveIp(IPAddress? ip)
    {
        if (ip is null)
        {
            return "ip:desconocida";
        }

        if (ip.IsIPv4MappedToIPv6)
        {
            ip = ip.MapToIPv4();
        }

        if (ip.AddressFamily != AddressFamily.InterNetworkV6)
        {
            return "ip:" + ip;
        }

        var bytes = ip.GetAddressBytes();
        Array.Clear(bytes, BytesPrefijoIpv6, bytes.Length - BytesPrefijoIpv6);
        return "ip6:" + new IPAddress(bytes) + "/64";
    }

    private static FixedWindowRateLimiterOptions VentanaFija(int permitidas) => new()
    {
        PermitLimit = permitidas,
        Window = Ventana,
        QueueLimit = 0,
    };

    private static string ClaveIp(HttpContext contexto) => ClaveIp(contexto.Connection.RemoteIpAddress);

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
