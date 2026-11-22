using System.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ServiYa.Comun;
using ServiYa.Comun.Seguridad;
using ServiYa.Cuentas.Datos;
using ServiYa.Cuentas.Dominio;
using ServiYa.Cuentas.Servicios;
using ServiYa.IntegrationTests.Infraestructura;

namespace ServiYa.IntegrationTests.Cuentas;

/// <summary><c>ICuentasService</c>, lo que los demás módulos saben de una cuenta (T062).</summary>
public sealed class CuentasServiceTests(ServiYaFactory fabrica) : IClassFixture<ServiYaFactory>
{
    private static readonly Guid NoExiste = Guid.CreateVersion7();

    private static CancellationToken Cancelacion => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Dice_si_es_mayor_de_edad_con_la_fecha_de_hoy_en_Mexico()
    {
        var hoy = fabrica.Reloj.HoyEnMexico();
        var mayor = await fabrica.CrearUsuarioAsync(hoy.AddYears(-18), Roles.Cliente);
        var menor = await fabrica.CrearUsuarioAsync(hoy.AddYears(-18).AddDays(1), Roles.Cliente);

        await ConServicioAsync(async cuentas =>
        {
            (await cuentas.EsMayorDeEdadAsync(mayor.Id, Cancelacion)).Should().BeTrue();
            (await cuentas.EsMayorDeEdadAsync(menor.Id, Cancelacion)).Should().BeFalse();
            (await cuentas.EsMayorDeEdadAsync(NoExiste, Cancelacion)).Should().BeFalse();
        });
    }

    [Fact]
    public async Task Devuelve_el_telefono_privado_y_el_nombre_publico()
    {
        var usuario = await fabrica.CrearUsuarioAsync(Roles.Prestador);
        await using (var ambito = fabrica.CrearAmbito())
        {
            var usuarios = ambito.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
            var guardado = await usuarios.FindByIdAsync(usuario.Id.ToString());
            (await usuarios.SetPhoneNumberAsync(guardado!, "+525512345678")).Succeeded.Should().BeTrue();
        }

        await ConServicioAsync(async cuentas =>
        {
            (await cuentas.TelefonoDeAsync(usuario.Id, Cancelacion)).Should().Be("+525512345678");
            (await cuentas.NombrePublicoDeAsync(usuario.Id, Cancelacion)).Should().Be("Persona F.");
            (await cuentas.TelefonoDeAsync(NoExiste, Cancelacion)).Should().BeNull();
            (await cuentas.NombrePublicoDeAsync(NoExiste, Cancelacion)).Should().BeNull();
        });
    }

    [Fact]
    public async Task Un_bloqueo_cuenta_en_ambos_sentidos()
    {
        var bloqueador = await fabrica.CrearUsuarioAsync(Roles.Cliente);
        var bloqueado = await fabrica.CrearUsuarioAsync(Roles.Prestador);
        var tercero = await fabrica.CrearUsuarioAsync(Roles.Cliente);
        await using (var ambito = fabrica.CrearAmbito())
        {
            var datos = ambito.ServiceProvider.GetRequiredService<CuentasDbContext>();
            datos.Bloqueos.Add(new Bloqueo(bloqueador.Id, bloqueado.Id));
            await datos.SaveChangesAsync(Cancelacion);
        }

        await ConServicioAsync(async cuentas =>
        {
            (await cuentas.EstaBloqueadoAsync(bloqueador.Id, bloqueado.Id, Cancelacion)).Should().BeTrue();
            (await cuentas.EstaBloqueadoAsync(bloqueado.Id, bloqueador.Id, Cancelacion)).Should().BeTrue();
            (await cuentas.EstaBloqueadoAsync(bloqueador.Id, tercero.Id, Cancelacion)).Should().BeFalse();
        });
    }

    [Fact]
    public async Task Registra_un_consentimiento_con_la_fecha_del_reloj()
    {
        var usuario = await fabrica.CrearUsuarioAsync(Roles.Prestador);

        await ConServicioAsync(cuentas => cuentas.RegistrarConsentimientoAsync(
            usuario.Id, DocumentoLegal.DatosSensibles, "2026-11", IPAddress.Parse("198.18.0.9"), Cancelacion));

        await using var ambito = fabrica.CrearAmbito();
        var consentimiento = await ambito.ServiceProvider.GetRequiredService<CuentasDbContext>().Consentimientos
            .SingleAsync(c => c.UsuarioId == usuario.Id, Cancelacion);
        consentimiento.Documento.Should().Be(DocumentoLegal.DatosSensibles);
        consentimiento.Version.Should().Be("2026-11");
        consentimiento.AceptadoEn.Should().Be(fabrica.Reloj.GetUtcNow());
        consentimiento.Ip.Should().Be(IPAddress.Parse("198.18.0.9"));
    }

    [Fact]
    public async Task Devuelve_el_estado_de_la_cuenta()
    {
        var usuario = await fabrica.CrearUsuarioAsync(Roles.Cliente);
        await fabrica.SuspenderAsync(usuario.Id);

        await ConServicioAsync(async cuentas =>
        {
            (await cuentas.EstadoDeAsync(usuario.Id, Cancelacion)).Should().Be(EstadoUsuario.Suspendido);
            (await cuentas.EstadoDeAsync(NoExiste, Cancelacion)).Should().BeNull();
        });
    }

    private async Task ConServicioAsync(Func<ICuentasService, Task> prueba)
    {
        await using var ambito = fabrica.CrearAmbito();
        await prueba(ambito.ServiceProvider.GetRequiredService<ICuentasService>());
    }
}
