using System.ComponentModel.DataAnnotations;

namespace ServiYa.Comun.Configuracion;

public enum ProveedorCorreo
{
    /// <summary>Solo desarrollo: guarda los correos en memoria (T051).</summary>
    Consola,

    /// <summary>Envío real con la API HTTP de Resend.</summary>
    Resend,
}

/// <summary>Envío de correos (códigos de verificación y avisos).</summary>
public sealed class EmailOptions : IOpcionesConSeccion, IValidatableObject
{
    public static string Seccion => "Email";

    public ProveedorCorreo Proveedor { get; init; } = ProveedorCorreo.Consola;

    /// <summary>Obligatoria con <see cref="ProveedorCorreo.Resend"/>.</summary>
    public string? ApiKey { get; init; }

    [Required, EmailAddress]
    public string Remitente { get; init; } = string.Empty;

    [Required]
    public string NombreRemitente { get; init; } = "ServiYa";

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Proveedor == ProveedorCorreo.Resend && string.IsNullOrWhiteSpace(ApiKey))
        {
            yield return new ValidationResult(
                "Email:ApiKey es obligatoria cuando el proveedor es Resend.",
                [nameof(ApiKey)]);
        }
    }
}
