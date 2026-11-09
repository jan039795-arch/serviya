using ServiYa.Api.Modulos;
using ServiYa.Api.Seguridad;
using ServiYa.Comun.Configuracion;
using ServiYa.Comun.Correo;
using ServiYa.Comun.Errores;

var builder = WebApplication.CreateBuilder(args);

builder.AgregarRegistro();
builder.Services.AddProblemDetails(opciones => opciones.CustomizeProblemDetails = ProblemasEnEspanol.Traducir);
builder.Services.AddOpenApi();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AgregarOpcionesDeServiYa(builder.Configuration);
builder.Services.AgregarCorreo();
builder.Services.AgregarSeguridadHttp(builder.Configuration);
builder.Services.AgregarLimitesUso();
builder.AgregarModulos();

var app = builder.Build();

app.UseHttpLogging();

// Excepciones y respuestas vacías (404, 405...) salen como ProblemDetails, sin detalles internos.
app.UseExceptionHandler();
app.UseStatusCodePages();

app.UsarSeguridadHttp();
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

var api = app.MapearModulos();
api.MapearCorreosDeDesarrollo(app.Environment);

await app.RunAsync();
