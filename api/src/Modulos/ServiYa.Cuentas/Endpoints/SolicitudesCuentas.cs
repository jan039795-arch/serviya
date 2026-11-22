using System.Globalization;
using System.Net.Mail;
using ServiYa.Comun.Seguridad;
using ServiYa.Cuentas.Dominio;
using ServiYa.Cuentas.Seguridad;

namespace ServiYa.Cuentas.Endpoints;

/// <summary>Documento legal aceptado al registrarse (FR-044).</summary>
public sealed record AceptacionSolicitud(string? Documento, string? Version);

/// <summary>Cuerpos de las peticiones de Cuentas (contracts/api.md). Todo llega como texto y se valida aquí.</summary>
public sealed record RegistroSolicitud(
    string? Email,
    string? Password,
    string? NombreCompleto,
    string? FechaNacimiento,
    string? Rol,
    IReadOnlyList<AceptacionSolicitud>? Aceptaciones);

public sealed record VerificarCorreoSolicitud(string? Email, string? Codigo);

public sealed record CorreoSolicitud(string? Email);

public sealed record LoginSolicitud(string? Email, string? Password);

public sealed record RestablecerSolicitud(string? Email, string? Codigo, string? NuevaPassword);

/// <summary>Registro ya validado.</summary>
internal sealed record RegistroValido(
    string Email,
    string Password,
    string NombreCompleto,
    DateOnly FechaNacimiento,
    IReadOnlyList<string> Roles,
    IReadOnlyList<(DocumentoLegal Documento, string Version)> Aceptaciones);

/// <summary>
/// Reglas de los datos de Cuentas, iguales a las del prototipo (<c>web/src/app/core/mocks/handlers/cuentas.ts</c>).
/// Los errores van por campo, con la clave en camelCase que usa la app.
/// </summary>
internal static class ValidacionCuentas
{
    /// <summary>PBKDF2 con textos enormes gasta CPU; ninguna contraseña real pasa de esto.</summary>
    public const int LargoMaximoPassword = 128;

    public const int LargoMaximoEmail = 256;

    private const int MaximoAceptaciones = 4;

    private static readonly DateOnly NacimientoMasAntiguo = new(1900, 1, 1);

    private static readonly DocumentoLegal[] Obligatorios = [DocumentoLegal.AvisoPrivacidad, DocumentoLegal.Terminos];

    /// <summary>Correo en minúsculas y sin espacios alrededor, o null si no parece un correo.</summary>
    public static string? NormalizarEmail(string? email)
    {
        var limpio = email?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(limpio) || limpio.Length > LargoMaximoEmail || limpio.Any(char.IsWhiteSpace)
            || !MailAddress.TryCreate(limpio, out var direccion) || direccion.Address != limpio
            || !direccion.Host.Contains('.', StringComparison.Ordinal))
        {
            return null;
        }

        return limpio;
    }

    public static string? ErrorDePassword(string? password) =>
        password is null || password.Length < RegistroIdentity.LargoMinimoPassword || password.Length > LargoMaximoPassword
            ? $"La contraseña debe tener al menos {RegistroIdentity.LargoMinimoPassword} caracteres."
            : null;

    public static (RegistroValido? Registro, Dictionary<string, string[]> Errores) ValidarRegistro(RegistroSolicitud solicitud, DateOnly hoy)
    {
        ArgumentNullException.ThrowIfNull(solicitud);
        var errores = new Dictionary<string, string[]>(StringComparer.Ordinal);
        var email = NormalizarEmail(solicitud.Email);
        Agregar(errores, "email", email is null ? "Escribe un correo válido." : null);
        Agregar(errores, "password", ErrorDePassword(solicitud.Password));

        var nombre = solicitud.NombreCompleto?.Trim() ?? string.Empty;
        Agregar(errores, "nombreCompleto", nombre.Length is < Usuario.LargoMinimoNombre or > Usuario.LargoMaximoNombre
            ? $"Escribe tu nombre completo (de {Usuario.LargoMinimoNombre} a {Usuario.LargoMaximoNombre} caracteres)."
            : null);

        var fecha = LeerFecha(solicitud.FechaNacimiento, hoy);
        Agregar(errores, "fechaNacimiento", fecha is null ? "Escribe tu fecha de nacimiento." : null);

        string[]? roles = solicitud.Rol switch
        {
            Roles.Cliente => [Roles.Cliente],
            Roles.Prestador => [Roles.Cliente, Roles.Prestador],
            _ => null,
        };
        Agregar(errores, "rol", roles is null ? "Elige si quieres contratar o trabajar." : null);

        var aceptaciones = LeerAceptaciones(solicitud.Aceptaciones);
        Agregar(errores, "aceptaciones", aceptaciones is null ? "Acepta el aviso de privacidad y los términos." : null);

        return errores.Count > 0
            ? (null, errores)
            : (new RegistroValido(email!, solicitud.Password!, nombre, fecha!.Value, roles!, aceptaciones!), errores);
    }

    public static void Agregar(Dictionary<string, string[]> errores, string campo, string? error)
    {
        ArgumentNullException.ThrowIfNull(errores);
        if (error is not null)
        {
            errores[campo] = [error];
        }
    }

    private static DateOnly? LeerFecha(string? texto, DateOnly hoy) =>
        DateOnly.TryParseExact(texto, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fecha)
        && fecha >= NacimientoMasAntiguo && fecha <= hoy
            ? fecha
            : null;

    /// <summary>Documentos válidos y sin repetir, con al menos el aviso de privacidad y los términos; si no, null.</summary>
    private static List<(DocumentoLegal, string)>? LeerAceptaciones(IReadOnlyList<AceptacionSolicitud>? aceptaciones)
    {
        if (aceptaciones is null || aceptaciones.Count > MaximoAceptaciones)
        {
            return null;
        }

        var leidas = new List<(DocumentoLegal Documento, string Version)>();
        foreach (var aceptacion in aceptaciones)
        {
            var version = aceptacion?.Version?.Trim();
            if (aceptacion is null || !Enum.TryParse<DocumentoLegal>(aceptacion.Documento, ignoreCase: false, out var documento)
                || !Enum.IsDefined(documento) || int.TryParse(aceptacion.Documento, out _)
                || string.IsNullOrEmpty(version) || version.Length > Consentimiento.LargoMaximoVersion
                || leidas.Exists(l => l.Documento == documento))
            {
                return null;
            }

            leidas.Add((documento, version));
        }

        return Obligatorios.All(o => leidas.Exists(l => l.Documento == o)) ? leidas : null;
    }
}
