using Microsoft.EntityFrameworkCore;
using ServiYa.Administracion.Dominio;
using ServiYa.Comun.Datos;

namespace ServiYa.Administracion.Datos;

/// <summary>Datos de Administración en el esquema <c>admin</c>.</summary>
public sealed class AdministracionDbContext(DbContextOptions<AdministracionDbContext> options) : DbContext(options)
{
    public const string Esquema = "admin";

    public DbSet<RegistroBitacora> Bitacora => Set<RegistroBitacora>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Esquema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AdministracionDbContext).Assembly);
        modelBuilder.AplicarConvencionesServiYa();
    }
}
