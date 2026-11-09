using System.Web;
using Amazon.S3;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using ServiYa.Comun.Almacenamiento;
using ServiYa.Comun.Configuracion;

namespace ServiYa.UnitTests.Almacenamiento;

/// <summary>
/// URL firmadas de R2 (T050). Se calculan sin conectarse; la copia, el borrado y la lectura se prueban
/// contra RustFS en las pruebas de integración (requieren Docker).
/// </summary>
public sealed class AlmacenR2Tests : IDisposable
{
    private static readonly StorageOptions Opciones = new()
    {
        Endpoint = "https://cuenta-ficticia.r2.cloudflarestorage.com",
        AccessKey = "clave-ficticia",
        SecretKey = "secreto-ficticio",
        UrlPublica = "https://archivos.serviya.test/",
        Region = "auto",
    };

    // El SDK firma con el reloj real, así que el reloj falso empieza en la hora actual.
    private readonly FakeTimeProvider reloj = new(DateTimeOffset.UtcNow);
    private readonly AmazonS3Client s3 = AlmacenR2.CrearCliente(Opciones);
    private readonly AlmacenR2 almacen;
    private readonly string clave = ClavesArchivo.Nueva("documentos");

    public AlmacenR2Tests() => almacen = new AlmacenR2(s3, Options.Create(Opciones), reloj);

    [Fact]
    public async Task La_URL_de_subida_es_PUT_al_bucket_privado_por_10_minutos_con_el_tipo_firmado()
    {
        var firmada = await almacen.UrlSubidaAsync(clave, "image/jpeg", TestContext.Current.CancellationToken);

        firmada.Url.Scheme.Should().Be("https");
        firmada.Url.Host.Should().Be("cuenta-ficticia.r2.cloudflarestorage.com");
        firmada.Url.AbsolutePath.Should().Be($"/serviya-privado/{clave}");
        var consulta = HttpUtility.ParseQueryString(firmada.Url.Query);
        consulta["X-Amz-Expires"].Should().Be("600");
        consulta["X-Amz-SignedHeaders"].Should().Contain("content-type");
        consulta["X-Amz-Credential"].Should().Contain("/auto/s3/");
        firmada.ExpiraEn.Should().Be(reloj.GetUtcNow().AddMinutes(10));
    }

    [Fact]
    public async Task La_URL_de_lectura_dura_5_minutos_y_es_del_bucket_privado()
    {
        var firmada = await almacen.UrlLecturaAsync(clave, TestContext.Current.CancellationToken);

        firmada.Url.AbsolutePath.Should().Be($"/serviya-privado/{clave}");
        HttpUtility.ParseQueryString(firmada.Url.Query)["X-Amz-Expires"].Should().Be("300");
        firmada.ExpiraEn.Should().Be(reloj.GetUtcNow().AddMinutes(5));
    }

    [Fact]
    public async Task Con_un_endpoint_http_local_la_URL_tambien_es_http()
    {
        var local = new StorageOptions
        {
            Endpoint = "http://localhost:9000",
            AccessKey = "clave-ficticia",
            SecretKey = "secreto-ficticio",
            UrlPublica = "http://localhost:9000/serviya-publico",
            Region = "us-east-1",
        };
        using var cliente = AlmacenR2.CrearCliente(local);
        var almacenLocal = new AlmacenR2(cliente, Options.Create(local), reloj);

        var firmada = await almacenLocal.UrlSubidaAsync(clave, "image/png", TestContext.Current.CancellationToken);

        firmada.Url.GetLeftPart(UriPartial.Path).Should().Be($"http://localhost:9000/serviya-privado/{clave}");
    }

    [Fact]
    public void La_URL_publica_usa_el_dominio_publico()
    {
        var portafolio = ClavesArchivo.Nueva("portafolio");

        almacen.UrlPublica(portafolio).Should().Be(new Uri($"https://archivos.serviya.test/{portafolio}"));
    }

    [Theory]
    [InlineData("../documentos/abc")]
    [InlineData("documentos/Juan Perez INE.jpg")]
    [InlineData("documentos/")]
    [InlineData("")]
    public async Task Rechaza_claves_que_no_genero_la_API(string claveInvalida)
    {
        var firmar = () => almacen.UrlLecturaAsync(claveInvalida, TestContext.Current.CancellationToken);

        await firmar.Should().ThrowAsync<ArgumentException>();
        var publica = () => almacen.UrlPublica(claveInvalida);
        publica.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Las_claves_nuevas_son_carpeta_y_GUID_sin_datos_personales()
    {
        clave.Should().MatchRegex("^documentos/[0-9a-f]{32}$");
        ClavesArchivo.Nueva("documentos").Should().NotBe(clave);
    }

    public void Dispose() => s3.Dispose();
}
