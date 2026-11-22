using Microsoft.Extensions.DependencyInjection.Extensions;
using ServiYa.Api.Comandos;
using ServiYa.Api.Modulos;
using ServiYa.Api.Seguridad;
using ServiYa.Comun.Almacenamiento;
using ServiYa.Comun.Configuracion;
using ServiYa.Comun.Correo;
using ServiYa.Comun.Errores;
using ServiYa.Comun.Seguridad;
using ServiYa.Comun.Tareas;

var builder = WebApplication.CreateBuilder(args);

builder.AgregarRegistro();
builder.Services.AddProblemDetails(opciones => opciones.CustomizeProblemDetails = ProblemasEnEspanol.Traducir);
builder.Services.AddOpenApi();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AgregarOpcionesDeServiYa(builder.Configuration);
builder.Services.AgregarCorreo();
builder.Services.AgregarAlmacenamiento();
builder.Services.AgregarTareasProgramadas();
builder.Services.AgregarSeguridadHttp(builder.Configuration);
builder.Services.AgregarLimitesUso();
builder.Services.AgregarXsrf(builder.Configuration, builder.Environment);
builder.AgregarModulos();

// Hasta que Reportes registre las suspensiones con motivo (T135).
builder.Services.TryAddScoped<IConsultaSuspensiones, SinMotivosDeSuspension>();

var app = builder.Build();

if (ComandosConsola.EsComando(args))
{
    return await ComandosConsola.EjecutarAsync(app.Services, args, Console.Out, CancellationToken.None);
}

app.UseHttpLogging();

// Excepciones y respuestas vacías (404, 405...) salen como ProblemDetails, sin detalles internos.
app.UseExceptionHandler();
app.UseStatusCodePages();

app.UsarSeguridadHttp();

// La sesión primero: los límites por usuario y el token XSRF dependen de quién es.
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();
app.UsarValidacionXsrf();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
}

var api = app.MapearModulos();
api.MapearCorreosDeDesarrollo(app.Environment);

await app.RunAsync();
return 0;
