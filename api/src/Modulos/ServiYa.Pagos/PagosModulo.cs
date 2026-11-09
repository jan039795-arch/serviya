using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ServiYa.Comun.Modulos;

namespace ServiYa.Pagos;

public sealed class PagosModulo : IModulo
{
    public void AgregarServicios(IServiceCollection services, IConfiguration configuracion, IHostEnvironment entorno)
    {
    }

    public void MapearEndpoints(IEndpointRouteBuilder api)
    {
    }
}
