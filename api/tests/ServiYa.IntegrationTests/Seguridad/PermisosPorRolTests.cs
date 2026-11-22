using System.Net;
using ServiYa.Comun.Seguridad;
using ServiYa.IntegrationTests.Infraestructura;

namespace ServiYa.IntegrationTests.Seguridad;

/// <summary>
/// Matriz endpoint × rol (T058). Cada historia agrega sus endpoints: un endpoint nuevo sin su fila
/// aquí es un hueco en la revisión de permisos.
/// </summary>
public sealed class PermisosPorRolTests(ServiYaFactory fabrica) : IClassFixture<ServiYaFactory>
{
    /// <summary>Sin sesión.</summary>
    public const string Anonimo = nameof(Anonimo);

    private const HttpStatusCode Ok = HttpStatusCode.OK;
    private const HttpStatusCode SinContenido = HttpStatusCode.NoContent;
    private const HttpStatusCode SinSesion = HttpStatusCode.Unauthorized;

    private static CancellationToken Cancelacion => TestContext.Current.CancellationToken;

    public static TheoryData<string, string, string, HttpStatusCode> Matriz { get; } = Construir(
        ("GET", "cuentas/xsrf", [SinContenido, SinContenido, SinContenido, SinContenido]),
        ("GET", "cuentas/yo", [SinSesion, Ok, Ok, Ok]),
        ("POST", "cuentas/logout", [SinSesion, SinContenido, SinContenido, SinContenido]),
        ("POST", "cuentas/yo/roles/prestador", [SinSesion, SinContenido, SinContenido, SinContenido]));

    [Theory]
    [MemberData(nameof(Matriz))]
    public async Task Cada_rol_recibe_la_respuesta_esperada(string metodo, string ruta, string rol, HttpStatusCode esperado)
    {
        using var cliente = await ClienteDeAsync(rol);
        using var peticion = new HttpRequestMessage(new HttpMethod(metodo), new Uri($"/api/v1/{ruta}", UriKind.Relative));

        using var respuesta = await cliente.SendAsync(peticion, Cancelacion);

        respuesta.StatusCode.Should().Be(esperado, $"{metodo} {ruta} como {rol}");
    }

    /// <summary>Columnas: anónimo, Cliente, Prestador (también es cliente) y Admin.</summary>
    private static TheoryData<string, string, string, HttpStatusCode> Construir(
        params (string Metodo, string Ruta, HttpStatusCode[] Esperados)[] filas)
    {
        string[] columnas = [Anonimo, Roles.Cliente, Roles.Prestador, Roles.Admin];
        var datos = new TheoryData<string, string, string, HttpStatusCode>();
        foreach (var (metodo, ruta, esperados) in filas)
        {
            for (var i = 0; i < columnas.Length; i++)
            {
                datos.Add(metodo, ruta, columnas[i], esperados[i]);
            }
        }

        return datos;
    }

    private async Task<HttpClient> ClienteDeAsync(string rol)
    {
        if (rol == Anonimo)
        {
            var anonimo = fabrica.CrearCliente();
            await anonimo.PrepararXsrfAsync();
            return anonimo;
        }

        string[] roles = rol == Roles.Prestador ? [Roles.Cliente, Roles.Prestador] : [rol];
        var usuario = await fabrica.CrearUsuarioAsync(roles);
        return await fabrica.ClienteConSesionAsync(usuario);
    }
}
