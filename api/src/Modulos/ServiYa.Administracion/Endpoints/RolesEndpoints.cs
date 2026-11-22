using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using ServiYa.Comun.Bitacora;
using ServiYa.Comun.Errores;
using ServiYa.Comun.Seguridad;
using ServiYa.Cuentas.Servicios;

namespace ServiYa.Administracion.Endpoints;

/// <summary>
/// Otorgar y retirar el rol de administrador (T064, FR-003): solo un administrador, nunca a sí mismo
/// y sin dejar la plataforma sin administradores. Cada cambio queda en la bitácora.
/// </summary>
internal static class RolesEndpoints
{
    public static void MapearRolesAdmin(this IEndpointRouteBuilder admin)
    {
        admin.MapPost("/usuarios/{id:guid}/roles/admin", OtorgarAsync);
        admin.MapDelete("/usuarios/{id:guid}/roles/admin", RetirarAsync);
    }

    private static Task<Results<NoContent, ProblemHttpResult>> OtorgarAsync(
        Guid id,
        HttpContext contexto,
        ICuentasService cuentas,
        IBitacora bitacora,
        CancellationToken cancellationToken) =>
        CambiarAsync(id, contexto, bitacora, AccionesBitacora.OtorgarRolAdmin, cuentas.OtorgarAdminAsync, cancellationToken);

    private static Task<Results<NoContent, ProblemHttpResult>> RetirarAsync(
        Guid id,
        HttpContext contexto,
        ICuentasService cuentas,
        IBitacora bitacora,
        CancellationToken cancellationToken) =>
        CambiarAsync(id, contexto, bitacora, AccionesBitacora.RetirarRolAdmin, cuentas.RetirarAdminAsync, cancellationToken);

    private static async Task<Results<NoContent, ProblemHttpResult>> CambiarAsync(
        Guid id,
        HttpContext contexto,
        IBitacora bitacora,
        string accion,
        Func<Guid, CancellationToken, Task<ResultadoCambioAdmin>> cambiar,
        CancellationToken cancellationToken)
    {
        var actor = Guid.Parse(contexto.User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        if (actor == id)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                type: TiposProblema.RolPropio,
                title: "No puedes cambiar tu propio rol de administrador.",
                detail: "Pide a otro administrador que lo haga.");
        }

        var resultado = await cambiar(id, cancellationToken).ConfigureAwait(false);
        if (resultado == ResultadoCambioAdmin.Hecho)
        {
            await bitacora.RegistrarAsync(
                new EntradaBitacora(actor, accion, EntidadesBitacora.Usuario, id, contexto.Connection.RemoteIpAddress),
                cancellationToken).ConfigureAwait(false);
        }

        return Respuesta(resultado);
    }

    private static Results<NoContent, ProblemHttpResult> Respuesta(ResultadoCambioAdmin resultado) => resultado switch
    {
        ResultadoCambioAdmin.Hecho or ResultadoCambioAdmin.SinCambio => TypedResults.NoContent(),
        ResultadoCambioAdmin.NoExiste => TypedResults.Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "No encontramos esa cuenta."),
        ResultadoCambioAdmin.CorreoSinVerificar => TypedResults.Problem(
            statusCode: StatusCodes.Status422UnprocessableEntity,
            type: TiposProblema.CorreoSinVerificar,
            title: "Esa cuenta todavía no verifica su correo.",
            detail: "Solo una cuenta verificada puede ser administradora."),
        ResultadoCambioAdmin.UltimoAdmin => TypedResults.Problem(
            statusCode: StatusCodes.Status409Conflict,
            type: TiposProblema.UltimoAdmin,
            title: "Es el último administrador activo.",
            detail: "Otorga el rol a otra cuenta antes de retirarlo."),
        _ => throw new ArgumentOutOfRangeException(nameof(resultado), resultado, null),
    };
}
