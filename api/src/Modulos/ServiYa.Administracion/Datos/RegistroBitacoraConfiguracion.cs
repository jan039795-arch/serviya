using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiYa.Administracion.Dominio;
using ServiYa.Comun.Bitacora;

namespace ServiYa.Administracion.Datos;

internal sealed class RegistroBitacoraConfiguracion : IEntityTypeConfiguration<RegistroBitacora>
{
    public const string Tabla = "registro_bitacora";

    public void Configure(EntityTypeBuilder<RegistroBitacora> builder)
    {
        builder.ToTable(Tabla);
        builder.Property(r => r.Accion).HasMaxLength(EntradaBitacora.LargoMaximoNombre).IsRequired();
        builder.Property(r => r.EntidadTipo).HasMaxLength(EntradaBitacora.LargoMaximoNombre).IsRequired();
        builder.Property(r => r.Detalle).HasColumnType("jsonb").IsRequired();

        // La convención crea la propiedad CreadoEn (created_at); aquí solo se nombra la de la entidad.
        builder.Property(r => r.CreadoEn).HasColumnName("created_at");

        // Historial de una entidad (un perfil, un documento) y lo hecho por un administrador.
        builder.HasIndex(r => new { r.EntidadTipo, r.EntidadId });
        builder.HasIndex(r => r.ActorId);
        builder.HasIndex(r => r.CreadoEn);
    }
}
