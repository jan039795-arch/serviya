using System.Security.Claims;
using ServiYa.Comun.Errores;
using ServiYa.Comun.Seguridad;
using ServiYa.Cuentas.Dominio;
using ServiYa.Cuentas.Servicios;

namespace ServiYa.Api.Seguridad;

/// <summary>
/// Una cuenta suspendida recibe 403 con <c>type: "/errores/cuenta-suspendida"</c> y el motivo en
/// <c>detail</c> (T063), salvo en los endpoints con <see cref="PermitidoASuspendidosAttribute"/>. Se
/// aplica a todo el grupo <c>/api/v1</c>. El estado se lee de la base en cada petición con sesión: así
/// la suspensión vale de inmediato y no hasta que se renueve la cookie.
/// </summary>
internal sealed class CuentaSuspendidaFiltro : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);
        var http = context.HttpContext;
        if (http.GetEndpoint()?.Metadata.GetMetadata<PermitidoASuspendidosAttribute>() is not null
            || !Guid.TryParse(http.User.FindFirstValue(ClaimTypes.NameIdentifier), out var usuarioId))
        {
            return await next(context).ConfigureAwait(false);
        }

        var servicios = http.RequestServices;
        var estado = await servicios.GetRequiredService<ICuentasService>().EstadoDeAsync(usuarioId, http.RequestAborted).ConfigureAwait(false);
        if (estado is null or EstadoUsuario.Eliminado)
        {
            // La cookie sigue viva pero la cuenta ya no existe.
            return TypedResults.Unauthorized();
        }

        if (estado != EstadoUsuario.Suspendido)
        {
            return await next(context).ConfigureAwait(false);
        }

        var motivo = await servicios.GetRequiredService<IConsultaSuspensiones>().MotivoAsync(usuarioId, http.RequestAborted).ConfigureAwait(false);
        return TypedResults.Problem(
            statusCode: StatusCodes.Status403Forbidden,
            type: TiposProblema.CuentaSuspendida,
            title: "Tu cuenta está suspendida.",
            detail: motivo);
    }
}
