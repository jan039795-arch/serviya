using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ServiYa.Administracion.Datos;
using ServiYa.Administracion.Dominio;
using ServiYa.Api.Comandos;
using ServiYa.Comun.Bitacora;
using ServiYa.Comun.Errores;
using ServiYa.Comun.Seguridad;
using ServiYa.Cuentas.Dominio;
using ServiYa.IntegrationTests.Infraestructura;

namespace ServiYa.IntegrationTests.Administracion;

/// <summary>Otorgar y retirar el rol de administrador (T064, FR-003), siempre con bitácora.</summary>
public sealed class RolesAdminTests(ServiYaFactory fabrica) : IClassFixture<ServiYaFactory>
{
    private static CancellationToken Cancelacion => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Un_admin_otorga_el_rol_a_otra_cuenta_y_queda_en_la_bitacora()
    {
        var admin = await fabrica.CrearUsuarioAsync(Roles.Admin);
        var destino = await fabrica.CrearUsuarioAsync(Roles.Cliente);
        using var cliente = await fabrica.ClienteConSesionAsync(admin);

        using var respuesta = await cliente.PostAsync(RutaRol(destino.Id), null, Cancelacion);

        respuesta.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await RolesDeAsync(destino.Id)).Should().Contain(Roles.Admin);
        var registro = await UltimoRegistroAsync(destino.Id);
        registro.Accion.Should().Be(AccionesBitacora.OtorgarRolAdmin);
        registro.ActorId.Should().Be(admin.Id);
        registro.EntidadTipo.Should().Be(EntidadesBitacora.Usuario);
        registro.Ip.Should().NotBeNull();
    }

    [Fact]
    public async Task El_rol_nuevo_llega_a_la_sesion_abierta_del_destino_en_5_minutos()
    {
        var admin = await fabrica.CrearUsuarioAsync(Roles.Admin);
        var destino = await fabrica.CrearUsuarioAsync(Roles.Cliente);
        using var deAdmin = await fabrica.ClienteConSesionAsync(admin);
        using var deDestino = await fabrica.ClienteConSesionAsync(destino);
        (await deAdmin.PostAsync(RutaRol(destino.Id), null, Cancelacion)).Dispose();

        fabrica.Reloj.Advance(TimeSpan.FromMinutes(6));
        using var yo = await deDestino.GetAsync(new Uri("/api/v1/cuentas/yo", UriKind.Relative), Cancelacion);

        var roles = (await yo.Content.ReadFromJsonAsync<JsonElement>(Cancelacion)).GetProperty("roles").EnumerateArray().Select(r => r.GetString());
        roles.Should().Contain(Roles.Admin);
    }

    [Fact]
    public async Task Un_admin_retira_el_rol_a_otro_admin_y_queda_en_la_bitacora()
    {
        var admin = await fabrica.CrearUsuarioAsync(Roles.Admin);
        var otro = await fabrica.CrearUsuarioAsync(Roles.Cliente, Roles.Admin);
        using var cliente = await fabrica.ClienteConSesionAsync(admin);

        using var respuesta = await cliente.DeleteAsync(RutaRol(otro.Id), Cancelacion);

        respuesta.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await RolesDeAsync(otro.Id)).Should().Equal(Roles.Cliente);
        var registro = await UltimoRegistroAsync(otro.Id);
        registro.Accion.Should().Be(AccionesBitacora.RetirarRolAdmin);
        registro.ActorId.Should().Be(admin.Id);
    }

    [Fact]
    public async Task A_quien_le_retiran_el_rol_pierde_el_acceso_de_inmediato_aunque_su_cookie_diga_Admin()
    {
        var admin = await fabrica.CrearUsuarioAsync(Roles.Admin);
        var otro = await fabrica.CrearUsuarioAsync(Roles.Admin);
        var destino = await fabrica.CrearUsuarioAsync(Roles.Cliente);
        using var deAdmin = await fabrica.ClienteConSesionAsync(admin);
        using var deOtro = await fabrica.ClienteConSesionAsync(otro);
        (await deAdmin.DeleteAsync(RutaRol(otro.Id), Cancelacion)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var respuesta = await deOtro.PostAsync(RutaRol(destino.Id), null, Cancelacion);

        respuesta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await RolesDeAsync(destino.Id)).Should().NotContain(Roles.Admin);
    }

    [Fact]
    public async Task Un_admin_suspendido_no_puede_administrar()
    {
        var admin = await fabrica.CrearUsuarioAsync(Roles.Admin);
        var destino = await fabrica.CrearUsuarioAsync(Roles.Cliente);
        using var cliente = await fabrica.ClienteConSesionAsync(admin);
        await fabrica.SuspenderAsync(admin.Id);

        using var respuesta = await cliente.PostAsync(RutaRol(destino.Id), null, Cancelacion);

        respuesta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await RolesDeAsync(destino.Id)).Should().NotContain(Roles.Admin);
    }

    [Fact]
    public async Task Repetir_la_misma_accion_no_cambia_nada_ni_se_registra_otra_vez()
    {
        var admin = await fabrica.CrearUsuarioAsync(Roles.Admin);
        var destino = await fabrica.CrearUsuarioAsync(Roles.Cliente);
        using var cliente = await fabrica.ClienteConSesionAsync(admin);
        (await cliente.PostAsync(RutaRol(destino.Id), null, Cancelacion)).Dispose();

        using var otraVez = await cliente.PostAsync(RutaRol(destino.Id), null, Cancelacion);

        otraVez.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await RegistrosDeAsync(destino.Id)).Should().HaveCount(1);
    }

    [Fact]
    public async Task Un_admin_no_cambia_su_propio_rol()
    {
        var admin = await fabrica.CrearUsuarioAsync(Roles.Admin);
        using var cliente = await fabrica.ClienteConSesionAsync(admin);

        using var respuesta = await cliente.DeleteAsync(RutaRol(admin.Id), Cancelacion);

        respuesta.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await respuesta.Content.ReadFromJsonAsync<ProblemDetails>(Cancelacion))!.Type.Should().Be(TiposProblema.RolPropio);
        (await RolesDeAsync(admin.Id)).Should().Contain(Roles.Admin);
    }

    [Fact]
    public async Task No_se_otorga_a_una_cuenta_sin_verificar_ni_a_una_que_no_existe()
    {
        var admin = await fabrica.CrearUsuarioAsync(Roles.Admin);
        var sinVerificar = await fabrica.CrearUsuarioAsync(Roles.Cliente);
        await using (var ambito = fabrica.CrearAmbito())
        {
            var usuarios = ambito.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
            var usuario = await usuarios.FindByIdAsync(sinVerificar.Id.ToString());
            usuario!.EmailConfirmed = false;
            (await usuarios.UpdateAsync(usuario)).Succeeded.Should().BeTrue();
        }

        using var cliente = await fabrica.ClienteConSesionAsync(admin);

        using var aSinVerificar = await cliente.PostAsync(RutaRol(sinVerificar.Id), null, Cancelacion);
        using var aNadie = await cliente.PostAsync(RutaRol(Guid.CreateVersion7()), null, Cancelacion);

        aSinVerificar.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        aNadie.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await RegistrosDeAsync(sinVerificar.Id)).Should().BeEmpty();
    }

    [Fact]
    public async Task El_comando_crear_admin_otorga_el_rol_a_una_cuenta_verificada_y_lo_registra_sin_actor()
    {
        var usuario = await fabrica.CrearUsuarioAsync(Roles.Cliente);
        using var salida = new StringWriter();

        var codigo = await ComandosConsola.EjecutarAsync(fabrica.Services, ["crear-admin", "--email", usuario.Email.ToUpperInvariant()], salida, Cancelacion);

        codigo.Should().Be(0);
        (await RolesDeAsync(usuario.Id)).Should().Contain(Roles.Admin);
        var registro = await UltimoRegistroAsync(usuario.Id);
        registro.Accion.Should().Be(AccionesBitacora.OtorgarRolAdmin);
        registro.ActorId.Should().BeNull();
        registro.Detalle.Should().Contain("consola");
        salida.ToString().Should().NotContain(usuario.Email, "la consola no repite datos personales");
    }

    [Theory]
    [InlineData("crear-admin")]
    [InlineData("crear-admin --email")]
    [InlineData("crear-admin --email nadie@correo.test")]
    public async Task El_comando_crear_admin_falla_sin_un_correo_registrado(string argumentos)
    {
        using var salida = new StringWriter();

        var codigo = await ComandosConsola.EjecutarAsync(fabrica.Services, argumentos.Split(' '), salida, Cancelacion);

        codigo.Should().NotBe(0);
        salida.ToString().Should().NotBeEmpty();
    }

    [Fact]
    public void Solo_crear_admin_es_un_comando_de_consola()
    {
        ComandosConsola.EsComando(["crear-admin", "--email", "x@correo.test"]).Should().BeTrue();
        ComandosConsola.EsComando([]).Should().BeFalse();
        ComandosConsola.EsComando(["--urls", "http://localhost:5189"]).Should().BeFalse();
    }

    private static Uri RutaRol(Guid usuarioId) => new($"/api/v1/admin/usuarios/{usuarioId}/roles/admin", UriKind.Relative);

    private async Task<IList<string>> RolesDeAsync(Guid usuarioId)
    {
        await using var ambito = fabrica.CrearAmbito();
        var usuarios = ambito.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
        return await usuarios.GetRolesAsync((await usuarios.FindByIdAsync(usuarioId.ToString()))!);
    }

    private async Task<List<RegistroBitacora>> RegistrosDeAsync(Guid usuarioId)
    {
        await using var ambito = fabrica.CrearAmbito();
        return await ambito.ServiceProvider.GetRequiredService<AdministracionDbContext>().Bitacora
            .Where(r => r.EntidadId == usuarioId)
            .ToListAsync(Cancelacion);
    }

    private async Task<RegistroBitacora> UltimoRegistroAsync(Guid usuarioId) =>
        (await RegistrosDeAsync(usuarioId)).OrderBy(r => r.CreadoEn).ThenBy(r => r.Id).Last();
}
