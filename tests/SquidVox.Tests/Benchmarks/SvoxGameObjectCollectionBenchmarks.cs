using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using SquidVox.Core.Collections;
using Microsoft.Xna.Framework;
using SquidVox.Core.Extensions.Collections;
using SquidVox.Tests.TestHelpers;

namespace SquidVox.Tests.Benchmarks;

/// <summary>
/// Performance benchmarks for SvoxGameObjectCollection.
/// Run with: dotnet run -c Release --project SquidVox.Tests.csproj --filter *SvoxGameObjectCollectionBenchmarks*
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
/// <summary>
/// 
/// </summary>
public class SvoxGameObjectCollectionBenchmarks
{
    private SvoxGameObjectCollection<MockGameObject> _collection = null!;
    private SvoxGameObjectCollection<MockGameObject> _collectionSmall = null!;
    private SvoxGameObjectCollection<MockGameObject> _collectionMedium = null!;
    private SvoxGameObjectCollection<MockGameObject> _collectionLarge = null!;
    private List<MockGameObject> _testObjects = null!;
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
        _testObjects = new List<MockGameObject>(ObjectCount);

        // Create test objects with random ZIndex
        var random = new Random(42); // Fixed seed for reproducibility
        for (int i = 0; i < ObjectCount; i++)
        {
            _testObjects.Add(new MockGameObject
            {
                Name = $"Object_{i}",
                ZIndex = random.Next(0, 100),
                IsEnabled = true,
                IsVisible = true
            });
        }

        // Setup collections
        _collectionSmall = new SvoxGameObjectCollection<MockGameObject>(100);
        for (int i = 0; i < Math.Min(100, ObjectCount); i++)
        {
            _collectionSmall.Add(_testObjects[i]);
        }

        _collectionMedium = new SvoxGameObjectCollection<MockGameObject>(1000);
        for (int i = 0; i < Math.Min(1000, ObjectCount); i++)
        {
            _collectionMedium.Add(_testObjects[i]);
        }

        _collectionLarge = new SvoxGameObjectCollection<MockGameObject>(10000);
        for (int i = 0; i < ObjectCount; i++)
        {
            _collectionLarge.Add(_testObjects[i]);
        }
    }

    [IterationSetup]
    /// <summary>
    /// 
    /// </summary>
    public void IterationSetup()
    {
        _collection = new SvoxGameObjectCollection<MockGameObject>(ObjectCount);
    }

    [Benchmark(Description = "Add objects")]
    /// <summary>
    /// 
    /// </summary>
    public void AddObjects()
    {
        foreach (var obj in _testObjects)
        {
            _collection.Add(obj);
        }
    }

    [Benchmark(Description = "Add with pre-allocated capacity")]
    /// <summary>
    /// 
    /// </summary>
    public void AddObjectsWithCapacity()
    {
        var collection = new SvoxGameObjectCollection<MockGameObject>(ObjectCount);
        foreach (var obj in _testObjects)
        {
            collection.Add(obj);
        }
    }

    [Benchmark(Description = "Iterate all objects")]
    /// <summary>
    /// 
    /// </summary>
    public void IterateAll()
    {
        var count = 0;
        foreach (var obj in _collectionLarge)
        {
            count++;
        }
    }

    [Benchmark(Description = "Iterate with for loop")]
    /// <summary>
    /// 
    /// </summary>
    public void IterateWithForLoop()
    {
        var count = 0;
        for (int i = 0; i < _collectionLarge.Count; i++)
        {
            var obj = _collectionLarge[i];
            count++;
        }
    }

    [Benchmark(Description = "Update all enabled")]
    /// <summary>
    /// 
    /// </summary>
    public void UpdateAllEnabled()
    {
        _collectionLarge.UpdateAll(_gameTime);
    }

    [Benchmark(Description = "Get enabled objects")]
    /// <summary>
    /// 
    /// </summary>
    public void GetEnabledObjects()
    {
        var enabled = _collectionLarge.GetEnabledGameObjects().ToList();
    }

    [Benchmark(Description = "Get visible objects")]
    /// <summary>
    /// 
    /// </summary>
    public void GetVisibleObjects()
    {
        var visible = _collectionLarge.GetVisibleGameObjects().ToList();
    }

    [Benchmark(Description = "Get active objects")]
    /// <summary>
    /// 
    /// </summary>
    public void GetActiveObjects()
    {
        var active = _collectionLarge.GetActiveGameObjects().ToList();
    }

    [Benchmark(Description = "Get objects in Z range")]
    /// <summary>
    /// 
    /// </summary>
    public void GetObjectsInZRange()
    {
        var inRange = _collectionLarge.GetGameObjectsInZRange(25, 75).ToList();
    }

    [Benchmark(Description = "Get objects by type")]
    /// <summary>
    /// 
    /// </summary>
    public void GetObjectsByType()
    {
        var typed = _collectionLarge.GetGameObjectsOfType<MockGameObject>().ToList();
    }

    [Benchmark(Description = "Find by name")]
    /// <summary>
    /// 
    /// </summary>
    public void FindByName()
    {
        var found = _collectionLarge.GetGameObjectByName("Object_50");
    }

    [Benchmark(Description = "Check for ZIndex changes")]
    /// <summary>
    /// 
    /// </summary>
    public void CheckForZIndexChanges()
    {
        // Modify some ZIndex values
        for (int i = 0; i < Math.Min(10, _collectionLarge.Count); i++)
        {
            _testObjects[i].ZIndex += 1;
        }

        _collectionLarge.CheckForZIndexChanges();

        // Reset for next iteration
        for (int i = 0; i < Math.Min(10, _collectionLarge.Count); i++)
        {
            _testObjects[i].ZIndex -= 1;
        }
    }

    [Benchmark(Description = "Force sort")]
    /// <summary>
    /// 
    /// </summary>
    public void ForceSort()
    {
        _collectionLarge.ForceSort();
    }

    [Benchmark(Description = "Contains check")]
    /// <summary>
    /// 
    /// </summary>
    public void ContainsCheck()
    {
        var result = _collectionLarge.Contains(_testObjects[50]);
    }

    [Benchmark(Description = "Remove single object")]
    /// <summary>
    /// 
    /// </summary>
    public void RemoveSingleObject()
    {
        var tempCollection = new SvoxGameObjectCollection<MockGameObject>();
        foreach (var obj in _testObjects.Take(100))
        {
            tempCollection.Add(obj);
        }

        tempCollection.Remove(_testObjects[50]);
    }

    [Benchmark(Description = "Clear collection")]
    /// <summary>
    /// 
    /// </summary>
    public void ClearCollection()
    {
        var tempCollection = new SvoxGameObjectCollection<MockGameObject>();
        foreach (var obj in _testObjects.Take(100))
        {
            tempCollection.Add(obj);
        }

        tempCollection.Clear();
    }

    [Benchmark(Description = "ToArray conversion")]
    /// <summary>
    /// 
    /// </summary>
    public void ToArrayConversion()
    {
        var array = _collectionLarge.ToArray();
    }
}


