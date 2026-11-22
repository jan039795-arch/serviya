namespace ServiYa.Busqueda.Dominio;

/// <summary>
/// Oficio del catálogo (data-model.md). Los 12 de la semilla vienen en la migración; la app solo los
/// lee. Para quitar uno del piloto se marca inactivo, nunca se borra (los perfiles lo referencian).
/// </summary>
public sealed class Oficio
{
    public const int LargoMaximoNombre = 60;
    public const int LargoMaximoSlug = 60;
    public const int LargoMaximoIcono = 40;

    /// <summary>Para EF Core.</summary>
    private Oficio()
    {
    }

    public Guid Id { get; private set; }

    public string Nombre { get; private set; } = string.Empty;

    /// <summary>Para las URL de búsqueda (<c>?oficio=albanil</c>).</summary>
    public string Slug { get; private set; } = string.Empty;

    /// <summary>Nombre del ícono en la app (<c>web/src/app/shared/ui/</c>).</summary>
    public string Icono { get; private set; } = string.Empty;

    public bool Activo { get; private set; }
}
