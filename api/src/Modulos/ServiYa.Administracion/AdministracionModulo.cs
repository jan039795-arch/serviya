using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ServiYa.Administracion.Datos;
using ServiYa.Administracion.Endpoints;
using ServiYa.Administracion.Servicios;
using ServiYa.Comun.Bitacora;
using ServiYa.Comun.Datos;
using ServiYa.Comun.Modulos;
using ServiYa.Comun.Seguridad;

namespace ServiYa.Administracion;

public sealed class AdministracionModulo : IModulo
{
    public void AgregarServicios(IServiceCollection services, IConfiguration configuracion, IHostEnvironment entorno)
    {
        services.AgregarDbContextDeModulo<AdministracionDbContext>(configuracion, AdministracionDbContext.Esquema);
        services.AddScoped<IBitacora, Bitacora>();
    }

    /// <summary>Todo <c>/admin</c> pide la política <see cref="Politicas.SoloAdmin"/> (rol vigente en la base).</summary>
    public void MapearEndpoints(IEndpointRouteBuilder api)
    {
        var admin = api.MapGroup("/admin").RequireAuthorization(Politicas.SoloAdmin).WithTags("Administración");
        admin.MapearRolesAdmin();
    }
}
