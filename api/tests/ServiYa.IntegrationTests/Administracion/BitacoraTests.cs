using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using ServiYa.Administracion.Datos;
using ServiYa.Comun.Bitacora;
using ServiYa.Comun.Privacidad;
using ServiYa.IntegrationTests.Infraestructura;

namespace ServiYa.IntegrationTests.Administracion;

/// <summary>Bitácora en <c>admin.registro_bitacora</c> (T049): se registra y la app no la puede alterar.</summary>
public sealed class BitacoraTests(ServiYaFactory fabrica) : IClassFixture<ServiYaFactory>
{
    private static CancellationToken Cancelacion => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Registra_la_accion_con_el_reloj_de_la_app_y_el_detalle_en_jsonb()
    {
        var actor = Guid.CreateVersion7();
        var documento = Guid.CreateVersion7();
        var entrada = new EntradaBitacora(
            actor,
            AccionesBitacora.RechazarDocumento,
            EntidadesBitacora.Documento,
            documento,
            IPAddress.Parse("2001:db8::7"),
            new Dictionary<string, string> { ["motivo"] = "Foto borrosa; llamar al 55 1234 5678" });

        await RegistrarAsync(entrada);

        await using var ambito = fabrica.CrearAmbito();
        var registro = await ambito.ServiceProvider.GetRequiredService<AdministracionDbContext>().Bitacora
            .SingleAsync(r => r.EntidadId == documento, Cancelacion);
        registro.Id.Version.Should().Be(7);
        registro.ActorId.Should().Be(actor);
        registro.Accion.Should().Be("RechazarDocumento");
        registro.EntidadTipo.Should().Be("Documento");
        registro.Ip.Should().Be(IPAddress.Parse("2001:db8::7"));
        registro.CreadoEn.Should().Be(fabrica.Reloj.GetUtcNow());
        var detalle = JsonSerializer.Deserialize<Dictionary<string, string>>(registro.Detalle)!;
        detalle["motivo"].Should().StartWith("Foto borrosa").And.Contain(ContactMasker.Marcador).And.NotContain("1234");
        (await LeerTipoDeColumnaAsync("detalle")).Should().Be("jsonb");
    }

    [Fact]
    public async Task Las_acciones_del_sistema_se_registran_sin_actor_ni_ip()
    {
        var usuario = Guid.CreateVersion7();

        await RegistrarAsync(new EntradaBitacora(null, AccionesBitacora.SuspenderCuenta, EntidadesBitacora.Usuario, usuario, null));

        await using var ambito = fabrica.CrearAmbito();
        var registro = await ambito.ServiceProvider.GetRequiredService<AdministracionDbContext>().Bitacora
            .SingleAsync(r => r.EntidadId == usuario, Cancelacion);
        registro.ActorId.Should().BeNull();
        registro.Ip.Should().BeNull();
        registro.Detalle.Should().Be("{}");
    }

    [Theory]
    [InlineData("UPDATE admin.registro_bitacora SET accion = 'Alterada'")]
    [InlineData("DELETE FROM admin.registro_bitacora")]
    [InlineData("TRUNCATE admin.registro_bitacora")]
    public async Task La_app_no_puede_modificar_ni_borrar_la_bitacora(string sql)
    {
        await RegistrarAsync(new EntradaBitacora(Guid.CreateVersion7(), AccionesBitacora.VerDocumento, EntidadesBitacora.Documento, Guid.CreateVersion7(), null));
        var antes = await ContarAsync();

        var ejecutar = () => EjecutarComoAppAsync(sql);

        (await ejecutar.Should().ThrowAsync<PostgresException>())
            .Which.SqlState.Should().Be(PostgresErrorCodes.InsufficientPrivilege);
        (await ContarAsync()).Should().Be(antes);
    }

    [Fact]
    public async Task La_app_si_puede_leer_e_insertar_en_la_bitacora()
    {
        var antes = await ContarAsync();

        await RegistrarAsync(new EntradaBitacora(Guid.CreateVersion7(), AccionesBitacora.AprobarPerfil, EntidadesBitacora.Prestador, Guid.CreateVersion7(), null));

        (await ContarAsync()).Should().Be(antes + 1);
    }

    private async Task RegistrarAsync(EntradaBitacora entrada)
    {
        await using var ambito = fabrica.CrearAmbito();
        await ambito.ServiceProvider.GetRequiredService<IBitacora>().RegistrarAsync(entrada, Cancelacion);
    }

    private async Task<long> ContarAsync()
    {
        await using var conexion = new NpgsqlConnection(fabrica.ConexionApp);
        await conexion.OpenAsync(Cancelacion);
        await using var comando = new NpgsqlCommand("SELECT count(*) FROM admin.registro_bitacora", conexion);
        return (long)(await comando.ExecuteScalarAsync(Cancelacion))!;
    }

    private async Task EjecutarComoAppAsync(string sql)
    {
        await using var conexion = new NpgsqlConnection(fabrica.ConexionApp);
        await conexion.OpenAsync(Cancelacion);
#pragma warning disable CA2100 // SQL fijo de la prueba.
        await using var comando = new NpgsqlCommand(sql, conexion);
#pragma warning restore CA2100
        await comando.ExecuteNonQueryAsync(Cancelacion);
    }

    private async Task<string> LeerTipoDeColumnaAsync(string columna)
    {
        await using var conexion = new NpgsqlConnection(fabrica.ConexionDueno);
        await conexion.OpenAsync(Cancelacion);
        await using var comando = new NpgsqlCommand(
            "SELECT data_type FROM information_schema.columns WHERE table_schema = 'admin' AND table_name = 'registro_bitacora' AND column_name = @columna",
            conexion);
        comando.Parameters.AddWithValue("columna", columna);
        return (string)(await comando.ExecuteScalarAsync(Cancelacion))!;
    }
}
