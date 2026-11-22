using ServiYa.Cuentas.Dominio;

namespace ServiYa.UnitTests.Cuentas;

/// <summary>Suspender y reactivar una cuenta (FR-041); una cuenta eliminada ya no cambia.</summary>
public sealed class EstadoUsuarioTests
{
    [Fact]
    public void Una_cuenta_activa_se_suspende_y_se_reactiva()
    {
        var usuario = Nuevo();

        usuario.Suspender();
        usuario.Estado.Should().Be(EstadoUsuario.Suspendido);

        usuario.Reactivar();
        usuario.Estado.Should().Be(EstadoUsuario.Activo);
    }

    [Fact]
    public void Una_cuenta_eliminada_no_se_suspende_ni_se_reactiva()
    {
        var usuario = Nuevo();
        usuario.MarcarEliminado(DateTimeOffset.UnixEpoch);

        var suspender = () => usuario.Suspender();
        var reactivar = () => usuario.Reactivar();

        suspender.Should().Throw<InvalidOperationException>();
        reactivar.Should().Throw<InvalidOperationException>();
        usuario.Estado.Should().Be(EstadoUsuario.Eliminado);
        usuario.EliminadoEn.Should().Be(DateTimeOffset.UnixEpoch);
    }

    private static Usuario Nuevo() => new("persona@correo.test", "Persona Ficticia", new DateOnly(1990, 1, 1));
}
