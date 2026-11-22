using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using ServiYa.Cuentas.Dominio;
using ServiYa.Cuentas.Servicios;

namespace ServiYa.Cuentas.Endpoints;

/// <summary><c>recuperar</c> y <c>restablecer</c> la contraseña con un código por correo (FR-001).</summary>
internal static class RecuperacionEndpoints
{
    /// <summary>Siempre 204, exista o no la cuenta.</summary>
    public static async Task<NoContent> RecuperarAsync(
        CorreoSolicitud solicitud,
        UserManager<Usuario> usuarios,
        EnvioCodigos envio,
        CancellationToken cancellationToken)
    {
        var usuario = await RegistroEndpoints.BuscarAsync(usuarios, solicitud.Email).ConfigureAwait(false);
        if (usuario is { Estado: not EstadoUsuario.Eliminado })
        {
            await envio.EnviarAsync(usuario, PropositoCodigo.RecuperarContrasena, cancellationToken).ConfigureAwait(false);
        }

        return TypedResults.NoContent();
    }

    /// <summary>
    /// Cambia la contraseña si el código es correcto. Cambia también el sello de seguridad, así que las
    /// demás sesiones se cierran en la siguiente revisión; quita la pausa por intentos fallidos y, como
    /// el código llegó al correo, lo da por verificado.
    /// </summary>
    public static async Task<Results<NoContent, ValidationProblem, ProblemHttpResult>> RestablecerAsync(
        RestablecerSolicitud solicitud,
        UserManager<Usuario> usuarios,
        EnvioCodigos envio,
        CancellationToken cancellationToken)
    {
        // Antes que el código, para que una contraseña corta no gaste un intento.
        if (ValidacionCuentas.ErrorDePassword(solicitud.NuevaPassword) is { } error)
        {
            return ProblemasCuentas.Validacion(new Dictionary<string, string[]>(StringComparer.Ordinal) { ["nuevaPassword"] = [error] });
        }

        var usuario = await RegistroEndpoints.BuscarAsync(usuarios, solicitud.Email).ConfigureAwait(false);
        if (usuario is null || usuario.Estado == EstadoUsuario.Eliminado
            || !await envio.VerificarAsync(usuario, PropositoCodigo.RecuperarContrasena, solicitud.Codigo, cancellationToken).ConfigureAwait(false))
        {
            return ProblemasCuentas.CodigoInvalido();
        }

        usuario.EmailConfirmed = true;
        usuario.LockoutEnd = null;
        usuario.AccessFailedCount = 0;
        var token = await usuarios.GeneratePasswordResetTokenAsync(usuario).ConfigureAwait(false);

        // Guarda la contraseña, el código usado y lo demás juntos; si otra petición usó el código, falla.
        var cambio = await usuarios.ResetPasswordAsync(usuario, token, solicitud.NuevaPassword!).ConfigureAwait(false);
        return cambio.Succeeded ? TypedResults.NoContent() : ProblemasCuentas.CodigoInvalido();
    }
}
