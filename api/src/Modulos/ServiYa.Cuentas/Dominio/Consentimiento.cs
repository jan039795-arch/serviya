using System.Net;

namespace ServiYa.Cuentas.Dominio;

public enum DocumentoLegal
{
    AvisoPrivacidad,
    Terminos,
    PoliticaReembolsos,
    DatosSensibles,
}

/// <summary>
/// Aceptación de un documento legal en una versión (FR-007, FR-044). Solo se agregan registros;
/// nunca se modifican, por eso no tiene métodos que cambien su estado.
/// </summary>
public sealed class Consentimiento
{
    public const int LargoMaximoVersion = 20;

    public Consentimiento(Guid usuarioId, DocumentoLegal documento, string version, DateTimeOffset aceptadoEn, IPAddress? ip)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(version);
        if (version.Length > LargoMaximoVersion)
        {
            throw new ArgumentException($"La versión no puede pasar de {LargoMaximoVersion} caracteres.", nameof(version));
        }

        UsuarioId = usuarioId;
        Documento = documento;
        Version = version;
        AceptadoEn = aceptadoEn;
        Ip = ip;
    }

    /// <summary>Para EF Core.</summary>
    private Consentimiento()
    {
    }

    public Guid Id { get; private set; }

    public Guid UsuarioId { get; private set; }

    public DocumentoLegal Documento { get; private set; }

    public string Version { get; private set; } = string.Empty;

    public DateTimeOffset AceptadoEn { get; private set; }

    /// <summary>IP desde la que se aceptó, como evidencia legal. Se guarda como <c>inet</c>.</summary>
    public IPAddress? Ip { get; private set; }
}
