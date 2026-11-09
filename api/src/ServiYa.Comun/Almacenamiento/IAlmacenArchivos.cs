using System.Text.RegularExpressions;

namespace ServiYa.Comun.Almacenamiento;

/// <summary>URL firmada y cuándo deja de servir.</summary>
public sealed record UrlFirmada(Uri Url, DateTimeOffset ExpiraEn);

/// <summary>Tamaño, tipo y primeros bytes de un objeto ya subido.</summary>
public sealed record ObjetoAlmacenado(long TamanoBytes, string? TipoContenido, byte[] Inicio);

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

    /// <summary>Copia un objeto aprobado del bucket privado al público.</summary>
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

    [GeneratedRegex("^[a-z]{3,20}/[0-9a-f]{32}$", RegexOptions.CultureInvariant)]
    private static partial Regex Formato();
}
