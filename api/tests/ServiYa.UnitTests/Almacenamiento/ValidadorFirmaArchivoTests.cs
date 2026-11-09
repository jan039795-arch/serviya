using System.Text;
using ServiYa.Comun.Almacenamiento;

namespace ServiYa.UnitTests.Almacenamiento;

/// <summary>
/// El tipo de un archivo subido se comprueba por sus primeros bytes, no por lo que dice el navegador
/// (research R3, T050). Los bytes son cabeceras sintéticas, no archivos reales.
/// </summary>
public sealed class ValidadorFirmaArchivoTests
{
    public static TheoryData<string, byte[]> Validos => new()
    {
        { "image/jpeg", [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01] },
        { "image/png", [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D] },
        { "image/webp", [.. Ascii("RIFF"), 0x24, 0x00, 0x00, 0x00, .. Ascii("WEBPVP8 ")] },
        { "application/pdf", [.. Ascii("%PDF-1.7\n%"), 0xE2, 0xE3] },
        { "video/mp4", [0x00, 0x00, 0x00, 0x20, .. Ascii("ftypisom"), 0x00, 0x00, 0x02, 0x00] },
        { "video/mp4", [0x00, 0x00, 0x00, 0x18, .. Ascii("ftypmp42"), 0x00, 0x00, 0x00, 0x00] },
        { "video/quicktime", [0x00, 0x00, 0x00, 0x14, .. Ascii("ftypqt  "), 0x00, 0x00, 0x00, 0x00] },
    };

    [Theory]
    [MemberData(nameof(Validos))]
    public void Reconoce_cada_tipo_permitido(string tipo, byte[] inicio)
    {
        ValidadorFirmaArchivo.Detectar(inicio).Should().Be(tipo);
        ValidadorFirmaArchivo.Coincide(inicio, tipo).Should().BeTrue();
    }

    [Fact]
    public void Un_ejecutable_renombrado_como_imagen_no_coincide()
    {
        byte[] ejecutable = [.. Ascii("MZ"), 0x90, 0x00, 0x03, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00];

        ValidadorFirmaArchivo.Detectar(ejecutable).Should().BeNull();
        ValidadorFirmaArchivo.Coincide(ejecutable, "image/jpeg").Should().BeFalse();
    }

    [Fact]
    public void Un_PNG_declarado_como_JPEG_no_coincide()
    {
        byte[] png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D];

        ValidadorFirmaArchivo.Coincide(png, "image/jpeg").Should().BeFalse();
    }

    [Theory]
    [InlineData("video/mp4")]
    [InlineData("video/quicktime")]
    public void MP4_y_MOV_son_intercambiables_porque_cada_celular_los_marca_distinto(string declarado)
    {
        byte[] mov = [0x00, 0x00, 0x00, 0x14, .. Ascii("ftypqt  "), 0x00, 0x00, 0x00, 0x00];

        ValidadorFirmaArchivo.Coincide(mov, declarado).Should().BeTrue();
    }

    [Fact]
    public void Una_imagen_HEIC_no_es_un_video_aunque_use_ftyp()
    {
        byte[] heic = [0x00, 0x00, 0x00, 0x18, .. Ascii("ftypheic"), 0x00, 0x00, 0x00, 0x00];

        ValidadorFirmaArchivo.Detectar(heic).Should().BeNull();
        ValidadorFirmaArchivo.Coincide(heic, "video/mp4").Should().BeFalse();
    }

    [Fact]
    public void Un_RIFF_que_no_es_WebP_no_es_imagen()
    {
        byte[] wav = [.. Ascii("RIFF"), 0x24, 0x00, 0x00, 0x00, .. Ascii("WAVEfmt ")];

        ValidadorFirmaArchivo.Detectar(wav).Should().BeNull();
    }

    [Fact]
    public void Con_menos_bytes_de_los_necesarios_no_coincide()
    {
        ValidadorFirmaArchivo.Detectar([0xFF, 0xD8]).Should().BeNull();
        ValidadorFirmaArchivo.Detectar([]).Should().BeNull();
    }

    [Fact]
    public void Un_tipo_declarado_fuera_de_la_lista_no_coincide()
    {
        byte[] jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01];

        ValidadorFirmaArchivo.Coincide(jpeg, "image/gif").Should().BeFalse();
        ValidadorFirmaArchivo.EsTipoPermitido("image/svg+xml").Should().BeFalse();
    }

    [Fact]
    public void El_tipo_declarado_se_compara_sin_importar_mayusculas_ni_parametros()
    {
        byte[] jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01];

        ValidadorFirmaArchivo.Coincide(jpeg, "Image/JPEG; charset=binary").Should().BeTrue();
    }

    [Theory]
    [InlineData("image/jpeg", 10L * 1024 * 1024, true)]
    [InlineData("image/jpeg", (10L * 1024 * 1024) + 1, false)]
    [InlineData("image/jpeg", 0, false)]
    [InlineData("video/mp4", 60L * 1024 * 1024, true)]
    [InlineData("video/mp4", (60L * 1024 * 1024) + 1, false)]
    public void Un_archivo_subido_es_valido_solo_dentro_del_tamano_de_su_tipo(string tipo, long tamano, bool esperado)
    {
        byte[] video = [0x00, 0x00, 0x00, 0x20, .. Ascii("ftypisom"), 0x00, 0x00, 0x02, 0x00];
        byte[] jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01];
        var inicio = tipo == "video/mp4" ? video : jpeg;

        ValidadorFirmaArchivo.EsValido(new ObjetoAlmacenado(tamano, tipo, inicio, "\"e\""), tipo).Should().Be(esperado);
    }

    [Fact]
    public void Un_archivo_con_bytes_de_otro_tipo_no_es_valido_aunque_el_tamano_este_bien()
    {
        byte[] png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D];

        ValidadorFirmaArchivo.EsValido(new ObjetoAlmacenado(1024, "image/jpeg", png, "\"e\""), "image/jpeg").Should().BeFalse();
    }

    private static byte[] Ascii(string texto) => Encoding.ASCII.GetBytes(texto);
}
