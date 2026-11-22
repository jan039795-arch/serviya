using System.Text.Json;
using ServiYa.Administracion.Datos;
using ServiYa.Administracion.Dominio;
using ServiYa.Comun.Bitacora;

namespace ServiYa.Administracion.Servicios;

/// <summary>
/// <see cref="IBitacora"/> sobre <c>admin.registro_bitacora</c>. Usa el <c>DbContext</c> de
/// Administración, así que guarda en su propia transacción aunque quien llama tenga cambios
/// pendientes en el suyo.
/// </summary>
internal sealed class Bitacora(AdministracionDbContext contexto) : IBitacora
{
    public async Task RegistrarAsync(EntradaBitacora entrada, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entrada);
        var detalle = JsonSerializer.Serialize(entrada.Detalle);
        contexto.Bitacora.Add(RegistroBitacora.Desde(entrada, detalle));
        await contexto.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
