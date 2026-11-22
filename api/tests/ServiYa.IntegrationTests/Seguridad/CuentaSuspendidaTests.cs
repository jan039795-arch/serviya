using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using ServiYa.Api.Seguridad;
using ServiYa.Comun.Errores;
using ServiYa.Comun.Seguridad;
using ServiYa.Cuentas.Dominio;
using ServiYa.Cuentas.Servicios;
using ServiYa.IntegrationTests.Infraestructura;

namespace ServiYa.IntegrationTests.Seguridad;

/// <summary>Cuenta suspendida (T063, FR-041): 403 con el motivo, salvo para ver la sesión, salir y aclarar.</summary>
public sealed class CuentaSuspendidaTests(ServiYaFactory fabrica) : IClassFixture<ServiYaFactory>
{
    private static CancellationToken Cancelacion => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Una_cuenta_suspendida_recibe_403_con_su_tipo_al_modificar_datos()
    {
        using var cliente = await SuspendidoConSesionAsync();

        using var respuesta = await cliente.PostAsync(Ruta("cuentas/yo/roles/prestador"), null, Cancelacion);

        respuesta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var problema = await respuesta.Content.ReadFromJsonAsync<ProblemDetails>(Cancelacion);
        problema!.Type.Should().Be(TiposProblema.CuentaSuspendida);
        problema.Title.Should().Be("Tu cuenta está suspendida.");
    }

    [Fact]
    public async Task Una_cuenta_suspendida_puede_ver_su_sesion_y_salir()
    {
        using var cliente = await SuspendidoConSesionAsync();

        using var yo = await cliente.GetAsync(Ruta("cuentas/yo"), Cancelacion);
        using var logout = await cliente.PostAsync(Ruta("cuentas/logout"), null, Cancelacion);

        yo.StatusCode.Should().Be(HttpStatusCode.OK);
        (await yo.Content.ReadFromJsonAsync<Dictionary<string, object>>(Cancelacion))!["estado"].ToString().Should().Be("Suspendido");
        logout.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Una_cuenta_suspendida_puede_entrar_para_ver_el_motivo()
    {
        var usuario = await fabrica.CrearUsuarioAsync(Roles.Cliente);
        await fabrica.SuspenderAsync(usuario.Id);

        using var cliente = await fabrica.ClienteConSesionAsync(usuario);

        using var yo = await cliente.GetAsync(Ruta("cuentas/yo"), Cancelacion);
        yo.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task El_filtro_pone_el_motivo_en_el_detalle()
    {
        var usuarioId = Guid.CreateVersion7();
        var cuentas = Substitute.For<ICuentasService>();
        cuentas.EstadoDeAsync(usuarioId, Arg.Any<CancellationToken>()).Returns(EstadoUsuario.Suspendido);
        var suspensiones = Substitute.For<IConsultaSuspensiones>();
        suspensiones.MotivoAsync(usuarioId, Arg.Any<CancellationToken>()).Returns("Reportes repetidos.");

        var resultado = await EjecutarFiltroAsync(usuarioId, cuentas, suspensiones);

        var problema = resultado.Should().BeOfType<ProblemHttpResult>().Subject;
        problema.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        problema.ProblemDetails.Type.Should().Be(TiposProblema.CuentaSuspendida);
        problema.ProblemDetails.Detail.Should().Be("Reportes repetidos.");
    }

    [Fact]
    public async Task El_filtro_deja_pasar_a_una_cuenta_activa()
    {
        var usuarioId = Guid.CreateVersion7();
        var cuentas = Substitute.For<ICuentasService>();
        cuentas.EstadoDeAsync(usuarioId, Arg.Any<CancellationToken>()).Returns(EstadoUsuario.Activo);

        var resultado = await EjecutarFiltroAsync(usuarioId, cuentas, Substitute.For<IConsultaSuspensiones>());

        resultado.Should().Be("siguiente");
    }

    /// <summary>Entra con una cuenta activa y luego se suspende: la cookie sigue siendo válida.</summary>
    private async Task<HttpClient> SuspendidoConSesionAsync()
    {
        var usuario = await fabrica.CrearUsuarioAsync(Roles.Cliente);
        var cliente = await fabrica.ClienteConSesionAsync(usuario);
        await fabrica.SuspenderAsync(usuario.Id);
        return cliente;
    }

    private static async Task<object?> EjecutarFiltroAsync(Guid usuarioId, ICuentasService cuentas, IConsultaSuspensiones suspensiones)
    {
        var servicios = new ServiceCollection().AddSingleton(cuentas).AddSingleton(suspensiones).BuildServiceProvider();
        var http = new DefaultHttpContext
        {
            RequestServices = servicios,
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, usuarioId.ToString())], "Prueba")),
        };
        var contexto = EndpointFilterInvocationContext.Create(http);
        return await new CuentaSuspendidaFiltro().InvokeAsync(contexto, _ => ValueTask.FromResult<object?>("siguiente"));
    }

    private static Uri Ruta(string ruta) => new($"/api/v1/{ruta}", UriKind.Relative);
}
