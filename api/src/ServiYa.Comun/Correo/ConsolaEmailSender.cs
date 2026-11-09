using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace ServiYa.Comun.Correo;

/// <summary>Correo guardado en memoria por <see cref="ConsolaEmailSender"/>.</summary>
public sealed record CorreoGuardado(string Para, string Asunto, string Texto, DateTimeOffset EnviadoEn);

/// <summary>
/// Solo desarrollo y pruebas: no envía nada; guarda los últimos correos en memoria para leerlos en
/// <c>GET /api/v1/dev/correos</c> o desde las pruebas. En el registro solo queda que se guardó un
/// correo, nunca el destinatario, el asunto ni el código (principio I).
/// </summary>
public sealed partial class ConsolaEmailSender(TimeProvider reloj, ILogger<ConsolaEmailSender> logger) : IEmailSender
{
    public const int Capacidad = 50;

    private readonly ConcurrentQueue<CorreoGuardado> correos = new();

    /// <summary>Del más reciente al más antiguo.</summary>
    public IReadOnlyList<CorreoGuardado> Recientes => [.. correos.Reverse()];

    public Task EnviarAsync(CorreoSaliente correo, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(correo);
        correos.Enqueue(new CorreoGuardado(correo.Para, correo.Asunto, correo.Texto, reloj.GetUtcNow()));
        while (correos.Count > Capacidad && correos.TryDequeue(out _))
        {
        }

        CorreoSimulado(logger);
        return Task.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Correo simulado guardado en memoria")]
    private static partial void CorreoSimulado(ILogger logger);
}
