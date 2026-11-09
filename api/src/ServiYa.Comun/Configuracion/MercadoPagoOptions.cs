using System.ComponentModel.DataAnnotations;

namespace ServiYa.Comun.Configuracion;

/// <summary>Credenciales de Mercado Pago (research R6). Solo vienen de secretos, nunca del repositorio.</summary>
public sealed class MercadoPagoOptions : IOpcionesConSeccion
{
    public static string Seccion => "MercadoPago";

    [Required]
    public string AccessToken { get; init; } = string.Empty;

    /// <summary>Clave con la que se verifica la firma <c>x-signature</c> de los webhooks.</summary>
    [Required]
    public string WebhookSecret { get; init; } = string.Empty;
}
