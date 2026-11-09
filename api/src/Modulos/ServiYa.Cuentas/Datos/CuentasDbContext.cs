using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ServiYa.Comun.Datos;
using ServiYa.Cuentas.Dominio;

namespace ServiYa.Cuentas.Datos;

/// <summary>Datos de Cuentas en el esquema <c>cuentas</c>, con las tablas de Identity renombradas.</summary>
public sealed class CuentasDbContext(DbContextOptions<CuentasDbContext> options)
    : IdentityDbContext<Usuario, IdentityRole<Guid>, Guid>(options)
{
    public const string Esquema = "cuentas";

    public DbSet<CodigoVerificacion> CodigosVerificacion => Set<CodigoVerificacion>();

    public DbSet<Consentimiento> Consentimientos => Set<Consentimiento>();

    public DbSet<Bloqueo> Bloqueos => Set<Bloqueo>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema(Esquema);
        RenombrarTablasDeIdentity(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(CuentasDbContext).Assembly);
        builder.AplicarConvencionesServiYa();
    }

    private static void RenombrarTablasDeIdentity(ModelBuilder builder)
    {
        builder.Entity<Usuario>().ToTable("usuarios");
        builder.Entity<IdentityRole<Guid>>().ToTable("roles");
        builder.Entity<IdentityUserRole<Guid>>().ToTable("usuario_roles");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("usuario_claims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("usuario_logins");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("usuario_tokens");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("rol_claims");
    }
}
