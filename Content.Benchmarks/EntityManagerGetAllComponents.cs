using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Content.IntegrationTests;
using Content.IntegrationTests.Pair;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;

namespace Content.Benchmarks;

[Virtual]
public partial class EntityManagerGetAllComponents
{
    private TestPair _pair = default!;
    private IEntityManager _entityManager = default!;

    [Params(5000)]
    public int N { get; set; }

    [GlobalSetup]
    public async Task Setup()
    {
        BenchmarkPath.Configure();
        PoolManager.Startup(typeof(BenchmarkDummyComponent).Assembly);
        _pair = await PoolManager.GetServerClient();
        _entityManager = _pair.Server.EntMan;

        await _pair.Server.WaitPost(() =>
        {
            for (var i = 0; i < N; i++)
            {
                var entity = _entityManager.Spawn();
                _entityManager.AddComponent<BenchmarkDummyComponent>(entity);
            }
        });
    }

    [GlobalCleanup]
    public async Task Cleanup()
    {
        await _pair.DisposeAsync();
        PoolManager.Shutdown();
    }

    [Benchmark]
    public int Run()
    {
        var count = 0;
        foreach (var _ in _entityManager.EntityQuery<BenchmarkDummyComponent>(true))
            count++;
        return count;
    }

    [Benchmark]
    public int Noop()
    {
        _entityManager.TryGetComponent(default, out BenchmarkDummyComponent _);
        return 0;
    }
}

[RegisterComponent]
public sealed partial class BenchmarkDummyComponent : Component;
