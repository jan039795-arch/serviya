using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using ServiYa.Comun.Tareas;

namespace ServiYa.UnitTests.Tareas;

/// <summary>
/// Planificador de tareas (T052) con un candado simulado. Que dos instancias reales no corran la misma
/// tarea se prueba con PostgreSQL en las pruebas de integración (requieren Docker).
/// </summary>
public sealed class PlanificadorTareasTests
{
    private readonly Bitacora bitacora = new();
    private readonly CandadoFalso candado = new();
    private readonly FakeTimeProvider reloj = new(new DateTimeOffset(2026, 11, 8, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task Una_ronda_corre_cada_tarea_con_su_candado_y_lo_suelta()
    {
        using var proveedor = Proveedor(s => s.AgregarTarea<TareaA>().AgregarTarea<TareaB>());

        await Planificador(proveedor).EjecutarRondaAsync(TestContext.Current.CancellationToken);

        bitacora.Ejecutadas.Should().Equal("A", "B");
        candado.Tomados.Should().Equal("A", "B");
        candado.Activos.Should().BeEmpty();
    }

    [Fact]
    public async Task Si_otra_instancia_tiene_el_candado_la_tarea_se_salta()
    {
        candado.Ocupados.Add("A");
        using var proveedor = Proveedor(s => s.AgregarTarea<TareaA>().AgregarTarea<TareaB>());

        await Planificador(proveedor).EjecutarRondaAsync(TestContext.Current.CancellationToken);

        bitacora.Ejecutadas.Should().Equal("B");
    }

    [Fact]
    public async Task Una_tarea_que_no_se_puede_crear_no_detiene_a_las_demas()
    {
        using var proveedor = Proveedor(s => s.AgregarTarea<TareaSinDependencia>().AgregarTarea<TareaB>());

        await Planificador(proveedor).EjecutarRondaAsync(TestContext.Current.CancellationToken);

        bitacora.Ejecutadas.Should().Equal("B");
    }

    [Fact]
    public async Task Si_el_candado_falla_la_ronda_sigue_con_las_demas()
    {
        candado.FallaCon = "A";
        using var proveedor = Proveedor(s => s.AgregarTarea<TareaA>().AgregarTarea<TareaB>());

        await Planificador(proveedor).EjecutarRondaAsync(TestContext.Current.CancellationToken);

        bitacora.Ejecutadas.Should().Equal("B");
    }

    [Fact]
    public async Task Una_ronda_con_errores_no_detiene_el_servicio()
    {
        candado.FallaCon = "A";
        using var proveedor = Proveedor(s => s.AgregarTarea<TareaA>().AgregarTarea<TareaSinDependencia>());
        using var planificador = Planificador(proveedor);

        await planificador.StartAsync(TestContext.Current.CancellationToken);
        reloj.Advance(PlanificadorTareas.Intervalo);
        reloj.Advance(PlanificadorTareas.Intervalo);
        await Task.Delay(100, TestContext.Current.CancellationToken);

        planificador.ExecuteTask!.IsFaulted.Should().BeFalse();
        planificador.ExecuteTask.IsCompleted.Should().BeFalse("sigue esperando la siguiente ronda");
        await planificador.StopAsync(TestContext.Current.CancellationToken);
        planificador.ExecuteTask.IsFaulted.Should().BeFalse();
    }

    [Fact]
    public async Task Una_tarea_que_falla_no_detiene_a_las_demas_y_suelta_su_candado()
    {
        using var proveedor = Proveedor(s => s.AgregarTarea<TareaQueFalla>().AgregarTarea<TareaB>());

        await Planificador(proveedor).EjecutarRondaAsync(TestContext.Current.CancellationToken);

        bitacora.Ejecutadas.Should().Equal("Falla", "B");
        candado.Activos.Should().BeEmpty();
    }

    [Fact]
    public async Task Cada_ejecucion_usa_un_ambito_nuevo()
    {
        using var proveedor = Proveedor(s => s.AgregarTarea<TareaA>());
        var planificador = Planificador(proveedor);

        await planificador.EjecutarRondaAsync(TestContext.Current.CancellationToken);
        await planificador.EjecutarRondaAsync(TestContext.Current.CancellationToken);

        bitacora.Instancias.Distinct().Should().HaveCount(2);
    }

    [Fact]
    public async Task Corre_al_arrancar_y_luego_cada_5_minutos()
    {
        using var proveedor = Proveedor(s => s.AgregarTarea<TareaA>());
        using var planificador = Planificador(proveedor);

        await planificador.StartAsync(TestContext.Current.CancellationToken);
        await bitacora.EsperarAsync(1);
        reloj.Advance(TimeSpan.FromMinutes(4));
        bitacora.Ejecutadas.Should().HaveCount(1);
        reloj.Advance(TimeSpan.FromMinutes(1));
        await bitacora.EsperarAsync(2);
        await planificador.StopAsync(TestContext.Current.CancellationToken);

        bitacora.Ejecutadas.Should().Equal("A", "A");
    }

    [Fact]
    public void La_clave_del_candado_es_estable_y_distinta_por_tarea()
    {
        CandadoPostgres.ClaveDe("vencer-accesos").Should().Be(CandadoPostgres.ClaveDe("vencer-accesos"));
        CandadoPostgres.ClaveDe("vencer-accesos").Should().NotBe(CandadoPostgres.ClaveDe("creditos-48h"));
    }

    [Fact]
    public void Registrar_las_tareas_antes_o_despues_del_planificador_da_el_mismo_catalogo()
    {
        var antes = new ServiceCollection().AgregarTarea<TareaA>().AgregarTareasProgramadas().AgregarTarea<TareaB>();

        using var proveedor = antes.BuildServiceProvider();

        proveedor.GetRequiredService<CatalogoTareas>().Tipos.Should().Equal(typeof(TareaA), typeof(TareaB));
        antes.Count(d => d.ServiceType == typeof(IHostedService)).Should().Be(1);
    }

    private ServiceProvider Proveedor(Action<IServiceCollection> tareas)
    {
        var services = new ServiceCollection().AddSingleton(bitacora);
        tareas(services);
        services.AgregarTareasProgramadas();
        return services.BuildServiceProvider();
    }

    private PlanificadorTareas Planificador(ServiceProvider proveedor) => new(
        proveedor.GetRequiredService<IServiceScopeFactory>(),
        proveedor.GetRequiredService<CatalogoTareas>(),
        candado,
        reloj,
        NullLogger<PlanificadorTareas>.Instance);

    private sealed class Bitacora
    {
        private readonly ConcurrentQueue<string> ejecutadas = new();
        private readonly ConcurrentQueue<object> instancias = new();

        public IReadOnlyList<string> Ejecutadas => [.. ejecutadas];

        public IReadOnlyList<object> Instancias => [.. instancias];

        public void Registrar(string nombre, object instancia)
        {
            instancias.Enqueue(instancia);
            ejecutadas.Enqueue(nombre);
        }

        public async Task EsperarAsync(int cantidad)
        {
            using var limite = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            while (ejecutadas.Count < cantidad)
            {
                await Task.Delay(10, limite.Token);
            }
        }
    }

    private sealed class CandadoFalso : ICandadoTareas
    {
        private readonly ConcurrentDictionary<string, bool> activos = new();

        public HashSet<string> Ocupados { get; } = [];

        public List<string> Tomados { get; } = [];

        public ICollection<string> Activos => activos.Keys;

        /// <summary>Tarea para la que el candado lanza una excepción (por ejemplo, sin base de datos).</summary>
        public string? FallaCon { get; set; }

        public Task<IAsyncDisposable?> IntentarTomarAsync(string nombreTarea, CancellationToken cancellationToken)
        {
            if (nombreTarea == FallaCon)
            {
                throw new InvalidOperationException("sin conexión ficticia");
            }

            if (Ocupados.Contains(nombreTarea) || !activos.TryAdd(nombreTarea, true))
            {
                return Task.FromResult<IAsyncDisposable?>(null);
            }

            Tomados.Add(nombreTarea);
            return Task.FromResult<IAsyncDisposable?>(new Soltar(() => activos.TryRemove(nombreTarea, out _)));
        }

        private sealed class Soltar(Action soltar) : IAsyncDisposable
        {
            public ValueTask DisposeAsync()
            {
                soltar();
                return ValueTask.CompletedTask;
            }
        }
    }

    private sealed class TareaA(Bitacora bitacora) : ITareaProgramada
    {
        public string Nombre => "A";

        public Task EjecutarAsync(CancellationToken cancellationToken)
        {
            bitacora.Registrar(Nombre, this);
            return Task.CompletedTask;
        }
    }

    private sealed class TareaB(Bitacora bitacora) : ITareaProgramada
    {
        public string Nombre => "B";

        public Task EjecutarAsync(CancellationToken cancellationToken)
        {
            bitacora.Registrar(Nombre, this);
            return Task.CompletedTask;
        }
    }

    /// <summary>Depende de un servicio que nadie registró: no se puede crear.</summary>
    private sealed class TareaSinDependencia(IServicioNoRegistrado servicio) : ITareaProgramada
    {
        public string Nombre => servicio.ToString() ?? "SinDependencia";

        public Task EjecutarAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private interface IServicioNoRegistrado;

    private sealed class TareaQueFalla(Bitacora bitacora) : ITareaProgramada
    {
        public string Nombre => "Falla";

        public Task EjecutarAsync(CancellationToken cancellationToken)
        {
            bitacora.Registrar(Nombre, this);
            throw new InvalidOperationException("falla ficticia");
        }
    }
}
