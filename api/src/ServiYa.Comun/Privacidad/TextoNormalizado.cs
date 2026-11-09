using System.Globalization;
using System.Text;

namespace ServiYa.Comun.Privacidad;

/// <summary>
/// Texto preparado para buscar contactos, con el mapa a las posiciones del original. Quita lo que no se
/// ve o no cambia la lectura (espacios de ancho cero, guion suave, selectores de variación y el marco
/// de los emojis de teclas como 5️⃣) y lee como dígito una O o una l pegadas a un número ("56O8").
/// Sobre el original se reemplaza, así el resto del mensaje queda tal cual.
/// </summary>
internal sealed class TextoNormalizado
{
    private readonly int[] origen;

    private TextoNormalizado(string texto, int[] origen)
    {
        Texto = texto;
        this.origen = origen;
    }

    public string Texto { get; }

    public static TextoNormalizado Crear(string original)
    {
        var texto = new StringBuilder(original.Length);
        var origen = new List<int>(original.Length);
        for (var i = 0; i < original.Length; i++)
        {
            if (!SeIgnora(original[i]))
            {
                texto.Append(original[i]);
                origen.Add(i);
            }
        }

        LeerLetrasComoDigitos(texto);
        return new TextoNormalizado(texto.ToString(), [.. origen]);
    }

    /// <summary>Tramo del texto normalizado convertido a posiciones del original.</summary>
    public (int Inicio, int Fin) AlOriginal(int inicio, int fin) => (origen[inicio], origen[fin - 1] + 1);

    private static bool SeIgnora(char caracter) => CharUnicodeInfo.GetUnicodeCategory(caracter) is
        UnicodeCategory.Format or UnicodeCategory.NonSpacingMark or UnicodeCategory.EnclosingMark;

    /// <summary>O → 0 y l/I → 1 cuando tocan un dígito y del otro lado no hay otra letra.</summary>
    private static void LeerLetrasComoDigitos(StringBuilder texto)
    {
        for (var i = 0; i < texto.Length; i++)
        {
            var digito = texto[i] switch
            {
                'o' or 'O' => '0',
                'l' or 'I' => '1',
                _ => '\0',
            };
            if (digito == '\0')
            {
                continue;
            }

            var antes = i > 0 ? texto[i - 1] : ' ';
            var despues = i + 1 < texto.Length ? texto[i + 1] : ' ';
            var tocaDigito = char.IsDigit(antes) || char.IsDigit(despues);
            if (tocaDigito && !char.IsLetter(antes) && !char.IsLetter(despues))
            {
                texto[i] = digito;
            }
        }
    }
}
