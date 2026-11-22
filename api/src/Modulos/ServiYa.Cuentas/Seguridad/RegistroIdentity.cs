using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ServiYa.Comun.Seguridad;
using ServiYa.Cuentas.Datos;
using ServiYa.Cuentas.Dominio;

namespace ServiYa.Cuentas.Seguridad;

/// <summary>
/// Identity y la sesión por cookie (research R2, T059). La cookie <c>serviya.sesion</c> es HttpOnly,
/// <c>Secure</c> y <c>SameSite=Lax</c>, dura 14 días y se renueva con el uso. La API responde 401 y 403
/// sin redirigir a una página de login.
/// </summary>
internal static class RegistroIdentity
{
    public const string CookieSesion = "serviya.sesion";

    public static readonly TimeSpan DuracionSesion = TimeSpan.FromDays(14);

    /// <summary>
    /// Cada cuánto la cookie se compara con el sello de seguridad del usuario. Al cambiar la contraseña,
    /// los roles o el estado, las demás sesiones caen o se actualizan en este tiempo.
    /// </summary>
    public static readonly TimeSpan RevisionSello = TimeSpan.FromMinutes(5);

    /// <summary>Intentos de contraseña antes de pausar la cuenta. Más que el límite por IP para que un tercero no la bloquee fácil.</summary>
    public const int IntentosAntesDePausa = 10;

    public static readonly TimeSpan Pausa = TimeSpan.FromMinutes(15);

    public const int LargoMinimoPassword = 8;

    public static IServiceCollection AgregarIdentityYSesion(
        this IServiceCollection services,
        IConfiguration configuracion,
        IHostEnvironment entorno)
    {
        services.AddIdentityCore<Usuario>(opciones =>
            {
                opciones.Password.RequiredLength = LargoMinimoPassword;
                opciones.Password.RequireDigit = false;
                opciones.Password.RequireLowercase = false;
                opciones.Password.RequireUppercase = false;
                opciones.Password.RequireNonAlphanumeric = false;
                opciones.Password.RequiredUniqueChars = 1;

                // El correo es el nombre de usuario y se valida al registrarse.
                opciones.User.RequireUniqueEmail = true;
                opciones.User.AllowedUserNameCharacters = string.Empty;

                // Se revisa después de la contraseña, para no revelar a quien no la sabe que la cuenta existe.
                opciones.SignIn.RequireConfirmedEmail = false;

                opciones.Lockout.AllowedForNewUsers = true;
                opciones.Lockout.MaxFailedAccessAttempts = IntentosAntesDePausa;
                opciones.Lockout.DefaultLockoutTimeSpan = Pausa;

                // Esquema 2: sin las tablas de passkeys de .NET 10, que el MVP no usa.
                opciones.Stores.SchemaVersion = IdentitySchemaVersions.Version2;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<CuentasDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        services.AddOptions<SecurityStampValidatorOptions>().Configure<TimeProvider>((opciones, reloj) =>
        {
            opciones.ValidationInterval = RevisionSello;
            opciones.TimeProvider = reloj;
        });

        var dominio = Xsrf.DominioCookie(configuracion);

        // También las cookies de dos pasos y externas de Identity, que el MVP no usa pero que Identity
        // cierra cuando el sello de seguridad cambia.
        services.AddAuthentication(IdentityConstants.ApplicationScheme)
            .AddIdentityCookies(cookies => cookies.ApplicationCookie!.Configure(opciones =>
            {
                opciones.Cookie.Name = CookieSesion;
                opciones.Cookie.HttpOnly = true;
                opciones.Cookie.SecurePolicy = Xsrf.PoliticaSecure(entorno);
                opciones.Cookie.SameSite = SameSiteMode.Lax;
                opciones.Cookie.Domain = dominio;
                opciones.ExpireTimeSpan = DuracionSesion;
                opciones.SlidingExpiration = true;
                opciones.Events = new CookieAuthenticationEvents
                {
                    OnRedirectToLogin = Responder(StatusCodes.Status401Unauthorized),
                    OnRedirectToAccessDenied = Responder(StatusCodes.Status403Forbidden),
                    OnValidatePrincipal = SecurityStampValidator.ValidatePrincipalAsync,
                };
            }));
        services.AddOptions<CookieAuthenticationOptions>(IdentityConstants.ApplicationScheme)
            .Configure<TimeProvider>((opciones, reloj) => opciones.TimeProvider = reloj);

        services.AddScoped<IAuthorizationHandler, AdminVigenteHandler>();
        services.AddAuthorizationBuilder()
            .AddPolicy(Politicas.SoloAdmin, politica => politica.RequireRole(Roles.Admin).AddRequirements(new AdminVigente()));
        return services;
    }

    private static Func<RedirectContext<CookieAuthenticationOptions>, Task> Responder(int estado) => contexto =>
    {
        contexto.Response.StatusCode = estado;
        return Task.CompletedTask;
    };
}
