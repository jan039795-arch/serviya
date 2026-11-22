using Npgsql;

namespace ServiYa.IntegrationTests.Infraestructura;

/// <summary>
/// Los 6 municipios ficticios de <c>api/src/Modulos/ServiYa.Busqueda/Datos/Semillas/zona-prueba.geojson</c>
/// (T066): cuadros de 0.1° en una cuadrícula de 3 × 2 que empieza en (-100.6, 20.5). Se cargan con la
/// misma función que usa <c>importar-municipios.sql</c>, como dueño del esquema.
/// </summary>
public static class ZonaDePrueba
{
    public const int Municipios = 6;

    /// <summary>Centro del cuadro de cada municipio: columna = índice % 3, fila = índice / 3.</summary>
    public static (double Lat, double Lng) CentroDe(string claveInegi)
    {
        ArgumentNullException.ThrowIfNull(claveInegi);
        var indice = int.Parse(claveInegi[2..], System.Globalization.CultureInfo.InvariantCulture) - 1;
        return (20.55 + (0.1 * (indice / 3)), -100.55 + (0.1 * (indice % 3)));
    }

    public static string RutaGeoJson() => Path.Combine(
        ContenedoresServiYa.RaizDelRepositorio(), "api", "src", "Modulos", "ServiYa.Busqueda", "Datos", "Semillas", "zona-prueba.geojson");

    /// <summary>Importa la zona y devuelve cuántos municipios se guardaron.</summary>
    public static async Task<int> ImportarAsync(this ServiYaFactory fabrica, string? geojson = null)
    {
        ArgumentNullException.ThrowIfNull(fabrica);
        var contenido = geojson ?? await File.ReadAllTextAsync(RutaGeoJson(), TestContext.Current.CancellationToken);
        await using var conexion = new NpgsqlConnection(fabrica.ConexionDueno);
        await conexion.OpenAsync(TestContext.Current.CancellationToken);
        await using var comando = new NpgsqlCommand("SELECT catalogos.importar_municipios(@geojson::jsonb)", conexion);
        comando.Parameters.AddWithValue("geojson", contenido);
        return (int)(await comando.ExecuteScalarAsync(TestContext.Current.CancellationToken))!;
    }
}
