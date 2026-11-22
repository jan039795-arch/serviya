using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiYa.Busqueda.Dominio;

namespace ServiYa.Busqueda.Datos;

internal sealed class OficioConfiguracion : IEntityTypeConfiguration<Oficio>
{
    private static readonly DateTimeOffset Creacion = new(2026, 11, 22, 0, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// Semilla de data-model.md, con los mismos identificadores que los datos ficticios del prototipo
    /// (<c>web/src/mocks/oficios.json</c>). Los oficios del piloto siguen como pendiente abierto.
    /// </summary>
    private static readonly (string Nombre, string Slug, string Icono)[] Semilla =
    [
        ("Albañil", "albanil", "ladrillo"),
        ("Fontanero", "fontanero", "llave"),
        ("Electricista", "electricista", "rayo"),
        ("Carpintero", "carpintero", "serrucho"),
        ("Pintor", "pintor", "brocha"),
        ("Jardinero", "jardinero", "planta"),
        ("Chofer", "chofer", "auto"),
        ("Maestro particular", "maestro-particular", "libro"),
        ("Limpieza del hogar", "limpieza-del-hogar", "escoba"),
        ("Cuidado de niños", "cuidado-de-ninos", "nino"),
        ("Cuidado de adultos mayores", "cuidado-de-adultos-mayores", "manos"),
        ("Cerrajero", "cerrajero", "llave-puerta"),
    ];

    public void Configure(EntityTypeBuilder<Oficio> builder)
    {
        builder.ToTable("oficios");
        builder.Property(o => o.Nombre).HasMaxLength(Oficio.LargoMaximoNombre).IsRequired();
        builder.Property(o => o.Slug).HasMaxLength(Oficio.LargoMaximoSlug).IsRequired();
        builder.Property(o => o.Icono).HasMaxLength(Oficio.LargoMaximoIcono).IsRequired();
        builder.HasIndex(o => o.Slug).IsUnique();
        builder.HasData(Semilla.Select((oficio, i) => new
        {
            Id = new Guid($"a0000000-0000-7000-8000-{i + 1:D12}"),
            oficio.Nombre,
            oficio.Slug,
            oficio.Icono,
            Activo = true,
            CreadoEn = Creacion,
            ActualizadoEn = Creacion,
        }));
    }
}

internal sealed class MunicipioConfiguracion : IEntityTypeConfiguration<Municipio>
{
    public void Configure(EntityTypeBuilder<Municipio> builder)
    {
        builder.ToTable("municipios");
        builder.Property(m => m.ClaveInegi).HasMaxLength(Municipio.LargoClaveInegi).IsFixedLength().IsRequired();
        builder.Property(m => m.Nombre).HasMaxLength(Municipio.LargoMaximoNombre).IsRequired();
        builder.Property(m => m.Estado).HasMaxLength(Municipio.LargoMaximoEstado).IsRequired();
        builder.Property(m => m.Poligono).HasColumnType("geography (multipolygon, 4326)").IsRequired();
        builder.Property(m => m.Centroide).HasColumnType("geography (point, 4326)").IsRequired();
        builder.HasIndex(m => m.ClaveInegi).IsUnique();

        // "Punto dentro de polígono" para ubicar al cliente y validar la cobertura del prestador.
        builder.HasIndex(m => m.Poligono).HasMethod("gist");
    }
}
