using System.Collections.Frozen;
using System.Net;
using System.Text.RegularExpressions;
using ServiYa.Comun.Privacidad;

namespace ServiYa.Comun.Bitacora;

/// <summary>
/// Lo que se registra en la bitácora: quién (<see cref="ActorId"/>, nulo en tareas del sistema), qué
/// (<see cref="Accion"/>), sobre qué (<see cref="EntidadTipo"/> y <see cref="EntidadId"/>), desde
/// dónde (<see cref="Ip"/>) y un <see cref="Detalle"/> breve. La fecha la pone la base con el reloj
/// de la app.
/// <para>
/// El detalle nunca lleva datos personales (data-model.md): sus claves son identificadores en
/// camelCase que no nombran datos personales, y a sus valores se les ocultan teléfonos, correos y
/// cuentas con <see cref="ContactMasker"/>. Es la primera barrera; quien registra solo debe poner
/// identificadores, estados y motivos.
/// </para>
/// </summary>
public sealed partial record EntradaBitacora
{
    public const int LargoMaximoNombre = 60;
    public const int MaximoClavesDetalle = 10;
    public const int LargoMaximoClaveDetalle = 40;
    public const int LargoMaximoValorDetalle = 500;

    /// <summary>Partes de una clave de detalle que delatan un dato personal.</summary>
    private static readonly string[] ClavesConDatosPersonales =
    [
        "nombre", "telefono", "celular", "correo", "email", "curp", "rfc", "direccion", "domicilio",
        "calle", "contrasena", "password", "whatsapp", "nacimiento",
    ];

    public EntradaBitacora(
        Guid? actorId,
        string accion,
        string entidadTipo,
        Guid? entidadId,
        IPAddress? ip,
        IReadOnlyDictionary<string, string>? detalle = null)
    {
        ValidarNombre(accion, nameof(accion));
        ValidarNombre(entidadTipo, nameof(entidadTipo));

        ActorId = actorId;
        Accion = accion;
        EntidadTipo = entidadTipo;
        EntidadId = entidadId;
        Ip = ip;
        Detalle = PrepararDetalle(detalle);
    }

    public Guid? ActorId { get; }

    public string Accion { get; }

    public string EntidadTipo { get; }

    public Guid? EntidadId { get; }

    public IPAddress? Ip { get; }

    /// <summary>Copia propia del detalle, ya revisada; nunca nula.</summary>
    public IReadOnlyDictionary<string, string> Detalle { get; }

    private static void ValidarNombre(string valor, string parametro)
    {
        if (valor is null || valor.Length > LargoMaximoNombre || !NombrePascalCase().IsMatch(valor))
        {
            throw new ArgumentException(
                $"Debe ser un nombre en PascalCase de 3 a {LargoMaximoNombre} letras o dígitos.", parametro);
        }
    }

    private static FrozenDictionary<string, string> PrepararDetalle(IReadOnlyDictionary<string, string>? detalle)
    {
        if (detalle is null || detalle.Count == 0)
        {
            return FrozenDictionary<string, string>.Empty;
        }

        if (detalle.Count > MaximoClavesDetalle)
        {
            throw new ArgumentException($"El detalle admite hasta {MaximoClavesDetalle} claves.", nameof(detalle));
        }

        var limpio = new Dictionary<string, string>(detalle.Count, StringComparer.Ordinal);
        foreach (var (clave, valor) in detalle)
        {
            if ((ErrorDeClave(clave) ?? ErrorDeValor(valor)) is { } error)
            {
                throw new ArgumentException(error, nameof(detalle));
            }

            limpio[clave] = ContactMasker.Ocultar(valor).Texto;
        }

        return limpio.ToFrozenDictionary(StringComparer.Ordinal);
    }

    private static string? ErrorDeClave(string clave)
    {
        if (clave is null || clave.Length > LargoMaximoClaveDetalle || !ClaveCamelCase().IsMatch(clave))
        {
            return $"Cada clave del detalle debe ser un identificador en camelCase de hasta {LargoMaximoClaveDetalle} caracteres.";
        }

        var minusculas = clave.ToLowerInvariant();
        return ClavesConDatosPersonales.Any(parte => minusculas.Contains(parte, StringComparison.Ordinal))
            ? $"La clave '{clave}' nombra un dato personal; la bitácora no los guarda."
            : null;
    }

    private static string? ErrorDeValor(string valor) => valor switch
    {
        null => "Los valores del detalle no pueden ser nulos.",
        { Length: > LargoMaximoValorDetalle } => $"Cada valor del detalle admite hasta {LargoMaximoValorDetalle} caracteres.",
        _ => null,
    };

    [GeneratedRegex(@"\A[A-Z][A-Za-z0-9]{2,}\z", RegexOptions.CultureInvariant)]
    private static partial Regex NombrePascalCase();

    [GeneratedRegex(@"\A[a-z][A-Za-z0-9]*\z", RegexOptions.CultureInvariant)]
    private static partial Regex ClaveCamelCase();
}
