using System.Diagnostics;
using Microsoft.Xna.Framework;
using Serilog;
using SquidVox.Core.Interfaces.Services;
using SquidVox.Core.Noise;
using SquidVox.Voxel.Contexts;
using SquidVox.Voxel.Data.Cache;
using SquidVox.Voxel.Interfaces.Generation.Pipeline;
using SquidVox.Voxel.Interfaces.Services;
using SquidVox.Voxel.Primitives;
using SquidVox.Voxel.Utils;

namespace SquidVox.Voxel.Services;

/// <summary>
/// Manages chunk generation using a configurable pipeline and time-based cache.
/// </summary>
public class ChunkGeneratorService : IChunkGeneratorService, IDisposable
{
    public int Seed { get; set; }

    private readonly ILogger _logger = Log.ForContext<ChunkGeneratorService>();
    private readonly ChunkCache _chunkCache;
    private readonly List<IGeneratorStep> _pipeline;
    private readonly FastNoiseLite _noiseGenerator;

    private int _initialChunkRadius = 5;
    private Vector3 _initialPosition = Vector3.Zero;

    // Metrics counters
    private long _totalChunksGenerated;
    private long _cacheHits;
    private long _cacheMisses;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChunkGeneratorService"/> class.
    /// </summary>
    /// <param name="timerService">Timer service for cache management.</param>
    /// <param name="config">Chunk generator configuration.</param>
    public ChunkGeneratorService(ITimerService timerService )
    {
        ArgumentNullException.ThrowIfNull(timerService);


        // Initialize noise generator
        _noiseGenerator = new FastNoiseLite(Seed);
        _noiseGenerator.SetNoiseType(NoiseType.OpenSimplex2);
        _noiseGenerator.SetFrequency(0.01f);

        // Initialize cache with expiration time
        _chunkCache = new ChunkCache(timerService, TimeSpan.FromMinutes(10));
        _logger.Information("Chunk cache initialized with {Minutes} minute expiration", 10);

        // Initialize generation pipeline
        _pipeline =
        [
            // new BiomeGeneratorStep(),    // Generate biome data first
            // new TerrainGeneratorStep(),  // Then generate terrain based on biome
            // new CaveGeneratorStep(),     // Carve out caves
            // new TreeGeneratorStep()      // Finally place trees on the surface
        ];

        _logger.Information(
            "Generation pipeline initialized with {StepCount} steps: {Steps}",
            _pipeline.Count,
            string.Join(", ", _pipeline.Select(s => s.Name))
        );
    }

    public async Task<ChunkEntity> GetChunkByWorldPosition(Vector3 position)
    {
        _logger.Debug("Requested chunk at world position {Position}", position);

        // Normalize position to chunk coordinates
        var chunkPosition = ChunkUtils.NormalizeToChunkPosition(position.ToNumerics());

        // Try to get from cache first
        if (_chunkCache.TryGet(chunkPosition, out var cachedChunk) && cachedChunk != null)
        {
            Interlocked.Increment(ref _cacheHits);
            _logger.Debug("Returning cached chunk at {Position}", chunkPosition);
            return cachedChunk;
        }

        // Cache miss
        Interlocked.Increment(ref _cacheMisses);

        // Generate new chunk
        _logger.Information("Generating new chunk at {Position}", chunkPosition);
        var chunk = await GenerateChunkAsync(chunkPosition);

        // Cache the generated chunk
        _chunkCache.Set(chunkPosition, chunk);

        return chunk;
    }

    public async Task<IEnumerable<ChunkEntity>> GetChunksByPositions(IEnumerable<Vector3> positions)
    {
        _logger.Debug("Requested chunks for {Count} positions", positions.Count());

        // Get all chunks in parallel
        var tasks = positions.Select(GetChunkByWorldPosition);
        var chunks = await Task.WhenAll(tasks);

        _logger.Debug("Returned {Count} chunks", chunks.Length);
        return chunks;
    }



    public async Task GenerateInitialChunksAsync()
    {
        var startTime = Stopwatch.GetTimestamp();
        _logger.Information(
            "Generating initial chunks with radius {Radius} around position {Position}",
            _initialChunkRadius,
            _initialChunkRadius
        );

        var chunksToGenerate = new List<Vector3>();

        // Normalize the initial position to chunk coordinates
        var centerChunkPos = ChunkUtils.NormalizeToChunkPosition(_initialPosition.ToNumerics());

        // Calculate all chunk positions to generate in a radius around the initial position
        for (int x = -_initialChunkRadius; x <= _initialChunkRadius; x++)
        {
            for (int z = -_initialChunkRadius; z <= _initialChunkRadius; z++)
            {
                var chunkPos = new Vector3(
                    centerChunkPos.X + (x * ChunkEntity.Size),
                    centerChunkPos.Y,
                    centerChunkPos.Z + (z * ChunkEntity.Size)
                );
                chunksToGenerate.Add(chunkPos);
            }
        }

        _logger.Information("Generating {Count} initial chunks", chunksToGenerate.Count);

        // Generate chunks in parallel
        var tasks = chunksToGenerate.Select(GetChunkByWorldPosition);
        await Task.WhenAll(tasks);

        var elapsed = Stopwatch.GetElapsedTime(startTime);
        _logger.Information("Initial chunk generation completed. Generated {Count} chunks in {Elapsed:F2}ms",
            chunksToGenerate.Count, elapsed.TotalMilliseconds);
    }

    /// <summary>
    /// Generates a new chunk at the specified position using the generation pipeline.
    /// </summary>
    /// <param name="chunkPosition">The normalized chunk position.</param>
    /// <returns>The generated chunk.</returns>
    private async Task<ChunkEntity> GenerateChunkAsync(Vector3 chunkPosition)
    {
        var chunk = new ChunkEntity(chunkPosition);
        var context = new GeneratorContext(chunk, chunkPosition, _noiseGenerator, Seed);

        _logger.Debug("Starting generation pipeline for chunk at {Position}", chunkPosition);

        // Execute each step in the pipeline
        foreach (var step in _pipeline)
        {
            _logger.Debug("Executing generation step: {StepName}", step.Name);
            await step.ExecuteAsync(context);
        }

        Interlocked.Increment(ref _totalChunksGenerated);
        _logger.Debug("Chunk generation completed at {Position}", chunkPosition);
        return chunk;
    }

    /// <summary>
    /// Gets the current number of cached chunks.
    /// </summary>
    public int CachedChunkCount => _chunkCache.Count;

    /// <summary>
    /// Clears all cached chunks.
    /// </summary>
    public void ClearCache()
    {
        _logger.Information("Clearing chunk cache");
        _chunkCache.Clear();
    }

    /// <summary>
    /// Adds a generation step to the pipeline.
    /// </summary>
    /// <param name="step">The generator step to add.</param>
    public void AddGeneratorStep(IGeneratorStep step)
    {
        ArgumentNullException.ThrowIfNull(step);
        _pipeline.Add(step);
        _logger.Information(
            "Added generator step '{StepName}' to pipeline. Total steps: {Count}",
            step.Name,
            _pipeline.Count
        );
    }

    /// <summary>
    /// Removes a generation step from the pipeline by name.
    /// </summary>
    /// <param name="stepName">The name of the step to remove.</param>
    /// <returns>True if the step was removed; otherwise, false.</returns>
    public bool RemoveGeneratorStep(string stepName)
    {
        var step = _pipeline.FirstOrDefault(s => s.Name == stepName);
        if (step != null)
        {
            _pipeline.Remove(step);
            _logger.Information(
                "Removed generator step '{StepName}' from pipeline. Remaining steps: {Count}",
                stepName,
                _pipeline.Count
            );
            return true;
        }

        _logger.Warning("Generator step '{StepName}' not found in pipeline", stepName);
        return false;
    }

    /// <summary>
    /// Gets all generator steps in the pipeline.
    /// </summary>
    public IReadOnlyList<IGeneratorStep> GetGeneratorSteps() => _pipeline.AsReadOnly();

    /// <summary>
    /// Clears all generator steps from the pipeline.
    /// </summary>
    public void ClearGeneratorSteps()
    {
        _pipeline.Clear();
        _logger.Information("Cleared all generator steps from pipeline");
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.Information("Starting ChunkGeneratorService");

        try
        {
            await GenerateInitialChunksAsync();
            _logger.Information("ChunkGeneratorService started successfully");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to start ChunkGeneratorService");
            throw;
        }
    }




    /// <inheritdoc/>
    public void Dispose()
    {
        _logger.Information("Disposing ChunkGeneratorService");
        _chunkCache.Dispose();

        GC.SuppressFinalize(this);
    }
}
