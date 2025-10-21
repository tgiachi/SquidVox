using SquidVox.Voxel.Types;

namespace SquidVox.World3d.GameObjects;

/// <summary>
/// Represents a merged face for greedy meshing.
/// </summary>
internal struct MergedFace
{
    /// <summary>
    /// Gets or sets the block type.
    /// </summary>
    public BlockType BlockType { get; set; }

    /// <summary>
    /// Gets or sets the X position.
    /// </summary>
    public int X { get; set; }

    /// <summary>
    /// Gets or sets the Y position.
    /// </summary>
    public int Y { get; set; }

    /// <summary>
    /// Gets or sets the Z position.
    /// </summary>
    public int Z { get; set; }

    /// <summary>
    /// Gets or sets the width.
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// Gets or sets the height.
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    /// Gets or sets the side.
    /// </summary>
    public BlockSide Side { get; set; }
}
