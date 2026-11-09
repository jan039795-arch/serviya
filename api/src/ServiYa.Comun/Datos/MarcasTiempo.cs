using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ServiYa.Comun.Datos;

/// <summary>
/// Columnas <c>created_at</c> y <c>updated_at</c>. Son propiedades sombra: las entidades no las
/// declaran, pero se pueden consultar con <c>EF.Property&lt;DateTimeOffset&gt;(e, MarcasTiempo.CreadoEn)</c>.
/// </summary>
public static class MarcasTiempo
{
    public const string CreadoEn = nameof(CreadoEn);
    public const string ActualizadoEn = nameof(ActualizadoEn);

    /// <summary>Pone las marcas de lo que se va a guardar. Lo nuevo recibe ambas; lo modificado, solo la de actualización.</summary>
    public static void Aplicar(ChangeTracker cambios, DateTimeOffset ahora)
    {
        ArgumentNullException.ThrowIfNull(cambios);
        foreach (var entrada in cambios.Entries())
        {
            if (entrada.Metadata.FindProperty(CreadoEn) is null)
            {
                continue;
            }

            switch (entrada.State)
            {
                case EntityState.Added:
                    entrada.Property(CreadoEn).CurrentValue = ahora;
                    entrada.Property(ActualizadoEn).CurrentValue = ahora;
                    break;
                case EntityState.Modified:
                    entrada.Property(CreadoEn).IsModified = false;
                    entrada.Property(ActualizadoEn).CurrentValue = ahora;
                    break;
                default:
                    break;
            }
        }
    }
}

/// <summary>Aplica <see cref="MarcasTiempo"/> con el reloj inyectado (<c>FakeTimeProvider</c> en pruebas).</summary>
public sealed class MarcasTiempoInterceptor(TimeProvider reloj) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Aplicar(eventData);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Aplicar(eventData);
        return ValueTask.FromResult(result);
    }

    private void Aplicar(DbContextEventData eventData)
    {
        if (eventData.Context is { } contexto)
        {
            MarcasTiempo.Aplicar(contexto.ChangeTracker, reloj.GetUtcNow());
        }
    }
}
