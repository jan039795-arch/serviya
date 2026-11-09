using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using ServiYa.Cuentas.Dominio;

namespace ServiYa.Cuentas.Servicios;

public enum ResultadoVerificacion
{
    Correcto,
    Incorrecto,
    Vencido,

    /// <summary>Se agotaron los intentos: hay que pedir un código nuevo.</summary>
    Agotado,
    YaUsado,
}

/// <summary>Código creado: el registro se guarda y el código en claro solo viaja en el correo.</summary>
public sealed record CodigoCreado(CodigoVerificacion Registro, string Codigo);

/// <summary>
/// Códigos de 6 dígitos con <see cref="RandomNumberGenerator"/>, vigentes 10 minutos, con 5 intentos
/// y hasta 3 envíos por hora (research R2, T060). Quien lo usa guarda el registro y cuenta los envíos.
/// </summary>
/// <remarks>
/// El hash es SHA-256 de usuario, propósito y código. Un millón de combinaciones se pueden probar si
/// alguien lee la base, pero el código vence en 10 minutos y solo sirve para su usuario y propósito.
/// </remarks>
public sealed class ServicioCodigos(TimeProvider reloj)
{
    public const int Digitos = 6;
    public const int IntentosMaximos = 5;
    public const int EnviosPorHora = 3;
    public static readonly TimeSpan Vigencia = TimeSpan.FromMinutes(10);
    public static readonly TimeSpan VentanaEnvios = TimeSpan.FromHours(1);

    private const int Combinaciones = 1_000_000;

    public CodigoCreado Crear(Guid usuarioId, PropositoCodigo proposito)
    {
        var codigo = RandomNumberGenerator.GetInt32(Combinaciones).ToString("D6", CultureInfo.InvariantCulture);
        var registro = new CodigoVerificacion(usuarioId, proposito, Hash(usuarioId, proposito, codigo), reloj.GetUtcNow() + Vigencia);
        return new CodigoCreado(registro, codigo);
    }

    /// <summary>Revisa el código y actualiza intentos o uso en <paramref name="registro"/>.</summary>
    public ResultadoVerificacion Verificar(CodigoVerificacion registro, string codigoIngresado)
    {
        ArgumentNullException.ThrowIfNull(registro);
        if (registro.UsadoEn is not null)
        {
            return ResultadoVerificacion.YaUsado;
        }

        var ahora = reloj.GetUtcNow();
        if (ahora >= registro.ExpiraEn)
        {
            return ResultadoVerificacion.Vencido;
        }

        if (registro.Intentos >= IntentosMaximos)
        {
            return ResultadoVerificacion.Agotado;
        }

        var codigo = Normalizar(codigoIngresado);
        if (codigo is not null && Coincide(registro, codigo))
        {
            registro.MarcarUsado(ahora);
            return ResultadoVerificacion.Correcto;
        }

        registro.RegistrarIntentoFallido();
        return registro.Intentos >= IntentosMaximos ? ResultadoVerificacion.Agotado : ResultadoVerificacion.Incorrecto;
    }

    public static bool PuedeEnviar(int enviadosEnLaUltimaHora) => enviadosEnLaUltimaHora < EnviosPorHora;

    /// <summary>Desde cuándo contar los envíos para <see cref="PuedeEnviar"/>.</summary>
    public DateTimeOffset InicioVentanaEnvios() => reloj.GetUtcNow() - VentanaEnvios;

    internal static string Hash(Guid usuarioId, PropositoCodigo proposito, string codigo)
    {
        var datos = Encoding.UTF8.GetBytes($"{usuarioId:N}:{proposito}:{codigo}");
        return Convert.ToHexStringLower(SHA256.HashData(datos));
    }

    private static bool Coincide(CodigoVerificacion registro, string codigo)
    {
        var esperado = Encoding.ASCII.GetBytes(registro.CodigoHash);
        var calculado = Encoding.ASCII.GetBytes(Hash(registro.UsuarioId, registro.Proposito, codigo));
        return CryptographicOperations.FixedTimeEquals(esperado, calculado);
    }

    /// <summary>Quita espacios (se pega "123 456" desde el correo); devuelve null si no son 6 dígitos.</summary>
    private static string? Normalizar(string? ingresado)
    {
        if (ingresado is null)
        {
            return null;
        }

        var sinEspacios = string.Concat(ingresado.Where(c => !char.IsWhiteSpace(c)));
        return sinEspacios.Length == Digitos && sinEspacios.All(char.IsAsciiDigit) ? sinEspacios : null;
    }
}
