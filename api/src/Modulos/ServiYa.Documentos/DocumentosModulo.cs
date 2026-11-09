using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ServiYa.Comun.Modulos;

namespace ServiYa.Documentos;

public sealed class DocumentosModulo : IModulo
{
    public void AgregarServicios(IServiceCollection services, IConfiguration configuracion, IHostEnvironment entorno)
    {
    }

    public void MapearEndpoints(IEndpointRouteBuilder api)
    {
    }
}
