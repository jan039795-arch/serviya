using System.Text;
using System.Text.RegularExpressions;

namespace ServiYa.Comun.Privacidad;

/// <summary>Texto ya revisado y si se ocultó algo.</summary>
public sealed record ResultadoOcultamiento(string Texto, bool HuboOcultamiento);

/// <summary>
/// Oculta teléfonos, correos y cuentas o enlaces de otras apps de mensajería (FR-033, research R8).
/// Se aplica en el servidor a los mensajes sin acceso activo y a los textos públicos del perfil.
/// Servicio puro: sin estado ni dependencias. Es la primera barrera, no la única: siempre se podrá
/// inventar otra forma de escribir un número, por eso existen los reportes y la moderación (US7).
/// Los casos que cubre están en <c>tests/ServiYa.UnitTests/Privacidad/CasosEvasion.cs</c>; las
/// expresiones, en <c>ContactMasker.Patrones.cs</c>.
/// </summary>
public static partial class ContactMasker
{
    public const string Marcador = "[contacto oculto]";

    /// <summary>Más que cualquier texto de la app (mensajes de 2000); evita gastar CPU en textos enormes.</summary>
    public const int LargoMaximo = 5000;

    /// <summary>Un teléfono de México tiene 10 dígitos; con 8 ya se puede adivinar el resto.</summary>
    private const int DigitosDeTelefono = 8;

    /// <summary>Después de "WhatsApp" basta con menos dígitos para considerarlo un contacto.</summary>
    private const int DigitosTrasWhatsApp = 4;

    /// <summary>Hasta dónde se busca "WhatsApp" antes de los números.</summary>
    private const int VentanaWhatsApp = 30;

    public static ResultadoOcultamiento Ocultar(string texto)
    {
        ArgumentNullException.ThrowIfNull(texto);
        if (texto.Length > LargoMaximo)
        {
            throw new ArgumentException($"El texto pasa de {LargoMaximo} caracteres.", nameof(texto));
        }

        var normalizado = TextoNormalizado.Crear(texto);
        if (normalizado.Texto.Length == 0)
        {
            return new ResultadoOcultamiento(texto, false);
        }

        try
        {
            var tramos = BuscarContactos(normalizado.Texto)
                .Select(t => normalizado.AlOriginal(t.Inicio, t.Fin))
                .ToList();
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

    private static List<(int Inicio, int Fin)> BuscarContactos(string texto)
    {
        var tramos = new List<(int Inicio, int Fin)>();
        AgregarCoincidencias(tramos, Enlace().Matches(texto));
        AgregarCoincidencias(tramos, Correo().Matches(texto));
        AgregarCoincidencias(tramos, CorreoDisfrazado().Matches(texto));
        AgregarCoincidencias(tramos, CorreoEnIngles().Matches(texto));
        AgregarCoincidencias(tramos, CuentaConArroba().Matches(texto));
        foreach (Match coincidencia in CuentaTrasPalabra().Matches(texto))
        {
            var cuenta = coincidencia.Groups["cuenta"];
            tramos.Add((cuenta.Index, cuenta.Index + cuenta.Length));
        }

        AgregarTelefonos(tramos, texto);
        return tramos;
    }

    private static void AgregarCoincidencias(List<(int Inicio, int Fin)> tramos, MatchCollection coincidencias)
    {
        foreach (Match coincidencia in coincidencias)
        {
            tramos.Add((coincidencia.Index, coincidencia.Index + coincidencia.Length));
        }
    }

    private static void AgregarTelefonos(List<(int Inicio, int Fin)> tramos, string texto)
    {
        foreach (Match secuencia in SecuenciaNumerica().Matches(texto))
        {
            var digitos = ContarDigitos(secuencia.Value);
            var esTelefono = digitos >= DigitosDeTelefono
                || (digitos >= DigitosTrasWhatsApp && VieneDespuesDeWhatsApp(texto, secuencia.Index));
            if (esTelefono)
            {
                tramos.Add((secuencia.Index, secuencia.Index + secuencia.Length));
            }
        }
    }

    /// <summary>
    /// Cuenta los dígitos de una secuencia (cifras y números con palabras). No cuentan las palabras
    /// sueltas que son por completo una fecha, una hora o una cantidad con comas de miles: así
    /// "12/11/2026 10:30" o "$1,250,000.00" no se ocultan, pero "10-12-3456-78" sí.
    /// </summary>
    private static int ContarDigitos(string secuencia)
    {
        var exentas = Palabra().Matches(secuencia)
            .Where(p => EsFechaHoraOCantidad(p.Value))
            .Select(p => (Inicio: p.Index, Fin: p.Index + p.Length))
            .ToList();

        var total = 0;
        foreach (Match unidad in UnidadNumerica().Matches(secuencia))
        {
            if (!exentas.Exists(e => unidad.Index >= e.Inicio && unidad.Index < e.Fin))
            {
                total += unidad.Groups["dos"].Success ? 2 : 1;
            }
        }

        return total;
    }

    private static bool EsFechaHoraOCantidad(string palabra)
    {
        var limpia = palabra.Trim().TrimEnd(',', ';', '.', ')', ':').TrimStart('(', '$');
        return FechaCompleta().IsMatch(limpia) || HoraCompleta().IsMatch(limpia) || CantidadCompleta().IsMatch(limpia);
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
}
