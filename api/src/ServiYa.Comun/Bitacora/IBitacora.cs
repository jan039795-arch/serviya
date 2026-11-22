namespace ServiYa.Comun.Bitacora;

/// <summary>
/// Bitácora de acciones administrativas y consultas de documentos (FR-016, FR-042). La implementa el
/// módulo de Administración en la tabla <c>admin.registro_bitacora</c>, de solo inserción: la app no
/// puede modificar ni borrar lo registrado.
/// </summary>
public interface IBitacora
{
    /// <summary>
    /// Guarda la entrada de inmediato, en su propia transacción. Se llama antes de entregar algo
    /// sensible (como la URL firmada de un documento) o después de guardar la decisión que registra.
    /// </summary>
    Task RegistrarAsync(EntradaBitacora entrada, CancellationToken cancellationToken);
}
