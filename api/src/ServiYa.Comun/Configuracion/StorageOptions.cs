using System.ComponentModel.DataAnnotations;

namespace ServiYa.Comun.Configuracion;

/// <summary>
/// Almacenamiento de archivos compatible con S3: Cloudflare R2 en producción y RustFS en desarrollo
/// (research R3). Es <c>class</c> y no <c>record</c> para que <c>ToString()</c> no imprima las claves.
/// </summary>
public sealed class StorageOptions : IOpcionesConSeccion
{
    public static string Seccion => "Storage";

    /// <summary>URL del servicio S3 (por ejemplo <c>https://&lt;cuenta&gt;.r2.cloudflarestorage.com</c>).</summary>
    [Required, Url]
    public string Endpoint { get; init; } = string.Empty;

    [Required]
    public string AccessKey { get; init; } = string.Empty;

    [Required]
    public string SecretKey { get; init; } = string.Empty;

    /// <summary>Documentos, selfies y archivos en revisión. Nunca tiene acceso público.</summary>
    [Required]
    public string BucketPrivado { get; init; } = "serviya-privado";

    /// <summary>Portafolio aprobado, servido por el dominio público con caché.</summary>
    [Required]
    public string BucketPublico { get; init; } = "serviya-publico";

    /// <summary>URL base con la que se arman las direcciones públicas del bucket público.</summary>
    [Required, Url]
    public string UrlPublica { get; init; } = string.Empty;
}
