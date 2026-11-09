using System.ComponentModel.DataAnnotations;

namespace ServiYa.Comun.Configuracion;

/// <summary>Claves VAPID para las notificaciones push del chat.</summary>
public sealed class WebPushOptions : IOpcionesConSeccion
{
    public static string Seccion => "WebPush";

    [Required]
    public string PublicKey { get; init; } = string.Empty;

    [Required]
    public string PrivateKey { get; init; } = string.Empty;

    /// <summary>Contacto que exige VAPID: <c>mailto:</c> o una URL <c>https://</c>.</summary>
    [Required, RegularExpression("^(mailto:|https://).+")]
    public string Asunto { get; init; } = "mailto:soporte@serviya.mx";
}
