using System.Net;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using ServiYa.Comun.Configuracion;

namespace ServiYa.Comun.Almacenamiento;

/// <summary>
/// <see cref="IAlmacenArchivos"/> sobre la API S3 de Cloudflare R2 (RustFS en desarrollo y pruebas).
/// Las URL firmadas se calculan en la API, sin llamar al servicio.
/// </summary>
public sealed class AlmacenR2(IAmazonS3 s3, IOptions<StorageOptions> opciones, TimeProvider reloj) : IAlmacenArchivos
{
    public static readonly TimeSpan VigenciaSubida = TimeSpan.FromMinutes(10);
    public static readonly TimeSpan VigenciaLectura = TimeSpan.FromMinutes(5);

    private StorageOptions Configuracion => opciones.Value;

    /// <summary>Cliente S3 para R2: estilo de ruta y sin sumas de verificación que R2 no acepta.</summary>
    public static AmazonS3Client CrearCliente(StorageOptions configuracion)
    {
        ArgumentNullException.ThrowIfNull(configuracion);
        return new AmazonS3Client(
            new BasicAWSCredentials(configuracion.AccessKey, configuracion.SecretKey),
            new AmazonS3Config
            {
                ServiceURL = configuracion.Endpoint,
                ForcePathStyle = true,
                AuthenticationRegion = configuracion.Region,
                RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
                ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED,
            });
    }

    public Task<UrlFirmada> UrlSubidaAsync(string clave, string tipoContenido, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tipoContenido);
        return FirmarAsync(clave, HttpVerb.PUT, VigenciaSubida, tipoContenido);
    }

    public Task<UrlFirmada> UrlLecturaAsync(string clave, CancellationToken cancellationToken) =>
        FirmarAsync(clave, HttpVerb.GET, VigenciaLectura, tipoContenido: null);

    public Uri UrlPublica(string clave)
    {
        ClavesArchivo.Validar(clave);
        return new Uri($"{Configuracion.UrlPublica.TrimEnd('/')}/{clave}");
    }

    public async Task CopiarAPublicoAsync(string clave, CancellationToken cancellationToken)
    {
        ClavesArchivo.Validar(clave);
        await s3.CopyObjectAsync(
            new CopyObjectRequest
            {
                SourceBucket = Configuracion.BucketPrivado,
                SourceKey = clave,
                DestinationBucket = Configuracion.BucketPublico,
                DestinationKey = clave,
            },
            cancellationToken).ConfigureAwait(false);
    }

    public async Task EliminarAsync(Contenedor contenedor, string clave, CancellationToken cancellationToken)
    {
        ClavesArchivo.Validar(clave);
        var bucket = contenedor == Contenedor.Publico ? Configuracion.BucketPublico : Configuracion.BucketPrivado;
        await s3.DeleteObjectAsync(bucket, clave, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ObjetoAlmacenado?> LeerInicioAsync(string clave, int bytes, CancellationToken cancellationToken)
    {
        ClavesArchivo.Validar(clave);
        ArgumentOutOfRangeException.ThrowIfLessThan(bytes, 1);
        GetObjectMetadataResponse metadatos;
        try
        {
            metadatos = await s3.GetObjectMetadataAsync(Configuracion.BucketPrivado, clave, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (AmazonS3Exception error) when (error.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        if (metadatos.ContentLength == 0)
        {
            return new ObjetoAlmacenado(0, metadatos.Headers.ContentType, []);
        }

        var peticion = new GetObjectRequest
        {
            BucketName = Configuracion.BucketPrivado,
            Key = clave,
            ByteRange = new ByteRange(0, Math.Min(bytes, metadatos.ContentLength) - 1),
        };
        using var respuesta = await s3.GetObjectAsync(peticion, cancellationToken).ConfigureAwait(false);
        using var memoria = new MemoryStream();
        await respuesta.ResponseStream.CopyToAsync(memoria, cancellationToken).ConfigureAwait(false);
        return new ObjetoAlmacenado(metadatos.ContentLength, metadatos.Headers.ContentType, memoria.ToArray());
    }

    private async Task<UrlFirmada> FirmarAsync(string clave, HttpVerb metodo, TimeSpan vigencia, string? tipoContenido)
    {
        ClavesArchivo.Validar(clave);
        var expiraEn = reloj.GetUtcNow() + vigencia;
        var peticion = new GetPreSignedUrlRequest
        {
            BucketName = Configuracion.BucketPrivado,
            Key = clave,
            Verb = metodo,
            Expires = expiraEn.UtcDateTime,
            Protocol = Configuracion.Endpoint.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                ? Protocol.HTTP
                : Protocol.HTTPS,
        };
        if (tipoContenido is not null)
        {
            // Firmado: el navegador debe subir exactamente con este tipo.
            peticion.ContentType = tipoContenido;
        }

        var url = await s3.GetPreSignedURLAsync(peticion).ConfigureAwait(false);
        return new UrlFirmada(new Uri(url), expiraEn);
    }
}
