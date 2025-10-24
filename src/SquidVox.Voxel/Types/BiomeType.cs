namespace SquidVox.Voxel.Types;

/// <summary>
/// Defines biome classifications used during chunk generation.
/// </summary>
public enum BiomeType : byte
{
    /// <summary>
    /// Represents deep water regions.
    /// </summary>
    Ocean,

    /// <summary>
    /// Represents flat grassland regions.
    /// </summary>
    Plains,

    /// <summary>
    /// Represents densely vegetated regions.
    /// </summary>
    Forest,

    /// <summary>
    /// Represents elevated rocky regions.
    /// </summary>
    Mountains,
}
