using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using ServiYa.Comun.Seguridad;
using ServiYa.Cuentas.Dominio;
using ServiYa.Cuentas.Servicios;

namespace ServiYa.Cuentas.Endpoints;

/// <summary><c>login</c>, <c>logout</c>, <c>yo</c>, <c>yo/roles/prestador</c> y <c>xsrf</c>.</summary>
internal static class SesionEndpoints
{
    /// <summary>Hash para comparar cuando el correo no existe: así tarda lo mismo que con una cuenta real.</summary>
    private static readonly Lazy<string> HashFicticio = new(() =>
        new PasswordHasher<Usuario>().HashPassword(null!, Guid.NewGuid().ToString()));

    /// <summary>
    /// Revisa la contraseña antes que la verificación del correo: a quien no la sabe no se le dice que la
    /// cuenta existe. Los intentos fallidos cuentan para la pausa de 15 minutos.
    /// </summary>
    public static async Task<Results<Ok<Sesion>, ProblemHttpResult>> EntrarAsync(
        LoginSolicitud solicitud,
        HttpContext contexto,
        UserManager<Usuario> usuarios,
        SignInManager<Usuario> accesos,
        SesionesCuentas sesiones)
    {
        var password = solicitud.Password ?? string.Empty;
        var usuario = await RegistroEndpoints.BuscarAsync(usuarios, solicitud.Email).ConfigureAwait(false);
        if (usuario is null || usuario.Estado == EstadoUsuario.Eliminado || password.Length > ValidacionCuentas.LargoMaximoPassword)
        {
            usuarios.PasswordHasher.VerifyHashedPassword(null!, HashFicticio.Value, password);
            return ProblemasCuentas.Credenciales();
        }

        var resultado = await accesos.CheckPasswordSignInAsync(usuario, password, lockoutOnFailure: true).ConfigureAwait(false);
        if (!resultado.Succeeded)
        {
            return ProblemasCuentas.Credenciales();
        }

        if (!usuario.EmailConfirmed)
        {
            return ProblemasCuentas.CorreoSinVerificar();
        }

        await sesiones.IniciarAsync(contexto, usuario).ConfigureAwait(false);
        return TypedResults.Ok(await sesiones.SesionDeAsync(usuario).ConfigureAwait(false));
    }

    public static async Task<NoContent> SalirAsync(HttpContext contexto, SesionesCuentas sesiones)
    {
        await sesiones.CerrarAsync(contexto).ConfigureAwait(false);
        return TypedResults.NoContent();
    }

    public static async Task<Results<Ok<Sesion>, UnauthorizedHttpResult>> YoAsync(
        ClaimsPrincipal principal,
        UserManager<Usuario> usuarios,
        SesionesCuentas sesiones)
    {
        var usuario = await usuarios.GetUserAsync(principal).ConfigureAwait(false);
        return usuario is null || usuario.Estado == EstadoUsuario.Eliminado
            ? TypedResults.Unauthorized()
            : TypedResults.Ok(await sesiones.SesionDeAsync(usuario).ConfigureAwait(false));
    }

    /// <summary>Un cliente también puede ofrecer servicios con la misma cuenta (FR-002). Repetirlo no cambia nada.</summary>
    public static async Task<Results<NoContent, UnauthorizedHttpResult>> HacersePrestadorAsync(
        HttpContext contexto,
        UserManager<Usuario> usuarios,
        SesionesCuentas sesiones)
    {
        var usuario = await usuarios.GetUserAsync(contexto.User).ConfigureAwait(false);
        if (usuario is null)
        {
            return TypedResults.Unauthorized();
        }

        if (!await usuarios.IsInRoleAsync(usuario, Roles.Prestador).ConfigureAwait(false))
        {
            var agregado = await usuarios.AddToRoleAsync(usuario, Roles.Prestador).ConfigureAwait(false);
            if (!agregado.Succeeded)
            {
                throw new InvalidOperationException("No se pudo agregar el rol de prestador.");
            }

            // La cookie lleva los roles: se emite de nuevo para que el cambio valga desde ya.
            await sesiones.IniciarAsync(contexto, usuario).ConfigureAwait(false);
        }

        return TypedResults.NoContent();
    }

    /// <summary>La app lo pide al arrancar (<c>web/src/app/core/http/xsrf.interceptor.ts</c>).</summary>
    public static NoContent EmitirXsrf(HttpContext contexto, EmisorXsrf xsrf)
    {
        xsrf.Emitir(contexto);
        return TypedResults.NoContent();
    }
}
