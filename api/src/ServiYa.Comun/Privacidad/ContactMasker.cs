using System.Text;
using System.Text.RegularExpressions;

namespace ServiYa.Comun.Privacidad;

/// <summary>Texto ya revisado y si se ocultó algo.</summary>
public sealed record ResultadoOcultamiento(string Texto, bool HuboOcultamiento);

/// <summary>
/// Oculta teléfonos, correos y cuentas o enlaces de otras apps de mensajería (FR-033, research R8).
/// Se aplica en el servidor a los mensajes sin acceso activo y a los textos públicos del perfil.
/// Servicio puro: sin estado ni dependencias. Los casos que cubre están en
/// <c>tests/ServiYa.UnitTests/Privacidad/CasosEvasion.cs</c>.
/// </summary>
public static partial class ContactMasker
{
    public const string Marcador = "[contacto oculto]";

    /// <summary>Un teléfono de México tiene 10 dígitos; con 8 ya se puede adivinar el resto.</summary>
    private const int DigitosDeTelefono = 8;

    /// <summary>Después de "WhatsApp" basta con menos dígitos para considerarlo un contacto.</summary>
    private const int DigitosTrasWhatsApp = 4;

    /// <summary>Hasta dónde se busca "WhatsApp" antes de los números.</summary>
    private const int VentanaWhatsApp = 30;

    private const int TiempoMaximoMs = 250;
    private const RegexOptions Opciones = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;

    private const string Unidades = "cero|uno|una|dos|tres|cuatro|cinco|seis|siete|ocho|nueve";

    /// <summary>Palabras que valen dos dígitos: 10–19, 20–29 y decenas con "y" opcional.</summary>
    private const string Decenas =
        "(?:treinta|cuarenta|cincuenta|sesenta|setenta|ochenta|noventa)(?:\\s+y\\s+(?:" + Unidades + "))?"
        + "|diez|once|doce|trece|catorce|quince|dieci(?:s[eé]is|siete|ocho|nueve)"
        + "|veinti(?:uno|una|d[oó]s|tr[eé]s|cuatro|cinco|s[eé]is|siete|ocho|nueve)|veinte";

    /// <summary>Un dígito o un número escrito con palabra, sin pegarse a otras letras.</summary>
    private const string Unidad =
        "(?:(?<d>\\d)|(?<!\\p{L})(?:(?<dos>" + Decenas + ")|(?<uno>" + Unidades + "))(?!\\p{L}))";

    /// <summary>Espacios, guiones, puntos, paréntesis, más, guion bajo, asterisco y coma seguida de espacio.</summary>
    private const string Separador = "(?:[\\s\\-\\u2010-\\u2015.()+_*]|,(?=\\s))";

    public static ResultadoOcultamiento Ocultar(string texto)
    {
        ArgumentNullException.ThrowIfNull(texto);
        if (texto.Length == 0)
        {
            return new ResultadoOcultamiento(texto, false);
        }

        try
        {
            var tramos = new List<(int Inicio, int Fin)>();
            AgregarCoincidencias(tramos, Enlace().Matches(texto));
            AgregarCoincidencias(tramos, Correo().Matches(texto));
            AgregarCoincidencias(tramos, CorreoDisfrazado().Matches(texto));
            AgregarCoincidencias(tramos, CuentaConArroba().Matches(texto));
            AgregarCuentasTrasPalabra(tramos, texto);
            AgregarTelefonos(tramos, texto);
            return tramos.Count == 0
                ? new ResultadoOcultamiento(texto, false)
                : new ResultadoOcultamiento(Reemplazar(texto, tramos), true);
        }
        catch (RegexMatchTimeoutException)
        {
            // Si no se pudo revisar a tiempo, no se arriesga a mostrar un contacto.
            return new ResultadoOcultamiento(Marcador, true);
        }
    }

    private static void AgregarCoincidencias(List<(int Inicio, int Fin)> tramos, MatchCollection coincidencias)
    {
        foreach (Match coincidencia in coincidencias)
        {
            tramos.Add((coincidencia.Index, coincidencia.Index + coincidencia.Length));
        }
    }

    private static void AgregarCuentasTrasPalabra(List<(int Inicio, int Fin)> tramos, string texto)
    {
        foreach (Match coincidencia in CuentaTrasPalabra().Matches(texto))
        {
            var cuenta = coincidencia.Groups["cuenta"];
            tramos.Add((cuenta.Index, cuenta.Index + cuenta.Length));
        }
    }

    private static void AgregarTelefonos(List<(int Inicio, int Fin)> tramos, string texto)
    {
        var fechas = Fecha().Matches(texto).Select(f => (Inicio: f.Index, Fin: f.Index + f.Length)).ToList();
        foreach (Match secuencia in SecuenciaNumerica().Matches(texto))
        {
            var digitos = ContarDigitos(secuencia, fechas);
            var esTelefono = digitos >= DigitosDeTelefono
                || (digitos >= DigitosTrasWhatsApp && VieneDespuesDeWhatsApp(texto, secuencia.Index));
            if (esTelefono)
            {
                tramos.Add((secuencia.Index, secuencia.Index + secuencia.Length));
            }
        }
    }

    /// <summary>Cuenta los dígitos de la secuencia, sin los que forman parte de una fecha.</summary>
    private static int ContarDigitos(Match secuencia, List<(int Inicio, int Fin)> fechas)
    {
        var total = 0;
        foreach (Match unidad in UnidadNumerica().Matches(secuencia.Value))
        {
            var posicion = secuencia.Index + unidad.Index;
            if (fechas.Exists(f => posicion >= f.Inicio && posicion < f.Fin))
            {
                continue;
            }

            total += unidad.Groups["dos"].Success ? 2 : 1;
        }

        return total;
    }

    private static bool VieneDespuesDeWhatsApp(string texto, int inicio)
    {
        var desde = Math.Max(0, inicio - VentanaWhatsApp);
        return PrefijoWhatsApp().IsMatch(texto.AsSpan(desde, inicio - desde));
    }

    /// <summary>Une los tramos que se enciman o se tocan y los cambia por el marcador.</summary>
    private static string Reemplazar(string texto, List<(int Inicio, int Fin)> tramos)
    {
        tramos.Sort((a, b) => a.Inicio != b.Inicio ? a.Inicio.CompareTo(b.Inicio) : b.Fin.CompareTo(a.Fin));
        var resultado = new StringBuilder(texto.Length);
        var posicion = 0;
        var indice = 0;
        while (indice < tramos.Count)
        {
            var (inicio, fin) = tramos[indice++];
            while (indice < tramos.Count && tramos[indice].Inicio <= fin)
            {
                fin = Math.Max(fin, tramos[indice++].Fin);
            }

            resultado.Append(texto, posicion, inicio - posicion).Append(Marcador);
            posicion = fin;
        }

        return resultado.Append(texto, posicion, texto.Length - posicion).ToString();
    }

    /// <summary>Enlaces de WhatsApp, Telegram, Instagram, Facebook, Messenger y TikTok.</summary>
    [GeneratedRegex(
        @"(?<![\w.])(?:https?://)?(?:www\.)?(?:wa\.me|(?:api\.|chat\.|web\.)?whatsapp\.com|t\.me|telegram\.(?:me|dog)"
        + @"|instagram\.com|instagr\.am|facebook\.com|fb\.com|fb\.me|m\.me|tiktok\.com)(?![\w-])(?:/\S*)?",
        Opciones,
        TiempoMaximoMs)]
    private static partial Regex Enlace();

    [GeneratedRegex(@"[\w.+-]+@[\w-]+(?:\.[\w-]+)*", Opciones, TiempoMaximoMs)]
    private static partial Regex Correo();

    /// <summary>"juan arroba correo punto com", "juan (arroba) correo (punto) com", "[at]" y "[dot]".</summary>
    [GeneratedRegex(
        @"[\w.+-]+\s*(?:\(\s*arroba\s*\)|\[\s*arroba\s*\]|\barroba\b|\[\s*at\s*\]|\(\s*at\s*\))\s*[\w-]+"
        + @"(?:\s*(?:\.|\(\s*punto\s*\)|\[\s*punto\s*\]|\bpunto\b|\[\s*dot\s*\]|\(\s*dot\s*\))\s*[\w-]+)*",
        Opciones,
        TiempoMaximoMs)]
    private static partial Regex CorreoDisfrazado();

    /// <summary>Una cuenta con arroba ("@plomero.juan") que no es parte de un correo.</summary>
    [GeneratedRegex(@"(?<![\w.@])@\w(?:[\w.]{0,28}\w)?", Opciones, TiempoMaximoMs)]
    private static partial Regex CuentaConArroba();

    /// <summary>"insta: plomero.juan", "mi face es plomerojuan": se oculta solo la cuenta.</summary>
    [GeneratedRegex(
        @"\b(?:instagram|insta|ig|telegram|tg|facebook|face|fb|tiktok)\b\s*(?::|=|\bes\b|\bcomo\b)\s*(?<cuenta>@?\w[\w.]{2,29})",
        Opciones,
        TiempoMaximoMs)]
    private static partial Regex CuentaTrasPalabra();

    /// <summary>Dígitos y números con palabras unidos solo por separadores; puede empezar con "+" o "(".</summary>
    [GeneratedRegex("(?:[+(]\\s*)*" + Unidad + "(?:" + Separador + "*" + Unidad + ")*", Opciones, TiempoMaximoMs)]
    private static partial Regex SecuenciaNumerica();

    [GeneratedRegex(Unidad, Opciones, TiempoMaximoMs)]
    private static partial Regex UnidadNumerica();

    /// <summary>Fechas como 12/11/2026, 12-11-26 o 2026-11-12, que no son teléfonos.</summary>
    [GeneratedRegex(
        @"(?<!\d)(?:(?:0?[1-9]|[12]\d|3[01])[-/.](?:0?[1-9]|1[0-2])[-/.](?:\d{4}|\d{2})"
        + @"|(?:19|20)\d{2}-(?:0?[1-9]|1[0-2])-(?:0?[1-9]|[12]\d|3[01]))(?!\d)",
        Opciones,
        TiempoMaximoMs)]
    private static partial Regex Fecha();

    [GeneratedRegex(@"\b(?:whats\s?app|whats|wats|wasap|guasap|wsp|wpp)\b[^\d\n]{0,25}$", Opciones, TiempoMaximoMs)]
    private static partial Regex PrefijoWhatsApp();
}
