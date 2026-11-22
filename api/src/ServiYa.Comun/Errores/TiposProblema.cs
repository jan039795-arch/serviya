namespace ServiYa.Comun.Errores;

/// <summary>
/// Valores de <c>type</c> de los <c>ProblemDetails</c> propios de ServiYa. La app los compara para
/// decidir qué mostrar (por ejemplo, llevar a verificar el correo).
/// </summary>
public static class TiposProblema
{
    public const string CodigoInvalido = "/errores/codigo-invalido";
    public const string CorreoRegistrado = "/errores/correo-registrado";
    public const string CorreoSinVerificar = "/errores/correo-sin-verificar";
    public const string CredencialesIncorrectas = "/errores/credenciales-incorrectas";
    public const string CuentaSuspendida = "/errores/cuenta-suspendida";
    public const string RolPropio = "/errores/rol-propio";
    public const string UltimoAdmin = "/errores/ultimo-admin";
    public const string XsrfInvalido = "/errores/xsrf-invalido";
}
