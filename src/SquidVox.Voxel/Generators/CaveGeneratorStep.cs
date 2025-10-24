using System;
using System.Collections.Generic;
using SquidVox.Core.Noise;
using SquidVox.Voxel.Interfaces.Generation.Pipeline;
using Microsoft.Xna.Framework;

namespace SquidVox.Voxel.Generators;

/// <summary>
/// Carves caves using the Perlin Worms algorithm (similar to Minecraft).
/// </summary>
public class CaveGeneratorStep : IGeneratorStep
{
    private const int WormsPerChunk = 3;
    private const int MinWormLength = 30;
    private const int MaxWormLength = 80;
    private const float MinRadius = 1.5f;
    private const float MaxRadius = 3.5f;
    private const int MinCaveHeight = 8;
    private const double WormSpawnChance = 0.5;

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

    private static List<List<WormNode>> GenerateWorms(IGeneratorContext context, Vector3 worldBase)
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


    private static void GenerateChunkWorms(
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

    private static float CalculateStartHeight(Random random, int baseY, int chunkHeight)
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

    private static bool CarveWorm(IGeneratorContext context, Primitives.ChunkEntity chunk, List<WormNode> worm, int chunkBaseY, int[,] heightMap)
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
