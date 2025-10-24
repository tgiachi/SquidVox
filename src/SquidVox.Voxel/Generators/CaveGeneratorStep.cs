using System;
using SquidVox.Core.Noise;
using SquidVox.Voxel.Interfaces.Generation.Pipeline;

namespace SquidVox.Voxel.Generators;

/// <summary>
/// Carves subterranean cavities within generated terrain.
/// </summary>
public class CaveGeneratorStep : IGeneratorStep
{
    private const int CaveStartDepth = 8;
    private const float CaveThreshold = 0.6f;
    private const float VerticalAttenuation = 0.35f;

    private static FastNoiseLite CreateCaveNoise(int seed)
    {
        var noise = new FastNoiseLite(seed + 1234);
        noise.SetNoiseType(NoiseType.OpenSimplex2);
        noise.SetFrequency(0.02f);
        noise.SetFractalType(FractalType.FBm);
        noise.SetFractalOctaves(4);
        noise.SetFractalGain(0.5f);
        noise.SetFractalLacunarity(2.0f);
        return noise;
    }

    private static FastNoiseLite CreateRegionNoise(int seed)
    {
        var noise = new FastNoiseLite(seed + 5678);
        noise.SetNoiseType(NoiseType.OpenSimplex2);
        noise.SetFrequency(0.008f);
        noise.SetFractalType(FractalType.FBm);
        noise.SetFractalOctaves(3);
        noise.SetFractalGain(0.5f);
        noise.SetFractalLacunarity(2.0f);
        return noise;
    }

    private static FastNoiseLite CreateDetailNoise(int seed)
    {
        var noise = new FastNoiseLite(seed + 9012);
        noise.SetNoiseType(NoiseType.OpenSimplex2);
        noise.SetFrequency(0.05f);
        noise.SetFractalType(FractalType.FBm);
        noise.SetFractalOctaves(2);
        noise.SetFractalGain(0.5f);
        noise.SetFractalLacunarity(2.0f);
        return noise;
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

        var size = context.ChunkSize();
        var worldBase = context.GetWorldPosition();
        var caveNoise = CreateCaveNoise(context.Seed);
        var regionNoise = CreateRegionNoise(context.Seed);
        var detailNoise = CreateDetailNoise(context.Seed);
        var chunk = context.GetChunk();
        var chunkBaseY = (int)MathF.Round(worldBase.Y);
        var chunkTopY = chunkBaseY + context.ChunkHeight();
        var carved = false;

        for (int x = 0; x < size; x++)
        {
            for (int z = 0; z < size; z++)
            {
                var columnHeight = heightMap[x, z];
                var startY = Math.Max(CaveStartDepth, chunkBaseY);
                var endY = Math.Min(columnHeight - 3, chunkTopY);

                if (startY >= endY)
                {
                    continue;
                }

                var worldX = worldBase.X + x;
                var worldZ = worldBase.Z + z;

                for (int worldY = startY; worldY < endY; worldY++)
                {
                    // Calculate vertical attenuation (caves less likely near surface and bottom)
                    var depthFromSurface = columnHeight - worldY;
                    var normalizedDepth = (float)depthFromSurface / (columnHeight - startY);
                    var verticalFactor = 1.0f - (MathF.Abs(normalizedDepth - 0.5f) * 2.0f * VerticalAttenuation);

                    // Check if this region should have caves
                    var regionValue = regionNoise.GetNoise(worldX, worldY, worldZ);
                    if (regionValue < -0.3f)
                    {
                        continue;
                    }

                    // Get cave noise value
                    var caveValue = caveNoise.GetNoise(worldX, worldY, worldZ);

                    // Add detail
                    var detailValue = detailNoise.GetNoise(worldX, worldY, worldZ) * 0.2f;

                    // Combine values
                    var finalValue = (caveValue + detailValue) * verticalFactor;

                    // Normalize to 0-1 range
                    var normalizedValue = (finalValue + 1f) * 0.5f;

                    // Carve if above threshold
                    if (normalizedValue > CaveThreshold)
                    {
                        var localY = worldY - chunkBaseY;
                        if (localY >= 0 && localY < context.ChunkHeight())
                        {
                            context.SetBlock(x, localY, z, null);
                            carved = true;
                        }
                    }
                }
            }
        }

        if (carved)
        {
            chunk.IsLightingDirty = true;
        }

        return Task.CompletedTask;
    }
}
