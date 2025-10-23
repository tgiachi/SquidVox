using SquidVox.Voxel.Types;

namespace SquidVox.Voxel.Data.Entities;

/// <summary>
/// Represents the definition data for a block type in the voxel system.
/// </summary>
public class BlockDefinitionData
{
    /// <summary>
    /// Gets or sets the type of the block.
    /// </summary>
    public BlockType BlockType { get; set; }

    /// <summary>
    /// Gets or sets the dictionary of block sides and their corresponding texture names.
    /// </summary>
    public Dictionary<BlockSide, string> Sides { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether the block is transparent.
    /// </summary>
    public bool IsTransparent { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the block is a liquid.
    /// </summary>
    public bool IsLiquid { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the block is solid.
    /// </summary>
    public bool IsSolid { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the block is a billboard (always faces camera).
    /// </summary>
    public bool IsBillboard { get; set; }

    // /// <summary>
    // /// Gets or sets a value indicating whether the block is affected by wind.
    // /// </summary>
    // public bool IsWindable { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the block renders as a camera-facing item.
    /// </summary>
    public bool IsItem { get; set; }

    // /// <summary>
    // /// Gets or sets the wind speed multiplier for the block.
    // /// </summary>
    // public float WindSpeed { get; set; } = 1.0f;

    /// <summary>
    /// Gets or sets the height of the block.
    /// </summary>
    public float Height { get; set; } = 1.0f;

    /// <summary>
    /// Adds a side texture to the block definition.
    /// </summary>
    /// <param name="type">The side of the block to add the texture for.</param>
    /// <param name="side">The texture name for the side.</param>
    public void AddSide(BlockSide type, string side)
    {
        Sides.Add(type, side);
    }

    /// <summary>
    /// Returns a string representation of the block definition data.
    /// </summary>
    /// <returns>A string containing the block type and number of sides.</returns>
    public override string ToString()
    {
        return "BlockDefinitionData { BlockType: " + BlockType + ", Sides: " + Sides.Count + " }";
    }
}
