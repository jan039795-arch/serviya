using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;

namespace ServiYa.IntegrationTests.Infraestructura;

/// <summary>
/// Guarda en memoria todo lo que la API escribiría en el registro (categoría, mensaje, valores y
/// ámbitos) para comprobar que no salen datos personales. Respeta los niveles de appsettings.
/// </summary>
internal sealed class RegistroEnMemoria : ILoggerProvider, ISupportExternalScope
{
    private readonly ConcurrentQueue<string> entradas = new();
    private IExternalScopeProvider ambitos = new LoggerExternalScopeProvider();

    public IReadOnlyCollection<string> Entradas => entradas;

    public ILogger CreateLogger(string categoryName) => new Registrador(categoryName, this);

    public void SetScopeProvider(IExternalScopeProvider scopeProvider) => ambitos = scopeProvider;

    public void Dispose()
    {
    }

    private sealed class Registrador(string categoria, RegistroEnMemoria registro) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => registro.ambitos.Push(state);

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var cultura = CultureInfo.InvariantCulture;
            var texto = new StringBuilder();
            texto.Append(cultura, $"{categoria} [{logLevel}] {formatter(state, exception)}");
            if (state is IEnumerable<KeyValuePair<string, object?>> valores)
            {
                foreach (var (clave, valor) in valores)
                {
                    texto.Append(cultura, $" | {clave}={valor}");
                }
            }

            registro.ambitos.ForEachScope(
                (ambito, destino) => destino.Append(cultura, $" | ámbito: {ambito}"),
                texto);
            if (exception is not null)
            {
                texto.Append(cultura, $" | excepción: {exception}");
            }

            registro.entradas.Enqueue(texto.ToString());
        }
    }
}
