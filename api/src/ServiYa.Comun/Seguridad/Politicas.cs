namespace ServiYa.Comun.Seguridad;

/// <summary>Políticas de autorización con nombre; las define Cuentas y las piden los demás módulos.</summary>
public static class Politicas
{
    /// <summary>
    /// Rol <c>Admin</c> en la cookie <b>y</b> en la base, con la cuenta activa. A quien le retiran el
    /// rol o lo suspenden pierde el acceso de inmediato, sin esperar a que se renueve su cookie.
    /// </summary>
    public const string SoloAdmin = "solo-admin";
}
