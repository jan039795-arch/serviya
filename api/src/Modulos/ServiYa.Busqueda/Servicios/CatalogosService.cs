using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using ServiYa.Busqueda.Datos;

namespace ServiYa.Busqueda.Servicios;

/// <inheritdoc />
internal sealed class CatalogosService(CatalogosDbContext catalogos) : ICatalogosService
{
    /// <summary>WGS 84, el sistema del GPS y de GeoJSON.</summary>
    private const int Wgs84 = 4326;

    private static readonly GeometryFactory Geometrias = new(new PrecisionModel(), Wgs84);

    public async Task<IReadOnlyList<OficioCatalogo>> OficiosAsync(CancellationToken cancellationToken) =>
        await catalogos.Oficios.AsNoTracking()
            .Where(o => o.Activo)
            .OrderBy(o => o.Nombre)
            .Select(o => new OficioCatalogo(o.Id, o.Nombre, o.Slug, o.Icono))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<MunicipioCatalogo>> MunicipiosAsync(CancellationToken cancellationToken) =>
        await catalogos.Municipios.AsNoTracking()
            .OrderBy(m => m.Nombre)
            .Select(m => new MunicipioCatalogo(m.Id, m.ClaveInegi, m.Nombre, m.Estado))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<bool> SonOficiosActivosAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(ids);
        var distintos = ids.Distinct().ToList();
        return distintos.Count > 0
            && await catalogos.Oficios.CountAsync(o => o.Activo && distintos.Contains(o.Id), cancellationToken).ConfigureAwait(false) == distintos.Count;
    }

    public async Task<bool> ExistenMunicipiosAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(ids);
        var distintos = ids.Distinct().ToList();
        return distintos.Count > 0
            && await catalogos.Municipios.CountAsync(m => distintos.Contains(m.Id), cancellationToken).ConfigureAwait(false) == distintos.Count;
    }

    /// <summary>
    /// <c>ST_Covers</c> sobre <c>geography</c> con el índice GIST. Un punto en el límite entre dos
    /// municipios cae en ambos: se toma el de clave menor para que la respuesta sea siempre la misma.
    /// </summary>
    public async Task<MunicipioCatalogo?> UbicarAsync(double lat, double lng, CancellationToken cancellationToken)
    {
        var punto = Geometrias.CreatePoint(new Coordinate(lng, lat));
        return await catalogos.Municipios.AsNoTracking()
            .Where(m => m.Poligono.Covers(punto))
            .OrderBy(m => m.ClaveInegi)
            .Select(m => new MunicipioCatalogo(m.Id, m.ClaveInegi, m.Nombre, m.Estado))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
