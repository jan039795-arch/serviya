using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ServiYa.Comun.Tareas;

/// <summary>Tipos de las tareas registradas con <see cref="RegistroTareas.AgregarTarea{T}"/>.</summary>
public sealed class CatalogoTareas
{
    private readonly List<Type> tipos = [];

    public IReadOnlyList<Type> Tipos => tipos;

    internal void Agregar(Type tipo)
    {
        if (!tipos.Contains(tipo))
        {
            tipos.Add(tipo);
        }
    }
}

/// <summary>
/// Corre las tareas programadas al arrancar y luego cada 5 minutos (research R7, T052). Cada tarea
/// usa su propio ámbito de servicios y su candado; si una falla, se registra y siguen las demás.
/// </summary>
public sealed partial class PlanificadorTareas(
    IServiceScopeFactory ambitos,
    CatalogoTareas catalogo,
    ICandadoTareas candado,
    TimeProvider reloj,
    ILogger<PlanificadorTareas> logger) : BackgroundService
{
    public static readonly TimeSpan Intervalo = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var temporizador = new PeriodicTimer(Intervalo, reloj);
        try
        {
            do
            {
                await EjecutarRondaSinFallarAsync(stoppingToken).ConfigureAwait(false);
            }
            while (await temporizador.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // La API se está deteniendo.
        }
    }

    /// <summary>
    /// Una excepción que escape de <c>ExecuteAsync</c> detendría toda la API
    /// (<c>BackgroundServiceExceptionBehavior.StopHost</c>), así que la ronda nunca falla hacia afuera.
    /// </summary>
    private async Task EjecutarRondaSinFallarAsync(CancellationToken stoppingToken)
    {
        try
        {
            await EjecutarRondaAsync(stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            throw;
        }
#pragma warning disable CA1031 // Ver el resumen: el planificador no debe tumbar la API.
        catch (Exception error)
#pragma warning restore CA1031
        {
            RondaFallo(logger, error);
        }
    }

    /// <summary>Una vuelta por todas las tareas.</summary>
    public async Task EjecutarRondaAsync(CancellationToken cancellationToken)
    {
        foreach (var tipo in catalogo.Tipos)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await EjecutarAsync(tipo, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task EjecutarAsync(Type tipo, CancellationToken cancellationToken)
    {
        var ambito = ambitos.CreateAsyncScope();
        await using (ambito.ConfigureAwait(false))
        {
            var nombre = tipo.Name;
            try
            {
                // Dentro del try: si la tarea no se puede crear (falta una dependencia), solo falla ella.
                var tarea = (ITareaProgramada)ambito.ServiceProvider.GetRequiredService(tipo);
                nombre = tarea.Nombre;
                var tomado = await candado.IntentarTomarAsync(nombre, cancellationToken).ConfigureAwait(false);
                if (tomado is null)
                {
                    TareaEnOtraInstancia(logger, nombre);
                    return;
                }

                await using (tomado.ConfigureAwait(false))
                {
                    await tarea.EjecutarAsync(cancellationToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
#pragma warning disable CA1031 // Una tarea con error no debe detener a las demás.
            catch (Exception error)
#pragma warning restore CA1031
            {
                TareaFallo(logger, nombre, error);
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "La tarea {Tarea} ya corre en otra instancia")]
    private static partial void TareaEnOtraInstancia(ILogger logger, string tarea);

    [LoggerMessage(Level = LogLevel.Error, Message = "La tarea {Tarea} falló")]
    private static partial void TareaFallo(ILogger logger, string tarea, Exception error);

    [LoggerMessage(Level = LogLevel.Error, Message = "La ronda de tareas programadas falló")]
    private static partial void RondaFallo(ILogger logger, Exception error);
}
