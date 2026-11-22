namespace ServiYa.Comun.Seguridad;

/// <summary>Nombres de las políticas de límite de uso; la API las define y los módulos las piden.</summary>
public static class PoliticasLimite
{
    /// <summary>Login, registro, verificación, reenvío y recuperación: 5 por minuto por IP.</summary>
    public const string LoginCodigos = "login-codigos";

    /// <summary>Mensajes del chat: 30 por minuto por usuario.</summary>
    public const string Mensajes = "mensajes";
}
