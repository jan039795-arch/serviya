namespace ServiYa.Comun.Seguridad;

/// <summary>
/// Marca un endpoint que modifica datos pero no lleva el encabezado <c>X-XSRF-TOKEN</c>, porque no lo
/// llama el navegador con la cookie de sesión. Solo el webhook de Mercado Pago, que se valida con su
/// firma (contracts/webhooks.md).
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class SinValidacionXsrfAttribute : Attribute;
