using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using ServiYa.Comun;
using ServiYa.Comun.Seguridad;
using ServiYa.Cuentas.Dominio;

namespace ServiYa.Cuentas.Servicios;

/// <summary>Sesión que ve la app (contracts/api.md, <c>web/src/app/core/api/modelos.ts</c>).</summary>
public sealed record Sesion(Guid Id, string NombreCompleto, string Email, IReadOnlyList<string> Roles, string Estado, bool EsMayorDeEdad);

/// <summary>Inicia y cierra la sesión por cookie y arma <see cref="Sesion"/>.</summary>
internal sealed class SesionesCuentas(
    UserManager<Usuario> usuarios,
    SignInManager<Usuario> accesos,
    EmisorXsrf xsrf,
    TimeProvider reloj)
{
    /// <summary>
    /// Emite la cookie de sesión (persistente, 14 días con renovación) y un token XSRF nuevo para este
    /// usuario: el anterior estaba ligado a la sesión anónima y ya no serviría.
    /// </summary>
    public async Task IniciarAsync(HttpContext contexto, Usuario usuario)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        var principal = await accesos.CreateUserPrincipalAsync(usuario).ConfigureAwait(false);
        await contexto.SignInAsync(IdentityConstants.ApplicationScheme, principal, new AuthenticationProperties { IsPersistent = true })
            .ConfigureAwait(false);
        contexto.User = principal;
        xsrf.Emitir(contexto);
    }

    public async Task CerrarAsync(HttpContext contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        await contexto.SignOutAsync(IdentityConstants.ApplicationScheme).ConfigureAwait(false);
        contexto.User = new ClaimsPrincipal(new ClaimsIdentity());
        xsrf.Emitir(contexto);
    }

    public async Task<Sesion> SesionDeAsync(Usuario usuario)
    {
        ArgumentNullException.ThrowIfNull(usuario);
        var suyos = await usuarios.GetRolesAsync(usuario).ConfigureAwait(false);
        return new Sesion(
            usuario.Id,
            usuario.NombreCompleto,
            usuario.Email!,
            [.. Roles.Todos.Where(suyos.Contains)],
            usuario.Estado.ToString(),
            usuario.EsMayorDeEdad(reloj.HoyEnMexico()));
    }
}
