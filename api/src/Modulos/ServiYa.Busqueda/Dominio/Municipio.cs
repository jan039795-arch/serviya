using NetTopologySuite.Geometries;

namespace ServiYa.Busqueda.Dominio;

/// <summary>
/// Municipio o alcaldía de la zona del piloto con su polígono del Marco Geoestadístico de INEGI (R5).
/// Lo carga <c>Datos/Semillas/importar-municipios.sql</c>; la app solo lo lee.
/// </summary>
public sealed class Municipio
{
    /// <summary>Clave geoestadística: 2 dígitos de la entidad + 3 del municipio (<c>CVEGEO</c>).</summary>
    public const int LargoClaveInegi = 5;

    public const int LargoMaximoNombre = 120;
    public const int LargoMaximoEstado = 60;

    /// <summary>Para EF Core.</summary>
    private Municipio()
    {
    }

    public Guid Id { get; private set; }

    public string ClaveInegi { get; private set; } = string.Empty;

    public string Nombre { get; private set; } = string.Empty;

    public string Estado { get; private set; } = string.Empty;

    /// <summary>Límite del municipio (WGS 84). Nunca sale en las respuestas: es grande y no hace falta.</summary>
    public MultiPolygon Poligono { get; private set; } = null!;

    /// <summary>
    /// Un punto dentro del municipio (<c>ST_PointOnSurface</c>, que a diferencia del centroide nunca
    /// queda fuera en municipios con forma irregular).
    /// </summary>
    public Point Centroide { get; private set; } = null!;
}
