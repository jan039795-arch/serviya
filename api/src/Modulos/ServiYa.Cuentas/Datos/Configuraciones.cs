using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiYa.Comun.Seguridad;
using ServiYa.Cuentas.Dominio;

namespace ServiYa.Cuentas.Datos;

/// <summary>Largo de las columnas de enumeraciones guardadas como texto.</summary>
internal static class Largos
{
    public const int Enumeracion = 30;
}

internal sealed class UsuarioConfiguracion : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.Property(u => u.NombreCompleto).HasMaxLength(Usuario.LargoMaximoNombre).IsRequired();
        builder.Property(u => u.FechaNacimiento).IsRequired();
        builder.Property(u => u.Estado).HasConversion<string>().HasMaxLength(Largos.Enumeracion);

        // Identity crea el índice del correo sin unicidad; aquí se exige (FR-001). Una cuenta
        // eliminada se anonimiza, así que el correo vuelve a quedar libre.
        builder.HasIndex(u => u.NormalizedEmail).IsUnique();
    }
}

internal sealed class CodigoVerificacionConfiguracion : IEntityTypeConfiguration<CodigoVerificacion>
{
    public void Configure(EntityTypeBuilder<CodigoVerificacion> builder)
    {
        builder.ToTable("codigos_verificacion");
        builder.Property(c => c.Proposito).HasConversion<string>().HasMaxLength(Largos.Enumeracion);
        builder.Property(c => c.CodigoHash).HasMaxLength(CodigoVerificacion.LargoHash).IsFixedLength();
        // Columna de sistema de PostgreSQL: no se crea; Npgsql la lee y la compara al guardar.
        builder.Property(c => c.Version).IsRowVersion().HasColumnName("xmin").HasColumnType("xid");
        builder.HasOne<Usuario>().WithMany().HasForeignKey(c => c.UsuarioId).OnDelete(DeleteBehavior.Cascade);

        // Buscar el último código y contar los envíos de la última hora (máximo 3).
        builder.HasIndex(c => new { c.UsuarioId, c.Proposito });
    }
}

internal sealed class ConsentimientoConfiguracion : IEntityTypeConfiguration<Consentimiento>
{
    public void Configure(EntityTypeBuilder<Consentimiento> builder)
    {
        builder.ToTable("consentimientos");
        builder.Property(c => c.Documento).HasConversion<string>().HasMaxLength(Largos.Enumeracion);
        builder.Property(c => c.Version).HasMaxLength(Consentimiento.LargoMaximoVersion);

        // Evidencia legal: no se borra junto con el usuario (el usuario solo se anonimiza).
        builder.HasOne<Usuario>().WithMany().HasForeignKey(c => c.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(c => c.UsuarioId);
    }
}

internal sealed class BloqueoConfiguracion : IEntityTypeConfiguration<Bloqueo>
{
    public void Configure(EntityTypeBuilder<Bloqueo> builder)
    {
        builder.ToTable("bloqueos", tabla => tabla.HasCheckConstraint(
            "ck_bloqueos_distintos",
            "bloqueador_id <> bloqueado_id"));
        builder.HasKey(b => new { b.BloqueadorId, b.BloqueadoId });
        builder.HasOne<Usuario>().WithMany().HasForeignKey(b => b.BloqueadorId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Usuario>().WithMany().HasForeignKey(b => b.BloqueadoId).OnDelete(DeleteBehavior.Cascade);

        // "¿Quién me bloqueó?" para ocultar perfiles y conversaciones.
        builder.HasIndex(b => b.BloqueadoId);
    }
}

/// <summary>Roles semilla con identificadores fijos, para que la migración sea igual en todos los entornos (FR-002).</summary>
internal sealed class RolConfiguracion : IEntityTypeConfiguration<IdentityRole<Guid>>
{
    private static readonly DateTimeOffset Creacion = new(2026, 11, 22, 0, 0, 0, TimeSpan.Zero);

    public static readonly IReadOnlyDictionary<string, Guid> Ids = new Dictionary<string, Guid>
    {
        [Roles.Cliente] = new("0193e0a0-0000-7000-8000-000000000001"),
        [Roles.Prestador] = new("0193e0a0-0000-7000-8000-000000000002"),
        [Roles.Admin] = new("0193e0a0-0000-7000-8000-000000000003"),
    };

    public void Configure(EntityTypeBuilder<IdentityRole<Guid>> builder) =>
        builder.HasData(Roles.Todos.Select(rol => new
        {
            Id = Ids[rol],
            Name = rol,
            NormalizedName = rol.ToUpperInvariant(),
            ConcurrencyStamp = Ids[rol].ToString(),
            CreadoEn = Creacion,
            ActualizadoEn = Creacion,
        }));
}
