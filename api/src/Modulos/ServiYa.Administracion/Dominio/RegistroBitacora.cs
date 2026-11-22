using System.Net;
using ServiYa.Comun.Bitacora;

namespace ServiYa.Administracion.Dominio;

/// <summary>
/// Una fila de la bitácora (FR-016, FR-042). Solo se agrega: no tiene métodos que cambien su estado
/// y la base le quita al rol de la app los permisos de <c>UPDATE</c>, <c>DELETE</c> y <c>TRUNCATE</c>.
/// </summary>
public sealed class RegistroBitacora
{
    /// <summary>Para EF Core.</summary>
    private RegistroBitacora()
    {
    }

    private RegistroBitacora(EntradaBitacora entrada, string detalle)
    {
        ActorId = entrada.ActorId;
        Accion = entrada.Accion;
        EntidadTipo = entrada.EntidadTipo;
        EntidadId = entrada.EntidadId;
        Ip = entrada.Ip;
        Detalle = detalle;
    }

    public Guid Id { get; private set; }

    /// <summary>Quién hizo la acción; nulo si fue una tarea del sistema. Sin llave foránea: es de otro módulo.</summary>
    public Guid? ActorId { get; private set; }

    public string Accion { get; private set; } = string.Empty;

    public string EntidadTipo { get; private set; } = string.Empty;

    public Guid? EntidadId { get; private set; }

    /// <summary>JSON (<c>jsonb</c>) con el detalle ya revisado de <see cref="EntradaBitacora.Detalle"/>.</summary>
    public string Detalle { get; private set; } = "{}";

    public IPAddress? Ip { get; private set; }

    /// <summary>Columna <c>created_at</c>; la pone <c>MarcasTiempoInterceptor</c> con el reloj de la app.</summary>
    public DateTimeOffset CreadoEn { get; private set; }

    internal static RegistroBitacora Desde(EntradaBitacora entrada, string detalleJson)
    {
        ArgumentNullException.ThrowIfNull(entrada);
        ArgumentException.ThrowIfNullOrWhiteSpace(detalleJson);
        return new RegistroBitacora(entrada, detalleJson);
    }
}
