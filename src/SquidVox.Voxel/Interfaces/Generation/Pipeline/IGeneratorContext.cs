using Microsoft.Xna.Framework;
using SquidVox.Core.Noise;
using SquidVox.Voxel.Primitives;

namespace SquidVox.Voxel.Interfaces.Generation.Pipeline;

/// <summary>
/// Provides shared context data for all generation steps in the chunk pipeline.
/// </summary>
public interface IGeneratorContext
{
    /// <summary>
    /// Gets the chunk being generated.
    /// </summary>
    ChunkEntity Chunk { get; }

    /// <summary>
    /// Gets the world position for the chunk.
    /// </summary>
    Vector3 WorldPosition { get; }

    /// <summary>
    /// Gets the noise generator used for terrain generation.
    /// </summary>
    FastNoiseLite NoiseGenerator { get; }

    /// <summary>
    /// Gets the seed used for procedural generation.
    /// </summary>
    int Seed { get; }

    /// <summary>
    /// Gets or sets custom data that can be shared between pipeline steps.
    /// </summary>
    IDictionary<string, object> CustomData { get; }
}
