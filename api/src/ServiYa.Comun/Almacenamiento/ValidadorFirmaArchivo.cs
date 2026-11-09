using System.Text;

namespace ServiYa.Comun.Almacenamiento;

/// <summary>
/// Reconoce JPEG, PNG, WebP, PDF, MP4 y MOV por sus primeros bytes (research R3). Después de la
/// subida directa a R2, la API lee el inicio del objeto y rechaza lo que no coincide con el tipo
/// declarado: así un ejecutable renombrado no pasa como foto.
/// </summary>
public static class ValidadorFirmaArchivo
{
    public const string Jpeg = "image/jpeg";
    public const string Png = "image/png";
    public const string WebP = "image/webp";
    public const string Pdf = "application/pdf";
    public const string Mp4 = "video/mp4";
    public const string Mov = "video/quicktime";

    /// <summary>Bytes que hay que leer del inicio del archivo.</summary>
    public const int BytesNecesarios = 12;

    private static readonly HashSet<string> Permitidos = new(StringComparer.Ordinal) { Jpeg, Png, WebP, Pdf, Mp4, Mov };

    /// <summary>Marcas de "ftyp" de video. HEIC/AVIF también usan "ftyp" y son imágenes: no entran.</summary>
    private static readonly HashSet<string> MarcasMp4 = new(StringComparer.Ordinal)
    {
        "isom", "iso2", "iso4", "iso5", "iso6", "mp41", "mp42", "avc1", "M4V ", "dash", "3gp4", "3gp5", "3gp6", "MSNV",
    };

    private static ReadOnlySpan<byte> FirmaJpeg => [0xFF, 0xD8, 0xFF];

    private static ReadOnlySpan<byte> FirmaPng => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    private static ReadOnlySpan<byte> FirmaPdf => "%PDF-"u8;

    private static ReadOnlySpan<byte> FirmaRiff => "RIFF"u8;

    private static ReadOnlySpan<byte> FirmaWebP => "WEBP"u8;

    private static ReadOnlySpan<byte> FirmaFtyp => "ftyp"u8;

    /// <summary>Fotos y documentos: 10 MB (igual que la app).</summary>
    public const long TamanoMaximoArchivo = 10L * 1024 * 1024;

    /// <summary>Video de presentación: 60 MB (research R4).</summary>
    public const long TamanoMaximoVideo = 60L * 1024 * 1024;

    public static bool EsTipoPermitido(string tipoContenido) => Permitidos.Contains(Normalizar(tipoContenido));

    public static long TamanoMaximo(string tipoContenido) =>
        EsVideo(Normalizar(tipoContenido)) ? TamanoMaximoVideo : TamanoMaximoArchivo;

    /// <summary>
    /// Revisión completa de un archivo ya subido: no vacío, dentro del tamaño máximo de su tipo y con
    /// los bytes del tipo declarado. La URL firmada no limita el tamaño, así que esto es obligatorio.
    /// </summary>
    public static bool EsValido(ObjetoAlmacenado objeto, string tipoDeclarado)
    {
        ArgumentNullException.ThrowIfNull(objeto);
        return objeto.TamanoBytes > 0
            && EsTipoPermitido(tipoDeclarado)
            && objeto.TamanoBytes <= TamanoMaximo(tipoDeclarado)
            && Coincide(objeto.Inicio, tipoDeclarado);
    }

    /// <summary>Tipo reconocido por los primeros bytes, o <c>null</c> si no es uno permitido.</summary>
    public static string? Detectar(ReadOnlySpan<byte> inicio)
    {
        if (inicio.Length < BytesNecesarios)
        {
            return null;
        }

        if (inicio.StartsWith(FirmaJpeg))
        {
            return Jpeg;
        }

        if (inicio.StartsWith(FirmaPng))
        {
            return Png;
        }

        if (inicio.StartsWith(FirmaPdf))
        {
            return Pdf;
        }

        if (inicio.StartsWith(FirmaRiff) && inicio[8..12].SequenceEqual(FirmaWebP))
        {
            return WebP;
        }

        return inicio[4..8].SequenceEqual(FirmaFtyp) ? DetectarVideo(inicio[8..12]) : null;
    }

    /// <summary>
    /// Si los bytes corresponden al tipo declarado. MP4 y MOV cuentan como el mismo, porque el mismo
    /// video puede declararse de una forma y marcarse de otra según el celular.
    /// </summary>
    public static bool Coincide(ReadOnlySpan<byte> inicio, string tipoDeclarado)
    {
        var declarado = Normalizar(tipoDeclarado);
        var detectado = Detectar(inicio);
        if (detectado is null || !Permitidos.Contains(declarado))
        {
            return false;
        }

        return detectado == declarado || (EsVideo(detectado) && EsVideo(declarado));
    }

    private static string? DetectarVideo(ReadOnlySpan<byte> marca)
    {
        var texto = Encoding.ASCII.GetString(marca);
        if (texto == "qt  ")
        {
            return Mov;
        }

        return MarcasMp4.Contains(texto) ? Mp4 : null;
    }

    private static bool EsVideo(string tipo) => tipo is Mp4 or Mov;

    /// <summary>"Image/JPEG; charset=binary" → "image/jpeg".</summary>
    private static string Normalizar(string tipoContenido)
    {
        ArgumentNullException.ThrowIfNull(tipoContenido);
        var separador = tipoContenido.IndexOf(';', StringComparison.Ordinal);
        var tipo = separador >= 0 ? tipoContenido[..separador] : tipoContenido;
        return tipo.Trim().ToLowerInvariant();
    }
}
