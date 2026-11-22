using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ServiYa.Comun.Datos;
using ServiYa.Comun.Modulos;
using ServiYa.Cuentas.Datos;
using ServiYa.Cuentas.Endpoints;
using ServiYa.Cuentas.Seguridad;
using ServiYa.Cuentas.Servicios;

namespace ServiYa.Cuentas;

public sealed class CuentasModulo : IModulo
{
    public void AgregarServicios(IServiceCollection services, IConfiguration configuracion, IHostEnvironment entorno)
    {
        services.AgregarDbContextDeModulo<CuentasDbContext>(configuracion, CuentasDbContext.Esquema);
        services.AgregarIdentityYSesion(configuracion, entorno);
        services.AddSingleton<ServicioCodigos>();
        services.AddScoped<EnvioCodigos>();
        services.AddScoped<SesionesCuentas>();
    }

    public void MapearEndpoints(IEndpointRouteBuilder api) => api.MapearCuentas();
}
