using ServiYa.Cuentas.Dominio;

namespace ServiYa.UnitTests.Cuentas;

/// <summary>
/// Nombre público (FR-017, T072): nombre de pila + inicial del primer apellido. Mismos casos que
/// <c>web/src/app/shared/nombre-publico.spec.ts</c>, que es la vista previa del alta.
/// </summary>
public sealed class NombrePublicoTests
{
    [Theory]
    [InlineData("Juan Pérez López", "Juan P.")]
    [InlineData("Ana Ruiz", "Ana R.")]
    [InlineData("María José Hernández López", "María H.")]
    [InlineData("  luis   órtiz  ", "luis Ó.")]
    [InlineData("Lupita", "Lupita")]
    [InlineData("", "")]
    [InlineData("José Ñúñez", "José Ñ.")]
    public void Usa_el_nombre_de_pila_y_la_inicial_del_primer_apellido(string nombreCompleto, string esperado)
    {
        NombrePublico.De(nombreCompleto).Should().Be(esperado);
    }

    [Fact]
    public void Una_inicial_con_acento_combinado_no_se_parte()
    {
        // "É" escrita como "E" + acento combinable (U+0301).
        NombrePublico.De("Ana Éscobar").Should().Be("Ana É.");
    }
}
