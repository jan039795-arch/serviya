using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ServiYa.Comun.Configuracion;

namespace ServiYa.Comun.Correo;

/// <summary>Envío con la API HTTP de Resend (<c>POST /emails</c>), sin SDK.</summary>
public sealed partial class ResendEmailSender(
    HttpClient http,
    IOptions<EmailOptions> opciones,
    ILogger<ResendEmailSender> logger) : IEmailSender
{
    public static readonly Uri DireccionBase = new("https://api.resend.com/");
    public static readonly TimeSpan TiempoMaximo = TimeSpan.FromSeconds(10);

    private static readonly Uri RutaEnvio = new("emails", UriKind.Relative);

    public async Task EnviarAsync(CorreoSaliente correo, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(correo);
        var configuracion = opciones.Value;
        var cuerpo = new PeticionResend(
            $"{configuracion.NombreRemitente} <{configuracion.Remitente}>",
            [correo.Para],
            correo.Asunto,
            correo.Texto);

        using var peticion = new HttpRequestMessage(HttpMethod.Post, RutaEnvio);
        peticion.Content = JsonContent.Create(cuerpo);
        peticion.Headers.Authorization = new AuthenticationHeaderValue("Bearer", configuracion.ApiKey);

        using var respuesta = await Enviar(peticion, cancellationToken).ConfigureAwait(false);
        if (!respuesta.IsSuccessStatusCode)
        {
            // Solo el estado: el cuerpo de la respuesta puede repetir el destinatario.
            var estado = (int)respuesta.StatusCode;
            EnvioRechazado(logger, estado);
            throw new ErrorEnvioCorreoException($"El proveedor de correo respondió {estado}.");
        }
    }

    private async Task<HttpResponseMessage> Enviar(HttpRequestMessage peticion, CancellationToken cancellationToken)
    {
        try
        {
            return await http.SendAsync(peticion, cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException error)
        {
            throw new ErrorEnvioCorreoException("No se pudo conectar con el proveedor de correo.", error);
        }
        catch (TaskCanceledException error) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ErrorEnvioCorreoException("El proveedor de correo no respondió a tiempo.", error);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Resend rechazó un correo con estado {Estado}")]
    private static partial void EnvioRechazado(ILogger logger, int estado);

    private sealed record PeticionResend(
        [property: JsonPropertyName("from")] string De,
        [property: JsonPropertyName("to")] string[] Para,
        [property: JsonPropertyName("subject")] string Asunto,
        [property: JsonPropertyName("text")] string Texto);
}
