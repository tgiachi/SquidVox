using System;
using SquidVox.Core.Noise;
using SquidVox.Voxel.Interfaces.Generation.Pipeline;
using SquidVox.Voxel.Primitives;
using SquidVox.Voxel.Types;

namespace SquidVox.Voxel.Generators;

/// <summary>
/// Builds the solid terrain volume for a chunk based on biome data.
/// </summary>
public class TerrainGeneratorStep : IGeneratorStep
{
    private const float TerrainFrequency = 0.01f;

    private readonly struct TerrainSettings
    {
        public TerrainSettings(int baseHeight, int variation, int waterLevel, BlockType surfaceBlock, BlockType subSurfaceBlock, BlockType deepBlock)
        {
            BaseHeight = baseHeight;
            Variation = variation;
            WaterLevel = waterLevel;
            SurfaceBlock = surfaceBlock;
            SubSurfaceBlock = subSurfaceBlock;
            DeepBlock = deepBlock;
        }

        public int BaseHeight { get; }

        public int Variation { get; }

        public int WaterLevel { get; }

        public BlockType SurfaceBlock { get; }

        public BlockType SubSurfaceBlock { get; }

        public BlockType DeepBlock { get; }
    }

    private static TerrainSettings GetTerrainSettings(BiomeType biome)
    {
        return biome switch
        {
            BiomeType.Ocean => new TerrainSettings(18, 6, 28, BlockType.Sand, BlockType.Sand, BlockType.Stone),
            BiomeType.Forest => new TerrainSettings(32, 12, 0, BlockType.Grass, BlockType.Dirt, BlockType.Stone),
            BiomeType.Mountains => new TerrainSettings(40, 20, 0, BlockType.Stone, BlockType.Stone, BlockType.Stone),
            _ => new TerrainSettings(30, 10, 0, BlockType.Grass, BlockType.Dirt, BlockType.Stone),
        };
    }

    private static FastNoiseLite CreateTerrainNoise(int seed)
    {
        var noise = new FastNoiseLite(seed);
        noise.SetNoiseType(NoiseType.OpenSimplex2);
        noise.SetFrequency(TerrainFrequency);
        noise.SetFractalType(FractalType.FBm);
        noise.SetFractalOctaves(5);
        noise.SetFractalGain(0.45f);
        noise.SetFractalLacunarity(2.1f);
        return noise;
    }

    private static int CalculateColumnHeight(TerrainSettings settings, float noiseValue)
    {
        var normalized = (noiseValue + 1f) * 0.5f;
        var scaled = settings.BaseHeight + (int)MathF.Round(normalized * settings.Variation);
        return Math.Max(2, scaled);
    }

    private static void BuildColumn(
        IGeneratorContext context,
        ChunkEntity chunk,
        TerrainSettings settings,
        int columnHeight,
        int x,
        int z,
        int chunkHeight,
        int chunkBaseY,
        BlockEntity bedrock,
        BlockEntity water)
    {
        var surfaceBlock = context.NewBlockEntity(settings.SurfaceBlock);
        var subSurfaceBlock = context.NewBlockEntity(settings.SubSurfaceBlock);
        var deepBlock = context.NewBlockEntity(settings.DeepBlock);

        for (int y = 0; y < chunkHeight; y++)
        {
            var worldY = chunkBaseY + y;

            if (worldY <= 0)
            {
                chunk.SetBlock(x, y, z, bedrock);
                continue;
            }

            if (worldY < columnHeight - 4)
            {
                chunk.SetBlock(x, y, z, deepBlock);
                continue;
            }

            if (worldY < columnHeight - 1)
            {
                chunk.SetBlock(x, y, z, subSurfaceBlock);
                continue;
            }

            if (worldY == columnHeight - 1)
            {
                chunk.SetBlock(x, y, z, surfaceBlock);
                continue;
            }

            if (worldY <= settings.WaterLevel)
            {
                chunk.SetBlock(x, y, z, water);
                continue;
            }

            context.SetBlock(x, y, z, null);
        }
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

        context.CustomData[GeneratorContextKeys.BiomeMap] = fallback;
        return fallback;
    }

    /// <inheritdoc/>
    public string Name => "TerrainGenerator";

    /// <inheritdoc/>
    public Task ExecuteAsync(IGeneratorContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var chunk = context.GetChunk();
        var size = context.ChunkSize();
        var height = context.ChunkHeight();
        var worldBase = context.GetWorldPosition();
        var noise = CreateTerrainNoise(context.Seed);
        var chunkBaseY = (int)MathF.Round(worldBase.Y);
        var biomeMap = ResolveBiomeMap(context);
        var heightMap = new int[size, size];
        var bedrock = context.NewBlockEntity(BlockType.Bedrock);
        var water = context.NewBlockEntity(BlockType.Water);

        for (int x = 0; x < size; x++)
        {
            for (int z = 0; z < size; z++)
            {
                var biome = biomeMap[x, z];
                var settings = GetTerrainSettings(biome);
                var worldX = worldBase.X + x;
                var worldZ = worldBase.Z + z;
                var noiseValue = noise.GetNoise(worldX, worldZ);
                var columnHeight = CalculateColumnHeight(settings, noiseValue);
                heightMap[x, z] = columnHeight;
                BuildColumn(context, chunk, settings, columnHeight, x, z, height, chunkBaseY, bedrock, water);
            }
        }

        chunk.IsLightingDirty = true;
        context.CustomData[GeneratorContextKeys.HeightMap] = heightMap;

        return Task.CompletedTask;
    }
}
