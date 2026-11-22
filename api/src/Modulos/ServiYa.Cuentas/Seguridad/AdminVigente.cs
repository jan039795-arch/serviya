using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using ServiYa.Cuentas.Servicios;

namespace ServiYa.Cuentas.Seguridad;

/// <summary>Requisito de <see cref="Comun.Seguridad.Politicas.SoloAdmin"/>: el rol sigue vigente en la base.</summary>
internal sealed class AdminVigente : IAuthorizationRequirement;

internal sealed class AdminVigenteHandler(ICuentasService cuentas) : AuthorizationHandler<AdminVigente>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, AdminVigente requirement)
    {
        if (Guid.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var usuarioId)
            && await cuentas.EsAdminVigenteAsync(usuarioId, CancellationToken.None).ConfigureAwait(false))
        {
            context.Succeed(requirement);
        }
    }
}
