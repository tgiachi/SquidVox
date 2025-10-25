using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using SquidVox.Core.Collections;
using Microsoft.Xna.Framework;
using SquidVox.Core.Extensions.Collections;
using SquidVox.Tests.TestHelpers;

namespace SquidVox.Tests.Benchmarks;

/// <summary>
/// Comparison benchmarks between SvoxGameObjectCollection and naive implementations.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
/// <summary>
///
/// </summary>
public class CollectionComparisonBenchmarks
{
    private SvoxGameObjectCollection<MockGameObject> _svoxCollection = null!;
    private List<MockGameObject> _naiveList = null!;
    private GameTime _gameTime = null!;

    [Params(100, 1_000, 10_000)]
    /// <summary>
    ///
    /// </summary>
    public int ObjectCount { get; set; }

    [GlobalSetup]
    /// <summary>
    ///
    /// </summary>
    public void GlobalSetup()
    {
        _gameTime = new GameTime();
        var random = new Random(42);

        // Setup SvoxCollection
        _svoxCollection = new SvoxGameObjectCollection<MockGameObject>(ObjectCount);
        for (int i = 0; i < ObjectCount; i++)
        {
            _svoxCollection.Add(new MockGameObject
            {
                Name = $"Object_{i}",
                ZIndex = random.Next(0, 100),
                IsEnabled = true,
                IsVisible = true
            });
        }

        // Setup naive List
        _naiveList = new List<MockGameObject>(ObjectCount);
        for (int i = 0; i < ObjectCount; i++)
        {
            _naiveList.Add(new MockGameObject
            {
                Name = $"Object_{i}",
                ZIndex = random.Next(0, 100),
                IsEnabled = true,
                IsVisible = true
            });
        }
    }

    [Benchmark(Baseline = true, Description = "Svox - Update all")]
    /// <summary>
    ///
    /// </summary>
    public void SvoxUpdateAll()
    {
        _svoxCollection.UpdateAll(_gameTime);
    }

    [Benchmark(Description = "Naive - Update all")]
    /// <summary>
    ///
    /// </summary>
    public void NaiveUpdateAll()
    {
        foreach (var obj in _naiveList)
        {
            if (obj.IsEnabled)
            {
                obj.Update(_gameTime);
            }
        }
    }

    [Benchmark(Description = "Svox - Get enabled")]
    /// <summary>
    ///
    /// </summary>
    public void SvoxGetEnabled()
    {
        var enabled = _svoxCollection.GetEnabledGameObjects().ToList();
    }

    [Benchmark(Description = "Naive - Get enabled")]
    /// <summary>
    ///
    /// </summary>
    public void NaiveGetEnabled()
    {
        var enabled = _naiveList.Where(x => x.IsEnabled).ToList();
    }

    [Benchmark(Description = "Svox - Find by name")]
    /// <summary>
    ///
    /// </summary>
    public void SvoxFindByName()
    {
        var found = _svoxCollection.GetGameObjectByName("Object_50");
    }

    [Benchmark(Description = "Naive - Find by name")]
    /// <summary>
    ///
    /// </summary>
    public void NaiveFindByName()
    {
        var found = _naiveList.FirstOrDefault(x => x.Name == "Object_50");
    }

    [Benchmark(Description = "Svox - Get by type")]
    /// <summary>
    ///
    /// </summary>
    public void SvoxGetByType()
    {
        var typed = _svoxCollection.GetGameObjectsOfType<MockGameObject>().ToList();
    }

    [Benchmark(Description = "Naive - Get by type")]
    /// <summary>
    ///
    /// </summary>
    public void NaiveGetByType()
    {
        var typed = _naiveList.OfType<MockGameObject>().ToList();
    }
}