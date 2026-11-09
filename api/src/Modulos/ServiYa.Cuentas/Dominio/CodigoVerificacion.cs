namespace ServiYa.Cuentas.Dominio;

public enum PropositoCodigo
{
    VerificarCorreo,
    RecuperarContrasena,
}

/// <summary>
/// Código de un solo uso enviado por correo (research R2). Solo se guarda su hash; las reglas de
/// vigencia e intentos están en <see cref="Servicios.ServicioCodigos"/>.
/// </summary>
public sealed class CodigoVerificacion
{
    public const int LargoHash = 64;

    internal CodigoVerificacion(Guid usuarioId, PropositoCodigo proposito, string codigoHash, DateTimeOffset expiraEn)
    {
        UsuarioId = usuarioId;
        Proposito = proposito;
        CodigoHash = codigoHash;
        ExpiraEn = expiraEn;
    }

    /// <summary>Para EF Core.</summary>
    private CodigoVerificacion()
    {
    }

    public Guid Id { get; private set; }

    public Guid UsuarioId { get; private set; }

    public PropositoCodigo Proposito { get; private set; }

    /// <summary>SHA-256 en hexadecimal de usuario, propósito y código.</summary>
    public string CodigoHash { get; private set; } = string.Empty;

    public DateTimeOffset ExpiraEn { get; private set; }

    public int Intentos { get; private set; }

    public DateTimeOffset? UsadoEn { get; private set; }

    /// <summary>
    /// Columna de sistema <c>xmin</c> como token de concurrencia: si dos peticiones verifican el mismo
    /// código a la vez, solo una guarda; la otra recibe <c>DbUpdateConcurrencyException</c>. Sin esto,
    /// peticiones en paralelo se saltarían el límite de 5 intentos o usarían el código dos veces.
    /// </summary>
    public uint Version { get; private set; }

    internal void RegistrarIntentoFallido() => Intentos++;

    internal void MarcarUsado(DateTimeOffset ahora) => UsadoEn = ahora;
}
