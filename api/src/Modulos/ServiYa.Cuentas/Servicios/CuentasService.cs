using System.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ServiYa.Comun;
using ServiYa.Comun.Seguridad;
using ServiYa.Cuentas.Datos;
using ServiYa.Cuentas.Dominio;

namespace ServiYa.Cuentas.Servicios;

/// <inheritdoc />
internal sealed class CuentasService(CuentasDbContext cuentas, UserManager<Usuario> usuarios, TimeProvider reloj) : ICuentasService
{
    /// <summary>
    /// Candado de PostgreSQL (<c>pg_advisory_xact_lock</c>) para los cambios del rol de administrador:
    /// dos administradores que se retiran el rol al mismo tiempo se atienden uno tras otro.
    /// </summary>
    private const long CandadoRolAdmin = 0x5E5_1A_AD_0001;

    private static readonly Guid RolAdminId = RolConfiguracion.Ids[Roles.Admin];

    public async Task<bool> EsMayorDeEdadAsync(Guid usuarioId, CancellationToken cancellationToken)
    {
        var usuario = await BuscarAsync(usuarioId, cancellationToken).ConfigureAwait(false);
        return usuario is not null && usuario.EsMayorDeEdad(reloj.HoyEnMexico());
    }

    public async Task<string?> TelefonoDeAsync(Guid usuarioId, CancellationToken cancellationToken) =>
        (await BuscarAsync(usuarioId, cancellationToken).ConfigureAwait(false))?.PhoneNumber;

    public Task<bool> EstaBloqueadoAsync(Guid usuarioId, Guid otroUsuarioId, CancellationToken cancellationToken) =>
        cuentas.Bloqueos.AnyAsync(
            b => (b.BloqueadorId == usuarioId && b.BloqueadoId == otroUsuarioId)
                || (b.BloqueadorId == otroUsuarioId && b.BloqueadoId == usuarioId),
            cancellationToken);

    public async Task RegistrarConsentimientoAsync(
        Guid usuarioId,
        DocumentoLegal documento,
        string version,
        IPAddress? ip,
        CancellationToken cancellationToken)
    {
        cuentas.Consentimientos.Add(new Consentimiento(usuarioId, documento, version, reloj.GetUtcNow(), ip));
        await cuentas.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<string?> NombrePublicoDeAsync(Guid usuarioId, CancellationToken cancellationToken) =>
        await BuscarAsync(usuarioId, cancellationToken).ConfigureAwait(false) is { } usuario
            ? NombrePublico.De(usuario.NombreCompleto)
            : null;

    public async Task<EstadoUsuario?> EstadoDeAsync(Guid usuarioId, CancellationToken cancellationToken) =>
        await cuentas.Users.AsNoTracking()
            .Where(u => u.Id == usuarioId)
            .Select(u => (EstadoUsuario?)u.Estado)
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

    public Task<bool> EsAdminVigenteAsync(Guid usuarioId, CancellationToken cancellationToken) =>
        AdminsActivos().AnyAsync(u => u.Id == usuarioId, cancellationToken);

    public async Task<Guid?> IdPorCorreoAsync(string email, CancellationToken cancellationToken) =>
        (await usuarios.FindByEmailAsync(email.Trim()).ConfigureAwait(false))?.Id;

    public Task<ResultadoCambioAdmin> OtorgarAdminAsync(Guid usuarioId, CancellationToken cancellationToken) =>
        CambiarAdminAsync(usuarioId, otorgar: true, cancellationToken);

    public Task<ResultadoCambioAdmin> RetirarAdminAsync(Guid usuarioId, CancellationToken cancellationToken) =>
        CambiarAdminAsync(usuarioId, otorgar: false, cancellationToken);

    /// <summary>
    /// Con el candado tomado: revisa y cambia el rol. El sello de seguridad no se cambia (eso cerraría
    /// su sesión): la revisión de cada 5 min vuelve a leer los roles y renueva la cookie. Las rutas de
    /// administración no esperan: revisan el rol en la base (<c>Politicas.SoloAdmin</c>).
    /// </summary>
    private async Task<ResultadoCambioAdmin> CambiarAdminAsync(Guid usuarioId, bool otorgar, CancellationToken cancellationToken)
    {
        await using var transaccion = await cuentas.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        await cuentas.Database.ExecuteSqlAsync($"SELECT pg_advisory_xact_lock({CandadoRolAdmin})", cancellationToken).ConfigureAwait(false);

        var usuario = await usuarios.FindByIdAsync(usuarioId.ToString()).ConfigureAwait(false);
        var resultado = await RevisarAsync(usuario, otorgar, cancellationToken).ConfigureAwait(false);
        if (resultado != ResultadoCambioAdmin.Hecho)
        {
            return resultado;
        }

        var cambio = otorgar
            ? await usuarios.AddToRoleAsync(usuario!, Roles.Admin).ConfigureAwait(false)
            : await usuarios.RemoveFromRoleAsync(usuario!, Roles.Admin).ConfigureAwait(false);
        if (!cambio.Succeeded)
        {
            throw new InvalidOperationException("No se pudo cambiar el rol de administrador.");
        }

        await transaccion.CommitAsync(cancellationToken).ConfigureAwait(false);
        return ResultadoCambioAdmin.Hecho;
    }

    private async Task<ResultadoCambioAdmin> RevisarAsync(Usuario? usuario, bool otorgar, CancellationToken cancellationToken)
    {
        if (usuario is null || usuario.Estado == EstadoUsuario.Eliminado)
        {
            return ResultadoCambioAdmin.NoExiste;
        }

        var esAdmin = await usuarios.IsInRoleAsync(usuario, Roles.Admin).ConfigureAwait(false);
        if (otorgar)
        {
            return esAdmin ? ResultadoCambioAdmin.SinCambio
                : usuario.EmailConfirmed ? ResultadoCambioAdmin.Hecho
                : ResultadoCambioAdmin.CorreoSinVerificar;
        }

        if (!esAdmin)
        {
            return ResultadoCambioAdmin.SinCambio;
        }

        var quedanOtros = await AdminsActivos().AnyAsync(u => u.Id != usuario.Id, cancellationToken).ConfigureAwait(false);
        return quedanOtros ? ResultadoCambioAdmin.Hecho : ResultadoCambioAdmin.UltimoAdmin;
    }

    private IQueryable<Usuario> AdminsActivos() =>
        from u in cuentas.Users
        join r in cuentas.UserRoles on u.Id equals r.UserId
        where r.RoleId == RolAdminId && u.Estado == EstadoUsuario.Activo
        select u;

    private Task<Usuario?> BuscarAsync(Guid usuarioId, CancellationToken cancellationToken) =>
        cuentas.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == usuarioId, cancellationToken);
}
