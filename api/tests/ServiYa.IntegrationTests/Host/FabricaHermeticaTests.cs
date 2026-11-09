using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ServiYa.Comun.Tareas;
using ServiYa.IntegrationTests.Infraestructura;

namespace ServiYa.IntegrationTests.Host;

/// <summary>Las pruebas del host no dependen de la máquina (user-secrets) ni de la base de datos.</summary>
public sealed class FabricaHermeticaTests
{
    [Theory]
    [InlineData("Development")]
    [InlineData("Testing")]
    public async Task La_configuracion_de_prueba_gana_a_los_user_secrets_del_desarrollador(string entorno)
    {
        await using var fabrica = FabricaSinBaseDeDatos.Crear(entorno);
        using var cliente = fabrica.CreateClient();

        var configuracion = fabrica.Services.GetRequiredService<IConfiguration>();

        configuracion["Storage:AccessKey"].Should().Be("clave-ficticia");
        configuracion["ConnectionStrings:Default"].Should().Contain("Database=ficticia");
        // Solo se compara si existe, para no imprimir un secreto si la prueba falla.
        configuracion.GetSection("ConnectionStrings:Migraciones").Exists().Should().BeFalse();
    }

    [Fact]
    public async Task El_planificador_no_toca_la_base_de_datos()
    {
        await using var fabrica = FabricaSinBaseDeDatos.Crear("Testing");
        using var cliente = fabrica.CreateClient();

        fabrica.Services.GetRequiredService<ICandadoTareas>().Should().NotBeOfType<CandadoPostgres>();
    }
}
