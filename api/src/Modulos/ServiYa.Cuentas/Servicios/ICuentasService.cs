using System.Net;
using ServiYa.Cuentas.Dominio;

namespace ServiYa.Cuentas.Servicios;

/// <summary>Resultado de otorgar o retirar el rol de administrador.</summary>
public enum ResultadoCambioAdmin
{
    Hecho,

    /// <summary>Ya lo tenía (al otorgar) o ya no lo tenía (al retirar).</summary>
    SinCambio,
    NoExiste,
    CorreoSinVerificar,

    /// <summary>Quitarlo dejaría la plataforma sin ningún administrador activo (FR-003).</summary>
    UltimoAdmin,
}

/// <summary>
/// Lo que los demás módulos saben de una cuenta (plan.md, T062). Ningún módulo lee las tablas de
/// <c>cuentas</c>: pregunta aquí. Para un usuario que no existe, las consultas responden lo más
/// restrictivo (no es mayor de edad, sin teléfono, sin estado).
/// </summary>
public interface ICuentasService
{
    /// <summary>18 años cumplidos hoy, en hora de México (FR-004).</summary>
    Task<bool> EsMayorDeEdadAsync(Guid usuarioId, CancellationToken cancellationToken);

    /// <summary>Teléfono privado (E.164). Solo para quien tiene un acceso activo al contacto.</summary>
    Task<string?> TelefonoDeAsync(Guid usuarioId, CancellationToken cancellationToken);

    /// <summary>Si alguno de los dos bloqueó al otro (FR-040).</summary>
    Task<bool> EstaBloqueadoAsync(Guid usuarioId, Guid otroUsuarioId, CancellationToken cancellationToken);

    /// <summary>Agrega la aceptación de un documento legal con la fecha del reloj (FR-007, FR-044).</summary>
    Task RegistrarConsentimientoAsync(Guid usuarioId, DocumentoLegal documento, string version, IPAddress? ip, CancellationToken cancellationToken);

    /// <summary>Nombre de pila + inicial del apellido (<see cref="NombrePublico"/>).</summary>
    Task<string?> NombrePublicoDeAsync(Guid usuarioId, CancellationToken cancellationToken);

    Task<EstadoUsuario?> EstadoDeAsync(Guid usuarioId, CancellationToken cancellationToken);

    /// <summary>Tiene hoy el rol <c>Admin</c> en la base y su cuenta está activa (no se fía de la cookie).</summary>
    Task<bool> EsAdminVigenteAsync(Guid usuarioId, CancellationToken cancellationToken);

    Task<Guid?> IdPorCorreoAsync(string email, CancellationToken cancellationToken);

    /// <summary>Otorga el rol a una cuenta verificada y no eliminada.</summary>
    Task<ResultadoCambioAdmin> OtorgarAdminAsync(Guid usuarioId, CancellationToken cancellationToken);

    /// <summary>Retira el rol, salvo que no quede ningún otro administrador activo.</summary>
    Task<ResultadoCambioAdmin> RetirarAdminAsync(Guid usuarioId, CancellationToken cancellationToken);
}
