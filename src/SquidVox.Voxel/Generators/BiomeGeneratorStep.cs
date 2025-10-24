using System;
using SquidVox.Core.Noise;
using SquidVox.Voxel.Interfaces.Generation.Pipeline;
using SquidVox.Voxel.Types;

namespace SquidVox.Voxel.Generators;

/// <summary>
/// Determines biome assignments for the chunk columns.
/// </summary>
public class BiomeGeneratorStep : IGeneratorStep
{
    private const float BiomeFrequency = 0.0012f;

    private static BiomeType SelectBiome(float noiseValue)
    {
        if (noiseValue < -0.35f)
        {
            return BiomeType.Ocean;
        }

        if (noiseValue < 0.1f)
        {
            return BiomeType.Plains;
        }

        if (noiseValue < 0.35f)
        {
            return BiomeType.Forest;
        }

        return BiomeType.Mountains;
    }

    /// <inheritdoc/>
    public string Name => "BiomeGenerator";

    /// <inheritdoc/>
    public Task ExecuteAsync(IGeneratorContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var size = context.ChunkSize();
        var biomeMap = new BiomeType[size, size];
        var noise = CreateNoise(context.Seed);
        var worldBase = context.GetWorldPosition();

        for (int x = 0; x < size; x++)
        {
            for (int z = 0; z < size; z++)
            {
                var worldX = worldBase.X + x;
                var worldZ = worldBase.Z + z;
                var value = noise.GetNoise(worldX, worldZ);
                biomeMap[x, z] = SelectBiome(value);
            }
        }

        context.CustomData[GeneratorContextKeys.BiomeMap] = biomeMap;

        return Task.CompletedTask;
    }


    private static FastNoiseLite CreateNoise(int seed)
    {
        var noise = new FastNoiseLite(seed);
        noise.SetNoiseType(NoiseType.OpenSimplex2S);
        noise.SetFrequency(BiomeFrequency);
        noise.SetFractalType(FractalType.FBm);
        noise.SetFractalOctaves(3);
        return noise;
    }
}
