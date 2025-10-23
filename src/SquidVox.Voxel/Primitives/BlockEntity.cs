using Microsoft.Xna.Framework;
using SquidVox.Voxel.Types;

namespace SquidVox.Voxel.Primitives;

/// <summary>
/// Represents a single block instance within a chunk, including its identifier and type.
/// </summary>
public class BlockEntity
{
    /// <summary>
    /// Gets or sets the unique identifier for the block instance.
    /// </summary>
    //public long Id { get; set; }

    /// <summary>
    /// Gets or sets the semantic type for the block.
    /// </summary>
    public BlockType BlockType { get; set; }

    /// <summary>
    /// Gets or sets the water level (0-7, where 7 is full source block, 0 is no water).
    /// </summary>
    public byte WaterLevel { get; set; }

    /// <summary>
    /// Gets or sets the light level (0-15, where 15 is full brightness, 0 is dark).
    /// </summary>
    public byte LightLevel { get; set; }

    /// <summary>
    /// Gets or sets the light color (RGB values 0-1).
    /// </summary>
    public Vector3 LightColor { get; set; }

    /// <summary>
    /// Initializes a new <see cref="BlockEntity"/> with the provided identifier and type.
    /// </summary>
    /// <param name="id">Unique identifier assigned to the block.</param>
    /// <param name="blockType">The type of block represented by this entity.</param>
    public BlockEntity(BlockType blockType)
    {
        BlockType = blockType;
        WaterLevel = blockType == BlockType.Water ? (byte)7 : (byte)0;
        LightLevel = 1;
        LightColor = Vector3.One;
    }

    public override string ToString() => $"BlockEntity({BlockType})";
}
