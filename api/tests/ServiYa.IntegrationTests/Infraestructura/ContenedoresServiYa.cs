using System.Security.Cryptography;
using Amazon.S3;
using Amazon.S3.Model;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Npgsql;
using ServiYa.Comun.Almacenamiento;
using ServiYa.Comun.Configuracion;
using ServiYa.IntegrationTests.Infraestructura;
using Testcontainers.PostgreSql;

[assembly: AssemblyFixture(typeof(ContenedoresServiYa))]

namespace ServiYa.IntegrationTests.Infraestructura;

/// <summary>Cadenas de conexión de una base de pruebas: la de la app y la del dueño (migraciones).</summary>
internal sealed record BaseDePrueba(string Nombre, string ConexionApp, string ConexionDueno);

/// <summary>
/// PostgreSQL con PostGIS y RustFS, compartidos por todas las pruebas de integración (T046). PostgreSQL
/// se inicializa con el mismo <c>docker/postgres/init.sql</c> del proyecto, así los roles
/// <c>serviya_owner</c> y <c>serviya_app</c> son los de desarrollo y producción. Cada fábrica pide su
/// propia base, copiada de la plantilla <c>serviya</c>, para que las pruebas no se estorben.
/// </summary>
public sealed class ContenedoresServiYa : IAsyncLifetime
{
    public const string ImagenPostgis = "postgis/postgis:17-3.7";
    public const string ImagenRustFs = "rustfs/rustfs:1.0.4";
    private const int PuertoS3 = 9000;

    // Contraseñas solo de esta ejecución, generadas al azar.
    private readonly string claveSuperusuario = Secreto();
    private readonly string claveDueno = Secreto();
    private readonly string claveApp = Secreto();
    private readonly SemaphoreSlim creandoBase = new(1, 1);

    private PostgreSqlContainer? postgres;
    private IContainer? almacen;

    public string AccesoAlmacen { get; } = "serviya-pruebas";

    public string SecretoAlmacen { get; } = Secreto();

    /// <summary>URL del S3 de RustFS en el equipo, por ejemplo <c>http://127.0.0.1:49153</c>.</summary>
    public string EndpointAlmacen => $"http://{almacen!.Hostname}:{almacen.GetMappedPublicPort(PuertoS3)}";

    public async ValueTask InitializeAsync()
    {
        postgres = new PostgreSqlBuilder(ImagenPostgis)
            .WithDatabase("serviya")
            .WithUsername("postgres")
            .WithPassword(claveSuperusuario)
            .WithEnvironment("SERVIYA_OWNER_PASSWORD", claveDueno)
            .WithEnvironment("SERVIYA_APP_PASSWORD", claveApp)
            .WithResourceMapping(await File.ReadAllBytesAsync(RutaInitSql()), "/docker-entrypoint-initdb.d/20_serviya.sql")
            .Build();
        almacen = new ContainerBuilder(ImagenRustFs)
            .WithEnvironment("RUSTFS_VOLUMES", "/data")
            .WithEnvironment("RUSTFS_ADDRESS", $"0.0.0.0:{PuertoS3}")
            .WithEnvironment("RUSTFS_ACCESS_KEY", AccesoAlmacen)
            .WithEnvironment("RUSTFS_SECRET_KEY", SecretoAlmacen)
            .WithTmpfsMount("/data")
            .WithCreateParameterModifier(parametros => parametros.User = "0:0")
            .WithPortBinding(PuertoS3, assignRandomHostPort: true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(peticion => peticion
                .ForPort(PuertoS3)
                .ForPath("/health")))
            .Build();

        await Task.WhenAll(postgres.StartAsync(), almacen.StartAsync()).ConfigureAwait(false);
        await CrearBucketsAsync().ConfigureAwait(false);
    }

    /// <summary>Base nueva, copia de la plantilla <c>serviya</c> (roles, PostGIS y privilegios por defecto).</summary>
    internal async Task<BaseDePrueba> CrearBaseAsync()
    {
        var nombre = $"serviya_{Guid.CreateVersion7():N}";
        await creandoBase.WaitAsync().ConfigureAwait(false);
        try
        {
            await using var conexion = new NpgsqlConnection(postgres!.GetConnectionString());
            await conexion.OpenAsync().ConfigureAwait(false);
            // Los permisos sobre la base no se copian con la plantilla: se repiten los de init.sql.
            foreach (var sql in new[]
            {
                $"CREATE DATABASE {nombre} TEMPLATE serviya OWNER serviya_owner",
                $"REVOKE ALL ON DATABASE {nombre} FROM PUBLIC",
                $"GRANT CONNECT, TEMPORARY ON DATABASE {nombre} TO serviya_app",
            })
            {
#pragma warning disable CA2100 // El nombre lo genera la prueba (GUID), no viene de fuera.
                await using var comando = new NpgsqlCommand(sql, conexion);
#pragma warning restore CA2100
                await comando.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
        }
        finally
        {
            creandoBase.Release();
        }

        return new BaseDePrueba(nombre, Conexion(nombre, "serviya_app", claveApp), Conexion(nombre, "serviya_owner", claveDueno));
    }

    public async ValueTask DisposeAsync()
    {
        creandoBase.Dispose();
        if (postgres is not null)
        {
            await postgres.DisposeAsync().ConfigureAwait(false);
        }

        if (almacen is not null)
        {
            await almacen.DisposeAsync().ConfigureAwait(false);
        }
    }

    /// <summary>Opciones de almacenamiento que apuntan al RustFS de las pruebas.</summary>
    internal StorageOptions OpcionesAlmacen() => new()
    {
        Endpoint = EndpointAlmacen,
        AccessKey = AccesoAlmacen,
        SecretKey = SecretoAlmacen,
        Region = "us-east-1",
        UrlPublica = $"{EndpointAlmacen}/serviya-publico",
    };

    private string Conexion(string baseDeDatos, string usuario, string clave) =>
        new NpgsqlConnectionStringBuilder(postgres!.GetConnectionString())
        {
            Database = baseDeDatos,
            Username = usuario,
            Password = clave,
        }.ConnectionString;

    private async Task CrearBucketsAsync()
    {
        using var s3 = AlmacenR2.CrearCliente(OpcionesAlmacen());
        await s3.PutBucketAsync("serviya-privado").ConfigureAwait(false);
        await s3.PutBucketAsync("serviya-publico").ConfigureAwait(false);
        var politica = await File.ReadAllTextAsync(Path.Combine(RaizDelRepositorio(), "docker", "almacen", "politica-publico.json"))
            .ConfigureAwait(false);
        await s3.PutBucketPolicyAsync(new PutBucketPolicyRequest { BucketName = "serviya-publico", Policy = politica })
            .ConfigureAwait(false);
    }

    private static string RutaInitSql() => Path.Combine(RaizDelRepositorio(), "docker", "postgres", "init.sql");

    private static string RaizDelRepositorio()
    {
        for (var carpeta = new DirectoryInfo(AppContext.BaseDirectory); carpeta is not null; carpeta = carpeta.Parent)
        {
            if (File.Exists(Path.Combine(carpeta.FullName, "docker-compose.yml")))
            {
                return carpeta.FullName;
            }
        }

        throw new InvalidOperationException("No se encontró la raíz del repositorio (docker-compose.yml).");
    }

    private static string Secreto() => Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(16));
}
