using Microsoft.EntityFrameworkCore;
using ServiYa.Comun.Datos;

namespace ServiYa.UnitTests.Datos;

/// <summary>Convenciones de persistencia (T045). El modelo se arma sin conectarse a PostgreSQL.</summary>
public sealed class ConvencionesDatosTests
{
    [Theory]
    [InlineData("NormalizedEmail", "normalized_email")]
    [InlineData("PK_AspNetUsers", "pk_asp_net_users")]
    [InlineData("UsuarioId", "usuario_id")]
    [InlineData("IX_Codigo_UsuarioId_Proposito", "ix_codigo_usuario_id_proposito")]
    [InlineData("HTTPServidor", "http_servidor")]
    [InlineData("Paso10Video", "paso10_video")]
    [InlineData("ya_en_snake", "ya_en_snake")]
    [InlineData("Id", "id")]
    public void Convierte_nombres_a_snake_case(string nombre, string esperado)
    {
        ConvencionNombres.ASnakeCase(nombre).Should().Be(esperado);
    }

    [Fact]
    public void Tablas_columnas_claves_e_indices_quedan_en_snake_case_dentro_del_esquema()
    {
        using var contexto = new ContextoDePrueba();
        var entidad = contexto.Model.FindEntityType(typeof(EntidadDePrueba))!;

        entidad.GetSchema().Should().Be("pruebas");
        entidad.GetTableName().Should().Be("entidad_de_prueba");
        entidad.FindProperty(nameof(EntidadDePrueba.NombreCompleto))!.GetColumnName().Should().Be("nombre_completo");
        entidad.FindPrimaryKey()!.GetName().Should().Be("pk_entidad_de_prueba");
        entidad.GetIndexes().Single().GetDatabaseName().Should().Be("ix_entidad_de_prueba_nombre_completo");
    }

    [Fact]
    public void Toda_tabla_tiene_created_at_y_updated_at_como_timestamptz()
    {
        using var contexto = new ContextoDePrueba();
        var entidad = contexto.Model.FindEntityType(typeof(EntidadDePrueba))!;

        var creado = entidad.FindProperty(MarcasTiempo.CreadoEn)!;
        var actualizado = entidad.FindProperty(MarcasTiempo.ActualizadoEn)!;

        creado.GetColumnName().Should().Be("created_at");
        actualizado.GetColumnName().Should().Be("updated_at");
        creado.GetColumnType().Should().Be("timestamp with time zone");
        creado.IsShadowProperty().Should().BeTrue();
    }

    [Fact]
    public void Las_claves_nuevas_son_uuid_version_7()
    {
        using var contexto = new ContextoDePrueba();
        var entidad = new EntidadDePrueba { NombreCompleto = "Rita Ficticia" };

        contexto.Add(entidad);

        entidad.Id.Version.Should().Be(7);
    }

    [Fact]
    public void Lo_nuevo_recibe_ambas_marcas_y_lo_modificado_solo_la_de_actualizacion()
    {
        using var contexto = new ContextoDePrueba();
        var creadoEn = new DateTimeOffset(2026, 11, 8, 10, 0, 0, TimeSpan.Zero);
        var entidad = new EntidadDePrueba { NombreCompleto = "Rita Ficticia" };
        contexto.Add(entidad);

        MarcasTiempo.Aplicar(contexto.ChangeTracker, creadoEn);
        var entrada = contexto.Entry(entidad);
        entrada.Property(MarcasTiempo.CreadoEn).CurrentValue.Should().Be(creadoEn);
        entrada.Property(MarcasTiempo.ActualizadoEn).CurrentValue.Should().Be(creadoEn);

        entrada.State = EntityState.Unchanged;
        entidad.NombreCompleto = "Rita Ficticia Luna";
        contexto.ChangeTracker.DetectChanges();
        var actualizadoEn = creadoEn.AddHours(2);
        MarcasTiempo.Aplicar(contexto.ChangeTracker, actualizadoEn);

        entrada.Property(MarcasTiempo.CreadoEn).CurrentValue.Should().Be(creadoEn);
        entrada.Property(MarcasTiempo.CreadoEn).IsModified.Should().BeFalse();
        entrada.Property(MarcasTiempo.ActualizadoEn).CurrentValue.Should().Be(actualizadoEn);
    }

    private sealed class EntidadDePrueba
    {
        public Guid Id { get; set; }

        public string NombreCompleto { get; set; } = string.Empty;
    }

    private sealed class ContextoDePrueba : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
            optionsBuilder.UseNpgsql("Host=localhost;Database=ficticia");

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("pruebas");
            modelBuilder.Entity<EntidadDePrueba>().HasIndex(e => e.NombreCompleto);
            modelBuilder.AplicarConvencionesServiYa();
        }
    }
}
