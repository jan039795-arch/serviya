using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ServiYa.Comun.Configuracion;

namespace ServiYa.UnitTests.Configuracion;

public sealed class RegistroOpcionesTests
{
    private static readonly Dictionary<string, string?> ConfiguracionValida = new()
    {
        ["Storage:Endpoint"] = "http://localhost:9000",
        ["Storage:AccessKey"] = "clave-ficticia",
        ["Storage:SecretKey"] = "secreto-ficticio",
        ["Storage:UrlPublica"] = "http://localhost:9000/serviya-publico",
        ["MercadoPago:AccessToken"] = "TEST-ficticio",
        ["MercadoPago:WebhookSecret"] = "firma-ficticia",
        ["Email:Proveedor"] = "Consola",
        ["Email:Remitente"] = "no-responder@serviya.test",
        ["WebPush:PublicKey"] = "publica-ficticia",
        ["WebPush:PrivateKey"] = "privada-ficticia",
    };

    [Fact]
    public void Con_la_configuracion_completa_las_opciones_se_leen()
    {
        using var proveedor = Construir(ConfiguracionValida);

        var storage = proveedor.GetRequiredService<IOptions<StorageOptions>>().Value;

        storage.Endpoint.Should().Be("http://localhost:9000");
        storage.BucketPrivado.Should().Be("serviya-privado");
        storage.BucketPublico.Should().Be("serviya-publico");
        proveedor.GetRequiredService<IOptions<EmailOptions>>().Value.Proveedor
            .Should().Be(ProveedorCorreo.Consola);
    }

    [Theory]
    [InlineData("Storage:SecretKey")]
    [InlineData("MercadoPago:WebhookSecret")]
    [InlineData("WebPush:PrivateKey")]
    [InlineData("Email:Remitente")]
    public void Si_falta_una_clave_obligatoria_falla_con_su_nombre_y_sin_valores(string clave)
    {
        var configuracion = new Dictionary<string, string?>(ConfiguracionValida) { [clave] = null };

        var errores = ErroresDeValidacion(configuracion);

        errores.Should().Contain(error => error.Contains(clave.Split(':')[1], StringComparison.Ordinal));
        errores.Should().NotContain(error => error.Contains("ficticio", StringComparison.Ordinal));
    }

    [Fact]
    public void Resend_sin_ApiKey_no_es_valido()
    {
        var configuracion = new Dictionary<string, string?>(ConfiguracionValida)
        {
            ["Email:Proveedor"] = "Resend",
        };

        ErroresDeValidacion(configuracion)
            .Should().ContainSingle(error => error.Contains("Email:ApiKey", StringComparison.Ordinal));
    }

    [Fact]
    public void El_asunto_de_WebPush_debe_ser_mailto_o_https()
    {
        var configuracion = new Dictionary<string, string?>(ConfiguracionValida)
        {
            ["WebPush:Asunto"] = "http://serviya.test",
        };

        ErroresDeValidacion(configuracion).Should().Contain(error => error.Contains("Asunto", StringComparison.Ordinal));
    }

    [Fact]
    public void ToString_no_expone_las_claves()
    {
        using var proveedor = Construir(ConfiguracionValida);

        var storage = proveedor.GetRequiredService<IOptions<StorageOptions>>().Value;

        storage.ToString().Should().NotContain("secreto-ficticio");
    }

    private static ServiceProvider Construir(Dictionary<string, string?> valores)
    {
        var configuracion = new ConfigurationBuilder().AddInMemoryCollection(valores).Build();
        return new ServiceCollection().AgregarOpcionesDeServiYa(configuracion).BuildServiceProvider();
    }

    /// <summary>Simula el arranque: <c>ValidateOnStart</c> valida todas las opciones registradas.</summary>
    private static List<string> ErroresDeValidacion(Dictionary<string, string?> valores)
    {
        using var proveedor = Construir(valores);
        var validador = proveedor.GetRequiredService<IStartupValidator>();
        try
        {
            validador.Validate();
            return [];
        }
        catch (OptionsValidationException error)
        {
            return [.. error.Failures];
        }
        catch (AggregateException error)
        {
            return [.. error.InnerExceptions.OfType<OptionsValidationException>().SelectMany(e => e.Failures)];
        }
    }
}
