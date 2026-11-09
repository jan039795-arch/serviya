using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.Options;
using ServiYa.Comun.Almacenamiento;
using ServiYa.IntegrationTests.Infraestructura;

namespace ServiYa.IntegrationTests.Almacenamiento;

/// <summary>
/// <see cref="AlmacenR2"/> contra RustFS real (T050): lo mismo que hará el navegador con la URL
/// firmada y lo que hará la API al validar y conservar. Los archivos son bytes ficticios.
/// </summary>
public sealed class AlmacenRustFsTests : IDisposable
{
    /// <summary>Cabecera JPEG y relleno: no es una foto real.</summary>
    private static readonly byte[] JpegFicticio = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, .. "JFIF FICTICIO"u8, .. new byte[256]];

    private readonly HttpClient navegador = new();
    private readonly AlmacenR2 almacen;
    private readonly Amazon.S3.AmazonS3Client s3;

    public AlmacenRustFsTests(ContenedoresServiYa contenedores)
    {
        ArgumentNullException.ThrowIfNull(contenedores);
        var opciones = contenedores.OpcionesAlmacen();
        s3 = AlmacenR2.CrearCliente(opciones);
        almacen = new AlmacenR2(s3, Options.Create(opciones), TimeProvider.System);
    }

    private static CancellationToken Cancelacion => TestContext.Current.CancellationToken;

    [Fact]
    public async Task El_navegador_sube_con_la_URL_firmada_y_la_API_valida_y_conserva_el_archivo()
    {
        var claveSubida = ClavesArchivo.Nueva("subidas");

        (await SubirAsync(claveSubida, JpegFicticio, "image/jpeg")).Should().Be(HttpStatusCode.OK);
        var objeto = await almacen.LeerInicioAsync(claveSubida, ValidadorFirmaArchivo.BytesNecesarios, Cancelacion);

        objeto.Should().NotBeNull();
        objeto!.TamanoBytes.Should().Be(JpegFicticio.Length);
        objeto.TipoContenido.Should().Be("image/jpeg");
        ValidadorFirmaArchivo.EsValido(objeto, "image/jpeg").Should().BeTrue();

        var definitiva = await almacen.ConservarAsync(claveSubida, objeto.ETag, "documentos", Cancelacion);

        definitiva.Should().MatchRegex("^documentos/[0-9a-f]{32}$");
        (await almacen.LeerInicioAsync(claveSubida, 12, Cancelacion)).Should().BeNull("la clave de subida se borra");
        (await almacen.LeerInicioAsync(definitiva!, 12, Cancelacion))!.TamanoBytes.Should().Be(JpegFicticio.Length);
    }

    [Fact]
    public async Task Si_lo_reemplazan_despues_de_validarlo_no_se_conserva()
    {
        var claveSubida = ClavesArchivo.Nueva("subidas");
        await SubirAsync(claveSubida, JpegFicticio, "image/jpeg");
        var validado = await almacen.LeerInicioAsync(claveSubida, 12, Cancelacion);

        // La URL de subida sigue viva 10 minutos: alguien sube otra cosa a la misma clave.
        await SubirAsync(claveSubida, [.. JpegFicticio, 0x01, 0x02], "image/jpeg");

        (await almacen.ConservarAsync(claveSubida, validado!.ETag, "documentos", Cancelacion)).Should().BeNull();
    }

    [Fact]
    public async Task La_URL_firmada_solo_acepta_el_tipo_con_el_que_se_firmo()
    {
        var clave = ClavesArchivo.Nueva("subidas");
        var firmada = await almacen.UrlSubidaAsync(clave, "image/jpeg", Cancelacion);
        using var contenido = new ByteArrayContent(JpegFicticio);
        contenido.Headers.ContentType = new MediaTypeHeaderValue("text/html");

        using var respuesta = await navegador.PutAsync(firmada.Url, contenido, Cancelacion);

        respuesta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await almacen.LeerInicioAsync(clave, 12, Cancelacion)).Should().BeNull();
    }

    [Fact]
    public async Task El_bucket_privado_solo_se_lee_con_URL_firmada()
    {
        var clave = ClavesArchivo.Nueva("documentos");
        await SubirAsync(clave, JpegFicticio, "image/jpeg");
        var lectura = await almacen.UrlLecturaAsync(clave, Cancelacion);

        using var firmada = await navegador.GetAsync(lectura.Url, Cancelacion);
        using var sinFirma = await navegador.GetAsync(new Uri(lectura.Url.GetLeftPart(UriPartial.Path)), Cancelacion);

        firmada.StatusCode.Should().Be(HttpStatusCode.OK);
        (await firmada.Content.ReadAsByteArrayAsync(Cancelacion)).Should().Equal(JpegFicticio);
        sinFirma.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Lo_aprobado_se_copia_al_bucket_publico_y_se_puede_eliminar()
    {
        var clave = ClavesArchivo.Nueva("portafolio");
        await SubirAsync(clave, JpegFicticio, "image/jpeg");

        await almacen.CopiarAPublicoAsync(clave, Cancelacion);
        using (var publica = await navegador.GetAsync(almacen.UrlPublica(clave), Cancelacion))
        {
            publica.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        await almacen.EliminarAsync(Contenedor.Publico, clave, Cancelacion);
        await almacen.EliminarAsync(Contenedor.Privado, clave, Cancelacion);
        using var borrada = await navegador.GetAsync(almacen.UrlPublica(clave), Cancelacion);
        borrada.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await almacen.LeerInicioAsync(clave, 12, Cancelacion)).Should().BeNull();
    }

    public void Dispose()
    {
        navegador.Dispose();
        s3.Dispose();
    }

    /// <summary>Lo que hace <c>Subidor</c> en la app: PUT directo con el tipo firmado.</summary>
    private async Task<HttpStatusCode> SubirAsync(string clave, byte[] bytes, string tipo)
    {
        var firmada = await almacen.UrlSubidaAsync(clave, tipo, Cancelacion);
        using var contenido = new ByteArrayContent(bytes);
        contenido.Headers.ContentType = new MediaTypeHeaderValue(tipo);
        using var respuesta = await navegador.PutAsync(firmada.Url, contenido, Cancelacion);
        return respuesta.StatusCode;
    }
}
