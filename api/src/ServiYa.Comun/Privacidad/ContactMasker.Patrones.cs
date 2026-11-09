using System.Text.RegularExpressions;

namespace ServiYa.Comun.Privacidad;

/// <summary>Expresiones de <see cref="ContactMasker"/>. Todas con límite de tiempo.</summary>
public static partial class ContactMasker
{
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

    /// <summary>
    /// Entre dígitos puede haber hasta 3 símbolos cualesquiera que no sean letras ni números
    /// (espacios, guiones, puntos, "/", "|", ":", comas, emojis...).
    /// </summary>
    private const string Separador = "[^\\p{L}\\p{N}]{0,3}";

    /// <summary>Punto de un dominio, aunque lo escriban con espacios ("wa . me").</summary>
    private const string Punto = "\\s*\\.\\s*";

    /// <summary>Dominios de mensajería y de enlaces a perfiles.</summary>
    private const string Dominios =
        "wa" + Punto + "me|wa" + Punto + "link|(?:(?:api|chat|web)" + Punto + ")?whatsapp" + Punto + "com"
        + "|t" + Punto + "me|telegram" + Punto + "(?:me|dog|org)"
        + "|instagram" + Punto + "com|instagr" + Punto + "am|threads" + Punto + "net"
        + "|facebook" + Punto + "com|fb" + Punto + "(?:com|me)|m" + Punto + "me"
        + "|tiktok" + Punto + "com|signal" + Punto + "(?:me|group)|line" + Punto + "me"
        + "|discord" + Punto + "(?:gg|com)|linktr" + Punto + "ee";

    /// <summary>Hora (8:00, 18:30) o rango de horas (08:00-18:00).</summary>
    private const string Hora = "(?:[01]?\\d|2[0-3]):[0-5]\\d";

    [GeneratedRegex(
        "(?<![\\w.])(?:https?://)?(?:www" + Punto + ")?(?:" + Dominios + ")(?![\\w-])(?:/\\S*)?",
        Opciones,
        TiempoMaximoMs)]
    private static partial Regex Enlace();

    /// <summary>Correo, también con espacios alrededor de "@". La vista atrás evita el costo cuadrático.</summary>
    [GeneratedRegex(@"(?<![\w.+-])[\w.+-]+\s*@\s*[\w-]+(?:\s*\.\s*[\w-]+)*", Opciones, TiempoMaximoMs)]
    private static partial Regex Correo();

    /// <summary>"juan arroba correo punto com", "juan (arroba) correo (punto) com", "[at]" y "[dot]".</summary>
    [GeneratedRegex(
        @"(?<![\w.+-])[\w.+-]+\s*(?:\(\s*arroba\s*\)|\[\s*arroba\s*\]|\barroba\b|\[\s*at\s*\]|\(\s*at\s*\))\s*[\w-]+"
        + @"(?:\s*(?:\.|\(\s*punto\s*\)|\[\s*punto\s*\]|\bpunto\b|\[\s*dot\s*\]|\(\s*dot\s*\))\s*[\w-]+)*",
        Opciones,
        TiempoMaximoMs)]
    private static partial Regex CorreoDisfrazado();

    /// <summary>"juan at correo dot com": exige "at" y "dot" juntos para no tocar texto normal.</summary>
    [GeneratedRegex(@"(?<![\w.+-])[\w.+-]+\s+at\s+[\w-]+(?:\s+dot\s+[\w-]+)+", Opciones, TiempoMaximoMs)]
    private static partial Regex CorreoEnIngles();

    /// <summary>Una cuenta con arroba ("@plomero.juan") que no es parte de un correo.</summary>
    [GeneratedRegex(@"(?<![\w.@])@\w(?:[\w.]{0,28}\w)?", Opciones, TiempoMaximoMs)]
    private static partial Regex CuentaConArroba();

    /// <summary>"insta: plomero.juan", "mi face es plomerojuan": se oculta solo la cuenta.</summary>
    [GeneratedRegex(
        @"\b(?:instagram|insta|ig|telegram|tg|facebook|face|fb|tiktok|signal|discord)\b\s*(?::|=|\bes\b|\bcomo\b)\s*(?<cuenta>@?\w[\w.]{2,29})",
        Opciones,
        TiempoMaximoMs)]
    private static partial Regex CuentaTrasPalabra();

    /// <summary>Dígitos y números con palabras unidos solo por separadores; puede empezar con "+" o "(".</summary>
    [GeneratedRegex("(?:[+(]\\s*)*" + Unidad + "(?:" + Separador + Unidad + ")*", Opciones, TiempoMaximoMs)]
    private static partial Regex SecuenciaNumerica();

    [GeneratedRegex(Unidad, Opciones, TiempoMaximoMs)]
    private static partial Regex UnidadNumerica();

    [GeneratedRegex(@"\S+", RegexOptions.CultureInvariant, TiempoMaximoMs)]
    private static partial Regex Palabra();

    /// <summary>Fecha completa con año creíble: 12/11/2026, 12-11-26 o 2026-11-12.</summary>
    [GeneratedRegex(
        @"^(?:(?:0?[1-9]|[12]\d|3[01])[-/.](?:0?[1-9]|1[0-2])[-/.](?:(?:19|20)\d{2}|\d{2})"
        + @"|(?:19|20)\d{2}-(?:0?[1-9]|1[0-2])-(?:0?[1-9]|[12]\d|3[01]))\z",
        RegexOptions.CultureInvariant,
        TiempoMaximoMs)]
    private static partial Regex FechaCompleta();

    [GeneratedRegex("^" + Hora + "(?:-" + Hora + ")?\\z", RegexOptions.CultureInvariant, TiempoMaximoMs)]
    private static partial Regex HoraCompleta();

    /// <summary>Cantidad con comas de miles: 1,500 · 15,000.00 · 1,250,000.</summary>
    [GeneratedRegex(@"^\d{1,3}(?:,\d{3})+(?:\.\d{1,2})?\z", RegexOptions.CultureInvariant, TiempoMaximoMs)]
    private static partial Regex CantidadCompleta();

    [GeneratedRegex(@"\b(?:whats\s?app|whats|wats|wasap|guasap|wsp|wpp)\b[^\d\n]{0,25}$", Opciones, TiempoMaximoMs)]
    private static partial Regex PrefijoWhatsApp();
}
