using System;
using SquidVox.Voxel.Interfaces.Generation.Pipeline;
using SquidVox.Voxel.Types;

namespace SquidVox.Voxel.Generators;

/// <summary>
/// Places decorative features like flowers and tall grass on the terrain surface.
/// </summary>
public class FeatureGeneratorStep : IGeneratorStep
{
    private readonly struct FeatureSettings
    {
        public FeatureSettings(float flowerChance, float grassChance)
        {
            FlowerChance = flowerChance;
            GrassChance = grassChance;
        }

        public float FlowerChance { get; }
        public float GrassChance { get; }
    }

    private static FeatureSettings GetFeatureSettings(BiomeType biome)
    {
        return biome switch
        {
            BiomeType.Plains => new FeatureSettings(0.02f, 0.08f),
            BiomeType.Forest => new FeatureSettings(0.015f, 0.06f),
            BiomeType.Mountains => new FeatureSettings(0.005f, 0.02f),
            _ => new FeatureSettings(0f, 0f),
        };
    }

    private static BiomeType[,] ResolveBiomeMap(IGeneratorContext context)
    {
        if (context.CustomData.TryGetValue(GeneratorContextKeys.BiomeMap, out var stored)
            && stored is BiomeType[,] biomeMap)
        {
            return biomeMap;
        }

        var fallback = new BiomeType[context.ChunkSize(), context.ChunkSize()];
        for (int x = 0; x < fallback.GetLength(0); x++)
        {
            for (int z = 0; z < fallback.GetLength(1); z++)
            {
                fallback[x, z] = BiomeType.Plains;
            }
        }

        return fallback;
    }

    private static int[,] ResolveHeightMap(IGeneratorContext context)
    {
        if (context.CustomData.TryGetValue(GeneratorContextKeys.HeightMap, out var stored)
            && stored is int[,] heightMap)
        {
            return heightMap;
        }

        var fallback = new int[context.ChunkSize(), context.ChunkSize()];
        for (int x = 0; x < fallback.GetLength(0); x++)
        {
            for (int z = 0; z < fallback.GetLength(1); z++)
            {
                fallback[x, z] = 32; // Default height
            }
        }

        return fallback;
    }

    private static int HashPosition(int x, int z, int seed)
    {
        // Simple hash function for deterministic random
        unchecked
        {
            int hash = seed;
            hash = hash * 31 + x;
            hash = hash * 31 + z;
            return hash;
        }
    }

    /// <inheritdoc/>
    public string Name => "FeatureGenerator";

    /// <inheritdoc/>
    public Task ExecuteAsync(IGeneratorContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var chunk = context.GetChunk();
        var size = context.ChunkSize();
        var worldBase = context.GetWorldPosition();
        var chunkBaseY = (int)MathF.Round(worldBase.Y);
        var biomeMap = ResolveBiomeMap(context);
        var heightMap = ResolveHeightMap(context);

        for (int x = 0; x < size; x++)
        {
            for (int z = 0; z < size; z++)
            {
                var biome = biomeMap[x, z];
                var settings = GetFeatureSettings(biome);

                // Skip if this biome doesn't have features
                if (settings.FlowerChance == 0f && settings.GrassChance == 0f)
                {
                    continue;
                }

                var surfaceWorldY = heightMap[x, z] - 1;
                if (surfaceWorldY < chunkBaseY || surfaceWorldY >= chunkBaseY + context.ChunkHeight())
                {
                    continue;
                }

                var localY = surfaceWorldY - chunkBaseY;

                // Check if the surface block is grass (suitable for features)
                var surfaceBlock = chunk.GetBlock(x, localY, z);
                if (surfaceBlock.BlockType != BlockType.Grass)
                {
                    continue;
                }

                // Check if the block above is air
                var featureY = localY + 1;
                if (featureY >= context.ChunkHeight())
                {
                    continue;
                }

                var aboveBlock = chunk.GetBlock(x, featureY, z);
                if (aboveBlock.BlockType != BlockType.Air)
                {
                    continue;
                }

                // Use world coordinates for deterministic randomness
                var worldX = (int)worldBase.X + x;
                var worldZ = (int)worldBase.Z + z;
                var hash = HashPosition(worldX, worldZ, context.Seed);
                var random = new Random(hash);
                var chance = random.NextDouble();
                var density = (context.GetNoise().GetNoise(worldX * 0.05f, worldZ * 0.05f) + 1f) * 0.5f;

                // Try to place flower first (rarer)
                if (chance < settings.FlowerChance)
                {
                    var featureBlock = context.NewBlockEntity(BlockType.Flower);
                    context.SetBlock(x, featureY, z, featureBlock);
                }
                // Otherwise try tall grass
                else if (density > 0.65f && chance < settings.FlowerChance + settings.GrassChance)
                {
                    var featureBlock = context.NewBlockEntity(BlockType.TallGrass);
                    context.SetBlock(x, featureY, z, featureBlock);
                }
            }
        }

        return Task.CompletedTask;
    }
}
