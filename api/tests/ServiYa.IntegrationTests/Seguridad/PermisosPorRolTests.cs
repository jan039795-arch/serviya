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
    private const HttpStatusCode Prohibido = HttpStatusCode.Forbidden;
    private const HttpStatusCode NoEncontrado = HttpStatusCode.NotFound;

    /// <summary>Público, pero la matriz no manda cuerpo: llega hasta la validación.</summary>
    private const HttpStatusCode SinCuerpo = HttpStatusCode.BadRequest;

    /// <summary>Una cuenta que no existe: el administrador llega hasta el 404; los demás se quedan antes.</summary>
    private const string RolAdminDeNadie = "admin/usuarios/0193e0a0-0000-7000-8000-0000000000ff/roles/admin";

    private static CancellationToken Cancelacion => TestContext.Current.CancellationToken;

    public static TheoryData<string, string, string, HttpStatusCode> Matriz { get; } = Construir(
        ("GET", "cuentas/xsrf", [SinContenido, SinContenido, SinContenido, SinContenido]),
        ("GET", "cuentas/yo", [SinSesion, Ok, Ok, Ok]),
        ("POST", "cuentas/logout", [SinSesion, SinContenido, SinContenido, SinContenido]),
        ("POST", "cuentas/yo/roles/prestador", [SinSesion, SinContenido, SinContenido, SinContenido]),
        ("GET", "catalogos/oficios", [Ok, Ok, Ok, Ok]),
        ("GET", "catalogos/municipios", [Ok, Ok, Ok, Ok]),
        ("POST", "catalogos/municipios/ubicar", [SinCuerpo, SinCuerpo, SinCuerpo, SinCuerpo]),
        ("POST", RolAdminDeNadie, [SinSesion, Prohibido, Prohibido, NoEncontrado]),
        ("DELETE", RolAdminDeNadie, [SinSesion, Prohibido, Prohibido, NoEncontrado]));

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
