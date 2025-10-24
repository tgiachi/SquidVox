using System;
using SquidVox.Core.Noise;
using SquidVox.Voxel.Interfaces.Generation.Pipeline;
using SquidVox.Voxel.Types;

namespace SquidVox.Voxel.Generators;

/// <summary>
/// Places decorative features like flowers and tall grass on the terrain surface.
/// </summary>
public class FeatureGeneratorStep : IGeneratorStep
{
    private const float FeatureFrequency = 0.3f;
    private const float FlowerThreshold = 0.65f;
    private const float TallGrassThreshold = 0.45f;

    private readonly struct FeatureSettings
    {
        public FeatureSettings(float flowerDensity, float grassDensity)
        {
            FlowerDensity = flowerDensity;
            GrassDensity = grassDensity;
        }

        public float FlowerDensity { get; }
        public float GrassDensity { get; }
    }

    private static FeatureSettings GetFeatureSettings(BiomeType biome)
    {
        return biome switch
        {
            BiomeType.Plains => new FeatureSettings(0.4f, 0.8f),
            BiomeType.Forest => new FeatureSettings(0.3f, 0.6f),
            BiomeType.Mountains => new FeatureSettings(0.1f, 0.2f),
            _ => new FeatureSettings(0f, 0f),
        };
    }

    private static FastNoiseLite CreateFeatureNoise(int seed)
    {
        var noise = new FastNoiseLite(seed + 12345); // Offset seed for feature variation
        noise.SetNoiseType(NoiseType.OpenSimplex2);
        noise.SetFrequency(FeatureFrequency);
        return noise;
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

    private static bool ShouldPlaceFeature(float noiseValue, float threshold, float density)
    {
        // Normalize noise value to 0-1 range
        var normalized = (noiseValue + 1f) * 0.5f;
        return normalized > threshold * (1f - density);
    }

    private static BlockType? SelectFeature(float noiseValue, FeatureSettings settings)
    {
        // Check flower first (rarer)
        if (ShouldPlaceFeature(noiseValue, FlowerThreshold, settings.FlowerDensity))
        {
            return BlockType.Flower;
        }

        // Then check tall grass
        if (ShouldPlaceFeature(noiseValue, TallGrassThreshold, settings.GrassDensity))
        {
            return BlockType.TallGrass;
        }

        return null;
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
        var noise = CreateFeatureNoise(context.Seed);

        for (int x = 0; x < size; x++)
        {
            for (int z = 0; z < size; z++)
            {
                var biome = biomeMap[x, z];
                var settings = GetFeatureSettings(biome);

                // Skip if this biome doesn't have features
                if (settings.FlowerDensity == 0f && settings.GrassDensity == 0f)
                {
                    continue;
                }

                var surfaceHeight = heightMap[x, z];
                var localY = surfaceHeight - chunkBaseY;

                // Only place features if the surface is within this chunk
                if (localY < 0 || localY >= context.ChunkHeight())
                {
                    continue;
                }

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

                // Use world coordinates for noise to ensure consistency across chunks
                var worldX = worldBase.X + x;
                var worldZ = worldBase.Z + z;
                var noiseValue = noise.GetNoise(worldX, worldZ);

                var featureType = SelectFeature(noiseValue, settings);
                if (featureType.HasValue)
                {
                    var featureBlock = context.NewBlockEntity(featureType.Value);
                    context.SetBlock(x, featureY, z, featureBlock);
                }
            }
        }

        return Task.CompletedTask;
    }
}
