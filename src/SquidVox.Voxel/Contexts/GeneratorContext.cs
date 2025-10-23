using Microsoft.Xna.Framework;
using SquidVox.Core.Data.Primitives;
using SquidVox.Core.Noise;
using SquidVox.Voxel.Interfaces.Generation.Pipeline;
using SquidVox.Voxel.Primitives;

namespace SquidVox.Voxel.Contexts;

/// <summary>
/// Concrete implementation of generation context that holds shared data for pipeline steps.
/// </summary>
public class GeneratorContext : IGeneratorContext
{
    /// <inheritdoc/>
    public ChunkEntity Chunk { get; }

    public Vector3 WorldPosition { get; }

    /// <inheritdoc/>
    public FastNoiseLite NoiseGenerator { get; }

    /// <inheritdoc/>
    public int Seed { get; }

    /// <inheritdoc/>
    public IDictionary<string, object> CustomData { get; }


    public List<PositionAndSize> CloudAreas { get; } = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="GeneratorContext"/> class.
    /// </summary>
    /// <param name="chunk">The chunk being generated.</param>
    /// <param name="worldPosition">The world position of the chunk.</param>
    /// <param name="noiseGenerator">The noise generator to use.</param>
    /// <param name="seed">The seed for procedural generation.</param>
    public GeneratorContext(ChunkEntity chunk, Vector3 worldPosition, FastNoiseLite noiseGenerator, int seed)
    {
        Chunk = chunk ?? throw new ArgumentNullException(nameof(chunk));
        WorldPosition = worldPosition;
        NoiseGenerator = noiseGenerator ?? throw new ArgumentNullException(nameof(noiseGenerator));
        Seed = seed;
        CustomData = new Dictionary<string, object>();
    }

    public void AddCustomData(string key, object value)
    {
        CustomData[key] = value;
    }

    public void AddCloudArea(PositionAndSize area)
    {
        CloudAreas.Add(area);
    }

    public void ClearCloudAreas()
    {
        CloudAreas.Clear();
    }

    public void AddCloudArea(Vector3 cloudPosition, Vector3 size)
    {
        CloudAreas.Add(new PositionAndSize(cloudPosition, size));
    }

    public void AddCloudArea(float x, float y, float z, float sizeX, float sizeY, float sizeZ)
    {
        CloudAreas.Add(new PositionAndSize(new Vector3(x, y, z), new Vector3(sizeX, sizeY, sizeZ)));
    }
}
