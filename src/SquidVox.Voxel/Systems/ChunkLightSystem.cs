using Serilog;
using SquidVox.Voxel.Primitives;
using SquidVox.Voxel.Types;

namespace SquidVox.Voxel.Systems;

public class ChunkLightSystem
{
    private readonly ILogger _logger = Log.ForContext<ChunkLightSystem>();

    private const byte MaxLightLevel = 15;

    /// <summary>
    /// Minimum ambient light level - set to 0 for true darkness in caves.
    /// Only air blocks in fully shadowed areas receive 0 light.
    /// </summary>
    public byte MinAmbientLight { get; set; } = 0;

    // Cache for block opacity to avoid repeated BlockManagerService calls
    // Opacity: 0 = transparent (air), 1-14 = semi-transparent (gradual), 15 = fully opaque
    private static readonly Dictionary<BlockType, byte> BlockOpacity = new()
    {
        { BlockType.Air, 0 },           // No opacity - light passes fully
        { BlockType.Stone, 15 },        // Fully opaque
        { BlockType.Dirt, 15 },         // Fully opaque
        { BlockType.Grass, 15 },        // Fully opaque
        { BlockType.Bedrock, 15 },      // Fully opaque
        { BlockType.Snow, 3 },          // Semi-transparent (lets some light through)
        { BlockType.Wood, 15 },         // Fully opaque
        { BlockType.Leaves, 2 },        // Very transparent (light passes mostly through)
        { BlockType.Water, 4 },         // Semi-transparent (water absorbs some light)
        { BlockType.TallGrass, 1 },     // Very transparent
        { BlockType.Flower, 1 },        // Very transparent
    };

    // Light sources and their emission levels
    private static readonly Dictionary<BlockType, byte> LightSources = new()
    {
        { BlockType.Water, 8 }, // Water glows
    };

    // Delegate to get neighboring chunks for cross-chunk lighting
    public delegate ChunkEntity? GetNeighborChunkHandler(int chunkX, int chunkZ);

    /// <summary>
    /// Calculate initial sunlight for a single chunk.
    /// For proper cross-chunk lighting, use CalculateCrossChunkLighting instead.
    /// </summary>
    public void CalculateInitialSunlight(ChunkEntity chunk)
    {
        // Lazy lighting: only recalculate if dirty
        if (!chunk.IsLightingDirty)
        {
            return;
        }

        var lightLevels = new byte[ChunkEntity.Size * ChunkEntity.Size * ChunkEntity.Height];

        // Initialize all blocks to 0 light
        Array.Fill(lightLevels, (byte)0);

        // Phase 1: Sunlight propagation from top
        CalculateSunlight(chunk, lightLevels);

        // Phase 2: Block light sources
        CalculateBlockLights(chunk, lightLevels);

        chunk.SetLightLevels(lightLevels);
        chunk.IsLightingDirty = false; // Mark as clean

        _logger.Debug("Calculated lighting for chunk at {Position}", chunk.Position);
    }

    /// <summary>
    /// Invalidates the lighting for a chunk, marking it as dirty so it will be recalculated on next access.
    /// Call this when blocks in the chunk are modified.
    /// </summary>
    public void InvalidateLighting(ChunkEntity chunk)
    {
        chunk.IsLightingDirty = true;
        _logger.Debug("Invalidated lighting for chunk at {Position}", chunk.Position);
    }

    /// <summary>
    /// Calculate lighting for multiple chunks with proper cross-chunk propagation.
    /// This ensures sunlight and block lights propagate correctly across chunk boundaries.
    /// </summary>
    public void CalculateCrossChunkLighting(IEnumerable<ChunkEntity> chunks, GetNeighborChunkHandler getNeighborChunk)
    {
        var chunkList = chunks.ToList();

        if (chunkList.Count == 0)
            return;

        // Create a map of chunk positions to chunks for easy lookup
        var chunkMap = chunkList.ToDictionary(
            c => ((int)(c.Position.X / ChunkEntity.Size), (int)(c.Position.Z / ChunkEntity.Size)),
            c => c
        );

        // Initialize all light levels to 0
        foreach (var chunk in chunkList)
        {
            var lightLevels = new byte[ChunkEntity.Size * ChunkEntity.Size * ChunkEntity.Height];
            Array.Fill(lightLevels, (byte)0);
            chunk.SetLightLevels(lightLevels);
        }

        // Phase 1: Calculate sunlight for all chunks independently first
        foreach (var chunk in chunkList)
        {
            CalculateSunlight(chunk, chunk.LightLevels);
        }

        // Phase 2: Propagate sunlight across chunk boundaries bidirectionally
        PropagateAllSunlight(chunkList, chunkMap);

        // Phase 3: Calculate block light sources for all chunks
        foreach (var chunk in chunkList)
        {
            CalculateBlockLights(chunk, chunk.LightLevels);
        }

        // Phase 4: Propagate block lights across chunk boundaries with multi-chunk support
        PropagateAllBlockLights(chunkList, chunkMap);

        _logger.Debug("Calculated cross-chunk lighting for {Count} chunks", chunkList.Count);
    }

    /// <summary>
    /// Propagate sunlight bidirectionally between all chunk pairs.
    /// </summary>
    private void PropagateAllSunlight(List<ChunkEntity> chunks, Dictionary<(int, int), ChunkEntity> chunkMap)
    {
        // Process each chunk and propagate to neighbors
        foreach (var chunk in chunks)
        {
            var chunkX = (int)(chunk.Position.X / ChunkEntity.Size);
            var chunkZ = (int)(chunk.Position.Z / ChunkEntity.Size);

            // Check all 4 neighbors
            var neighbors = new[] { (0, 1), (0, -1), (1, 0), (-1, 0) };

            foreach (var (dx, dz) in neighbors)
            {
                var neighborPos = (chunkX + dx, chunkZ + dz);

                if (chunkMap.TryGetValue(neighborPos, out var neighborChunk))
                {
                    // Bidirectional propagation: from chunk to neighbor AND neighbor to chunk
                    PropagateLightAcrossBoundary(chunk, neighborChunk, dx, dz);
                    PropagateLightAcrossBoundary(neighborChunk, chunk, -dx, -dz);
                }
            }
        }
    }

    /// <summary>
    /// Propagate block lights across all chunk boundaries.
    /// </summary>
    private void PropagateAllBlockLights(List<ChunkEntity> chunks, Dictionary<(int, int), ChunkEntity> chunkMap)
    {
        foreach (var chunk in chunks)
        {
            var chunkX = (int)(chunk.Position.X / ChunkEntity.Size);
            var chunkZ = (int)(chunk.Position.Z / ChunkEntity.Size);

            var neighbors = new[] { (0, 1), (0, -1), (1, 0), (-1, 0) };

            foreach (var (dx, dz) in neighbors)
            {
                var neighborPos = (chunkX + dx, chunkZ + dz);

                if (chunkMap.TryGetValue(neighborPos, out var neighborChunk))
                {
                    // Bidirectional block light propagation
                    PropagateLightAcrossBoundary(chunk, neighborChunk, dx, dz);
                    PropagateLightAcrossBoundary(neighborChunk, chunk, -dx, -dz);
                }
            }
        }
    }

    /// <summary>
    /// Propagate light from one chunk to an adjacent chunk across the boundary.
    /// Uses bidirectional approach: takes the maximum light from both sides.
    /// </summary>
    private void PropagateLightAcrossBoundary(ChunkEntity fromChunk, ChunkEntity toChunk, int dx, int dz)
    {
        // Determine which faces are adjacent
        int fromStartX, fromEndX, fromStartZ, fromEndZ;
        int toStartX, toEndX, toStartZ, toEndZ;

        if (dx == 1) // fromChunk is west of toChunk
        {
            fromStartX = ChunkEntity.Size - 1;
            fromEndX = ChunkEntity.Size - 1;
            toStartX = 0;
            toEndX = 0;
            fromStartZ = 0;
            fromEndZ = ChunkEntity.Size - 1;
            toStartZ = 0;
            toEndZ = ChunkEntity.Size - 1;
        }
        else if (dx == -1) // fromChunk is east of toChunk
        {
            fromStartX = 0;
            fromEndX = 0;
            toStartX = ChunkEntity.Size - 1;
            toEndX = ChunkEntity.Size - 1;
            fromStartZ = 0;
            fromEndZ = ChunkEntity.Size - 1;
            toStartZ = 0;
            toEndZ = ChunkEntity.Size - 1;
        }
        else if (dz == 1) // fromChunk is north of toChunk
        {
            fromStartZ = ChunkEntity.Size - 1;
            fromEndZ = ChunkEntity.Size - 1;
            toStartZ = 0;
            toEndZ = 0;
            fromStartX = 0;
            fromEndX = ChunkEntity.Size - 1;
            toStartX = 0;
            toEndX = ChunkEntity.Size - 1;
        }
        else // dz == -1, fromChunk is south of toChunk
        {
            fromStartZ = 0;
            fromEndZ = 0;
            toStartZ = ChunkEntity.Size - 1;
            toEndZ = ChunkEntity.Size - 1;
            fromStartX = 0;
            fromEndX = ChunkEntity.Size - 1;
            toStartX = 0;
            toEndX = ChunkEntity.Size - 1;
        }

        var fromLightLevels = fromChunk.LightLevels;
        var toLightLevels = toChunk.LightLevels;

        // Propagate light from boundary blocks with opacity consideration
        for (int x = fromStartX; x <= fromEndX; x++)
        {
            for (int z = fromStartZ; z <= fromEndZ; z++)
            {
                for (int y = 0; y < ChunkEntity.Height; y++)
                {
                    var fromIndex = ChunkEntity.GetIndex(x, y, z);
                    var fromLight = fromLightLevels[fromIndex];

                    if (fromLight > 0)
                    {
                        var toX = dx == 0 ? x : (dx == 1 ? toStartX : toEndX);
                        var toZ = dz == 0 ? z : (dz == 1 ? toStartZ : toEndZ);
                        var toIndex = ChunkEntity.GetIndex(toX, y, toZ);

                        // Get the target block to calculate light reduction
                        var toBlock = toChunk.GetBlock(toX, y, toZ);
                        byte opacity = BlockOpacity.TryGetValue(toBlock.BlockType, out var op) ? op : (byte)15;

                        // Light reduces by opacity amount (0-15)
                        // Fully opaque (15) blocks get light reduced to 0
                        // Transparent (0) blocks get same light as source
                        var propagatedLight = (byte)Math.Max(0, fromLight - opacity);

                        // Take the maximum to handle bidirectional propagation
                        toLightLevels[toIndex] = Math.Max(toLightLevels[toIndex], propagatedLight);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Calculate sunlight propagation from top to bottom.
    /// Only air and transparent blocks receive sunlight.
    /// Solid blocks block light but don't get illuminated.
    /// </summary>
    private void CalculateSunlight(ChunkEntity chunk, byte[] lightLevels)
    {
        // Sunlight comes from the top (Y = Height - 1) with full intensity
        for (int x = 0; x < ChunkEntity.Size; x++)
        {
            for (int z = 0; z < ChunkEntity.Size; z++)
            {
                byte currentLight = MaxLightLevel; // Start with full sunlight

                for (int y = ChunkEntity.Height - 1; y >= 0; y--)
                {
                    var index = ChunkEntity.GetIndex(x, y, z);
                    var block = chunk.Blocks[index];

                    // Get opacity for this block type
                    byte opacity = BlockOpacity.TryGetValue(block.BlockType, out var op) ? op : (byte)15;

                    if (block.BlockType == BlockType.Air)
                    {
                        // Air blocks receive full light and propagate downward
                        lightLevels[index] = Math.Max(lightLevels[index], currentLight);
                    }
                    else if (opacity < 15)
                    {
                        // Semi-transparent blocks (like leaves, water) get light reduced by their opacity
                        byte reducedLight = (byte)Math.Max(0, currentLight - opacity);
                        lightLevels[index] = Math.Max(lightLevels[index], reducedLight);
                        currentLight = reducedLight;
                    }
                    else
                    {
                        // Fully opaque blocks (stone, dirt, wood) DON'T get sunlight
                        // But they block light for blocks below
                        // Only internal block faces in mesh will use block light sources
                        currentLight = 0; // Sunlight blocked completely
                    }

                    // If we've hit minimum light, no point continuing down
                    if (currentLight <= MinAmbientLight)
                        break;
                }
            }
        }
    }

    /// <summary>
    /// Calculate light from block light sources (torches, glowing blocks, etc.)
    /// These lights propagate in all directions using a priority queue flood-fill.
    /// </summary>
    private void CalculateBlockLights(ChunkEntity chunk, byte[] lightLevels)
    {
        // Find all light sources and propagate their light
        for (int x = 0; x < ChunkEntity.Size; x++)
        {
            for (int y = 0; y < ChunkEntity.Height; y++)
            {
                for (int z = 0; z < ChunkEntity.Size; z++)
                {
                    var index = ChunkEntity.GetIndex(x, y, z);
                    var block = chunk.Blocks[index];

                    if (LightSources.TryGetValue(block.BlockType, out var emissionLevel))
                    {
                        // This block emits light - propagate it
                        PropagateLightFromSource(chunk, lightLevels, x, y, z, emissionLevel);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Propagate light from a single source block in all directions using flood-fill.
    /// Light reduces based on block opacity as it travels.
    /// This is chunk-local only; cross-chunk propagation happens separately.
    /// </summary>
    private void PropagateLightFromSource(
        ChunkEntity chunk, byte[] lightLevels, int startX, int startY, int startZ, byte startLight
    )
    {
        var queue = new PriorityQueue<(int x, int y, int z, byte light), int>();
        var visited = new HashSet<(int, int, int)>();

        queue.Enqueue((startX, startY, startZ, startLight), -startLight); // Higher light = higher priority
        visited.Add((startX, startY, startZ));

        // Set the source block light
        var sourceIndex = ChunkEntity.GetIndex(startX, startY, startZ);
        lightLevels[sourceIndex] = Math.Max(lightLevels[sourceIndex], startLight);

        while (queue.Count > 0)
        {
            var (x, y, z, light) = queue.Dequeue();

            if (light <= 0) continue; // No more light to propagate

            // Check all 6 neighbors (up, down, left, right, forward, backward)
            var neighbors = new[]
            {
                (x + 1, y, z), (x - 1, y, z),
                (x, y + 1, z), (x, y - 1, z),
                (x, y, z + 1), (x, y, z - 1)
            };

            foreach (var (nx, ny, nz) in neighbors)
            {
                if (!chunk.IsInBounds(nx, ny, nz) || visited.Contains((nx, ny, nz)))
                    continue;

                var neighborIndex = ChunkEntity.GetIndex(nx, ny, nz);
                var neighborBlock = chunk.Blocks[neighborIndex];

                // Calculate light reduction based on block opacity
                byte opacity = BlockOpacity.TryGetValue(neighborBlock.BlockType, out var op) ? op : (byte)15;

                // Light reduces by opacity (0-15)
                var newLight = (byte)Math.Max(0, light - opacity);

                // Only propagate if this light is stronger than what's already there
                if (newLight > lightLevels[neighborIndex])
                {
                    lightLevels[neighborIndex] = newLight;
                    visited.Add((nx, ny, nz));
                    queue.Enqueue((nx, ny, nz, newLight), -newLight);
                }
            }
        }
    }
}
