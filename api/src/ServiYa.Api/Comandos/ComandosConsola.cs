using ServiYa.Comun.Bitacora;
using ServiYa.Cuentas.Servicios;

namespace ServiYa.Api.Comandos;

/// <summary>
/// Comandos que se corren con <c>dotnet run --project src/ServiYa.Api -- &lt;comando&gt;</c> en lugar de
/// levantar la API (T064). Por ahora solo <c>crear-admin --email &lt;correo&gt;</c>, para el primer
/// administrador: los siguientes se otorgan desde la API. La salida no repite el correo.
/// </summary>
internal static class ComandosConsola
{
    private const string CrearAdmin = "crear-admin";

    public static bool EsComando(IReadOnlyList<string> argumentos)
    {
        ArgumentNullException.ThrowIfNull(argumentos);
        return argumentos.Count > 0 && argumentos[0] == CrearAdmin;
    }

    /// <returns>0 si quedó hecho, 1 si la cuenta no sirve y 2 si los argumentos no son válidos.</returns>
    public static async Task<int> EjecutarAsync(
        IServiceProvider servicios,
        IReadOnlyList<string> argumentos,
        TextWriter salida,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(servicios);
        ArgumentNullException.ThrowIfNull(salida);
        if (argumentos is not [CrearAdmin, "--email", var email] || string.IsNullOrWhiteSpace(email))
        {
            await salida.WriteLineAsync("Uso: crear-admin --email <correo de una cuenta verificada>").ConfigureAwait(false);
            return 2;
        }

        await using var ambito = servicios.CreateAsyncScope();
        var cuentas = ambito.ServiceProvider.GetRequiredService<ICuentasService>();
        var id = await cuentas.IdPorCorreoAsync(email, cancellationToken).ConfigureAwait(false);
        var resultado = id is null
            ? ResultadoCambioAdmin.NoExiste
            : await cuentas.OtorgarAdminAsync(id.Value, cancellationToken).ConfigureAwait(false);

        if (resultado == ResultadoCambioAdmin.Hecho)
        {
            // Sin actor: lo hizo quien tiene acceso al servidor, no una cuenta.
            await ambito.ServiceProvider.GetRequiredService<IBitacora>().RegistrarAsync(
                new EntradaBitacora(null, AccionesBitacora.OtorgarRolAdmin, EntidadesBitacora.Usuario, id, null,
                    new Dictionary<string, string> { ["origen"] = "consola" }),
                cancellationToken).ConfigureAwait(false);
        }

        var (mensaje, codigo) = resultado switch
        {
            ResultadoCambioAdmin.Hecho => ("Listo: la cuenta ahora es administradora.", 0),
            ResultadoCambioAdmin.SinCambio => ("La cuenta ya era administradora.", 0),
            ResultadoCambioAdmin.CorreoSinVerificar => ("La cuenta todavía no verifica su correo.", 1),
            _ => ("No hay una cuenta con ese correo.", 1),
        };
        await salida.WriteLineAsync(mensaje).ConfigureAwait(false);
        return codigo;
    }
}
