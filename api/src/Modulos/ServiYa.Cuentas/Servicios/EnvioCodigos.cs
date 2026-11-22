using Microsoft.EntityFrameworkCore;
using ServiYa.Comun.Correo;
using ServiYa.Comun.Correo.Plantillas;
using ServiYa.Comun.Datos;
using ServiYa.Cuentas.Datos;
using ServiYa.Cuentas.Dominio;

namespace ServiYa.Cuentas.Servicios;

/// <summary>
/// Manda y revisa los códigos por correo con las reglas de <see cref="ServicioCodigos"/>: hasta 3 envíos
/// por hora por usuario y propósito (contando el del registro) y solo vale el último código enviado.
/// </summary>
internal sealed class EnvioCodigos(CuentasDbContext cuentas, ServicioCodigos codigos, IEmailSender correo)
{
    /// <summary>Guarda un código nuevo y lo manda. Devuelve false, sin mandar nada, si ya se mandaron 3 en la última hora.</summary>
    public async Task<bool> EnviarAsync(Usuario usuario, PropositoCodigo proposito, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(usuario);
        var desde = codigos.InicioVentanaEnvios();
        var enviados = await cuentas.CodigosVerificacion
            .CountAsync(
                c => c.UsuarioId == usuario.Id && c.Proposito == proposito && EF.Property<DateTimeOffset>(c, MarcasTiempo.CreadoEn) > desde,
                cancellationToken)
            .ConfigureAwait(false);
        if (!ServicioCodigos.PuedeEnviar(enviados))
        {
            return false;
        }

        var creado = codigos.Crear(usuario.Id, proposito);
        cuentas.CodigosVerificacion.Add(creado.Registro);
        await cuentas.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var mensaje = proposito == PropositoCodigo.VerificarCorreo
            ? PlantillasCorreo.CodigoVerificacion(usuario.Email!, creado.Codigo)
            : PlantillasCorreo.RecuperarContrasena(usuario.Email!, creado.Codigo);
        await correo.EnviarAsync(mensaje, cancellationToken).ConfigureAwait(false);
        return true;
    }

    /// <summary>
    /// Revisa <paramref name="codigoIngresado"/> contra el último código del propósito. Un intento fallido
    /// se guarda de inmediato. Si es correcto, el código queda marcado como usado <b>sin guardar</b>: quien
    /// llama guarda ese cambio junto con el suyo (confirmar el correo o cambiar la contraseña), y si al
    /// guardar otra petición ya lo usó (token <c>xmin</c>), el intento se rechaza.
    /// </summary>
    public async Task<bool> VerificarAsync(Usuario usuario, PropositoCodigo proposito, string? codigoIngresado, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(usuario);
        var registro = await cuentas.CodigosVerificacion
            .Where(c => c.UsuarioId == usuario.Id && c.Proposito == proposito)
            .OrderByDescending(c => EF.Property<DateTimeOffset>(c, MarcasTiempo.CreadoEn))
            .ThenByDescending(c => c.Id)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        if (registro is null)
        {
            return false;
        }

        if (codigos.Verificar(registro, codigoIngresado ?? string.Empty) == ResultadoVerificacion.Correcto)
        {
            return true;
        }

        try
        {
            await cuentas.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Otra petición revisó el mismo código a la vez; su intento ya quedó contado.
        }

        return false;
    }
}
