using System;
using System.Collections.Generic;
using SquidVox.Core.Attributes.Debugger;
using SquidVox.Core.Noise;
using SquidVox.Voxel.Interfaces.Generation.Pipeline;
using Microsoft.Xna.Framework;

namespace SquidVox.Voxel.Generators;

/// <summary>
/// Carves caves using the Perlin Worms algorithm (similar to Minecraft).
/// </summary>
[DebuggerHeader("🕳️ Cave Generation Settings")]
public class CaveGeneratorStep : IGeneratorStep
{
    /// <summary>
    /// Gets or sets the number of worm attempts per chunk.
    /// </summary>
    [DebuggerRange(1, 10)]
    [DebuggerField]
    public int WormsPerChunk { get; set; } = 3;

    /// <summary>
    /// Gets or sets the minimum length of a cave worm.
    /// </summary>
    [DebuggerRange(10, 150)]
    [DebuggerField]
    public int MinWormLength { get; set; } = 30;

    /// <summary>
    /// Gets or sets the maximum length of a cave worm.
    /// </summary>
    [DebuggerRange(20, 200)]
    [DebuggerField]
    public int MaxWormLength { get; set; } = 80;

    /// <summary>
    /// Gets or sets the minimum radius of cave tunnels.
    /// </summary>
    [DebuggerRange(0.5, 8.0, 0.1)]
    [DebuggerField]
    public float MinRadius { get; set; } = 1.5f;

    /// <summary>
    /// Gets or sets the maximum radius of cave tunnels.
    /// </summary>
    [DebuggerRange(1.0, 10.0, 0.1)]
    [DebuggerField]
    public float MaxRadius { get; set; } = 3.5f;

    /// <summary>
    /// Gets or sets the minimum height from bedrock where caves can spawn.
    /// </summary>
    [DebuggerRange(1, 30)]
    [DebuggerField]
    public int MinCaveHeight { get; set; } = 8;

    /// <summary>
    /// Gets or sets the probability (0-1) that a worm will spawn.
    /// </summary>
    [DebuggerRange(0.0, 1.0, 0.05)]
    [DebuggerField]
    public double WormSpawnChance { get; set; } = 0.5;

    private readonly struct WormNode
    {
        public WormNode(Vector3 position, float radius)
        {
            Position = position;
            Radius = radius;
        }

        public Vector3 Position { get; }
        public float Radius { get; }
    }

    /// <inheritdoc/>
    public string Name => "CaveGenerator";

    /// <inheritdoc/>
    public Task ExecuteAsync(IGeneratorContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!context.CustomData.TryGetValue(GeneratorContextKeys.HeightMap, out var stored)
            || stored is not int[,] heightMap)
        {
            return Task.CompletedTask;
        }

        var worldBase = context.GetWorldPosition();
        var chunk = context.GetChunk();
        var chunkBaseY = (int)MathF.Round(worldBase.Y);
        var carved = false;

        // Generate worms for this chunk
        var worms = GenerateWorms(context, worldBase);

        // Carve all worms
        foreach (var worm in worms)
        {
            if (CarveWorm(context, chunk, worm, chunkBaseY, heightMap))
            {
                carved = true;
            }
        }

        if (carved)
        {
            chunk.IsLightingDirty = true;
        }

        return Task.CompletedTask;
    }

    private List<List<WormNode>> GenerateWorms(IGeneratorContext context, Vector3 worldBase)
    {
        var worms = new List<List<WormNode>>();
        var chunkSize = context.ChunkSize();
        var baseChunkX = (int)MathF.Floor(worldBase.X / chunkSize);
        var baseChunkZ = (int)MathF.Floor(worldBase.Z / chunkSize);
        var chunkBaseY = (int)MathF.Round(worldBase.Y);

        var noiseX = CreateDirectionNoise(context.Seed, 1000);
        var noiseY = CreateDirectionNoise(context.Seed, 2000);
        var noiseZ = CreateDirectionNoise(context.Seed, 3000);
        var noiseRadius = CreateDirectionNoise(context.Seed, 4000);

        for (int offsetX = -1; offsetX <= 1; offsetX++)
        {
            var candidateChunkX = baseChunkX + offsetX;
            var candidateWorldX = candidateChunkX * chunkSize;

            for (int offsetZ = -1; offsetZ <= 1; offsetZ++)
            {
                var candidateChunkZ = baseChunkZ + offsetZ;
                var candidateWorldZ = candidateChunkZ * chunkSize;

                GenerateChunkWorms(
                    context,
                    candidateWorldX,
                    chunkBaseY,
                    candidateWorldZ,
                    candidateChunkX,
                    candidateChunkZ,
                    noiseX,
                    noiseY,
                    noiseZ,
                    noiseRadius,
                    worms
                );
            }
        }

        return worms;
    }


    private void GenerateChunkWorms(
        IGeneratorContext context,
        float worldX,
        int baseY,
        float worldZ,
        int chunkX,
        int chunkZ,
        FastNoiseLite noiseX,
        FastNoiseLite noiseY,
        FastNoiseLite noiseZ,
        FastNoiseLite noiseRadius,
        List<List<WormNode>> worms
    )
    {
        for (int w = 0; w < WormsPerChunk; w++)
        {
            var hash = HashCoords(chunkX, chunkZ, w, context.Seed);
            var random = new Random(hash);

            if (random.NextDouble() > WormSpawnChance)
            {
                continue;
            }

            var length = random.Next(MinWormLength, MaxWormLength);
            var startX = worldX + (float)random.NextDouble() * context.ChunkSize();
            var startY = CalculateStartHeight(random, baseY, context.ChunkHeight());
            var startZ = worldZ + (float)random.NextDouble() * context.ChunkSize();
            var position = new Vector3(startX, startY, startZ);

            var worm = new List<WormNode>();
            var direction = new Vector3(
                (float)(random.NextDouble() * 2.0 - 1.0),
                (float)(random.NextDouble() * 2.0 - 1.0),
                (float)(random.NextDouble() * 2.0 - 1.0)
            );
            direction.Normalize();

            for (int i = 0; i < length; i++)
            {
                var noiseScale = 0.1f;
                var nxValue = noiseX.GetNoise(position.X * noiseScale, position.Y * noiseScale, position.Z * noiseScale);
                var nyValue = noiseY.GetNoise(position.X * noiseScale, position.Y * noiseScale, position.Z * noiseScale);
                var nzValue = noiseZ.GetNoise(position.X * noiseScale, position.Y * noiseScale, position.Z * noiseScale);

                direction.X += nxValue * 0.3f;
                direction.Y += nyValue * 0.2f; // Less vertical movement
                direction.Z += nzValue * 0.3f;
                direction.Normalize();

                var radiusNoise = noiseRadius.GetNoise(position.X * 0.05f, position.Y * 0.05f, position.Z * 0.05f);
                var radius = MathHelper.Lerp(MinRadius, MaxRadius, (radiusNoise + 1f) * 0.5f);

                worm.Add(new WormNode(position, radius));

                position += direction;
            }

            if (worm.Count > 0)
            {
                worms.Add(worm);
            }
        }

    }

    private float CalculateStartHeight(Random random, int baseY, int chunkHeight)
    {
        var minY = baseY + MinCaveHeight;
        var maxY = baseY + chunkHeight - MinCaveHeight;
        if (maxY <= minY)
        {
            return minY;
        }

        var span = maxY - minY;
        return minY + (float)random.NextDouble() * span;
    }

    private bool CarveWorm(IGeneratorContext context, Primitives.ChunkEntity chunk, List<WormNode> worm, int chunkBaseY, int[,] heightMap)
    {
        var carved = false;
        var worldBase = context.GetWorldPosition();
        var size = context.ChunkSize();
        var height = context.ChunkHeight();

        foreach (var node in worm)
        {
            var radius = node.Radius;
            var radiusSq = radius * radius;

            // Calculate bounding box
            var minX = (int)MathF.Floor(node.Position.X - radius);
            var maxX = (int)MathF.Ceiling(node.Position.X + radius);
            var minY = (int)MathF.Floor(node.Position.Y - radius);
            var maxY = (int)MathF.Ceiling(node.Position.Y + radius);
            var minZ = (int)MathF.Floor(node.Position.Z - radius);
            var maxZ = (int)MathF.Ceiling(node.Position.Z + radius);

            // Carve sphere
            for (int worldX = minX; worldX <= maxX; worldX++)
            {
                for (int worldY = minY; worldY <= maxY; worldY++)
                {
                    for (int worldZ = minZ; worldZ <= maxZ; worldZ++)
                    {
                        // Check if this block is in this chunk
                        var localX = worldX - (int)worldBase.X;
                        var localY = worldY - chunkBaseY;
                        var localZ = worldZ - (int)worldBase.Z;

                        if (localX < 0 || localX >= size || localY < 0 || localY >= height || localZ < 0 || localZ >= size)
                        {
                            continue;
                        }

                        // Check if within sphere
                        var dx = worldX - node.Position.X;
                        var dy = worldY - node.Position.Y;
                        var dz = worldZ - node.Position.Z;
                        var distSq = dx * dx + dy * dy + dz * dz;

                        if (distSq <= radiusSq)
                        {
                            // Don't carve too close to surface
                            var surfaceHeight = heightMap[localX, localZ];
                            if (worldY >= surfaceHeight - 3)
                            {
                                continue;
                            }

                            // Don't carve bedrock
                            if (worldY <= 0)
                            {
                                continue;
                            }

                            context.SetBlock(localX, localY, localZ, null);
                            carved = true;
                        }
                    }
                }
            }
        }

        return carved;
    }

    private static FastNoiseLite CreateDirectionNoise(int seed, int offset)
    {
        var noise = new FastNoiseLite(seed + offset);
        noise.SetNoiseType(NoiseType.OpenSimplex2);
        noise.SetFrequency(0.05f);
        noise.SetFractalType(FractalType.FBm);
        noise.SetFractalOctaves(2);
        return noise;
    }

    private static int HashCoords(int x, int z, int index, int seed)
    {
        unchecked
        {
            int hash = seed;
            hash = hash * 31 + x;
            hash = hash * 31 + z;
            hash = hash * 31 + index;
            return hash;
        }
    }
}
