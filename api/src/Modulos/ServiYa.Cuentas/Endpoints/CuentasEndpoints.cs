using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ServiYa.Comun.Seguridad;

namespace ServiYa.Cuentas.Endpoints;

/// <summary>
/// Rutas de <c>/api/v1/cuentas</c> (contracts/api.md, T061). Las públicas que reciben correos o códigos
/// tienen el límite de 5 por minuto por IP; las demás piden sesión por la política por defecto.
/// </summary>
internal static class CuentasEndpoints
{
    public static void MapearCuentas(this IEndpointRouteBuilder api)
    {
        var cuentas = api.MapGroup("/cuentas").WithTags("Cuentas");

        var publicas = cuentas.MapGroup(string.Empty).AllowAnonymous().RequireRateLimiting(PoliticasLimite.LoginCodigos);
        publicas.MapPost("/registro", RegistroEndpoints.RegistrarAsync);
        publicas.MapPost("/verificar-correo", RegistroEndpoints.VerificarCorreoAsync);
        publicas.MapPost("/reenviar-codigo", RegistroEndpoints.ReenviarCodigoAsync);
        publicas.MapPost("/login", SesionEndpoints.EntrarAsync);
        publicas.MapPost("/recuperar", RecuperacionEndpoints.RecuperarAsync);
        publicas.MapPost("/restablecer", RecuperacionEndpoints.RestablecerAsync);

        cuentas.MapGet("/xsrf", SesionEndpoints.EmitirXsrf).AllowAnonymous();

        // Una cuenta suspendida también puede ver su sesión (y el motivo) y salir.
        cuentas.MapPost("/logout", SesionEndpoints.SalirAsync).WithMetadata(new PermitidoASuspendidosAttribute());
        cuentas.MapGet("/yo", SesionEndpoints.YoAsync).WithMetadata(new PermitidoASuspendidosAttribute());
        cuentas.MapPost("/yo/roles/prestador", SesionEndpoints.HacersePrestadorAsync);
    }
}
