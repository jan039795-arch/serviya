using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using ServiYa.Comun.Datos;
using ServiYa.Cuentas.Datos;
using ServiYa.Cuentas.Dominio;
using ServiYa.Cuentas.Servicios;
using ServiYa.IntegrationTests.Infraestructura;

namespace ServiYa.IntegrationTests.Datos;

/// <summary>La base real: migraciones, permisos del rol de la app, marcas de tiempo y concurrencia (T045, T046, T057).</summary>
public sealed class BaseDeDatosTests(ServiYaFactory fabrica) : IClassFixture<ServiYaFactory>
{
    private static CancellationToken Cancelacion => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Las_migraciones_crean_las_tablas_de_cuentas_y_no_quedan_pendientes()
    {
        await using var ambito = fabrica.CrearAmbito();
        var cuentas = ambito.ServiceProvider.GetRequiredService<CuentasDbContext>();

        (await cuentas.Database.GetPendingMigrationsAsync(Cancelacion)).Should().BeEmpty();
        (await cuentas.Database.GetAppliedMigrationsAsync(Cancelacion)).Should().ContainSingle(m => m.EndsWith("_Inicial", StringComparison.Ordinal));
        (await cuentas.Database.CanConnectAsync(Cancelacion)).Should().BeTrue();
    }

    [Theory]
    [InlineData("CREATE TABLE cuentas.intrusa (id int)")]
    [InlineData("ALTER TABLE cuentas.usuarios ADD COLUMN intrusa int")]
    [InlineData("DROP TABLE cuentas.bloqueos")]
    public async Task La_app_no_puede_cambiar_el_esquema(string sql)
    {
        await using var conexion = new NpgsqlConnection(fabrica.ConexionApp);
        await conexion.OpenAsync(Cancelacion);
#pragma warning disable CA2100 // SQL fijo de la prueba.
        await using var comando = new NpgsqlCommand(sql, conexion);
#pragma warning restore CA2100

        var ejecutar = () => comando.ExecuteNonQueryAsync(Cancelacion);

        (await ejecutar.Should().ThrowAsync<PostgresException>())
            .Which.SqlState.Should().BeOneOf(PostgresErrorCodes.InsufficientPrivilege, "42501");
    }

    [Fact]
    public async Task Al_guardar_se_ponen_las_marcas_con_el_reloj_y_las_claves_son_uuid_v7()
    {
        var usuario = NuevoUsuario();
        await using (var ambito = fabrica.CrearAmbito())
        {
            var cuentas = ambito.ServiceProvider.GetRequiredService<CuentasDbContext>();
            cuentas.Users.Add(usuario);
            await cuentas.SaveChangesAsync(Cancelacion);
        }

        await using var lectura = fabrica.CrearAmbito();
        var guardado = await lectura.ServiceProvider.GetRequiredService<CuentasDbContext>().Users
            .Where(u => u.Id == usuario.Id)
            .Select(u => new
            {
                Creado = EF.Property<DateTimeOffset>(u, MarcasTiempo.CreadoEn),
                Actualizado = EF.Property<DateTimeOffset>(u, MarcasTiempo.ActualizadoEn),
            })
            .SingleAsync(Cancelacion);

        usuario.Id.Version.Should().Be(7);
        guardado.Creado.Should().Be(fabrica.Reloj.GetUtcNow());
        guardado.Actualizado.Should().Be(fabrica.Reloj.GetUtcNow());
    }

    [Fact]
    public async Task Dos_verificaciones_simultaneas_del_mismo_codigo_no_se_saltan_los_intentos()
    {
        var servicio = fabrica.Services.GetRequiredService<ServicioCodigos>();
        var usuario = NuevoUsuario();
        Guid codigoId;
        string codigo;
        await using (var ambito = fabrica.CrearAmbito())
        {
            var cuentas = ambito.ServiceProvider.GetRequiredService<CuentasDbContext>();
            cuentas.Users.Add(usuario);
            var creado = servicio.Crear(usuario.Id, PropositoCodigo.VerificarCorreo);
            codigo = creado.Codigo;
            cuentas.CodigosVerificacion.Add(creado.Registro);
            await cuentas.SaveChangesAsync(Cancelacion);
            codigoId = creado.Registro.Id;
        }

        // Dos peticiones leen el mismo código antes de que cualquiera guarde.
        await using var primera = fabrica.CrearAmbito();
        await using var segunda = fabrica.CrearAmbito();
        var cuentasA = primera.ServiceProvider.GetRequiredService<CuentasDbContext>();
        var cuentasB = segunda.ServiceProvider.GetRequiredService<CuentasDbContext>();
        var registroA = await cuentasA.CodigosVerificacion.SingleAsync(c => c.Id == codigoId, Cancelacion);
        var registroB = await cuentasB.CodigosVerificacion.SingleAsync(c => c.Id == codigoId, Cancelacion);

        servicio.Verificar(registroA, Otro(codigo)).Should().Be(ResultadoVerificacion.Incorrecto);
        servicio.Verificar(registroB, Otro(codigo)).Should().Be(ResultadoVerificacion.Incorrecto);
        await cuentasA.SaveChangesAsync(Cancelacion);
        var guardarSegunda = () => cuentasB.SaveChangesAsync(Cancelacion);

        await guardarSegunda.Should().ThrowAsync<DbUpdateConcurrencyException>();
        await using var lectura = fabrica.CrearAmbito();
        (await lectura.ServiceProvider.GetRequiredService<CuentasDbContext>().CodigosVerificacion
            .SingleAsync(c => c.Id == codigoId, Cancelacion)).Intentos.Should().Be(1);
    }

    [Fact]
    public async Task El_correo_es_unico_en_la_base()
    {
        var correo = $"repetido-{Guid.NewGuid():N}@correo.test";
        await using var ambito = fabrica.CrearAmbito();
        var cuentas = ambito.ServiceProvider.GetRequiredService<CuentasDbContext>();
        cuentas.Users.AddRange(NuevoUsuario(correo), NuevoUsuario(correo));

        var guardar = () => cuentas.SaveChangesAsync(Cancelacion);

        (await guardar.Should().ThrowAsync<DbUpdateException>())
            .WithInnerException<PostgresException>().Which.SqlState.Should().Be(PostgresErrorCodes.UniqueViolation);
    }

    private static Usuario NuevoUsuario(string? correo = null)
    {
        var email = correo ?? $"persona-{Guid.NewGuid():N}@correo.test";
        return new Usuario(email, "Persona Ficticia", new DateOnly(1990, 1, 1))
        {
            NormalizedEmail = email.ToUpperInvariant(),
            NormalizedUserName = $"{email.ToUpperInvariant()}-{Guid.NewGuid():N}",
        };
    }

    private static string Otro(string codigo) => codigo == "000000" ? "111111" : "000000";
}
