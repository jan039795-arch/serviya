using System.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using ServiYa.Comun.Seguridad;
using ServiYa.Cuentas.Dominio;
using ServiYa.IntegrationTests.Infraestructura;

namespace ServiYa.IntegrationTests.Administracion;

/// <summary>
/// Nunca se queda la plataforma sin administradores (FR-003). En su propia base: solo existen los dos
/// administradores de la prueba.
/// </summary>
public sealed class UltimoAdminTests(ServiYaFactory fabrica) : IClassFixture<ServiYaFactory>
{
    private static CancellationToken Cancelacion => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Dos_admins_que_se_retiran_el_rol_al_mismo_tiempo_no_dejan_la_plataforma_sin_admin()
    {
        var primero = await fabrica.CrearUsuarioAsync(Roles.Admin);
        var segundo = await fabrica.CrearUsuarioAsync(Roles.Admin);
        using var dePrimero = await fabrica.ClienteConSesionAsync(primero);
        using var deSegundo = await fabrica.ClienteConSesionAsync(segundo);

        var respuestas = await Task.WhenAll(
            dePrimero.DeleteAsync(RutaRol(segundo.Id), Cancelacion),
            deSegundo.DeleteAsync(RutaRol(primero.Id), Cancelacion));

        respuestas.Select(r => r.StatusCode).Should().BeEquivalentTo([HttpStatusCode.NoContent, HttpStatusCode.Conflict]);
        foreach (var respuesta in respuestas)
        {
            respuesta.Dispose();
        }

        await using var ambito = fabrica.CrearAmbito();
        (await ambito.ServiceProvider.GetRequiredService<UserManager<Usuario>>().GetUsersInRoleAsync(Roles.Admin))
            .Should().ContainSingle();
    }

    private static Uri RutaRol(Guid usuarioId) => new($"/api/v1/admin/usuarios/{usuarioId}/roles/admin", UriKind.Relative);
}
