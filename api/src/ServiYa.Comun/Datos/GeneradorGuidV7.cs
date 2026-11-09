using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace ServiYa.Comun.Datos;

/// <summary>Claves <c>uuid</c> v7: ordenadas por tiempo, así los índices no se fragmentan.</summary>
public sealed class GeneradorGuidV7 : ValueGenerator<Guid>
{
    public override bool GeneratesTemporaryValues => false;

    public override Guid Next(EntityEntry entry) => Guid.CreateVersion7();
}
