using ServiYa.Administracion;
using ServiYa.Busqueda;
using ServiYa.Calificaciones;
using ServiYa.Chat;
using ServiYa.Comun.Modulos;
using ServiYa.Contrataciones;
using ServiYa.Cuentas;
using ServiYa.Documentos;
using ServiYa.Pagos;
using ServiYa.Prestadores;
using ServiYa.Reportes;

namespace ServiYa.Api.Modulos;

/// <summary>Lista explícita de módulos: sin reflexión, el orden es visible y fácil de seguir.</summary>
internal static class RegistroModulos
{
    public const string PrefijoApi = "/api/v1";

    private static readonly IModulo[] Modulos =
    [
        new CuentasModulo(),
        new PrestadoresModulo(),
        new DocumentosModulo(),
        new BusquedaModulo(),
        new PagosModulo(),
        new ChatModulo(),
        new ContratacionesModulo(),
        new CalificacionesModulo(),
        new ReportesModulo(),
        new AdministracionModulo(),
    ];

    public static WebApplicationBuilder AgregarModulos(this WebApplicationBuilder builder)
    {
        foreach (var modulo in Modulos)
        {
            modulo.AgregarServicios(builder.Services, builder.Configuration, builder.Environment);
        }

        return builder;
    }

    public static RouteGroupBuilder MapearModulos(this IEndpointRouteBuilder rutas)
    {
        var api = rutas.MapGroup(PrefijoApi);
        foreach (var modulo in Modulos)
        {
            modulo.MapearEndpoints(api);
        }

        return api;
    }
}
