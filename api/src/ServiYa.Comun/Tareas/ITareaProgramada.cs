namespace ServiYa.Comun.Tareas;

/// <summary>
/// Tarea que corre cada 5 minutos (research R7): vencer accesos, dar créditos a las 48 h, publicar
/// calificaciones a los 14 días... Debe ser idempotente: si se repite o se salta una ronda, el
/// resultado es el mismo, porque decide con lo que dice la base de datos.
/// </summary>
public interface ITareaProgramada
{
    /// <summary>Nombre estable: con él se toma el candado y se escribe en el registro.</summary>
    string Nombre { get; }

    Task EjecutarAsync(CancellationToken cancellationToken);
}

/// <summary>Evita que dos instancias de la API ejecuten la misma tarea a la vez.</summary>
public interface ICandadoTareas
{
    /// <summary>El candado tomado (se suelta al desecharlo) o <c>null</c> si otra instancia lo tiene.</summary>
    Task<IAsyncDisposable?> IntentarTomarAsync(string nombreTarea, CancellationToken cancellationToken);
}
