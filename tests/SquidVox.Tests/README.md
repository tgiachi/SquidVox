# SquidVox Tests

This directory contains unit tests and performance benchmarks for the SquidVox project.

## Unit Tests

Run all unit tests:
```bash
dotnet test
```

Run tests with detailed output:
```bash
dotnet test --logger "console;verbosity=detailed"
```

Run tests in Release mode:
```bash
dotnet test -c Release
```

## Performance Benchmarks

The project includes comprehensive performance benchmarks using BenchmarkDotNet.

### Run All Benchmarks

```bash
cd tests/SquidVox.Tests
dotnet run -c Release -- --all
```

### Run Specific Benchmarks

**Collection Operations:**
```bash
dotnet run -c Release -- --collection
```

**Comparison with Naive Implementations:**
```bash
dotnet run -c Release -- --comparison
```

### Benchmark Categories

#### SvoxGameObjectCollectionBenchmarks
Tests various collection operations with different object counts (100, 1,000, 10,000):
- Add objects
- Add with pre-allocated capacity
- Iterate all objects
- Iterate with for loop
- Update all enabled
- Get enabled/visible/active objects
- Get objects in Z range
- Get objects by type
- Find by name
- Check for ZIndex changes
- Force sort
- Contains check
- Remove single object
- Clear collection
- ToArray conversion

#### CollectionComparisonBenchmarks
Compares SvoxGameObjectCollection against naive `List<T>` implementations:
- Update all objects
- Get enabled objects
- Find by name
- Get objects by type

### Reading Benchmark Results

BenchmarkDotNet will output:
- **Mean**: Average execution time
- **Error**: 99.9% confidence interval
- **StdDev**: Standard deviation
- **Rank**: Performance ranking (1 is fastest)
- **Allocated**: Memory allocations

### Example Output

```
| Method              | ObjectCount | Mean       | Error    | StdDev   | Rank | Allocated |
|-------------------- |------------ |-----------:|---------:|---------:|-----:|----------:|
| Iterate with for    |         100 |   125.3 ns |  2.1 ns |  1.8 ns |    1 |         - |
| Update all enabled  |         100 |   1,234 ns | 15.2 ns | 14.1 ns |    2 |      32 B |
| Get enabled objects |         100 |   2,145 ns | 21.3 ns | 19.9 ns |    3 |     512 B |
```

## Test Structure

```
SquidVox.Tests/
├── Benchmarks/
│   ├── BenchmarkRunner.cs                      # Benchmark entry point
│   └── SvoxGameObjectCollectionBenchmarks.cs   # Performance benchmarks
├── Collections/
│   └── SvoxGameObjectCollectionTests.cs        # Unit tests
└── TestHelpers/
    └── MockGameObject.cs                       # Mock objects for testing
```

## Mock Objects

The test suite includes several mock game objects:
- **MockGameObject**: Basic game object with update/render tracking
- **MockInitializableGameObject**: With ISVoxInitializable support
- **MockInputGameObject**: With ISVoxInputReceiver support

## Coverage

Current test coverage includes:
- ✅ Add/Remove operations
- ✅ Sorting by ZIndex
- ✅ ZIndex change detection
- ✅ Filtering (enabled, visible, active)
- ✅ Z-range queries
- ✅ Type-based queries
- ✅ Name-based queries
- ✅ Enumeration
- ✅ Array conversion
- ✅ Performance benchmarks

## Performance Expectations

Based on benchmarks, SvoxGameObjectCollection should perform:
- **Add**: O(1) - ~50-100ns per operation
- **Iterate**: O(n) - ~1-2ns per object
- **Update All**: O(n) - ~10-20ns per object
- **Get by Type**: O(1) cached - ~100-500ns total
- **Find by Name**: O(n) - ~5-10ns per object
- **Sort**: O(n log n) - Only when dirty

## Continuous Integration

To run tests in CI:
```bash
# Run tests
dotnet test -c Release --logger trx

# Run benchmarks (optional, may be slow)
dotnet run -c Release --project tests/SquidVox.Tests/SquidVox.Tests.csproj -- --collection
```

## Troubleshooting

**Benchmarks not running?**
- Ensure you're using Release configuration (`-c Release`)
- Check that BenchmarkDotNet package is installed
- Verify the project builds without errors

**Tests failing?**
- Check that SquidVox.Core builds successfully
- Ensure all dependencies are restored (`dotnet restore`)
- Run tests with verbose logging to see details

## Contributing

When adding new features to SvoxGameObjectCollection:
1. Add corresponding unit tests
2. Consider adding performance benchmarks for critical paths
3. Run all tests before submitting PR
4. Check benchmark results to avoid performance regressions
