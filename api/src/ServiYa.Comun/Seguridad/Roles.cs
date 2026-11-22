namespace ServiYa.Comun.Seguridad;

/// <summary>Roles de ServiYa (FR-002). Una persona puede ser cliente y prestador con la misma cuenta.</summary>
public static class Roles
{
    public const string Cliente = nameof(Cliente);
    public const string Prestador = nameof(Prestador);
    public const string Admin = nameof(Admin);

    public static IReadOnlyList<string> Todos { get; } = [Cliente, Prestador, Admin];
}
