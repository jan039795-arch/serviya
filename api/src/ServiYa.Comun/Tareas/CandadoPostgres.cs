using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Npgsql;
using ServiYa.Comun.Datos;

namespace ServiYa.Comun.Tareas;

/// <summary>
/// Candado con <c>pg_try_advisory_lock</c> (research R7). El candado vive en la sesión de una conexión
/// abierta mientras corre la tarea; si la API se cae, PostgreSQL lo suelta al cerrarse la conexión.
/// </summary>
public sealed class CandadoPostgres(IConfiguration configuracion) : ICandadoTareas
{
    public async Task<IAsyncDisposable?> IntentarTomarAsync(string nombreTarea, CancellationToken cancellationToken)
    {
        var cadena = configuracion.GetConnectionString(RegistroDatos.ConexionApp)
            ?? throw new InvalidOperationException($"Falta la cadena de conexión ConnectionStrings:{RegistroDatos.ConexionApp}.");
        var clave = ClaveDe(nombreTarea);
        var conexion = new NpgsqlConnection(cadena);
        try
        {
            await conexion.OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var comando = new NpgsqlCommand("SELECT pg_try_advisory_lock(@clave)", conexion);
            comando.Parameters.AddWithValue("clave", clave);
            var tomado = (bool)(await comando.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false))!;
            if (tomado)
            {
                return new CandadoTomado(conexion, clave);
            }
        }
        catch
        {
            await conexion.DisposeAsync().ConfigureAwait(false);
            throw;
        }

        await conexion.DisposeAsync().ConfigureAwait(false);
        return null;
    }

    /// <summary>Número estable de 64 bits a partir del nombre de la tarea.</summary>
    public static long ClaveDe(string nombreTarea)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nombreTarea);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes("serviya.tarea:" + nombreTarea));
        return BinaryPrimitives.ReadInt64BigEndian(hash);
    }

    private sealed class CandadoTomado(NpgsqlConnection conexion, long clave) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            try
            {
                await using var comando = new NpgsqlCommand("SELECT pg_advisory_unlock(@clave)", conexion);
                comando.Parameters.AddWithValue("clave", clave);
                await comando.ExecuteScalarAsync().ConfigureAwait(false);
            }
            finally
            {
                // Cerrar la conexión también suelta el candado si el UNLOCK falló.
                await conexion.DisposeAsync().ConfigureAwait(false);
            }
        }
    }
}
