using Microsoft.AspNetCore.Identity;

namespace ServiYa.Cuentas.Dominio;

public enum EstadoUsuario
{
    Activo,
    Suspendido,
    Eliminado,
}

/// <summary>
/// Cuenta de ServiYa (data-model.md). El correo es también el nombre de usuario y debe verificarse
/// antes de usar la cuenta (FR-001). El teléfono (<c>PhoneNumber</c>, E.164 +52) es privado.
/// </summary>
public sealed class Usuario : IdentityUser<Guid>
{
    /// <summary>Edad para publicar un perfil o pagar (FR-004).</summary>
    public const int EdadMinima = 18;

    public const int LargoMinimoNombre = 3;
    public const int LargoMaximoNombre = 120;

    public Usuario(string email, string nombreCompleto, DateOnly fechaNacimiento)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        var nombre = nombreCompleto?.Trim() ?? string.Empty;
        if (nombre.Length is < LargoMinimoNombre or > LargoMaximoNombre)
        {
            throw new ArgumentException(
                $"El nombre debe tener entre {LargoMinimoNombre} y {LargoMaximoNombre} caracteres.",
                nameof(nombreCompleto));
        }

        Email = email;
        UserName = email;
        NombreCompleto = nombre;
        FechaNacimiento = fechaNacimiento;
        Estado = EstadoUsuario.Activo;
    }

    /// <summary>Para EF Core.</summary>
    private Usuario()
    {
    }

    public string NombreCompleto { get; private set; } = string.Empty;

    public DateOnly FechaNacimiento { get; private set; }

    public EstadoUsuario Estado { get; private set; }

    /// <summary>Borrado lógico: al eliminar la cuenta se anonimizan sus datos (FR-045).</summary>
    public DateTimeOffset? EliminadoEn { get; private set; }

    /// <summary>
    /// Edad cumplida en <paramref name="hoy"/> (día en hora de México). Se compara mes y día, como en
    /// <c>web/src/app/shared/fechas/edad.ts</c>: quien nació un 29 de febrero cumple el 1 de marzo en
    /// los años que no son bisiestos.
    /// </summary>
    public int EdadEn(DateOnly hoy)
    {
        var edad = hoy.Year - FechaNacimiento.Year;
        var yaCumplio = (hoy.Month, hoy.Day).CompareTo((FechaNacimiento.Month, FechaNacimiento.Day)) >= 0;
        return yaCumplio ? edad : edad - 1;
    }

    public bool EsMayorDeEdad(DateOnly hoy) => EdadEn(hoy) >= EdadMinima;
}
