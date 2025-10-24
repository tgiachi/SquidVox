namespace SquidVox.Voxel.Generators;

/// <summary>
/// Provides keys shared between generator steps.
/// </summary>
internal static class GeneratorContextKeys
{
    /// <summary>
    /// Identifies the biome map stored in the generator context.
    /// </summary>
    public const string BiomeMap = "BiomeMap";

    /// <summary>
    /// Identifies the terrain height map stored in the generator context.
    /// </summary>
    public const string HeightMap = "HeightMap";
}
