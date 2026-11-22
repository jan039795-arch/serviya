using System.Globalization;

namespace ServiYa.Cuentas.Dominio;

/// <summary>
/// Nombre que se muestra en público (FR-017): nombre de pila + inicial del primer apellido.
/// "Juan Pérez López" → "Juan P."; "Ana Ruiz" → "Ana R.". Con 3 palabras o más se toma la penúltima
/// como primer apellido. Igual que la vista previa de <c>web/src/app/shared/nombre-publico.ts</c>.
/// </summary>
public static class NombrePublico
{
    private static readonly CultureInfo EspanolMexico = CultureInfo.GetCultureInfo("es-MX");

    public static string De(string nombreCompleto)
    {
        ArgumentNullException.ThrowIfNull(nombreCompleto);
        var palabras = nombreCompleto.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        switch (palabras.Length)
        {
            case 0:
                return string.Empty;
            case 1:
                return palabras[0];
            default:
                var apellido = palabras.Length >= 3 ? palabras[^2] : palabras[1];

                // Primer carácter visible completo: una letra con acento combinable no se parte.
                var inicial = StringInfo.GetNextTextElement(apellido);
                return $"{palabras[0]} {inicial.ToUpper(EspanolMexico)}.";
        }
    }
}
