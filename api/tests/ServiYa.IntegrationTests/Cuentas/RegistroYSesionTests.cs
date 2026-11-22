using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ServiYa.Comun;
using ServiYa.Comun.Errores;
using ServiYa.Comun.Seguridad;
using ServiYa.Cuentas.Datos;
using ServiYa.Cuentas.Dominio;
using ServiYa.IntegrationTests.Infraestructura;

namespace ServiYa.IntegrationTests.Cuentas;

/// <summary>Registro, verificación, sesión y recuperación por HTTP (T058, TDD; FR-001, FR-004, FR-044).</summary>
public sealed class RegistroYSesionTests(ServiYaFactory fabrica) : IClassFixture<ServiYaFactory>
{
    private const string MensajeCodigo = "El código no es correcto o ya venció.";

    private static CancellationToken Cancelacion => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Registro_envia_un_codigo_y_al_verificarlo_inicia_la_sesion_con_cookie_segura()
    {
        using var cliente = await ClienteAnonimoAsync();
        var email = CorreoNuevo();

        using var registro = await PostAsync(cliente, "cuentas/registro", Registro(email));
        registro.StatusCode.Should().Be(HttpStatusCode.Created);
        var codigo = fabrica.UltimoCodigo(email);
        codigo.Should().MatchRegex(@"^\d{6}$");

        using var verificacion = await PostAsync(cliente, "cuentas/verificar-correo", new { email, codigo });

        verificacion.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var sesion = CookiesNuevas(verificacion).Single(c => c.StartsWith("serviya.sesion=", StringComparison.Ordinal));
        sesion.Should().ContainEquivalentOf("httponly").And.ContainEquivalentOf("secure").And.ContainEquivalentOf("samesite=lax");
        var yo = await LeerSesionAsync(cliente);
        yo.Email.Should().Be(email);
        yo.NombreCompleto.Should().Be("Persona Ficticia");
        yo.Roles.Should().Equal(Roles.Cliente);
        yo.Estado.Should().Be("Activo");
        yo.EsMayorDeEdad.Should().BeTrue();
    }

    [Fact]
    public async Task Quien_se_registra_como_prestador_tambien_es_cliente()
    {
        using var cliente = await ClienteAnonimoAsync();
        var email = CorreoNuevo();
        (await PostAsync(cliente, "cuentas/registro", Registro(email, rol: Roles.Prestador))).Dispose();

        (await PostAsync(cliente, "cuentas/verificar-correo", new { email, codigo = fabrica.UltimoCodigo(email) })).Dispose();

        (await LeerSesionAsync(cliente)).Roles.Should().BeEquivalentTo([Roles.Cliente, Roles.Prestador]);
    }

    [Fact]
    public async Task El_registro_guarda_los_consentimientos_con_su_version_y_la_ip()
    {
        using var cliente = await ClienteAnonimoAsync(ip: "198.18.40.41");
        var email = CorreoNuevo();

        (await PostAsync(cliente, "cuentas/registro", Registro(email))).StatusCode.Should().Be(HttpStatusCode.Created);

        await using var ambito = fabrica.CrearAmbito();
        var cuentas = ambito.ServiceProvider.GetRequiredService<CuentasDbContext>();
        var normalizado = email.ToUpperInvariant();
        var usuario = await cuentas.Users.SingleAsync(u => u.NormalizedEmail == normalizado, Cancelacion);
        var consentimientos = await cuentas.Consentimientos.Where(c => c.UsuarioId == usuario.Id).ToListAsync(Cancelacion);
        consentimientos.Select(c => (c.Documento, c.Version)).Should().BeEquivalentTo(
            [(DocumentoLegal.AvisoPrivacidad, "2026-11"), (DocumentoLegal.Terminos, "2026-11")]);
        consentimientos.Should().AllSatisfy(c =>
        {
            c.Ip.Should().Be(IPAddress.Parse("198.18.40.41"));
            c.AceptadoEn.Should().Be(fabrica.Reloj.GetUtcNow());
        });
        usuario.EmailConfirmed.Should().BeFalse();
    }

    [Fact]
    public async Task No_se_puede_registrar_dos_veces_el_mismo_correo()
    {
        using var cliente = await ClienteAnonimoAsync();
        var email = CorreoNuevo();
        (await PostAsync(cliente, "cuentas/registro", Registro(email))).Dispose();

        using var repetido = await PostAsync(cliente, "cuentas/registro", Registro(email.ToUpperInvariant()));

        repetido.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Dos_registros_simultaneos_con_el_mismo_correo_crean_una_sola_cuenta()
    {
        var email = CorreoNuevo();

        var estados = await Task.WhenAll(
            PostAnonimoAsync("cuentas/registro", Registro(email)),
            PostAnonimoAsync("cuentas/registro", Registro(email)));

        estados.Should().BeEquivalentTo([HttpStatusCode.Created, HttpStatusCode.Conflict]);
    }

    [Fact]
    public async Task El_registro_con_datos_invalidos_responde_los_errores_por_campo()
    {
        using var cliente = await ClienteAnonimoAsync();

        using var respuesta = await PostAsync(cliente, "cuentas/registro", new
        {
            email = "no-es-correo",
            password = "corta",
            nombreCompleto = "Yo",
            fechaNacimiento = "31/12/1990",
            rol = "Admin",
            aceptaciones = new[] { new { documento = "Terminos", version = "2026-11" } },
        });

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problema = await respuesta.Content.ReadFromJsonAsync<ValidationProblemDetails>(Cancelacion);
        problema!.Errors.Keys.Should().BeEquivalentTo(["email", "password", "nombreCompleto", "fechaNacimiento", "rol", "aceptaciones"]);
    }

    [Theory]
    [InlineData("2100-01-01")]
    [InlineData("1899-12-31")]
    public async Task El_registro_rechaza_fechas_de_nacimiento_imposibles(string fecha)
    {
        using var cliente = await ClienteAnonimoAsync();

        using var respuesta = await PostAsync(cliente, "cuentas/registro", Registro(CorreoNuevo(), fechaNacimiento: fecha));

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await respuesta.Content.ReadFromJsonAsync<ValidationProblemDetails>(Cancelacion))!.Errors.Should().ContainKey("fechaNacimiento");
    }

    [Fact]
    public async Task Un_menor_de_18_se_puede_registrar_pero_no_es_mayor_de_edad()
    {
        var hoy = fabrica.Reloj.HoyEnMexico();
        using var menor = await RegistrarYVerificarAsync(hoy.AddYears(-Usuario.EdadMinima).AddDays(1));
        using var cumpleHoy = await RegistrarYVerificarAsync(hoy.AddYears(-Usuario.EdadMinima));

        (await LeerSesionAsync(menor)).EsMayorDeEdad.Should().BeFalse();
        (await LeerSesionAsync(cumpleHoy)).EsMayorDeEdad.Should().BeTrue();
    }

    [Fact]
    public async Task Sin_verificar_el_correo_no_se_puede_entrar()
    {
        using var cliente = await ClienteAnonimoAsync();
        var email = CorreoNuevo();
        (await PostAsync(cliente, "cuentas/registro", Registro(email))).Dispose();

        using var login = await PostAsync(cliente, "cuentas/login", new { email, password = ClientesDePrueba.PasswordDePrueba });

        login.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await LeerProblemaAsync(login)).Type.Should().Be(TiposProblema.CorreoSinVerificar);
        CookiesNuevas(login).Should().NotContain(c => c.StartsWith("serviya.sesion=", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Sin_verificar_y_con_la_contrasena_incorrecta_no_se_revela_que_falta_verificar()
    {
        using var cliente = await ClienteAnonimoAsync();
        var email = CorreoNuevo();
        (await PostAsync(cliente, "cuentas/registro", Registro(email))).Dispose();

        using var login = await PostAsync(cliente, "cuentas/login", new { email, password = "otra-contrasena" });

        login.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await LeerProblemaAsync(login)).Type.Should().NotBe(TiposProblema.CorreoSinVerificar);
    }

    [Fact]
    public async Task Login_correcto_devuelve_la_sesion_y_el_incorrecto_no_dice_si_el_correo_existe()
    {
        var usuario = await fabrica.CrearUsuarioAsync(Roles.Cliente);
        using var cliente = await ClienteAnonimoAsync();

        using var malaContrasena = await PostAsync(cliente, "cuentas/login", new { email = usuario.Email, password = "otra-contrasena" });
        using var noExiste = await PostAsync(cliente, "cuentas/login", new { email = CorreoNuevo(), password = "otra-contrasena" });
        using var correcto = await PostAsync(cliente, "cuentas/login", new { email = usuario.Email, password = usuario.Password });

        malaContrasena.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        noExiste.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await LeerProblemaAsync(noExiste)).Detail.Should().Be((await LeerProblemaAsync(malaContrasena)).Detail);
        correcto.StatusCode.Should().Be(HttpStatusCode.OK);
        var sesion = await correcto.Content.ReadFromJsonAsync<SesionDto>(Cancelacion);
        sesion!.Id.Should().Be(usuario.Id);
        sesion.Roles.Should().Equal(Roles.Cliente);
    }

    [Fact]
    public async Task El_codigo_vence_a_los_10_minutos()
    {
        using var cliente = await ClienteAnonimoAsync();
        var email = CorreoNuevo();
        (await PostAsync(cliente, "cuentas/registro", Registro(email))).Dispose();
        var codigo = fabrica.UltimoCodigo(email);

        fabrica.Reloj.Advance(TimeSpan.FromMinutes(10));
        using var verificacion = await PostAsync(cliente, "cuentas/verificar-correo", new { email, codigo });

        verificacion.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await LeerProblemaAsync(verificacion)).Title.Should().Be(MensajeCodigo);
    }

    [Fact]
    public async Task Tras_5_intentos_fallidos_el_codigo_ya_no_sirve_aunque_sea_el_correcto()
    {
        var email = CorreoNuevo();
        using (var registro = await ClienteAnonimoAsync())
        {
            (await PostAsync(registro, "cuentas/registro", Registro(email))).Dispose();
        }

        var codigo = fabrica.UltimoCodigo(email)!;
        var incorrecto = codigo == "000000" ? "111111" : "000000";

        // Cada intento desde otra IP: el bloqueo es del código, no del límite por IP.
        for (var intento = 0; intento < 5; intento++)
        {
            (await PostAnonimoAsync("cuentas/verificar-correo", new { email, codigo = incorrecto })).Should().Be(HttpStatusCode.BadRequest);
        }

        using var cliente = await ClienteAnonimoAsync();
        using var correcto = await PostAsync(cliente, "cuentas/verificar-correo", new { email, codigo });

        correcto.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Un_codigo_no_sirve_dos_veces_y_reenviar_invalida_el_anterior()
    {
        var email = CorreoNuevo();
        using var cliente = await ClienteAnonimoAsync();
        (await PostAsync(cliente, "cuentas/registro", Registro(email))).Dispose();
        var primero = fabrica.UltimoCodigo(email);
        (await PostAsync(cliente, "cuentas/reenviar-codigo", new { email })).StatusCode.Should().Be(HttpStatusCode.NoContent);
        var segundo = fabrica.UltimoCodigo(email);

        using var conElPrimero = await PostAsync(cliente, "cuentas/verificar-correo", new { email, codigo = primero });
        using var conElSegundo = await PostAsync(cliente, "cuentas/verificar-correo", new { email, codigo = segundo });
        var otraVez = await PostAnonimoAsync("cuentas/verificar-correo", new { email, codigo = segundo });

        if (primero != segundo)
        {
            conElPrimero.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        conElSegundo.StatusCode.Should().Be(HttpStatusCode.NoContent);
        otraVez.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Se_mandan_como_maximo_3_codigos_por_hora_y_reenviar_siempre_responde_204()
    {
        var email = CorreoNuevo();
        (await PostAnonimoAsync("cuentas/registro", Registro(email))).Should().Be(HttpStatusCode.Created);

        for (var reenvio = 0; reenvio < 3; reenvio++)
        {
            (await PostAnonimoAsync("cuentas/reenviar-codigo", new { email })).Should().Be(HttpStatusCode.NoContent);
        }

        fabrica.CorreosPara(email).Should().Be(3, "el registro y dos reenvíos; el tercero ya no se manda");

        fabrica.Reloj.Advance(TimeSpan.FromHours(1));
        (await PostAnonimoAsync("cuentas/reenviar-codigo", new { email })).Should().Be(HttpStatusCode.NoContent);
        fabrica.CorreosPara(email).Should().Be(4);
    }

    [Fact]
    public async Task Una_cuenta_ya_verificada_no_recibe_otro_codigo_de_verificacion_ni_lo_puede_usar()
    {
        var usuario = await fabrica.CrearUsuarioAsync(Roles.Cliente);

        (await PostAnonimoAsync("cuentas/reenviar-codigo", new { email = usuario.Email })).Should().Be(HttpStatusCode.NoContent);
        var verificar = await PostAnonimoAsync("cuentas/verificar-correo", new { email = usuario.Email, codigo = "123456" });

        fabrica.CorreosPara(usuario.Email).Should().Be(0);
        verificar.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Reenviar_o_recuperar_con_un_correo_que_no_existe_responde_204_sin_mandar_nada()
    {
        var email = CorreoNuevo();
        using var cliente = await ClienteAnonimoAsync();

        using var reenvio = await PostAsync(cliente, "cuentas/reenviar-codigo", new { email });
        using var recuperar = await PostAsync(cliente, "cuentas/recuperar", new { email });

        reenvio.StatusCode.Should().Be(HttpStatusCode.NoContent);
        recuperar.StatusCode.Should().Be(HttpStatusCode.NoContent);
        fabrica.CorreosPara(email).Should().Be(0);
    }

    [Fact]
    public async Task Recuperar_la_contrasena_cambia_la_contrasena_y_cierra_las_demas_sesiones()
    {
        var usuario = await fabrica.CrearUsuarioAsync(Roles.Cliente);
        using var sesionAnterior = await fabrica.ClienteConSesionAsync(usuario);
        using var cliente = await ClienteAnonimoAsync();
        (await PostAsync(cliente, "cuentas/recuperar", new { email = usuario.Email })).StatusCode.Should().Be(HttpStatusCode.NoContent);
        var codigo = fabrica.UltimoCodigo(usuario.Email);

        using var corta = await PostAsync(cliente, "cuentas/restablecer", new { email = usuario.Email, codigo, nuevaPassword = "corta" });
        using var restablecer = await PostAsync(cliente, "cuentas/restablecer", new { email = usuario.Email, codigo, nuevaPassword = "nueva-contrasena-123" });

        corta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await corta.Content.ReadFromJsonAsync<ValidationProblemDetails>(Cancelacion))!.Errors.Should().ContainKey("nuevaPassword");
        restablecer.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await PostAsync(cliente, "cuentas/login", new { email = usuario.Email, password = usuario.Password })).StatusCode
            .Should().Be(HttpStatusCode.BadRequest);
        (await PostAsync(cliente, "cuentas/login", new { email = usuario.Email, password = "nueva-contrasena-123" })).StatusCode
            .Should().Be(HttpStatusCode.OK);

        // La cookie anterior deja de servir cuando se revisa su sello de seguridad.
        fabrica.Reloj.Advance(TimeSpan.FromMinutes(6));
        (await GetAsync(sesionAnterior, "cuentas/yo")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Cerrar_sesion_borra_la_cookie()
    {
        var usuario = await fabrica.CrearUsuarioAsync(Roles.Cliente);
        using var cliente = await fabrica.ClienteConSesionAsync(usuario);

        using var logout = await PostAsync(cliente, "cuentas/logout", null);

        logout.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await GetAsync(cliente, "cuentas/yo")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task La_sesion_dura_14_dias_sin_usarse()
    {
        var usuario = await fabrica.CrearUsuarioAsync(Roles.Cliente);
        using var cliente = await fabrica.ClienteConSesionAsync(usuario);

        fabrica.Reloj.Advance(TimeSpan.FromDays(13));
        (await GetAsync(cliente, "cuentas/yo")).StatusCode.Should().Be(HttpStatusCode.OK);
        fabrica.Reloj.Advance(TimeSpan.FromDays(14) + TimeSpan.FromMinutes(1));

        (await GetAsync(cliente, "cuentas/yo")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Un_cliente_puede_hacerse_prestador_con_la_misma_cuenta()
    {
        var usuario = await fabrica.CrearUsuarioAsync(Roles.Cliente);
        using var cliente = await fabrica.ClienteConSesionAsync(usuario);

        using var respuesta = await PostAsync(cliente, "cuentas/yo/roles/prestador", null);
        using var otraVez = await PostAsync(cliente, "cuentas/yo/roles/prestador", null);

        respuesta.StatusCode.Should().Be(HttpStatusCode.NoContent);
        otraVez.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await LeerSesionAsync(cliente)).Roles.Should().BeEquivalentTo([Roles.Cliente, Roles.Prestador]);
    }

    [Fact]
    public async Task Una_peticion_que_modifica_datos_sin_X_XSRF_TOKEN_se_rechaza()
    {
        var usuario = await fabrica.CrearUsuarioAsync(Roles.Cliente);
        using var sinEncabezado = fabrica.CrearCliente(enviarXsrf: false);
        await sinEncabezado.PrepararXsrfAsync();

        using var login = await PostAsync(sinEncabezado, "cuentas/login", new { email = usuario.Email, password = usuario.Password });

        login.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await LeerProblemaAsync(login)).Type.Should().Be(TiposProblema.XsrfInvalido);
    }

    [Fact]
    public async Task El_token_XSRF_de_otra_sesion_no_sirve()
    {
        var usuario = await fabrica.CrearUsuarioAsync(Roles.Cliente);
        using var victima = await fabrica.ClienteConSesionAsync(usuario);
        using var atacante = await ClienteAnonimoAsync();
        using var xsrf = await GetAsync(atacante, "cuentas/xsrf");
        var tokenAjeno = CookiesNuevas(xsrf).Single(c => c.StartsWith("XSRF-TOKEN=", StringComparison.Ordinal))
            .Split(';')[0]["XSRF-TOKEN=".Length..];

        using var peticion = new HttpRequestMessage(HttpMethod.Post, Ruta("cuentas/yo/roles/prestador"));
        peticion.Headers.Add("X-XSRF-TOKEN", Uri.UnescapeDataString(tokenAjeno));
        using var respuesta = await victima.SendAsync(peticion, Cancelacion);

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_y_codigos_permiten_5_peticiones_por_minuto_por_ip()
    {
        using var cliente = await ClienteAnonimoAsync();
        var estados = new List<HttpStatusCode>();
        for (var intento = 0; intento < 6; intento++)
        {
            using var respuesta = await PostAsync(cliente, "cuentas/login", new { email = CorreoNuevo(), password = "otra-contrasena" });
            estados.Add(respuesta.StatusCode);
        }

        estados.Take(5).Should().AllBeEquivalentTo(HttpStatusCode.BadRequest);
        estados[^1].Should().Be(HttpStatusCode.TooManyRequests);
    }

    private async Task<HttpClient> ClienteAnonimoAsync(string? ip = null)
    {
        var cliente = fabrica.CrearCliente(ip);
        await cliente.PrepararXsrfAsync();
        return cliente;
    }

    private async Task<HttpClient> RegistrarYVerificarAsync(DateOnly fechaNacimiento)
    {
        var cliente = await ClienteAnonimoAsync();
        var email = CorreoNuevo();
        var fecha = fechaNacimiento.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        (await PostAsync(cliente, "cuentas/registro", Registro(email, fechaNacimiento: fecha))).StatusCode.Should().Be(HttpStatusCode.Created);
        (await PostAsync(cliente, "cuentas/verificar-correo", new { email, codigo = fabrica.UltimoCodigo(email) })).StatusCode
            .Should().Be(HttpStatusCode.NoContent);
        return cliente;
    }

    /// <summary>Una petición desde un cliente nuevo, con su propia IP; devuelve solo el estado.</summary>
    private async Task<HttpStatusCode> PostAnonimoAsync(string ruta, object cuerpo)
    {
        using var cliente = await ClienteAnonimoAsync();
        using var respuesta = await PostAsync(cliente, ruta, cuerpo);
        return respuesta.StatusCode;
    }

    private static IEnumerable<string> CookiesNuevas(HttpResponseMessage respuesta) =>
        respuesta.Headers.TryGetValues("Set-Cookie", out var cookies) ? cookies : [];

    private static object Registro(string email, string rol = Roles.Cliente, string fechaNacimiento = "1990-05-20") => new
    {
        email,
        password = ClientesDePrueba.PasswordDePrueba,
        nombreCompleto = "Persona Ficticia",
        fechaNacimiento,
        rol,
        aceptaciones = new[]
        {
            new { documento = "AvisoPrivacidad", version = "2026-11" },
            new { documento = "Terminos", version = "2026-11" },
        },
    };

    private static string CorreoNuevo() => $"persona-{Guid.NewGuid():N}@correo.test";

    private static Uri Ruta(string ruta) => new($"/api/v1/{ruta}", UriKind.Relative);

    private static Task<HttpResponseMessage> PostAsync(HttpClient cliente, string ruta, object? cuerpo) =>
        cuerpo is null
            ? cliente.PostAsync(Ruta(ruta), null, Cancelacion)
            : cliente.PostAsJsonAsync(Ruta(ruta), cuerpo, Cancelacion);

    private static Task<HttpResponseMessage> GetAsync(HttpClient cliente, string ruta) => cliente.GetAsync(Ruta(ruta), Cancelacion);

    private static async Task<SesionDto> LeerSesionAsync(HttpClient cliente)
    {
        using var respuesta = await GetAsync(cliente, "cuentas/yo");
        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await respuesta.Content.ReadFromJsonAsync<SesionDto>(Cancelacion))!;
    }

    private static async Task<ProblemDetails> LeerProblemaAsync(HttpResponseMessage respuesta)
    {
        respuesta.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        return (await respuesta.Content.ReadFromJsonAsync<ProblemDetails>(Cancelacion))!;
    }

    private sealed record SesionDto(Guid Id, string NombreCompleto, string Email, string[] Roles, string Estado, bool EsMayorDeEdad);
}
