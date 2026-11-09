using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ServiYa.Comun.Datos;
using ServiYa.Comun.Modulos;
using ServiYa.Cuentas.Datos;
using ServiYa.Cuentas.Servicios;

namespace ServiYa.Cuentas;

public sealed class CuentasModulo : IModulo
{
    public void AgregarServicios(IServiceCollection services, IConfiguration configuracion, IHostEnvironment entorno)
    {
        // Esquema 2 de Identity: sin las tablas de passkeys de .NET 10, que el MVP no usa.
        services.Configure<IdentityOptions>(opciones => opciones.Stores.SchemaVersion = IdentitySchemaVersions.Version2);
        services.AgregarDbContextDeModulo<CuentasDbContext>(configuracion, CuentasDbContext.Esquema);
        services.AddSingleton<ServicioCodigos>();
    }

    public void MapearEndpoints(IEndpointRouteBuilder api)
    {
    }
}
