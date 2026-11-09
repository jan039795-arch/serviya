using Microsoft.AspNetCore.Http;

namespace ServiYa.Comun.Errores;

/// <summary>
/// Títulos en español para los <c>ProblemDetails</c> que genera ASP.NET Core (404, 405, 500...).
/// La app muestra <c>detail</c> o <c>title</c> al usuario, así que no deben llegar en inglés. Los
/// problemas propios de ServiYa (<c>type</c> que empieza con <c>/errores/</c>) conservan su título.
/// </summary>
public static class ProblemasEnEspanol
{
    private const string PrefijoTipoEstandar = "https://tools.ietf.org/html/rfc";

    private static readonly Dictionary<int, string> Titulos = new()
    {
        [StatusCodes.Status400BadRequest] = "La solicitud no es válida.",
        [StatusCodes.Status401Unauthorized] = "Necesitas entrar a tu cuenta.",
        [StatusCodes.Status403Forbidden] = "No tienes permiso para hacer esto.",
        [StatusCodes.Status404NotFound] = "No encontramos lo que buscas.",
        [StatusCodes.Status405MethodNotAllowed] = "Esta operación no está permitida.",
        [StatusCodes.Status409Conflict] = "Los datos cambiaron mientras tanto. Vuelve a intentarlo.",
        [StatusCodes.Status413PayloadTooLarge] = "El contenido es demasiado grande.",
        [StatusCodes.Status415UnsupportedMediaType] = "El formato enviado no es compatible.",
        [StatusCodes.Status422UnprocessableEntity] = "Revisa los datos marcados.",
        [StatusCodes.Status429TooManyRequests] = "Hiciste demasiados intentos. Espera un momento.",
        [StatusCodes.Status500InternalServerError] = "Algo salió mal de nuestro lado. Inténtalo más tarde.",
        [StatusCodes.Status503ServiceUnavailable] = "El servicio no está disponible. Inténtalo más tarde.",
    };

    public static void Traducir(ProblemDetailsContext contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        var problema = contexto.ProblemDetails;
        var esEstandar = problema.Type is null
            || problema.Type.StartsWith(PrefijoTipoEstandar, StringComparison.Ordinal);
        if (esEstandar && problema.Status is { } estado && Titulos.TryGetValue(estado, out var titulo))
        {
            problema.Title = titulo;
        }
    }
}
