using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using ServiYa.Comun.Configuracion;

namespace ServiYa.Comun.Correo;

public static class RegistroCorreo
{
    public const string MensajeConsolaFueraDeDesarrollo =
        "Email:Proveedor 'Consola' solo se permite en desarrollo y pruebas.";

    public static IServiceCollection AgregarCorreo(this IServiceCollection services)
    {
        // Así un error de configuración no deja a producción "enviando" correos a la memoria.
        services.AddOptions<EmailOptions>().Validate<IHostEnvironment>(
            (opciones, entorno) => opciones.Proveedor != ProveedorCorreo.Consola || entorno.PermiteSimulaciones(),
            MensajeConsolaFueraDeDesarrollo);

        services.AddSingleton<ConsolaEmailSender>();
        services.AddHttpClient<ResendEmailSender>(http =>
        {
            http.BaseAddress = ResendEmailSender.DireccionBase;
            http.Timeout = ResendEmailSender.TiempoMaximo;
        });
        // Transitorio a propósito: ResendEmailSender es un cliente tipado y su HttpClient se renueva.
        // No inyectar IEmailSender en un singleton (fijaría un HttpMessageHandler para siempre).
        services.AddTransient<IEmailSender>(proveedor =>
            proveedor.GetRequiredService<IOptions<EmailOptions>>().Value.Proveedor == ProveedorCorreo.Resend
                ? proveedor.GetRequiredService<ResendEmailSender>()
                : proveedor.GetRequiredService<ConsolaEmailSender>());
        return services;
    }

    /// <summary><c>GET /dev/correos</c>: los últimos correos simulados. Solo existe en desarrollo.</summary>
    public static void MapearCorreosDeDesarrollo(this IEndpointRouteBuilder api, IHostEnvironment entorno)
    {
        ArgumentNullException.ThrowIfNull(entorno);
        if (!entorno.IsDevelopment())
        {
            return;
        }

        api.MapGet("/dev/correos", (ConsolaEmailSender correos) => Results.Ok(correos.Recientes))
            .ExcludeFromDescription();
    }
}
