using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using ServiYa.Comun.Configuracion;
using ServiYa.Comun.Correo;
using ServiYa.Comun.Correo.Plantillas;

namespace ServiYa.UnitTests.Correo;

public sealed class CorreoTests
{
    private static readonly EmailOptions OpcionesResend = new()
    {
        Proveedor = ProveedorCorreo.Resend,
        ApiKey = "re_ficticia",
        Remitente = "no-responder@serviya.test",
        NombreRemitente = "ServiYa",
    };

    [Fact]
    public async Task Resend_recibe_remitente_destinatario_asunto_texto_y_la_clave()
    {
        var manejador = new ManejadorFalso(HttpStatusCode.OK);
        var remitente = CrearResend(manejador);

        await remitente.EnviarAsync(
            new CorreoSaliente("persona@correo.test", "Asunto", "Texto"),
            TestContext.Current.CancellationToken);

        manejador.Peticion!.Method.Should().Be(HttpMethod.Post);
        manejador.Peticion.RequestUri.Should().Be(new Uri("https://api.resend.com/emails"));
        manejador.Peticion.Headers.Authorization!.ToString().Should().Be("Bearer re_ficticia");
        using var cuerpo = JsonDocument.Parse(manejador.Cuerpo!);
        cuerpo.RootElement.GetProperty("from").GetString().Should().Be("ServiYa <no-responder@serviya.test>");
        cuerpo.RootElement.GetProperty("to")[0].GetString().Should().Be("persona@correo.test");
        cuerpo.RootElement.GetProperty("subject").GetString().Should().Be("Asunto");
        cuerpo.RootElement.GetProperty("text").GetString().Should().Be("Texto");
    }

    [Fact]
    public async Task Si_Resend_rechaza_el_correo_falla_sin_mostrar_el_destinatario()
    {
        var remitente = CrearResend(new ManejadorFalso(HttpStatusCode.UnprocessableEntity));

        var enviar = () => remitente.EnviarAsync(
            new CorreoSaliente("persona@correo.test", "Asunto", "Texto"),
            TestContext.Current.CancellationToken);

        var error = (await enviar.Should().ThrowAsync<ErrorEnvioCorreoException>()).Which;
        error.Message.Should().Contain("422").And.NotContain("persona@correo.test");
    }

    [Fact]
    public async Task Si_no_hay_conexion_falla_con_un_error_de_envio()
    {
        var remitente = CrearResend(new ManejadorFalso(new HttpRequestException("sin red")));

        var enviar = () => remitente.EnviarAsync(
            new CorreoSaliente("persona@correo.test", "Asunto", "Texto"),
            TestContext.Current.CancellationToken);

        await enviar.Should().ThrowAsync<ErrorEnvioCorreoException>();
    }

    [Fact]
    public async Task La_consola_guarda_los_ultimos_correos_del_mas_reciente_al_mas_antiguo()
    {
        var reloj = new FakeTimeProvider(new DateTimeOffset(2026, 11, 8, 12, 0, 0, TimeSpan.Zero));
        var consola = new ConsolaEmailSender(reloj, NullLogger<ConsolaEmailSender>.Instance);

        for (var i = 1; i <= ConsolaEmailSender.Capacidad + 5; i++)
        {
            await consola.EnviarAsync(new CorreoSaliente($"p{i}@correo.test", $"Correo {i}", "Texto"), CancellationToken.None);
        }

        consola.Recientes.Should().HaveCount(ConsolaEmailSender.Capacidad);
        consola.Recientes[0].Asunto.Should().Be($"Correo {ConsolaEmailSender.Capacidad + 5}");
        consola.Recientes[^1].Asunto.Should().Be("Correo 6");
        consola.Recientes[0].EnviadoEn.Should().Be(reloj.GetUtcNow());
    }

    [Fact]
    public void Las_plantillas_incluyen_el_codigo_y_su_vigencia()
    {
        var verificacion = PlantillasCorreo.CodigoVerificacion("persona@correo.test", "123456");
        var recuperacion = PlantillasCorreo.RecuperarContrasena("persona@correo.test", "654321");

        verificacion.Para.Should().Be("persona@correo.test");
        verificacion.Asunto.Should().Contain("123456");
        verificacion.Texto.Should().Contain("123456").And.Contain("10 minutos");
        recuperacion.Texto.Should().Contain("654321").And.Contain("contraseña");
    }

    private static ResendEmailSender CrearResend(ManejadorFalso manejador) => new(
        new HttpClient(manejador) { BaseAddress = ResendEmailSender.DireccionBase },
        Options.Create(OpcionesResend),
        NullLogger<ResendEmailSender>.Instance);

    private sealed class ManejadorFalso : HttpMessageHandler
    {
        private readonly HttpStatusCode estado;
        private readonly Exception? error;

        public ManejadorFalso(HttpStatusCode estado) => this.estado = estado;

        public ManejadorFalso(Exception error) => this.error = error;

        public HttpRequestMessage? Peticion { get; private set; }

        public string? Cuerpo { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (error is not null)
            {
                throw error;
            }

            Peticion = request;
            Cuerpo = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(estado) { Content = new StringContent("""{"to":"persona@correo.test"}""") };
        }
    }
}
