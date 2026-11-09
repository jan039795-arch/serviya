namespace ServiYa.Cuentas.Dominio;

/// <summary>Un usuario bloquea a otro (FR-040). Clave única (bloqueador, bloqueado).</summary>
public sealed class Bloqueo
{
    public Bloqueo(Guid bloqueadorId, Guid bloqueadoId)
    {
        if (bloqueadorId == bloqueadoId)
        {
            throw new ArgumentException("Nadie puede bloquearse a sí mismo.", nameof(bloqueadoId));
        }

        BloqueadorId = bloqueadorId;
        BloqueadoId = bloqueadoId;
    }

    /// <summary>Para EF Core.</summary>
    private Bloqueo()
    {
    }

    public Guid BloqueadorId { get; private set; }

    public Guid BloqueadoId { get; private set; }
}
