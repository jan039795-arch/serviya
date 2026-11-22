using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using ServiYa.Comun;
using ServiYa.Cuentas.Datos;
using ServiYa.Cuentas.Dominio;
using ServiYa.Cuentas.Servicios;

namespace ServiYa.Cuentas.Endpoints;

/// <summary><c>registro</c>, <c>verificar-correo</c> y <c>reenviar-codigo</c> (FR-001, FR-004, FR-044).</summary>
internal static class RegistroEndpoints
{
    /// <summary>
    /// Crea la cuenta sin verificar, con sus roles y consentimientos en una transacción, y manda el
    /// código. Un menor de 18 se puede registrar: lo que no puede es publicar un perfil ni pagar (FR-004).
    /// </summary>
    public static async Task<Results<Created, ValidationProblem, ProblemHttpResult>> RegistrarAsync(
        RegistroSolicitud solicitud,
        HttpContext contexto,
        UserManager<Usuario> usuarios,
        CuentasDbContext cuentas,
        EnvioCodigos envio,
        TimeProvider reloj,
        CancellationToken cancellationToken)
    {
        var (registro, errores) = ValidacionCuentas.ValidarRegistro(solicitud, reloj.HoyEnMexico());
        if (registro is null)
        {
            return ProblemasCuentas.Validacion(errores);
        }

        if (await usuarios.FindByEmailAsync(registro.Email).ConfigureAwait(false) is not null)
        {
            return ProblemasCuentas.CorreoRegistrado();
        }

        var usuario = new Usuario(registro.Email, registro.NombreCompleto, registro.FechaNacimiento);
        await using (var transaccion = await cuentas.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false))
        {
            IdentityResult creado;
            try
            {
                creado = await usuarios.CreateAsync(usuario, registro.Password).ConfigureAwait(false);
            }
            catch (DbUpdateException error) when (error.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                // Dos registros simultáneos con el mismo correo: el índice único decide.
                return ProblemasCuentas.CorreoRegistrado();
            }

            if (!creado.Succeeded)
            {
                return ResultadoDeIdentity(creado);
            }

            if (!(await usuarios.AddToRolesAsync(usuario, registro.Roles).ConfigureAwait(false)).Succeeded)
            {
                throw new InvalidOperationException("No se pudieron asignar los roles del registro.");
            }

            var ahora = reloj.GetUtcNow();
            var ip = contexto.Connection.RemoteIpAddress;
            cuentas.Consentimientos.AddRange(registro.Aceptaciones.Select(a => new Consentimiento(usuario.Id, a.Documento, a.Version, ahora, ip)));
            await cuentas.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await transaccion.CommitAsync(cancellationToken).ConfigureAwait(false);
        }

        // Fuera de la transacción: si el correo falla, la cuenta queda y se puede pedir otro código.
        await envio.EnviarAsync(usuario, PropositoCodigo.VerificarCorreo, cancellationToken).ConfigureAwait(false);
        return TypedResults.Created();
    }

    /// <summary>Confirma el correo con el código y deja la sesión iniciada.</summary>
    public static async Task<Results<NoContent, ProblemHttpResult>> VerificarCorreoAsync(
        VerificarCorreoSolicitud solicitud,
        HttpContext contexto,
        UserManager<Usuario> usuarios,
        EnvioCodigos envio,
        SesionesCuentas sesiones,
        CancellationToken cancellationToken)
    {
        var usuario = await BuscarAsync(usuarios, solicitud.Email).ConfigureAwait(false);
        if (usuario is null || usuario.EmailConfirmed
            || !await envio.VerificarAsync(usuario, PropositoCodigo.VerificarCorreo, solicitud.Codigo, cancellationToken).ConfigureAwait(false))
        {
            return ProblemasCuentas.CodigoInvalido();
        }

        // Guarda la confirmación y el código usado juntos; si otra petición lo usó antes, falla.
        usuario.EmailConfirmed = true;
        if (!(await usuarios.UpdateAsync(usuario).ConfigureAwait(false)).Succeeded)
        {
            return ProblemasCuentas.CodigoInvalido();
        }

        await sesiones.IniciarAsync(contexto, usuario).ConfigureAwait(false);
        return TypedResults.NoContent();
    }

    /// <summary>Siempre 204: no revela si el correo existe ni si ya se pasó del límite de envíos.</summary>
    public static async Task<NoContent> ReenviarCodigoAsync(
        CorreoSolicitud solicitud,
        UserManager<Usuario> usuarios,
        EnvioCodigos envio,
        CancellationToken cancellationToken)
    {
        var usuario = await BuscarAsync(usuarios, solicitud.Email).ConfigureAwait(false);
        if (usuario is { EmailConfirmed: false, Estado: not EstadoUsuario.Eliminado })
        {
            await envio.EnviarAsync(usuario, PropositoCodigo.VerificarCorreo, cancellationToken).ConfigureAwait(false);
        }

        return TypedResults.NoContent();
    }

    public static async Task<Usuario?> BuscarAsync(UserManager<Usuario> usuarios, string? email) =>
        ValidacionCuentas.NormalizarEmail(email) is { } normalizado
            ? await usuarios.FindByEmailAsync(normalizado).ConfigureAwait(false)
            : null;

    /// <summary>
    /// La validación de arriba ya aplica las reglas de Identity; lo único que puede fallar aquí es un
    /// correo que otra petición registró al mismo tiempo.
    /// </summary>
    private static ProblemHttpResult ResultadoDeIdentity(IdentityResult resultado) =>
        resultado.Errors.Any(e => e.Code is nameof(IdentityErrorDescriber.DuplicateEmail) or nameof(IdentityErrorDescriber.DuplicateUserName))
            ? ProblemasCuentas.CorreoRegistrado()
            : throw new InvalidOperationException(
                "Identity rechazó un registro ya validado: " + string.Join(", ", resultado.Errors.Select(e => e.Code)));
}
