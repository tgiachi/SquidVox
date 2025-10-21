using SquidVox.Voxel.Data;
using SquidVox.Voxel.Types;

namespace SquidVox.World3d.Services;

/// <summary>
/// Simple flat world generator for testing.
/// </summary>
public class FlatWorldGeneratorService
{
    public Task<ChunkEntity> GenerateChunkAsync(int chunkX, int chunkY, int chunkZ)
    {
        var chunk = new ChunkEntity(chunkX, chunkY, chunkZ);

        // Only generate ground chunks (y = 0)
        if (chunkY == 0)
        {
            for (int x = 0; x < ChunkEntity.Width; x++)
            {
                for (int z = 0; z < ChunkEntity.Depth; z++)
                {
                    // Bottom layer: Stone
                    chunk[x, 0, z] = new BlockCell(BlockType.Stone);
                    
                    // Middle layers: Dirt
                    for (int y = 1; y < 3; y++)
                    {
                        chunk[x, y, z] = new BlockCell(BlockType.Dirt);
                    }
                    
                    // Top layer: Grass
                    chunk[x, 3, z] = new BlockCell(BlockType.Grass);
                }
            }
        }

        return Task.FromResult(chunk);
    }
}
