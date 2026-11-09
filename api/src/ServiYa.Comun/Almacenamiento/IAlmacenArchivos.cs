using System.Text.RegularExpressions;

namespace ServiYa.Comun.Almacenamiento;

/// <summary>URL firmada y cuándo deja de servir.</summary>
public sealed record UrlFirmada(Uri Url, DateTimeOffset ExpiraEn);

/// <summary>Tamaño, tipo, primeros bytes y versión (ETag) de un objeto ya subido.</summary>
public sealed record ObjetoAlmacenado(long TamanoBytes, string? TipoContenido, byte[] Inicio, string ETag);

/// <summary>
/// Archivos en R2 (research R3). Todo se sube y se revisa en el bucket privado; solo el portafolio
/// aprobado se copia al público. Las claves son GUID y nunca llevan datos personales.
/// </summary>
public interface IAlmacenArchivos
{
    /// <summary>PUT directo desde el navegador al bucket privado, 10 minutos, con el tipo firmado.</summary>
    Task<UrlFirmada> UrlSubidaAsync(string clave, string tipoContenido, CancellationToken cancellationToken);

    /// <summary>Lectura del bucket privado por 5 minutos (cada emisión se registra en la bitácora).</summary>
    Task<UrlFirmada> UrlLecturaAsync(string clave, CancellationToken cancellationToken);

    /// <summary>Dirección pública de un objeto del bucket público.</summary>
    Uri UrlPublica(string clave);

    /// <summary>
    /// Fija un archivo ya validado: lo copia a una clave nueva de <paramref name="carpetaDestino"/> solo
    /// si sigue siendo la misma versión (<paramref name="etag"/>) y borra la clave de subida. Así nadie
    /// puede reemplazarlo con la URL de subida, que sigue viva 10 minutos. Devuelve la clave definitiva,
    /// o <c>null</c> si el archivo cambió después de validarlo (hay que volver a subirlo).
    /// </summary>
    Task<string?> ConservarAsync(string claveSubida, string etag, string carpetaDestino, CancellationToken cancellationToken);

    /// <summary>Copia un objeto ya conservado y aprobado del bucket privado al público.</summary>
    Task CopiarAPublicoAsync(string clave, CancellationToken cancellationToken);

    Task EliminarAsync(Contenedor contenedor, string clave, CancellationToken cancellationToken);

    /// <summary>Lee tamaño, tipo y los primeros bytes del bucket privado; <c>null</c> si no existe.</summary>
    Task<ObjetoAlmacenado?> LeerInicioAsync(string clave, int bytes, CancellationToken cancellationToken);
}

public enum Contenedor
{
    Privado,
    Publico,
}

/// <summary>Claves de objeto: carpeta fija y un GUID v7, por ejemplo <c>documentos/0193…</c>.</summary>
public static partial class ClavesArchivo
{
    public static string Nueva(string carpeta)
    {
        var clave = $"{carpeta}/{Guid.CreateVersion7():N}";
        Validar(clave);
        return clave;
    }

    /// <summary>Rechaza claves que no generó la API (sin "..", espacios, mayúsculas ni datos personales).</summary>
    public static void Validar(string clave)
    {
        ArgumentNullException.ThrowIfNull(clave);
        if (!Formato().IsMatch(clave))
        {
            throw new ArgumentException("La clave del archivo no tiene el formato esperado.", nameof(clave));
        }
    }

    // \z y no $: en .NET "$" también acepta un salto de línea final.
    [GeneratedRegex(@"^[a-z]{3,20}/[0-9a-f]{32}\z", RegexOptions.CultureInvariant)]
    private static partial Regex Formato();
}
