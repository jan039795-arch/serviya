using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ServiYa.Administracion.Datos;
using ServiYa.Administracion.Servicios;
using ServiYa.Comun.Bitacora;
using ServiYa.Comun.Datos;
using ServiYa.Comun.Modulos;

namespace ServiYa.Administracion;

public sealed class AdministracionModulo : IModulo
{
    public void AgregarServicios(IServiceCollection services, IConfiguration configuracion, IHostEnvironment entorno)
    {
        services.AgregarDbContextDeModulo<AdministracionDbContext>(configuracion, AdministracionDbContext.Esquema);
        services.AddScoped<IBitacora, Bitacora>();
    }

    public void MapearEndpoints(IEndpointRouteBuilder api)
    {
    }
}
