using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ServiYa.Comun.Modulos;

/// <summary>
/// Un módulo del monolito (plan.md). Registra sus servicios y su <c>DbContext</c>, y mapea sus
/// endpoints dentro del grupo <c>/api/v1</c>. Un módulo no consulta tablas de otro: usa sus
/// interfaces públicas.
/// </summary>
public interface IModulo
{
    void AgregarServicios(IServiceCollection services, IConfiguration configuracion, IHostEnvironment entorno);

    /// <param name="api">Grupo de rutas <c>/api/v1</c>.</param>
    void MapearEndpoints(IEndpointRouteBuilder api);
}
