using Microsoft.EntityFrameworkCore;
using ServiYa.Busqueda.Dominio;
using ServiYa.Comun.Datos;

namespace ServiYa.Busqueda.Datos;

/// <summary>Catálogos de oficios y municipios en el esquema <c>catalogos</c>, con PostGIS.</summary>
public sealed class CatalogosDbContext(DbContextOptions<CatalogosDbContext> options) : DbContext(options)
{
    public const string Esquema = "catalogos";

    public DbSet<Oficio> Oficios => Set<Oficio>();

    public DbSet<Municipio> Municipios => Set<Municipio>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Esquema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogosDbContext).Assembly);
        modelBuilder.AplicarConvencionesServiYa();
    }
}
