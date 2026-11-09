using ServiYa.Comun.Privacidad;

namespace ServiYa.UnitTests.Privacidad;

public sealed class ContactMaskerTests
{
    [Theory]
    [MemberData(nameof(CasosEvasion.Ocultables), MemberType = typeof(CasosEvasion))]
    public void Oculta_el_contacto(string entrada, string fragmentoSensible)
    {
        var resultado = ContactMasker.Ocultar(entrada);

        resultado.HuboOcultamiento.Should().BeTrue();
        resultado.Texto.Should().Contain(ContactMasker.Marcador);
        resultado.Texto.Should().NotContainEquivalentOf(fragmentoSensible);
    }

    [Theory]
    [MemberData(nameof(CasosEvasion.NoSeOcultan), MemberType = typeof(CasosEvasion))]
    public void No_toca_textos_normales(string entrada)
    {
        var resultado = ContactMasker.Ocultar(entrada);

        resultado.HuboOcultamiento.Should().BeFalse();
        resultado.Texto.Should().Be(entrada);
    }

    [Fact]
    public void Conserva_el_resto_del_mensaje()
    {
        var resultado = ContactMasker.Ocultar("Hola, llámame al 55 1234 5678 y te paso precio.");

        resultado.Texto.Should().Be($"Hola, llámame al {ContactMasker.Marcador} y te paso precio.");
    }

    [Fact]
    public void Oculta_varios_contactos_en_el_mismo_texto()
    {
        var resultado = ContactMasker.Ocultar("55 1234 5678 o juan@correo.test o wa.me/5215512345678");

        resultado.Texto.Should().Be(
            $"{ContactMasker.Marcador} o {ContactMasker.Marcador} o {ContactMasker.Marcador}");
    }

    [Fact]
    public void Incluye_el_parentesis_y_el_mas_del_inicio()
    {
        ContactMasker.Ocultar("(55) 1234 5678").Texto.Should().NotStartWith("(");
        ContactMasker.Ocultar("+52 55 1234 5678").Texto.Should().Be(ContactMasker.Marcador);
    }

    [Fact]
    public void Un_texto_nulo_no_es_valido()
    {
        var ocultar = () => ContactMasker.Ocultar(null!);

        ocultar.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData("5 a 5-5 . ( ")]
    [InlineData("a")]
    [InlineData("a.")]
    [InlineData("a arroba ")]
    [InlineData("cinco y ")]
    public void Un_texto_del_largo_maximo_se_procesa_rapido_y_sin_ocultarlo_todo(string fragmento)
    {
        // Sin la vista atrás, una palabra larga sin "@" costaba tiempo cuadrático y llegaba al límite,
        // con lo que el mensaje completo se cambiaba por el marcador.
        var repeticiones = ContactMasker.LargoMaximo / fragmento.Length;
        var texto = string.Concat(Enumerable.Repeat(fragmento, repeticiones));

        var inicio = TimeProvider.System.GetTimestamp();
        var resultado = ContactMasker.Ocultar(texto);

        TimeProvider.System.GetElapsedTime(inicio).Should().BeLessThan(TimeSpan.FromMilliseconds(500));
        resultado.Texto.Should().NotBe(ContactMasker.Marcador);
    }

    [Fact]
    public void Un_texto_mas_largo_que_el_maximo_no_es_valido()
    {
        var ocultar = () => ContactMasker.Ocultar(new string('a', ContactMasker.LargoMaximo + 1));

        ocultar.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Los_caracteres_invisibles_fuera_del_contacto_se_conservan()
    {
        var resultado = ContactMasker.Ocultar("Hola​, llámame al 55​1234​5678");

        resultado.Texto.Should().Be($"Hola​, llámame al {ContactMasker.Marcador}");
    }
}
