namespace ServiYa.Comun.Seguridad;

/// <summary>
/// Motivo de la suspensión vigente de una cuenta (FR-041). Lo implementa el módulo de Reportes, donde
/// vive la entidad <c>Suspension</c> (T135); hasta entonces se usa <see cref="SinMotivosDeSuspension"/>.
/// </summary>
public interface IConsultaSuspensiones
{
    Task<string?> MotivoAsync(Guid usuarioId, CancellationToken cancellationToken);
}

/// <summary>Mientras no existan las suspensiones con motivo: la cuenta suspendida recibe un mensaje general.</summary>
public sealed class SinMotivosDeSuspension : IConsultaSuspensiones
{
    public Task<string?> MotivoAsync(Guid usuarioId, CancellationToken cancellationToken) => Task.FromResult<string?>(null);
}

/// <summary>
/// Endpoint que una cuenta suspendida sí puede usar: ver su sesión, salir y enviar su aclaración
/// (contracts/api.md, reglas transversales).
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class PermitidoASuspendidosAttribute : Attribute;
