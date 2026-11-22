using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ServiYa.Cuentas.Datos;
using ServiYa.Cuentas.Dominio;

namespace ServiYa.IntegrationTests.Infraestructura;

/// <summary>Usuario creado para una prueba, con su contraseña.</summary>
public sealed record UsuarioDePrueba(Guid Id, string Email, string Password);

/// <summary>
/// Clientes HTTP de las pruebas (T046): manejan las cookies como un navegador, mandan
/// <c>X-XSRF-TOKEN</c> con el valor de la cookie <c>XSRF-TOKEN</c> y cada uno sale de una IP propia,
/// para que el límite de 5 por minuto de login y códigos no mezcle pruebas.
/// </summary>
public static partial class ClientesDePrueba
{
    public const string PasswordDePrueba = "contrasena-de-prueba";

    /// <summary>Encabezado que solo entiende <see cref="IpDePruebaFiltro"/>; la API real lo ignora.</summary>
    internal const string EncabezadoIp = "X-Ip-Prueba";

    private static readonly Uri Base = new("https://localhost");

    /// <summary>Cliente anónimo. Sin <paramref name="enviarXsrf"/> nunca manda el encabezado.</summary>
    public static HttpClient CrearCliente(this ServiYaFactory fabrica, string? ip = null, bool enviarXsrf = true)
    {
        ArgumentNullException.ThrowIfNull(fabrica);
        return fabrica.CreateDefaultClient(Base, new ManejadorNavegador(ip ?? IpAlAzar(), enviarXsrf));
    }

    /// <summary>Crea un usuario ya verificado, con los roles dados, directamente en la base.</summary>
    public static Task<UsuarioDePrueba> CrearUsuarioAsync(this ServiYaFactory fabrica, params string[] roles) =>
        fabrica.CrearUsuarioAsync(new DateOnly(1990, 1, 1), roles);

    /// <inheritdoc cref="CrearUsuarioAsync(ServiYaFactory, string[])"/>
    public static async Task<UsuarioDePrueba> CrearUsuarioAsync(
        this ServiYaFactory fabrica,
        DateOnly fechaNacimiento,
        params string[] roles)
    {
        ArgumentNullException.ThrowIfNull(fabrica);
        ArgumentNullException.ThrowIfNull(roles);
        var email = $"persona-{Guid.NewGuid():N}@correo.test";
        var usuario = new Usuario(email, "Persona Ficticia", fechaNacimiento) { EmailConfirmed = true };
        await using var ambito = fabrica.CrearAmbito();
        var usuarios = ambito.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
        Revisar(await usuarios.CreateAsync(usuario, PasswordDePrueba));
        if (roles.Length > 0)
        {
            Revisar(await usuarios.AddToRolesAsync(usuario, roles));
        }

        return new UsuarioDePrueba(usuario.Id, email, PasswordDePrueba);
    }

    /// <summary>Suspende la cuenta directamente en la base (los endpoints de suspensión llegan con US7).</summary>
    public static async Task SuspenderAsync(this ServiYaFactory fabrica, Guid usuarioId)
    {
        ArgumentNullException.ThrowIfNull(fabrica);
        await using var ambito = fabrica.CrearAmbito();
        var cuentas = ambito.ServiceProvider.GetRequiredService<CuentasDbContext>();
        var usuario = await cuentas.Users.SingleAsync(u => u.Id == usuarioId, TestContext.Current.CancellationToken);
        usuario.Suspender();
        await cuentas.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>Cliente con la sesión iniciada por la API (login), listo para modificar datos.</summary>
    public static async Task<HttpClient> ClienteConSesionAsync(this ServiYaFactory fabrica, UsuarioDePrueba usuario)
    {
        ArgumentNullException.ThrowIfNull(usuario);
        var cliente = fabrica.CrearCliente();
        await cliente.PrepararXsrfAsync();
        using var login = await cliente.PostAsJsonAsync(
            new Uri("/api/v1/cuentas/login", UriKind.Relative),
            new { email = usuario.Email, password = usuario.Password },
            TestContext.Current.CancellationToken);
        login.EnsureSuccessStatusCode();
        return cliente;
    }

    /// <summary>Pide la cookie <c>XSRF-TOKEN</c>, como hace la app al arrancar.</summary>
    public static async Task PrepararXsrfAsync(this HttpClient cliente)
    {
        ArgumentNullException.ThrowIfNull(cliente);
        using var respuesta = await cliente.GetAsync(new Uri("/api/v1/cuentas/xsrf", UriKind.Relative), TestContext.Current.CancellationToken);
        respuesta.EnsureSuccessStatusCode();
    }

    /// <summary>El último código de 6 dígitos que se le mandó a <paramref name="email"/>, si hay.</summary>
    public static string? UltimoCodigo(this ServiYaFactory fabrica, string email)
    {
        ArgumentNullException.ThrowIfNull(fabrica);
        var correo = fabrica.Correos.Recientes.FirstOrDefault(c => string.Equals(c.Para, email, StringComparison.OrdinalIgnoreCase));
        return correo is null ? null : SeisDigitos().Match(correo.Asunto).Value;
    }

    public static int CorreosPara(this ServiYaFactory fabrica, string email)
    {
        ArgumentNullException.ThrowIfNull(fabrica);
        return fabrica.Correos.Recientes.Count(c => string.Equals(c.Para, email, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Una IP de la red de pruebas 198.18.0.0/15 (RFC 2544).</summary>
    public static string IpAlAzar() => string.Create(
        CultureInfo.InvariantCulture,
        $"198.{18 + RandomNumberGenerator.GetInt32(2)}.{RandomNumberGenerator.GetInt32(256)}.{RandomNumberGenerator.GetInt32(1, 255)}");

    private static void Revisar(IdentityResult resultado)
    {
        if (!resultado.Succeeded)
        {
            throw new InvalidOperationException(string.Join("; ", resultado.Errors.Select(e => e.Code)));
        }
    }

    [GeneratedRegex(@"\d{6}")]
    private static partial Regex SeisDigitos();

    /// <summary>
    /// Cookies como un navegador y el encabezado XSRF. Guarda nombre y valor sin revisar el vencimiento:
    /// la API usa el reloj falso de la fábrica y <c>CookieContainer</c> usaría la hora real. El
    /// vencimiento de la sesión lo revisa la API con su propio reloj. Una cookie se borra cuando la API
    /// la manda vacía (así la borra ASP.NET Core).
    /// </summary>
    private sealed class ManejadorNavegador(string ip, bool enviarXsrf) : DelegatingHandler
    {
        private readonly Dictionary<string, string> cookies = new(StringComparer.Ordinal);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Headers.Add(EncabezadoIp, ip);
            if (cookies.Count > 0)
            {
                request.Headers.Add("Cookie", string.Join("; ", cookies.Select(c => $"{c.Key}={c.Value}")));
            }

            if (enviarXsrf && !EsSeguro(request.Method) && !request.Headers.Contains("X-XSRF-TOKEN")
                && cookies.TryGetValue("XSRF-TOKEN", out var token))
            {
                request.Headers.Add("X-XSRF-TOKEN", Uri.UnescapeDataString(token));
            }

            var respuesta = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (respuesta.Headers.TryGetValues("Set-Cookie", out var nuevas))
            {
                foreach (var cookie in nuevas)
                {
                    Guardar(cookie);
                }
            }

            return respuesta;
        }

        private static bool EsSeguro(HttpMethod metodo) =>
            metodo == HttpMethod.Get || metodo == HttpMethod.Head || metodo == HttpMethod.Options;

        private void Guardar(string setCookie)
        {
            var par = setCookie.Split(';', 2)[0];
            var separador = par.IndexOf('=', StringComparison.Ordinal);
            var nombre = par[..separador].Trim();
            var valor = par[(separador + 1)..].Trim();
            if (valor.Length == 0)
            {
                cookies.Remove(nombre);
            }
            else
            {
                cookies[nombre] = valor;
            }
        }
    }
}

/// <summary>
/// Solo en las pruebas: toma la IP del encabezado <see cref="ClientesDePrueba.EncabezadoIp"/> antes que
/// cualquier middleware de la API, como haría <c>ForwardedHeaders</c> detrás del proxy del hospedaje.
/// </summary>
internal sealed class IpDePruebaFiltro : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        app.Use((contexto, siguiente) =>
        {
            if (IPAddress.TryParse(contexto.Request.Headers[ClientesDePrueba.EncabezadoIp], out var ip))
            {
                contexto.Connection.RemoteIpAddress = ip;
            }

            return siguiente(contexto);
        });
        next(app);
    };
}
