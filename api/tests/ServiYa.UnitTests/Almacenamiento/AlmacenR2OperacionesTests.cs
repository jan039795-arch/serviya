using System.Net;
using System.Text;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using ServiYa.Comun.Almacenamiento;
using ServiYa.Comun.Configuracion;

namespace ServiYa.UnitTests.Almacenamiento;

/// <summary>
/// Qué pide <see cref="AlmacenR2"/> a S3 al conservar y leer archivos (revisión de seguridad de la
/// Etapa 3). Contra un servidor real se prueba con RustFS en integración (requiere Docker).
/// </summary>
public sealed class AlmacenR2OperacionesTests
{
    private static readonly StorageOptions Opciones = new()
    {
        Endpoint = "https://cuenta-ficticia.r2.cloudflarestorage.com",
        AccessKey = "clave-ficticia",
        SecretKey = "secreto-ficticio",
        UrlPublica = "https://archivos.serviya.test/",
    };

    private readonly IAmazonS3 s3 = Substitute.For<IAmazonS3>();
    private readonly AlmacenR2 almacen;
    private readonly string claveSubida = ClavesArchivo.Nueva("subidas");

    public AlmacenR2OperacionesTests() =>
        almacen = new AlmacenR2(s3, Options.Create(Opciones), new FakeTimeProvider(DateTimeOffset.UtcNow));

    private static CancellationToken Cancelacion => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Conservar_copia_solo_la_version_validada_a_una_clave_nueva_y_borra_la_de_subida()
    {
        var claveDefinitiva = await almacen.ConservarAsync(claveSubida, "\"etag-validado\"", "documentos", Cancelacion);

        claveDefinitiva.Should().MatchRegex("^documentos/[0-9a-f]{32}$").And.NotBe(claveSubida);
        await s3.Received(1).CopyObjectAsync(
            Arg.Is<CopyObjectRequest>(c =>
                c.SourceBucket == "serviya-privado" && c.SourceKey == claveSubida
                && c.DestinationBucket == "serviya-privado" && c.DestinationKey == claveDefinitiva
                && c.ETagToMatch == "\"etag-validado\""),
            Arg.Any<CancellationToken>());
        await s3.Received(1).DeleteObjectAsync("serviya-privado", claveSubida, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Si_el_archivo_cambio_despues_de_validarlo_no_se_conserva()
    {
        s3.CopyObjectAsync(Arg.Any<CopyObjectRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new AmazonS3Exception("cambió") { StatusCode = HttpStatusCode.PreconditionFailed });

        var claveDefinitiva = await almacen.ConservarAsync(claveSubida, "\"etag-validado\"", "documentos", Cancelacion);

        claveDefinitiva.Should().BeNull();
        await s3.DidNotReceive().DeleteObjectAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Leer_el_inicio_devuelve_tamano_tipo_bytes_y_ETag_de_la_misma_version()
    {
        var metadatos = new GetObjectMetadataResponse { ContentLength = 2048, ETag = "\"v1\"" };
        metadatos.Headers.ContentType = "image/jpeg";
        s3.GetObjectMetadataAsync("serviya-privado", claveSubida, Arg.Any<CancellationToken>()).Returns(metadatos);
        s3.GetObjectAsync(Arg.Any<GetObjectRequest>(), Arg.Any<CancellationToken>())
            .Returns(_ => new GetObjectResponse { ResponseStream = new MemoryStream(Encoding.ASCII.GetBytes("123456789012")) });

        var objeto = await almacen.LeerInicioAsync(claveSubida, 12, Cancelacion);

        objeto.Should().BeEquivalentTo(new { TamanoBytes = 2048L, TipoContenido = "image/jpeg", ETag = "\"v1\"" });
        objeto!.Inicio.Should().HaveCount(12);
        await s3.Received(1).GetObjectAsync(
            Arg.Is<GetObjectRequest>(g => g.EtagToMatch == "\"v1\"" && g.ByteRange.Start == 0 && g.ByteRange.End == 11),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.PreconditionFailed)]
    public async Task Si_no_existe_o_cambio_entre_lecturas_devuelve_null(HttpStatusCode estado)
    {
        s3.GetObjectMetadataAsync("serviya-privado", claveSubida, Arg.Any<CancellationToken>())
            .Returns(new GetObjectMetadataResponse { ContentLength = 10, ETag = "\"v1\"" });
        s3.GetObjectAsync(Arg.Any<GetObjectRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new AmazonS3Exception("no") { StatusCode = estado });

        (await almacen.LeerInicioAsync(claveSubida, 12, Cancelacion)).Should().BeNull();
    }
}
