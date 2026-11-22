using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ServiYa.Busqueda.Datos;
using ServiYa.Busqueda.Endpoints;
using ServiYa.Busqueda.Servicios;
using ServiYa.Comun.Datos;
using ServiYa.Comun.Modulos;

namespace ServiYa.Busqueda;

public sealed class BusquedaModulo : IModulo
{
    public void AgregarServicios(IServiceCollection services, IConfiguration configuracion, IHostEnvironment entorno)
    {
        services.AgregarDbContextDeModulo<CatalogosDbContext>(configuracion, CatalogosDbContext.Esquema, usaGeografia: true);
        services.AddScoped<ICatalogosService, CatalogosService>();
    }

    public void MapearEndpoints(IEndpointRouteBuilder api) => api.MapearCatalogos();
}
