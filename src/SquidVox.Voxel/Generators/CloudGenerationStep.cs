using System;
using Microsoft.Xna.Framework;
using SquidVox.Core.Noise;
using SquidVox.Voxel.Interfaces.Generation.Pipeline;

namespace SquidVox.Voxel.Generators;

/// <summary>
/// Identifies cloud volumes for the chunk and registers their bounds within the generation context.
/// </summary>
public class CloudGenerationStep : IGeneratorStep
{
    private const float CoverageFrequency = 0.0016f;
    private const float DensityFrequency = 0.028f;
    private const float CoverageThreshold = 0.68f;
    private const float DensityThreshold = 0.62f;
    private const int CloudBaseHeight = 60;
    private const int CloudThickness = 3;
    private const int TerrainClearance = 8;

    /// <inheritdoc/>
    public string Name => "CloudGenerator";

    /// <inheritdoc/>
    public Task ExecuteAsync(IGeneratorContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var chunkSize = context.ChunkSize();
        var chunkHeight = context.ChunkHeight();
        var worldOrigin = context.GetWorldPosition();
        var chunkBaseY = (int)MathF.Round(worldOrigin.Y);
        var chunkTopY = chunkBaseY + chunkHeight;

        var cloudMinY = CloudBaseHeight;
        var cloudMaxY = CloudBaseHeight + CloudThickness;

        if (chunkTopY <= cloudMinY || chunkBaseY >= cloudMaxY)
        {
            return Task.CompletedTask;
        }

        var terrainHeights = ResolveHeightMap(context, chunkSize);
        var coverageNoise = CreateCoverageNoise(context.Seed);
        var densityNoise = CreateDensityNoise(context.Seed);

        var anyCloudDetected = false;
        var minPlacedY = int.MaxValue;
        var maxPlacedY = int.MinValue;
        var minPlacedX = int.MaxValue;
        var maxPlacedX = int.MinValue;
        var minPlacedZ = int.MaxValue;
        var maxPlacedZ = int.MinValue;

        for (int x = 0; x < chunkSize; x++)
        {
            for (int z = 0; z < chunkSize; z++)
            {
                if (!IsAboveTerrain(terrainHeights, x, z, cloudMinY, terrainClearance: TerrainClearance))
                {
                    continue;
                }

                var worldX = worldOrigin.X + x;
                var worldZ = worldOrigin.Z + z;

                var coverageSample = coverageNoise.GetNoise(worldX, worldZ);
                var coverage = (coverageSample + 1f) * 0.5f;
                if (coverage < CoverageThreshold)
                {
                    continue;
                }

                for (int worldY = cloudMinY; worldY < cloudMaxY; worldY++)
                {
                    if (worldY < chunkBaseY || worldY >= chunkTopY)
                    {
                        continue;
                    }

                    var densitySample = densityNoise.GetNoise(worldX, worldY, worldZ);
                    var density = (densitySample + 1f) * 0.5f;
                    if (density < DensityThreshold)
                    {
                        continue;
                    }

                    anyCloudDetected = true;
                    minPlacedY = Math.Min(minPlacedY, worldY);
                    maxPlacedY = Math.Max(maxPlacedY, worldY);
                    minPlacedX = Math.Min(minPlacedX, x);
                    maxPlacedX = Math.Max(maxPlacedX, x);
                    minPlacedZ = Math.Min(minPlacedZ, z);
                    maxPlacedZ = Math.Max(maxPlacedZ, z);
                    break;
                }
            }
        }

        if (!anyCloudDetected)
        {
            return Task.CompletedTask;
        }

        var cloudAreaPosition = new Vector3(worldOrigin.X + minPlacedX, minPlacedY, worldOrigin.Z + minPlacedZ);
        var cloudAreaSize = new Vector3(
            Math.Max(1, (maxPlacedX - minPlacedX) + 1),
            Math.Max(1, (maxPlacedY - minPlacedY) + 1),
            Math.Max(1, (maxPlacedZ - minPlacedZ) + 1)
        );
        context.AddCloudArea(cloudAreaPosition, cloudAreaSize);

        return Task.CompletedTask;
    }

    private static FastNoiseLite CreateCoverageNoise(int seed)
    {
        var noise = new FastNoiseLite(seed * 17 + 11);
        noise.SetNoiseType(NoiseType.OpenSimplex2S);
        noise.SetFrequency(CoverageFrequency);
        noise.SetFractalType(FractalType.FBm);
        noise.SetFractalOctaves(5);
        noise.SetFractalGain(0.46f);
        noise.SetFractalLacunarity(2.1f);
        return noise;
    }

    private static FastNoiseLite CreateDensityNoise(int seed)
    {
        var noise = new FastNoiseLite(seed * 29 + 23);
        noise.SetNoiseType(NoiseType.OpenSimplex2);
        noise.SetFrequency(DensityFrequency);
        noise.SetFractalType(FractalType.Ridged);
        noise.SetFractalOctaves(4);
        noise.SetFractalGain(0.55f);
        noise.SetFractalLacunarity(2.4f);
        noise.SetRotationType3D(RotationType3D.ImproveXYPlanes);
        return noise;
    }

    private static bool IsAboveTerrain(int[,] heights, int x, int z, int cloudMinY, int terrainClearance)
    {
        if (heights.Length == 0)
        {
            return true;
        }

        var terrainHeight = heights[x, z];
        return terrainHeight + terrainClearance <= cloudMinY;
    }

    private static int[,] ResolveHeightMap(IGeneratorContext context, int chunkSize)
    {
        if (context.CustomData.TryGetValue(GeneratorContextKeys.HeightMap, out var stored)
            && stored is int[,] heightMap)
        {
            return heightMap;
        }

        return new int[chunkSize, chunkSize];
    }
}
