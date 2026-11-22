namespace ServiYa.Busqueda.Servicios;

/// <summary>Oficio como lo ve la app (<c>web/src/app/core/api/modelos.ts</c>).</summary>
public sealed record OficioCatalogo(Guid Id, string Nombre, string Slug, string Icono);

/// <summary>Municipio sin su polígono.</summary>
public sealed record MunicipioCatalogo(Guid Id, string ClaveInegi, string Nombre, string Estado);

/// <summary>Catálogos para los endpoints y para los demás módulos (Prestadores valida con esto su alta).</summary>
public interface ICatalogosService
{
    /// <summary>Oficios activos, por nombre.</summary>
    Task<IReadOnlyList<OficioCatalogo>> OficiosAsync(CancellationToken cancellationToken);

    /// <summary>Municipios de la zona, por nombre.</summary>
    Task<IReadOnlyList<MunicipioCatalogo>> MunicipiosAsync(CancellationToken cancellationToken);

    /// <summary>Todos existen y están activos. Una lista vacía no es válida.</summary>
    Task<bool> SonOficiosActivosAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);

    /// <summary>Todos existen. Una lista vacía no es válida.</summary>
    Task<bool> ExistenMunicipiosAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);

    /// <summary>Municipio que contiene el punto, o null si cae fuera de la zona. Las coordenadas ya vienen validadas.</summary>
    Task<MunicipioCatalogo?> UbicarAsync(double lat, double lng, CancellationToken cancellationToken);
}
