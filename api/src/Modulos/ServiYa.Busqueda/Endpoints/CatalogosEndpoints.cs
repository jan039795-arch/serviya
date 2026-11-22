using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using ServiYa.Busqueda.Servicios;
using ServiYa.Comun.Errores;

namespace ServiYa.Busqueda.Endpoints;

/// <summary>Ubicación del cliente; llega en el cuerpo y no en la URL porque es un dato personal (principio I).</summary>
public sealed record UbicarSolicitud(double? Lat, double? Lng);

/// <summary>
/// <c>/api/v1/catalogos</c> (contracts/api.md, T067), públicos. Las listas se pueden guardar en caché
/// una hora; la respuesta de <c>ubicar</c> no, porque deja ver dónde está la persona.
/// </summary>
internal static class CatalogosEndpoints
{
    private const string CacheCatalogo = "public, max-age=3600";

    public static void MapearCatalogos(this IEndpointRouteBuilder api)
    {
        var catalogos = api.MapGroup("/catalogos").AllowAnonymous().WithTags("Catálogos");
        catalogos.MapGet("/oficios", OficiosAsync);
        catalogos.MapGet("/municipios", MunicipiosAsync);
        catalogos.MapPost("/municipios/ubicar", UbicarAsync);
    }

    private static async Task<Ok<IReadOnlyList<OficioCatalogo>>> OficiosAsync(
        HttpContext contexto,
        ICatalogosService catalogos,
        CancellationToken cancellationToken)
    {
        contexto.Response.Headers.CacheControl = CacheCatalogo;
        return TypedResults.Ok(await catalogos.OficiosAsync(cancellationToken).ConfigureAwait(false));
    }

    private static async Task<Ok<IReadOnlyList<MunicipioCatalogo>>> MunicipiosAsync(
        HttpContext contexto,
        ICatalogosService catalogos,
        CancellationToken cancellationToken)
    {
        contexto.Response.Headers.CacheControl = CacheCatalogo;
        return TypedResults.Ok(await catalogos.MunicipiosAsync(cancellationToken).ConfigureAwait(false));
    }

    private static async Task<Results<Ok<MunicipioCatalogo>, ValidationProblem, ProblemHttpResult>> UbicarAsync(
        UbicarSolicitud solicitud,
        ICatalogosService catalogos,
        CancellationToken cancellationToken)
    {
        var errores = new Dictionary<string, string[]>(StringComparer.Ordinal);
        if (solicitud.Lat is not (>= -90 and <= 90))
        {
            errores["lat"] = ["La latitud debe estar entre -90 y 90."];
        }

        if (solicitud.Lng is not (>= -180 and <= 180))
        {
            errores["lng"] = ["La longitud debe estar entre -180 y 180."];
        }

        if (errores.Count > 0)
        {
            return TypedResults.ValidationProblem(errores, title: "Ubicación no válida.");
        }

        var municipio = await catalogos.UbicarAsync(solicitud.Lat!.Value, solicitud.Lng!.Value, cancellationToken).ConfigureAwait(false);
        return municipio is null
            ? TypedResults.Problem(
                statusCode: StatusCodes.Status404NotFound,
                type: TiposProblema.FueraDeZona,
                title: "Tu ubicación está fuera de la zona de servicio.",
                detail: "Elige tu municipio de la lista.")
            : TypedResults.Ok(municipio);
    }
}
