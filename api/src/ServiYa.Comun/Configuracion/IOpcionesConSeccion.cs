namespace ServiYa.Comun.Configuracion;

/// <summary>Opciones que se leen de una sección de la configuración.</summary>
public interface IOpcionesConSeccion
{
    /// <summary>Nombre de la sección, por ejemplo <c>Storage</c>.</summary>
    static abstract string Seccion { get; }
}
