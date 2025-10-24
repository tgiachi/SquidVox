using Microsoft.Xna.Framework;
using SquidVox.Core.Data.Primitives;
using SquidVox.Core.Noise;
using SquidVox.Voxel.Interfaces.Generation.Pipeline;
using SquidVox.Voxel.Primitives;
using SquidVox.Voxel.Types;

namespace SquidVox.Voxel.Contexts;

/// <summary>
/// Concrete implementation of generation context that holds shared data for pipeline steps.
/// </summary>
public class GeneratorContext : IGeneratorContext
{
    /// <inheritdoc/>
    public ChunkEntity Chunk { get; set; }

    public Vector3 WorldPosition { get; }
    /// <inheritdoc/>
    public FastNoiseLite NoiseGenerator { get; }

    /// <inheritdoc/>
    public int Seed { get; }

    /// <inheritdoc/>
    public IDictionary<string, object> CustomData { get; }


    public int ChunkSize() => ChunkEntity.Size;

    public int ChunkHeight() => ChunkEntity.Height;

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


    public ChunkEntity GetChunk() => Chunk;

    public Vector3 GetWorldPosition() => WorldPosition;

    public FastNoiseLite GetNoise() => NoiseGenerator;


    public BlockEntity NewBlockEntity(BlockType type)
    {
        return new BlockEntity(type);
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

    /// <summary>
    /// Fills a 3D region with the specified block. Optimized for bulk operations.
    /// </summary>
    public void FillBlocks(int startX, int startY, int startZ, int endX, int endY, int endZ, BlockEntity block)
    {
        // Validate bounds
        if (startX < 0 || endX > ChunkEntity.Size || startX >= endX)
            throw new ArgumentOutOfRangeException(nameof(startX), "Invalid X range");
        if (startY < 0 || endY > ChunkEntity.Height || startY >= endY)
            throw new ArgumentOutOfRangeException(nameof(startY), "Invalid Y range");
        if (startZ < 0 || endZ > ChunkEntity.Size || startZ >= endZ)
            throw new ArgumentOutOfRangeException(nameof(startZ), "Invalid Z range");

        // Fill the region with native C# loops for maximum performance
        for (int x = startX; x < endX; x++)
        {
            for (int z = startZ; z < endZ; z++)
            {
                for (int y = startY; y < endY; y++)
                {
                    Chunk.SetBlock(x, y, z, block);
                }
            }
        }
    }

    /// <summary>
    /// Fills an entire horizontal layer at the specified Y coordinate.
    /// </summary>
    public void FillLayer(int y, BlockEntity block)
    {
        if (y < 0 || y >= ChunkEntity.Height)
            throw new ArgumentOutOfRangeException(nameof(y), "Y coordinate out of bounds");

        FillBlocks(0, y, 0, ChunkEntity.Size, y + 1, ChunkEntity.Size, block);
    }

    /// <summary>
    /// Fills a vertical column from startY to endY at the specified X,Z coordinates.
    /// </summary>
    public void FillColumn(int x, int z, int startY, int endY, BlockEntity block)
    {
        if (x < 0 || x >= ChunkEntity.Size)
            throw new ArgumentOutOfRangeException(nameof(x), "X coordinate out of bounds");
        if (z < 0 || z >= ChunkEntity.Size)
            throw new ArgumentOutOfRangeException(nameof(z), "Z coordinate out of bounds");
        if (startY < 0 || endY > ChunkEntity.Height || startY >= endY)
            throw new ArgumentOutOfRangeException(nameof(startY), "Invalid Y range");

        for (int y = startY; y < endY; y++)
        {
            Chunk.SetBlock(x, y, z, block);
        }
    }

    /// <summary>
    /// Sets a single block at the specified coordinates.
    /// Pass null to remove a block (create air/cave).
    /// </summary>
    public void SetBlock(int x, int y, int z, BlockEntity? block)
    {
        // Use default(BlockEntity) for null, which is Air (BlockType.Air = 0)
        Chunk.SetBlock(x, y, z, block ?? default(BlockEntity));
    }

    public override string ToString()
    {
        return $"GeneratorContext(Chunk: {Chunk}, WorldPosition: {WorldPosition}, Seed: {Seed}, CustomDataCount: {CustomData.Count})";
    }

}
