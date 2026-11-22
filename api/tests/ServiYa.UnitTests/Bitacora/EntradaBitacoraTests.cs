using System.Net;
using ServiYa.Comun.Bitacora;
using ServiYa.Comun.Privacidad;

namespace ServiYa.UnitTests.Bitacora;

/// <summary>Entrada de la bitácora (T049): acción y entidad válidas y detalle sin datos personales.</summary>
public sealed class EntradaBitacoraTests
{
    private static readonly Guid Actor = Guid.CreateVersion7();
    private static readonly Guid Entidad = Guid.CreateVersion7();

    [Fact]
    public void Guarda_los_datos_de_la_accion()
    {
        var ip = IPAddress.Parse("203.0.113.7");

        var entrada = new EntradaBitacora(
            Actor,
            AccionesBitacora.AprobarPerfil,
            EntidadesBitacora.Prestador,
            Entidad,
            ip,
            new Dictionary<string, string> { ["insignia"] = "AntecedentesPresentados" });

        entrada.ActorId.Should().Be(Actor);
        entrada.Accion.Should().Be("AprobarPerfil");
        entrada.EntidadTipo.Should().Be("Prestador");
        entrada.EntidadId.Should().Be(Entidad);
        entrada.Ip.Should().Be(ip);
        entrada.Detalle.Should().Equal(new Dictionary<string, string> { ["insignia"] = "AntecedentesPresentados" });
    }

    [Fact]
    public void Sin_detalle_queda_vacio()
    {
        var entrada = new EntradaBitacora(Actor, AccionesBitacora.VerDocumento, EntidadesBitacora.Documento, Entidad, null);

        entrada.Detalle.Should().BeEmpty();
    }

    [Fact]
    public void Una_tarea_del_sistema_no_tiene_actor()
    {
        var entrada = new EntradaBitacora(null, AccionesBitacora.SuspenderCuenta, EntidadesBitacora.Usuario, Entidad, null);

        entrada.ActorId.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("aprobarPerfil")]
    [InlineData("Aprobar Perfil")]
    [InlineData("Aprobar-Perfil")]
    [InlineData("AprobarPerfil'; DROP TABLE x")]
    [InlineData("Ab")]
    public void Rechaza_una_accion_que_no_es_un_nombre_en_PascalCase(string accion)
    {
        var crear = () => new EntradaBitacora(Actor, accion, EntidadesBitacora.Prestador, Entidad, null);

        crear.Should().Throw<ArgumentException>().WithParameterName(nameof(accion));
    }

    [Fact]
    public void Rechaza_una_accion_demasiado_larga()
    {
        var accion = "A" + new string('b', EntradaBitacora.LargoMaximoNombre);

        var crear = () => new EntradaBitacora(Actor, accion, EntidadesBitacora.Prestador, Entidad, null);

        crear.Should().Throw<ArgumentException>().WithParameterName(nameof(accion));
    }

    [Theory]
    [InlineData("")]
    [InlineData("prestador")]
    [InlineData("Prestador.Perfil")]
    public void Rechaza_un_tipo_de_entidad_invalido(string entidadTipo)
    {
        var crear = () => new EntradaBitacora(Actor, AccionesBitacora.AprobarPerfil, entidadTipo, Entidad, null);

        crear.Should().Throw<ArgumentException>().WithParameterName(nameof(entidadTipo));
    }

    [Theory]
    [InlineData("telefono")]
    [InlineData("telefonoNuevo")]
    [InlineData("correo")]
    [InlineData("email")]
    [InlineData("nombreCompleto")]
    [InlineData("curp")]
    [InlineData("rfc")]
    [InlineData("direccion")]
    [InlineData("domicilio")]
    [InlineData("contrasena")]
    [InlineData("password")]
    [InlineData("whatsapp")]
    [InlineData("fechaNacimiento")]
    public void Rechaza_claves_de_detalle_que_nombran_datos_personales(string clave)
    {
        var crear = () => new EntradaBitacora(Actor, AccionesBitacora.AprobarPerfil, EntidadesBitacora.Prestador, Entidad, null,
            new Dictionary<string, string> { [clave] = "x" });

        crear.Should().Throw<ArgumentException>().WithParameterName("detalle");
    }

    [Theory]
    [InlineData("")]
    [InlineData("Motivo")]
    [InlineData("mot ivo")]
    [InlineData("motivo.texto")]
    public void Rechaza_claves_de_detalle_que_no_son_identificadores_en_camelCase(string clave)
    {
        var crear = () => new EntradaBitacora(Actor, AccionesBitacora.AprobarPerfil, EntidadesBitacora.Prestador, Entidad, null,
            new Dictionary<string, string> { [clave] = "x" });

        crear.Should().Throw<ArgumentException>().WithParameterName("detalle");
    }

    [Fact]
    public void Rechaza_demasiadas_claves_de_detalle()
    {
        var detalle = Enumerable.Range(0, EntradaBitacora.MaximoClavesDetalle + 1).ToDictionary(i => $"clave{i}", _ => "x");

        var crear = () => new EntradaBitacora(Actor, AccionesBitacora.AprobarPerfil, EntidadesBitacora.Prestador, Entidad, null, detalle);

        crear.Should().Throw<ArgumentException>().WithParameterName("detalle");
    }

    [Fact]
    public void Rechaza_un_valor_de_detalle_demasiado_largo()
    {
        var crear = () => new EntradaBitacora(Actor, AccionesBitacora.RechazarPerfil, EntidadesBitacora.Prestador, Entidad, null,
            new Dictionary<string, string> { ["motivo"] = new string('a', EntradaBitacora.LargoMaximoValorDetalle + 1) });

        crear.Should().Throw<ArgumentException>().WithParameterName("detalle");
    }

    [Theory]
    [InlineData("Llámale al 55 1234 5678 para aclarar")]
    [InlineData("Que escriba a persona.ficticia@correo.test")]
    [InlineData("Su WhatsApp es 5512345678")]
    public void Oculta_contactos_escritos_en_los_valores_del_detalle(string motivo)
    {
        var entrada = new EntradaBitacora(Actor, AccionesBitacora.RechazarDocumento, EntidadesBitacora.Documento, Entidad, null,
            new Dictionary<string, string> { ["motivo"] = motivo });

        entrada.Detalle["motivo"].Should().Contain(ContactMasker.Marcador);
        entrada.Detalle["motivo"].Should().NotContainAny("1234", "5678", "@correo.test");
    }

    [Fact]
    public void El_detalle_no_cambia_si_se_modifica_el_diccionario_original()
    {
        var original = new Dictionary<string, string> { ["motivo"] = "Foto borrosa" };
        var entrada = new EntradaBitacora(Actor, AccionesBitacora.RechazarDocumento, EntidadesBitacora.Documento, Entidad, null, original);

        original["motivo"] = "Otro";
        original["extra"] = "x";

        entrada.Detalle.Should().Equal(new Dictionary<string, string> { ["motivo"] = "Foto borrosa" });
    }

    [Fact]
    public void Todas_las_acciones_y_entidades_conocidas_son_validas()
    {
        var acciones = typeof(AccionesBitacora).GetFields().Select(f => (string)f.GetRawConstantValue()!);
        var entidades = typeof(EntidadesBitacora).GetFields().Select(f => (string)f.GetRawConstantValue()!);

        foreach (var accion in acciones)
        {
            foreach (var entidad in entidades)
            {
                var crear = () => new EntradaBitacora(Actor, accion, entidad, Entidad, null);
                crear.Should().NotThrow();
            }
        }
    }
}
