using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using ServiYa.Comun.Errores;

namespace ServiYa.Cuentas.Endpoints;

/// <summary>
/// Errores de Cuentas. Los del código y las credenciales son siempre el mismo mensaje, exista o no la
/// cuenta, para no revelar qué correos están registrados.
/// </summary>
internal static class ProblemasCuentas
{
    public static ProblemHttpResult CodigoInvalido() => TypedResults.Problem(
        statusCode: StatusCodes.Status400BadRequest,
        type: TiposProblema.CodigoInvalido,
        title: "El código no es correcto o ya venció.",
        detail: "El código no es correcto o ya venció. Revisa tu correo o pide uno nuevo.");

    public static ProblemHttpResult Credenciales() => TypedResults.Problem(
        statusCode: StatusCodes.Status400BadRequest,
        type: TiposProblema.CredencialesIncorrectas,
        title: "Correo o contraseña incorrectos.",
        detail: "Correo o contraseña incorrectos. Tras varios intentos fallidos la cuenta se pausa 15 minutos.");

    public static ProblemHttpResult CorreoSinVerificar() => TypedResults.Problem(
        statusCode: StatusCodes.Status403Forbidden,
        type: TiposProblema.CorreoSinVerificar,
        title: "Verifica tu correo.",
        detail: "Primero verifica tu correo con el código que te enviamos.");

    public static ProblemHttpResult CorreoRegistrado() => TypedResults.Problem(
        statusCode: StatusCodes.Status409Conflict,
        type: TiposProblema.CorreoRegistrado,
        title: "Ya existe una cuenta con ese correo.",
        detail: "Ya existe una cuenta con ese correo. Entra o recupera tu contraseña.");

    public static ValidationProblem Validacion(Dictionary<string, string[]> errores) =>
        TypedResults.ValidationProblem(errores, title: "Revisa los datos marcados.");
}
