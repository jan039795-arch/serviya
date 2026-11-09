using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace ServiYa.Comun.Datos;

/// <summary>
/// Convenciones comunes de todos los <c>DbContext</c> (data-model.md), sin paquetes extra. Se llama
/// al final de <c>OnModelCreating</c>, cuando ya están todas las entidades:
/// <list type="bullet">
///   <item>tablas, columnas, claves, índices y llaves foráneas en snake_case;</item>
///   <item>columnas <c>created_at</c> y <c>updated_at</c> en todas las tablas (ver <see cref="MarcasTiempo"/>);</item>
///   <item>claves <c>uuid</c> v7 generadas en la app con <see cref="Guid.CreateVersion7()"/>.</item>
/// </list>
/// </summary>
public static class ConvencionNombres
{
    public static ModelBuilder AplicarConvencionesServiYa(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        var entidades = modelBuilder.Model.GetEntityTypes().Where(e => !e.IsOwned()).ToList();
        foreach (var entidad in entidades)
        {
            AgregarMarcasTiempo(modelBuilder, entidad);
            UsarGuidV7(entidad);
        }

        // Los tipos guardados como JSON dentro de una columna no tienen columnas propias.
        foreach (var entidad in modelBuilder.Model.GetEntityTypes().Where(e => !e.IsMappedToJson()))
        {
            RenombrarASnakeCase(entidad);
        }

        return modelBuilder;
    }

    /// <summary>"NormalizedEmail" → "normalized_email", "PK_AspNetUsers" → "pk_asp_net_users".</summary>
    public static string ASnakeCase(string nombre)
    {
        ArgumentNullException.ThrowIfNull(nombre);
        var resultado = new StringBuilder(nombre.Length + 8);
        for (var i = 0; i < nombre.Length; i++)
        {
            var actual = nombre[i];
            if (char.IsUpper(actual) && i > 0 && resultado[^1] != '_')
            {
                var anterior = nombre[i - 1];
                var siguienteEsMinuscula = i + 1 < nombre.Length && char.IsLower(nombre[i + 1]);
                if (char.IsLower(anterior) || char.IsDigit(anterior) || (char.IsUpper(anterior) && siguienteEsMinuscula))
                {
                    resultado.Append('_');
                }
            }

            resultado.Append(char.ToLowerInvariant(actual));
        }

        return resultado.ToString();
    }

    private static void AgregarMarcasTiempo(ModelBuilder modelBuilder, IMutableEntityType entidad)
    {
        // Sin clave no es una tabla propia. Además, configurar un tipo que quedó suelto (como el de
        // passkeys de Identity cuando no se usan) lo volvería explícito y EF exigiría su clave.
        if (entidad.IsKeyless || entidad.FindPrimaryKey() is null)
        {
            return;
        }

        var builder = modelBuilder.Entity(entidad.Name);
        builder.Property<DateTimeOffset>(MarcasTiempo.CreadoEn).HasColumnName("created_at");
        builder.Property<DateTimeOffset>(MarcasTiempo.ActualizadoEn).HasColumnName("updated_at");
    }

    private static void UsarGuidV7(IMutableEntityType entidad)
    {
        var clave = entidad.FindPrimaryKey();
        if (clave is not { Properties: [{ ClrType: var tipo } propiedad] } || tipo != typeof(Guid))
        {
            return;
        }

        if (propiedad.ValueGenerated == ValueGenerated.OnAdd)
        {
            propiedad.SetValueGeneratorFactory((_, _) => new GeneradorGuidV7());
        }
    }

    private static void RenombrarASnakeCase(IMutableEntityType entidad)
    {
        if (entidad.GetTableName() is { } tabla && entidad.FindOwnership() is null)
        {
            entidad.SetTableName(ASnakeCase(tabla));
        }

        foreach (var propiedad in entidad.GetProperties())
        {
            propiedad.SetColumnName(ASnakeCase(propiedad.GetColumnName()));
        }

        foreach (var clave in entidad.GetKeys())
        {
            if (clave.GetName() is { } nombre)
            {
                clave.SetName(ASnakeCase(nombre));
            }
        }

        foreach (var llave in entidad.GetForeignKeys())
        {
            if (llave.GetConstraintName() is { } nombre)
            {
                llave.SetConstraintName(ASnakeCase(nombre));
            }
        }

        foreach (var indice in entidad.GetIndexes())
        {
            if (indice.GetDatabaseName() is { } nombre)
            {
                indice.SetDatabaseName(ASnakeCase(nombre));
            }
        }
    }
}
